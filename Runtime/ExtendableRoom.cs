using System;
using System.Collections.Generic;

using UnityEditor;

using UnityEngine;

namespace UnityLevelEditor.RoomExtension
{
    using Model;

    public class ExtendableRoom : MonoBehaviour
    {
        [SerializeField]
        private MaterialSlotSetup materialSlotSetup;

        public MaterialSlotSetup MaterialSlotSetup
        {
            get => materialSlotSetup;
            set => materialSlotSetup = value;
        }

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
        public float FloorSize { get; set; }

        [SerializeField] private GameObject floorRoot;

        private GameObject FloorRoot => GetRoot(ref floorRoot , "Floors");

        [SerializeField] private GameObject eastWallRoot;

        private GameObject EastWallRoot  => GetRoot(ref eastWallRoot , "East Walls");


        [SerializeField] private GameObject southWallRoot;

        private GameObject SouthWallRoot => GetRoot(ref southWallRoot , "South Walls");
  
        [SerializeField] private GameObject westWallRoot; 

        private GameObject WestWallRoot => GetRoot(ref westWallRoot , "West Walls");
     
        
        [SerializeField] private GameObject northWallRoot;

        private GameObject NorthWallRoot => GetRoot(ref northWallRoot, "North Walls");

        [SerializeField] private GameObject innerCornerRoot;

        private GameObject InnerCornerRoot => GetRoot(ref innerCornerRoot, "Inner Corners");

            [SerializeField] private GameObject outerCornerRoot;
        
        private GameObject OuterCornerRoot => GetRoot(ref outerCornerRoot, "Outer Corners");

        private GameObject GetRoot(ref GameObject variableRef, string name)
        {
            if (variableRef == null && !TryFindRoot(name, ref variableRef))
            {
                variableRef = new GameObject();
                variableRef.transform.parent = transform;
                variableRef.gameObject.name = name;
            }

            return variableRef;
        }
        
        private bool TryFindRoot(string name, ref GameObject variableRef)
        {
            var roots = new List<Transform>();
            
            var changeParents = new List<Transform>();
            
            foreach (Transform childTransform in transform)
            {
                if (childTransform.gameObject.name.Equals(name))
                {
                    roots.Add(childTransform);

                    if (roots.Count > 1)
                    {
                        foreach (Transform grandChild in childTransform)
                        {
                            changeParents.Add(grandChild);
                        }
                    }
                }
            }

            if (roots.Count == 0)
            {
                variableRef = null;
                return false;
            }

            var root = roots[0];

            foreach (var newChild in changeParents)
            {
                newChild.SetParent(root, true);
            }

            for (var i = 1; i < roots.Count; i++)
            {
                DestroyImmediate(roots[i].gameObject);
            }

            variableRef = root.gameObject;
            return true;
        }

#if UNITY_EDITOR

        public FloorElement Spawn(Vector2Int floorTilePosition)
        {
            Undo.RegisterCompleteObjectUndo(this, "");

            var neighborsToReevaluate = GetAndRecordRespawnRelevantFloors(floorTilePosition);
            
            foreach (var neighborPosition in neighborsToReevaluate)
            {
                RespawnWalls(neighborPosition);
            }

            return FloorGridDictionary[floorTilePosition];
        }

        public FloorElement DeleteFloor(Vector2Int floorTilePosition, Vector2Int newFloorPosition)
        {
            Undo.RegisterCompleteObjectUndo(this, "");

            if (!FloorGridDictionary.TryGetValue(floorTilePosition, out var floorElement))
            {
                return null;
            }

            var neighborsToReevaluate = GetAndRecordRespawnRelevantFloors(floorTilePosition);
            
            floorElement.DeleteAllNeighbors();
            Undo.DestroyObjectImmediate(floorElement.gameObject);
            neighborsToReevaluate.Remove(floorTilePosition);
            FloorGridDictionary.Remove(floorTilePosition);
            
            if (FloorGridDictionary.Count == 0)
            {
                Undo.DestroyObjectImmediate(this.gameObject);
                return null;
            }

            foreach (var neighborPosition in neighborsToReevaluate)
            {
                RespawnWalls(neighborPosition);
            }

            if (FloorGridDictionary.TryGetValue(newFloorPosition, out var newFloor))
            {
                return newFloor;
            }

            return null;

        }

