
using UnityEngine;

namespace UnityLevelEditor.Model
{
    public enum SpawnOrientation
    {
        Front,
        Right,
        Left,
        Back
    }

    public static class SpawnOrientationExtensions
    {
        public static float ToAngle(this SpawnOrientation orientation)
        {
            switch (orientation)
            {
                case SpawnOrientation.Front: return 0f;
                case SpawnOrientation.Right: return 90f;
                case SpawnOrientation.Back: return 180f;
                case SpawnOrientation.Left: return 270f;
                default:
                    Debug.LogError($"Not supported orientation {orientation}.");
                    return 0f;
            }
        }

        public static bool IsSideways(this SpawnOrientation orientation)
        {
            return orientation == SpawnOrientation.Left || orientation == SpawnOrientation.Right;
        }
    }
   
}