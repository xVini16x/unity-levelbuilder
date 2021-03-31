using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using UnityLevelEditor.RoomExtension;

namespace UnityLevelEditor.Model
{
    [CreateAssetMenu(menuName = "RoomBuilder - MaterialSlotSetup", fileName = "New Material Mappings")]
    public class MaterialSlotSetup : ScriptableObject
    {
        [SerializeField] private MaterialSlotMappingsPerMesh materialSlots;
        public MaterialSlotMappingsPerMesh MaterialSlots => materialSlots;

        public void ApplyRandomMaterials(Mesh mesh, MeshRenderer meshRenderer, MaterialSelectionDictionary materialChoices)
        {
            ApplyMaterials(mesh, meshRenderer, PickRandomMaterials(materialChoices));
        }

        public void ApplyMaterials(Mesh mesh, MeshRenderer meshRenderer, Dictionary<MaterialSlotType, Material> materialChoices)
        {
            var materials = meshRenderer.sharedMaterials;
            meshRenderer.sharedMaterials = ApplyMaterialsToArray(mesh, materials, materialChoices);
        }
        
        public void ApplyMaterial(Mesh mesh, MeshRenderer meshRenderer, MaterialSlotType materialSlotType, Material newMaterial)
        {
            if (TryGetMapping(mesh, out var mapping))
            {
                var materialArray = meshRenderer.sharedMaterials;
                meshRenderer.sharedMaterials = ApplyMaterialToArray(materialArray, materialSlotType, newMaterial, mapping);
            }
        }
        
        public Material[] ApplyRandomMaterialsToArray(Mesh mesh, Material[] materialArray, MaterialSelectionDictionary materialChoices)
        {
            return ApplyMaterialsToArray(mesh, materialArray, PickRandomMaterials(materialChoices));
        }

        public Material[] ApplyMaterialsToArray(Mesh mesh, Material[] materialArray, Dictionary<MaterialSlotType, Material> materialChoices)
        {
            if (materialChoices.Count == 0)
            {
                return materialArray;
            }
            
            if (!TryGetMapping(mesh, out var mapping))
            {
                return materialArray;
            }

            foreach (var slotType in materialChoices.Keys)
            {
                var newMaterial = materialChoices[slotType];

                materialArray = ApplyMaterialToArray(materialArray, slotType, newMaterial, mapping);
            }

            return materialArray;
        }

        public Material[] ApplyMaterialToArray(Mesh mesh, Material[] materialArray, MaterialSlotType materialSlotType, Material newMaterial)
        {
            if (TryGetMapping(mesh, out var mapping))
            {
                return ApplyMaterialToArray(materialArray, materialSlotType, newMaterial, mapping);
            }

            return materialArray;
        }

        private Material[] ApplyMaterialToArray(Material[] materialArray, MaterialSlotType slotType, Material newMaterial, MaterialSlotsDictionary mapping)
        {
            if (!mapping.TryGetValue(slotType, out var materialIndex))
            {
                Debug.LogWarning($"Material cannot be applied. Slot type '{slotType}' is not defined in mapping of given mesh.");
                return materialArray;
            }

            if (materialIndex >= materialArray.Length)
            {
                Array.Resize(ref materialArray, materialIndex);
            }

            materialArray[materialIndex] = newMaterial;

            return materialArray;
        }

        private bool TryGetMapping(Mesh mesh, out MaterialSlotsDictionary mapping)
        {
            if (!materialSlots.TryGetValue(mesh, out mapping))
            {
                Debug.LogWarning($"No material mappings given for mesh '{mesh.name}' please add them to the materialSlotSetup '{name}'.");
                return false;
            }

            return true;
        }

        private Dictionary<MaterialSlotType, Material> PickRandomMaterials(MaterialSelectionDictionary materialChoices)
        {
            Dictionary<MaterialSlotType, Material> pickedMaterials = new Dictionary<MaterialSlotType, Material>();
            
            foreach (var kvp in materialChoices)
            {
                if (kvp.Value == null || kvp.Value.Count == 0)
                {
                    Debug.LogWarning("Material Choice for side '" + kvp.Key + "' cannot be applied. List is null or empty.");
                    continue;
                }

                pickedMaterials[kvp.Key] = kvp.Value.PickRandom();
            }

            return pickedMaterials;
        }
    }
}