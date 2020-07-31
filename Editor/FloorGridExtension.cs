namespace UnityLevelEditor.Model
{
    public static class FloorGridExtension
    {
        //As an Extension method to have it in the Editor folder because of it's Editor dependencies
        public static (FloorElement clockwiseDiagonalFloor, FloorElement counterClockwiseDiagonalFloor) GetDiagonalCollision(this FloorGridDictionary floorGridDictionary, FloorElement floorElement, Direction wallDirection)
        {
            var gridPos = RoomExtension.RoomExtension.GetGridPosition(floorElement.GridPosition, wallDirection);

            var clockwiseDirection = RoomExtension.RoomExtension.GetGridPosition(gridPos, wallDirection.Shift(1));
            var counterClockwiseDirection = RoomExtension.RoomExtension.GetGridPosition(gridPos, wallDirection.Shift(-1));

            FloorElement clockwise = null;
            FloorElement counterClockwise = null;

            if (floorGridDictionary.ContainsKey(clockwiseDirection))
            {
                clockwise = floorGridDictionary[clockwiseDirection];
            }

            if (floorGridDictionary.ContainsKey(counterClockwiseDirection))
            {
                counterClockwise = floorGridDictionary[counterClockwiseDirection];
            }

            return (clockwise, counterClockwise);
        }
    }
}