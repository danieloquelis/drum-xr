using System;
using UnityEngine;

namespace Models
{
    [Serializable]
    public class DrumPadAnchor
    {
        public DrumPadType type;
        public Vector2Int screenPosition;
        public SerializableVector3 worldPosition;
    }
}