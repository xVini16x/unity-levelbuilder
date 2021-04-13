using System;
using System.Collections.Generic;
using System.Linq;

using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;

using UnityEngine;
using UnityEditor;

namespace UnityLevelEditor.Editor
{
    using Model;
    using RoomExtension;

    public class RoomSpawner : OdinEditorWindow
    {
        internal const int RoomSizeLimit = 100;
        private const string WindowName = "Room Spawner";
        private const string BulletCharacter = "•";
        
        [HideInInspector]
        [SerializeField] private bool loadedDefaultValues;

        #region Inspector Fields

        [DetailedInfoBox("Missing / Wrong Prefabs", "@errorMessageDetails", InfoMessageType.Error, "@showErrorMessage")]
        [SerializeField] private PrefabsPerSide fullWall;
        [SerializeField] private PrefabsPerSide wallShortenedLeft;
        [SerializeField] private PrefabsPerSide wallShortenedRight;
        [SerializeField] private PrefabsPerSide wallShortenedBothSides;
        [SerializeField] private RoomElementSpawnSettings floor;
        [SerializeField] private RoomElementSpawnSettings outerCorner;
        [SerializeField] private RoomElementSpawnSettings innerCorner;
        [SerializeField] private MaterialSlotSetup materialSlotSetup;

        [DetailedInfoBox("Unexpected element sizes", "@warningMessageDetails", InfoMessageType.Warning, "@showWarningMessage")]
        [SuffixLabel("UU")]
        [SerializeField] private float floorSize;
        [SerializeField] private string roomName = "StandardRoom";

        [Title("RoomSize")]
        [PropertyRange(1, RoomSizeLimit)]
        [SuffixLabel("Full Walls")]
        [SerializeField] private int roomSizeX;
        
        [PropertyRange(1, RoomSizeLimit)]
        [SuffixLabel("Full Walls")]
        [SerializeField] private int roomSizeZ;

        #endregion

        #region UI

        #region Window Creation

        protected void OnFocus()
        {
            if (loadedDefaultValues)
            {
                return;
            }
            
            loadedDefaultValues = true;
            var levelBuilderSettings = LevelBuilderSettings.GetOrCreateSettings();
            fullWall = (PrefabsPerSide) levelBuilderSettings.fullWall.Clone();
            wallShortenedLeft = (PrefabsPerSide) levelBuilderSettings.wallShortenedLeft.Clone();
            wallShortenedRight = (PrefabsPerSide) levelBuilderSettings.wallShortenedRight.Clone();
            wallShortenedBothSides = (PrefabsPerSide) levelBuilderSettings.wallShortenedBothSides.Clone();
            floor = (RoomElementSpawnSettings) levelBuilderSettings.floor.Clone();
            innerCorner = (RoomElementSpawnSettings) levelBuilderSettings.innerCorner.Clone();
            outerCorner = (RoomElementSpawnSettings) levelBuilderSettings.outerCorner.Clone();
            materialSlotSetup = levelBuilderSettings.materialSlotSetup;
            floorSize = levelBuilderSettings.floorSize;
            roomName = (string) levelBuilderSettings.roomName.Clone();
            roomSizeX = levelBuilderSettings.roomSizeX;
            roomSizeZ = levelBuilderSettings.roomSizeZ;
        }

        [MenuItem("Tools/Room Builder/" + WindowName)]
        private static void Init()
        {
            GetWindow<RoomSpawner>().Show();
        }

        protected override void OnEndDrawEditors()
        {
            ValidatePrefabs();
        }

        [Button("Create Room")]
        private void CreateRoom()
        {
            SpawnNewRoom();
        }
        
        #endregion

        #region Validation
        
        private string errorMessageDetails;
        private bool showErrorMessage;

