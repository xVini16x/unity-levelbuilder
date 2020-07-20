using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityLevelEditor.Model;
using UnityLevelEditor.RoomExtension;

namespace UnityLevelEditor
{
    [Serializable]
    public class ElementSpawner
    {
        [field: SerializeField] public Bounds Bounds { get; private set; }

        [field: SerializeField]
        private Bounds SidewaysRotatedBounds { get; set; }

        [SerializeField]
        private GameObject toInstantiate;
        
        [SerializeField]
        private RoomElementTyp type;

        public ElementSpawner(GameObject toInstantiate, Bounds bounds, RoomElementTyp type)
        {
            this.toInstantiate = toInstantiate;
            this.type = type;
            Bounds = bounds;
            var sizeRotatedSideways = Bounds.size;
            sizeRotatedSideways.x = Bounds.size.z;
            sizeRotatedSideways.z = Bounds.size.x;
            SidewaysRotatedBounds = new Bounds(Bounds.center, sizeRotatedSideways);
        }

        public Vector3 ConvertLeftBottomFrontPositionToLeftBottomCenterPosition(Vector3 position, SpawnOrientation orientation)
        {
            var applicableBounds = orientation.IsSideways() ? SidewaysRotatedBounds : Bounds;
            position.z -= applicableBounds.extents.z;
            return position;
        }

        public Vector3 ConvertLeftBottomBackPositionToLeftBottomCenterPosition(Vector3 position, SpawnOrientation orientation)
        {
            var applicableBounds = orientation.IsSideways() ? SidewaysRotatedBounds : Bounds;
            position.z += applicableBounds.extents.z;
            return position;
        }

        public (RoomElement roomElement, Vector3 dimensions) SpawnByLeftBottomCenter(Vector3 position, SpawnOrientation orientation, ExtendableRoom parent, string name)
        {
            var applicableBounds = orientation.IsSideways() ? SidewaysRotatedBounds : Bounds;
            position.x += applicableBounds.extents.x;
            position.y += applicableBounds.extents.y;

            return (Spawn(position, orientation, parent, name), applicableBounds.size);
        }

        public (RoomElement roomElement, Vector3 dimensions) SpawnByCenterPosition(Vector3 position, SpawnOrientation orientation, ExtendableRoom parent, string name)
        {
            var applicableBounds = orientation.IsSideways() ? SidewaysRotatedBounds : Bounds;
            return (Spawn(position, orientation, parent, name), applicableBounds.size);
        }
        

        public RoomElement SpawnNextToRoomElement(RoomElement roomElement, Direction direction)
        {
            var position = roomElement.transform.position;
            var parent = roomElement.ExtendableRoom;
            var roomElementSpawner = roomElement.ExtendableRoom.ElementSpawner[(int) roomElement.Type];
            var roomElementBounds = roomElement.SpawnOrientation.IsSideways() ? roomElementSpawner.SidewaysRotatedBounds : roomElementSpawner.Bounds;

            var factor = direction.TowardsNegative() ? -1 : 1;
            var spawnOrientation = GetOrientationBasedOnRoomElement(roomElement, direction);
            var applicableBounds = spawnOrientation.IsSideways() ? SidewaysRotatedBounds : Bounds;
            
            if (direction.IsSideways())
            {
                position.x += factor * (roomElementBounds.extents.x + applicableBounds.extents.x);
            }
            else
            {
                position.z += factor * (roomElementBounds.extents.z + applicableBounds.extents.z);
            }

            if (type == RoomElementTyp.Floor && roomElement.Type != RoomElementTyp.Floor)
            {
                position.y -= roomElementBounds.extents.y - applicableBounds.extents.y;
            }

            return Spawn(position, spawnOrientation, parent, "");
        }


