using System;

using UnityEngine;
using UnityLevelEditor.RoomExtension;

namespace UnityLevelEditor.Model
{
    [Serializable]
    public class WallElement : MonoBehaviour
    {
        [field: SerializeField]
        public RoomElementType Type { get; set; }
        
        [field: SerializeField]
        public ExtendableRoom ExtendableRoom { get; set; }

        [field: SerializeField]
        public Direction Direction { get; set; }
        
        [field: SerializeField]
        public Vector2Int FloorTilePosition { get; set; }
    }
}