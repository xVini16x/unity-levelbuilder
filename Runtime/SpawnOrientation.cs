
using System;
using UnityEngine;

namespace UnityLevelEditor.Model
{
    public enum SpawnOrientation
    {
        Front,
        Right,
        Back,
        Left
    }

    public static class SpawnOrientationExtensions
    {
        private static readonly int SpawnOrientationEnumLength = Enum.GetValues(typeof(SpawnOrientation)).Length;
        
        public static float ToAngle(this SpawnOrientation spawnOrientation)
        {
            switch (spawnOrientation)
            {
                case SpawnOrientation.Front: return 0f;
                case SpawnOrientation.Right: return 90f;
                case SpawnOrientation.Back: return 180f;
                case SpawnOrientation.Left: return 270f;
                default:
                    Debug.LogError($"Not supported SpawnOrientation {spawnOrientation}.");
                    return 0f;
            }
        }

        public static Direction ToDirection(this SpawnOrientation spawnOrientation)
        {
            switch (spawnOrientation)
            {
                case SpawnOrientation.Front:
                    return Direction.Front;
                case SpawnOrientation.Right:
                    return Direction.Right;
                case SpawnOrientation.Back:
                    return Direction.Back;
                case SpawnOrientation.Left:
                    return Direction.Left;
                default:
                    throw new ArgumentOutOfRangeException(nameof(spawnOrientation), spawnOrientation, $"Not supported SpawnOrientation {spawnOrientation}.");
            }
        }

        public static SpawnOrientation Shift(this SpawnOrientation spawnOrientation, int shiftBy)
        {
            int index = (int) spawnOrientation;

            index += shiftBy;

            index %= SpawnOrientationEnumLength;
            
            if (index < 0)
            {
                index += SpawnOrientationEnumLength;
            }

            return (SpawnOrientation) index;
        }

        public static bool IsSideways(this SpawnOrientation spawnOrientation)
        {
            return spawnOrientation == SpawnOrientation.Left || spawnOrientation == SpawnOrientation.Right;
        }

        public static bool TowardsNegative(this SpawnOrientation spawnOrientation)
        {
            return spawnOrientation == SpawnOrientation.Left || spawnOrientation == SpawnOrientation.Back;
        }
    }
   
}