        private SpawnOrientation GetOrientationBasedOnRoomElement(RoomElement roomElement, Direction direction)
        {
            if (type == RoomElementTyp.Floor)
            {
                return SpawnOrientation.Front;
            }

            if (roomElement.Type == RoomElementTyp.Floor)
            {
                return (SpawnOrientation) direction;
            }
            
            if (roomElement.Type.IsWallType())
            {
                if (type.IsWallType())
                {
                    return roomElement.SpawnOrientation;
                }
                
                if (roomElement.Type == RoomElementTyp.WallShortenedRight)
                {
                    return roomElement.SpawnOrientation.Shift(-1);//1-6 wand -90 drehung der corner
                }
                
                if (roomElement.Type == RoomElementTyp.WallShortenedLeft)
                {
                    return roomElement.SpawnOrientation;
                }

                switch (roomElement.SpawnOrientation)
                {
                    case SpawnOrientation.Front:
                        if (direction == Direction.Left)
                        {
                            return SpawnOrientation.Right;  //+180
                        }

                        if (direction == Direction.Right)
                        {
                            return SpawnOrientation.Back; // +90
                        }
                        break;
                    case SpawnOrientation.Right:
                        if (direction == Direction.Front)
                        {
                            return SpawnOrientation.Back; //+90
                        }

                        if (direction == Direction.Back)
                        {
                            return SpawnOrientation.Left; //+180
                        }
                        break;
                    case SpawnOrientation.Back:
                        if (direction == Direction.Right)
                        {
                            return SpawnOrientation.Left;
                        }

                        if (direction == Direction.Left)
                        {
                            return SpawnOrientation.Front;
                        }
                        break;
                    case SpawnOrientation.Left:
                        if (direction == Direction.Back)
                        {
                            return SpawnOrientation.Front;
                        }

                        if (direction == Direction.Front)
                        {
                            return SpawnOrientation.Right;
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                
                //TODO Other cases not supported
                Debug.LogError($"Not supported wall orientation {roomElement.SpawnOrientation} and corner in direction {direction}");
                return roomElement.SpawnOrientation.Shift(1);
        
            }

            if (roomElement.Type.IsCornerType())
            {
                if (type.IsCornerType())
                {
                    Debug.LogError($"Spawning a corner next to a corner is not supported.");
                    //TODO: Error? Is there a case where two corners should be spawned next to each other?
                }
                else
                {
                    
                    switch (roomElement.SpawnOrientation)
                    {
                        case SpawnOrientation.Front:
                            if (direction == Direction.Right)
                            {
                                return SpawnOrientation.Back; // +180
                            }

                            if (direction == Direction.Front)
                            {
                                return SpawnOrientation.Left; // -90
                            }
                            break;
                        case SpawnOrientation.Right:
                            if (direction == Direction.Right)
                            {
                                return SpawnOrientation.Front; //-90
                            }
                            
                            if (direction == Direction.Back)
                            {
                                return SpawnOrientation.Left; //+180
                            }
                            break;
                        case SpawnOrientation.Back:
                            if (direction == Direction.Left)
                            {
                                return SpawnOrientation.Front; // +180
                            }

                            if (direction == Direction.Back)
                            {
                                return SpawnOrientation.Right; // -90
                            }
                            break;
                        case SpawnOrientation.Left:
                            if (direction == Direction.Left)
                            {
                                return SpawnOrientation.Back; // -90
                            }

                            if (direction == Direction.Front)
                            {
                                return SpawnOrientation.Right; // +180
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException($"Not supported SpawnOrientation {roomElement.SpawnOrientation}.");
                    }
                    
                    //TODO: Other rotations not supported

                    Debug.LogError($"Not supported corner orientation {roomElement.SpawnOrientation} and wall in direction {direction}");
                    return SpawnOrientation.Front;
                }
                
             
            }

            //TODO Error? Is there a case I overlooked?
            return SpawnOrientation.Front;
        }

        private RoomElement Spawn(Vector3 position, SpawnOrientation orientation, ExtendableRoom parent, string name)
        {
            var angle = orientation.ToAngle();
            var spawnedObject = (GameObject) PrefabUtility.InstantiatePrefab(toInstantiate, parent.transform);
            spawnedObject.name = name;
            spawnedObject.transform.position = position;
            var roomElement = spawnedObject.AddComponent<RoomElement>();
            if (Mathf.Abs(angle) > 0.01f)
            {
                spawnedObject.transform.Rotate(Vector3.up, angle);
            }

            roomElement.Type = type;
            roomElement.ExtendableRoom = parent;
            roomElement.SpawnOrientation = orientation;
            return roomElement;
        }

     
    }
    

}