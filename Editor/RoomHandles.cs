using UnityEditor;
using UnityEngine;
using UnityLevelEditor.Model;
using UnityEditor.EditorTools;
using UnityEngine.Serialization;

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
            //Debug.Log("Enable Tool");
        }

        private void OnDisable()
        {
            //Debug.Log("Disable");
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

        private RoomElement roomElement;

        private float capOffset = 3f;

        private float capSize = 2f;

        public override bool IsAvailable()
        {
            if (!(target is GameObject t))
            {
                return false;
            }

            roomElement = t.GetComponent<RoomElement>();
            return (roomElement != null && roomElement.Type == RoomElementTyp.Wall);
        }

        private Vector3 Snapping(Vector3 pos)
        {
            var snapValue = roomElement.ExtendableRoom.FloorSpawner.Bounds.size.x;
            
            Debug.Log(snapValue);
            pos.x = Mathf.Round(pos.x / snapValue) * snapValue;
            pos.z = Mathf.Round(pos.z / snapValue) * snapValue;
            return pos;
        }

        public override void OnToolGUI(EditorWindow window)
        {
            if (!roomElement || roomElement.Type != RoomElementTyp.Wall)
            {
                return;
            }

            EditorGUI.BeginChangeCheck();

            Vector3 position = Tools.handlePosition;

            switch (roomElement.SpawnOrientation)
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
                Vector3 delta = Snapping(position - Tools.handlePosition);

                Undo.RecordObjects(Selection.transforms, "Room Extension");

                foreach (var transform in Selection.transforms)
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
    }
}