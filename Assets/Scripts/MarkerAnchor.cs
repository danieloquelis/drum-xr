using System;
using System.Threading.Tasks;
using Meta.XR.ImmersiveDebugger;
using UnityEngine;

public class MarkerAnchor : MonoBehaviour
{
    [SerializeField] private TextMesh label;
    public string Class { get; private set; }

    private Transform m_centerEye;

    private void Awake()
    {
        var rig = FindFirstObjectByType<OVRCameraRig>();
        m_centerEye = rig?.centerEyeAnchor;
    }

    public void SetClass(string className)
    {
        Class = className;
        label.text = className;
    }

    [DebugMember]
    public void SaveAnchor()
    {
        var anchor = GetComponent<OVRSpatialAnchor>();
        if (!anchor)
        {
            Debug.LogWarning($"No OVRSpatialAnchor found on prefab for class {Class}");
            return;
        }

        _ = SaveAsync(anchor);
    }
    
    [DebugMember]
    private async Task SaveAsync(OVRSpatialAnchor anchor)
    {
        try
        {
            var result = await anchor.SaveAnchorAsync();
            if (!result.Success)
            {
                Debug.Log($"Anchor saved for class {Class}");
            }
            else
            {
                Debug.LogError($"Failed to save anchor for class {Class}");
            }
        }
        catch (Exception exception)
        {
            Debug.LogError($"Failed to save anchor for class {Class}: {exception.Message}");
        }
    }

    private void Update()
    {
        if (m_centerEye)
        {
            label.transform.LookAt(m_centerEye);
        }
    }
}