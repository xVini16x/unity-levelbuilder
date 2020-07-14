using UnityEngine;
using UnityEditor;
using UnityLevelEditor.Model;

namespace UnityLevelEditor.RoomSpawning
{
    public class RoomSpawner : EditorWindow
    {
        private const float RoomSizeLimit = 200f;
        
        #region Inspector Fields
        [SerializeField] private GameObject wall;
        [SerializeField] private GameObject floor;
        [SerializeField] private GameObject corner;
        [SerializeField] private string roomName = "StandardRoom";
        [SerializeField] private Vector2 roomSize = new Vector2(1, 1);
        #endregion

        #region UI
        #region Window Creation
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

            if (!PrefabsAreAssigned())
            {
                EditorGUILayout.HelpBox("Please assign all prefabs to create a room. You can find default prefabs in the prefabs folder.", MessageType.Warning);
                return;
            }

            Bounds wallBounds, cornerBounds, floorBounds;
            
            try
            { 
                wallBounds = GetBoundsOfPrefab(wall, RoomElementTyp.Wall);
                cornerBounds = GetBoundsOfPrefab(corner, RoomElementTyp.Corner);
                floorBounds = GetBoundsOfPrefab(floor, RoomElementTyp.Floor);
            }
            catch (MissingComponentException e)
            {
                EditorGUILayout.HelpBox(e.Message, MessageType.Error);
                return;
            }

            if (!CheckBoundSizesCorrect(wallBounds, cornerBounds, floorBounds, out var message))
            {
                EditorGUILayout.HelpBox(message, MessageType.Error);
                return;
            }

            var numberOfWalls = new Vector2Int();
            // Calculate valid room sizes (in Unity Units) based on wall and corner size for creation of slider
            var minRoomSize = wallBounds.size.x + 2 * cornerBounds.size.x;
            var (_, maxRoomSize) = CalculateNumberOfWallsAndRoomSize(RoomSizeLimit, wallBounds.size.x, minRoomSize);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Room Size X");
            (numberOfWalls.x, roomSize.x) = CalculateNumberOfWallsAndRoomSize(EditorGUILayout.Slider(roomSize.x, minRoomSize, maxRoomSize),wallBounds.size.x, minRoomSize);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Room Size Z");
            (numberOfWalls.y, roomSize.y) = CalculateNumberOfWallsAndRoomSize(EditorGUILayout.Slider(roomSize.y, minRoomSize, maxRoomSize), wallBounds.size.x, minRoomSize);
            EditorGUILayout.EndHorizontal();
            
            var roomSize3D = new Vector3(roomSize.x, wallBounds.size.y, roomSize.y);

            if (GUILayout.Button("Create Room"))
            {
                var spawnInfo = new SpawnInfo(Vector3.zero, roomSize3D, numberOfWalls);
                SpawnNewRoom(spawnInfo, wallBounds, cornerBounds, floorBounds);
            }
        }
        #endregion

        #region Validation
        
        private bool PrefabsAreAssigned()
        {
            if (wall == null || floor == null || corner == null)
            {
                return false;
            }

            return true;
        }
        
        private Bounds GetBoundsOfPrefab(GameObject prefab, RoomElementTyp type)
        {
            MeshRenderer meshRenderer = prefab.GetComponentInChildren<MeshRenderer>();
            
            if (meshRenderer == null)
            {
                throw new MissingComponentException("Room elements are expected to contain a mesh renderer. The " + type.ToString() + " prefab doesn't seem to have a mesh renderer in it's hierarchy. Please add one.");
            }

            return meshRenderer.bounds;
        }

        private bool CheckBoundSizesCorrect(Bounds wallBounds, Bounds cornerBounds, Bounds floorBounds, out string message)
        {
            if (Mathf.Abs(wallBounds.size.x - floorBounds.size.x) > 0.01f)
            {
                message = "X-direction of wall and floor has to be the same size.";
                return false;
            }

            if (Mathf.Abs(floorBounds.size.x - floorBounds.size.z) > 0.01f)
            {
                message = "Size of z-direction and x-direction needs to be the same for floor elements.";
                return false;
            }

            if (Mathf.Abs(cornerBounds.size.x - cornerBounds.size.z) > 0.01f)
            {
                message = "Size of z-direction and x-direction needs to be the same for corner elements.";
                return false;
            }

            if (Mathf.Abs(cornerBounds.size.x - wallBounds.size.z) > 0.01f)
            {
                message = "Size of z-direction of wall and size of x-direction of corner needs to be the same.";
                return false;
            }

            message = null;
            return true;
        }
        
        private (int numberOfWalls, float roomSize) CalculateNumberOfWallsAndRoomSize(float desiredRoomSize, float wallSize, float minRoomSize)
        {
            var numberOfWalls = Mathf.FloorToInt((desiredRoomSize - minRoomSize) / wallSize);
            var actualRoomSize = minRoomSize + (numberOfWalls * wallSize);
            
            return (++numberOfWalls, actualRoomSize);
        }
        
