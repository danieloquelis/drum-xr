using System;
using System.Collections;
using System.Threading.Tasks;
using Meta.XR.ImmersiveDebugger;
using Models;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Detection
{
    public class DrumPad : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        public DrumPadType drumPadType;

        public UnityEvent<DrumPadType> onPadTouched;
        
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

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.layer != LayerMask.NameToLayer("DrumStick"))
            {
                return;
            }
            
            onPadTouched?.Invoke(drumPadType);
        }
        
        public void SaveAnchor()
        {
            var anchor = GetComponent<OVRSpatialAnchor>();
            if (anchor) return;
            
            Debug.LogWarning($"No OVRSpatialAnchor found on prefab for {drumPadType}. Adding one...");
            StartCoroutine(CreateSpatialAnchor());
        }
        
        private IEnumerator CreateSpatialAnchor()
        {
            var anchor = gameObject.AddComponent<OVRSpatialAnchor>();

            // Wait for the async creation
            yield return new WaitUntil(() => anchor.Created);
            _ = SaveAsync(anchor);
        }
    
        private async Task SaveAsync(OVRSpatialAnchor anchor)
        {
            try
            {
                var result = await anchor.SaveAnchorAsync();
                if (!result)
                {
                    Debug.LogError($"Failed to save anchor for {drumPadType}");
                    return;
                }
                
                PlayerPrefs.SetString($"{drumPadType}", anchor.Uuid.ToString());
                PlayerPrefs.Save();
                Debug.Log($"Anchor saved for {drumPadType}, UUID: {anchor.Uuid}");
            }
            catch (Exception exception)
            {
                Debug.LogError($"Exception saving anchor for {drumPadType}: {exception.Message}");
            }
        }
    }
}