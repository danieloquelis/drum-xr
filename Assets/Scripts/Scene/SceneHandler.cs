using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scene
{
    public class SceneHandler : MonoBehaviour
    {
        [SerializeField] private List<string> scenes;

        public void GoToScene(int index)
        {
            if (index < 0 || index >= scenes.Count)
            {
                Debug.LogWarning("Scene index out of range.");
                return;
            }

            var targetScene = scenes[index];
            var currentScene = SceneManager.GetActiveScene();

            if (currentScene.name == targetScene)
            {
                Debug.Log("Already in the target scene.");
                return;
            }
        
            SceneManager.LoadScene(targetScene);
        }
    }
}
