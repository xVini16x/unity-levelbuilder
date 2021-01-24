using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityLevelEditor.Model;
using UnityLevelEditor.RoomExtension;

namespace UnityLevelEditor.RoomSpawning
{
    public class RoomSpawner : EditorWindow
    {
        private const int RoomSizeLimit = 100;
        [SerializeField] private bool loadedDefaultValues;

        #region Inspector Fields

        [SerializeField] private GameObject fullWall;
        [SerializeField] private GameObject wallShortenedLeft;
        [SerializeField] private GameObject wallShortenedRight;
        [SerializeField] private GameObject wallShortenedBothSides;
        [SerializeField] private GameObject floor;
        [SerializeField] private GameObject corner;
        [SerializeField] private Material transparentMaterial;
        [SerializeField] private Material wallSideMaterial;

        [SerializeField] private string roomName = "StandardRoom";
        [SerializeField] private Vector2Int roomSize = new Vector2Int(1, 1);

        #endregion

        #region UI

        #region Window Creation

        private void OnFocus()
        {
            if (loadedDefaultValues)
            {
                return;
            }

            loadedDefaultValues = true;
            var levelBuilderSettings = LevelBuilderSettings.GetSerializedSettings();
            fullWall = levelBuilderSettings.FindProperty("fullWall").objectReferenceValue as GameObject;
            wallShortenedLeft = levelBuilderSettings.FindProperty("wallShortenedLeft").objectReferenceValue as GameObject;
            wallShortenedRight = levelBuilderSettings.FindProperty("wallShortenedRight").objectReferenceValue as GameObject;
            wallShortenedBothSides = levelBuilderSettings.FindProperty("wallShortenedBothSides").objectReferenceValue as GameObject;
            floor = levelBuilderSettings.FindProperty("floor").objectReferenceValue as GameObject;
            corner = levelBuilderSettings.FindProperty("corner").objectReferenceValue as GameObject;
            transparentMaterial = levelBuilderSettings.FindProperty("transparentMaterial").objectReferenceValue as Material;
            wallSideMaterial = levelBuilderSettings.FindProperty("wallSideMaterial").objectReferenceValue as Material;
            roomName = levelBuilderSettings.FindProperty("roomName").stringValue;
            roomSize = levelBuilderSettings.FindProperty("roomSize").vector2IntValue;
        }

        // Add menu named "My Window" to the Window menu
        [MenuItem("Window/Unity Levelbuilder Tool")]
        private static void Init()
        {
            // Get existing open window or if none, make a new one:
            var window = (RoomSpawner) EditorWindow.GetWindow(typeof(RoomSpawner));
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("Room Element Prefabs", EditorStyles.boldLabel);
            fullWall = EditorGUILayout.ObjectField("Full Wall", fullWall, typeof(GameObject), true) as GameObject;
            wallShortenedLeft = EditorGUILayout.ObjectField("Wall Shortened Left Side", wallShortenedLeft, typeof(GameObject), true) as GameObject;
            wallShortenedRight = EditorGUILayout.ObjectField("Wall Shortened Right Side", wallShortenedRight, typeof(GameObject), true) as GameObject;
            wallShortenedBothSides = EditorGUILayout.ObjectField("Wall Shortened Both Sides", wallShortenedBothSides, typeof(GameObject), true) as GameObject;
            floor = EditorGUILayout.ObjectField("Floor Prefab", floor, typeof(GameObject), true) as GameObject;
            corner = EditorGUILayout.ObjectField("Corner Prefab", corner, typeof(GameObject), true) as GameObject;
            transparentMaterial = EditorGUILayout.ObjectField("Transparent Material", transparentMaterial, typeof(Material), false) as Material;
            wallSideMaterial = EditorGUILayout.ObjectField("Wall Side Material", wallSideMaterial, typeof(Material), false) as Material;

            GUILayout.Label("Room Settings", EditorStyles.boldLabel);
            roomName = EditorGUILayout.TextField("Name", roomName);

            if (!PrefabsAreAssigned())
            {
                EditorGUILayout.HelpBox("Please assign all prefabs to create a room. You can find default prefabs in the prefabs folder.", MessageType.Warning);
                return;
            }

            Dictionary<RoomElementType, Bounds> boundsByType;

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

            // Calculate valid room sizes (in Unity Units) based on wall and corner size for creation of slider
            var fullWallBounds = boundsByType[RoomElementType.Wall];
            var wallXLength = fullWallBounds.size.x;
            var minRoomSize = wallXLength + 2 * boundsByType[RoomElementType.Corner].size.x;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Room Size X");
            roomSize.x = EditorGUILayout.IntSlider(roomSize.x, 1, RoomSizeLimit);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Room Size Z");
            roomSize.y = EditorGUILayout.IntSlider(roomSize.y, 1, RoomSizeLimit);
            EditorGUILayout.EndHorizontal();

            var roomSize3D = new Vector3(minRoomSize + ((roomSize.x - 1) * wallXLength), fullWallBounds.size.y, minRoomSize + ((roomSize.y - 1) * wallXLength));

            if (GUILayout.Button("Create Room"))
            {
                var roomCenter = Vector3.zero;
                roomCenter.y = fullWallBounds.extents.y - boundsByType[RoomElementType.Floor].size.y;
                var spawnInfo = new SpawnInfo(roomCenter, roomSize3D, roomSize);
                SpawnNewRoom(spawnInfo, boundsByType);
            }
        }