        private string warningMessageDetails;
        private bool showWarningMessage;
        private void ValidatePrefabs()
        {
            errorMessageDetails = "";
            showErrorMessage = false;
            
            if (!PrefabsAreAssigned())
            {
                showErrorMessage = true;
                return;
            }

            Dictionary<RoomElementType, Dictionary<RoomSide, Bounds>> boundsByType;
            
            try
            {
                boundsByType = GetAllBoundsByType();
            }
            catch (MissingComponentException e)
            {
                showErrorMessage = true;
                errorMessageDetails += BulletCharacter + e.Message;
                return;
            }

            warningMessageDetails = "";
            showWarningMessage = !CheckBoundSizesCorrect(boundsByType);
        }

        private bool PrefabsAreAssigned()
        {
            var prefabsAssigned = true;
            
            if (fullWall.Count < 3)
            {
                errorMessageDetails += BulletCharacter + " <b>Full wall</b>: Please assign prefabs for the following sides:\n ";
                CheckPrefabsForAllSides(fullWall);
                prefabsAssigned = false;
            }

            if (wallShortenedLeft.Count < 3)
            {
                errorMessageDetails += BulletCharacter + " <b>Wall Shortened Left</b>: Please assign prefabs for the following sides:\n ";
                CheckPrefabsForAllSides(wallShortenedLeft);
                prefabsAssigned = false;
            }

            if (wallShortenedRight.Count < 3)
            {
                errorMessageDetails += BulletCharacter + " <b>Wall Shortened Right</b>: Please assign prefabs for the following sides:\n ";
                CheckPrefabsForAllSides(wallShortenedRight);
                prefabsAssigned = false;
            }

            if (wallShortenedBothSides.Count < 3)
            {
                errorMessageDetails += BulletCharacter + " <b>Wall Shortened Both Sides</b>: Please assign prefabs for the following sides:\n ";
                CheckPrefabsForAllSides(wallShortenedBothSides);
                prefabsAssigned = false;
            }
            
            if (floor == null)
            {
                errorMessageDetails += BulletCharacter + " <b>Floor</b> Please assign prefab!";
                prefabsAssigned = false;
            }

            if (outerCorner == null)
            {
                errorMessageDetails += BulletCharacter + " <b>Outer Corner</b> Please assign prefab!";
                prefabsAssigned = false;
            }

            if (innerCorner == null)
            {
                errorMessageDetails += BulletCharacter + " <b>Inner Corner</b> Please assign prefab!";
                prefabsAssigned = false;
            }

            return prefabsAssigned;
        }
        
        private void CheckPrefabsForAllSides(PrefabsPerSide prefabsPerSide)
        {
            var first = true;
            
            foreach (RoomSide side in Enum.GetValues(typeof(RoomSide)))
            {
                if (!prefabsPerSide.ContainsKey(side))
                {
                    if (!first)
                    {
                        errorMessageDetails += ", ";
                    }

                    errorMessageDetails += side.GetName();
                    first = false;
                }
            }
            
            errorMessageDetails += "\n";
        }

        private Bounds GetBoundsOfPrefab(GameObject prefab, RoomElementType type, RoomSide roomSide)
        {
            var meshRenderer = prefab.GetComponentInChildren<MeshRenderer>();

            if (meshRenderer == null)
            {
                throw new MissingComponentException("Room elements are expected to contain a mesh renderer. The " + type.ToString() + " - " + roomSide.GetName() + " prefab doesn't seem to have a mesh renderer in it's hierarchy. Please add one.");
            }

            return meshRenderer.bounds;
        }

