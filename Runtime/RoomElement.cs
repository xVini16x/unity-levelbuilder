using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using UnityLevelEditor.RoomExtension;

namespace UnityLevelEditor.Model
{
    public class RoomElement : MonoBehaviour
    {
        [field: SerializeField, HideInInspector]
        public ExtendableRoom ExtendableRoom { get; set; }
        
        public void ApplyMaterial(MaterialSelectionDictionary materialChoices, bool pickOnlyFirst = false)
        {
            if (materialChoices.Count == 0)
            {
                return;
            }
            
            var meshFilter = GetComponentsInChildren<MeshFilter>();

            if (meshFilter.Length == 0)
            {
                Debug.LogWarning("Cannot apply materials because roomElement '" + gameObject.name + "' has no meshFilters as children.");
                return;
            }
            
            var meshes = meshFilter.Select(filter => filter.sharedMesh).ToArray();
            var meshRendererArray = meshFilter.Select(filter => filter.GetComponent<MeshRenderer>()).ToArray();
            
            var mappings = ExtendableRoom.MaterialSlotSetup;

            for (var i = 0; i < meshes.Length; i++)
            {
                var mesh = meshes[i];
                var meshRenderer = meshRendererArray[i];
                var materials = meshRenderer.sharedMaterials;
                
                var mapping = mappings.MaterialSlots[mesh];
                
                foreach (var slot in materialChoices.Keys)
                {
                    var materialChoice = materialChoices[slot];

                    if (materialChoice == null || materialChoice.Count == 0)
                    {
                        Debug.LogWarning("Material Choice for side '" + slot + "' cannot be applied. List is null or empty.");
                        continue;
                    }
                    
                    materials[mapping[slot]] = pickOnlyFirst ? materialChoice[0] : materialChoice.PickRandom();
                }

                meshRenderer.sharedMaterials = materials;
            }
        }
    }
}
