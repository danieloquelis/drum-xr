using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Sentis;
using Unity.VisualScripting;
using UnityEngine;
using System.Linq;

public class RecognitionTest : MonoBehaviour
{
    public Texture2D testPicture;
    public ModelAsset modelAsset;
    [Range(0f, 1f)] public float confidenceThreshold = 0.4f;
    [Range(0f, 1f)] public float iouThreshold = 0.5f;

    private Worker worker;

    public float[] results;

    private void Start()
    {
        var model = ModelLoader.Load(modelAsset);
        
        // Log model information
        Debug.Log($"Model inputs: {string.Join(", ", model.inputs.Select(i => $"{i.name} ({string.Join(", ", i.shape)})"))}");
        Debug.Log($"Model outputs: {string.Join(", ", model.outputs.Select(o => o.name))}");
        
        worker = new Worker(model, BackendType.CPU);
        StartCoroutine(RunAI(testPicture));
    }

    private IEnumerator RunAI(Texture2D picture)
    {
        var inputTensor = PreprocessImage(picture);
        var schedule = worker.ScheduleIterable(inputTensor);
        while (schedule.MoveNext())
        {
            yield return null;   
        }
        var outputTensor = worker.PeekOutput() as Tensor<float>;
        outputTensor.ReadbackRequest();

        while (!outputTensor.IsReadbackRequestDone())
        {
            yield return null;
        }
        
        var output = outputTensor.ReadbackAndClone();
        ProcessOutput(output);
        
        output.Dispose();
        inputTensor.Dispose();
    }
    
