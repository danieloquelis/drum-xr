using System;
using System.Threading.Tasks;
using Meta.XR.ImmersiveDebugger;
using Models;
using TMPro;
using UnityEngine;

namespace Detection
{
    public class DrumPad : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        public DrumPadType drumPadType;

        private Transform m_centerEye;

        private void Awake()
        {
            var rig = FindFirstObjectByType<OVRCameraRig>();
            m_centerEye = rig?.centerEyeAnchor;
        }

        private void Start()
        {
            label.text = drumPadType.ToString();
        }

        [DebugMember]
        public void SaveAnchor()
        {
            var anchor = GetComponent<OVRSpatialAnchor>();
            if (!anchor)
            {
                Debug.Log($"No OVRSpatialAnchor found on prefab for {drumPadType}");
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
                    Debug.Log($"Anchor saved for {drumPadType}");
                }
                else
                {
                    Debug.LogError($"Failed to save anchor for {drumPadType}");
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to save anchor for {drumPadType}: {exception.Message}");
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
}