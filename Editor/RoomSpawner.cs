using System;
using UnityEngine;
using UnityEditor;
using UnityLevelEditor.Model;

namespace UnityLevelEditor.RoomSpawning
{
    public class RoomSpawner : EditorWindow
    {
        private const float roomSizeLimit = 200f;
        [SerializeField] private GameObject wall;
        [SerializeField] private GameObject floor;
        [SerializeField] private GameObject corner;
        [SerializeField] private string roomName = "StandardRoom";
        [SerializeField] private Vector2 roomSize = new Vector2(1, 1);

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

            if (wall == null || floor == null || corner == null)
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

            if (!CheckBoundSizesCorrect(wallBounds, cornerBounds, floorBounds))
            {
                return;
            }
            
            var minRoomSize = wallBounds.size.x + 2 * cornerBounds.size.x;
            var maxRoomSize = AdaptRoomSize(roomSizeLimit, minRoomSize, wallBounds.size.x);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Room Size X");
            roomSize.x = AdaptRoomSize(EditorGUILayout.Slider(roomSize.x, minRoomSize, maxRoomSize), minRoomSize, wallBounds.size.x);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Room Size Z");
            roomSize.y = AdaptRoomSize(EditorGUILayout.Slider(roomSize.y, minRoomSize, maxRoomSize), minRoomSize, wallBounds.size.x);
            EditorGUILayout.EndHorizontal();
            
            if (GUILayout.Button("Create Room"))
            {
                SpawnNewRoom();
            }
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

        private bool CheckBoundSizesCorrect(Bounds wallBounds, Bounds cornerBounds, Bounds floorBounds)
        {
            if (Mathf.Abs(wallBounds.size.x - floorBounds.size.x) > 0.01f)
            {
                EditorGUILayout.HelpBox("X-direction of wall and floor has to be the same size.", MessageType.Error);
                return false;
            }

            if (Mathf.Abs(floorBounds.size.x - floorBounds.size.z) > 0.01f)
            {
                EditorGUILayout.HelpBox("Size of z-direction and x-direction needs to be the same for floor elements.", MessageType.Error);
                return false;
            }

            if (Mathf.Abs(cornerBounds.size.x - cornerBounds.size.z) > 0.01f)
            {
                EditorGUILayout.HelpBox("Size of z-direction and x-direction needs to be the same for corner elements.", MessageType.Error);
                return false;
            }

            if (Mathf.Abs(cornerBounds.size.x - wallBounds.size.z) > 0.01f)
            {
                EditorGUILayout.HelpBox("Size of z-direction of wall and size of x-direction of corner needs to be the same.", MessageType.Error);
                return false;
            }

            return true;
        }

        private float AdaptRoomSize(float roomSize, float minRoomSize, float wallSize)
        {
            return wallSize * Mathf.FloorToInt((roomSize - minRoomSize) / wallSize) + minRoomSize;
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

            SpawnInfo spawnInfo = GetSpawnInfo(wallSpawner.Bounds, cornerSpawner.Bounds);
            RoomElement[,] roomElements = new RoomElement[spawnInfo.NumberOfWalls.y + 2, spawnInfo.NumberOfWalls.x + 2];

            GameObject room = new GameObject(roomName);
            SpawnFrontOrBackOfRoom(spawnInfo, wallSpawner, cornerSpawner, roomElements, room.transform, false);
            SpawnRoomCenter(spawnInfo, wallSpawner, floorSpawner, roomElements, room.transform);
            SpawnFrontOrBackOfRoom(spawnInfo, wallSpawner, cornerSpawner, roomElements, room.transform, true);
            Debug.Log("Room created");
        }

        private SpawnInfo GetSpawnInfo(Bounds wallMeshBounds, Bounds cornerMeshBounds)
        {
            var wallSize = wallMeshBounds.size;
            var cornerSize = cornerMeshBounds.size;

            var actualRoomSize = new Vector3();
            var numberOfWalls = new Vector2Int();

            var numberOfWallsAndRoomSize = CalculateNumberOfWallsAndRoomSize(this.roomSize.x, wallSize.x, cornerSize.x, "x");
            actualRoomSize.x = numberOfWallsAndRoomSize.roomSize;
            numberOfWalls.x = numberOfWallsAndRoomSize.numberOfWalls;

            numberOfWallsAndRoomSize = CalculateNumberOfWallsAndRoomSize(this.roomSize.y, wallSize.x, cornerSize.z, "z");
            actualRoomSize.z = numberOfWallsAndRoomSize.roomSize;
            numberOfWalls.y = numberOfWallsAndRoomSize.numberOfWalls;

            actualRoomSize.y = wallMeshBounds.size.y;

            return new SpawnInfo(Vector3.zero, actualRoomSize, numberOfWalls);
        }

        private (int numberOfWalls, float roomSize) CalculateNumberOfWallsAndRoomSize(float unitsToFill, float wallSize, float cornerSize, string dimensionName)
        {
            // Guarantees to generate a room as big or smaller than the configured room size (0.01f tolerance to avoid problems due to floating point imprecision)
            var numberOfWalls = (int) ((unitsToFill + 0.01f - (2f * cornerSize)) / wallSize);

            if (numberOfWalls == 0)
            {
                Debug.LogWarning($"{dimensionName.ToUpper()}-Dimension was too small. At least one wall should be spawned. Room will therefore will be bigger than given {dimensionName.ToLower()}-value.");
                numberOfWalls = 1;
            }

            return (numberOfWalls, 2f * cornerSize + (numberOfWalls * wallSize));
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
    }
}