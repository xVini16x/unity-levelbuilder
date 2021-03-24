using UnityEngine;

namespace UnityLevelEditor.Model
{
    using RoomExtension;
    
    public class CornerElement : MonoBehaviour
    {
        [field: SerializeField, HideInInspector]
        public RoomElementType Type { get; set; }
        
        [field: SerializeField, HideInInspector]
        public ExtendableRoom ExtendableRoom { get; set; }

        [field: SerializeField, HideInInspector]
        public Direction4Diagonal Direction { get; set; }
        
        [field: SerializeField, HideInInspector]
        public Vector2Int FloorTilePosition { get; set; }
    }
}
