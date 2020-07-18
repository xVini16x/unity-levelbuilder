using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityLevelEditor.Model;
using UnityLevelEditor.RoomExtension;

namespace UnityLevelEditor.RoomSpawning
{
    public class RoomSpawner : EditorWindow
    {
        private const float RoomSizeLimit = 200f;
        
        #region Inspector Fields
        [SerializeField] private GameObject fullWall;
        [SerializeField] private GameObject fullWallBackside;
        [SerializeField] private GameObject wallShortenedLeft;
        [SerializeField] private GameObject wallShortenedRight;
        [SerializeField] private GameObject wallShortenedBothSides;
        [SerializeField] private GameObject floor;
        [SerializeField] private GameObject corner;
        [SerializeField] private GameObject cornerBackside;
        
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
            GUILayout.Label("Room Element Prefabs", EditorStyles.boldLabel);
            fullWall = EditorGUILayout.ObjectField("Full Wall", fullWall, typeof(GameObject), true) as GameObject;
            fullWallBackside = EditorGUILayout.ObjectField("Full Wall Backside", fullWallBackside, typeof(GameObject), true) as GameObject;
            wallShortenedLeft = EditorGUILayout.ObjectField("Wall Shortened Left Side", wallShortenedLeft, typeof(GameObject), true) as GameObject;
            wallShortenedRight = EditorGUILayout.ObjectField("Wall Shortened Right Side", wallShortenedRight, typeof(GameObject), true) as GameObject;
            wallShortenedBothSides = EditorGUILayout.ObjectField("Wall Shortened Both Sides", wallShortenedBothSides, typeof(GameObject), true) as GameObject;
            floor = EditorGUILayout.ObjectField("Floor Prefab", floor, typeof(GameObject), true) as GameObject;
            corner = EditorGUILayout.ObjectField("Corner Prefab", corner, typeof(GameObject), true) as GameObject;
            cornerBackside = EditorGUILayout.ObjectField("Corner Backside", cornerBackside, typeof(GameObject), true) as GameObject;
            GUILayout.Label("Room Settings", EditorStyles.boldLabel);
            roomName = EditorGUILayout.TextField("Name", roomName);

            if (!PrefabsAreAssigned())
            {
                EditorGUILayout.HelpBox("Please assign all prefabs to create a room. You can find default prefabs in the prefabs folder.", MessageType.Warning);
                return;
            }

            Dictionary<RoomElementTyp, Bounds> boundsByType;
            
            try
            {
                boundsByType = GetAllBoundsByType();
            }
            catch (MissingComponentException e)
            {
                EditorGUILayout.HelpBox(e.Message, MessageType.Error);
                return;
            }

            if (!CheckBoundSizesCorrect(boundsByType, out var message))
            {
                EditorGUILayout.HelpBox(message, MessageType.Error);
                return;
            }

            var numberOfWalls = new Vector2Int();
            // Calculate valid room sizes (in Unity Units) based on wall and corner size for creation of slider
            var fullWallBounds = boundsByType[RoomElementTyp.Wall];
            var wallXLength = fullWallBounds.size.x;
            var minRoomSize = wallXLength + 2 * boundsByType[RoomElementTyp.Corner].size.x;
            var (_, maxRoomSize) = CalculateNumberOfWallsAndRoomSize(RoomSizeLimit, wallXLength, minRoomSize);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Room Size X");
            (numberOfWalls.x, roomSize.x) = CalculateNumberOfWallsAndRoomSize(EditorGUILayout.Slider(roomSize.x, minRoomSize, maxRoomSize),wallXLength, minRoomSize);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Room Size Z");
            (numberOfWalls.y, roomSize.y) = CalculateNumberOfWallsAndRoomSize(EditorGUILayout.Slider(roomSize.y, minRoomSize, maxRoomSize), wallXLength, minRoomSize);
            EditorGUILayout.EndHorizontal();
            
            var roomSize3D = new Vector3(roomSize.x, fullWallBounds.size.y, roomSize.y);

            if (GUILayout.Button("Create Room"))
            {
                var spawnInfo = new SpawnInfo(Vector3.zero, roomSize3D, numberOfWalls);
                SpawnNewRoom(spawnInfo, boundsByType);
            }
        }
        #endregion

        #region Validation

