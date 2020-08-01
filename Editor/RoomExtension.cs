using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityLevelEditor.Model;

namespace UnityLevelEditor.RoomExtension
{
    public static class RoomExtension
    {
        #region Extension Based On Wall Type (Entry Point)

        public static void ExtendTheRoom(IEnumerable<RoomElement> selectedWalls, Vector3 movementDelta)
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Room Extension");
            var undoGroupId = Undo.GetCurrentGroup();

            // Needed because some walls may be replaced by shorter walls or larger walls, but can still be referenced by their floor elements
            var floorsAndWallDirections = new List<(FloorElement floor, Direction wallDirection)>();

            foreach (var wall in selectedWalls)
            {
                var wallDirection = wall.SpawnOrientation.ToDirection();
                var floor = (FloorElement) wall.GetRoomElementByDirection(wallDirection.Opposite());
                floorsAndWallDirections.Add((floor, wallDirection));
            }

            foreach (var (floor, wallDirection) in floorsAndWallDirections)
            {
                var selectedWall = floor.GetRoomElementByDirection(wallDirection);
                ExtendWall(selectedWall, movementDelta);
                Undo.CollapseUndoOperations(undoGroupId);
            }

            Undo.CollapseUndoOperations(undoGroupId);
        }

        private static void ExtendWall(RoomElement selectedWall, Vector3 movementDelta)
        {
            var wallConditions = GetWallConditions(selectedWall);

            if (wallConditions.Wall.Type == RoomElementType.WallShortenedBothEnds)
            {
                ExtendShortestWall(wallConditions, movementDelta);
                return;
            }

            Undo.RecordObject(selectedWall.transform, "");
            Undo.RecordObject(selectedWall, "");
            selectedWall.transform.position += movementDelta;

            //Prevent spawning double walls, shorter wall is only spawned when there is already an additional way
            if (selectedWall.Type.IsShortenedWall())
            {
                ExtendShortenedWall(wallConditions, movementDelta);
            }
            else
            {
                ExtendFullWall(wallConditions, movementDelta);
            }
        }

        private static void ExtendFullWall(WallConditions wallConditions, Vector3 movementDelta)
        {
            var (newFloor, collidingFloor) = SpawnFloorNextToSelectedWall(wallConditions.Wall);

            if (collidingFloor != null)
            {
                HandleDirectCollision(wallConditions, collidingFloor, newFloor);
                return;
            }

            var (clockwiseDiagonalFloor, counterClockwiseDiagonalFloor) =
                newFloor.ExtendableRoom.FloorGridDictionary.GetDiagonalCollision((FloorElement) newFloor,
                    wallConditions.Wall.SpawnOrientation.ToDirection());

            if (counterClockwiseDiagonalFloor != null)
            {
                HandleDiagonalCollision(wallConditions, newFloor, counterClockwiseDiagonalFloor, false);
            }
            else if ((wallConditions.CornerSituation & CornerSituation.CounterClockwise) ==
                     CornerSituation.CounterClockwise)
            {
                HandleCornerNextToSelectedWall(wallConditions, newFloor, movementDelta, false);
            }
            else
            {
                HandleWallNextToSelectedWall(wallConditions, newFloor, false);
            }

            if (clockwiseDiagonalFloor != null)
            {
                HandleDiagonalCollision(wallConditions, newFloor, clockwiseDiagonalFloor, true);
            }
            else if ((wallConditions.CornerSituation & CornerSituation.Clockwise) == CornerSituation.Clockwise)
            {
                HandleCornerNextToSelectedWall(wallConditions, newFloor, movementDelta, true);
            }
            else
            {
                HandleWallNextToSelectedWall(wallConditions, newFloor, true);
            }

            if (wallConditions.Wall == null)
            {
                return;
            }

            ReplaceWallSideMaterialsIfNecessary(wallConditions.Wall, newFloor, wallConditions.Wall.ElementFront);
        }

        private static void ExtendShortenedWall(WallConditions wallConditions, Vector3 movementDelta)
        {
            var selectedWall = wallConditions.Wall;

            var wallNeedsToBePlaced = ShortenedWallNeedsToBeReplacedThroughFullWall(wallConditions);

            var (newFloor, collidingFloor) = SpawnFloorNextToSelectedWall(selectedWall);

            if (collidingFloor)
            {
                HandleDirectCollision(wallConditions, collidingFloor, newFloor);
            }
            else
            {
                if (wallNeedsToBePlaced)
                {
                    selectedWall = EnlargeShortenedWall(selectedWall);
                    wallConditions.Wall = selectedWall;
                }

                var (clockwiseDiagonalFloor, counterClockwiseDiagonalFloor) =
                    newFloor.ExtendableRoom.FloorGridDictionary.GetDiagonalCollision((FloorElement) newFloor,
                        selectedWall.SpawnOrientation.ToDirection());

                if (counterClockwiseDiagonalFloor != null &&
                    !IsInnerCorner(wallConditions.GetElement(0, false), selectedWall))
                {
                    HandleDiagonalCollision(wallConditions, newFloor, counterClockwiseDiagonalFloor, false);
                }
                else if ((wallConditions.CornerSituation & CornerSituation.CounterClockwise) ==
                         CornerSituation.CounterClockwise)
                {
                    HandleCornerNextToShortenedWall(wallConditions, newFloor, movementDelta, false);
                }
                else
                {
                    HandleWallNextToSelectedWall(wallConditions, newFloor, false);
                }

                if (clockwiseDiagonalFloor != null &&
                    !IsInnerCorner(wallConditions.GetElement(0, true), selectedWall))
                {
                    HandleDiagonalCollision(wallConditions, newFloor, clockwiseDiagonalFloor, true);
                }
                else if ((wallConditions.CornerSituation & CornerSituation.Clockwise) ==
                         CornerSituation.Clockwise)
                {
                    HandleCornerNextToShortenedWall(wallConditions, newFloor, movementDelta, true);
                }
                else
                {
                    HandleWallNextToSelectedWall(wallConditions, newFloor, true);
                }
            }

            if (wallConditions.Wall == null)
            {
                return;
            }

            ReplaceWallSideMaterialsIfNecessary(wallConditions.Wall, newFloor, wallConditions.Wall.ElementFront);
        }

