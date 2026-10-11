using OpenCvSharp;
using OpenCvSharp.Dnn;
using CitizenRadar.Models;

namespace CitizenRadar.Core;

public class Detector
{
    private readonly Net _net;

    private readonly float _confidenceThreshold;

    private readonly float _nmsThreshold;

    private readonly Size _inputSize = new Size(640, 640);


    // YOLOv8 COCO vehicle classes
    private readonly Dictionary<int, string> _vehicleClasses = new()
    {
        { 2, "car" },
        { 3, "motorcycle" },
        { 5, "bus" },
        { 7, "truck" }
    };


    public Detector(
        string modelPath,
        float confidenceThreshold = 0.4f,
        float nmsThreshold = 0.45f)
    {
        _confidenceThreshold = confidenceThreshold;

        _nmsThreshold = nmsThreshold;

        _net = Net.ReadNetFromONNX(modelPath)
    ?? throw new InvalidOperationException("Failed to load ONNX model.");

        _net.SetPreferableBackend(Backend.OPENCV);

        _net.SetPreferableTarget(Target.CPU);
    }


    public List<Detection> Detect(Mat frame)
    {
        var detections = new List<Detection>();


        // ----------------------------------------
        // STEP 1: Convert image into YOLO input
        // ----------------------------------------

        using var blob = CvDnn.BlobFromImage(
            frame,
            1.0 / 255.0,
            _inputSize,
            new Scalar(),
            swapRB: true,
            crop: false
        );


        _net.SetInput(blob);


        // ----------------------------------------
        // STEP 2: Run YOLO
        // ----------------------------------------

        using var output = _net.Forward();


        // ----------------------------------------
        // STEP 3: Read YOLO output
        // ----------------------------------------

        ParseOutput(
            output,
            frame.Size(),
            detections
        );


        // ----------------------------------------
        // STEP 4: Apply Non-Maximum Suppression
        // ----------------------------------------

        return ApplyNms(detections);
    }


    private void ParseOutput(
        Mat output,
        Size frameSize,
        List<Detection> detections)
    {
        int dimensions = output.Size(1);

        int rows = output.Size(2);


        // YOLOv8 ONNX normally produces:
        //
        // [1, 84, 8400]
        //
        // 4 values = bounding box
        // 80 values = COCO classes


        if (dimensions < 5)
        {
            throw new Exception(
                $"Unexpected YOLO output dimensions: {dimensions}"
            );
        }


        float xScale =
            frameSize.Width / (float)_inputSize.Width;

        float yScale =
            frameSize.Height / (float)_inputSize.Height;


        for (int i = 0; i < rows; i++)
        {
            float centerX = output.At<float>(0, 0, i);

            float centerY = output.At<float>(0, 1, i);

            float width = output.At<float>(0, 2, i);

            float height = output.At<float>(0, 3, i);


            int bestClassId = -1;

            float bestConfidence = 0f;


            // Start at 4 because:
            //
            // 0 = center X
            // 1 = center Y
            // 2 = width
            // 3 = height
            //
            // 4 onward = class scores

            for (int classId = 4; classId < dimensions; classId++)
            {
                float confidence =
                    output.At<float>(0, classId, i);


                if (confidence > bestConfidence)
                {
                    bestConfidence = confidence;

                    bestClassId = classId - 4;
                }
            }


            // We only care about vehicles.

            if (!_vehicleClasses.ContainsKey(bestClassId))
            {
                continue;
            }


            if (bestConfidence < _confidenceThreshold)
            {
                continue;
            }


            // Convert center coordinates
            // into top-left coordinates.

            float x =
                (centerX - width / 2f) * xScale;

            float y =
                (centerY - height / 2f) * yScale;

            float boxWidth =
                width * xScale;

            float boxHeight =
                height * yScale;


            // Keep box inside image.

            x = Math.Max(0, x);

            y = Math.Max(0, y);

            boxWidth =
                Math.Min(boxWidth, frameSize.Width - x);

            boxHeight =
                Math.Min(boxHeight, frameSize.Height - y);


            if (boxWidth <= 0 || boxHeight <= 0)
            {
                continue;
            }


            var boundingBox = new Rect2f(
                x,
                y,
                boxWidth,
                boxHeight
            );


            detections.Add(
                new Detection(
                    bestClassId,
                    _vehicleClasses[bestClassId],
                    bestConfidence,
                    boundingBox
                )
            );
        }
    }


    private List<Detection> ApplyNms(
        List<Detection> detections)
    {
        if (detections.Count == 0)
        {
            return detections;
        }


        var boxes = new List<Rect>();

        var scores = new List<float>();


        foreach (var detection in detections)
        {
            boxes.Add(
                new Rect(
                    (int)detection.BoundingBox.X,
                    (int)detection.BoundingBox.Y,
                    (int)detection.BoundingBox.Width,
                    (int)detection.BoundingBox.Height
                )
            );

            scores.Add(detection.Confidence);
        }


        CvDnn.NMSBoxes(
            boxes,
            scores,
            _confidenceThreshold,
            _nmsThreshold,
            out int[] indices
        );


        var result = new List<Detection>();


        foreach (int index in indices)
        {
            result.Add(detections[index]);
        }


        return result;
    }
}