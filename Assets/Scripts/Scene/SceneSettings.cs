using System.Collections.Generic;
using Models;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

namespace Scene
{
    public class SceneSettings : MonoBehaviour
    {
        public UnityEvent onAnchorsDataEmpty;
        public UnityEvent onAnchorsDataLoaded;
        
        private void Awake()
        {
            var json = PlayerPrefs.GetString("DrumAnchors", "[]");
            var detections = JsonConvert.DeserializeObject<List<DrumPadAnchor>>(json);

            // if (detections.Count == 0)
            // {
            //     onAnchorsDataEmpty?.Invoke();
            // } else
            // {
            //     onAnchorsDataLoaded?.Invoke();
            // }
        }
    }
}