        private static void ExtendShortestWall(WallConditions wallConditions, Vector3 movementDelta)
        {
            var wallAroundCorner = wallConditions.GetElement(1, true);

            if (wallAroundCorner.Type != RoomElementType.WallShortenedBothEnds)
            {
                //two wallShortenedBothEnds means this section has a square form in all cases
                ExtendWallAroundCornerInsteadOfShortestWall(wallConditions, movementDelta);
                return;
            }

            Undo.RecordObject(wallConditions.Wall, "");
            DeleteSquareOfShortestWall(wallConditions);
        }

        private static void ExtendWallAroundCornerInsteadOfShortestWall(WallConditions wallConditions,
            Vector3 movementDelta)
        {
            // Get Grid Position for floor that will be spawned so we are able to get an reference to the new wall later on and add it to selection
            var selectedWall = wallConditions.Wall;
            var wallDirection = selectedWall.SpawnOrientation.ToDirection();
            var oldFloor = selectedWall.GetRoomElementByDirection(wallDirection.Opposite());
            var newFloorGridPos = GetGridPosition(oldFloor, wallDirection);
            var extendableRoom = selectedWall.ExtendableRoom;

            var wallAroundCorner = wallConditions.GetElement(1, true);
            var newWallConditions = GetWallConditions(wallAroundCorner);
            var rotationAmount = -90;
            var rotation = selectedWall.transform.rotation * Quaternion.Euler(0, rotationAmount, 0);
            movementDelta = rotation * movementDelta;

            Undo.RecordObject(wallAroundCorner.transform, "");
            Undo.RecordObject(wallAroundCorner, "");

            wallAroundCorner.transform.position += movementDelta;
            ExtendShortenedWall(newWallConditions, movementDelta);

            //Could have changed during extension
            var newFloor = extendableRoom.FloorGridDictionary[newFloorGridPos];
            var newSelectedWall = newFloor.GetRoomElementByDirection(wallDirection);
            AddToSelection(newSelectedWall, false);
        }

        #endregion

        #region Replace

        private static RoomElement EnlargeShortenedWall(RoomElement wallToEnlarge)
        {
            if (!wallToEnlarge.Type.IsWallType())
            {
                throw new NotSupportedException($"Can only shrink walls! Found type {wallToEnlarge.Type}");
            }

            if (wallToEnlarge.Type == RoomElementType.Wall)
            {
                throw new NotSupportedException($"Can not enlarge wall of type {wallToEnlarge.Type} anymore!");
            }

            var spawnerList = wallToEnlarge.ExtendableRoom.ElementSpawner;
            var newWallSpawner = spawnerList[(int) RoomElementType.Wall];
            var newPosition = GetPositionForWallReplacement(wallToEnlarge, RoomElementType.Wall, false);
            var (newWall, _) = newWallSpawner.SpawnByCenterPosition(newPosition, wallToEnlarge.SpawnOrientation,
                wallToEnlarge.ExtendableRoom);
            Undo.RegisterCreatedObjectUndo(newWall.gameObject, "");
            newWall.CopyNeighbors(wallToEnlarge);
            newWall.CopySideAndTopMaterials(wallToEnlarge);

            if (Selection.Contains(wallToEnlarge.gameObject))
            {
                AddToSelection(newWall, false);
            }

            Undo.DestroyObjectImmediate(wallToEnlarge.gameObject);
            return newWall;
        }

