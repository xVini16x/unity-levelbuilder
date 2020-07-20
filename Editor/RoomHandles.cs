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
                var snapValue = representativeWall.ExtendableRoom.ElementSpawner[(int) RoomElementTyp.Floor].Bounds.size.x;
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
                return Handles.Slider(position, direction, HandleUtility.GetHandleSize(position), Handles.ArrowHandleCap, 1f);
            }
        }
        #endregion
        
        #region RoomExtending 
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
      
            Debug.Log("undo group id " + undoGroupId);
            
            var wallConditions = GetWallConditions(wallToMove);

            Undo.RecordObject(wallToMove.transform, "");
            Undo.RecordObject(wallToMove, "");
            wallToMove.transform.position += movementDelta;

            var spawnerList = wallToMove.ExtendableRoom.ElementSpawner;
            var wallDirection = wallToMove.SpawnOrientation.ToDirection();
            var floorSpawnDirection = wallDirection.Opposite();
            
            var newFloor = spawnerList[(int) RoomElementTyp.Floor].SpawnNextToRoomElement(wallToMove, floorSpawnDirection);
            Undo.RegisterCreatedObjectUndo(newFloor.gameObject, "");

            var oldFloor = wallToMove.GetRoomElementByDirection(floorSpawnDirection);
            Undo.RecordObject(oldFloor, "");
            wallToMove.ConnectElementByDirection(newFloor, floorSpawnDirection);
            oldFloor.ConnectElementByDirection(newFloor, wallDirection);

            
            if ((wallConditions.WallExtensionScenario & WallExtensionScenario.CornerCounterClockwise) == WallExtensionScenario.CornerCounterClockwise)
            {
                var factor = -1;
                var corner =  wallConditions.CounterClockwiseElement;
                Undo.RecordObject(corner.transform, "");
                Undo.RecordObject(corner, "");
                corner.transform.position += movementDelta;
                SpawnWallBasedOnCorner(corner, wallToMove, newFloor, RoomElementTyp.Wall, factor);
            }
            else
            {
                
                var factor = -1;
                var (newCorner, shorterWall) = SpawnNewCornerNextToMovedWall(wallToMove, wallConditions.CounterClockwiseDirection, "", factor);
                var wallType = RoomElementTyp.WallShortenedLeft;
                var newWall = SpawnWallBasedOnCorner(newCorner, wallToMove, newFloor, wallType,  factor);
                var (newCorner2, _) = SpawnNewCornerNextToMovedWall(newWall, newWall.SpawnOrientation.ToDirection().Shift(factor), "", factor);
                newCorner2.ConnectElementByDirection(shorterWall, wallConditions.CounterClockwiseDirection);
            }

            if ((wallConditions.WallExtensionScenario & WallExtensionScenario.CornerClockwise) == WallExtensionScenario.CornerClockwise)
            {
                var corner = wallConditions.ClockwiseElement;
                Undo.RecordObject(corner.transform, "");
                Undo.RecordObject(corner, "");
                corner.transform.position += movementDelta;
                var factor = 1;
                SpawnWallBasedOnCorner(corner, wallToMove, newFloor, RoomElementTyp.Wall, factor);
            }
            else
            {
                var factor = 1;
                var (newCorner, shorterWall) = SpawnNewCornerNextToMovedWall(wallToMove, wallConditions.ClockwiseDirection, "", factor);
                var wallType = RoomElementTyp.WallShortenedRight;
                var newWall = SpawnWallBasedOnCorner(newCorner, wallToMove, newFloor, wallType, factor);
                var (newCorner2, _) = SpawnNewCornerNextToMovedWall(newWall, newWall.SpawnOrientation.ToDirection().Shift(factor), "COI", factor);
                newCorner2.ConnectElementByDirection(shorterWall, wallConditions.ClockwiseDirection);
                
            }
            
            /*
            foreach (var transform in Selection.transforms)
            {
                transform.position += movementDelta;
            }
            */
            
            Undo.CollapseUndoOperations(undoGroupId);
        }

        private RoomElement SpawnWallBasedOnCorner(RoomElement corner, RoomElement wallToMove, RoomElement floor, RoomElementTyp spawnerType, int factor)
        {
            var spawnerList = wallToMove.ExtendableRoom.ElementSpawner;
            var spawnOrientation = wallToMove.SpawnOrientation.Shift(factor);
            //var spawnerType = spawnOrientation == SpawnOrientation.Back ? RoomElementTyp.WallTransparent : RoomElementTyp.Wall;
            var spawnDirection = spawnOrientation.ToDirection().Shift(factor);
            var spawner = spawnerList[(int) spawnerType];
            var newWall =  spawner.SpawnNextToRoomElement(corner, spawnDirection);
            var oldElementInSpawnDirection = corner.GetRoomElementByDirection(spawnDirection);
            if (oldElementInSpawnDirection != null)
            {
                Undo.RecordObject(oldElementInSpawnDirection, "");
                newWall.ConnectElementByDirection(oldElementInSpawnDirection, spawnDirection);
            }
            corner.ConnectElementByDirection(newWall, spawnDirection);
            floor.ConnectElementByDirection(newWall, newWall.SpawnOrientation.ToDirection());
            Undo.RegisterCreatedObjectUndo(newWall.gameObject, "");
            return newWall;
        }

        private (RoomElement newCorner, RoomElement shorterWall) SpawnNewCornerNextToMovedWall(RoomElement wallToMove, Direction direction, string name, int factor)
        {
            var spawnerList = wallToMove.ExtendableRoom.ElementSpawner;
            var cornerSpawner = spawnerList[(int) RoomElementTyp.Corner];
            var newCorner = cornerSpawner.SpawnNextToRoomElement(wallToMove, direction);
            newCorner.gameObject.name = name;
            Undo.RegisterCreatedObjectUndo(newCorner.gameObject, "");
            Undo.RecordObject(wallToMove, "");
            var oldWall = wallToMove.GetRoomElementByDirection(direction);
            RoomElement shorterWall = null;
            if (oldWall != null)
            {
                var wallType = factor == 1
                    ? RoomElementTyp.WallShortenedLeft
                    : RoomElementTyp.WallShortenedRight;

                RoomElement oldWallNeighbourInDirection = oldWall.GetRoomElementByDirection(direction);
                var wallSpawner = spawnerList[(int) wallType];
                shorterWall = wallSpawner.SpawnNextToRoomElement(oldWallNeighbourInDirection, direction.Opposite());
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
            WallExtensionScenario wallExtensionScenario = WallExtensionScenario.Empty;

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

            return new WallConditions(clockwiseDirection, counterClockwiseDirection, elementClockDir, elementCountClockDir, wallExtensionScenario);
        }

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

            var roomElementsOfActiveType = FilterSelectionForRoomElementsOfGivenTypeAndOrientation(selectedRoomElement.Type, selectedRoomElement.SpawnOrientation);
            
            Selection.objects = roomElementsOfActiveType.Select(roomElement => roomElement.gameObject).ToArray<Object>();

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
            Selection.objects = Selection.gameObjects.Where(go => go.GetComponent<RoomElement>() == null).ToArray<Object>();
        }

        private List<RoomElement> FilterSelectionForRoomElementsOfGivenTypeAndOrientation(RoomElementTyp type, SpawnOrientation orientation)
        {
            return Selection.transforms.Select(t => t.GetComponent<RoomElement>())
                .Where(r => r != null && r.Type == type && r.SpawnOrientation == orientation).ToList();
        }
        #endregion

        private class WallConditions
        {
            public Direction ClockwiseDirection { get; private set; }
            public Direction CounterClockwiseDirection { get; private set; }
            public RoomElement ClockwiseElement { get; private set; }
            public RoomElement CounterClockwiseElement { get; private set; }
            public WallExtensionScenario WallExtensionScenario { get; private set; }

            public WallConditions(Direction clockwiseDirection, Direction counterClockwiseDirection, RoomElement clockwiseElement, RoomElement counterClockwiseElement, WallExtensionScenario wallExtensionScenario)
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