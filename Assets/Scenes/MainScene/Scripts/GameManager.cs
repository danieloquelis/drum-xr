using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Detection;
using Models;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private List<DrumPad> drumPads;
    
    private readonly Dictionary<Guid, DrumPadType> m_padAnchorsUuids = new();
    private readonly List<OVRSpatialAnchor.UnboundAnchor> m_unboundAnchors = new();
    
    private void Awake()
    {
        LoadAnchorRefs();
    }

    private async void Start()
    {
        try
        {
            var didAnchorsLoad = await LoadAnchorsByUuid(m_padAnchorsUuids.Keys);
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
    }

    private void OnPadTouched(DrumPadType drumPadType)
    {
        Debug.Log($"Touched {drumPadType}");
    }

    private void LoadAnchorRefs()
    {
        foreach (var drumPad in drumPads)
        {
            var uuidString = PlayerPrefs.GetString(drumPad.drumPadType.ToString(), null);
            if (string.IsNullOrEmpty(uuidString))
            {
                continue;
            }
            
            Debug.Log($"[Pref] Anchor for {drumPad.drumPadType} is {uuidString}");
            m_padAnchorsUuids.Add(Guid.Parse(uuidString), drumPad.drumPadType);
        }
    }
    
    private DrumPad GetDrumPad(DrumPadType drumPadType)
    {
        return drumPadType switch
        {
            DrumPadType.Kick => drumPads[0],
            DrumPadType.Snare => drumPads[1],
            DrumPadType.HiHat => drumPads[2],
            DrumPadType.Tom => drumPads[3],
            DrumPadType.Cymbal => drumPads[4],
            DrumPadType.Unknown => drumPads[1]
        };
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
                
                var drumPadType = m_padAnchorsUuids[anchor.Uuid];
                var prefab = GetDrumPad(drumPadType);
                
                var drumPad = Instantiate(prefab, transform.position, Quaternion.identity);
                drumPad.onPadTouched.AddListener(OnPadTouched);
                drumPad.MirrorLabel();
                
                var spatialAnchor = drumPad.gameObject.AddComponent<OVRSpatialAnchor>();
                
                // Because the anchor has already been localized, BindTo will set the
                // transform component immediately.
                unboundAnchor.BindTo(spatialAnchor);
            }, unboundAnchor);
        }

        return true;
    }
}
