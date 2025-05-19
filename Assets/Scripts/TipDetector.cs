using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Sentis;
using PassthroughCameraSamples.MultiObjectDetection;
using Utils;

public class TipDetector : MonoBehaviour
{
    [Header("Model Config")]
    [SerializeField] private Vector2Int m_inputSize = new(640, 640);
    [SerializeField] private BackendType m_backend = BackendType.GPUCompute;
    [SerializeField] private ModelAsset m_sentisModel;
    [SerializeField] private int m_layersPerFrame = 25;

    [Header("Detection Thresholds")]
    [Range(0f, 1f)] public float confidenceThreshold = 0.4f;
    [Range(0f, 1f)] public float iouThreshold = 0.5f;

    [Header("UI")]
    [SerializeField] private SentisInferenceUiManager m_uiInference;

    public bool IsModelLoaded { get; private set; } = false;

    private Worker m_engine;
    private Tensor<float> m_input;
    private Tensor<float> m_pullBoxes;
    private Tensor<float> m_outputBoxes;

    private IEnumerator m_schedule;
    private bool m_started = false;
    private bool m_isWaiting = false;
    private int m_downloadState = 0;

    private void OnDestroy()
    {
        if (m_schedule != null)
            StopCoroutine(m_schedule);

        m_input?.Dispose();
        m_engine?.Dispose();
    }

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(0.05f);
        LoadModel();
    }

    private void LoadModel()
    {
        var model = ModelLoader.Load(m_sentisModel);
        m_engine = new Worker(model, m_backend);

        var dummy = new Texture2D(m_inputSize.x, m_inputSize.y);
        var dummyTensor = TextureConverter.ToTensor(dummy, m_inputSize.x, m_inputSize.y, 3);
        m_engine.Schedule(dummyTensor);
        dummyTensor.Dispose();

        Debug.Log("Sentis model loaded.");
        IsModelLoaded = true;
    }

    public bool IsRunning()
    {
        return m_started;
    }

    public void RunInference(Texture targetTexture)
    {
        if (m_started || !targetTexture) return;
        m_input?.Dispose();

        m_uiInference.SetDetectionCapture(targetTexture);
        
        // var convertedTexture = new Texture2D(targetTexture.width, targetTexture.height, TextureFormat.RGBA32, false);
        // var rt = RenderTexture.GetTemporary(targetTexture.width, targetTexture.height);
        // Graphics.Blit(targetTexture, rt);
        // RenderTexture.active = rt;
        // convertedTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        // convertedTexture.Apply();
        // RenderTexture.ReleaseTemporary(rt);
        
        //m_input = OnnxUtils.TextureToTensor(convertedTexture, m_inputSize.x, m_inputSize.y);
        m_input = TextureConverter.ToTensor(targetTexture, m_inputSize.x, m_inputSize.y, 3);
        m_schedule = m_engine.ScheduleIterable(m_input);
        
        m_downloadState = 0;
        m_started = true;
    }

    private void Update()
    {
        InferenceUpdate();
    }

    private void InferenceUpdate()
    {
        if (!m_started) return;
        try
        {
            switch (m_downloadState)
            {
                case 0:
                    var it = 0;
                    while (m_schedule.MoveNext())
                    {
                        if (++it % m_layersPerFrame == 0)
                            return;
                    }
                    m_downloadState = 1;
                    break;

                case 1:
                    m_pullBoxes = m_engine.PeekOutput() as Tensor<float>;
                    if (m_pullBoxes?.dataOnBackend != null)
                    {
                        m_pullBoxes.ReadbackRequest();
                        m_isWaiting = true;
                        m_downloadState = 2;
                    }
                    else
                    {
                        Debug.LogError("No output tensor found.");
                        m_downloadState = 4;
                    }
                    break;

                case 2:
                    if (m_isWaiting && m_pullBoxes.IsReadbackRequestDone())
                    {
                        m_outputBoxes = m_pullBoxes.ReadbackAndClone();
                        m_isWaiting = false;
                        m_downloadState = 6;
                    }
                    break;

                case 6:
                    var boxes = new List<(float[] box, float conf)>();
                    var numBoxes = m_outputBoxes.shape[2];

                    for (var i = 0; i < numBoxes; i++)
                    {
                        var cx = m_outputBoxes[0, 0, i];
                        var cy = m_outputBoxes[0, 1, i];
                        var w  = m_outputBoxes[0, 2, i];
                        var h  = m_outputBoxes[0, 3, i];
                        var conf = m_outputBoxes[0, 4, i];

                        if (cx < 1f && cy < 1f && w < 1f && h < 1f)
                        {
                            cx *= m_inputSize.x;
                            cy *= m_inputSize.y;
                            w  *= m_inputSize.x;
                            h  *= m_inputSize.y;
                        }

                        if (conf > confidenceThreshold)
                        {
                            boxes.Add((new[] { cx, cy, w, h }, conf));
                        }
                    }

                    var (filteredBoxes, _) = OnnxUtils.NonMaxSuppression(boxes, iouThreshold);
                    m_uiInference.DrawUIBoxes(filteredBoxes,  m_inputSize.x, m_inputSize.y);
                    m_outputBoxes?.Dispose();
                    m_started = false;
                    break;

                case 4:
                    m_uiInference.OnObjectDetectionError();
                    m_started = false;
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Inference exception: {e.Message}");
            m_started = false;
        }
    }
} 