        #endregion

        #region Validation

        private GameObject GetPrefabByType(RoomElementType type)
        {
            switch (type)
            {
                case RoomElementType.Wall:
                    return fullWall;
                case RoomElementType.WallShortenedLeft:
                    return wallShortenedLeft;
                case RoomElementType.WallShortenedRight:
                    return wallShortenedRight;
                case RoomElementType.WallShortenedBothEnds:
                    return wallShortenedBothSides;
                case RoomElementType.Floor:
                    return floor;
                case RoomElementType.Corner:
                    return corner;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, $"GetPrefabByType is not supported for type {type} as of current.");
            }
        }

        private Dictionary<RoomElementType, Bounds> GetAllBoundsByType()
        {
            var dict = new Dictionary<RoomElementType, Bounds>();

            foreach (var type in Enum.GetValues(typeof(RoomElementType)).Cast<RoomElementType>())
            {
                dict[type] = GetBoundsOfPrefab(GetPrefabByType(type), type);
            }

            return dict;
        }

        private bool PrefabsAreAssigned()
        {
            return fullWall && wallShortenedLeft && wallShortenedRight && wallShortenedBothSides && floor && corner;
        }

        private Bounds GetBoundsOfPrefab(GameObject prefab, RoomElementType type)
        {
            var meshRenderer = prefab.GetComponentInChildren<MeshRenderer>();

            if (meshRenderer == null)
            {
                throw new MissingComponentException("Room elements are expected to contain a mesh renderer. The " + type.ToString() + " prefab doesn't seem to have a mesh renderer in it's hierarchy. Please add one.");
            }

            return meshRenderer.bounds;
        }

