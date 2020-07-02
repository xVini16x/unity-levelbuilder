using UnityEditor;

[CustomEditor(typeof (PersistentInstanceId))]
public class PersistentIdEditor : Editor {

    public void OnEnable()
    {
        PersistentInstanceId persistentId = (PersistentInstanceId) target;

        if(persistentId.guid == System.Guid.Empty)
        {
            persistentId.CreateNewId();
            //Stop from updating 
            EditorUtility.SetDirty(target);
        }
    }

    public override void OnInspectorGUI()
    {
        PersistentInstanceId persistentId = (PersistentInstanceId)target;

        EditorGUILayout.SelectableLabel(persistentId.guid.ToString());
    }
}
