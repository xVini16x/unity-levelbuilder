using System;
using System.ComponentModel;
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

    [SerializeField] private WallElement northWall;
    [SerializeField] private WallElement southWall;
    [SerializeField] private WallElement eastWall;
    [SerializeField] private WallElement westWall;

    [SerializeField] private CornerElement northEastCorner;
    [SerializeField] private CornerElement southEastCorner;
    [SerializeField] private CornerElement southWestCorner;
    [SerializeField] private CornerElement northWestCorner;

#if UNITY_EDITOR
    public void DeleteAllNeighbors()
    {
        foreach (Direction direction in Enum.GetValues(typeof(Direction)))
        {
            ref var wall = ref GetWall(direction);
            
            if (wall != null)
            {
                Undo.DestroyObjectImmediate(wall.gameObject);
                wall = null;
            }
        }
        
        foreach (Direction4Diagonal direction in Enum.GetValues(typeof(Direction4Diagonal)))
        {
            ref var corner = ref GetCorner(direction);

            if (corner != null)
            {
                Undo.DestroyObjectImmediate(corner.gameObject);
                corner = null;
            }
        }
    }
    
    private ref WallElement GetWall(Direction direction)
    {
        switch (direction)
        {
            case Direction.Front:
                return ref northWall;
            case Direction.Right:
                return ref eastWall;
            case Direction.Back:
                return ref southWall;
            case Direction.Left:
                return ref westWall;
            default:
                throw new InvalidEnumArgumentException();
        }
    }
    
    public bool TryGetWall(Direction direction, out WallElement wallElement)
    {
        wallElement = GetWall(direction);
        return wallElement != null;
    }

    internal void SetWall(Direction direction, WallElement toSet)
    {
        ref var wall = ref GetWall(direction);
        wall = toSet;
    }

    private ref CornerElement GetCorner(Direction4Diagonal direction4Diagonal)
    {
        switch (direction4Diagonal)
        {
            case Direction4Diagonal.UpRight:
                return ref northEastCorner;
            case Direction4Diagonal.DownRight:
                return ref southEastCorner;
            case Direction4Diagonal.DownLeft:
                return ref southWestCorner;
            case Direction4Diagonal.UpLeft:
                return ref northWestCorner;
            default:
                throw new InvalidEnumArgumentException();
        }
    }

    internal void SetCorner(Direction4Diagonal direction4Diagonal, CornerElement toSet)
    {
        ref var corner = ref GetCorner(direction4Diagonal);
        corner = toSet;
    }
   

#endif
}
