using UnityEditor;
using UnityEngine;
using UnityLevelEditor.Model;

namespace UnityLevelEditor
{
    public class ElementSpawner
    {
        public Bounds Bounds { get; }
        private Bounds SidewaysRotatedBounds { get; }
        private readonly GameObject toInstantiate;
        private readonly RoomElementTyp type;

        public ElementSpawner(GameObject toInstantiate, RoomElementTyp type)
        {
            this.toInstantiate = toInstantiate;
            this.type = type;

            var meshRenderer = toInstantiate.GetComponentInChildren<MeshRenderer>();

            Bounds = meshRenderer.bounds;
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

        public (RoomElement roomElement, Vector3 dimensions) SpawnByLeftBottomCenter(Vector3 position, SpawnOrientation orientation, Transform parent, string name)
        {
            var applicableBounds = orientation.IsSideways() ? SidewaysRotatedBounds : Bounds;
            position.x += applicableBounds.extents.x;
            position.y += applicableBounds.extents.y;

            return (Spawn(position, orientation.ToAngle(), parent, name), applicableBounds.size);
        }

        private RoomElement Spawn(Vector3 position, float angle, Transform parent, string name)
        {
            var spawnedObject = (GameObject) PrefabUtility.InstantiatePrefab(toInstantiate, parent);
            spawnedObject.name = name;
            spawnedObject.transform.position = position;
            var roomElement = spawnedObject.AddComponent<RoomElement>();
            if (Mathf.Abs(angle) > 0.01f)
            {
                spawnedObject.transform.Rotate(Vector3.up, angle);
            }

            roomElement.Type = type;
            return roomElement;
        }
    }
}