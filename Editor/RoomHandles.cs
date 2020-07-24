using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityLevelEditor.Model;
using UnityEditor.EditorTools;
using Object = UnityEngine.Object;

namespace UnityLevelEditor.RoomExtension
{
    [EditorTool("RoomExtension")]
    public class RoomHandles : EditorTool
    {
        #region Inspector Fields

        [SerializeField] private Texture2D toolIcon;

        [SerializeField] private Texture2D toolIconActive;

        [SerializeField] private string text = "Room Extension Tool";

        [SerializeField] private string tooltip = "Room Extension Tool";

        #endregion

        #region IconHandling

        private GUIContent iconContent;
        public override GUIContent toolbarIcon => iconContent;

        void OnEnable()
        {
            EditorTools.activeToolChanged += ChangeIcon;
            ChangeIcon();
            Debug.Log("Enable Tool");
        }

        private void OnDisable()
        {
            Debug.Log("Disable");
            EditorTools.activeToolChanged -= ChangeIcon;
        }

        private void ChangeIcon()
        {
            //Debug.Log("Changed Tool");
            Texture2D icon = EditorTools.IsActiveTool(this) ? toolIconActive : toolIcon;

            iconContent = new GUIContent()
            {
                image = icon,
                text = text,
                tooltip = tooltip
            };
        }

        #endregion

        #region Handles

        public override bool IsAvailable()
        {
            if (!(target is GameObject t))
            {
                return false;
            }

            var roomElement = t.GetComponent<RoomElement>();
            return (roomElement != null && roomElement.Type.IsWallType());
        }


        public override void OnToolGUI(EditorWindow window)
        {
            if (!TryGetSelectedWallsAndUpdateSelection(out var selectedWalls))
            {
                return;
            }

            EditorGUI.BeginChangeCheck();

            Vector3 position = Tools.handlePosition;
            var representativeWall = selectedWalls[0];


            switch (representativeWall.SpawnOrientation)
            {
                case SpawnOrientation.Back:
                    position = DrawHandle(position, Vector3.back, Color.blue);
                    break;
                case SpawnOrientation.Front:
                    position = DrawHandle(position, Vector3.forward, Color.blue);
                    break;
                case SpawnOrientation.Left:
                    position = DrawHandle(position, Vector3.left, Color.red);
                    break;
                case SpawnOrientation.Right:
                    position = DrawHandle(position, Vector3.right, Color.red);
                    break;
            }

            if (EditorGUI.EndChangeCheck())
            {
                var snapValue = representativeWall.ExtendableRoom.ElementSpawner[(int) RoomElementTyp.Floor].Bounds.size
                    .x;
                var movementDelta = SnapVectorXZ(position - Tools.handlePosition, snapValue);
                if (Mathf.Abs(movementDelta.x) < 0.01f && Mathf.Abs(movementDelta.z) < 0.01f)
                {
                    return;
                }

                ExtendTheRoom(selectedWalls, movementDelta);
            }
        }

        private Vector3 DrawHandle(Vector3 position, Vector3 direction, Color color)
        {
            using (new Handles.DrawingScope(color))
            {
                return Handles.Slider(position, direction, HandleUtility.GetHandleSize(position),
                    Handles.ArrowHandleCap, 1f);
            }
        }

        #endregion

        #region RoomExtending

