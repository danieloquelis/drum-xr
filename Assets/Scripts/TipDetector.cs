using System;
using System.Collections;
using PassthroughCameraSamples.MultiObjectDetection;
using UnityEngine;
using Unity.Sentis;

public class TipDetector : MonoBehaviour
{
    [Header("Model Config")]
    [SerializeField] private Vector2Int m_inputSize = new(560, 560);
    [SerializeField] private BackendType m_backend = BackendType.GPUCompute;
    [SerializeField] private ModelAsset m_sentisModel;
    [SerializeField] private int m_layersPerFrame = 25;
    [SerializeField, Range(0, 1)] private float m_scoreThreshold = 0.5f;

    [Header("UI")]
    [SerializeField] private SentisInferenceUiManager m_uiInference;
    [SerializeField] private TextAsset m_labelsAsset;

    public bool IsModelLoaded { get; private set; } = false;

    private Worker m_engine;
    private Tensor<float> m_input;
    private Tensor<float> m_pullBoxes;
    private Tensor<int> m_pullLabelIds;
    private Tensor<float> m_outputBoxes;
    private Tensor<int> m_outputLabelIds;

    private string[] m_labels;

    private IEnumerator m_schedule;
    private bool m_started = false;
    private bool m_isWaiting = false;
    private int m_download_state = 0;

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
        m_labels = m_labelsAsset.text.Split('\n');
        LoadModel();
    }

    private void LoadModel()
    {
        var model = ModelLoader.Load(m_sentisModel);
        m_engine = new Worker(model, m_backend);

        var dummy = TextureConverter.ToTensor(new Texture2D(m_inputSize.x, m_inputSize.y), m_inputSize.x, m_inputSize.y, 3);
        m_engine.Schedule(dummy);
        dummy.Dispose();

        Debug.Log("Sentis model loaded.");
        IsModelLoaded = true;
    }

    public bool IsRunning()
    {
        return m_started;
    }

    private void Update()
    {
        InferenceUpdate();
    }

    public void RunInference(Texture targetTexture)
    {
        if (!m_started && targetTexture)
        {
            m_input?.Dispose();

            m_uiInference.SetDetectionCapture(targetTexture);
            m_input = TextureConverter.ToTensor(targetTexture, m_inputSize.x, m_inputSize.y, 3);
            m_schedule = m_engine.ScheduleIterable(m_input);
            m_download_state = 0;
            m_started = true;
            Debug.LogWarning("RunInference Started");
        }
    }

    private void InferenceUpdate()
    {
        if (!m_started) return;
        try
        {
            switch (m_download_state)
            {
                case 0:
                    int it = 0;
                    while (m_schedule.MoveNext())
                    {
                        if (++it % m_layersPerFrame == 0)
                            return;
                    }
                    m_download_state = 1;
                    break;

                case 1:
                    m_pullBoxes = m_engine.PeekOutput("dets") as Tensor<float>;
                    if (m_pullBoxes?.dataOnBackend != null)
                    {
                        m_pullBoxes.ReadbackRequest();
                        m_isWaiting = true;
                        m_download_state = 2;
                    }
                    else
                    {
                        Debug.LogError("No output tensor for 'dets'");
                        m_download_state = 4;
                    }
                    break;

                case 2:
                    if (m_isWaiting && m_pullBoxes.IsReadbackRequestDone())
                    {
                        m_outputBoxes = m_pullBoxes.ReadbackAndClone();
                        m_isWaiting = false;
                        m_download_state = 3;
                    }
                    break;

                case 3:
                    m_pullLabelIds = m_engine.PeekOutput("labels") as Tensor<int>;
                    if (m_pullLabelIds?.dataOnBackend != null)
                    {
                        m_pullLabelIds.ReadbackRequest();
                        m_isWaiting = true;
                        m_download_state = 5;
                    }
                    else
                    {
                        Debug.LogError("No output tensor for 'labels'");
                        m_download_state = 4;
                    }
                    break;

                case 5:
                    if (m_isWaiting && m_pullLabelIds.IsReadbackRequestDone())
                    {
                        m_outputLabelIds = m_pullLabelIds.ReadbackAndClone();
                        m_isWaiting = false;
                        m_download_state = 6;
                    }
                    break;

                case 6:
                    // Filter only class "tip"
                    for (int i = 0; i < m_outputLabelIds.count; i++)
                    {
                        int labelIndex = m_outputLabelIds[i];
                        if (labelIndex < 0 || m_labels[labelIndex].Trim().ToLower() != "tip")
                        {
                            m_outputLabelIds[i] = -1;
                        }
                    }

                    m_uiInference.DrawUIBoxes(
                        m_outputBoxes,
                        m_outputLabelIds,
                        m_inputSize.x,
                        m_inputSize.y
                    );

                    m_outputBoxes?.Dispose();
                    m_outputLabelIds?.Dispose();

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
