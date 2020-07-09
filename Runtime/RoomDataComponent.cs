using UnityEditor;
using UnityEngine;

//founds some tipps in:
//https://answers.unity.com/questions/420772/how-to-destroy-linked-components-when-object-is-de.html#:~:text=Destroying%20object%20multiple%20times.,object%20in%20OnDisable%20or%20OnDestroy.&text=My%20work%20around%20for%20now,more%20certainty%20in%20destruction%20order.

[ExecuteInEditMode]
public class RoomDataComponent : MonoBehaviour
{
    public RoomData roomData { get; set; }

    private void OnDestroy()
    {
        //destroys according scriptable object data when room is deleted in editor hierarchy
#if UNITY_EDITOR
    if (!UnityEditor.EditorApplication.isPlaying)
        {
            if (Time.frameCount != 0 && Time.renderedFrameCount != 0) //not loading scene
            {
                if (this != null)
                {
                    EditorApplication.delayCall += () =>
                    {
                        if (AssetDatabase.DeleteAsset("Assets/_GameAssets/Data/" + roomData.name + ".asset"))
                        {
                            Debug.Log("Deleted Room Data successful");
                        }
                        else
                        {
                            Debug.LogWarning("Could not delete Room Data "+roomData.name);
                        }
                       
                        GameObject.DestroyImmediate(this);
                    };
                }
            }
        }
#endif
    }
}
