using System.Collections.Generic;
using Meta.XR.ImmersiveDebugger;
using UnityEngine;

public class DetectionMarkerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject markerPrefab;
    [SerializeField] private float minSpawnDistance = 0.25f;
    
    public List<GameObject> spawnedMarkers = new();
    
    [DebugMember]
    public void SpawnAnchor(Vector3 position, string className)
    {
        // Avoid duplicates
        foreach (var marker in spawnedMarkers)
        {
            if (Vector3.Distance(marker.transform.position, position) < minSpawnDistance &&
                marker.GetComponent<MarkerAnchor>().Class == className)
            {
                return;
            }
        }

        var go = Instantiate(markerPrefab, position, Quaternion.identity);
        var anchor = go.GetComponent<MarkerAnchor>();
        
        if (!anchor)
        {
            Debug.LogError("MarkerAnchor component not found on marker prefab");
            return;
        }
        
        anchor.SetClass(className);
        anchor.SaveAnchor(); 

        spawnedMarkers.Add(go);
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
