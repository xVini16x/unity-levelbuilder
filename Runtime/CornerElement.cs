using UnityEngine;

namespace UnityLevelEditor.Model
{
    public class CornerElement : RoomElement
    {
        [field: SerializeField, HideInInspector]
        public RoomElementType Type { get; set; }

        [field: SerializeField, HideInInspector]
        public Direction4Diagonal Direction { get; set; }
        
        [field: SerializeField, HideInInspector]
        public Vector2Int FloorTilePosition { get; set; }
    }
}
