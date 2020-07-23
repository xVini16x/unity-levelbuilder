using UnityEngine;
using UnityLevelEditor.Model;

public class FloorElement : RoomElement
{
    //toDO: add hideInspector
    [field: SerializeField]
    public Vector2Int GridPosition { get; set; }

}
