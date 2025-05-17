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

    private void OnDetectionSuccess(string result)
    {
        m_isLoading = false;
        tooltip.text = "Detection complete. Press button to detect again.";
    }

    private void OnDetectionError(RoboflowInference.ErrorType error)
    {
        m_isLoading = false;
        tooltip.text = $"Error: {error}. Press button to retry.";
    }
}
