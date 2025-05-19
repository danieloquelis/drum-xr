using System.Collections;
using System.Collections.Generic;
using Unity.Sentis;
using UnityEngine;
using Utils;

public class RecognitionTest : MonoBehaviour
{
    public Texture2D testPicture;
    public ModelAsset modelAsset;
    [Range(0f, 1f)] public float confidenceThreshold = 0.4f;
    [Range(0f, 1f)] public float iouThreshold = 0.5f;

    private Worker worker;
    
    private void Start()
    {
        var model = ModelLoader.Load(modelAsset);
        worker = new Worker(model, BackendType.CPU);
        StartCoroutine(RunAI(testPicture));
    }

    private IEnumerator RunAI(Texture2D picture)
    {
        var inputTensor = OnnxUtils.TextureToTensor(picture, 640, 640);
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
    
    
    private void ProcessOutput(Tensor<float> output)
    {
        var numBoxes = output.shape[2];
        var boxes = new List<(float[] box, float conf)>();

        for (var i = 0; i < numBoxes; i++)
        {
            var cx = output[0, 0, i];
            var cy = output[0, 1, i];
            var w  = output[0, 2, i];
            var h  = output[0, 3, i];
            var conf = output[0, 4, i];

            if (cx < 1f && cy < 1f && w < 1f && h < 1f)
            {
                cx *= 640f;
                cy *= 640f;
                w  *= 640f;
                h  *= 640f;
            }

            if (conf > confidenceThreshold)
            {
                boxes.Add((new[] { cx, cy, w, h }, conf));
            }
        }

        var (filteredBoxes, filteredConfidences) = OnnxUtils.NonMaxSuppression(boxes, iouThreshold);
        
        for (var i = 0; i < filteredBoxes.Count; i++)
        {
            var box = filteredBoxes[i];
            var conf = filteredConfidences[i];

            var x1 = box[0] - box[2] / 2f;
            var y1 = box[1] - box[3] / 2f;
            var x2 = box[0] + box[2] / 2f;
            var y2 = box[1] + box[3] / 2f;

            Debug.Log($"[{i}] Confidence: {conf:F2}, Box: ({x1:F0}, {y1:F0}, {x2:F0}, {y2:F0})");
        }
    }

    private void OnDestroy()
    {
        worker?.Dispose();
    }
}
