using System;

using UnityEditor;

using UnityEngine;

namespace UnityLevelEditor.RoomExtension
{
    using Model;
    
    public class ExtendableRoom : MonoBehaviour
    {
        [field: SerializeField, HideInInspector]
        public FloorGridDictionary FloorGridDictionary { get; set; }
        
        [field: SerializeField, HideInInspector] 
        public PrefabsPerSide FullWall { get; set; }
  
        [field: SerializeField, HideInInspector] 
        public PrefabsPerSide WallShortenedLeft { get; set; }
        [field: SerializeField, HideInInspector]
        public PrefabsPerSide WallShortenedRight { get; set; }
        [field: SerializeField, HideInInspector] 
        public PrefabsPerSide WallShortenedBothSides { get; set; }
        [field: SerializeField, HideInInspector] 
        public RoomElementSpawnSettings Floor { get; set; }
        [field: SerializeField, HideInInspector] 
        public RoomElementSpawnSettings OuterCorner { get; set; }
        [field: SerializeField, HideInInspector] 
        public RoomElementSpawnSettings InnerCorner { get; set; }
        [field: SerializeField, HideInInspector] 
        public MaterialSlotSetup MaterialSlotSetup { get; internal set; }

        [field: SerializeField, HideInInspector] 
        public float FloorSize { get; set; }
        
        #if UNITY_EDITOR
        
        public void Spawn(Vector2Int floorTilePosition, bool firstCall = true)
        {
            if (FloorGridDictionary.TryGetValue(floorTilePosition, out var floorElement))
            {
                Undo.RecordObject(floorElement, "");
                floorElement.DeleteAllNeighbors();
            }
            else
            {
                floorElement = SpawnFloor(floorTilePosition);
            }
            
            SpawnWalls(floorElement);

            //Respawning floors around original spawned floor
            if (firstCall)
            {
                foreach (Direction direction in Enum.GetValues(typeof(Direction)))
                {
                    var tileInDirection = floorTilePosition + direction.AsVector2Int();
                    
                    if(FloorGridDictionary.ContainsKey(tileInDirection))
                    {
                        Spawn(tileInDirection, false);
                    }

                    tileInDirection = tileInDirection + direction.Shift(1).AsVector2Int();
                    
                    if(FloorGridDictionary.ContainsKey(tileInDirection))
                    {
                        Spawn(tileInDirection, false);
                    }
                }
            }
        }
        
        private void SpawnWalls(FloorElement floorElement)
        {
            var floorTilePosition = floorElement.GridPosition;

            foreach (Direction direction in Enum.GetValues(typeof(Direction)))
            {
                var tileInDirection = floorTilePosition + direction.AsVector2Int();
                
                if (FloorGridDictionary.ContainsKey(tileInDirection))
                {
                    //Spawn Nothing
                    continue;
                }
                
                var tileDiagonalClockwiseDirection = tileInDirection + direction.Shift(1).AsVector2Int();
                var tileDiagonalCounterClockWiseDirection = tileInDirection + direction.Shift(-1).AsVector2Int();

                var hasTileInDiagonalClockwiseDirection = FloorGridDictionary.ContainsKey(tileDiagonalClockwiseDirection);
                var hasTileInDiagonalCounterClockwiseDirection = FloorGridDictionary.ContainsKey(tileDiagonalCounterClockWiseDirection);

                WallElement wallElement = null;
                RoomElementType? cornerType = null; 
                
                if (hasTileInDiagonalClockwiseDirection && hasTileInDiagonalCounterClockwiseDirection)
                {
                    //Wall2
                    wallElement = SpawnWall(floorTilePosition, RoomElementType.WallShortenedBothEnds, direction, WallShortenedBothSides);
                    cornerType = RoomElementType.InnerCorner;
                }
                else if (hasTileInDiagonalClockwiseDirection)
                {
                    // Wall3ShortenedRight
                    wallElement = SpawnWall(floorTilePosition, RoomElementType.WallShortenedRight, direction, WallShortenedRight);
                    cornerType = RoomElementType.InnerCorner;
                }
                else if (hasTileInDiagonalCounterClockwiseDirection)
                {
                    //Wall3shortenedLeft
                    wallElement = SpawnWall(floorTilePosition, RoomElementType.WallShortenedLeft, direction, WallShortenedLeft);
                    cornerType = RoomElementType.OuterCorner;
                }
                else
                {
                    //wall4
                    wallElement = SpawnWall(floorTilePosition, RoomElementType.Wall, direction, FullWall);

                    var tileClockwiseDirection = floorTilePosition + direction.Shift(1).AsVector2Int();

                    if (!FloorGridDictionary.ContainsKey(tileClockwiseDirection))
                    {
                        cornerType = RoomElementType.OuterCorner;
                    }
                }

                if (cornerType != null)
                {
                    var cornerDirection = direction.ClockwiseDiagonalDirection();
                    var corner = SpawnCorner(floorTilePosition, cornerType.Value, cornerDirection);
                    floorElement.CornerPerDirection[cornerDirection] = corner;
                }
                
                floorElement.WallsPerDirection[direction] = wallElement;
            }
            
        }

