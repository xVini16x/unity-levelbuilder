namespace UnityLevelEditor.Model
{
    public enum RoomElementTyp
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
        public static bool IsWallType(this RoomElementTyp type)
        {
            return type == RoomElementTyp.Wall || type == RoomElementTyp.WallTransparent || type == RoomElementTyp.WallShortenedLeft || type == RoomElementTyp.WallShortenedRight
                   || type == RoomElementTyp.WallShortenedBothEnds;
        }

        public static bool IsCornerType(this RoomElementTyp type)
        {
            return type == RoomElementTyp.Corner || type == RoomElementTyp.CornerTransparent;
        }

        public static bool IsShortenedWall(this RoomElementTyp type)
        {
            return type == RoomElementTyp.WallShortenedLeft || type == RoomElementTyp.WallShortenedRight;
        }
    }
}
