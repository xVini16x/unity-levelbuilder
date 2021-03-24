using System;
using System.ComponentModel;

using UnityEngine;

using UnityLevelEditor.Model;

namespace UnityLevelEditor.Model
{
    public enum Direction
    {
        Front,
        Right,
        Back,
        Left
    }

    public static class DirectionExtensions
    {
        private static readonly int DirectionEnumLength = Enum.GetValues(typeof(Direction)).Length;

        public static SpawnOrientation ToSpawnOrientation(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Front:
                    return SpawnOrientation.Front;
                case Direction.Right:
                    return SpawnOrientation.Right;
                case Direction.Back:
                    return SpawnOrientation.Back;
                case Direction.Left:
                    return SpawnOrientation.Left;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction,
                        $"Converting '{direction}' to SpawnOrientation is not supported.");
            }
        }
        

        public static Direction Shift(this Direction direction, int shiftBy)
        {
            var index = (int) direction;

            index += shiftBy;
            index %= DirectionEnumLength;

            if (index < 0)
            {
                index += DirectionEnumLength;
            }

            return (Direction) index;
        }

        public static Direction Shift(this Direction direction, bool clockwise)
        {
            return clockwise ? direction.Shift(1) : direction.Shift(-1);
        }

        public static bool IsSideways(this Direction direction)
        {
            return direction == Direction.Left || direction == Direction.Right;
        }

        public static bool TowardsNegative(this Direction direction)
        {
            return direction == Direction.Left || direction == Direction.Back;
        }

        public static Direction Opposite(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Back:
                    return Direction.Front;
                case Direction.Front:
                    return Direction.Back;
                case Direction.Left:
                    return Direction.Right;
                case Direction.Right:
                    return Direction.Left;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction,
                        $"Getting opposite for Direction '{direction}' is not supported.");
            }
        }

        public static RoomSide ToRoomSide(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Front:
                    return RoomSide.North;
                case Direction.Back:
                    return RoomSide.South;
                case Direction.Right:
                case Direction.Left:
                    return RoomSide.EastAndWest;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction,
                                                          $"Converting '{direction}' to RoomSide is not supported.");
            }
        }

        public static Direction4Diagonal ClockwiseDiagonalDirection(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Front:
                    return Direction4Diagonal.UpRight;
                case Direction.Back:
                    return Direction4Diagonal.DownLeft;
                case Direction.Right:
                    return Direction4Diagonal.DownRight;
                case Direction.Left:
                    return Direction4Diagonal.UpLeft;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction,
                                                          $"Converting '{direction}' to ClockwiseDiagonalDirection is not supported.");
            }
        }
        
        public static Vector2Int AsVector2Int(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Right: return Vector2Int.right;
                case Direction.Back:  return Vector2Int.down;
                case Direction.Left:  return Vector2Int.left;
                case Direction.Front: return Vector2Int.up;
                default:
                    throw new InvalidEnumArgumentException(nameof(direction), (int)direction, typeof(Direction));
            }
        }
    }
}