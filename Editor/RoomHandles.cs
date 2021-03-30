using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

using UnityEditor;
using UnityEditor.EditorTools;

using UnityEngine;

using UnityLevelEditor.Model;

namespace UnityLevelEditor.Editor
{
    [EditorTool("RoomExtension")]
    public class RoomHandles : EditorTool
    {
        #region Inspector Fields

        [SerializeField] [UsedImplicitly] private Texture2D toolIcon;

        [SerializeField] [UsedImplicitly] private Texture2D toolIconActive;

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
        }

        private void OnDisable()
        {
            EditorTools.activeToolChanged -= ChangeIcon;
        }

        private void ChangeIcon()
        {
            var icon = EditorTools.IsActiveTool(this) ? toolIconActive : toolIcon;

            iconContent = new GUIContent() { image = icon, text = text, tooltip = tooltip };
        }

        #endregion

        #region Handles

        public override bool IsAvailable()
        {
            if (!(target is GameObject t))
            {
                return false;
            }

            var roomElement = t.GetComponent<WallElement>();
            return (roomElement != null && roomElement.Type.IsWallType());
        }

        public override void OnToolGUI(EditorWindow window)
        {
            if (!TryGetSelectedWallsAndUpdateSelection(out var selectedWalls))
            {
                return;
            }

            EditorGUI.BeginChangeCheck();

            var position = Tools.handlePosition;
            var representativeWall = selectedWalls[0];

            switch (representativeWall.Direction)
            {
                case Direction.Back:
                    position = DrawHandle(position, Vector3.back, Color.blue);
                    break;
                case Direction.Front:
                    position = DrawHandle(position, Vector3.forward, Color.blue);
                    break;
                case Direction.Left:
                    position = DrawHandle(position, Vector3.left, Color.red);
                    break;
                case Direction.Right:
                    position = DrawHandle(position, Vector3.right, Color.red);
                    break;
            }

            if (EditorGUI.EndChangeCheck())
            {
                var snapValue = representativeWall.ExtendableRoom.FloorSize;
                var movementDelta = SnapVectorXZ(position, Tools.handlePosition, snapValue);

                if (representativeWall.Direction.IsSideways())
                {
                    movementDelta.z = 0;
                }
                else
                {
                    movementDelta.x = 0;
                }

                if (Mathf.Abs(movementDelta.x) < 0.01f && Mathf.Abs(movementDelta.z) < 0.01f)
                {
                    return;
                }

                ExtendTheRoom(selectedWalls, movementDelta);
            }
        }

        private void ExtendTheRoom(List<WallElement> selectedWalls, Vector3 movementDelta)
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Room Extension");
            var undoGroupId = Undo.GetCurrentGroup();

            var representativeWall = selectedWalls[0];
            var extendableRoom = representativeWall.ExtendableRoom;
            var direction = movementDelta.AsDirectionXZ();
            var floorGridAddition = direction.AsVector2Int();

            var newFloors = new List<FloorElement>();
            var shrinking = representativeWall.Direction.Opposite() == direction;

            for (var i = 0; i < selectedWalls.Count; i++)
            {
                var selectedWall = selectedWalls[i];

                var newFloorTilePosition = selectedWall.FloorTilePosition + floorGridAddition;
                FloorElement newFloor;

                if (shrinking)
                {
                    newFloor = extendableRoom.DeleteFloor(selectedWall.FloorTilePosition, newFloorTilePosition);
                }
                else
                {
                    newFloor = extendableRoom.Spawn(newFloorTilePosition);
                }

                if (newFloor != null)
                {
                    newFloors.Add(newFloor);
                }
            }

            foreach (var newFloor in newFloors)
            {
                var relevantDirection = shrinking ? direction.Opposite() : direction;

                if (newFloor.TryGetWall(relevantDirection, out var wallElement))
                {
                    AddToSelection(wallElement, false);
                }
            }

            Undo.CollapseUndoOperations(undoGroupId);
        }

        private Vector3 DrawHandle(Vector3 position, Vector3 direction, Color color)
        {
            using (new Handles.DrawingScope(color))
            {
                return Handles.Slider(position, direction, HandleUtility.GetHandleSize(position), Handles.ArrowHandleCap, 0f);
            }
        }

        private static void AddToSelection(WallElement newSelectedElement, bool asActiveObject)
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

        private static Vector3 SnapVectorXZ(Vector3 newPosition, Vector3 oldPosition, float snapValue)
        {
            var vector = newPosition - oldPosition;

            var snapValueFactorX = Mathf.Clamp(Mathf.Round(vector.x / snapValue), -1, 1);
            vector.x = snapValueFactorX * snapValue;

            var snapValueFactorZ = Mathf.Clamp(Mathf.Round(vector.z / snapValue), -1, 1);
            vector.z = snapValueFactorZ * snapValue;

            return vector;
        }

        #endregion

        #region SelectionHandling

        private bool TryGetSelectedWallsAndUpdateSelection(out List<WallElement> selectedWalls)
        {
            selectedWalls = null;

            if (!TryGetSelectedGameObject(out var selectedGameObject))
            {
                return false;
            }

            var selectedRoomElement = selectedGameObject.GetComponent<WallElement>();

            if (selectedRoomElement == null)
            {
                RemoveRoomElementsFromSelection();
                return false;
            }

            var roomElementsOfActiveType = FilterSelectionForRoomElementsOfGivenTypeAndOrientation(selectedRoomElement.Type, selectedRoomElement.Direction);

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
            Selection.objects = Selection.gameObjects.Where(go => go.GetComponent<WallElement>() == null).ToArray<Object>();
        }

        private List<WallElement> FilterSelectionForRoomElementsOfGivenTypeAndOrientation(RoomElementType type, Direction direction)
        {
            return Selection.transforms.Select(t => t.GetComponent<WallElement>())
                            .Where(r => r != null
                                        && ((type.IsWallType() && r.Type.IsWallType()) || (type.IsCornerType() && r.Type.IsCornerType()) || r.Type == type)
                                        && r.Direction == direction)
                            .ToList();
        }

        #endregion
    }
}
