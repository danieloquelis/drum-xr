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
    public Vector2Int inputSize = new(640, 640);
    [SerializeField] private BackendType m_backend = BackendType.GPUCompute;
    [SerializeField] private ModelAsset m_sentisModel;
    [SerializeField] private int m_layersPerFrame = 25;

    [Header("Detection Thresholds")]
    [Range(0f, 1f)] public float confidenceThreshold = 0.4f;
    [Range(0f, 1f)] public float iouThreshold = 0.5f;

    [Header("UI")]
    [SerializeField] private SentisInferenceUiManager m_uiInference;
    
    [SerializeField] private Camera cameraEye; // assign OVRCameraRig.CenterEyeAnchor or LeftEyeAnchor
    [SerializeField] private GameObject detectionPrefab; // sphere or cube
    [SerializeField] private EnvironmentRayCastSampleManager environmentRaycaster; // your existing one
    
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

        var dummy = new Texture2D(inputSize.x, inputSize.y);
        var dummyTensor = TextureConverter.ToTensor(dummy, inputSize.x, inputSize.y, 3);
        m_engine.Schedule(dummyTensor);
        dummyTensor.Dispose();

        Debug.Log("Sentis model loaded.");
        IsModelLoaded = true;
    }

    public bool IsRunning()
    {
        return m_started;
    }

    public void RunInference(Texture2D targetTexture)
    {
        if (m_started || !targetTexture) return;
        m_input?.Dispose();

        m_uiInference.SetDetectionCapture(targetTexture);
        m_input = OnnxUtils.TextureToTensor(targetTexture, inputSize.x, inputSize.y);
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
                            cx *= inputSize.x;
                            cy *= inputSize.y;
                            w  *= inputSize.x;
                            h  *= inputSize.y;
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
                    
                    for (var i = 0; i < filteredBoxes.Count; i++)
                    {
                        var box = filteredBoxes[i];
                        var cx = box[0]; // center x in input image space (640)
                        var cy = box[1]; // center y in input image space (640)

                        // ✅ Normalize to 0–1 range for viewport space
                        var viewportX = cx / inputSize.x;
                        var viewportY = cy / inputSize.y;

                        // ✅ Invert Y to match Unity viewport space
                        var viewportPos = new Vector2(viewportX, 1.0f - viewportY);

                        // ✅ Build ray from camera (e.g., CenterEyeAnchor)
                        Ray ray = cameraEye.ViewportPointToRay(viewportPos);

                        // ✅ Raycast into scene
                        var hitPoint = environmentRaycaster.PlaceGameObjectByScreenPos(ray);
                        if (hitPoint.HasValue)
                        {
                            Instantiate(detectionPrefab, hitPoint.Value, Quaternion.identity);
                            Debug.Log($"Spawned detection at {hitPoint.Value}");
                        }
                        else
                        {
                            Debug.LogWarning($"Raycast failed for detection[{i}] at viewport {viewportPos}");
                        }
                    }


                    
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
