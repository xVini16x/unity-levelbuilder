using System;
using System.ComponentModel;

using UnityEngine;

namespace UnityLevelEditor.Model
{
   public enum Direction4Diagonal
   {
      UpRight,
      DownRight,
      DownLeft,
      UpLeft,
   }
   
    /// <summary>
    /// Collection of extensions for <see cref="Direction4Diagonal"/>.
    /// </summary>
    public static class Direction4DiagonalExtensions
    {
        private static readonly int DirectionEnumLength = Enum.GetValues(typeof(Direction4Diagonal)).Length;

        /// <summary>
        /// Shift the <see cref="Direction4Diagonal"/> clockwise.
        /// </summary>
        /// <param name="direction">The <see cref="Direction4Diagonal"/> to shift.</param>
        /// <param name="shiftBy">The number of times to shift.</param>
        /// <returns>A <see cref="Direction4Diagonal"/> shifted by <paramref name="shiftBy"/> amount of times, starting from <paramref name="direction"/>.</returns>
        public static Direction4Diagonal Shift(this Direction4Diagonal direction, int shiftBy)
        {
            var index = (int) direction;

            index += shiftBy;
            index %= DirectionEnumLength;

            if (index < 0)
            {
                index += DirectionEnumLength;
            }

            return (Direction4Diagonal) index;
        }

        /// <summary>
        /// Returns the opposite <see cref="Direction4Diagonal"/> of this <see cref="Direction4Diagonal"/>.
        /// </summary>
        /// <param name="direction">The <see cref="Direction4Diagonal"/> to invert.</param>
        /// <returns>The opposite <see cref="Direction4Diagonal"/> of <paramref name="direction"></paramref>.</returns>
        public static Direction4Diagonal Opposite(this Direction4Diagonal direction)
        {
            return direction.Shift((DirectionEnumLength - 1) / 2);
        }

        /// <summary>
        /// Returns a unit <see cref="Vector3"/> on a vertical plane, representing this <see cref="Direction4Diagonal"/>.
        /// </summary>
        /// <param name="direction">The input <see cref="Direction4Diagonal"/>.</param>
        /// <returns>A unit <see cref="Vector3"/> representing <paramref name="direction"/>.</returns>
        /// <exception cref="InvalidEnumArgumentException"><paramref name="direction"/> has an invalid value.</exception>
        public static Vector3 AsVector3XY(this Direction4Diagonal direction)
        {
            switch (direction)
            {
                case Direction4Diagonal.DownRight: return (Vector3.right + Vector3.down).normalized;
                case Direction4Diagonal.DownLeft: return (Vector3.left + Vector3.down).normalized;
                case Direction4Diagonal.UpLeft:  return (Vector3.left + Vector3.up).normalized;
                case Direction4Diagonal.UpRight: return (Vector3.right + Vector3.up).normalized; 
                default:
                    throw new InvalidEnumArgumentException(nameof(direction), (int)direction, typeof(Direction4Diagonal));
            }
        }
        
        /// <summary>
        /// Returns a unit <see cref="Vector3"/> on a horizontal plane, representing this <see cref="Direction4Diagonal"/>.
        /// </summary>
        /// <param name="direction">The input <see cref="Direction4Diagonal"/>.</param>
        /// <returns>A unit <see cref="Vector3"/> representing <paramref name="direction"/>.</returns>
        /// <exception cref="InvalidEnumArgumentException"><paramref name="direction"/> has an invalid value.</exception>
        public static Vector3 AsVector3XZ(this Direction4Diagonal direction)
        {
            switch (direction)
            {
                case Direction4Diagonal.DownRight:  return (Vector3.right + Vector3.back).normalized;
                case Direction4Diagonal.DownLeft: return (Vector3.left + Vector3.back).normalized;
                case Direction4Diagonal.UpLeft: return (Vector3.left + Vector3.forward).normalized;
                case Direction4Diagonal.UpRight: return (Vector3.right + Vector3.forward).normalized;
                default:
                    throw new InvalidEnumArgumentException(nameof(direction), (int)direction, typeof(Direction4Diagonal));
            }
        }

