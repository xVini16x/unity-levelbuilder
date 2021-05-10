using System;
using System.ComponentModel;
using System.Configuration;

using UnityEditor;

using UnityEngine;

namespace UnityLevelEditor.Model
{
    public class FloorElement : RoomElement
    {
        [field: SerializeField, HideInInspector]
        public Vector2Int GridPosition { get; set; }

        [SerializeField, HideInInspector] private WallElement northWall;
        [SerializeField, HideInInspector] private WallElement southWall;
        [SerializeField, HideInInspector] private WallElement eastWall;
        [SerializeField, HideInInspector] private WallElement westWall;

        [SerializeField, HideInInspector] private CornerElement northEastCorner;
        [SerializeField, HideInInspector] private CornerElement southEastCorner;
        [SerializeField, HideInInspector] private CornerElement southWestCorner;
        [SerializeField, HideInInspector] private CornerElement northWestCorner;

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

        public void SetWall(Direction direction, WallElement toSet)
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

        public void SetCorner(Direction4Diagonal direction4Diagonal, CornerElement toSet)
        {
            ref var corner = ref GetCorner(direction4Diagonal);
            corner = toSet;
        }
        
#endif
        public void CopyOverValues(FloorElement floorElement)
        {
            ExtendableRoom = floorElement.ExtendableRoom;
            GridPosition = floorElement.GridPosition;
            northWall = floorElement.northWall;
            southWall = floorElement.southWall;
            eastWall = floorElement.eastWall;
            westWall = floorElement.westWall;
            northEastCorner = floorElement.northEastCorner;
            southEastCorner = floorElement.southEastCorner;
            southWestCorner = floorElement.southWestCorner;
            northWestCorner = floorElement.northWestCorner;
        }
    }
}
