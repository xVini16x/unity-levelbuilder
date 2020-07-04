using UnityEditor;

[CustomEditor(typeof (PersistentInstanceId))]
public class PersistentIdEditor : Editor {

    public void OnEnable()
    {
        PersistentInstanceId persistentId = (PersistentInstanceId) target;

        if(persistentId.Guid == System.Guid.Empty)
        {
            persistentId.CreateNewId();            
        }
        EditorUtility.SetDirty(target);
    }

    public override void OnInspectorGUI()
    {
        PersistentInstanceId persistentId = (PersistentInstanceId)target;

        EditorGUILayout.SelectableLabel(persistentId.Guid.ToString());
    }
}
