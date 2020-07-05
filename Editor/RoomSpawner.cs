using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

public class RoomSpawner : EditorWindow
{
    private const string AssetPath = "Assets/_GameAssets/Data/";
    private const string FileFormat = ".asset";

    [SerializeField] private GameObject wall;
    [SerializeField] private GameObject floor;
    [SerializeField] private GameObject corner;
    [SerializeField] private string roomName = "StandardRoom";
    private Vector2Int dimensions = new Vector2Int(1, 1);


    // Add menu named "My Window" to the Window menu
    [MenuItem("Window/Unity Levelbuilder Tool")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        RoomSpawner window = (RoomSpawner) EditorWindow.GetWindow(typeof(RoomSpawner));
        window.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Room Components", EditorStyles.boldLabel);
        wall = EditorGUILayout.ObjectField("Wall Prefab", wall, typeof(GameObject), true) as GameObject;
        floor = EditorGUILayout.ObjectField("Floor Prefab", floor, typeof(GameObject), true) as GameObject;
        corner = EditorGUILayout.ObjectField("Corner Prefab", corner, typeof(GameObject), true) as GameObject;
        GUILayout.Label("Room Settings", EditorStyles.boldLabel);
        roomName = EditorGUILayout.TextField("Name", roomName);

        if (GUILayout.Button("Create Room"))
        {
            if (roomName == null)
            {
                Debug.LogError("Roomname cannot be empty.");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath(AssetPath + "/" + roomName + ".asset", typeof(RoomData)) != null)
            {
                Debug.LogError("Roomname already in use.");
                return;
            }

            if (wall == null || floor == null || corner == null)
            {
                Debug.LogError("Please assign all components to create a room. You can find default components in the prefabs folder.");
                return;
            }

            SpawnNewRoom();
        }
    }

    private void SpawnNewRoom()
    {
        ElementSpawner wallSpawner, cornerSpawner, floorSpawner;
        
        try
        {
            wallSpawner = new ElementSpawner(wall, RoomElementTyp.Wall);
            cornerSpawner = new ElementSpawner(corner, RoomElementTyp.Corner);
            floorSpawner = new ElementSpawner(floor, RoomElementTyp.Floor);
        }
        catch (MissingComponentException e)
        {
            Debug.LogError(e.Message);
            return;
        }
        
        var spawnInfo = GetSpawnInfo(wallSpawner.Bounds, cornerSpawner.Bounds);
        var roomElements = new RoomElement[spawnInfo.NumberOfWalls.y + 2, spawnInfo.NumberOfWalls.x + 2];

        RoomData roomData = ScriptableObject.CreateInstance<RoomData>();
        var room = new GameObject(roomName);
        SpawnFrontOrBackOfRoom(spawnInfo, wallSpawner, cornerSpawner, roomElements, roomData, room.transform,false);
        SpawnRoomCenter(spawnInfo, wallSpawner, floorSpawner, roomElements, roomData, room.transform);
        SpawnFrontOrBackOfRoom(spawnInfo, wallSpawner, cornerSpawner, roomElements, roomData, room.transform, true);
        
        AssetDatabase.CreateAsset(roomData, AssetPath + roomName + FileFormat);
        AssetDatabase.SaveAssets();
        RoomDataComponent roomDataComponent = room.AddComponent<RoomDataComponent>();
        roomDataComponent.roomData = roomData;
        Debug.Log("Room created");
    }

    private RoomElement CreateRoomElement(string name, Vector3 position, RoomElementTyp type, float rotationAngle, Transform parent)
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

        GameObject spawnedObject = Instantiate(prefab, parent);
        spawnedObject.name = name;
        spawnedObject.transform.position = position;
        var guidComp = spawnedObject.AddComponent<PersistentInstanceId>();
        if (Mathf.Abs(rotationAngle) > 0.01f)
            spawnedObject.transform.Rotate(Vector3.up, rotationAngle);

