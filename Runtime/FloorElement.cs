using UnityEditor;

using UnityEngine;
using UnityLevelEditor.Model;
using UnityLevelEditor.RoomExtension;

public class FloorElement : MonoBehaviour
{
    [field: SerializeField, HideInInspector]
    public ExtendableRoom ExtendableRoom { get; set; }
    
    [field: SerializeField, HideInInspector]
    public Vector2Int GridPosition { get; set; }

    [field: SerializeField, HideInInspector]
    public WallsPerDirection WallsPerDirection { get; } = new WallsPerDirection();
    
    [field: SerializeField, HideInInspector]
    public CornerPerDirection CornerPerDirection { get; } = new CornerPerDirection();

    #if UNITY_EDITOR
    public void DeleteAllNeighbors()
    {
        foreach (var direction in WallsPerDirection.Keys)
        {
            Undo.DestroyObjectImmediate(WallsPerDirection[direction].gameObject);
        }

        WallsPerDirection.Clear();
        
        foreach (var diagonalDirection in CornerPerDirection.Keys)
        {
            Undo.DestroyObjectImmediate(CornerPerDirection[diagonalDirection].gameObject);
        }
        
        CornerPerDirection.Clear();
    }
    #endif
}