using System.Collections.Generic;
using Models;
using UnityEngine;

namespace Detection
{
    public class DrumPadSpawner : MonoBehaviour
    {
        [SerializeField] private List<GameObject> drumPads;
        [SerializeField] private float minSpawnDistance = 0.25f;
    
        public List<GameObject> spawnedMarkers = new();
        
        public void SpawnAnchor(Vector3 position, DrumPadType drumPadType)
        {
            foreach (var marker in spawnedMarkers)
            {
                if (Vector3.Distance(marker.transform.position, position) < minSpawnDistance &&
                    marker.GetComponent<DrumPad>().drumPadType == drumPadType)
                {
                    return;
                }
            }
            
            var drumPadPrefab = GetDrumPadPrefab(drumPadType);
            
            // Apply a custom rotation if the type is Tom
            var rotation = drumPadType == DrumPadType.Tom
                ? Quaternion.Euler(-10f, 0f, 0f)
                : Quaternion.identity;
            
            var go = Instantiate(drumPadPrefab, position, rotation);
            var drumPad = go.GetComponent<DrumPad>();
        
            if (!drumPad)
            {
                Debug.LogError("DrumPad component not found on prefab");
                return;
            }
        
            drumPad.SaveAnchor(); 
            spawnedMarkers.Add(go);
        }

        private GameObject GetDrumPadPrefab(DrumPadType drumPadType)
        {
            return drumPadType switch
            {
                DrumPadType.Kick => drumPads[0],
                DrumPadType.Snare => drumPads[1],
                DrumPadType.HiHat => drumPads[2],
                DrumPadType.Tom => drumPads[3],
                DrumPadType.Cymbal => drumPads[4],
                DrumPadType.Unknown => drumPads[1],
            };
        }
    
        public void ClearAll()
        {
            foreach (var d in spawnedMarkers)
            {
                Destroy(d);
            }
            spawnedMarkers.Clear();
        }
    }
}