        RoomElement roomElement = new RoomElement();
        roomElement.Type = type;
        roomElement.Guid = guidComp.Guid;
        return roomElement;
    }

    private SpawnInfo GetSpawnInfo(Bounds wallMeshBounds, Bounds cornerMeshBounds)
    {
        var wallSize = wallMeshBounds.size;
        var cornerSize = cornerMeshBounds.size;

        var roomSize = new Vector3();
        var numberOfWalls = new Vector2Int();

        var numberOfWallsAndRoomSize = CalculateNumberOfWallsAndRoomSize(dimensions.x, wallSize.x, cornerSize.x, "x");
        roomSize.x = numberOfWallsAndRoomSize.roomSize;
        numberOfWalls.x = numberOfWallsAndRoomSize.numberOfWalls;

        numberOfWallsAndRoomSize = CalculateNumberOfWallsAndRoomSize(dimensions.y, wallSize.x, cornerSize.z, "z");
        roomSize.z = numberOfWallsAndRoomSize.roomSize;
        numberOfWalls.y = numberOfWallsAndRoomSize.numberOfWalls;

        roomSize.y = wallMeshBounds.size.y;

        return new SpawnInfo(Vector3.zero, roomSize, numberOfWalls);
    }

    private (int numberOfWalls, float roomSize) CalculateNumberOfWallsAndRoomSize(float unitsToFill, float wallSize, float cornerSize, string dimensionName)
    {
        //Guarantee to not create a room bigger than wished for (TODO: change to create a room near the size of the wished size)
        var numberOfWalls = (int) ((unitsToFill / 2f - cornerSize) / wallSize);

        if (numberOfWalls == 0)
        {
            Debug.LogWarning($"{dimensionName.ToUpper()}-Dimension was too small. At least one wall should be spawned. Room will therefore will be bigger than given {dimensionName.ToLower()}-value.");
            numberOfWalls = 1;
        }

        return (numberOfWalls, 2f * cornerSize + (numberOfWalls * wallSize));
    }

    private void SpawnFrontOrBackOfRoom(SpawnInfo spawnInfo, ElementSpawner wallSpawner, ElementSpawner cornerSpawner, RoomElement[,] roomElements, RoomData roomData, Transform parent, bool isBackWall)
    {
        Vector3 currentSpawnPos;
        Vector2Int indices;
        SpawnOrientation wallOrientation;
        SpawnOrientation firstCornerOrientation;
        SpawnOrientation secondCornerOrientation;

        RoomElement newRoomElement;
        Vector3 spawnedElementSize;
        
        if (isBackWall)
        {
            indices = new Vector2Int(0, roomElements.GetLength(0) - 1);
            wallOrientation = SpawnOrientation.Back;
            firstCornerOrientation = SpawnOrientation.Left;
            secondCornerOrientation = wallOrientation;
            currentSpawnPos = spawnInfo.RoomBounds.min;
            currentSpawnPos = cornerSpawner.ConvertLeftBottomBackPositionToLeftBottomCenterPosition(currentSpawnPos, wallOrientation);
        }
        else
        {
            indices = new Vector2Int(0, 0);
            wallOrientation = SpawnOrientation.Front;
            firstCornerOrientation = wallOrientation;
            secondCornerOrientation = SpawnOrientation.Right;
            currentSpawnPos = spawnInfo.RoomBounds.min;
            currentSpawnPos.z = spawnInfo.RoomBounds.max.z;
            currentSpawnPos = cornerSpawner.ConvertLeftBottomFrontPositionToLeftBottomCenterPosition(currentSpawnPos, wallOrientation);
        }
        
        (newRoomElement, spawnedElementSize) = cornerSpawner.SpawnByLeftBottomCenter(currentSpawnPos, firstCornerOrientation, parent,$"({indices.y}, {indices.x})");
        currentSpawnPos.x += spawnedElementSize.x;
        roomElements[indices.y, indices.x] = newRoomElement;
        roomData.AddRoomElement(newRoomElement);

        if (isBackWall)
        {
            ConnectFrontElement(newRoomElement, roomElements, indices);
        }

        indices.x++;

        for (; indices.x < roomElements.GetLength(1) - 1; indices.x++)
        {
            (newRoomElement, spawnedElementSize) = wallSpawner.SpawnByLeftBottomCenter(currentSpawnPos, wallOrientation, parent, $"({indices.y}, {indices.x})");
            currentSpawnPos.x += spawnedElementSize.x;
            roomElements[indices.y, indices.x] = newRoomElement;
            roomData.AddRoomElement(newRoomElement);

            if (isBackWall)
            {
                ConnectFrontAndLeftElement(newRoomElement, roomElements, indices);
            }
            else
            {
                ConnectLeftElement(newRoomElement, roomElements, indices);
            }
        }
        
        (newRoomElement, _) = cornerSpawner.SpawnByLeftBottomCenter(currentSpawnPos, secondCornerOrientation, parent, $"({indices.y}, {indices.x})");
        roomElements[indices.y, indices.x] = newRoomElement;
        roomData.AddRoomElement(newRoomElement);
        
        if (isBackWall)
        {
            ConnectFrontAndLeftElement(newRoomElement, roomElements, indices);
        }
        else
        {
            ConnectLeftElement(newRoomElement, roomElements, indices);
        }
    }

    private void SpawnRoomCenter(SpawnInfo spawnInfo, ElementSpawner wallSpawner, ElementSpawner floorSpawner, RoomElement[,] roomElements, RoomData roomData, Transform parent)
    {
        RoomElement newRoomElement;
        Vector3 spawnedElementSize;
        Vector3 currentSpawnPos = spawnInfo.RoomBounds.min;
        currentSpawnPos.z += spawnInfo.RoomBounds.size.z - wallSpawner.Bounds.size.z;
        currentSpawnPos = wallSpawner.ConvertLeftBottomFrontPositionToLeftBottomCenterPosition(currentSpawnPos, SpawnOrientation.Left);
        Vector2Int indices = new Vector2Int(0, 1);
        
        for ( ; indices.y < roomElements.GetLength(0) - 1; indices.y++)
        {
     
            (newRoomElement, spawnedElementSize) = wallSpawner.SpawnByLeftBottomCenter(currentSpawnPos, SpawnOrientation.Left, parent, $"({indices.y}, {indices.x})");
            currentSpawnPos.x += spawnedElementSize.x;
            roomElements[indices.y, indices.x] = newRoomElement;
            roomData.AddRoomElement(newRoomElement);
            ConnectFrontElement(newRoomElement, roomElements, indices);
            indices.x++;

            for (; indices.x < roomElements.GetLength(1) - 1; indices.x++)
            {
                (newRoomElement, spawnedElementSize) = floorSpawner.SpawnByLeftBottomCenter(currentSpawnPos, SpawnOrientation.Front, parent, $"({indices.y}, {indices.x})");
                currentSpawnPos.x += spawnedElementSize.x;
                roomElements[indices.y, indices.x] = newRoomElement;
                roomData.AddRoomElement(newRoomElement);
                ConnectFrontAndLeftElement(newRoomElement, roomElements, indices);
            }
            
            (newRoomElement, spawnedElementSize) = wallSpawner.SpawnByLeftBottomCenter(currentSpawnPos, SpawnOrientation.Right, parent, $"({indices.y}, {indices.x})");
            currentSpawnPos.x += spawnedElementSize.x;
            roomElements[indices.y, indices.x] = newRoomElement;
            roomData.AddRoomElement(newRoomElement);
            ConnectFrontAndLeftElement(newRoomElement, roomElements, indices);
            indices.x = 0;
            currentSpawnPos.x = spawnInfo.RoomBounds.min.x;
            currentSpawnPos.z += spawnedElementSize.z;
        }
    }
    

    private void ConnectFrontAndLeftElement(RoomElement newElement, RoomElement[,] roomElements, Vector2Int indices)
    {
        ConnectLeftElement(newElement, roomElements, indices);
        ConnectFrontElement(newElement, roomElements, indices);
    }

    private void ConnectFrontElement(RoomElement newElement, RoomElement[,] roomElements, Vector2Int indices)
    {
        newElement.ConnectFrontElement(roomElements[indices.y - 1, indices.x]);
    }

    private void ConnectLeftElement(RoomElement newElement, RoomElement[,] roomElements, Vector2Int indices)
    {
        newElement.ConnectLeftElement(roomElements[indices.y, indices.x - 1]);
    }

    private class SpawnInfo
    {
        public Bounds RoomBounds { get; }
        public Vector2Int NumberOfWalls { get; }

        public SpawnInfo(Vector3 center, Vector3 roomSize, Vector2Int numberOfWalls)
        {
            NumberOfWalls = numberOfWalls;
            RoomBounds = new Bounds(center, roomSize);
        }
    }

    private class ElementSpawner
    {
        public Bounds Bounds { get; }
        private Bounds sidewaysRotatedBounds { get; }
        private GameObject toInstantiate;
        private RoomElementTyp type;

        public ElementSpawner(GameObject toInstantiate, RoomElementTyp type)
        {
            this.toInstantiate = toInstantiate;
            this.type = type;

            var meshRenderer = toInstantiate.GetComponentInChildren<MeshRenderer>();

            if (meshRenderer == null)
            {
                throw new MissingComponentException("Room elements are expected to contain a mesh renderer. The " + type.ToString() + " prefab doesn't seem to have a mesh renderer in it's hierachy. Please add one.");
            }

            Bounds = meshRenderer.bounds;
            var sizeRotatedSideways = Bounds.size;
            sizeRotatedSideways.x = Bounds.size.z;
            sizeRotatedSideways.z = Bounds.size.x;
            sidewaysRotatedBounds = new Bounds(Bounds.center, sizeRotatedSideways);
        }

        public Vector3 ConvertLeftBottomFrontPositionToLeftBottomCenterPosition(Vector3 position, SpawnOrientation orientation)
        {
            var applicableBounds = orientation.IsSideways() ? sidewaysRotatedBounds : Bounds;
            position.z -= applicableBounds.extents.z;
            return position;
        }

        public Vector3 ConvertLeftBottomBackPositionToLeftBottomCenterPosition(Vector3 position, SpawnOrientation orientation)
        {
            var applicableBounds = orientation.IsSideways() ? sidewaysRotatedBounds : Bounds;
            position.z += applicableBounds.extents.z;
            return position;
        }

        public (RoomElement roomElement, Vector3 dimensions) SpawnByLeftBottomCenter(Vector3 position, SpawnOrientation orientation, Transform parent, string name)
        {
            var applicableBounds = orientation.IsSideways() ? sidewaysRotatedBounds : Bounds;
            position.x += applicableBounds.extents.x;
            position.y += applicableBounds.extents.y;

            return (Spawn(position, orientation.ToAngle(), parent, name), applicableBounds.size);
        }

        public (RoomElement roomElement, Vector3 dimensions) SpawnByBottomLeftBack(Vector3 position, SpawnOrientation orientation, Transform parent, string name)
        {
            var applicableBounds = orientation.IsSideways() ? sidewaysRotatedBounds : Bounds;
            position.x += applicableBounds.extents.x;
            position.y += applicableBounds.extents.y;
            position.z += applicableBounds.extents.z;
            return (Spawn(position, orientation.ToAngle(), parent, name), applicableBounds.size);
        }

        private RoomElement Spawn(Vector3 position, float angle, Transform parent, string name)
        {
            var spawnedObject = (GameObject) PrefabUtility.InstantiatePrefab(toInstantiate, parent);
            spawnedObject.name = name;
            spawnedObject.transform.position = position;
            var guidComp = spawnedObject.AddComponent<PersistentInstanceId>();
            if (Mathf.Abs(angle) > 0.01f)
            {
                spawnedObject.transform.Rotate(Vector3.up, angle);
            }
       
            RoomElement roomElement = new RoomElement();
            roomElement.Type = type;
            roomElement.Guid = guidComp.Guid;
            return roomElement;
        }
    }
}

public enum SpawnOrientation
{
    Front,
    Right, 
    Left, 
    Back
}

public static class Utility
{
    public static float ToAngle(this SpawnOrientation orientation)
    {
        switch(orientation)
        {
            case SpawnOrientation.Front: return 0f;
            case SpawnOrientation.Right: return 90f;
            case SpawnOrientation.Back: return 180f;
            case SpawnOrientation.Left: return 270f;
            default:
                Debug.LogError($"Not supported orientation {orientation}.");
                return 0f;
        }
    }

    public static bool IsSideways(this SpawnOrientation orientation)
    {
        return orientation == SpawnOrientation.Left || orientation == SpawnOrientation.Right;
    }
}