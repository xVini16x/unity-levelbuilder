using UnityEngine;

namespace UnityLevelEditor.Model
{
    public class FloorElement : RoomElement
    {
        [field: SerializeField, HideInInspector]
        public Vector2Int GridPosition { get; set; }
    }
}