using System.Collections.Generic;
using System.Linq;
using Unity.Sentis;
using UnityEngine;

namespace Utils
{
    public static class OnnxUtils
    {
        public static Tensor<float> TextureToTensor(Texture2D texture, int width, int height)
        {
            texture = ImageUtils.Resize(texture, width, height);
            texture = ImageUtils.FlipTextureVertically(texture); 

            var pixels = texture.GetPixels32();
            var input = new Tensor<float>(new TensorShape(1, 3, width, height));

            var floatPixels = new float[width * height * 3];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var pixel = pixels[y * height + x];
                    floatPixels[(y * height + x) * 3 + 0] = pixel.r / 255f;
                    floatPixels[(y * height + x) * 3 + 1] = pixel.g / 255f;
                    floatPixels[(y * height + x) * 3 + 2] = pixel.b / 255f;
                }
            }

            for (var c = 0; c < 3; c++)
            {
                for (var h = 0; h < height; h++)
                {
                    for (var w = 0; w < width; w++)
                    {
                        input[0, c, h, w] = floatPixels[(h * height + w) * 3 + c];
                    }
                }
            }

            return input;
        }
        
        public static (List<float[]> boxes, List<float> confidence) NonMaxSuppression(List<(float[] box, float conf)> detections, float iouThreshold = 0.5f)
        {
            var N = detections.Count;
            if (N == 0) return (new List<float[]>(), new List<float>());

            var cx = new float[N];
            var cy = new float[N];
            var w = new float[N];
            var h = new float[N];
            var confidences = new float[N];

            for (var i = 0; i < N; i++)
            {
                var (box, conf) = detections[i];
                cx[i] = box[0];
                cy[i] = box[1];
                w[i] = box[2];
                h[i] = box[3];
                confidences[i] = conf;
            }

            var x1 = new float[N];
            var y1 = new float[N];
            var x2 = new float[N];
            var y2 = new float[N];
            var areas = new float[N];

            for (var i = 0; i < N; i++)
            {
                x1[i] = cx[i] - w[i] / 2f;
                y1[i] = cy[i] - h[i] / 2f;
                x2[i] = cx[i] + w[i] / 2f;
                y2[i] = cy[i] + h[i] / 2f;
                areas[i] = (x2[i] - x1[i]) * (y2[i] - y1[i]);
            }

            // Sort by confidence descending
            var indices = Enumerable.Range(0, N).OrderByDescending(i => confidences[i]).ToArray();

            var keep = new List<int>();

            while (indices.Length > 0)
            {
                var i = indices[0];
                keep.Add(i);

                if (indices.Length == 1)
                    break;

                var newIndices = new List<int>();
                for (var j = 1; j < indices.Length; j++)
                {
                    var idx = indices[j];

                    var xx1 = Mathf.Max(x1[i], x1[idx]);
                    var yy1 = Mathf.Max(y1[i], y1[idx]);
                    var xx2 = Mathf.Min(x2[i], x2[idx]);
                    var yy2 = Mathf.Min(y2[i], y2[idx]);

                    var iw = Mathf.Max(0f, xx2 - xx1);
                    var ih = Mathf.Max(0f, yy2 - yy1);
                    var intersection = iw * ih;
                    var union = areas[i] + areas[idx] - intersection;
                    var iou = intersection / union;

                    if (iou <= iouThreshold)
                        newIndices.Add(idx);
                }

                indices = newIndices.ToArray();
            }

            // Return filtered boxes and confidences
            var finalBoxes = keep.Select(i => new float[] { cx[i], cy[i], w[i], h[i] }).ToList();
            var finalConfidences = keep.Select(i => confidences[i]).ToList();
            return (finalBoxes, finalConfidences);
        }
    }
}