        private static (RoomElement shrunkWall, RoomElement corner) ShrinkWall(RoomElement wallToShrink,
            bool spawnWithCorner, bool clockwise)
        {
            if (!wallToShrink.Type.IsWallType())
            {
                throw new NotSupportedException($"Can only shrink walls! Found {wallToShrink.Type}");
            }

            var spawnerList = wallToShrink.ExtendableRoom.ElementSpawner;
            RoomElementType spawnerType;

            switch (wallToShrink.Type)
            {
                case RoomElementType.Wall:
                    spawnerType = clockwise
                        ? RoomElementType.WallShortenedLeft
                        : RoomElementType.WallShortenedRight;
                    break;
                case RoomElementType.WallShortenedLeft:
                    goto case RoomElementType.WallShortenedRight;
                case RoomElementType.WallShortenedRight:
                    spawnerType = RoomElementType.WallShortenedBothEnds;
                    break;
                case RoomElementType.WallShortenedBothEnds:
                    throw new NotSupportedException($"Can not shrink wall of type {wallToShrink.Type} more!");
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var spawner = spawnerList[(int) spawnerType];
            var direction =
                wallToShrink.SpawnOrientation.Shift(clockwise)
                    .ToDirection(); // either a left or right shift from the current wall
            var spawnPosition = GetPositionForWallReplacement(wallToShrink, spawnerType, true);
            var (shrunkWall, _) = spawner.SpawnByCenterPosition(spawnPosition, wallToShrink.SpawnOrientation,
                wallToShrink.ExtendableRoom);

            //connection handling
            shrunkWall.CopyNeighbors(wallToShrink);
            shrunkWall.CopySideAndTopMaterials(wallToShrink);

            RoomElement corner = null;

            if (spawnWithCorner)
            {
                var cornerSpawner = spawnerList[(int) RoomElementType.Corner];
                corner = cornerSpawner.SpawnNextToRoomElement(shrunkWall, direction.Opposite(),
                    GetCornerOrientationBasedOnWall(shrunkWall, direction.Opposite()));
                shrunkWall.ConnectElementByDirection(corner, direction.Opposite());
            }

            if (Selection.Contains(wallToShrink.gameObject))
            {
                AddToSelection(shrunkWall, false);
            }

            DisconnectAndDestroyWithUndo(wallToShrink); //delete old wall

            return (shrunkWall, corner);
        }

        private static void AddToSelection(RoomElement newSelectedElement, bool asActiveObject)
        {
            var oldSelection = Selection.objects;
            var newSelection = new UnityEngine.Object[oldSelection.Length + 1];
            oldSelection.CopyTo(newSelection, 0);
            newSelection[newSelection.Length - 1] = newSelectedElement.gameObject;
            Selection.objects = newSelection;

            if (asActiveObject)
            {
                Selection.activeGameObject = newSelectedElement.gameObject;
            }
        }

        #endregion

        #region Material Handling

        private static void ReplaceWallSideMaterialsIfNecessary(RoomElement wall, RoomElement floorConnectedToWall,
            RoomElement elementInFrontDirectionOfWall)
        {
            if (WallSideFacesFront(wall, floorConnectedToWall))
            {
                UpdateWallSideTexture(wall, true);

                if (elementInFrontDirectionOfWall != null && elementInFrontDirectionOfWall.Type.IsWallType())
                {
                    UpdateWallSideTexture(elementInFrontDirectionOfWall, false);
                }
            }
        }

        private static void MakeCornerTransparentIfNecessary(RoomElement corner, RoomElement neighborWall)
        {
            if (IsFrontFacingCorner(corner, neighborWall))
            {
                corner.SetAllMaterialsTransparent();
            }
        }

        #endregion

        #region Deletion

        private static void DeleteSquareOfShortestWall(WallConditions wallConditions)
        {
            //Spawn newFloor
            var (newFloor, _) = SpawnFloorNextToSelectedWall(wallConditions.Wall);

            // Connect newFloor in clockwise direction
            var wall = wallConditions.GetElement(1, true);
            ConnectFloorBasedOnWall(wall);

            DisconnectAndDestroyWithUndo(wall);

            // Connect newFloor in counter clockwise direction
            wall = wallConditions.GetElement(1, false);
            ConnectFloorBasedOnWall(wall);

            DisconnectAndDestroyWithUndo(wall);

            // Connect newFloor to opposite direction of wall
            wall = wallConditions.GetElement(3, true);
            ConnectFloorBasedOnWall(wall);

            DisconnectAndDestroyWithUndo(wall);

            // Destroying selected wall
            wall = wallConditions.Wall;
            DisconnectAndDestroyWithUndo(wall);

            //Destroying corner
            var corner = wallConditions.GetElement(0, true);
            DisconnectAndDestroyWithUndo(corner);

            corner = wallConditions.GetElement(2, true);
            DisconnectAndDestroyWithUndo(corner);


            corner = wallConditions.GetElement(0, false);
            DisconnectAndDestroyWithUndo(corner);

            corner = wallConditions.GetElement(2, false);
            DisconnectAndDestroyWithUndo(corner);

            void ConnectFloorBasedOnWall(RoomElement referenceWall)
            {
                var direction = referenceWall.SpawnOrientation.ToDirection();
                var directionOpposite = direction.Opposite();
                var neighboringFloor = referenceWall.GetRoomElementByDirection(directionOpposite);
                Undo.RecordObject(neighboringFloor, "");
                newFloor.ConnectElementByDirection(neighboringFloor, directionOpposite);
            }
        }

        private static void DisconnectAndDestroyWithUndo(RoomElement roomElement)
        {
            roomElement.DisconnectFromAllNeighbors();
            Undo.DestroyObjectImmediate(roomElement.gameObject);
        }

        #endregion

        #region Handle Collision

        private static void HandleDirectCollision(WallConditions wallConditions, RoomElement collidingFloor,
            RoomElement newFloor)
        {
            var collidingWall =
                collidingFloor.GetRoomElementByDirection(wallConditions.Wall.SpawnOrientation.ToDirection().Opposite());
            var collidingWallConditions = GetWallConditions(collidingWall);

            ConnectCollidingFloors(wallConditions.Wall, newFloor, collidingFloor);

            if ((wallConditions.CornerSituation & CornerSituation.Clockwise) == CornerSituation.Clockwise)
            {
                HandleDirectCollisionWithCorner(wallConditions, collidingWallConditions, newFloor, true);
                var corner = wallConditions.GetElement(0, true);
                DisconnectAndDestroyWithUndo(corner);
            }
            else
            {
                HandleDirectCollisionNoCorner(wallConditions, collidingWallConditions, newFloor, true);
            }

            if ((wallConditions.CornerSituation & CornerSituation.CounterClockwise) ==
                CornerSituation.CounterClockwise)
            {
                HandleDirectCollisionWithCorner(wallConditions, collidingWallConditions, newFloor, false);
                var corner = wallConditions.GetElement(0, false);
                DisconnectAndDestroyWithUndo(corner);
            }
            else
            {
                HandleDirectCollisionNoCorner(wallConditions, collidingWallConditions, newFloor, false);
            }

            DisconnectAndDestroyWithUndo(wallConditions.Wall);
        }

        private static void ConnectCollidingFloors(RoomElement selectedWall, RoomElement newFloor,
            RoomElement collidingFloor)
        {
            var collisionWall =
                collidingFloor.GetRoomElementByDirection(selectedWall.SpawnOrientation.ToDirection().Opposite());

            DisconnectAndDestroyWithUndo(collisionWall); //delete old wall first - could be also different object

            //floor connection
            collidingFloor.ConnectElementByDirection(newFloor, selectedWall.SpawnOrientation.Opposite().ToDirection());
        }

        private static void HandleDirectCollisionWithCorner(WallConditions wallConditions,
            WallConditions collidingWallConditions, RoomElement newFloor, bool clockwise)
        {
            var collidingWallNeighborNeighbor = collidingWallConditions.GetElement(1, !clockwise);

            if (wallConditions.Wall.SpawnOrientation == SpawnOrientation.Front &&
                collidingWallNeighborNeighbor.SpawnOrientation.IsSideways())
            {
                UpdateWallSideTexture(collidingWallNeighborNeighbor, false);
            }

            var wallNeighborNeighbor = wallConditions.GetElement(1, clockwise);

            if (wallConditions.Wall.SpawnOrientation == SpawnOrientation.Back &&
                wallNeighborNeighbor.SpawnOrientation.IsSideways())
            {
                UpdateWallSideTexture(wallNeighborNeighbor, false);
            }

            var selectedWall = wallConditions.Wall;
            var corner = wallConditions.GetElement(0, clockwise);
            Undo.RecordObject(corner, "");

            var cornerNeighbor = wallConditions.GetElement(1, clockwise);
            Undo.RecordObject(cornerNeighbor, "");

            var spawnOrientation = wallConditions.GetDirection(0, clockwise).ToSpawnOrientation();
            var selectedWallDirection = selectedWall.SpawnOrientation.ToDirection();

            RoomElement newWall;

            var neighborOfCollidingWall = collidingWallConditions.GetElement(0, !clockwise);
            Undo.RecordObject(neighborOfCollidingWall, "");

            //need to handle replacement first
            ElementSpawner customSpawner;
            if (neighborOfCollidingWall.Type.IsWallType()) //if wall > wall shortened one side
            {
                customSpawner = clockwise
                    ? selectedWall.ExtendableRoom.ElementSpawner[(int) RoomElementType.WallShortenedLeft]
                    : selectedWall.ExtendableRoom.ElementSpawner[(int) RoomElementType.WallShortenedRight];

                newWall =
                    customSpawner.SpawnNextToRoomElement(cornerNeighbor, selectedWallDirection, spawnOrientation);

                if (spawnOrientation == SpawnOrientation.Back)
                {
                    newWall.SetTransparentMaterial(MaterialSlotType.Top);
                }

                Undo.RegisterCreatedObjectUndo(newWall.gameObject, "");
                cornerNeighbor.ConnectElementByDirection(newWall, selectedWallDirection);
                newWall.ConnectElementByDirection(newFloor, spawnOrientation.ToDirection().Opposite());

                //Replace Wall next to colliding wall
                var (shrunkWall, newCorner) = ShrinkWall(neighborOfCollidingWall, true, !clockwise);

                //Connect new Corner
                newWall.ConnectElementByDirection(newCorner, selectedWallDirection);

                MakeCornerTransparentIfNecessary(newCorner, shrunkWall);

                return;
            }

            if (neighborOfCollidingWall.Type.IsCornerType()) //if corner > delete
            {
                var direction = selectedWallDirection;
                var behindNeighbor = neighborOfCollidingWall.GetRoomElementByDirection(direction);

                // We have an inner corner
                if (behindNeighbor == null)
                {
                    direction = direction.Opposite();
                    behindNeighbor = neighborOfCollidingWall.GetRoomElementByDirection(direction);

                    // Connect floor to element behind wall
                    var floorNeighborDirection = behindNeighbor.SpawnOrientation.Opposite().ToDirection();
                    var newFloorNeighbor = behindNeighbor.GetRoomElementByDirection(floorNeighborDirection);
                    newFloor.ConnectElementByDirection(newFloorNeighbor, floorNeighborDirection);

                    DisconnectAndDestroyWithUndo(behindNeighbor);
                    DisconnectAndDestroyWithUndo(neighborOfCollidingWall);

                    return;
                }

                customSpawner = selectedWall.ExtendableRoom.ElementSpawner[(int) RoomElementType.Wall];
                Undo.RecordObject(behindNeighbor, "");
                newWall = customSpawner.SpawnNextToRoomElement(behindNeighbor, direction.Opposite(),
                    behindNeighbor.SpawnOrientation);
                Undo.RegisterCreatedObjectUndo(newWall.gameObject, "");

                if (spawnOrientation == SpawnOrientation.Back)
                {
                    newWall.SetTransparentMaterial(MaterialSlotType.Top);
                }


                behindNeighbor.ConnectElementByDirection(newWall, direction.Opposite());
                newWall.ConnectElementByDirection(cornerNeighbor, direction.Opposite());
                newWall.ConnectElementByDirection(newFloor, newWall.SpawnOrientation.ToDirection().Opposite());

                DisconnectAndDestroyWithUndo(neighborOfCollidingWall);

                ReplaceWallSideMaterialsIfNecessary(newWall, newFloor,
                    newWall.GetRoomElementByDirection(Direction.Front));
                return;
            }

            Debug.LogError("not supported cases");
        }

        private static void HandleDirectCollisionNoCorner(WallConditions wallConditions,
            WallConditions collidingWallConditions, RoomElement newFloor, bool clockwise)
        {
            //Handle own neighbors
            var neighboringWall = wallConditions.GetElement(0, clockwise);
            var (_, newCorner) = ShrinkWall(neighboringWall, true, clockwise);

            var collidingWallNeighbor = collidingWallConditions.GetElement(0, !clockwise);

            RoomElement wallInBetweenNeighbor;
            RoomElementType wallInBetweenType;

            if (collidingWallNeighbor.Type.IsWallType())
            {
                var (_, newCorner2) = ShrinkWall(collidingWallNeighbor, true, !clockwise);
                wallInBetweenType = RoomElementType.WallShortenedBothEnds;
                wallInBetweenNeighbor = newCorner2;
            }
            else
            {
                DisconnectAndDestroyWithUndo(collidingWallNeighbor);
                wallInBetweenType = clockwise ? RoomElementType.WallShortenedRight : RoomElementType.WallShortenedLeft;
                wallInBetweenNeighbor = collidingWallConditions.GetElement(1, !clockwise);
            }

            var spawnOrientation = wallConditions.Wall.SpawnOrientation.Shift(clockwise);
            var spawnDirection = wallConditions.Wall.SpawnOrientation.ToDirection();

            var wallInBetween = SpawnWallBasedOnCorner(newCorner, spawnOrientation, spawnDirection, wallInBetweenType);
            wallInBetween.ConnectElementByDirection(newFloor, wallInBetween.SpawnOrientation.Opposite().ToDirection());
            wallInBetween.ConnectElementByDirection(wallInBetweenNeighbor, spawnDirection);

            ReplaceWallSideMaterialsIfNecessary(wallInBetween, newFloor,
                wallInBetween.GetRoomElementByDirection(Direction.Front));
        }

        private static void HandleDiagonalCollision(WallConditions wallConditions, RoomElement newFloor, FloorElement diagonalFloor, bool clockwise)
        {
            var collidingWall =
                diagonalFloor.GetRoomElementByDirection(wallConditions.Wall.SpawnOrientation.Opposite().ToDirection());
            var collidingWallConditions = GetWallConditions(collidingWall);

            var selectedWallSpawnOrientation = wallConditions.Wall.SpawnOrientation;
            var selectedWallDirection = selectedWallSpawnOrientation.ToDirection();

            //Replace Colliding wall 
            var oldWallNeighbor = wallConditions.GetElement(0, clockwise);

            // Shrink wall that next to the corner in clockwise direction from the colliding wall
            var collidingWallWallAroundCorner = collidingWallConditions.GetElement(1, clockwise);
            (collidingWallWallAroundCorner, _) = ShrinkWall(collidingWallWallAroundCorner, false, clockwise);

            // Shrink colliding wall
            var (_, collidingWallCorner) = ShrinkWall(collidingWall, true, !clockwise);

            // Shrink selected wall
            var (shrunkWall, corner) = ShrinkWall(wallConditions.Wall, true, !clockwise);

            wallConditions.Wall = shrunkWall;
            corner.ConnectElementByDirection(collidingWallWallAroundCorner, selectedWallDirection);

            if (oldWallNeighbor.Type.IsCornerType())
            {
                // Spawn new wall in clockwise direction of selected wall
                var spawnOrientation = selectedWallSpawnOrientation.Shift(clockwise);
                var selectedWallOppositeDirection = selectedWallDirection.Opposite();
                var wallType = clockwise ? RoomElementType.WallShortenedLeft : RoomElementType.WallShortenedRight;
                var newWall = SpawnWallBasedOnCorner(collidingWallCorner, spawnOrientation, selectedWallOppositeDirection,
                    wallType);

                // Connect the new spawned wall to wall that next to the corner in clockwise direction from the selected wall
                var oldNeighborAroundCornerOfSelectedWall = wallConditions.GetElement(1, clockwise);
                Undo.RecordObject(oldNeighborAroundCornerOfSelectedWall, "");
                newWall.ConnectElementByDirection(oldNeighborAroundCornerOfSelectedWall,
                    wallConditions.GetDirection(1, clockwise));
                // Connect the new spawned wall to the new spawned floor
                newWall.ConnectElementByDirection(newFloor, newWall.SpawnOrientation.Opposite().ToDirection());

                DisconnectAndDestroyWithUndo(oldWallNeighbor);

                ReplaceWallSideMaterialsIfNecessary(newWall, newFloor,
                    newWall.GetRoomElementByDirection(Direction.Front));
            }
            else if (oldWallNeighbor.Type.IsWallType())
            {
                var (_, cornerNextToWallNeighbor) = ShrinkWall(oldWallNeighbor, true, clockwise);

                var spawnOrientation = selectedWallSpawnOrientation.Shift(clockwise);

                //spawn 2er wall
                var newWall = SpawnWallBasedOnCorner(cornerNextToWallNeighbor, spawnOrientation, selectedWallDirection,
                    RoomElementType.WallShortenedBothEnds);
                newWall.ConnectElementByDirection(collidingWallCorner, selectedWallDirection);
                newWall.ConnectElementByDirection(newFloor, newWall.SpawnOrientation.Opposite().ToDirection());

                ReplaceWallSideMaterialsIfNecessary(newWall, newFloor,
                    newWall.GetRoomElementByDirection(Direction.Front));
            }

            var collidingWallNeighboringCorner = collidingWallConditions.GetElement(0, clockwise);
            DisconnectAndDestroyWithUndo(collidingWallNeighboringCorner);
        }

        #endregion

        #region Handle Extension Without Merge

        private static void HandleCornerNextToSelectedWall(WallConditions wallConditions, RoomElement newFloor,
            Vector3 movementDelta, bool clockwise)
        {
            var corner = wallConditions.GetElement(0, clockwise);
            Undo.RecordObject(corner.transform, "");
            Undo.RecordObject(corner, "");
            corner.transform.position += movementDelta;
            var spawnOrientation = wallConditions.GetDirection(0, clockwise).ToSpawnOrientation();
            var spawnDirection = wallConditions.Wall.SpawnOrientation.Opposite().ToDirection();
            var newWall = SpawnWallBasedOnCorner(corner, spawnOrientation, spawnDirection, RoomElementType.Wall);
            newFloor.ConnectElementByDirection(newWall, newWall.SpawnOrientation.ToDirection());
            ReplaceWallSideMaterialsIfNecessary(newWall, newFloor, newWall.GetRoomElementByDirection(Direction.Front));
        }

        private static void HandleWallNextToSelectedWall(WallConditions wallConditions, RoomElement newFloor,
            bool clockwise)
        {
            // Get old neighbor of selected wall (should be wall type)
            var selectedWallNeighbor = wallConditions.GetElement(0, clockwise);

            // Shrink oldWall because of inner corner that's gonna be placed right next to it
            var (_, innerCorner) = ShrinkWall(selectedWallNeighbor, true, clockwise);

            // Spawn shortened wall next to newCorner1 and connect shortened Wall to corner1
            var wallType = clockwise ? RoomElementType.WallShortenedRight : RoomElementType.WallShortenedLeft;
            var spawnOrientation = wallConditions.GetDirection(0, clockwise).ToSpawnOrientation();
            var spawnDirection = wallConditions.Wall.SpawnOrientation.ToDirection();
            var newShortenedWall = SpawnWallBasedOnCorner(innerCorner, spawnOrientation, spawnDirection, wallType);

            // Connect new short wall to new floor
            newShortenedWall.ConnectElementByDirection(newFloor,
                newShortenedWall.SpawnOrientation.Opposite().ToDirection());

            // Spawn Corner next to selected wall and connect to selected wall
            var outerCorner = SpawnCornerNextToWall(wallConditions.Wall, clockwise);
            // Connect outerCorner to newShortenedWall
            outerCorner.ConnectElementByDirection(newShortenedWall,
                wallConditions.Wall.SpawnOrientation.ToDirection().Opposite());

            MakeCornerTransparentIfNecessary(outerCorner, newShortenedWall);
            ReplaceWallSideMaterialsIfNecessary(newShortenedWall, newFloor,
                newShortenedWall.GetRoomElementByDirection(Direction.Front));
        }

        private static void HandleCornerNextToShortenedWall(WallConditions wallConditions, RoomElement newFloor,
            Vector3 movementDelta, bool clockwise)
        {
            var selectedWall = wallConditions.Wall;
            var corner = wallConditions.GetElement(0, clockwise);
            Undo.RecordObject(corner, "");

            //get other wall connected to corner, when found shorter wall, then delete corner otherwise move corner
            var otherWallConnectedToCorner = wallConditions.GetElement(1, clockwise);
            var otherWallConnectedToCornerDirection = wallConditions.GetDirection(1, clockwise);

            //The room extends further next to selected wall; it isn't a 'normal' outer corner
            if (otherWallConnectedToCornerDirection == selectedWall.SpawnOrientation.ToDirection())
            {
                Undo.RecordObject(otherWallConnectedToCorner, ""); //store bindings

                //Connect newFloor to floorNextToOtherWallConnectedToCorner
                var otherWallDirection = otherWallConnectedToCorner.SpawnOrientation.ToDirection();

                var floorNextToOtherWallConnectedToCorner =
                    otherWallConnectedToCorner.GetRoomElementByDirection(otherWallDirection.Opposite());
                Undo.RecordObject(floorNextToOtherWallConnectedToCorner, "");

                floorNextToOtherWallConnectedToCorner.ConnectElementByDirection(newFloor, otherWallDirection);


                if (wallConditions.WallWasReplaced && wallConditions.Wall.Type == RoomElementType.Wall)
                {
                    //delete corner (next to otherWallConnectedToCorner)
                    var unnecessaryCorner = wallConditions.GetElement(2, clockwise);
                    Undo.RecordObject(unnecessaryCorner, "");
                    var newNeighbor = wallConditions.GetElement(3, clockwise);
                    Undo.RecordObject(newNeighbor, "");
                    selectedWall.ConnectElementByDirection(newNeighbor,
                        wallConditions.GetDirection(0, clockwise));
                    DisconnectAndDestroyWithUndo(unnecessaryCorner);
                    DisconnectAndDestroyWithUndo(corner); //delete corner (next to selectedWall)
                    DisconnectAndDestroyWithUndo(otherWallConnectedToCorner); //delete otherWallConnectedToCorner (next to corner)

                    return;
                }

                var elementAfterOtherWallConnectedToCorner = wallConditions.GetElement(2, clockwise);
                Undo.RecordObject(elementAfterOtherWallConnectedToCorner, "");

                if (elementAfterOtherWallConnectedToCorner.Type.IsWallType())
                {
                    (elementAfterOtherWallConnectedToCorner, _) =
                        ShrinkWall(elementAfterOtherWallConnectedToCorner, false, clockwise);
                    corner.ConnectElementByDirection(elementAfterOtherWallConnectedToCorner,
                        wallConditions.GetDirection(1, clockwise));
                    // Move corner by movementDelta
                    Undo.RecordObject(corner.transform, "");
                    corner.transform.position += movementDelta;
                }

                if (elementAfterOtherWallConnectedToCorner.Type.IsCornerType())
                {
                    //connect selectedWall with newWallNeighbor
                    var newWallNeighbor = wallConditions.GetElement(3, clockwise);
                    var newWallNeighborDirection = wallConditions.GetDirection(3, clockwise);
                    selectedWall.ConnectElementByDirection(newWallNeighbor, newWallNeighborDirection);

                    //Delete corner next to selected wall
                    DisconnectAndDestroyWithUndo(corner);
                    //Delete elementAfterOtherWallConnectedToCorner which is a corner
                    DisconnectAndDestroyWithUndo(elementAfterOtherWallConnectedToCorner);
                }

                // Destroy unnecessary in-between wall element
                Undo.DestroyObjectImmediate(otherWallConnectedToCorner.gameObject);

                if (selectedWall.SpawnOrientation == SpawnOrientation.Back)
                {
                    selectedWall.SetTransparentMaterial(MaterialSlotType.Top);
                }

                return;
            }

            HandleCornerNextToSelectedWall(wallConditions, newFloor, movementDelta, clockwise);
        }

        #endregion

        #region Spawning

        private static RoomElement SpawnCornerNextToWall(RoomElement wall, bool clockwise)
        {
            var shiftValue = clockwise ? 1 : -1;
            var selectedWallDirection = wall.SpawnOrientation.ToDirection();
            var spawnDirection = selectedWallDirection.Shift(shiftValue);

            var spawnerList = wall.ExtendableRoom.ElementSpawner;
            var cornerSpawner = spawnerList[(int) RoomElementType.Corner];
            var cornerOrientation = GetCornerOrientationBasedOnWall(wall, spawnDirection);
            var newCorner = cornerSpawner.SpawnNextToRoomElement(wall, spawnDirection, cornerOrientation);
            Undo.RegisterCreatedObjectUndo(newCorner.gameObject, "");
            Undo.RecordObject(wall, "");

            wall.ConnectElementByDirection(newCorner, spawnDirection);

            return newCorner;
        }

        private static RoomElement SpawnWallBasedOnCorner(RoomElement corner, SpawnOrientation spawnOrientation,
            Direction spawnDirection, RoomElementType spawnerType)
        {
            var spawnerList = corner.ExtendableRoom.ElementSpawner;
            var spawner = spawnerList[(int) spawnerType];
            var newWall = spawner.SpawnNextToRoomElement(corner, spawnDirection, spawnOrientation);
            var oldElementInSpawnDirection = corner.GetRoomElementByDirection(spawnDirection);

            if (oldElementInSpawnDirection != null)
            {
                Undo.RecordObject(oldElementInSpawnDirection, "");
                newWall.ConnectElementByDirection(oldElementInSpawnDirection, spawnDirection);
            }

            corner.ConnectElementByDirection(newWall, spawnDirection);

            Undo.RegisterCreatedObjectUndo(newWall.gameObject, "");

            if (spawnOrientation == SpawnOrientation.Back)
            {
                newWall.SetTransparentMaterial(MaterialSlotType.Top);
            }

            return newWall;
        }

        private static (RoomElement newFloor, RoomElement collidingFloor) SpawnFloorNextToSelectedWall(
            RoomElement selectedWall)
        {
            var wallDirection = selectedWall.SpawnOrientation.ToDirection();
            var spawnList = selectedWall.ExtendableRoom.ElementSpawner;
            var floorSpawner = spawnList[(int) RoomElementType.Floor];

            // Get The old floor which was connected to the wall
            var oldFloor = selectedWall.GetRoomElementByDirection(wallDirection.Opposite());
            var newGridPosition = GetGridPosition(oldFloor, wallDirection);
            Undo.RecordObject(oldFloor, "");
            // Old floor is used to spawn the new floor because otherwise there could be offset problems if the wall would be a shortened one
            var newFloor = floorSpawner.SpawnNextToRoomElement(oldFloor, wallDirection, SpawnOrientation.Front);
            Undo.RegisterCreatedObjectUndo(newFloor.gameObject, "");
            ((FloorElement) newFloor).GridPosition = newGridPosition;
            Undo.RecordObject(selectedWall.ExtendableRoom, "");

            var floorGridDictionary = selectedWall.ExtendableRoom.FloorGridDictionary;
            floorGridDictionary.Add(newGridPosition, newFloor as FloorElement);

            //Connect elements
            oldFloor.ConnectElementByDirection(newFloor, wallDirection);

            //check if there is a collision
            newGridPosition = GetGridPosition(newFloor, wallDirection);

            if (floorGridDictionary.ContainsKey(newGridPosition))
            {
                var collidingFloor = floorGridDictionary[newGridPosition];
                return (newFloor, collidingFloor);
            }

            newFloor.ConnectElementByDirection(selectedWall, wallDirection);

            return (newFloor, null);
        }

        #endregion

        #region Get Information Around RoomElements

        public static Vector2Int GetGridPosition(Vector2Int vectorPos, Direction spawnDirection)
        {
            switch (spawnDirection)
            {
                case Direction.Front:
                    return vectorPos + Vector2Int.up;
                case Direction.Right:
                    return vectorPos + Vector2Int.right;
                case Direction.Back:
                    return vectorPos + Vector2Int.down;
                case Direction.Left:
                    return vectorPos + Vector2Int.left;
                default:
                    throw new ArgumentOutOfRangeException(nameof(spawnDirection), spawnDirection,
                        "invalid spawnDirection");
            }
        }

        private static Vector2Int GetGridPosition(RoomElement oldFloor, Direction spawnDirection)
        {
            var oldFloorElement = oldFloor as FloorElement;

            if (oldFloorElement == null)
            {
                throw new Exception("GetGridPosition expects floorElement as parameter!");
            }

            var oldFloorGridPosition = oldFloorElement.GridPosition;

            return GetGridPosition(oldFloorGridPosition, spawnDirection);
        }

        private static bool HasCornerAndAnotherCornerInDirection(WallConditions wallConditions, bool clockwise)
        {
            var selectedWallNeigbor = wallConditions.GetElement(0, clockwise);

            if (!selectedWallNeigbor.Type.IsCornerType() || !IsInnerCorner(selectedWallNeigbor, wallConditions.Wall))
            {
                return false;
            }

            // If we have a double corner, the element after the corner which is always should be a wall should have a corner thereafter
            return wallConditions.GetElement(2, clockwise).Type.IsCornerType();
        }

        private static bool ShortenedWallNeedsToBeReplacedThroughFullWall(WallConditions wallConditions)
        {
            return HasCornerAndAnotherCornerInDirection(wallConditions, true) ||
                   HasCornerAndAnotherCornerInDirection(wallConditions, false);
        }

        private static bool IsInnerCorner(RoomElement corner, RoomElement selectedWallNeighbor)
        {
            return corner.Type == RoomElementType.Corner &&
                   corner.GetRoomElementByDirection(selectedWallNeighbor.SpawnOrientation.ToDirection()) != null;
        }

        private static bool IsFrontFacingCorner(RoomElement corner, RoomElement wall)
        {
            if (corner.Type != RoomElementType.Corner || IsInnerCorner(corner, wall))
            {
                return false;
            }

            return corner.GetRoomElementByDirection(Direction.Front) != null;
        }

        private static WallConditions GetWallConditions(RoomElement wall, int elementsToExtractPerDirection = 5)
        {
            var wallExtensionScenario = CornerSituation.None;

            if (!wall.Type.IsWallType())
            {
                return null;
            }

            var clockwiseElements = ExtractElementsNextToWall(wall, elementsToExtractPerDirection, true);
            var counterClockwiseElements = ExtractElementsNextToWall(wall, elementsToExtractPerDirection, false);

            if (clockwiseElements[0].roomElement.Type.IsCornerType())
            {
                wallExtensionScenario |= CornerSituation.Clockwise;
            }

            if (counterClockwiseElements[0].roomElement.Type.IsCornerType())
            {
                wallExtensionScenario |= CornerSituation.CounterClockwise;
            }

            return new WallConditions(wall, clockwiseElements, counterClockwiseElements, wallExtensionScenario);
        }

        private static (RoomElement roomElement, Direction directionOfElementBasedOnPredecessor)[]
            ExtractElementsNextToWall(RoomElement wall, int elementsToExtract, bool clockwise)
        {
            var shiftDirection = clockwise ? 1 : -1;
            var elements = new (RoomElement, Direction)[elementsToExtract];
            var currentElement = wall;
            var currentDirection = wall.SpawnOrientation.ToDirection().Shift(shiftDirection);

            for (var i = 0; i < elementsToExtract; i++)
            {
                if (currentElement.Type.IsCornerType())
                {
                    var nextDirection = currentDirection.Shift(shiftDirection);

                    if (currentElement.GetRoomElementByDirection(nextDirection) == null)
                    {
                        nextDirection = currentDirection.Shift(-shiftDirection);
                    }

                    currentDirection = nextDirection;
                }

                var nextElement = currentElement.GetRoomElementByDirection(currentDirection);

                currentElement = nextElement;
                elements[i] = (nextElement, currentDirection);
            }

            return elements;
        }

        private static SpawnOrientation GetCornerOrientationBasedOnWall(RoomElement wall, Direction direction)
        {
            if (!wall.Type.IsWallType())
            {
                throw new Exception("Fail");
            }

            if (wall.Type == RoomElementType.WallShortenedRight)
            {
                return wall.SpawnOrientation.Shift(-1); //-90 rotation of corner
            }

            if (wall.Type == RoomElementType.WallShortenedLeft)
            {
                return wall.SpawnOrientation;
            }

            if (wall.Type == RoomElementType.WallShortenedBothEnds)
            {
                if (wall.SpawnOrientation.ToDirection().Shift(1) == direction)
                {
                    return wall.SpawnOrientation.Shift(-1);
                }

                return wall.SpawnOrientation;
            }

            switch (wall.SpawnOrientation)
            {
                case SpawnOrientation.Front:
                    if (direction == Direction.Left)
                    {
                        return SpawnOrientation.Right; //+180
                    }

                    if (direction == Direction.Right)
                    {
                        return SpawnOrientation.Back; // +90
                    }

                    break;
                case SpawnOrientation.Right:
                    if (direction == Direction.Front)
                    {
                        return SpawnOrientation.Back; //+90
                    }

                    if (direction == Direction.Back)
                    {
                        return SpawnOrientation.Left; //+180
                    }

                    break;
                case SpawnOrientation.Back:
                    if (direction == Direction.Right)
                    {
                        return SpawnOrientation.Left;
                    }

                    if (direction == Direction.Left)
                    {
                        return SpawnOrientation.Front;
                    }

                    break;
                case SpawnOrientation.Left:
                    if (direction == Direction.Back)
                    {
                        return SpawnOrientation.Front;
                    }

                    if (direction == Direction.Front)
                    {
                        return SpawnOrientation.Right;
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Debug.LogError(
                $"Not supported wall orientation {wall.SpawnOrientation} and corner in direction {direction}");
            return wall.SpawnOrientation.Shift(1);
        }

        private static bool WallSideFacesFront(RoomElement wall, RoomElement floor)
        {
            if (!wall.SpawnOrientation.IsSideways())
            {
                return false;
            }

            var backElementGridPos = GetGridPosition(floor, Direction.Back);
            return !wall.ExtendableRoom.FloorGridDictionary.ContainsKey(backElementGridPos);
        }

        private static void UpdateWallSideTexture(RoomElement roomElement, bool wallSideMaterial)
        {
            if (!roomElement.SpawnOrientation.IsSideways())
            {
                return;
            }

            var materialSlotType = roomElement.SpawnOrientation == SpawnOrientation.Left
                ? MaterialSlotType.Left
                : MaterialSlotType.Right;

            if (wallSideMaterial)
            {
                roomElement.SetWallSideMaterial(materialSlotType);
                return;
            }

            roomElement.SetTransparentMaterial(materialSlotType);
        }

        private static Vector3 GetPositionForWallReplacement(RoomElement wallToReplace, RoomElementType newWallType,
            bool isShrinkingOperation)
        {
            var wallToShrinkDirection = wallToReplace.SpawnOrientation.ToDirection();
            var newPosition = wallToReplace.transform.position;
            var spawnerList = wallToReplace.ExtendableRoom.ElementSpawner;
            var wallToShrinkSpawner = spawnerList[(int) wallToReplace.Type];
            var newWallSpawner = spawnerList[(int) newWallType];
            float boundDifference;
            int signFactor = 1;

            if (isShrinkingOperation)
            {
                // Assumption wallToShrink should've larger extents than those of given goal type
                boundDifference = wallToShrinkSpawner.Bounds.extents.x - newWallSpawner.Bounds.extents.x;
            }
            else
            {
                boundDifference = newWallSpawner.Bounds.extents.x - wallToShrinkSpawner.Bounds.extents.x;
            }

            if (newWallType == RoomElementType.WallShortenedLeft && isShrinkingOperation)
            {
                signFactor = GetSignFactorForShrinkingShortenedWall(wallToShrinkDirection, false);
            }

            if (newWallType == RoomElementType.WallShortenedRight && isShrinkingOperation)
            {
                signFactor = GetSignFactorForShrinkingShortenedWall(wallToShrinkDirection, true);
            }

            if (newWallType == RoomElementType.WallShortenedBothEnds && isShrinkingOperation)
            {
                signFactor = !wallToShrinkDirection.IsSideways() == wallToShrinkDirection.TowardsNegative() ? 1 : -1;
                signFactor *= wallToReplace.Type == RoomElementType.WallShortenedRight ? -1 : 1;
            }

            if (newWallType == RoomElementType.Wall && !isShrinkingOperation)
            {
                signFactor = -1 * GetSignFactorForShrinkingShortenedWall(wallToShrinkDirection, wallToReplace.Type == RoomElementType.WallShortenedRight);
            }

            if (wallToShrinkDirection.IsSideways())
            {
                newPosition.z += signFactor * boundDifference;
                return newPosition;
            }

            newPosition.x += signFactor * boundDifference;
            return newPosition;
        }

        private static int GetSignFactorForShrinkingShortenedWall(Direction wallToShrinkDirection,
            bool isShortenedRight)
        {
            var signFactor = !wallToShrinkDirection.IsSideways() == wallToShrinkDirection.TowardsNegative() ? 1 : -1;
            return isShortenedRight ? signFactor : (-1 * signFactor);
        }

        #endregion

        private class WallConditions
        {
            public RoomElement Wall
            {
                get => wall;

                set
                {
                    wall = value;
                    WallWasReplaced = true;
                }
            }

            private RoomElement wall;
            public bool WallWasReplaced { get; set; }

            private (RoomElement roomElement, Direction directionOfElementBasedOnPredecessor)[] ClockwiseElements { get; }

            private (RoomElement roomElement, Direction directionOfElementBasedOnPredecessor)[] CounterClockwiseElements { get; }

            public CornerSituation CornerSituation { get; }

            public WallConditions(RoomElement wall, (RoomElement, Direction)[] clockwiseElements,
                (RoomElement, Direction)[] counterClockwiseElements, CornerSituation cornerSituation)
            {
                this.wall = wall;
                ClockwiseElements = clockwiseElements;
                CounterClockwiseElements = counterClockwiseElements;
                CornerSituation = cornerSituation;
            }

            public RoomElement GetElement(int index, bool clockwise)
            {
                if (index > ClockwiseElements.Length - 1)
                {
                    throw new ArgumentOutOfRangeException(
                        $"WallConditions only contain {ClockwiseElements.Length} elements in each direction. Index {index} is therefore not accesible. Please create WallConditions with more element in each direction or request a lower index.");
                }

                return clockwise ? ClockwiseElements[index].roomElement : CounterClockwiseElements[index].roomElement;
            }

            public Direction GetDirection(int index, bool clockwise)
            {
                if (index > ClockwiseElements.Length - 1)
                {
                    throw new ArgumentOutOfRangeException(
                        $"WallConditions only contain {ClockwiseElements.Length} elements in each direction. Index {index} is therefore not accesible. Please create WallConditions with more element in each direction or request a lower index.");
                }

                return clockwise
                    ? ClockwiseElements[index].directionOfElementBasedOnPredecessor
                    : CounterClockwiseElements[index].directionOfElementBasedOnPredecessor;
            }
        }


        [Flags]
        private enum CornerSituation
        {
            None = 0,
            Clockwise = 1 << 0,
            CounterClockwise = 1 << 1,
            [UsedImplicitly] BothDirections = CounterClockwise | Clockwise
        }
    }
}