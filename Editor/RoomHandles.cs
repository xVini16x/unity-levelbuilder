using System;
using System.Collections.Generic;
using System.Linq;
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

        private SpawnOrientation GetCornerOrientationBasedOnWall(RoomElement wall, Direction direction)
        {
            if (!wall.Type.IsWallType())
            {
                throw new Exception("Fail");
            }

            if (wall.Type == RoomElementTyp.WallShortenedRight)
            {
                return wall.SpawnOrientation.Shift(-1); //1-6 wand -90 drehung der corner
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
            Debug.Log("Extend room");
            //Debug.Log("undo group id " + undoGroupId);

            var wallConditions = GetWallConditions(wallToMove);

            Undo.RecordObject(wallToMove.transform, "");
            Undo.RecordObject(wallToMove, "");
            wallToMove.transform.position += movementDelta;


            //Prevent spawning double walls, shorter wall is only spawned when there is already an additional way
            if (wallToMove.Type.IsShortenedWall())
            {
                Debug.Log("Extend shorter wall");
                ExtendShorterWall(wallToMove, wallConditions, movementDelta);
            }
            else if (wallToMove.Type == RoomElementTyp.WallShortenedBothEnds)
            {
                //TODO
                Debug.LogWarning("Extending wall shortened on both ends not supported yet");
            }
            else
            {
                Debug.Log("Extend Full wall");
                ExtendFullWall(wallToMove, wallConditions, movementDelta);
            }

            Undo.CollapseUndoOperations(undoGroupId);
        }

        private void ExtendFullWall(RoomElement wallToMove, WallConditions wallConditions, Vector3 movementDelta)
        {
            var newFloor = SpawnFloorNextToMovedWall(wallToMove);

            if ((wallConditions.WallExtensionScenario & WallExtensionScenario.CornerCounterClockwise) ==
                WallExtensionScenario.CornerCounterClockwise)
            {
                var corner = wallConditions.CounterClockwiseElement;
                var newWall = MoveCornerSpawnFullWall(corner, wallToMove, movementDelta, false);
                newFloor.ConnectElementByDirection(newWall, newWall.SpawnOrientation.ToDirection());
            }
            else
            {
                SpawnCornerAndShorterWall(wallToMove, newFloor, false);
            }

            if ((wallConditions.WallExtensionScenario & WallExtensionScenario.CornerClockwise) ==
                WallExtensionScenario.CornerClockwise)
            {
                var corner = wallConditions.ClockwiseElement;
                var newWall = MoveCornerSpawnFullWall(corner, wallToMove, movementDelta, true);
                newFloor.ConnectElementByDirection(newWall, newWall.SpawnOrientation.ToDirection());
            }
            else
            {
                SpawnCornerAndShorterWall(wallToMove, newFloor, true);
            }
        }

        private void ExtendShorterWall(RoomElement wallToMove, WallConditions wallConditions, Vector3 movementDelta)
        {
            var wallNeedsToBePlaced = ShorterWallNeedsToBeReplacedThroughFullWall(wallToMove, wallConditions);

            var newFloor = SpawnFloorNextToMovedWall(wallToMove);

            if (wallNeedsToBePlaced)
            {
                wallToMove = ReplaceShortenedWallThroughFullWall(wallToMove);
            }

            if ((wallConditions.WallExtensionScenario & WallExtensionScenario.CornerCounterClockwise) ==
                WallExtensionScenario.CornerCounterClockwise)
            {
                HandleCornerNextToShorterWall(wallToMove, newFloor, movementDelta, wallNeedsToBePlaced, false);
            }
            else
            {
                SpawnCornerAndShorterWall(wallToMove, newFloor, false);
            }

            if ((wallConditions.WallExtensionScenario & WallExtensionScenario.CornerClockwise) ==
                WallExtensionScenario.CornerClockwise)
            {
                HandleCornerNextToShorterWall(wallToMove, newFloor, movementDelta, wallNeedsToBePlaced, true);
            }
            else
            {
                SpawnCornerAndShorterWall(wallToMove, newFloor, true);
            }
        }

        private void HandleCornerNextToShorterWall(RoomElement wallToMove, RoomElement newFloor, Vector3 movementDelta, bool wallWasReplaced, bool clockwise)
        {
            var shiftDirection = clockwise ? 1 : -1;
            var wallToMoveDirection = wallToMove.SpawnOrientation.ToDirection();
            var wallToMoveDirectionShifted = wallToMoveDirection.Shift(shiftDirection);

            var corner = wallToMove.GetRoomElementByDirection(wallToMoveDirectionShifted);
            Undo.RecordObject(corner, "");

            //get other wall connected to corner, when found shorter wall, then delete corner otherwise move corner
            var otherWallConnectedToCorner = corner.GetRoomElementByDirection(wallToMoveDirection);

            //The room extends further next to moved wall; it isn't a 'normal' outer corner
            if (otherWallConnectedToCorner)
            {
                Undo.RecordObject(otherWallConnectedToCorner, ""); //store bindings

                //Connect newFloor to floorNextToOtherWallConnectedToCorner
                Undo.RecordObject(otherWallConnectedToCorner, "");
                var floorNextToOtherWallConnectedToCorner = otherWallConnectedToCorner.GetRoomElementByDirection(otherWallConnectedToCorner.SpawnOrientation.ToDirection().Opposite());
                Undo.RecordObject(floorNextToOtherWallConnectedToCorner, "");
                floorNextToOtherWallConnectedToCorner.ConnectElementByDirection(newFloor, otherWallConnectedToCorner.SpawnOrientation.ToDirection());

                if (wallWasReplaced)
                {
                    //delete corner (next to otherWallConnectedToCorner)
                    var unnecessaryCorner = otherWallConnectedToCorner.GetRoomElementByDirection(wallToMove.SpawnOrientation.ToDirection());
                    Undo.RecordObject(unnecessaryCorner, "");
                    var newNeighbor = unnecessaryCorner.GetRoomElementByDirection(wallToMoveDirectionShifted);
                    Undo.RecordObject(newNeighbor, "");
                    wallToMove.ConnectElementByDirection(newNeighbor, wallToMoveDirectionShifted);
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

                var wallAfterOtherWallConnectedToCorner = otherWallConnectedToCorner.GetRoomElementByDirection(wallToMoveDirection);
                Undo.RecordObject(wallAfterOtherWallConnectedToCorner, "");

                var neighbor = wallAfterOtherWallConnectedToCorner.GetRoomElementByDirection(wallToMoveDirection);
                otherWallConnectedToCorner.ConnectElementByDirection(neighbor, wallToMoveDirection);
                neighbor = wallAfterOtherWallConnectedToCorner.GetRoomElementByDirection(wallToMoveDirectionShifted);
                otherWallConnectedToCorner.ConnectElementByDirection(neighbor, wallToMoveDirectionShifted);
                
                Undo.DestroyObjectImmediate(wallAfterOtherWallConnectedToCorner.gameObject);
                
                Undo.RecordObject(otherWallConnectedToCorner.transform, "");
                otherWallConnectedToCorner.transform.position += movementDelta;
                Undo.RecordObject(corner.transform, "");
                corner.transform.position += movementDelta;
                
                return;
            }

            var newWall = MoveCornerSpawnFullWall(corner, wallToMove, movementDelta, clockwise);
            newFloor.ConnectElementByDirection(newWall, newWall.SpawnOrientation.ToDirection());
        }

        private static bool ShorterWallNeedsToBeReplacedThroughFullWall(RoomElement wallToMove, WallConditions wallConditions)
        {
            return (wallConditions.CounterClockwiseElement.Type.IsCornerType() && IsInnerCornerAndIsFollowedByOuterCorner(wallConditions.CounterClockwiseElement, wallToMove))
                   || (wallConditions.ClockwiseElement.Type.IsCornerType() && IsInnerCornerAndIsFollowedByOuterCorner(wallConditions.ClockwiseElement, wallToMove));
        }

        private static bool IsInnerCornerAndIsFollowedByOuterCorner(RoomElement corner, RoomElement wallToMove)
        {
            if (!IsInnerCorner(corner, wallToMove))
            {
                return false;
            }
            
            var wallToMoveDirection = wallToMove.SpawnOrientation.ToDirection();
            var neighborAfterCorner = corner.GetRoomElementByDirection(wallToMoveDirection);
            RoomElement cornerCandidate = neighborAfterCorner.GetRoomElementByDirection(wallToMoveDirection);;

            return cornerCandidate.Type.IsCornerType();
        }

        private static bool IsInnerCorner(RoomElement corner, RoomElement adjacentWall)
        {
            return corner.GetRoomElementByDirection(adjacentWall.SpawnOrientation.ToDirection()) != null;
        }

        private static RoomElement SpawnFloorNextToMovedWall(RoomElement wallToMove)
        {
            var wallDirection = wallToMove.SpawnOrientation.ToDirection();
            var spawnList = wallToMove.ExtendableRoom.ElementSpawner;
            var floorSpawner = spawnList[(int) RoomElementTyp.Floor];
            var referenceFloor = wallToMove.GetRoomElementByDirection(wallDirection.Opposite());
            var newFloor = floorSpawner.SpawnNextToRoomElement(referenceFloor, wallDirection, SpawnOrientation.Front);
            Undo.RegisterCreatedObjectUndo(newFloor.gameObject, "");
            Undo.RecordObject(referenceFloor, "");
            referenceFloor.ConnectElementByDirection(newFloor, wallDirection);
            newFloor.ConnectElementByDirection(wallToMove, wallDirection);

            return newFloor;
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

        private (RoomElement newCorner, RoomElement shorterWall) SpawnNewCornerNextToMovedWall(RoomElement wallToMove,
            Direction direction, int factor)
        {
            var spawnerList = wallToMove.ExtendableRoom.ElementSpawner;
            var cornerSpawner = spawnerList[(int) RoomElementTyp.Corner];
            var newCorner = cornerSpawner.SpawnNextToRoomElement(wallToMove, direction,
                GetCornerOrientationBasedOnWall(wallToMove, direction));
            Undo.RegisterCreatedObjectUndo(newCorner.gameObject, "");
            Undo.RecordObject(wallToMove, "");
            var oldWall = wallToMove.GetRoomElementByDirection(direction);
            //Undo.RecordObject(oldWall, "");
            RoomElement shorterWall = null;
            if (oldWall != null)
            {
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


                RoomElement oldWallNeighbourInDirection = oldWall.GetRoomElementByDirection(direction);
                var wallSpawner = spawnerList[(int) wallType];
                shorterWall = wallSpawner.SpawnNextToRoomElement(oldWallNeighbourInDirection, direction.Opposite(), wallToMove.SpawnOrientation);
                Undo.RegisterCreatedObjectUndo(shorterWall.gameObject, "");
                shorterWall.CopyNeighbors(oldWall);
                wallToMove.ConnectElementByDirection(null, direction);
                Undo.DestroyObjectImmediate(oldWall.gameObject);
            }

            wallToMove.ConnectElementByDirection(newCorner, direction);

            return (newCorner, shorterWall);
        }


        private WallConditions GetWallConditions(RoomElement wall)
        {
            var wallExtensionScenario = WallExtensionScenario.Empty;

            if (!wall.Type.IsWallType())
            {
                return null;
            }

            var clockwiseDirection = wall.SpawnOrientation.ToDirection().Shift(1);
            var counterClockwiseDirection = wall.SpawnOrientation.ToDirection().Shift(-1);

            var elementClockDir = wall.GetRoomElementByDirection(clockwiseDirection);
            var elementCountClockDir = wall.GetRoomElementByDirection(counterClockwiseDirection);

            if (elementClockDir != null && elementClockDir.Type.IsCornerType())
            {
                wallExtensionScenario |= WallExtensionScenario.CornerClockwise;
            }

            if (elementCountClockDir != null && elementCountClockDir.Type.IsCornerType())
            {
                wallExtensionScenario |= WallExtensionScenario.CornerCounterClockwise;
            }

            return new WallConditions(clockwiseDirection, counterClockwiseDirection, elementClockDir,
                elementCountClockDir, wallExtensionScenario);
        }

        #region utilitys

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

        private RoomElement MoveCornerSpawnFullWall(RoomElement corner, RoomElement wallToMove, Vector3 movementDelta,
            bool clockwise)
        {
            Undo.RecordObject(corner.transform, "");
            Undo.RecordObject(corner, "");
            corner.transform.position += movementDelta;
            return SpawnWallBasedOnCorner(corner, wallToMove, RoomElementTyp.Wall, clockwise);
        }

        private void SpawnCornerAndShorterWall(RoomElement wallToMove, RoomElement newFloor, bool clockwise)
        {
            var factor = clockwise ? 1 : -1;
            var (newCorner, shorterWall) = SpawnNewCornerNextToMovedWall(wallToMove,
                wallToMove.SpawnOrientation.ToDirection().Shift(factor), factor);
            var wallType = clockwise ? RoomElementTyp.WallShortenedRight : RoomElementTyp.WallShortenedLeft;
            var newWall = SpawnWallBasedOnCorner(newCorner, wallToMove, wallType, clockwise);
            if (newFloor != null)
            {
                newFloor.ConnectElementByDirection(newWall, newWall.SpawnOrientation.ToDirection());
            }

            var (newCorner2, _) = SpawnNewCornerNextToMovedWall(newWall,
                newWall.SpawnOrientation.ToDirection().Shift(factor), factor);
            newCorner2.ConnectElementByDirection(shorterWall, wallToMove.SpawnOrientation.ToDirection().Shift(factor));
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
            public Direction ClockwiseDirection { get; }
            public Direction CounterClockwiseDirection { get; }
            public RoomElement ClockwiseElement { get; }
            public RoomElement CounterClockwiseElement { get; }
            public WallExtensionScenario WallExtensionScenario { get; }

            public WallConditions(Direction clockwiseDirection, Direction counterClockwiseDirection,
                RoomElement clockwiseElement, RoomElement counterClockwiseElement,
                WallExtensionScenario wallExtensionScenario)
            {
                ClockwiseDirection = clockwiseDirection;
                CounterClockwiseDirection = counterClockwiseDirection;
                ClockwiseElement = clockwiseElement;
                CounterClockwiseElement = counterClockwiseElement;
                WallExtensionScenario = wallExtensionScenario;
            }
        }


        [Flags]
        private enum WallExtensionScenario
        {
            Empty = 0,
            CornerCounterClockwise = 1 << 0,
            CornerClockwise = 1 << 1,
            CornerInBothDirections = CornerCounterClockwise | CornerClockwise
        }
    }
}