        private bool CheckBoundSizesCorrect(Dictionary<RoomElementType, Dictionary<RoomSide, Bounds>> boundsByType)
        {
            bool fail = false;

            foreach (var kvp in boundsByType)
            {
                if(!CheckSameBounds(kvp.Value, kvp.Key, out string message))
                {
                    warningMessageDetails += message;
                    fail = true;
                }
            }

            if (fail)
            {
                return false;
            }
            
            //When checking the bounds further down only one variant
            var sameXZTypes = new[] {RoomElementType.OuterCorner, RoomElementType.InnerCorner, RoomElementType.Floor};

            foreach (var type in sameXZTypes)
            { 
                // Floor and corners are only for one side available, so checking only front is good enough
                if (!CheckXZSameSize(boundsByType[type][RoomSide.North], boundsByType.ToString(), out string message))
                {
                    warningMessageDetails += message;
                    fail = true;
                }
            }

            if (fail)
            {
                return false;
            }
            
            // Corners are multiple times in dictionary but based on one prefab, so considering only one side is alright here
            // Considering only x is alright as well, because we checked previously if they have same x and z
            var innerCornerX = boundsByType[RoomElementType.InnerCorner][RoomSide.North].size.x;
            var outerCornerX = boundsByType[RoomElementType.OuterCorner][RoomSide.North].size.x;
            
            if (Mathf.Abs(innerCornerX - outerCornerX) > 0.01f)
            {
                warningMessageDetails += $"{BulletCharacter} X-direction of outer corner ({outerCornerX:F}) and inner corner ({innerCornerX:F}) should be the same size.\n";
                return false;
            }

            // Walls were already checked to have same x, so considering only one side is alright here
            var wallX = boundsByType[RoomElementType.Wall][RoomSide.North].size.x;
            // Floor is multiple times in dictionary but based on one prefab, so considering only one side is alright here
            var floorX = boundsByType[RoomElementType.Floor][RoomSide.North].size.x;

            if (Mathf.Abs(wallX - floorX) > 0.01f)
            {
                warningMessageDetails += $"{BulletCharacter} X-direction of wall ({wallX:F}) and floor ({floorX:F}) should be the same size.\n";
                return false;
            }

            // Walls were already checked to have same x, so considering only one side is alright here
            var wallShortenedLeftX = boundsByType[RoomElementType.WallShortenedLeft][RoomSide.North].size.x;
            var wallShortenedRightX = boundsByType[RoomElementType.WallShortenedRight][RoomSide.North].size.x;
            
            // just one corner and side is here enough because of previous checks
            var expectedWallLength = wallX - innerCornerX;

            if (Mathf.Abs(wallShortenedLeftX - expectedWallLength) > 0.01f ||
                Mathf.Abs(wallShortenedRightX - expectedWallLength) > 0.01f)
            {
                warningMessageDetails += $"{BulletCharacter} X size of wall shortened in left direction ({wallShortenedLeftX:F}) and X size of wall shortened in right direction ({wallShortenedRightX:F}) "
                          + $"should be the same length of the fullWall minus the length of the corner ({expectedWallLength:F})\n";
                return false;
            }

            // Walls were already checked to have same x, so considering only one side is alright here
            var wallShortenedBothSidesX = boundsByType[RoomElementType.WallShortenedBothEnds][RoomSide.North].size.x;
            expectedWallLength = wallX - (2 * innerCornerX);

            if (Mathf.Abs(wallShortenedBothSidesX - expectedWallLength) > 0.01f)
            {
                warningMessageDetails += $"{BulletCharacter} X size of wall shortened on both sides ({wallShortenedBothSidesX:F}) should be the same as full wall minus twice the length of the corner ({expectedWallLength:F})\n";
                return false;
            }
            
            return true;
        }

        private bool CheckXZSameSize(Bounds bounds, string roomElementName, out string message)
        {
            if (Mathf.Abs(bounds.size.x - bounds.size.z) > 0.01f)
            {
                message = $"{BulletCharacter} Size of z-direction ({bounds.size.z:F}) and x-direction ({bounds.size.x:F}) needs to be the same for {roomElementName} prefab.\n";
                return false;
            }

            message = null;
            return true;
        }

        private bool CheckSameBounds(Dictionary<RoomSide, Bounds> boundsPerSide, RoomElementType roomElementType, out string message)
        {
            message = BulletCharacter + roomElementType + " different sizes in x-direction:\n";
            var keyList = boundsPerSide.Keys.ToList();

            var bounds = boundsPerSide[keyList[0]];
            message += keyList[0] + ": " + bounds.size.x + "\n";
            var equal = true;

            for (var i = 1; i < keyList.Count; i++)
            {
                var key = keyList[i];
                var newBounds = boundsPerSide[key];

                if (bounds.size.x - newBounds.size.x > 0.01f)
                {
                    equal = false;
                }

                bounds = newBounds;
                message += key + ": " + bounds.size.x + "\n";
            }
            
            return equal;
        }

