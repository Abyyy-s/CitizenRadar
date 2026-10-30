
using OpenCvSharp;
using OpenCvSharp.Dnn;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using CitizenRadar.Models;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CitizenRadar.Core;

/// <summary>
/// Runs YOLOv8n ONNX inference using ONNX Runtime CUDA.
/// </summary>
public class Detector : IDisposable
{
    private readonly InferenceSession _session;
    private readonly string _inputName;
    private readonly int _inputSize;
    private bool _shapeVerified;
    private bool _disposed;

    public Detector(string onnxModelPath, int inputSize = 640)
    {
        if (!File.Exists(onnxModelPath))
        {
            throw new FileNotFoundException(
                $"ONNX model not found: {onnxModelPath}");
        }

        _inputSize = inputSize;
        _shapeVerified = false;

        Console.WriteLine(
            "Detector: Initialising CUDA execution provider on device 0...");

        using var sessionOptions =
            new Microsoft.ML.OnnxRuntime.SessionOptions();

        sessionOptions.GraphOptimizationLevel =
            GraphOptimizationLevel.ORT_ENABLE_ALL;

        using (var cudaOptions = new OrtCUDAProviderOptions())
        {
            cudaOptions.UpdateOptions(
                new Dictionary<string, string>
                {
                    ["device_id"] = "0"
                });

            try
            {
                sessionOptions.AppendExecutionProvider_CUDA(cudaOptions);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"Detector: CUDA initialisation failed: {ex.Message}");

                throw new InvalidOperationException(
                    "ONNX Runtime CUDA provider failed to initialise.", ex);
            }
        }

        _session = new InferenceSession(onnxModelPath, sessionOptions);
        _inputName = _session.InputMetadata.Keys.First();

        Console.WriteLine("Detector: CUDA provider configured.");
        Console.WriteLine($"Detector: Input name = '{_inputName}'");
        Console.WriteLine($"Detector: Input size = {_inputSize} x {_inputSize}");
        Console.WriteLine($"Detector: Model loaded from '{onnxModelPath}'");
    }

    public List<Detection> Detect(Mat frame)
    {
        int originalWidth = frame.Width;
        int originalHeight = frame.Height;

        using var blob = CvDnn.BlobFromImage(
            frame,
            scaleFactor: 1.0 / 255.0,
            size: new Size(_inputSize, _inputSize),
            mean: new Scalar(0, 0, 0),
            swapRB: true,
            crop: false);

        int totalElements = 3 * _inputSize * _inputSize;
        var inputData = new float[totalElements];

        Marshal.Copy(blob.Data, inputData, 0, totalElements);

        var inputTensor = new DenseTensor<float>(
            inputData,
            new[] { 1, 3, _inputSize, _inputSize });

        var ortInputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor(_inputName, inputTensor)
        };

        using var outputCollection = RunInference(ortInputs);

        var firstOutput = outputCollection.First();
        var outputTensor = firstOutput.AsTensor<float>();

        if (!_shapeVerified)
        {
            Console.WriteLine(
                $"Detector: Output shape = [{string.Join(" x ", outputTensor.Dimensions.ToArray())}]");

            _shapeVerified = true;
        }

        float[] outputData = outputTensor.ToArray();

        int numAttributes = outputTensor.Dimensions[1];
        int numPredictions = outputTensor.Dimensions[2];

        return ParseOutput(
            outputData,
            numAttributes,
            numPredictions,
            originalWidth,
            originalHeight,
            _inputSize);
    }

    private IDisposableReadOnlyCollection<DisposableNamedOnnxValue>
        RunInference(IReadOnlyCollection<NamedOnnxValue> inputs)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            return _session.Run(inputs);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"Detector: ORT inference failed: {ex.Message}");

            throw;
        }
        finally
        {
            stopwatch.Stop();

            Console.WriteLine(
                $"Inference time: {stopwatch.Elapsed.TotalMilliseconds:F1} ms");
        }
    }

    private static List<Detection> ParseOutput(
        float[] data,
        int numAttributes,
        int numPredictions,
        int originalWidth,
        int originalHeight,
        int inputSize)
    {
        // Scale model coordinates back to the original frame size.
        float xScale = (float)originalWidth / inputSize;
        float yScale = (float)originalHeight / inputSize;

        int stride = numPredictions;

        var boxes = new List<Rect>();
        var confidences = new List<float>();
        var classIds = new List<int>();

        for (int i = 0; i < numPredictions; i++)
        {
            float maxScore = 0f;
            int maxClassId = -1;

            for (int c = 4; c < numAttributes; c++)
            {
                float score = data[c * stride + i];

                if (score > maxScore)
                {
                    maxScore = score;
                    maxClassId = c - 4;
                }
            }

            if (maxScore < Config.ConfidenceThreshold)
                continue;

            if (!Config.VehicleClassIds.Contains(maxClassId))
                continue;

            float cx = data[i];
            float cy = data[stride + i];
            float w = data[2 * stride + i];
            float h = data[3 * stride + i];

            int x1 = (int)((cx - w / 2f) * xScale);
            int y1 = (int)((cy - h / 2f) * yScale);
            int bw = (int)(w * xScale);
            int bh = (int)(h * yScale);

            boxes.Add(new Rect(x1, y1, bw, bh));
            confidences.Add(maxScore);
            classIds.Add(maxClassId);
        }

        CvDnn.NMSBoxes(
            boxes,
            confidences,
            Config.ConfidenceThreshold,
            Config.NmsThreshold,
            out int[] indices);

        var detections = new List<Detection>();

        foreach (int idx in indices)
        {
            var box = boxes[idx];

            detections.Add(new Detection
            {
                BBox = new Rect2f(
                    box.X,
                    box.Y,
                    box.Width,
                    box.Height),

                ClassId = classIds[idx],
                Confidence = confidences[idx],

                BottomCenter = new Point2f(
                    box.X + box.Width / 2f,
                    box.Y + box.Height)
            });
        }

        return detections;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _session.Dispose();
        _disposed = true;

        GC.SuppressFinalize(this);
    }
}