        private List<Vector2Int> GetAndRecordRespawnRelevantFloors(Vector2Int floorTilePosition)
        {
            if (!FloorGridDictionary.TryGetValue(floorTilePosition, out var floorElement))
            {
                floorElement = SpawnFloor(floorTilePosition);
            }
            
            Undo.RegisterCompleteObjectUndo(floorElement, "");
            
            var neighborsToReevaluate = new List<Vector2Int>(){floorTilePosition};

            // Already check for neighbors to record their current state
            foreach (Direction direction in Enum.GetValues(typeof(Direction)))
            {
                var tileInDirection = floorTilePosition + direction.AsVector2Int();

                if (FloorGridDictionary.TryGetValue(tileInDirection, out var neighbor))
                {
                    HandleNeighboringFloor(neighbor, tileInDirection);
                }

                tileInDirection = tileInDirection + direction.Shift(1).AsVector2Int();

                if (FloorGridDictionary.TryGetValue(tileInDirection, out neighbor))
                {
                    HandleNeighboringFloor(neighbor, tileInDirection);
                }
            }

            return neighborsToReevaluate;
            
            void HandleNeighboringFloor(FloorElement neighbor, Vector2Int vector2Int)
            {
                if (neighbor == null)
                {
                    neighbor = SpawnFloor(vector2Int);
                }
                neighborsToReevaluate.Add(vector2Int);
                Undo.RegisterCompleteObjectUndo(neighbor, "");
            }
        }

        private void RespawnWalls(Vector2Int floorTilePosition)
        {
            if (FloorGridDictionary.TryGetValue(floorTilePosition, out var floorElement))
            {
                floorElement.DeleteAllNeighbors();
                SpawnWalls(floorElement);
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
                    var tileCounterClockwiseOfDirection = floorTilePosition + direction.Shift(1).AsVector2Int();

                    if (!FloorGridDictionary.ContainsKey(tileCounterClockwiseOfDirection))
                    {
                        cornerType = RoomElementType.OuterCorner;
                    }
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
                    floorElement.SetCorner(cornerDirection, corner);
                }

                floorElement.SetWall(direction, wallElement);
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

            Transform parent;
            string nameAddition;

            switch (spawnOrientation)
            {
                case SpawnOrientation.Front:
                    parent = NorthWallRoot.transform;
                    nameAddition = "North";
                    break;
                case SpawnOrientation.Right:
                    parent = EastWallRoot.transform;
                    nameAddition = "East"; 
                    break;
                case SpawnOrientation.Back:
                    parent = SouthWallRoot.transform;
                    nameAddition = "South";
                    break;
                case SpawnOrientation.Left:
                    parent = WestWallRoot.transform;
                    nameAddition = "West";
                    break;
                default:
                    parent = transform;
                    nameAddition = "Unidentified";
                    break;
            }
            
            var spawnedObject = (GameObject)PrefabUtility.InstantiatePrefab(spawnSettings.Prefab, parent);
            Undo.RegisterCreatedObjectUndo(spawnedObject, "");
            spawnedObject.transform.position = spawnPosition;
            spawnedObject.name = $"{spawnSettings.Prefab.name} | {floorTilePosition} {nameAddition}";

            var wallElement = spawnedObject.AddComponent<WallElement>();
            if (Mathf.Abs(angle) > 0.01f)
            {
                spawnedObject.transform.Rotate(Vector3.up, angle);
            }
            wallElement.Type = type;
            wallElement.ExtendableRoom = this;
            wallElement.Direction = direction;
            wallElement.FloorTilePosition = floorTilePosition;
            
            wallElement.ApplyMaterial(spawnSettings.MaterialOverrides);
            return wallElement;
        }

        private CornerElement SpawnCorner(Vector2Int floorTilePosition, RoomElementType type, Direction4Diagonal direction)
        {
            var spawnSettings = type == RoomElementType.InnerCorner ? InnerCorner : OuterCorner;
            var floorPosition = CalculateFloorPosition(floorTilePosition);
            var spawnPosition = OffSetPosition(floorPosition, direction, FloorSize / 2);
            var angle = type == RoomElementType.InnerCorner ? direction.GetInnerCornerAngle() : direction.GetOuterCornerAngle();
            var parent = type == RoomElementType.InnerCorner ? InnerCornerRoot.transform : OuterCornerRoot.transform;
            var nameAddition = direction.GeographicName();
            
            var spawnedObject = (GameObject)PrefabUtility.InstantiatePrefab(spawnSettings.Prefab, parent);
            spawnedObject.name = $"{spawnSettings.Prefab.name} | {floorTilePosition} {nameAddition}";
            
            Undo.RegisterCreatedObjectUndo(spawnedObject, "");
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
            
            corner.ApplyMaterial(spawnSettings.MaterialOverrides);
    
            return corner;
        }

        private FloorElement SpawnFloor(Vector2Int floorTilePosition)
        {
            var spawnPosition = CalculateFloorPosition(floorTilePosition);
            var spawnedObject = (GameObject)PrefabUtility.InstantiatePrefab(Floor.Prefab, FloorRoot.transform);
            Undo.RegisterCreatedObjectUndo(spawnedObject, "");
            spawnedObject.transform.position = spawnPosition;
            var floorElement = spawnedObject.AddComponent<FloorElement>();
            floorElement.ExtendableRoom = this;
            floorElement.GridPosition = floorTilePosition;
            floorElement.Type = RoomElementType.Floor;
            FloorGridDictionary[floorTilePosition] = floorElement;
            spawnedObject.name = floorTilePosition.ToString();

            floorElement.ApplyMaterial(Floor.MaterialOverrides);
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
