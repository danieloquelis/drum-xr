using UnityEngine;

namespace Scenes.MainScene.Scripts
{
    public class AnchorableObject: MonoBehaviour
    {
        public enum Type
        {
            Menu,
            Rythm
        }
        
        public Type type;
    }
}