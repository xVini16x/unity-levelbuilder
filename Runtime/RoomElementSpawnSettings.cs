using System;
using UnityEngine;

namespace UnityLevelEditor.Model
{
    [Serializable]
    public class RoomElementSpawnSettings
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
    }
}