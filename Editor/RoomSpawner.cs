using UnityEngine;
using UnityEditor;

public class RoomSpawner : EditorWindow
{
    [SerializeField] private GameObject wall;
    [SerializeField] private GameObject floor;
    [SerializeField] private GameObject corner;
    [SerializeField] private string roomname = "StandardRoom";

    private GameObject room; //parrent
    private GameObject backWall;
    private GameObject frontWall;
    private GameObject leftWall;
    private GameObject rightWall;
    private GameObject cornerBL;
    private GameObject cornerBR;
    private GameObject cornerFR;
    private GameObject cornerFL;
    private GameObject roomfloor;

    private string assetPath = "Assets/_GameAssets/Data/";
    private string fileformat = ".asset";

    // Add menu named "My Window" to the Window menu
    [MenuItem("Window/Unity Levelbuilder Tool")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        RoomSpawner window = (RoomSpawner)EditorWindow.GetWindow(typeof(RoomSpawner));
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Room Components", EditorStyles.boldLabel);
        wall = EditorGUILayout.ObjectField("Wall Prefab", wall, typeof(GameObject), true) as GameObject;
        floor = EditorGUILayout.ObjectField("Floor Prefab", floor, typeof(GameObject), true) as GameObject;
        corner = EditorGUILayout.ObjectField("Corner Prefab", corner, typeof(GameObject), true) as GameObject;
        GUILayout.Label("Room Settings", EditorStyles.boldLabel);
        roomname = EditorGUILayout.TextField("Name", roomname);

        if (GUILayout.Button("Create Room"))
        {
            if(roomname == null) {
                Debug.LogError("Roomname cannot be empty.");
                return;
            }
            if (AssetDatabase.LoadAssetAtPath(assetPath + "/" + roomname + ".asset", typeof(RoomData)) != null)
            {
                Debug.LogError("Roomname already in use.");
                return;
            }

            if (wall != null && floor != null && corner != null)
            {
                room = new GameObject(roomname);
                RoomElement backWallData = CreateRoomElement("Back Wall", new Vector3(0, -0.5f, 0), RoomElementTyp.Wall, 0f);
                RoomElement frontWallData = CreateRoomElement("Front Wall", new Vector3(0, -0.5f, -5f), RoomElementTyp.Wall, 180f);
                RoomElement leftWallData = CreateRoomElement("Left Wall", new Vector3(-2.5f, -0.5f, -2.5f), RoomElementTyp.Wall, -90f);
                RoomElement rightWallData = CreateRoomElement("Right Wall", new Vector3(2.5f, -0.5f, -2.5f), RoomElementTyp.Wall, 90f);

                RoomElement cornerBLData = CreateRoomElement("Corner BL", new Vector3(-2.5f, 2.5f, 0), RoomElementTyp.Corner, 0f);
                RoomElement cornerBRData = CreateRoomElement("Corner BR", new Vector3(2.5f, 2.5f, 0), RoomElementTyp.Corner, 90f);
                RoomElement cornerFLData = CreateRoomElement("Corner FL", new Vector3(-2.5f, 2.5f, -5), RoomElementTyp.Corner, -90f);
                RoomElement cornerFRData = CreateRoomElement("Corner FR", new Vector3(2.5f, 2.5f, -5), RoomElementTyp.Corner, 180f);

                RoomElement roomfloorData = CreateRoomElement("Floor", new Vector3(0, 0, -2.5f), RoomElementTyp.Floor, 0f);

                //set room element relations
                backWallData.GuidBack = roomfloorData.Guid; // due to lower z value
                backWallData.GuidLeft = cornerBLData.Guid;
                backWallData.GuidRight = cornerBRData.Guid;

                frontWallData.GuidFront = roomfloorData.Guid;
                frontWallData.GuidLeft = cornerFLData.Guid;
                frontWallData.GuidRight = cornerFRData.Guid;
                
                leftWallData.GuidFront = cornerBLData.Guid;
                leftWallData.GuidBack = cornerFLData.Guid;
                leftWallData.GuidRight = roomfloorData.Guid;

                rightWallData.GuidFront = cornerBRData.Guid;
                rightWallData.GuidBack = cornerFRData.Guid;
                rightWallData.GuidLeft = roomfloorData.Guid;

                cornerBLData.GuidBack = leftWallData.Guid;
                cornerBLData.GuidRight = backWallData.Guid;
                
                cornerBRData.GuidBack = rightWallData.Guid;
                cornerBRData.GuidLeft = backWallData.Guid;
                
                cornerFLData.GuidFront = leftWallData.Guid;
                cornerFLData.GuidRight = frontWallData.Guid;
                
                cornerFRData.GuidFront = rightWallData.Guid;
                cornerFRData.GuidLeft = frontWallData.Guid;

                roomfloorData.GuidBack = frontWallData.Guid;
                roomfloorData.GuidLeft = leftWallData.Guid;
                roomfloorData.GuidRight = rightWallData.Guid;
                roomfloorData.GuidFront = backWallData.Guid;

                RoomData roomData = ScriptableObject.CreateInstance<RoomData>();
                roomData.AddRoomElement(backWallData);
                roomData.AddRoomElement(frontWallData);
                roomData.AddRoomElement(leftWallData);
                roomData.AddRoomElement(rightWallData);
                roomData.AddRoomElement(cornerBLData);
                roomData.AddRoomElement(cornerBRData);
                roomData.AddRoomElement(cornerFLData);
                roomData.AddRoomElement(cornerFRData);
                roomData.AddRoomElement(roomfloorData);
                AssetDatabase.CreateAsset(roomData, assetPath + roomname + fileformat);
                AssetDatabase.SaveAssets();
                RoomDataComponent roomDataComponent= room.AddComponent<RoomDataComponent>();
                roomDataComponent.roomData = roomData;
                Debug.Log("Room created");
            }
            else
            {
                Debug.LogError("Please assign all components to create a room. You can find default components in the prefabs folder.");
            }             
        }
    }

    private RoomElement CreateRoomElement(string name, Vector3 position, RoomElementTyp type, float rotationAngle)
    {
        GameObject prefab;
        
        switch (type)
        {
            case RoomElementTyp.Corner:
                prefab = corner;
                break;
            case RoomElementTyp.Wall:
                prefab = wall;
                break;
            case RoomElementTyp.Floor:
                prefab = floor;
                break;
            default:
                Debug.LogError($"Unsupported Room Type '{type}' - Fallback to Wall");
                prefab = wall;
                break;
        }

        GameObject spawnedObject = Instantiate(prefab, room.transform);
        spawnedObject.name = name;
        spawnedObject.transform.position = position;
        var guidComp = spawnedObject.AddComponent<PersistentInstanceId>();
        if(Mathf.Abs(rotationAngle)>0.01f)
            spawnedObject.transform.Rotate(Vector3.up, rotationAngle);

        RoomElement roomElement = new RoomElement();
        roomElement.Type = type;
        roomElement.Guid = guidComp.Guid;
        return roomElement;
    }
}
