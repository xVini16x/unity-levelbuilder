using System;
using System.ComponentModel;
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

    public static Direction Shift(this Direction direction, int shiftBy = 1)
    {
        int index = (int) direction;

        index += shiftBy;
        index %= DirectionEnumLength;

        if (index < 0)
        {
            index += DirectionEnumLength;
        }

        return (Direction) index;
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
                throw new ArgumentOutOfRangeException(nameof(direction), direction, $"Direction {direction} not supported.");
        }
    }

    public static int GetDifference(this Direction direction, Direction otherDirection)
    {
        var diff = ((int) direction) - ((int) otherDirection);

        if (diff < 0)
        {
            diff += DirectionEnumLength;
            diff = -diff;
        }

        return diff;
    }


    public static bool TowardsNegative(this Direction direction)
    {
        return direction == Direction.Left || direction == Direction.Back;
    }

    public static bool IsSideways(this Direction direction)
    {
        return direction == Direction.Left || direction == Direction.Right;
    }

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
                throw new ArgumentOutOfRangeException(nameof(direction), direction, $"Direction {direction} not supported.");
        }
    }
}