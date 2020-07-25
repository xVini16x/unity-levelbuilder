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

        [field: SerializeField] private Bounds SidewaysRotatedBounds { get; set; }

        [SerializeField] private GameObject toInstantiate;

        [SerializeField] private RoomElementType type;

        public ElementSpawner(GameObject toInstantiate, Bounds bounds, RoomElementType type)
        {
            this.toInstantiate = toInstantiate;
            this.type = type;
            Bounds = bounds;
            var sizeRotatedSideways = Bounds.size;
            sizeRotatedSideways.x = Bounds.size.z;
            sizeRotatedSideways.z = Bounds.size.x;
            SidewaysRotatedBounds = new Bounds(Bounds.center, sizeRotatedSideways);
        }

        public Vector3 ConvertLeftBottomFrontPositionToLeftBottomCenterPosition(Vector3 position,
            SpawnOrientation orientation)
        {
            var applicableBounds = orientation.IsSideways() ? SidewaysRotatedBounds : Bounds;
            position.z -= applicableBounds.extents.z;
            return position;
        }

        public Vector3 ConvertLeftBottomBackPositionToLeftBottomCenterPosition(Vector3 position,
            SpawnOrientation orientation)
        {
            var applicableBounds = orientation.IsSideways() ? SidewaysRotatedBounds : Bounds;
            position.z += applicableBounds.extents.z;
            return position;
        }

        public (RoomElement roomElement, Vector3 dimensions) SpawnByLeftBottomCenter(Vector3 position,
            SpawnOrientation orientation, ExtendableRoom parent, string name)
        {
            var applicableBounds = orientation.IsSideways() ? SidewaysRotatedBounds : Bounds;
            position.x += applicableBounds.extents.x;
            position.y += applicableBounds.extents.y;

            return (Spawn(position, orientation, parent, name), applicableBounds.size);
        }

        public (RoomElement roomElement, Vector3 dimensions) SpawnByCenterPosition(Vector3 position,
            SpawnOrientation orientation, ExtendableRoom parent, string name)
        {
            var applicableBounds = orientation.IsSideways() ? SidewaysRotatedBounds : Bounds;
            return (Spawn(position, orientation, parent, name), applicableBounds.size);
        }


        public RoomElement SpawnNextToRoomElement(RoomElement roomElement, Direction direction,
            SpawnOrientation spawnOrientation)
        {
            var position = roomElement.transform.position;
            var parent = roomElement.ExtendableRoom;
            var roomElementSpawner = roomElement.ExtendableRoom.ElementSpawner[(int) roomElement.Type];
            var roomElementBounds = roomElement.SpawnOrientation.IsSideways()
                ? roomElementSpawner.SidewaysRotatedBounds
                : roomElementSpawner.Bounds;

            var factor = direction.TowardsNegative() ? -1 : 1;
            //var spawnOrientation = GetOrientationBasedOnRoomElement(roomElement, direction);
            var applicableBounds = spawnOrientation.IsSideways() ? SidewaysRotatedBounds : Bounds;

            if (direction.IsSideways())
            {
                position.x += factor * (roomElementBounds.extents.x + applicableBounds.extents.x);
            }
            else
            {
                position.z += factor * (roomElementBounds.extents.z + applicableBounds.extents.z);
            }

            if (type == RoomElementType.Floor && roomElement.Type != RoomElementType.Floor)
            {
                position.y -= roomElementBounds.extents.y - applicableBounds.extents.y;
            }

            return Spawn(position, spawnOrientation, parent, "");
        }

        private RoomElement Spawn(Vector3 position, SpawnOrientation orientation, ExtendableRoom parent, string name)
        {
            var angle = orientation.ToAngle();
            var spawnedObject = (GameObject) PrefabUtility.InstantiatePrefab(toInstantiate, parent.transform);
            spawnedObject.name = name;
            spawnedObject.transform.position = position;
            RoomElement roomElement;
            if (type == RoomElementType.Floor)
            {
                roomElement = spawnedObject.AddComponent<FloorElement>();
            }
            else
            {
                roomElement = spawnedObject.AddComponent<RoomElement>();
            }
            if (Mathf.Abs(angle) > 0.01f)
            {
                spawnedObject.transform.Rotate(Vector3.up, angle);
            }

            roomElement.Type = type;
            roomElement.ExtendableRoom = parent;
            roomElement.SpawnOrientation = orientation;
            Undo.RegisterCreatedObjectUndo(roomElement.gameObject, "");
            return roomElement;
        }
    }
}