        private WallElement SpawnWall(Vector2Int floorTilePosition, RoomElementType type, Direction direction, PrefabsPerSide prefabsPerSide)
        {
            var roomSide = direction.ToRoomSide();
            var spawnSettings = prefabsPerSide[roomSide];
            var floorPosition = CalculateFloorPosition(floorTilePosition); 
            
            var spawnPosition = OffSetPosition(floorPosition, direction, FloorSize / 2);
            var spawnOrientation = direction.ToSpawnOrientation();
            
            var angle = spawnOrientation.ToAngle();
            var spawnedObject = (GameObject) PrefabUtility.InstantiatePrefab(spawnSettings.Prefab, transform);
            spawnedObject.transform.position = spawnPosition;
            
            var wallElement = spawnedObject.AddComponent<WallElement>();

            if (Mathf.Abs(angle) > 0.01f)
            {
                spawnedObject.transform.Rotate(Vector3.up, angle);
            }

            wallElement.Type = type;
            wallElement.ExtendableRoom = this;
            wallElement.Direction = direction;
            wallElement.FloorTilePosition = floorTilePosition;
            //TODO Texture
            Undo.RegisterCreatedObjectUndo(wallElement.gameObject, "");
            return wallElement;
        }

        private CornerElement SpawnCorner(Vector2Int floorTilePosition, RoomElementType type, Direction4Diagonal direction)
        {
            var spawnSettings = type == RoomElementType.InnerCorner ? InnerCorner : OuterCorner;
            
            var floorPosition = CalculateFloorPosition(floorTilePosition);
            var spawnPosition = OffSetPosition(floorPosition, direction, FloorSize / 2);

            var angle = type == RoomElementType.InnerCorner ? direction.GetInnerCornerAngle() : direction.GetOuterCornerAngle();
            var spawnedObject = (GameObject) PrefabUtility.InstantiatePrefab(spawnSettings.Prefab, transform);
            spawnedObject.transform.position = spawnPosition;
            
            var corner = spawnedObject.AddComponent<CornerElement>();

            if (Mathf.Abs(angle) > 0.01f)
            {
                spawnedObject.transform.Rotate(Vector3.up, angle);
            }

            corner.Type = type;
            corner.ExtendableRoom = this;
            corner.FloorTilePosition = floorTilePosition;
            corner.Direction = direction;
            //TODO Texture
            Undo.RegisterCreatedObjectUndo(corner.gameObject, "");
            return corner;
        }

        private FloorElement SpawnFloor(Vector2Int floorTilePosition)
        {
            var spawnPosition = CalculateFloorPosition(floorTilePosition);
            var spawnedObject = (GameObject) PrefabUtility.InstantiatePrefab(Floor.Prefab, transform);
            spawnedObject.transform.position = spawnPosition;

            var floorElement = spawnedObject.AddComponent<FloorElement>();
            floorElement.ExtendableRoom = this;
            floorElement.GridPosition = floorTilePosition;
            FloorGridDictionary[floorTilePosition] = floorElement;
            
            //TODO Texture
            return floorElement;
        }

        private Vector3 OffSetPosition(Vector3 position, Direction direction, float offsetAmount)
        {
            switch (direction)
            {
                case Direction.Front:
                    position.z += offsetAmount;
                    return position;
                case Direction.Right:
                    position.x += offsetAmount;
                    return position;
                case Direction.Back:
                    position.z -= offsetAmount;
                    return position;
                case Direction.Left:
                    position.x -= offsetAmount;
                    return position;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }
        
        private Vector3 OffSetPosition(Vector3 position, Direction4Diagonal direction, float offsetAmount)
        {
            switch (direction)
            {
                case Direction4Diagonal.DownLeft:
                    position.x -= offsetAmount;
                    position.z -= offsetAmount;
                    return position;
                case Direction4Diagonal.DownRight:
                    position.x += offsetAmount;
                    position.z -= offsetAmount;
                    return position;
                case Direction4Diagonal.UpLeft:
                    position.x -= offsetAmount;
                    position.z += offsetAmount;
                    return position;
                case Direction4Diagonal.UpRight:
                    position.x += offsetAmount;
                    position.z += offsetAmount;
                    return position;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }
        }

        private Vector3 CalculateFloorPosition(Vector2Int tilePosition)
        {
            var worldPosition = transform.position;
            worldPosition.x += tilePosition.x * FloorSize;
            worldPosition.z += tilePosition.y * FloorSize;

            return worldPosition;
        }
        #endif
    }
}
