using System;
using UnityEditor;
using UnityEngine;
using UnityLevelEditor.Model;
using UnityLevelEditor.RoomExtension;

namespace UnityLevelEditor
{
    [Serializable]
    public class ElementSpawner
    {
        [field: SerializeField]
        public Bounds Bounds { get; }
        
        [field: SerializeField]
        private Bounds SidewaysRotatedBounds { get; }
        
        private GameObject toInstantiate;
        
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