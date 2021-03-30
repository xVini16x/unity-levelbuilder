using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityLevelEditor.Model
{
    [CreateAssetMenu(menuName = "RoomBuilder - MaterialSlotSetup", fileName = "New Material Mappings")]
    public class MaterialSlotSetup : ScriptableObject
    {
        [SerializeField] private MaterialSlotMappingsPerMesh materialSlots;
        
        public MaterialSlotMappingsPerMesh MaterialSlots => materialSlots;
    }
}