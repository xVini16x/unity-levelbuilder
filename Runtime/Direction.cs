using System;
using UnityLevelEditor.Model;

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
                throw new ArgumentOutOfRangeException(nameof(direction), direction, $"Converting '{direction}' to SpawnOrientation is not supported.");
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
                throw new ArgumentOutOfRangeException(nameof(direction), direction, $"Getting opposite for Direction '{direction}' is not supported.");
        }
    }
}