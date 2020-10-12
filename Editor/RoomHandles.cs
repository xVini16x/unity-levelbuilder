using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEditor.EditorTools;
using Object = UnityEngine.Object;

namespace UnityLevelEditor.Editor
{
    using Model;
    
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

            var position = Tools.handlePosition;
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
                var snapValue = representativeWall.ExtendableRoom.ElementSpawner[(int) RoomElementType.Floor].Bounds.size.x;
                var movementDelta = SnapVectorXZ(position, Tools.handlePosition, snapValue);

                if (representativeWall.SpawnOrientation.IsSideways())
                {
                    movementDelta.z = 0;
                }
                else
                {
                    movementDelta.x = 0;
                }

                if (representativeWall.SpawnOrientation.TowardsNegative() == (movementDelta.x > 0.01f || movementDelta.z > 0.01f))
                {
                    return;
                }

                if (Mathf.Abs(movementDelta.x) < 0.01f && Mathf.Abs(movementDelta.z) < 0.01f)
                {
                    return;
                }

                RoomExtension.ExtendTheRoom(selectedWalls, movementDelta);
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

        private List<RoomElement> FilterSelectionForRoomElementsOfGivenTypeAndOrientation(RoomElementType type,
            SpawnOrientation orientation)
        {
            return Selection.transforms.Select(t => t.GetComponent<RoomElement>())
                .Where(r => r != null
                            && ((type.IsWallType() && r.Type.IsWallType()) || (type.IsCornerType() && r.Type.IsCornerType()) || r.Type == type)
                            && r.SpawnOrientation == orientation)
                .ToList();
        }

        #endregion
    }
}