using Meta.XR;
using Meta.XR.ImmersiveDebugger;
using Models;
using Newtonsoft.Json;
using PassthroughCameraSamples;
using Roboflow;
using UnityEngine;
using UnityEngine.Assertions;

namespace Detection
{
    public class DetectionManager : MonoBehaviour
    {
        [Header("PCA")] 
        [SerializeField] private WebCamTextureManager webCamTextureManager;
    
        [Header("Roboflow")]
        [SerializeField] private RoboflowInference roboflowInference;
    
        [Header("Controls Configuration")]
        [SerializeField] private OVRInput.RawButton actionButton = OVRInput.RawButton.A;

        [Header("Placement")]
        [SerializeField] private EnvironmentRaycastManager environmentRaycastManager;
        [SerializeField] private DetectionMarkerSpawner detectionMarkerSpawner;
    
        [Header("UI")] 
        [SerializeField] private TextMesh tooltip;
    
        private bool m_isLoading;
        private PassthroughCameraEye Eye => webCamTextureManager.Eye;

        private void Start()
        {
            Assert.IsNotNull(roboflowInference, "RoboflowInference prefab is not set");
            Assert.IsNotNull(tooltip, "Tooltip text is not set");
        
            tooltip.text = "Press " + actionButton + " to trigger detection";
        
            roboflowInference.onStart.AddListener(OnDetectionStart);
            roboflowInference.onSuccess.AddListener(OnDetectionSuccess);
            roboflowInference.onError.AddListener(OnDetectionError);
        }

        private void Update()
        {
            if (!m_isLoading && OVRInput.GetDown(actionButton))
            {
                roboflowInference.RunInference();
            }
        }

        private void OnDetectionStart()
        {
            m_isLoading = true;
            tooltip.text = "Detecting...";
        }

        [DebugMember]
        private void OnDetectionSuccess(string result)
        {
            m_isLoading = false;
        
            var response = JsonConvert.DeserializeObject<RootResponse>(result);
            if (response?.Outputs == null || response.Outputs.Count == 0)
            {
                tooltip.text = "No detections found. Press button to detect again.";
                return;
            }

            var detection = response.Outputs[0];
            var predictions = detection.RawRecognition?.Predictions;

            if (predictions == null || predictions.Count == 0)
            {
                tooltip.text = "No drums detected. Press button to detect again.";
                return;
            }

            var imageWidth = detection.RawRecognition.Image.Width;
            var imageHeight = detection.RawRecognition.Image.Height;
            var camRes = PassthroughCameraUtils.GetCameraIntrinsics(Eye).Resolution;

            foreach (var prediction in predictions)
            {
                Debug.Log($"Detected {prediction.Class} at ({prediction.X}, {prediction.Y})");

                var px = Mathf.RoundToInt(prediction.X / imageWidth * camRes.x);
                var py = Mathf.RoundToInt((1f - prediction.Y / imageHeight) * camRes.y);

                var ray = PassthroughCameraUtils.ScreenPointToRayInWorld(Eye, new Vector2Int(px, py));

                if (environmentRaycastManager.Raycast(ray, out var hit))
                {
                    detectionMarkerSpawner.SpawnAnchor(hit.point, prediction.Class);
                }
            }


            tooltip.text = "Detection complete. Press button to detect again.";
        }

        private void OnDetectionError(RoboflowInference.ErrorType error)
        {
            m_isLoading = false;
            tooltip.text = $"Error: {error}. Press button to retry.";
        }
    }
}
