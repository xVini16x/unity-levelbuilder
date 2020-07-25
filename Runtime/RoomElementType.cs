namespace UnityLevelEditor.Model
{
    public enum RoomElementType
    {
        Wall = 0,
        WallTransparent = 1,
        WallShortenedLeft = 2,
        WallShortenedRight = 3,
        WallShortenedBothEnds = 4,
        Floor = 5,
        Corner = 6, 
        CornerTransparent = 7
    }

    public static class RoomElementTypeExtensions
    {
        public static bool IsWallType(this RoomElementType type)
        {
            return type == RoomElementType.Wall || type == RoomElementType.WallTransparent || type == RoomElementType.WallShortenedLeft || type == RoomElementType.WallShortenedRight
                   || type == RoomElementType.WallShortenedBothEnds;
        }

        public static bool IsCornerType(this RoomElementType type)
        {
            return type == RoomElementType.Corner || type == RoomElementType.CornerTransparent;
        }

        public static bool IsShortenedWall(this RoomElementType type)
        {
            return type == RoomElementType.WallShortenedLeft || type == RoomElementType.WallShortenedRight;
        }
    }
}
