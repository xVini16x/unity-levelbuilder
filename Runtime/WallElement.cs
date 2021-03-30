using System;

using UnityEngine;

namespace UnityLevelEditor.Model
{
    [Serializable]
    public class WallElement : RoomElement
    {
        [field: SerializeField, HideInInspector]
        public RoomElementType Type { get; set; }

        [field: SerializeField, HideInInspector]
        public Direction Direction { get; set; }
        
        [field: SerializeField, HideInInspector]
        public Vector2Int FloorTilePosition { get; set; }
        
    }
}