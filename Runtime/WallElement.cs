using UnityEngine;
using UnityLevelEditor.RoomExtension;

namespace UnityLevelEditor.Model
{
    public class WallElement : MonoBehaviour
    {
        [field: SerializeField, HideInInspector]
        public RoomElementType Type { get; set; }
        
        [field: SerializeField, HideInInspector]
        public ExtendableRoom ExtendableRoom { get; set; }

        [field: SerializeField, HideInInspector]
        public Direction Direction { get; set; }
        
        [field: SerializeField, HideInInspector]
        public Vector2Int FloorTilePosition { get; set; }
    }
}