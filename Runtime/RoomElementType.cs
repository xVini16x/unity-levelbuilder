using System;

namespace UnityLevelEditor.Model
{
    public enum RoomElementType
    {
        Wall,
        WallShortenedLeft,
        WallShortenedRight,
        WallShortenedBothEnds,
        Floor,
        OuterCorner,
        InnerCorner
    }

    public static class RoomElementTypeExtensions
    {
        public static bool IsWallType(this RoomElementType type)
        {
            return type == RoomElementType.Wall || type == RoomElementType.WallShortenedLeft || type == RoomElementType.WallShortenedRight
                   || type == RoomElementType.WallShortenedBothEnds;
        }

        public static bool IsCornerType(this RoomElementType type)
        {
            return type == RoomElementType.OuterCorner || type == RoomElementType.InnerCorner;
        }

        public static bool IsShortenedWall(this RoomElementType type)
        {
            return type == RoomElementType.WallShortenedLeft || type == RoomElementType.WallShortenedRight;
        }
        
    }
}