        private bool CheckBoundSizesCorrect(Dictionary<RoomElementType, Bounds> boundsByType, out string message)
        {
            var sameXZTypes = new[] {RoomElementType.Corner, RoomElementType.Floor};

            foreach (var type in sameXZTypes)
            {
                if (!CheckXZSameSize(boundsByType[type], boundsByType.ToString(), out message))
                {
                    return false;
                }
            }

            var wallTypes = new[] {RoomElementType.Wall, RoomElementType.WallShortenedLeft, RoomElementType.WallShortenedRight, RoomElementType.WallShortenedBothEnds};

            var cornerZ = boundsByType[RoomElementType.Corner].size.z;
            var tempMessage = $"The following elements need to have the same z-value: {RoomElementType.Corner} ({cornerZ:F}) ";

            var failed = false;

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

            var wallX = boundsByType[RoomElementType.Wall].size.x;
            var floorX = boundsByType[RoomElementType.Floor].size.x;

            if (Mathf.Abs(wallX - floorX) > 0.01f)
            {
                message = $"X-direction of wall ({wallX:F}) and floor ({floorX:F}) has to be the same size.";
                return false;
            }

            var wallShortenedLeftX = boundsByType[RoomElementType.WallShortenedLeft].size.x;
            var wallShortenedRightX = boundsByType[RoomElementType.WallShortenedRight].size.x;
            var expectedWallLength = wallX - cornerZ;

            if (Mathf.Abs(wallShortenedLeftX - expectedWallLength) > 0.01f ||
                Mathf.Abs(wallShortenedRightX - expectedWallLength) > 0.01f)
            {
                message = $"X size of wall shortened in left direction ({wallShortenedLeftX:F}) and X size of wall shortened in right direction ({wallShortenedRightX:F}) "
                          + $"needs to be the same length of the fullWall minus the length of the corner ({expectedWallLength:F})";
                return false;
            }

            var wallShortenedBothSidesX = boundsByType[RoomElementType.WallShortenedBothEnds].size.x;
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

        #endregion

        #endregion

        #region Room Creation

        #region Element Spawning

        private void SpawnNewRoom(SpawnInfo spawnInfo, Dictionary<RoomElementType, Bounds> boundsByType)
        {
            var elementSpawnerByType = GetElementSpawnerByType(boundsByType);

            var roomElements = new RoomElement[spawnInfo.NumberOfWalls.x + 2, spawnInfo.NumberOfWalls.y + 2];

            var room = new GameObject(roomName);

            var extendableRoom = room.AddComponent<ExtendableRoom>();
            extendableRoom.SetElementSpawner(elementSpawnerByType);
            extendableRoom.FloorGridDictionary = new FloorGridDictionary();
            extendableRoom.TransparentMaterial = transparentMaterial;
            extendableRoom.WallSideMaterial = wallSideMaterial;

            var wallSpawner = elementSpawnerByType[RoomElementType.Wall];
            var cornerSpawner = elementSpawnerByType[RoomElementType.Corner];
            var floorSpawner = elementSpawnerByType[RoomElementType.Floor];

            SpawnFrontOrBackOfRoom(spawnInfo, wallSpawner, cornerSpawner, roomElements, extendableRoom, false);
            SpawnRoomCenter(spawnInfo, wallSpawner, floorSpawner, roomElements, extendableRoom);
            SpawnFrontOrBackOfRoom(spawnInfo, wallSpawner, cornerSpawner, roomElements, extendableRoom, true);


            Undo.RegisterCreatedObjectUndo(room, "Room Creation");
            Undo.SetCurrentGroupName("Room Creation");
            Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
        }

        private Dictionary<RoomElementType, ElementSpawner> GetElementSpawnerByType(Dictionary<RoomElementType, Bounds> boundsByType)
        {
            var dict = new Dictionary<RoomElementType, ElementSpawner>();

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
                indices = new Vector2Int(0, 0);
                wallOrientation = SpawnOrientation.Back;
                firstCornerOrientation = SpawnOrientation.Front;
                secondCornerOrientation = SpawnOrientation.Left;
                currentSpawnPos = spawnInfo.RoomBounds.min;
                currentSpawnPos = cornerSpawner.ConvertLeftBottomBackPositionToLeftBottomCenterPosition(currentSpawnPos, wallOrientation);
            }
            else
            {
                indices = new Vector2Int(0, roomElements.GetLength(1) - 1);
                wallOrientation = SpawnOrientation.Front;
                firstCornerOrientation = SpawnOrientation.Right;
                secondCornerOrientation = SpawnOrientation.Back;
                currentSpawnPos = spawnInfo.RoomBounds.min;
                currentSpawnPos.z = spawnInfo.RoomBounds.max.z;
                currentSpawnPos = cornerSpawner.ConvertLeftBottomFrontPositionToLeftBottomCenterPosition(currentSpawnPos, wallOrientation);
            }

            (newRoomElement, spawnedElementSize) = cornerSpawner.SpawnByLeftBottomCenter(currentSpawnPos, firstCornerOrientation, parent);
            currentSpawnPos.x += spawnedElementSize.x;
            roomElements[indices.x, indices.y] = newRoomElement;

            if (isBackWall)
            {
                newRoomElement.SetAllMaterialsTransparent();
                ConnectFrontElement(newRoomElement, roomElements, indices);
            }

            indices.x++;

            for (; indices.x < roomElements.GetLength(0) - 1; indices.x++)
            {
                (newRoomElement, spawnedElementSize) = wallSpawner.SpawnByLeftBottomCenter(currentSpawnPos, wallOrientation, parent);
                currentSpawnPos.x += spawnedElementSize.x;
                roomElements[indices.x, indices.y] = newRoomElement;

                if (isBackWall)
                {
                    newRoomElement.SetTransparentMaterial(MaterialSlotType.Top);
                    newRoomElement.SetTransparentMaterial(MaterialSlotType.Back);
                    ConnectFrontAndLeftElement(newRoomElement, roomElements, indices);
                }
                else
                {
                    ConnectLeftElement(newRoomElement, roomElements, indices);
                }
            }

            (newRoomElement, _) = cornerSpawner.SpawnByLeftBottomCenter(currentSpawnPos, secondCornerOrientation, parent);
            roomElements[indices.x, indices.y] = newRoomElement;

            if (isBackWall)
            {
                newRoomElement.SetAllMaterialsTransparent();
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
            var currentSpawnPos = spawnInfo.RoomBounds.min;
            currentSpawnPos.z += spawnInfo.RoomBounds.size.z - wallSpawner.Bounds.size.z;
            currentSpawnPos = wallSpawner.ConvertLeftBottomFrontPositionToLeftBottomCenterPosition(currentSpawnPos, SpawnOrientation.Left);
            // -2 for y index because last row was already spawned
            var indices = new Vector2Int(0, roomElements.GetLength(1) - 2);
            var floorOffset = new Vector2Int(1, 1);

            for (; indices.y >= 1; indices.y--)
            {
                (newRoomElement, spawnedElementSize) = wallSpawner.SpawnByLeftBottomCenter(currentSpawnPos, SpawnOrientation.Left, parent);

                currentSpawnPos.x += spawnedElementSize.x;
                roomElements[indices.x, indices.y] = newRoomElement;
                ConnectFrontElement(newRoomElement, roomElements, indices);

                // Wall that's in first row on the left
                if (indices.y == 1)
                {
                    newRoomElement.SetWallSideMaterial(MaterialSlotType.Left);
                }

                indices.x++;

                for (; indices.x < roomElements.GetLength(0) - 1; indices.x++)
                {
                    (newRoomElement, spawnedElementSize) = floorSpawner.SpawnByLeftBottomCenter(currentSpawnPos, SpawnOrientation.Front, parent);
                    var floorElement = newRoomElement as FloorElement;
                    var gridPosition = indices - floorOffset;
                    // ReSharper disable once PossibleNullReferenceException - we always spawn floor here
                    floorElement.GridPosition = gridPosition;
                    parent.FloorGridDictionary.Add(gridPosition, floorElement);
                    currentSpawnPos.x += spawnedElementSize.x;
                    roomElements[indices.x, indices.y] = newRoomElement;
                    ConnectFrontAndLeftElement(newRoomElement, roomElements, indices);
                }

                (newRoomElement, spawnedElementSize) = wallSpawner.SpawnByLeftBottomCenter(currentSpawnPos, SpawnOrientation.Right, parent);
                currentSpawnPos.x += spawnedElementSize.x;
                roomElements[indices.x, indices.y] = newRoomElement;
                ConnectFrontAndLeftElement(newRoomElement, roomElements, indices);
                indices.x = 0;
                currentSpawnPos.x = spawnInfo.RoomBounds.min.x;
                currentSpawnPos.z -= spawnedElementSize.z;

                // Wall that's in first row on the right
                if (indices.y == 1)
                {
                    newRoomElement.SetWallSideMaterial(MaterialSlotType.Right);
                }
            }
        }

        private class SpawnInfo
        {
            public Bounds RoomBounds { get; }
            public Vector2Int NumberOfWalls { get; }

            public SpawnInfo(Vector3 center, Vector3 size, Vector2Int numberOfWalls)
            {
                RoomBounds = new Bounds(center, size);
                NumberOfWalls = numberOfWalls;
            }
        }

        #endregion

        #region Connecting RoomElement References

        private static void ConnectFrontAndLeftElement(RoomElement newElement, RoomElement[,] roomElements, Vector2Int indices)
        {
            ConnectLeftElement(newElement, roomElements, indices);
            ConnectFrontElement(newElement, roomElements, indices);
        }

        private static void ConnectFrontElement(RoomElement newElement, RoomElement[,] roomElements, Vector2Int indices)
        {
            newElement.ConnectFrontElement(roomElements[indices.x, indices.y + 1]);
        }

        private static void ConnectLeftElement(RoomElement newElement, RoomElement[,] roomElements, Vector2Int indices)
        {
            newElement.ConnectLeftElement(roomElements[indices.x - 1, indices.y]);
        }

        #endregion

        #endregion
    }
}