        /// <summary>
        /// Returns a unit <see cref="Vector2"/>, representing this <see cref="Direction4Diagonal"/>.
        /// </summary>
        /// <param name="direction">The input <see cref="Direction4Diagonal"/>.</param>
        /// <returns>A unit <see cref="Vector2"/> representing <paramref name="direction"/>.</returns>
        /// <exception cref="InvalidEnumArgumentException"><paramref name="direction"/> has an invalid value.</exception>
        public static Vector2 AsVector2(this Direction4Diagonal direction)
        {
            switch (direction)
            {
                case Direction4Diagonal.DownRight: return (Vector2.right + Vector2.down).normalized;
                case Direction4Diagonal.DownLeft: return (Vector2.left + Vector2.down).normalized;
                case Direction4Diagonal.UpLeft: return (Vector2.left + Vector2.up).normalized;
                case Direction4Diagonal.UpRight: return (Vector2.right + Vector2.up).normalized; 
                default:
                    throw new InvalidEnumArgumentException(nameof(direction), (int)direction, typeof(Direction4Diagonal));
            }
        }

        /// <summary>
        /// Returns a <see cref="Direction4Diagonal"/> best fitting to the <b>x</b> and <b>y</b> of this <see cref="Vector3"/>.
        /// </summary>
        /// <param name="vector3">The input <see cref="Vector3"/>.</param>
        /// <returns>The <see cref="Direction4Diagonal"/> best fitting to the <b>x</b> and <b>y</b> of <paramref name="vector3"/>.</returns>
        public static Direction4Diagonal AsDirection4DiagonalXY(this Vector3 vector3)
        {
            return GetDirection4Diagonal(vector3.x, vector3.y);
        }

        /// <summary>
        /// Returns a <see cref="Direction4Diagonal"/> best fitting to the <b>x</b> and <b>z</b> of this <see cref="Vector3"/>.
        /// </summary>
        /// <remarks> Useful for handling two-dimensional calculations on a horizontal plane. </remarks>
        /// <param name="vector3">The input <see cref="Vector3"/>.</param>
        /// <returns>The <see cref="Direction4Diagonal"/> best fitting to the <b>x</b> and <b>z</b> of <paramref name="vector3"/>.</returns>
        public static Direction4Diagonal AsDirection4DiagonalXZ(this Vector3 vector3)
        {
            return GetDirection4Diagonal(vector3.x, vector3.z);
        }

        /// <summary>
        /// Returns a <see cref="Direction4Diagonal"/> best fitting to this <see cref="Vector2"/>.
        /// </summary>
        /// <param name="vector2">The input <see cref="Vector2"/>.</param>
        /// <returns>The <see cref="Direction4Diagonal"/> best fitting to <paramref name="vector2"/>.</returns>
        public static Direction4Diagonal AsDirection4Diagonal(this Vector2 vector2)
        {
            return GetDirection4Diagonal(vector2.x, vector2.y);
        }

        /// <summary>
        /// Get a <see cref="Direction4Diagonal"/> best fitting to the specified horizontal and vertical components.
        /// </summary>
        /// <param name="horizontal">The horizontal component.</param>
        /// <param name="vertical">The vertical component.</param>
        /// <returns>The <see cref="Direction4Diagonal"/> best fitting to <paramref name="horizontal"/> and <paramref name="vertical"/>.</returns>
        public static Direction4Diagonal GetDirection4Diagonal(float horizontal, float vertical)
        {
            var angle = -Vector2.SignedAngle(Vector2.up, new Vector2(horizontal, vertical));
            
            int directionIndex = Mathf.FloorToInt(angle / (360f / 4));
            
            if (directionIndex < 0)
            {
                directionIndex += (DirectionEnumLength - 1);
            }

            return (Direction4Diagonal) directionIndex  + 1;
        }

        public static float GetInnerCornerAngle(this Direction4Diagonal direction4Diagonal)
        {
            switch (direction4Diagonal)
            {
                case Direction4Diagonal.UpRight:
                    return 0;
                case Direction4Diagonal.DownRight:
                    return 90;
                case Direction4Diagonal.DownLeft:
                    return 180;
                case Direction4Diagonal.UpLeft:
                    return 270;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction4Diagonal), direction4Diagonal, null);
            }
        }
        
        public static float GetOuterCornerAngle(this Direction4Diagonal direction4Diagonal)
        {
            switch (direction4Diagonal)
            {
                case Direction4Diagonal.UpRight:
                    return 90;
                case Direction4Diagonal.DownRight:
                    return 180;
                case Direction4Diagonal.DownLeft:
                    return 270;
                case Direction4Diagonal.UpLeft:
                    return 0;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction4Diagonal), direction4Diagonal, null);
            }
        }
    }
}