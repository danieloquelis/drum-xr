using System;
using System.Collections;
using System.Threading.Tasks;
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

        private void Start()
        {
            label.text = drumPadType.ToString();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.collider.CompareTag("DrumStick"))
            {
                return;
            }
            
            Debug.Log($"[DrumPad OnTriggerEnter] {drumPadType}");
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

        public void MirrorLabel()
        {
            var currentScale = label.gameObject.transform.localScale;
            label.gameObject.transform.localScale = new Vector3(currentScale.x * -1, currentScale.y, currentScale.z);
        }

    }
}