        #endregion
        #endregion
        
        #region Room Creation
        #region Element Spawning
        private void SpawnNewRoom(SpawnInfo spawnInfo, Bounds wallBounds, Bounds cornerBounds, Bounds floorBounds)
        {
            ElementSpawner wallSpawner, cornerSpawner, floorSpawner;
            wallSpawner = new ElementSpawner(wall, wallBounds, RoomElementTyp.Wall);
            cornerSpawner = new ElementSpawner(corner, cornerBounds, RoomElementTyp.Corner);
            floorSpawner = new ElementSpawner(floor, floorBounds, RoomElementTyp.Floor);
           
            
            RoomElement[,] roomElements = new RoomElement[spawnInfo.NumberOfWalls.y + 2, spawnInfo.NumberOfWalls.x + 2];

            GameObject room = new GameObject(roomName);
            SpawnFrontOrBackOfRoom(spawnInfo, wallSpawner, cornerSpawner, roomElements, room.transform, false);
            SpawnRoomCenter(spawnInfo, wallSpawner, floorSpawner, roomElements, room.transform);
            SpawnFrontOrBackOfRoom(spawnInfo, wallSpawner, cornerSpawner, roomElements, room.transform, true);
            
            Undo.RegisterCreatedObjectUndo(room, "Room Creation");
            Debug.Log("Room created");
        }


        private void SpawnFrontOrBackOfRoom(SpawnInfo spawnInfo, ElementSpawner wallSpawner, ElementSpawner cornerSpawner, RoomElement[,] roomElements, Transform parent, bool isBackWall)
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

            (newRoomElement, spawnedElementSize) = cornerSpawner.SpawnByLeftBottomCenter(currentSpawnPos, firstCornerOrientation, parent, $"({indices.y}, {indices.x})");
            currentSpawnPos.x += spawnedElementSize.x;
            roomElements[indices.y, indices.x] = newRoomElement;

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

            if (isBackWall)
            {
                ConnectFrontAndLeftElement(newRoomElement, roomElements, indices);
            }
            else
            {
                ConnectLeftElement(newRoomElement, roomElements, indices);
            }
        }

        private void SpawnRoomCenter(SpawnInfo spawnInfo, ElementSpawner wallSpawner, ElementSpawner floorSpawner, RoomElement[,] roomElements, Transform parent)
        {
            RoomElement newRoomElement;
            Vector3 spawnedElementSize;
            Vector3 currentSpawnPos = spawnInfo.RoomBounds.min;
            currentSpawnPos.z += spawnInfo.RoomBounds.size.z - wallSpawner.Bounds.size.z;
            currentSpawnPos = wallSpawner.ConvertLeftBottomFrontPositionToLeftBottomCenterPosition(currentSpawnPos, SpawnOrientation.Left);
            Vector2Int indices = new Vector2Int(0, 1);

            for (; indices.y < roomElements.GetLength(0) - 1; indices.y++)
            {

                (newRoomElement, spawnedElementSize) = wallSpawner.SpawnByLeftBottomCenter(currentSpawnPos, SpawnOrientation.Left, parent, $"({indices.y}, {indices.x})");
                currentSpawnPos.x += spawnedElementSize.x;
                roomElements[indices.y, indices.x] = newRoomElement;
                ConnectFrontElement(newRoomElement, roomElements, indices);
                indices.x++;

                for (; indices.x < roomElements.GetLength(1) - 1; indices.x++)
                {
                    (newRoomElement, spawnedElementSize) = floorSpawner.SpawnByLeftBottomCenter(currentSpawnPos, SpawnOrientation.Front, parent, $"({indices.y}, {indices.x})");
                    currentSpawnPos.x += spawnedElementSize.x;
                    roomElements[indices.y, indices.x] = newRoomElement;
                    ConnectFrontAndLeftElement(newRoomElement, roomElements, indices);
                }

                (newRoomElement, spawnedElementSize) = wallSpawner.SpawnByLeftBottomCenter(currentSpawnPos, SpawnOrientation.Right, parent, $"({indices.y}, {indices.x})");
                currentSpawnPos.x += spawnedElementSize.x;
                roomElements[indices.y, indices.x] = newRoomElement;
                ConnectFrontAndLeftElement(newRoomElement, roomElements, indices);
                indices.x = 0;
                currentSpawnPos.x = spawnInfo.RoomBounds.min.x;
                currentSpawnPos.z -= spawnedElementSize.z;
            }
        }

        private class SpawnInfo
        {
            public Bounds RoomBounds { get; set; }
            public Vector2Int NumberOfWalls { get; set; }

            public SpawnInfo(Vector3 center, Vector3 size, Vector2Int numberOfWalls)
            {
                RoomBounds = new Bounds(center, size);
                NumberOfWalls = numberOfWalls;
            }
        }
        #endregion
        
        #region Connecting RoomElement References
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
        #endregion
        #endregion
        
    }
}