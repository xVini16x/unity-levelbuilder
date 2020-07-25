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

        public static void ExtendTheRoom(List<RoomElement> walls, Vector3 movementDelta)
        {
            var wallToMove = walls[0];
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Room Extension");
            var undoGroupId = Undo.GetCurrentGroup();

            var wallConditions = GetWallConditions(wallToMove);

            Undo.RecordObject(wallToMove.transform, "");
            Undo.RecordObject(wallToMove, "");
            wallToMove.transform.position += movementDelta;


            //Prevent spawning double walls, shorter wall is only spawned when there is already an additional way
            if (wallToMove.Type.IsShortenedWall())
            {
                ExtendShorterWall(wallConditions, movementDelta);
            }
            else if (wallToMove.Type == RoomElementType.WallShortenedBothEnds)
            {
                //TODO
                Debug.LogWarning("Extending wall shortened on both ends not supported yet");
            }
            else
            {
                ExtendFullWall(wallConditions, movementDelta);
            }

            Undo.CollapseUndoOperations(undoGroupId);
        }

        private static void ExtendFullWall(WallConditions wallConditions, Vector3 movementDelta)
        {
            var (newFloor, collidingFloor) = SpawnFloorNextToMovedWall(wallConditions.Wall);

            if (collidingFloor != null)
            {
                HandleDirectCollision(wallConditions, collidingFloor, newFloor);
                return;
            }

            if ((wallConditions.CornerSituation & CornerSituation.CounterClockwise) ==
                CornerSituation.CounterClockwise)
            {
                var newWall = MoveCornerSpawnFullWall(wallConditions, movementDelta, false);
                newFloor.ConnectElementByDirection(newWall, newWall.SpawnOrientation.ToDirection());
            }
            else
            {
                Spawn2CornerAnd2ShortWalls(wallConditions, newFloor, false);
            }

            if ((wallConditions.CornerSituation & CornerSituation.Clockwise) ==
                CornerSituation.Clockwise)
            {
                var newWall = MoveCornerSpawnFullWall(wallConditions, movementDelta, true);
                newFloor.ConnectElementByDirection(newWall, newWall.SpawnOrientation.ToDirection());
            }
            else
            {
                Spawn2CornerAnd2ShortWalls(wallConditions, newFloor, true);
            }
        }

        private static void ExtendShorterWall(WallConditions wallConditions, Vector3 movementDelta)
        {
            var wallNeedsToBePlaced = ShorterWallNeedsToBeReplacedThroughFullWall(wallConditions);

            var (newFloor, collidingFloor) = SpawnFloorNextToMovedWall(wallConditions.Wall);

            if (collidingFloor)
            {
                HandleDirectCollision(wallConditions, collidingFloor, newFloor);    
                return;
            }
            
            if (wallNeedsToBePlaced)
            {
                wallConditions.Wall = EnlargeShortenedWall(wallConditions.Wall);
            }

            if ((wallConditions.CornerSituation & CornerSituation.CounterClockwise) ==
                CornerSituation.CounterClockwise)
            {
                HandleCornerNextToShorterWall(wallConditions, newFloor, movementDelta, false);
            }
            else
            {
                Spawn2CornerAnd2ShortWalls(wallConditions, newFloor, false);
            }

            if ((wallConditions.CornerSituation & CornerSituation.Clockwise) ==
                CornerSituation.Clockwise)
            {
                HandleCornerNextToShorterWall(wallConditions, newFloor, movementDelta, true);
            }
            else
            {
                Spawn2CornerAnd2ShortWalls(wallConditions, newFloor, true);
            }
        }

        #endregion

        #region Replace

        private static RoomElement EnlargeShortenedWall(RoomElement wallToMove)
        {
            var spawnerList = wallToMove.ExtendableRoom.ElementSpawner;
            //replace this wall and delete corner
            var newWallSpawner = spawnerList[(int) RoomElementType.Wall];
            var newPosition = GetWallPositionBasedOnShorterWall(wallToMove);
            var (newWall, _) = newWallSpawner.SpawnByCenterPosition(newPosition, wallToMove.SpawnOrientation,
                wallToMove.ExtendableRoom, "");
            Undo.RegisterCreatedObjectUndo(newWall.gameObject, "");
            newWall.CopyNeighbors(wallToMove);
            Undo.DestroyObjectImmediate(wallToMove.gameObject);
            return newWall;
        }

        /**
         *  Dont forget to connect corner with other wall
         */
        private static (RoomElement shrunkWall, RoomElement corner) ShrinkWall(RoomElement wallToShrink, bool spawnWithCorner, bool clockwise)
        {
            if (!wallToShrink.Type.IsWallType())
            {
                throw new NotSupportedException("Can only shrink walls!");
            }

            var spawnerList = wallToShrink.ExtendableRoom.ElementSpawner;
            ElementSpawner spawner;

            switch (wallToShrink.Type)
            {
                case RoomElementType.Wall:
                    goto case RoomElementType.WallTransparent;
                case RoomElementType.WallTransparent:
                    spawner = clockwise
                        ? spawnerList[(int) RoomElementType.WallShortenedLeft]
                        : spawnerList[(int) RoomElementType.WallShortenedRight];
                    break;
                case RoomElementType.WallShortenedLeft:
                    goto case RoomElementType.WallShortenedRight;
                case RoomElementType.WallShortenedRight:
                    spawner = spawnerList[(int) RoomElementType.WallShortenedBothEnds];
                    break;
                case RoomElementType.WallShortenedBothEnds:
                    throw new NotSupportedException("Can not shrink more!");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            var direction = wallToShrink.SpawnOrientation.ToDirection().Shift(clockwise ? 1 : -1); // either a left or right shift from the current wall
            var wallNeighbor = wallToShrink.GetRoomElementByDirection(direction); //either a corner or wall 
            var wall = spawner.SpawnNextToRoomElement(wallNeighbor, direction.Opposite(), wallToShrink.SpawnOrientation);

            //connection handling
            wall.CopyNeighbors(wallToShrink);

            RoomElement corner = null;
            if (spawnWithCorner)
            {
                var cornerSpawner = spawnerList[(int) RoomElementType.Corner];
                corner = cornerSpawner.SpawnNextToRoomElement(wall, direction.Opposite(),
                    GetCornerOrientationBasedOnWall(wall, direction.Opposite()));
                wall.ConnectElementByDirection(corner, direction.Opposite());
            }

            //delete old wall
            wallToShrink.DisconnectFromAllNeighbors();
            Undo.DestroyObjectImmediate(wallToShrink.gameObject);

            return (wall, corner);
        }

        #endregion

        #region Handle Collision

        private static void HandleDirectCollision(WallConditions wallConditions, RoomElement collidingFloor, RoomElement newFloor)
        {
            var collidingWall = collidingFloor.GetRoomElementByDirection(wallConditions.Wall.SpawnOrientation.ToDirection().Opposite());
            var collidingWallConditions = GetWallConditions(collidingWall);

            ConnectCollidingFloors(wallConditions.Wall, newFloor, collidingFloor);

            //spawn wallShortened One Side
            if ((wallConditions.CornerSituation & CornerSituation.Clockwise) == CornerSituation.Clockwise)
            {
                SpawnShortWallAndCornerOnCollision(wallConditions, collidingWallConditions, newFloor, true);
                var corner = wallConditions.GetElement(0, true);
                corner.DisconnectFromAllNeighbors();
                Undo.DestroyObjectImmediate(corner.gameObject);
            }
            else
            {
                HandleCollisionNoCorner(wallConditions, collidingWallConditions, newFloor, true);
            }


            if ((wallConditions.CornerSituation & CornerSituation.CounterClockwise) ==
                CornerSituation.CounterClockwise)
            {
                SpawnShortWallAndCornerOnCollision(wallConditions, collidingWallConditions, newFloor, false);
                var corner = wallConditions.GetElement(0, false);
                corner.DisconnectFromAllNeighbors();
                Undo.DestroyObjectImmediate(corner.gameObject);
            }
            else
            {
                HandleCollisionNoCorner(wallConditions, collidingWallConditions, newFloor, false);
            }

            //spawn 4 corners & 2 wallsShortenedBothEnds

            //delete walltoMove
            var wallToMove = wallConditions.Wall;
            wallToMove.DisconnectFromAllNeighbors();
            Undo.DestroyObjectImmediate(wallToMove.gameObject);
        }

        private static void ConnectCollidingFloors(RoomElement wallToMove, RoomElement newFloor, RoomElement collidingFloor)
        {
            //Check collision elements
            var newGridPosition = GetGridPosition(newFloor, wallToMove.SpawnOrientation.ToDirection());

            var collisionWall =
                collidingFloor.GetRoomElementByDirection(wallToMove.SpawnOrientation.ToDirection().Opposite());

            //delete old wall first - could be also different object
            collisionWall.DisconnectFromAllNeighbors();
            Undo.DestroyObjectImmediate(collisionWall.gameObject);

            //floor connection
            collidingFloor.ConnectElementByDirection(newFloor, wallToMove.SpawnOrientation.Opposite().ToDirection());
        }
        
        private static (RoomElement, RoomElement) SpawnShortWallAndCornerOnCollision(WallConditions wallConditions, WallConditions collidingWallConditions, RoomElement newFloor, bool clockwise)
        {
            var wallToMove = wallConditions.Wall;
            var corner = wallConditions.GetElement(0, clockwise);
            Undo.RecordObject(corner, "");

            var cornerNeighbor = wallConditions.GetElement(1, clockwise);
            Undo.RecordObject(cornerNeighbor, "");

            var spawnOrientation = wallConditions.GetDirection(0, clockwise).ToSpawnOrientation();
            var wallToMoveDirection = wallToMove.SpawnOrientation.ToDirection();

            RoomElement newWall;

            var neighborOfCollidingWall = collidingWallConditions.GetElement(0, !clockwise);
            Undo.RecordObject(neighborOfCollidingWall, "");

            //need to handle replacement first
            ElementSpawner customSpawner;
            if (neighborOfCollidingWall.Type.IsWallType()) //if wall > wall shortened one side
            {
                //Debug.Log("Called");

                customSpawner = clockwise
                    ? wallToMove.ExtendableRoom.ElementSpawner[(int) RoomElementType.WallShortenedLeft]
                    : wallToMove.ExtendableRoom.ElementSpawner[(int) RoomElementType.WallShortenedRight];

                newWall =
                    customSpawner.SpawnNextToRoomElement(cornerNeighbor, wallToMoveDirection, spawnOrientation);
                Undo.RegisterCreatedObjectUndo(newWall.gameObject, "");
                cornerNeighbor.ConnectElementByDirection(newWall, wallToMoveDirection);
                newWall.ConnectElementByDirection(newFloor, spawnOrientation.ToDirection().Opposite());

                //Replace Wall next to colliding wall
                var (shrunkWall, newCorner) = ShrinkWall(neighborOfCollidingWall, true, !clockwise);

                //Connect new Corner
                newWall.ConnectElementByDirection(newCorner, wallToMoveDirection);

                return (newWall, newCorner);
            }

            if (neighborOfCollidingWall.Type.IsCornerType()) //if corner > delete
            {
                customSpawner = wallToMove.ExtendableRoom.ElementSpawner[(int) RoomElementType.Wall];
                var direction = wallToMoveDirection;
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
                    
                    behindNeighbor.DisconnectFromAllNeighbors();
                    Undo.DestroyObjectImmediate(behindNeighbor.gameObject);
                    
                    neighborOfCollidingWall.DisconnectFromAllNeighbors();
                    Undo.DestroyObjectImmediate(neighborOfCollidingWall.gameObject);

                    return (null, null);
                }
                
                Undo.RecordObject(behindNeighbor, "");
                newWall = customSpawner.SpawnNextToRoomElement(behindNeighbor, direction.Opposite(),
                    behindNeighbor.SpawnOrientation);
                Undo.RegisterCreatedObjectUndo(newWall.gameObject, "");

                behindNeighbor.ConnectElementByDirection(newWall, direction.Opposite());
                newWall.ConnectElementByDirection(cornerNeighbor, direction.Opposite());
                newWall.ConnectElementByDirection(newFloor, newWall.SpawnOrientation.ToDirection().Opposite());

                neighborOfCollidingWall.DisconnectFromAllNeighbors();
                Undo.DestroyObjectImmediate(neighborOfCollidingWall.gameObject);
                return (newWall, null);
            }

            Debug.LogError("not supported cases");
            return (null, null);
        }


        #endregion

        #region Spawning

        #region Multiple Elements
        
        private static void HandleCollisionNoCorner(WallConditions wallConditions, WallConditions collidingWallConditions, RoomElement newFloor, bool clockwise)
        {
            //Handle own neighbors
            var neighboringWall = wallConditions.GetElement(0, clockwise);
            var (shrinkWall, newCorner) = ShrinkWall(neighboringWall, true, clockwise);

            var collidingWallNeighbor = collidingWallConditions.GetElement(0, !clockwise);

            RoomElement wallInBetweenNeighbor;
            RoomElementType wallInBetweenType;

            if (collidingWallNeighbor.Type.IsWallType())
            {
                var (shrinkWall2, newCorner2) = ShrinkWall(collidingWallNeighbor, true, !clockwise);
                wallInBetweenType = RoomElementType.WallShortenedBothEnds;
                wallInBetweenNeighbor = newCorner2;
            }
            else
            {
                collidingWallNeighbor.DisconnectFromAllNeighbors();
                Undo.DestroyObjectImmediate(collidingWallNeighbor.gameObject);
                wallInBetweenType = clockwise ? RoomElementType.WallShortenedRight : RoomElementType.WallShortenedLeft;
                wallInBetweenNeighbor = collidingWallConditions.GetElement(1, !clockwise);
            }  
            
            var spawnOrientation = wallConditions.Wall.SpawnOrientation.Shift(clockwise);
            var spawnDirection = wallConditions.Wall.SpawnOrientation.ToDirection();
           
            var wallInBetween = SpawnWallBasedOnCorner(newCorner, spawnOrientation, spawnDirection, wallInBetweenType);
            wallInBetween.ConnectElementByDirection(newFloor, wallInBetween.SpawnOrientation.Opposite().ToDirection());
            wallInBetween.ConnectElementByDirection(wallInBetweenNeighbor, spawnDirection);
            
        }

        // Standard case move full wall corner
        private static RoomElement MoveCornerSpawnFullWall(WallConditions wallConditions, Vector3 movementDelta,
            bool clockwise)
        {
            var corner = wallConditions.GetElement(0, clockwise);
            Undo.RecordObject(corner.transform, "");
            Undo.RecordObject(corner, "");
            corner.transform.position += movementDelta;
            var spawnOrientation = wallConditions.GetDirection(0, clockwise).ToSpawnOrientation();
            var spawnDirection = wallConditions.Wall.SpawnOrientation.Opposite().ToDirection();
            return SpawnWallBasedOnCorner(corner, spawnOrientation, spawnDirection, RoomElementType.Wall);
        }

        // Standard case move full wall no corner
        private static void Spawn2CornerAnd2ShortWalls(WallConditions wallConditions, RoomElement newFloor, bool clockwise)
        {
            // Get old neighbor of moved wall (should be wall type)
            var oldWall = wallConditions.GetElement(0, clockwise);

            // Shrink oldWall because of second corner
            var (_, newCorner2) = ShrinkWall(oldWall, true, clockwise);

            // Spawn shortened wall next to newCorner1 and connect shortened Wall to corner1
            var wallType = clockwise ? RoomElementType.WallShortenedRight : RoomElementType.WallShortenedLeft;
            var spawnOrientation = wallConditions.GetDirection(0, clockwise).ToSpawnOrientation();
            var spawnDirection = wallConditions.Wall.SpawnOrientation.ToDirection();
            var newShortWall = SpawnWallBasedOnCorner(newCorner2, spawnOrientation, spawnDirection, wallType);

            // Connect new short wall to new floor
            newShortWall.ConnectElementByDirection(newFloor, newShortWall.SpawnOrientation.Opposite().ToDirection());

            // Spawn Corner next to moved Wall and connect to moved wall
            var newCorner1 = SpawnCornerNextToWall(wallConditions.Wall, clockwise);

            // Connect newCorner1 to newShortWall
            newCorner1.ConnectElementByDirection(newShortWall, wallConditions.Wall.SpawnOrientation.ToDirection().Opposite());
        }

        private static void HandleCornerNextToShorterWall(WallConditions wallConditions, RoomElement newFloor,
            Vector3 movementDelta, bool clockwise)
        {
            var corner = wallConditions.GetElement(0, clockwise);
            Undo.RecordObject(corner, "");

            //get other wall connected to corner, when found shorter wall, then delete corner otherwise move corner
            var otherWallConnectedToCorner = wallConditions.GetElement(1, clockwise);
            var otherWallConnectedToCornerDirection = wallConditions.GetDirection(1, clockwise);

            //The room extends further next to moved wall; it isn't a 'normal' outer corner
            if (otherWallConnectedToCornerDirection == wallConditions.Wall.SpawnOrientation.ToDirection())
            {
                Undo.RecordObject(otherWallConnectedToCorner, ""); //store bindings

                //Connect newFloor to floorNextToOtherWallConnectedToCorner
                var otherWallDirection = otherWallConnectedToCorner.SpawnOrientation.ToDirection();

                var floorNextToOtherWallConnectedToCorner =
                    otherWallConnectedToCorner.GetRoomElementByDirection(otherWallDirection.Opposite());
                Undo.RecordObject(floorNextToOtherWallConnectedToCorner, "");

                floorNextToOtherWallConnectedToCorner.ConnectElementByDirection(newFloor, otherWallDirection);


                if (wallConditions.WallWasReplaced)
                {
                    //delete corner (next to otherWallConnectedToCorner)
                    var unnecessaryCorner = wallConditions.GetElement(2, clockwise);
                    Undo.RecordObject(unnecessaryCorner, "");
                    var newNeighbor = wallConditions.GetElement(3, clockwise);
                    Undo.RecordObject(newNeighbor, "");
                    wallConditions.Wall.ConnectElementByDirection(newNeighbor,
                        wallConditions.GetDirection(0, clockwise));
                    unnecessaryCorner.DisconnectFromAllNeighbors();
                    Undo.DestroyObjectImmediate(unnecessaryCorner.gameObject);

                    //delete corner (next to wallToMove)
                    corner.DisconnectFromAllNeighbors();
                    Undo.DestroyObjectImmediate(corner.gameObject);

                    //delete otherWallConnectedToCorner (next to corner)
                    otherWallConnectedToCorner.DisconnectFromAllNeighbors();
                    Undo.DestroyObjectImmediate(otherWallConnectedToCorner.gameObject);

                    return;
                }

                var wallAfterOtherWallConnectedToCorner = wallConditions.GetElement(2, clockwise);
                Undo.RecordObject(wallAfterOtherWallConnectedToCorner, "");

                var neighbor = wallConditions.GetElement(3, clockwise);
                otherWallConnectedToCorner.ConnectElementByDirection(neighbor,
                    wallConditions.GetDirection(2, clockwise));
                var directionOfFirstElement = wallConditions.GetDirection(0, clockwise);
                neighbor = wallAfterOtherWallConnectedToCorner.GetRoomElementByDirection(directionOfFirstElement);
                otherWallConnectedToCorner.ConnectElementByDirection(neighbor, directionOfFirstElement);

                Undo.DestroyObjectImmediate(wallAfterOtherWallConnectedToCorner.gameObject);

                Undo.RecordObject(otherWallConnectedToCorner.transform, "");
                otherWallConnectedToCorner.transform.position += movementDelta;
                Undo.RecordObject(corner.transform, "");
                corner.transform.position += movementDelta;

                return;
            }

            var newWall = MoveCornerSpawnFullWall(wallConditions, movementDelta, clockwise);
            newFloor.ConnectElementByDirection(newWall, newWall.SpawnOrientation.ToDirection());
        }

        #endregion

        #region One Element

        private static RoomElement SpawnCornerNextToWall(RoomElement wall, bool clockwise)
        {
            var shiftValue = clockwise ? 1 : -1;
            var wallToMoveDirection = wall.SpawnOrientation.ToDirection();
            var spawnDirection = wallToMoveDirection.Shift(shiftValue);

            var spawnerList = wall.ExtendableRoom.ElementSpawner;
            var cornerSpawner = spawnerList[(int) RoomElementType.Corner];
            var cornerOrientation = GetCornerOrientationBasedOnWall(wall, spawnDirection);
            var newCorner = cornerSpawner.SpawnNextToRoomElement(wall, spawnDirection, cornerOrientation);
            Undo.RegisterCreatedObjectUndo(newCorner.gameObject, "");
            Undo.RecordObject(wall, "");

            wall.ConnectElementByDirection(newCorner, spawnDirection);

            return newCorner;
        }

        private static RoomElement SpawnWallBasedOnCorner(RoomElement corner, SpawnOrientation spawnOrientation, Direction spawnDirection, RoomElementType spawnerType)
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
            return newWall;
        }

        private static (RoomElement newFloor, RoomElement collidingFloor) SpawnFloorNextToMovedWall(RoomElement wallToMove)
        {
            var wallDirection = wallToMove.SpawnOrientation.ToDirection();
            var spawnList = wallToMove.ExtendableRoom.ElementSpawner;
            var floorSpawner = spawnList[(int) RoomElementType.Floor];

            // Get The old floor which was connected to the wall
            var oldFloor = wallToMove.GetRoomElementByDirection(wallDirection.Opposite());
            var newGridPosition = GetGridPosition(oldFloor, wallDirection);
            Undo.RecordObject(oldFloor, "");
            // Old floor is used to spawn the new floor because otherwise there could be offset problems if the wall would be a shortened one
            var newFloor = floorSpawner.SpawnNextToRoomElement(oldFloor, wallDirection, SpawnOrientation.Front);
            Undo.RegisterCreatedObjectUndo(newFloor.gameObject, "");
            ((FloorElement) newFloor).GridPosition = newGridPosition;
            Undo.RecordObject(wallToMove.ExtendableRoom, "");

            var floorGridDictionary = wallToMove.ExtendableRoom.FloorGridDictionary;
            floorGridDictionary.Add(newGridPosition, newFloor as FloorElement);

            //check if there is a collision
            newGridPosition = GetGridPosition(newFloor, wallDirection);

            //Connect elements
            oldFloor.ConnectElementByDirection(newFloor, wallDirection);

            //TODO Check for diagonal floor collision
            if (floorGridDictionary.ContainsKey(newGridPosition))
            {
                var collidingFloor = floorGridDictionary[newGridPosition];
                return (newFloor, collidingFloor);
            }

            newFloor.ConnectElementByDirection(wallToMove, wallDirection);

            return (newFloor, null);
        }

        #endregion

        #endregion

        #region Get Information Around RoomElements

        private static bool HasCornerAndAnotherCornerInDirection(WallConditions wallConditions, bool clockwise)
        {
            var adjacentWallElement = wallConditions.GetElement(0, clockwise);

            if (!adjacentWallElement.Type.IsCornerType() || !IsInnerCorner(adjacentWallElement, wallConditions.Wall))
            {
                return false;
            }

            // If we have a double corner, the element after the corner which is always should be a wall should have a corner thereafter
            return wallConditions.GetElement(2, clockwise).Type.IsCornerType();
        }

        private static bool ShorterWallNeedsToBeReplacedThroughFullWall(WallConditions wallConditions)
        {
            return HasCornerAndAnotherCornerInDirection(wallConditions, true) ||
                   HasCornerAndAnotherCornerInDirection(wallConditions, false);
        }


        private static bool IsInnerCorner(RoomElement corner, RoomElement adjacentWall)
        {
            return corner.GetRoomElementByDirection(adjacentWall.SpawnOrientation.ToDirection()) != null;
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

        private static (RoomElement roomElement, Direction directionOfElementBasedOnPredecessor)[] ExtractElementsNextToWall(RoomElement wall, int elementsToExtract, bool clockwise)
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

            //TODO Other cases not supported
            Debug.LogError(
                $"Not supported wall orientation {wall.SpawnOrientation} and corner in direction {direction}");
            return wall.SpawnOrientation.Shift(1);
        }

        private static Vector2Int GetGridPosition(RoomElement oldFloor, Direction spawnDirection)
        {
            var oldFloorElement = oldFloor as FloorElement;

            if (oldFloorElement == null)
            {
                throw new Exception("GetGridPosition expects floorElement as parameter!");
            }

            Vector2Int oldFloorGridPosition = oldFloorElement.GridPosition;

            switch (spawnDirection)
            {
                case Direction.Front:
                    return oldFloorGridPosition + Vector2Int.up;
                case Direction.Right:
                    return oldFloorGridPosition + Vector2Int.right;
                case Direction.Back:
                    return oldFloorGridPosition + Vector2Int.down;
                case Direction.Left:
                    return oldFloorGridPosition + Vector2Int.left;
                default:
                    throw new ArgumentOutOfRangeException(nameof(spawnDirection), spawnDirection,
                        "invalid spawnDirection");
            }
        }

        private static Vector3 GetWallPositionBasedOnShorterWall(RoomElement wallToMove)
        {
            var spawnerList = wallToMove.ExtendableRoom.ElementSpawner;
            var newWallSpawner = spawnerList[(int) RoomElementType.Wall];
            var currentPosition = wallToMove.transform.position;

            //Calc wall bound diff from full to short
            var shortWallSpawner = spawnerList[(int) RoomElementType.WallShortenedLeft];
            var diff = newWallSpawner.Bounds.extents.x - shortWallSpawner.Bounds.extents.x;

            //TODO: Improve factor selection (low priority)
            var factor = wallToMove.Type == RoomElementType.WallShortenedLeft ? -1 : 1; //pos or negative
            factor = wallToMove.SpawnOrientation.TowardsNegative() ? factor : -factor;

            //which axis
            if (wallToMove.SpawnOrientation.IsSideways()) //z
            {
                currentPosition.z += diff * factor;
            }
            else //x
            {
                currentPosition.x -= diff * factor;
            }

            return currentPosition;
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
                (RoomElement, Direction)[] counterClockwiseElements,
                CornerSituation cornerSituation)
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