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
            return (roomElement != null);
        }

        private Vector3 Snapping(Vector3 pos)
        {
            pos.x= Mathf.Round(pos.x / 15f)*15f;
            pos.z= Mathf.Round(pos.z / 15f)*15f;
            return pos;
        }
        
        public override void OnToolGUI(EditorWindow window)
        {
            EditorGUI.BeginChangeCheck();

            Vector3 position = Tools.handlePosition;

            using (new Handles.DrawingScope(Color.red))
            {
                position = Handles.Slider(position, Vector3.right, capSize, Handles.ArrowHandleCap, 1f);
            }
            using (new Handles.DrawingScope(Color.blue))
            {
                position = Handles.Slider(position, Vector3.forward, capSize, Handles.ArrowHandleCap, 1f);
            }

            if (EditorGUI.EndChangeCheck())
            {
                Vector3 delta = Snapping(position - Tools.handlePosition);

                Undo.RecordObjects(Selection.transforms, "Room Extension");

                foreach (var transform in Selection.transforms)
                    transform.position += delta;
            }
        }
        
        #endregion
    }
}