    private Tensor<float> PreprocessImage(Texture2D texture)
    {
        texture = ResizeTo640X640(texture);
        texture = FlipTextureVertically(texture); // ✅ Apply flip

        Color32[] pixels = texture.GetPixels32();
        var input = new Tensor<float>(new TensorShape(1, 3, 640, 640));

        float[] floatPixels = new float[640 * 640 * 3];
        for (int y = 0; y < 640; y++)
        {
            for (int x = 0; x < 640; x++)
            {
                Color32 pixel = pixels[y * 640 + x];
                floatPixels[(y * 640 + x) * 3 + 0] = pixel.r / 255f;
                floatPixels[(y * 640 + x) * 3 + 1] = pixel.g / 255f;
                floatPixels[(y * 640 + x) * 3 + 2] = pixel.b / 255f;
            }
        }

        for (int c = 0; c < 3; c++)
        {
            for (int h = 0; h < 640; h++)
            {
                for (int w = 0; w < 640; w++)
                {
                    input[0, c, h, w] = floatPixels[(h * 640 + w) * 3 + c];
                }
            }
        }

        return input;
    }

    
    private static Texture2D ResizeTo640X640(Texture2D source)
    {
        RenderTexture rt = RenderTexture.GetTemporary(640, 640);
        RenderTexture.active = rt;
        Graphics.Blit(source, rt);

        Texture2D result = new Texture2D(640, 640, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, 640, 640), 0, 0);
        result.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        return result;
    }
    
    private Texture2D FlipTextureVertically(Texture2D original)
    {
        Texture2D flipped = new Texture2D(original.width, original.height, original.format, false);
        for (int y = 0; y < original.height; y++)
        {
            flipped.SetPixels(0, y, original.width, 1, original.GetPixels(0, original.height - y - 1, original.width, 1));
        }
        flipped.Apply();
        return flipped;
    }

    private void ProcessOutput(Tensor<float> output)
    {
        Debug.Log($"Output shape: {string.Join(", ", output.shape)}");

        int numBoxes = output.shape[2];

        var boxes = new List<(float[] box, float conf)>();

        for (int i = 0; i < numBoxes; i++)
        {
            float cx = output[0, 0, i];
            float cy = output[0, 1, i];
            float w  = output[0, 2, i];
            float h  = output[0, 3, i];
            float conf = output[0, 4, i];

            // Scale if normalized
            if (cx < 1f && cy < 1f && w < 1f && h < 1f)
            {
                cx *= 640f;
                cy *= 640f;
                w  *= 640f;
                h  *= 640f;
            }

            if (conf > confidenceThreshold)
            {
                boxes.Add((new float[] { cx, cy, w, h }, conf));
            }
        }

        Debug.Log($"Found {boxes.Count} objects above confidence threshold {confidenceThreshold}");

        for (int i = 0; i < boxes.Count; i++)
        {
            var (box, conf) = boxes[i];
            float x1 = box[0] - box[2] / 2f;
            float y1 = box[1] - box[3] / 2f;
            float x2 = box[0] + box[2] / 2f;
            float y2 = box[1] + box[3] / 2f;

            Debug.Log($"===> [{i}] Confidence: {conf:F2}, Box: ({x1:F0}, {y1:F0}, {x2:F0}, {y2:F0})");
        }

        // Apply NMS
        var (filteredBoxes, filteredConfs) = NonMaxSuppression(boxes, iouThreshold);

        Debug.Log($"After NMS: {filteredBoxes.Count} objects remaining");

        for (int i = 0; i < filteredBoxes.Count; i++)
        {
            var box = filteredBoxes[i];
            var conf = filteredConfs[i];

            float x1 = box[0] - box[2] / 2f;
            float y1 = box[1] - box[3] / 2f;
            float x2 = box[0] + box[2] / 2f;
            float y2 = box[1] + box[3] / 2f;

            Debug.Log($"[FINAL {i}] Confidence: {conf:F2}, Box: ({x1:F0}, {y1:F0}, {x2:F0}, {y2:F0})");
        }
    }




    private float Sigmoid(float x)
    {
        return 1f / (1f + Mathf.Exp(-x));
    }

    private (List<float[]> boxes, List<float> confs) NonMaxSuppression(List<(float[] box, float conf)> detections, float iouThreshold = 0.5f)
    {
        int N = detections.Count;
        if (N == 0) return (new List<float[]>(), new List<float>());

        float[] cx = new float[N];
        float[] cy = new float[N];
        float[] w = new float[N];
        float[] h = new float[N];
        float[] confs = new float[N];

        for (int i = 0; i < N; i++)
        {
            var (box, conf) = detections[i];
            cx[i] = box[0];
            cy[i] = box[1];
            w[i] = box[2];
            h[i] = box[3];
            confs[i] = conf;
        }

        float[] x1 = new float[N];
        float[] y1 = new float[N];
        float[] x2 = new float[N];
        float[] y2 = new float[N];
        float[] areas = new float[N];

        for (int i = 0; i < N; i++)
        {
            x1[i] = cx[i] - w[i] / 2f;
            y1[i] = cy[i] - h[i] / 2f;
            x2[i] = cx[i] + w[i] / 2f;
            y2[i] = cy[i] + h[i] / 2f;
            areas[i] = (x2[i] - x1[i]) * (y2[i] - y1[i]);
        }

        // Sort by confidence descending
        int[] indices = Enumerable.Range(0, N).OrderByDescending(i => confs[i]).ToArray();

        List<int> keep = new List<int>();

        while (indices.Length > 0)
        {
            int i = indices[0];
            keep.Add(i);

            if (indices.Length == 1)
                break;

            List<int> newIndices = new List<int>();
            for (int j = 1; j < indices.Length; j++)
            {
                int idx = indices[j];

                float xx1 = Mathf.Max(x1[i], x1[idx]);
                float yy1 = Mathf.Max(y1[i], y1[idx]);
                float xx2 = Mathf.Min(x2[i], x2[idx]);
                float yy2 = Mathf.Min(y2[i], y2[idx]);

                float iw = Mathf.Max(0f, xx2 - xx1);
                float ih = Mathf.Max(0f, yy2 - yy1);
                float intersection = iw * ih;
                float union = areas[i] + areas[idx] - intersection;
                float iou = intersection / union;

                if (iou <= iouThreshold)
                    newIndices.Add(idx);
            }

            indices = newIndices.ToArray();
        }

        // Return filtered boxes and confs
        var finalBoxes = keep.Select(i => new float[] { cx[i], cy[i], w[i], h[i] }).ToList();
        var finalConfs = keep.Select(i => confs[i]).ToList();
        return (finalBoxes, finalConfs);
    }


    private float CalculateIoU(float[] box1, float[] box2)
    {
        // Convert center format to corner format
        float x1_1 = box1[0] - box1[2] / 2;
        float y1_1 = box1[1] - box1[3] / 2;
        float x2_1 = box1[0] + box1[2] / 2;
        float y2_1 = box1[1] + box1[3] / 2;

        float x1_2 = box2[0] - box2[2] / 2;
        float y1_2 = box2[1] - box2[3] / 2;
        float x2_2 = box2[0] + box2[2] / 2;
        float y2_2 = box2[1] + box2[3] / 2;

        // Calculate intersection
        float x1 = Mathf.Max(x1_1, x1_2);
        float y1 = Mathf.Max(y1_1, y1_2);
        float x2 = Mathf.Min(x2_1, x2_2);
        float y2 = Mathf.Min(y2_1, y2_2);

        float intersection = Mathf.Max(0, x2 - x1) * Mathf.Max(0, y2 - y1);

        // Calculate areas
        float area1 = box1[2] * box1[3];
        float area2 = box2[2] * box2[3];

        // Calculate IoU
        return intersection / (area1 + area2 - intersection);
    }

    private void OnDestroy()
    {
        worker?.Dispose();
    }
}
