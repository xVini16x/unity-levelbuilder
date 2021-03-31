using UnityEngine;

namespace UnityLevelEditor.Model
{
    public class CornerElement : RoomElement
    {
        [field: SerializeField, HideInInspector]
        public Direction4Diagonal Direction { get; set; }
        
        [field: SerializeField, HideInInspector]
        public Vector2Int FloorTilePosition { get; set; }

        public void CopyOverValues(CornerElement cornerElement)
        {
            ExtendableRoom = cornerElement.ExtendableRoom;
            Type = cornerElement.Type;
            Direction = cornerElement.Direction;
            FloorTilePosition = cornerElement.FloorTilePosition;
        }
    }
}
