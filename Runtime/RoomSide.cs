namespace UnityLevelEditor.Model
{
    public enum RoomSide
    {
        North, 
        South, 
        EastAndWest
    }
    
    public static class RoomSideExtension {

        public static string GetName(this RoomSide roomSide)
        {
            switch (roomSide)
            {
                case RoomSide.North: return "North";
                case RoomSide.EastAndWest: return "EastAndWest";
                case RoomSide.South: return "South";
                default: return "Unknown";
            }
        }
    }
}