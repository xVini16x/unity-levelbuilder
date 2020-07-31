using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityLevelEditor.Model;

namespace UnityLevelEditor.RoomExtension
{
    public class ExtendableRoom : MonoBehaviour
    {
        [field: SerializeField, HideInInspector]
        public List<ElementSpawner> ElementSpawner { get; private set; }

        [field: SerializeField, HideInInspector]
        public FloorGridDictionary FloorGridDictionary { get; set; }
        
        [field: SerializeField, HideInInspector]
        public Material TransparentMaterial { get; set; }
        
        [field: SerializeField, HideInInspector]
        public Material WallSideMaterial { get; set; }

        public void SetElementSpawner(Dictionary<RoomElementType, ElementSpawner> elementSpawnerByType)
        {
            ElementSpawner = new List<ElementSpawner>();
            var kvpsOrdered = elementSpawnerByType.OrderBy(kvp => (int) kvp.Key);
            
            foreach (var keyValuePair in kvpsOrdered)
            {
                ElementSpawner.Add(keyValuePair.Value);
            }
        }
    }
}