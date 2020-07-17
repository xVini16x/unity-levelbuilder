﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityLevelEditor.Model;
using UnityEditor.EditorTools;
using UnityEngine.Serialization;
using UnityEngine.UI;
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
        
        private float capOffset = 3f;

        private float capSize = 2f;

        public override bool IsAvailable()
        {
            if (!(target is GameObject t))
            {
                return false;
            }

            var roomElement = t.GetComponent<RoomElement>();
            return (roomElement != null && roomElement.Type == RoomElementTyp.Wall);
        }

        private Vector3 Snapping(ElementSpawner floorSpawner, Vector3 pos)
        {
            var snapValue = floorSpawner.Bounds.size.x;
            
            Debug.Log(snapValue);
            pos.x = Mathf.Round(pos.x / snapValue) * snapValue;
            pos.z = Mathf.Round(pos.z / snapValue) * snapValue;
            return pos;
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
               ChangePosition( Snapping(representativeWall.ExtendableRoom.FloorSpawner, position - Tools.handlePosition));
            }
        }


        private void ChangePosition(Vector3 delta)
        {
            Undo.RecordObjects(Selection.transforms, "Room Extension");

            foreach (var transform in Selection.transforms)
            {
                transform.position += delta;
            }
        }

        private Vector3 DrawHandle(Vector3 position, Vector3 direction, Color color)
        {
            using (new Handles.DrawingScope(color))
            {
                return Handles.Slider(position, direction, capSize, Handles.ArrowHandleCap, 1f);
            }
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

            if (selectedRoomElement.Type != RoomElementTyp.Wall)
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
    }
}