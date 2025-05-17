using Meta.XR.ImmersiveDebugger;
using Models;
using Newtonsoft.Json;
using Roboflow;
using UnityEngine;
using UnityEngine.Assertions;

public class DetectionManager : MonoBehaviour
{
    [Header("Roboflow")]
    [SerializeField] private RoboflowInference roboflowInference;
    
    [Header("Controls configuration")]
    [SerializeField] private OVRInput.RawButton actionButton = OVRInput.RawButton.A;

    [Header("UI")] 
    [SerializeField] private TextMesh tooltip;
    
    private bool m_isLoading;
    
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

        foreach (var prediction in predictions)
        {
            Debug.Log($"Detected {prediction.Class} at ({prediction.X}, {prediction.Y})");
        }

        tooltip.text = "Detection complete. Press button to detect again.";
    }

    private void OnDetectionError(RoboflowInference.ErrorType error)
    {
        m_isLoading = false;
        tooltip.text = $"Error: {error}. Press button to retry.";
    }
}
