using UnityEngine;

namespace UnityLevelEditor.RoomExtension
{
    public class ExtendableRoom : MonoBehaviour
    {
        [field: SerializeField, HideInInspector]
        public ElementSpawner WallSpawner { get; set; }

        [field: SerializeField, HideInInspector]
        public ElementSpawner CornerSpawner { get; set; }

        [field: SerializeField, HideInInspector]
        public ElementSpawner FloorSpawner { get; set; }
    }
}