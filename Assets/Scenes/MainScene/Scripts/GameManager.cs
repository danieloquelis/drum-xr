using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Detection;
using Models;
using Rythm;
using Scenes.MainScene.Scripts;
using UnityEngine;
using Utils;

public class GameManager : MonoBehaviour
{
    
    [SerializeField] private List<DrumPad> drumPads;

    [Header("Anchorable Objects")] 
    [SerializeField] private List<AnchorableObject> anchorableGameObjects = new();
    
    [Header("Rythm")]
    [SerializeField] private BeatmapManager beatmapManager;
    
    private readonly Dictionary<Guid, DrumPad> m_padAnchorsUuids = new();
    private readonly Dictionary<Guid, AnchorableObject> m_anchorsUuids = new();
    private readonly List<OVRSpatialAnchor.UnboundAnchor> m_unboundAnchors = new();
    private readonly List<DrumPad> m_spawnedPads = new();
    
    private void Awake()
    {
        LoadAnchorRefs();
    }

    private async void Start()
    {
        try
        {
            // Merge pad and non-pad anchor Uuids
            var anchorUuids = new HashSet<Guid>(m_padAnchorsUuids.Keys);
            anchorUuids.UnionWith(m_anchorsUuids.Keys);
            
            var didAnchorsLoad = await LoadAnchorsByUuid(anchorUuids);
            if (!didAnchorsLoad)
            {
                Debug.LogError("[Start] Failed to load anchors.");
                // TODO: Show an error
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[StartException] {e.Message}");
        }

        beatmapManager.onNoteHitEventReceived.AddListener(OnNoteHit);
    }

    private void OnNoteHit(DrumPadType noteType)
    {
        foreach (var drumPad in m_spawnedPads.Where(drumPad => drumPad.drumPadType == noteType))
        {
            drumPad.HighLightPad();
            break;
        }
    }

    private void OnPadTouched(DrumPadType drumPadType)
    {
        Debug.Log($"Touched {drumPadType}");
    }

    private void LoadAnchorRefs()
    {
        // DrumPad Anchors
        foreach (var drumPad in drumPads)
        {
            var anchorUuid = LoadAnchorUuidFromPrefs(drumPad.drumPadType.ToString());
            if (!anchorUuid.HasValue) continue;
            
            m_padAnchorsUuids.Add(anchorUuid.Value, drumPad);
        }
        
        // OtherAnchorable Items
        // Used to place other objects in the scene
        foreach (var anchorableObject in anchorableGameObjects)
        {
            var menuAnchorUuid = LoadAnchorUuidFromPrefs(anchorableObject.type.ToString());
            if (menuAnchorUuid.HasValue)
            {
                m_anchorsUuids.Add(menuAnchorUuid.Value, anchorableObject);
            }
        }
    }
    
    private static Guid? LoadAnchorUuidFromPrefs(string key)
    {
        var uuidString = PlayerPrefs.GetString(key, null);
        if (string.IsNullOrEmpty(uuidString))
        {
            return null;
        }
            
        Debug.Log($"[Pref] Anchor for key {key} is {uuidString}");
        return Guid.Parse(uuidString);
    }
    
    private async Task<bool> LoadAnchorsByUuid(IEnumerable<Guid> uuids)
    {
        var result = await OVRSpatialAnchor.LoadUnboundAnchorsAsync(uuids, m_unboundAnchors);
        
        if (!result.Success)
        {
            Debug.LogError($"Load failed with error {result.Status}.");
            return false;
        }
        
        Debug.Log("Anchors loaded successfully.");

        foreach (var unboundAnchor in result.Value)
        {
            unboundAnchor.LocalizeAsync().ContinueWith((success, anchor) =>
            {
                if (!success)
                {
                    Debug.LogError($"Localization failed for anchor {unboundAnchor.Uuid}");
                }

                OVRSpatialAnchor spatialAnchor;
                
                if (m_padAnchorsUuids.TryGetValue(anchor.Uuid, out var drumPadPrefab))
                {
                    var drumPad = Instantiate(drumPadPrefab, transform.position, Quaternion.identity);
                    drumPad.onPadTouched.AddListener(OnPadTouched);
                    drumPad.MirrorLabel();
                
                    spatialAnchor = drumPad.gameObject.AddComponent<OVRSpatialAnchor>();
                    m_spawnedPads.Add(drumPad);
                }
                else if(m_anchorsUuids.TryGetValue(anchor.Uuid, out var anchorableObject))
                {
                    var anchorableGameObject = anchorableObject.gameObject;
                    spatialAnchor = anchorableGameObject.AddComponent<OVRSpatialAnchor>();
                }
                else
                {
                    Debug.LogError($"Failed to find anchor for {anchor.Uuid}");
                    return;
                }
                
                // Because the anchor has already been localized, BindTo will set the
                // transform component immediately.
                unboundAnchor.BindTo(spatialAnchor);
            }, unboundAnchor);
        }

        return true;
    }
}
