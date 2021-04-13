using System;
using UnityEngine;

namespace UnityLevelEditor.Model
{
    [Serializable]
    public class RoomElementSpawnSettings : ICloneable
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private MaterialSelectionDictionary materialOverrides;

        public GameObject Prefab
        {
            get => prefab;
            set => prefab = value;
        }

        public MaterialSelectionDictionary MaterialOverrides
        {
            get => materialOverrides;
            set => materialOverrides = value;
        }

        public object Clone()
        {
            var newSettings = new RoomElementSpawnSettings();
            newSettings.prefab = prefab;
            newSettings.materialOverrides = (MaterialSelectionDictionary) materialOverrides.Clone();

            return newSettings;
        }
        
    }
}