        private GameObject GetPrefabByType(RoomElementTyp type)
        {
            switch (type)
            {
                case RoomElementTyp.Wall:
                    return fullWall;
                case RoomElementTyp.WallTransparent:
                    return fullWallBackside;
                case RoomElementTyp.WallShortenedLeft:
                    return wallShortenedLeft;
                case RoomElementTyp.WallShortenedRight:
                    return wallShortenedRight;
                case RoomElementTyp.WallShortenedBothEnds:
                    return wallShortenedBothSides;
                case RoomElementTyp.Floor:
                    return floor;
                case RoomElementTyp.Corner:
                    return corner;
                case RoomElementTyp.CornerTransparent:
                    return cornerBackside;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, $"GetPrefabByType is not supported for type {type} as of current.");
            }
        }

        private Dictionary<RoomElementTyp, Bounds> GetAllBoundsByType()
        {
            var dict = new Dictionary<RoomElementTyp, Bounds>();
            
            foreach (var type in Enum.GetValues(typeof(RoomElementTyp)).Cast<RoomElementTyp>())
            {
                dict[type] = GetBoundsOfPrefab(GetPrefabByType(type), type);
            }

            return dict;
        }
        
        private bool PrefabsAreAssigned()
        {
            return fullWall && fullWallBackside && wallShortenedLeft && wallShortenedRight && wallShortenedBothSides && floor && corner && cornerBackside;
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

        private bool CheckBoundSizesCorrect(Dictionary<RoomElementTyp, Bounds> boundsByType, out string message)
        {
            var sameXZTypes = new[] {RoomElementTyp.Corner, RoomElementTyp.CornerTransparent, RoomElementTyp.Floor};

            foreach(var type in sameXZTypes)
            {
                if (!CheckXZSameSize(boundsByType[type], boundsByType.ToString(), out message))
                {
                    return false;
                }
            }

            var wallTypes = new[] { RoomElementTyp.CornerTransparent, RoomElementTyp.Wall, RoomElementTyp.WallTransparent, RoomElementTyp.WallShortenedLeft, RoomElementTyp.WallShortenedRight, RoomElementTyp.WallShortenedBothEnds};

            var cornerZ = boundsByType[RoomElementTyp.Corner].size.z;
            var tempMessage = $"The following elements need to have the same z-value: {RoomElementTyp.Corner} ({cornerZ:F}) ";

            bool failed = false;
            
            foreach (var type in wallTypes)
            {
                var size = boundsByType[type].size.z;
                failed = Mathf.Abs(cornerZ - size) > 0.01f;
                tempMessage += $"{type} ({size:F})";
            }

            if (failed)
            {
                message = tempMessage;
                return false;
            }

            var wallX = boundsByType[RoomElementTyp.Wall].size.x;
            var floorX = boundsByType[RoomElementTyp.Floor].size.x;
            
            if (Mathf.Abs(wallX - floorX) > 0.01f)
            {
                message = $"X-direction of wall ({wallX:F}) and floor ({floorX:F}) has to be the same size.";
                return false;
            }

            var wallShortenedLeftX = boundsByType[RoomElementTyp.WallShortenedLeft].size.x;
            var wallShortenedRightX = boundsByType[RoomElementTyp.WallShortenedRight].size.x;
            var expectedWallLength = wallX - cornerZ;
            
            if (Mathf.Abs(wallShortenedLeftX - expectedWallLength) > 0.01f ||
                Mathf.Abs(wallShortenedRightX - expectedWallLength) > 0.01f)
            {
                message = $"X size of wall shortened in left direction ({wallShortenedLeftX:F}) and X size of wall shortened in right direction ({wallShortenedRightX:F}) "
                          + $"needs to be the same length of the fullWall minus the length of the corner ({expectedWallLength:F})";
                return false;
            }

            var wallShortenedBothSidesX = boundsByType[RoomElementTyp.WallShortenedBothEnds].size.x;
            expectedWallLength = wallX - (2 * cornerZ);

            if (Mathf.Abs(wallShortenedBothSidesX - expectedWallLength) > 0.01f)
            {
                message = $"X size of wall shortened on both sides ({wallShortenedBothSidesX:F}) needs to be the same as full wall minus twice the length of the corner ({expectedWallLength:F})";
                return false;
            }
            
            message = null;
            return true;
        }

        private bool CheckXZSameSize(Bounds bounds, string roomElementName, out string message)
        {
            if (Mathf.Abs(bounds.size.x - bounds.size.z) > 0.01f)
            {
                message = $"Size of z-direction ({bounds.size.z:F}) and x-direction ({bounds.size.x:F}) needs to be the same for {roomElementName} prefab.";
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
        private void SpawnNewRoom(SpawnInfo spawnInfo, Dictionary<RoomElementTyp, Bounds> boundsByType)
        {
            var elementSpawnerByType = GetElementSpawnerByType(boundsByType);
            
            var roomElements = new RoomElement[spawnInfo.NumberOfWalls.y + 2, spawnInfo.NumberOfWalls.x + 2];

            var room = new GameObject(roomName);
            
            var eroom = room.AddComponent<ExtendableRoom>();
            eroom.SetElementSpawner(elementSpawnerByType);

            var wallSpawner = elementSpawnerByType[RoomElementTyp.Wall];
            var cornerSpawner = elementSpawnerByType[RoomElementTyp.Corner];
            var floorSpawner = elementSpawnerByType[RoomElementTyp.Floor];
            var backWallSpawner = elementSpawnerByType[RoomElementTyp.WallTransparent];
            var backCornerSpawner = elementSpawnerByType[RoomElementTyp.CornerTransparent];

            SpawnFrontOrBackOfRoom(spawnInfo, wallSpawner, cornerSpawner, roomElements, eroom, false);
            SpawnRoomCenter(spawnInfo, wallSpawner, floorSpawner, roomElements, eroom);
            SpawnFrontOrBackOfRoom(spawnInfo, backWallSpawner, backCornerSpawner, roomElements, eroom, true);

            Undo.RegisterCreatedObjectUndo(room, "Room Creation");
            Debug.Log("Room created");
        }

        private Dictionary<RoomElementTyp, ElementSpawner> GetElementSpawnerByType(Dictionary<RoomElementTyp, Bounds> boundsByType)
        {
            var dict = new Dictionary<RoomElementTyp, ElementSpawner>();

            foreach (var type in boundsByType.Keys)
            {
                dict[type] = new ElementSpawner(GetPrefabByType(type), boundsByType[type], type);
            }

            return dict;
        }
        
        private void SpawnFrontOrBackOfRoom(SpawnInfo spawnInfo, ElementSpawner wallSpawner, ElementSpawner cornerSpawner, RoomElement[,] roomElements, ExtendableRoom parent, bool isBackWall)
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

        private void SpawnRoomCenter(SpawnInfo spawnInfo, ElementSpawner wallSpawner, ElementSpawner floorSpawner, RoomElement[,] roomElements, ExtendableRoom parent)
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