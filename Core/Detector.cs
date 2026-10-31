using OpenCvSharp;
using OpenCvSharp.Dnn;
using CitizenRadar.Models;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OrtSessionOptions = Microsoft.ML.OnnxRuntime.SessionOptions;
using System.Runtime.InteropServices;

namespace CitizenRadar.Core;

/// <summary>
/// High-performance YOLOv8 detector with automatic GPU (CUDA) acceleration
/// using ONNX Runtime, with automatic fallback to CPU if CUDA is unavailable.
/// </summary>
public class Detector : IDisposable
{
    private readonly InferenceSession? _session;
    private readonly Net? _cvNet; // Fallback OpenCV DNN if needed
    private readonly int _inputSize;
    private readonly string _inputName = "images";
    private readonly string _outputName = "output0";

    public string BackendName { get; }
    public bool IsGpuAccelerated { get; }

    private static bool _cudaPreloaded = false;
    private static void PreloadCudaLibraries()
    {
        if (_cudaPreloaded) return;
        _cudaPreloaded = true;

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return;

        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string[] searchDirs = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "runtimes", "linux-x64", "native"),
            AppContext.BaseDirectory,
            Path.Combine(home, ".local", "lib", "python3.14", "site-packages", "nvidia", "cu13", "lib"),
            Path.Combine(home, ".local", "lib", "python3.14", "site-packages", "nvidia", "cudnn", "lib"),
            "/usr/lib64"
        };

        string[] libsToLoad = new[]
        {
            "libcuda.so.1",
            "libcudart.so.13",
            "libcurand.so.10",
            "libcublasLt.so.13",
            "libcublas.so.13",
            "libcudnn_ops.so",
            "libcudnn_cnn.so",
            "libcudnn_adv.so",
            "libcudnn_graph.so",
            "libcudnn_heuristic.so",
            "libcudnn_engines_precompiled.so",
            "libcudnn.so"
        };

        foreach (var lib in libsToLoad)
        {
            foreach (var dir in searchDirs)
            {
                var fullPath = Path.Combine(dir, lib);
                if (File.Exists(fullPath))
                {
                    if (NativeLibrary.TryLoad(fullPath, out _))
                        break;
                }
            }
        }
    }

    public Detector(string onnxModelPath, int inputSize = 640)
    {
        if (!File.Exists(onnxModelPath))
            throw new FileNotFoundException($"ONNX model not found: {onnxModelPath}");

        _inputSize = inputSize;

        // Preload CUDA 13 & cuDNN 9 shared libraries into process space
        PreloadCudaLibraries();

        // ── 1. Attempt GPU Acceleration with ONNX Runtime (CUDA Provider) ──
        InferenceSession? session = null;
        bool isGpu = false;
        string backend = "CPU";

        try
        {
            var gpuOptions = new OrtSessionOptions();
            gpuOptions.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
            gpuOptions.AppendExecutionProvider_CUDA(0);

            session = new InferenceSession(onnxModelPath, gpuOptions);

            // Execute a warm-up dummy pass to verify GPU kernel linkage (cuDNN, cuBLAS)
            string testInp = session.InputMetadata.Keys.FirstOrDefault() ?? "images";
            var dummyTensor = new DenseTensor<float>(new[] { 1, 3, _inputSize, _inputSize });
            using (var testOut = session.Run(new[] { NamedOnnxValue.CreateFromTensor(testInp, dummyTensor) }))
            {
                // Successful warm-up
            }

            isGpu = true;
            backend = "NVIDIA CUDA (RTX 4050)";
            Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║  🚀 GPU ACCELERATION ACTIVE: NVIDIA CUDA Execution Provider     ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[INFO] CUDA initialization skipped: {ex.Message}");
            session?.Dispose();
            session = null;
            Console.WriteLine("[INFO] Initializing ONNX Runtime CPU Provider...");

            try
            {
                var cpuOptions = new OrtSessionOptions();
                cpuOptions.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
                cpuOptions.AppendExecutionProvider_CPU();
                session = new InferenceSession(onnxModelPath, cpuOptions);
                backend = "ONNX Runtime (CPU)";
            }
            catch (Exception exCpu)
            {
                Console.WriteLine($"[INFO] ONNX Runtime CPU failed ({exCpu.Message}), falling back to OpenCV DNN.");
                _cvNet = CvDnn.ReadNetFromOnnx(onnxModelPath);
                backend = "OpenCV DNN (CPU)";
            }
        }

        _session = session;
        IsGpuAccelerated = isGpu;
        BackendName = backend;

        if (_session != null)
        {
            // Auto-detect input/output names if available
            if (_session.InputMetadata.Count > 0)
                _inputName = _session.InputMetadata.Keys.First();
            if (_session.OutputMetadata.Count > 0)
                _outputName = _session.OutputMetadata.Keys.First();

            Console.WriteLine($"Detector: Loaded model from {onnxModelPath} (Backend: {BackendName}, Input: {_inputName}, Output: {_outputName})");
        }
        else
        {
            Console.WriteLine($"Detector: Loaded model from {onnxModelPath} (Backend: {BackendName})");
        }
    }

    /// <summary>
    /// Runs inference on a single frame and returns vehicle detections only.
    /// </summary>
    public List<Detection> Detect(Mat frame)
    {
        int originalWidth = frame.Width;
        int originalHeight = frame.Height;

        if (_session != null)
        {
            return DetectWithOnnxRuntime(frame, originalWidth, originalHeight);
        }
        else
        {
            return DetectWithOpenCv(frame, originalWidth, originalHeight);
        }
    }

    private List<Detection> DetectWithOnnxRuntime(Mat frame, int originalWidth, int originalHeight)
    {
        // ── Preprocessing ─────────────────────────────────────────────
        // Create 640×640 float tensor normalized to [0,1], RGB order
        using var resized = new Mat();
        Cv2.Resize(frame, resized, new Size(_inputSize, _inputSize));

        using var rgb = new Mat();
        Cv2.CvtColor(resized, rgb, ColorConversionCodes.BGR2RGB);

        int channelSize = _inputSize * _inputSize;
        float[] inputData = new float[3 * channelSize];

        unsafe
        {
            byte* dataPtr = rgb.DataPointer;
            for (int i = 0; i < channelSize; i++)
            {
                // Normalise [0..255] to [0..1]
                inputData[i] = dataPtr[i * 3 + 0] / 255.0f;                   // R
                inputData[channelSize + i] = dataPtr[i * 3 + 1] / 255.0f;     // G
                inputData[2 * channelSize + i] = dataPtr[i * 3 + 2] / 255.0f; // B
            }
        }

        var inputTensor = new DenseTensor<float>(inputData, new[] { 1, 3, _inputSize, _inputSize });

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor(_inputName, inputTensor)
        };

        // ── Forward Pass ──────────────────────────────────────────────
        using var results = _session!.Run(inputs);
        var outputTensor = results.First().AsTensor<float>();

        // ── Parse Output [1, 84, 8400] ────────────────────────────────
        int numAttributes = outputTensor.Dimensions[1]; // 84
        int numPredictions = outputTensor.Dimensions[2]; // 8400

        float xScale = (float)originalWidth / _inputSize;
        float yScale = (float)originalHeight / _inputSize;

        var boxes = new List<Rect>();
        var confidences = new List<float>();
        var classIds = new List<int>();

        for (int i = 0; i < numPredictions; i++)
        {
            float maxScore = 0f;
            int maxClassId = -1;

            for (int c = 4; c < numAttributes; c++)
            {
                float score = outputTensor[0, c, i];
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

            float cx = outputTensor[0, 0, i];
            float cy = outputTensor[0, 1, i];
            float w = outputTensor[0, 2, i];
            float h = outputTensor[0, 3, i];

            int x1 = (int)((cx - w / 2f) * xScale);
            int y1 = (int)((cy - h / 2f) * yScale);
            int bw = (int)(w * xScale);
            int bh = (int)(h * yScale);

            boxes.Add(new Rect(x1, y1, bw, bh));
            confidences.Add(maxScore);
            classIds.Add(maxClassId);
        }

        // ── Non-Maximum Suppression ───────────────────────────────────
        CvDnn.NMSBoxes(boxes, confidences, Config.ConfidenceThreshold, Config.NmsThreshold, out int[] indices);

        var detections = new List<Detection>();
        foreach (int idx in indices)
        {
            var box = boxes[idx];
            var bottomCenter = new Point2f(box.X + box.Width / 2f, box.Y + box.Height);

            detections.Add(new Detection
            {
                BBox = new Rect2f(box.X, box.Y, box.Width, box.Height),
                ClassId = classIds[idx],
                Confidence = confidences[idx],
                BottomCenter = bottomCenter
            });
        }

        return detections;
    }

    private List<Detection> DetectWithOpenCv(Mat frame, int originalWidth, int originalHeight)
    {
        var blob = CvDnn.BlobFromImage(
            frame,
            scaleFactor: 1.0 / 255.0,
            size: new Size(_inputSize, _inputSize),
            mean: new Scalar(0, 0, 0),
            swapRB: true,
            crop: false
        );

        _cvNet!.SetInput(blob);
        var output = _cvNet.Forward();

        int numAttributes = output.Size(1);
        int numPredictions = output.Size(2);

        float xScale = (float)originalWidth / _inputSize;
        float yScale = (float)originalHeight / _inputSize;

        var output2D = output.Reshape(1, numAttributes);
        var boxes = new List<Rect>();
        var confidences = new List<float>();
        var classIds = new List<int>();

        unsafe
        {
            float* data = (float*)output2D.Data;
            int stride = numPredictions;

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

                float cx = data[0 * stride + i];
                float cy = data[1 * stride + i];
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
        }

        output2D.Dispose();
        blob.Dispose();
        output.Dispose();

        CvDnn.NMSBoxes(boxes, confidences, Config.ConfidenceThreshold, Config.NmsThreshold, out int[] indices);

        var detections = new List<Detection>();
        foreach (int idx in indices)
        {
            var box = boxes[idx];
            var bottomCenter = new Point2f(box.X + box.Width / 2f, box.Y + box.Height);

            detections.Add(new Detection
            {
                BBox = new Rect2f(box.X, box.Y, box.Width, box.Height),
                ClassId = classIds[idx],
                Confidence = confidences[idx],
                BottomCenter = bottomCenter
            });
        }

        return detections;
    }

    public void Dispose()
    {
        _session?.Dispose();
        _cvNet?.Dispose();
        GC.SuppressFinalize(this);
    }
}