        private GameObject GetPrefabByTypeAndSide(RoomElementType type, RoomSide roomSide)
        {
            return GetSpawnInformationByTypeAndSide(type, roomSide).Prefab;
        }

        private RoomElementSpawnSettings GetSpawnInformationByTypeAndSide(RoomElementType type, RoomSide roomSide)
        {
            switch (type)
            {
                case RoomElementType.Wall:
                    return fullWall[roomSide];
                case RoomElementType.WallShortenedLeft:
                    return wallShortenedLeft[roomSide];
                case RoomElementType.WallShortenedRight:
                    return wallShortenedRight[roomSide];
                case RoomElementType.WallShortenedBothEnds:
                    return wallShortenedBothSides[roomSide];
                case RoomElementType.Floor:
                    return floor;
                case RoomElementType.OuterCorner:
                    return outerCorner;
                case RoomElementType.InnerCorner:
                    return innerCorner;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, $"GetPrefabByType is not supported for type {type} as of current.");
            }
        }
        
        private Dictionary<RoomElementType, Dictionary<RoomSide, Bounds>> GetAllBoundsByType()
        {
            var dict = new Dictionary<RoomElementType, Dictionary<RoomSide, Bounds>>();

            foreach (RoomElementType elementType in Enum.GetValues(typeof(RoomElementType)))
            {
                dict[elementType] = new Dictionary<RoomSide, Bounds>();

                foreach (RoomSide roomSide in Enum.GetValues(typeof(RoomSide)))
                {
                    dict[elementType][roomSide] = GetBoundsOfPrefab(GetPrefabByTypeAndSide(elementType, roomSide), elementType, roomSide);
                }
            }

            return dict;
        }
        
        #endregion

        #endregion
      
              #region Room Creation
      
              #region Element Spawning
              
              private void SpawnNewRoom()
              {
                  var room = new GameObject(roomName);
                  var firstTilePosition = Vector3.zero;
                  firstTilePosition.x -= floorSize * (roomSizeX / 2f) - floorSize / 2f;
                  firstTilePosition.z -= floorSize * (roomSizeZ / 2f) - floorSize / 2f;
      
                  var extendableRoom = room.AddComponent<ExtendableRoom>();
                  
                  fullWall.OnBeforeSerialize();
                  
                  extendableRoom.FloorGridDictionary = new FloorGridDictionary();
                  extendableRoom.FullWall = (PrefabsPerSide) fullWall.Clone();
                  extendableRoom.WallShortenedRight = (PrefabsPerSide) wallShortenedRight.Clone();
                  extendableRoom.WallShortenedLeft = (PrefabsPerSide) wallShortenedLeft.Clone();
                  extendableRoom.WallShortenedBothSides = (PrefabsPerSide) wallShortenedBothSides.Clone();
                  extendableRoom.InnerCorner = (RoomElementSpawnSettings) innerCorner.Clone();
                  extendableRoom.OuterCorner = (RoomElementSpawnSettings) outerCorner.Clone();
                  extendableRoom.Floor = (RoomElementSpawnSettings) floor.Clone();
                  extendableRoom.FloorSize = floorSize;
                  extendableRoom.MaterialSlotSetup = materialSlotSetup;
                  
                  Undo.RegisterCreatedObjectUndo(room, "Room Creation");
                  Undo.SetCurrentGroupName("Room Creation");

                  for (var z = 0; z < roomSizeZ; z++)
                  {
                      for (var x = 0; x < roomSizeX; x++)
                      {
                          extendableRoom.Spawn(new Vector2Int(x, z));
                      }
                  }
                  
                  Undo.CollapseUndoOperations(Undo.GetCurrentGroup());
              }
              
              #endregion
    
            #endregion
          
    }
}