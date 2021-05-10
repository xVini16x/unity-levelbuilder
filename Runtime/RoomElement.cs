using System.Collections.Generic;
using System.Linq;

using Sirenix.Utilities;

using UnityEngine;

using UnityLevelEditor.RoomExtension;

namespace UnityLevelEditor.Model
{
    public abstract class RoomElement : MonoBehaviour
    {
        [field: SerializeField, HideInInspector]
        public RoomElementType Type { get; set; }
        
        [field: SerializeField, HideInInspector]
        public ExtendableRoom ExtendableRoom { get; set; }

        public abstract void CopyOverValues(RoomElement roomElement);

        public void ApplyMaterial(MaterialSelectionDictionary materialSelectionDictionary)
        {
            var meshFilter = GetComponentInChildren<MeshFilter>();

            if (meshFilter == null)
            {
                Debug.LogWarning("Cannot apply materials because roomElement '" + gameObject.name + "' has no meshFilters as children.");
                return;
            }
            
            var meshes = meshFilter.sharedMesh;
            var meshRendererArray = meshFilter.GetComponent<MeshRenderer>();
            
            ExtendableRoom.MaterialSlotSetup.ApplyRandomMaterials(meshes, meshRendererArray, materialSelectionDictionary);
        }
    }
}
