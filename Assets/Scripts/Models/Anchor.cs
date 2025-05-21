using System;
using UnityEngine;

namespace Models
{
    [Serializable]
    public class Anchor
    {
        public string Class;
        public Vector2Int ScreenPosition;
        public Vector3 WorldPosition;
    }
}