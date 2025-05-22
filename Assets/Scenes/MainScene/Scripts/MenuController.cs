using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Scenes.MainScene.Scripts
{
    public class MenuController: MonoBehaviour
    {
        [SerializeField] private List<AnchorableObject> anchorableGameObjects;
        
        private Guid m_mainMenuAnchorUuid;

        public void SaveAnchorForObject(int index)
        {
            StartCoroutine(CreateSpatialAnchor(anchorableGameObjects[index]));
        }
        
        private IEnumerator CreateSpatialAnchor(AnchorableObject anchorableGameObject)
        {
            var anchor = anchorableGameObject.gameObject.AddComponent<OVRSpatialAnchor>();

            // Wait for the async creation
            yield return new WaitUntil(() => anchor.Created);
            _ = SaveAsync(anchor, anchorableGameObject.type);
        }
    
        private async Task SaveAsync(OVRSpatialAnchor anchor, AnchorableObject.Type type)
        {
            try
            {
                var result = await anchor.SaveAnchorAsync();
                if (!result)
                {
                    Debug.LogError($"Failed to save anchor for {type}");
                    return;
                }
                
                PlayerPrefs.SetString($"{type}", anchor.Uuid.ToString());
                PlayerPrefs.Save();
                Debug.Log($"Anchor saved for {type}, UUID: {anchor.Uuid}");
            }
            catch (Exception exception)
            {
                Debug.LogError($"Exception saving anchor for {type}: {exception.Message}");
            }
        }
    }
}