        private static SpawnOrientation GetCornerOrientationBasedOnWall(RoomElement wall, Direction direction)
        {
            if (!wall.Type.IsWallType())
            {
                throw new Exception("Fail");
            }

            if (wall.Type == RoomElementTyp.WallShortenedRight)
            {
                return wall.SpawnOrientation.Shift(-1); //-90 rotation of corner
            }

            if (wall.Type == RoomElementTyp.WallShortenedLeft)
            {
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

        private Vector3 SnapVectorXZ(Vector3 vector, float snapValue)
        {
            vector.x = Mathf.Round(vector.x / snapValue) * snapValue;
            vector.z = Mathf.Round(vector.z / snapValue) * snapValue;
            return vector;
        }

        private void ExtendTheRoom(List<RoomElement> walls, Vector3 movementDelta)
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
            else if (wallToMove.Type == RoomElementTyp.WallShortenedBothEnds)
            {
                //TODO
                Debug.LogWarning("Extending wall shortened on both ends not supported yet");
            }
            else
            {
                ExtendFullWall(wallToMove, wallConditions, movementDelta);
            }

            Undo.CollapseUndoOperations(undoGroupId);
        }

        private void ExtendFullWall(RoomElement wallToMove, WallConditions wallConditions, Vector3 movementDelta)
        {
            var (newFloor, collision) = SpawnFloorNextToMovedWall(wallToMove);

            if (collision)
            {
                //get old corners and record them
                var oldNeighbor1 = wallConditions.GetElement(0, true);
                var oldNeighbor2 = wallConditions.GetElement(0, false);
                Undo.RecordObject(oldNeighbor1, "");
                Undo.RecordObject(oldNeighbor2, "");
                //Debug.Log("Found: " + oldNeighbor1.Type +" " + oldNeighbor2.Type);
                //if type is corner, record also wall neighbors  --------> did not fix the Missing bug
                if (oldNeighbor1.Type.IsCornerType())
                {
                    var oldNeighborNeighbor =
                        oldNeighbor1.GetRoomElementByDirection(wallToMove.SpawnOrientation.ToDirection()
                            .Opposite());
                    Undo.RecordObject(oldNeighborNeighbor, "");
                }

                if (oldNeighbor2.Type.IsCornerType())
                {
                    var oldNeighborNeighbor2 =
                        oldNeighbor2.GetRoomElementByDirection(wallToMove.SpawnOrientation.ToDirection()
                            .Opposite());
                    Undo.RecordObject(oldNeighborNeighbor2, "");
                }
//spawn wallShortened One Side
                if ((wallConditions.CornerSituation & CornerSituation.Clockwise) == CornerSituation.Clockwise)
                {
                    var (newShortWall, newCorner) =
                        SpawnShortWallAndCornerOnCollision(wallToMove, wallConditions, true, newFloor);  
                }
                else
                {
             
                    
                }


                if ((wallConditions.CornerSituation & CornerSituation.CounterClockwise) == CornerSituation.CounterClockwise)
                {
                    var (newShortWall2, newCorner2) =
                        SpawnShortWallAndCornerOnCollision(wallToMove, wallConditions, false, newFloor);
                }
                else
                {
                    
                    
                }

                //delete old corners
                if (oldNeighbor1.Type.IsCornerType() && oldNeighbor2.Type.IsCornerType())
                {
                    oldNeighbor1.DisconnectFromAllNeighbors();
                    oldNeighbor2.DisconnectFromAllNeighbors();
                    Undo.DestroyObjectImmediate(oldNeighbor1.gameObject);
                    Undo.DestroyObjectImmediate(oldNeighbor2.gameObject);
                }

                //spawn 4 corners & 2 wallsShortenedBothEnds


                //delete walltoMove
                wallToMove.DisconnectFromAllNeighbors();
                Undo.DestroyObjectImmediate(wallToMove.gameObject);

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
                SpawnCornerAndShorterWall(wallConditions, newFloor, false);
            }

            if ((wallConditions.CornerSituation & CornerSituation.Clockwise) ==
                CornerSituation.Clockwise)
            {
                var newWall = MoveCornerSpawnFullWall(wallConditions, movementDelta, true);
                newFloor.ConnectElementByDirection(newWall, newWall.SpawnOrientation.ToDirection());
            }
            else
            {
                SpawnCornerAndShorterWall(wallConditions, newFloor, true);
            }
        }

        private void ExtendShorterWall(WallConditions wallConditions, Vector3 movementDelta)
        {
            var wallNeedsToBePlaced = ShorterWallNeedsToBeReplacedThroughFullWall(wallConditions);

            var (newFloor, collision) = SpawnFloorNextToMovedWall(wallConditions.Wall);

            if (wallNeedsToBePlaced)
            {
                wallConditions.Wall = ReplaceShortenedWallThroughFullWall(wallConditions.Wall);
            }

            if ((wallConditions.CornerSituation & CornerSituation.CounterClockwise) ==
                CornerSituation.CounterClockwise)
            {
                HandleCornerNextToShorterWall(wallConditions, newFloor, movementDelta, false);
            }
            else
            {
                SpawnCornerAndShorterWall(wallConditions, newFloor, false);
            }

            if ((wallConditions.CornerSituation & CornerSituation.Clockwise) ==
                CornerSituation.Clockwise)
            {
                HandleCornerNextToShorterWall(wallConditions, newFloor, movementDelta, true);
            }
            else
            {
                SpawnCornerAndShorterWall(wallConditions, newFloor, true);
            }
        }

        private void HandleCornerNextToShorterWall(WallConditions wallConditions, RoomElement newFloor,
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
                Undo.RecordObject(otherWallConnectedToCorner, "");
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

        private static bool ShorterWallNeedsToBeReplacedThroughFullWall(WallConditions wallConditions)
        {
            return HasCornerAndAnotherCornerInDirection(wallConditions, true) ||
                   HasCornerAndAnotherCornerInDirection(wallConditions, false);
        }

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

        private static bool IsInnerCorner(RoomElement corner, RoomElement adjacentWall)
        {
            return corner.GetRoomElementByDirection(adjacentWall.SpawnOrientation.ToDirection()) != null;
        }

        private static (RoomElement, bool) SpawnFloorNextToMovedWall(RoomElement wallToMove)
        {
            var wallDirection = wallToMove.SpawnOrientation.ToDirection();
            var spawnList = wallToMove.ExtendableRoom.ElementSpawner;
            var floorSpawner = spawnList[(int) RoomElementTyp.Floor];

            // Get The old floor which was connected to the wall
            var oldFloor = wallToMove.GetRoomElementByDirection(wallDirection.Opposite());
            var newGridPosition = GetGridPosition(oldFloor, wallDirection);
            Undo.RecordObject(oldFloor, "");
            // Old floor is used to spawn the new floor because otherwise there could be offset problems if the wall would be a shortened one
            var newFloor = floorSpawner.SpawnNextToRoomElement(oldFloor, wallDirection, SpawnOrientation.Front);
            ((FloorElement) newFloor).GridPosition = newGridPosition;
            Undo.RecordObject(wallToMove.ExtendableRoom, "");
            wallToMove.ExtendableRoom.FloorGridDictionary.Add(newGridPosition, newFloor as FloorElement);
            Undo.RegisterCreatedObjectUndo(newFloor.gameObject, "");

            //check if there is a collision
            newGridPosition = GetGridPosition(newFloor, wallDirection);
            var collision = wallToMove.ExtendableRoom.FloorGridDictionary.ContainsKey(newGridPosition);

            //Connect elements
            oldFloor.ConnectElementByDirection(newFloor, wallDirection);

            if (collision)
            {
                //CHeck collision elements
                var collisionFloor = wallToMove.ExtendableRoom.FloorGridDictionary[newGridPosition];
                var collisionObject =
                    collisionFloor.GetRoomElementByDirection(wallToMove.SpawnOrientation.ToDirection().Opposite());
                if (collisionObject.Type.IsWallType())
                {
                    //delete old wall first - could be also different object
                    collisionObject.DisconnectFromAllNeighbors();
                    Undo.DestroyObjectImmediate(collisionObject.gameObject);
                }
                else
                {
                    Debug.LogWarning("type not handled yet");
                }

                //floor connection
                collisionFloor.ConnectElementByDirection(newFloor, wallDirection.Opposite());
            }
            else
            {
                newFloor.ConnectElementByDirection(wallToMove, wallDirection);
            }

            return (newFloor, collision);
        }

        private RoomElement ReplaceShortenedWallThroughFullWall(RoomElement wallToMove)
        {
            var spawnerList = wallToMove.ExtendableRoom.ElementSpawner;
            //replace this wall and delete corner
            var newWallSpawner = spawnerList[(int) RoomElementTyp.Wall];
            var newPosition = GetWallPositionBasedOnShorterWall(wallToMove);
            var (newWall, _) = newWallSpawner.SpawnByCenterPosition(newPosition, wallToMove.SpawnOrientation,
                wallToMove.ExtendableRoom, "");
            Undo.RegisterCreatedObjectUndo(newWall.gameObject, "");
            newWall.CopyNeighbors(wallToMove);
            Undo.DestroyObjectImmediate(wallToMove.gameObject);
            return newWall;
        }

        private RoomElement SpawnWallBasedOnCorner(RoomElement corner, RoomElement wallToMove,
            RoomElementTyp spawnerType, bool clockwise)
        {
            var factor = clockwise ? 1 : -1;
            var spawnerList = wallToMove.ExtendableRoom.ElementSpawner;
            var spawnOrientation = wallToMove.SpawnOrientation.Shift(factor);
            var spawnDirection = spawnOrientation.ToDirection().Shift(factor);
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

        private (RoomElement newCorner, RoomElement shorterWall) SpawnCornerNextToWallWithWallNeighbor(
            RoomElement wallToMove,
            Direction direction, int factor)
        {
            var spawnerList = wallToMove.ExtendableRoom.ElementSpawner;

            var newCorner = SpawnCornerNextToWall(wallToMove, direction);

            var oldWall = wallToMove.GetRoomElementByDirection(direction);

            RoomElement shorterWall = null;

            if (oldWall != null)
            {
                Undo.RecordObject(oldWall, "");
                RoomElementTyp wallType;
                if (oldWall.Type == RoomElementTyp.Wall || oldWall.Type == RoomElementTyp.WallTransparent)
                {
                    wallType = factor == 1
                        ? RoomElementTyp.WallShortenedLeft
                        : RoomElementTyp.WallShortenedRight;
                }
                else
                {
                    wallType = RoomElementTyp.WallShortenedBothEnds;
                }

                var oldWallNeighbourInDirection = oldWall.GetRoomElementByDirection(direction);
                Undo.RecordObject(oldWallNeighbourInDirection, "");
                var wallSpawner = spawnerList[(int) wallType];
                shorterWall = wallSpawner.SpawnNextToRoomElement(oldWallNeighbourInDirection, direction.Opposite(),
                    wallToMove.SpawnOrientation);
                Undo.RegisterCreatedObjectUndo(shorterWall.gameObject, "");
                shorterWall.CopyNeighbors(oldWall);
                wallToMove.ConnectElementByDirection(null, direction);
                oldWall.DisconnectFromAllNeighbors();
                Undo.DestroyObjectImmediate(oldWall.gameObject);
            }

            wallToMove.ConnectElementByDirection(newCorner, direction);

            return (newCorner, shorterWall);
        }

        private RoomElement SpawnCornerNextToWall(RoomElement wallToMove, Direction direction)
        {
            var spawnerList = wallToMove.ExtendableRoom.ElementSpawner;
            var cornerSpawner = spawnerList[(int) RoomElementTyp.Corner];
            var newCorner = cornerSpawner.SpawnNextToRoomElement(wallToMove, direction,
                GetCornerOrientationBasedOnWall(wallToMove, direction));
            Undo.RegisterCreatedObjectUndo(newCorner.gameObject, "");
            Undo.RecordObject(wallToMove, "");
            return newCorner;
        }


        private WallConditions GetWallConditions(RoomElement wall, int elementsToExtractPerDirection = 5)
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

        #region utilitys

        private static (RoomElement, RoomElement) SpawnShortWallAndCornerOnCollision(RoomElement wallToMove,
            WallConditions wallConditions,
            bool clockwise, RoomElement newFloor)
        {
            var cornerNeighbor = wallConditions.GetElement(1, clockwise);
            Undo.RecordObject(cornerNeighbor, "");
            RoomElementTyp roomElementTyp;
            var spawnOrientation = wallConditions.GetDirection(0, clockwise).ToSpawnOrientation();
            if (cornerNeighbor.Type.IsCornerType())
            {
                roomElementTyp = RoomElementTyp.WallShortenedBothEnds;
            }
            else
            {
                roomElementTyp = clockwise ? RoomElementTyp.WallShortenedLeft : RoomElementTyp.WallShortenedRight;
            }

            var wallSpawner = wallToMove.ExtendableRoom.ElementSpawner[(int) roomElementTyp];
            var cornerSpawner = wallToMove.ExtendableRoom.ElementSpawner[(int) RoomElementTyp.Corner];
            var wallToMoveDirection = wallToMove.SpawnOrientation.ToDirection();

            RoomElement newWall;

            //get neighbor on collision side
            var neighbor = newFloor.GetRoomElementByDirection(wallToMoveDirection); //floor element on collision side
            var nextToBehindCollisionElement =
                neighbor.GetRoomElementByDirection(wallConditions.GetDirection(0, clockwise)); //other Floor or Wall
            neighbor =
                nextToBehindCollisionElement.GetRoomElementByDirection(wallToMoveDirection.Opposite()); //Wall or Corner
            Undo.RecordObject(neighbor, "");
            //new to handle replacement first
            ElementSpawner customSpawner;
            if (neighbor.Type.IsWallType()) //if wall > wall shortened one side
            {
                //Debug.Log("Called");
                
                customSpawner = clockwise
                    ? wallToMove.ExtendableRoom.ElementSpawner[(int) RoomElementTyp.WallShortenedLeft]
                    : wallToMove.ExtendableRoom.ElementSpawner[(int) RoomElementTyp.WallShortenedRight];

                newWall =
                    customSpawner.SpawnNextToRoomElement(cornerNeighbor, wallToMoveDirection, spawnOrientation);
                Undo.RegisterCreatedObjectUndo(newWall.gameObject, "");
                cornerNeighbor.ConnectElementByDirection(newWall, wallToMoveDirection);
                newWall.ConnectElementByDirection(newFloor, spawnOrientation.ToDirection().Opposite());

                var newCorner = cornerSpawner.SpawnNextToRoomElement(newWall, wallToMoveDirection
                    , GetCornerOrientationBasedOnWall(newWall, wallToMoveDirection));
                Undo.RegisterCreatedObjectUndo(newCorner.gameObject, "");

                //Connect new Corner
                newWall.ConnectElementByDirection(newCorner, wallToMoveDirection);
                //neighbor.ConnectElementByDirection(newCorner, wallConditions.GetDirection(0, !clockwise));

                //Handle collision side wall
                if (neighbor.Type.IsShortenedWall()) //if shortened Wall > wall shortened both side
                {
                    customSpawner =
                        wallToMove.ExtendableRoom.ElementSpawner[(int) RoomElementTyp.WallShortenedBothEnds];
                }
                else
                {
                    customSpawner = clockwise
                        ? wallToMove.ExtendableRoom.ElementSpawner[(int) RoomElementTyp.WallShortenedLeft]
                        : wallToMove.ExtendableRoom.ElementSpawner[(int) RoomElementTyp.WallShortenedRight];
                }

                var oppositeSpawningDirection = wallConditions.GetDirection(0, clockwise);
                var spawningElement = neighbor.GetRoomElementByDirection(oppositeSpawningDirection);
                var replacedWall =
                    customSpawner.SpawnNextToRoomElement(spawningElement, oppositeSpawningDirection.Opposite(),
                        wallToMove.SpawnOrientation.Opposite());

                spawningElement.ConnectElementByDirection(replacedWall, oppositeSpawningDirection.Opposite());
                replacedWall.ConnectElementByDirection(nextToBehindCollisionElement, wallToMoveDirection);
                replacedWall.ConnectElementByDirection(newCorner, oppositeSpawningDirection.Opposite());

                neighbor.DisconnectFromAllNeighbors();
                Undo.DestroyObjectImmediate(neighbor.gameObject);

                return (newWall, newCorner);
            }
            else if (neighbor.Type.IsCornerType()) //if corner > delete
            {
                customSpawner = wallToMove.ExtendableRoom.ElementSpawner[(int) RoomElementTyp.Wall];
                var behindNeighbor = neighbor.GetRoomElementByDirection(wallToMoveDirection);
                Undo.RecordObject(behindNeighbor, "");
                newWall = customSpawner.SpawnNextToRoomElement(behindNeighbor, wallToMoveDirection.Opposite(),
                    behindNeighbor.SpawnOrientation);
                Undo.RegisterCreatedObjectUndo(newWall.gameObject, "");

                behindNeighbor.ConnectElementByDirection(newWall, wallToMoveDirection.Opposite());
                newWall.ConnectElementByDirection(cornerNeighbor, wallToMoveDirection.Opposite());
                newWall.ConnectElementByDirection(newFloor, newWall.SpawnOrientation.ToDirection().Opposite());

                neighbor.DisconnectFromAllNeighbors();
                Undo.DestroyObjectImmediate(neighbor.gameObject);
                return (newWall, null);
            }

            Debug.LogError("not supported cases");
            return (null, null);
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

        private Vector3 GetWallPositionBasedOnShorterWall(RoomElement wallToMove)
        {
            var spawnerList = wallToMove.ExtendableRoom.ElementSpawner;
            var newWallSpawner = spawnerList[(int) RoomElementTyp.Wall];
            var currentPosition = wallToMove.transform.position;

            //Calc wall bound diff from full to short
            var shortWallSpawner = spawnerList[(int) RoomElementTyp.WallShortenedLeft];
            var diff = newWallSpawner.Bounds.extents.x - shortWallSpawner.Bounds.extents.x;

            //TODO: Improve factor selection (low priority)
            var factor = wallToMove.Type == RoomElementTyp.WallShortenedLeft ? -1 : 1; //pos or negative
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

        private RoomElement MoveCornerSpawnFullWall(WallConditions wallConditions, Vector3 movementDelta,
            bool clockwise)
        {
            var corner = wallConditions.GetElement(0, clockwise);
            Undo.RecordObject(corner.transform, "");
            Undo.RecordObject(corner, "");
            corner.transform.position += movementDelta;
            return SpawnWallBasedOnCorner(corner, wallConditions.Wall, RoomElementTyp.Wall, clockwise);
        }

        private void SpawnCornerAndShorterWall(WallConditions wallConditions, RoomElement newFloor, bool clockwise)
        {
            var shiftDirection = clockwise ? 1 : -1;
            var (newCorner, shorterWall) = SpawnCornerNextToWallWithWallNeighbor(wallConditions.Wall,
                wallConditions.GetDirection(0, clockwise), shiftDirection);
            var wallType = clockwise ? RoomElementTyp.WallShortenedRight : RoomElementTyp.WallShortenedLeft;
            var newWall = SpawnWallBasedOnCorner(newCorner, wallConditions.Wall, wallType, clockwise);
            if (newFloor != null)
            {
                newFloor.ConnectElementByDirection(newWall, newWall.SpawnOrientation.ToDirection());
            }

            var (newCorner2, _) = SpawnCornerNextToWallWithWallNeighbor(newWall,
                newWall.SpawnOrientation.ToDirection().Shift(shiftDirection), shiftDirection);
            newCorner2.ConnectElementByDirection(shorterWall, wallConditions.GetDirection(0, clockwise));
        }

        #endregion

        #endregion

        #region SelectionHandling

        private bool TryGetSelectedWallsAndUpdateSelection(out List<RoomElement> selectedWalls)
        {
            selectedWalls = null;

            if (!TryGetSelectedGameObject(out var selectedGameObject))
            {
                return false;
            }

            var selectedRoomElement = selectedGameObject.GetComponent<RoomElement>();

            if (selectedRoomElement == null)
            {
                RemoveRoomElementsFromSelection();
                return false;
            }

            var roomElementsOfActiveType =
                FilterSelectionForRoomElementsOfGivenTypeAndOrientation(selectedRoomElement.Type,
                    selectedRoomElement.SpawnOrientation);

            Selection.objects =
                roomElementsOfActiveType.Select(roomElement => roomElement.gameObject).ToArray<Object>();

            if (!selectedRoomElement.Type.IsWallType())
            {
                return false;
            }

            selectedWalls = roomElementsOfActiveType;
            return true;
        }

        private bool TryGetSelectedGameObject(out GameObject selectedGameObject)
        {
            selectedGameObject = null;

            if (Selection.objects.Length <= 0)
            {
                return false;
            }

            var mostRecentSelected = Selection.objects[Selection.objects.Length - 1];

            if (mostRecentSelected is GameObject gameObject)
            {
                selectedGameObject = gameObject;
                return true;
            }

            return false;
        }

        private void RemoveRoomElementsFromSelection()
        {
            Selection.objects = Selection.gameObjects.Where(go => go.GetComponent<RoomElement>() == null)
                .ToArray<Object>();
        }

        private List<RoomElement> FilterSelectionForRoomElementsOfGivenTypeAndOrientation(RoomElementTyp type,
            SpawnOrientation orientation)
        {
            return Selection.transforms.Select(t => t.GetComponent<RoomElement>())
                .Where(r => r != null && r.Type == type && r.SpawnOrientation == orientation).ToList();
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

            private (RoomElement roomElement, Direction directionOfElementBasedOnPredecessor)[] ClockwiseElements
            {
                get;
            }

            private (RoomElement roomElement, Direction directionOfElementBasedOnPredecessor)[] CounterClockwiseElements
            {
                get;
            }

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