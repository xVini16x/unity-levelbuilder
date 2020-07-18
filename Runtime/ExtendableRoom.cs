﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityLevelEditor.Model;

namespace UnityLevelEditor.RoomExtension
{
    public class ExtendableRoom : MonoBehaviour
    {
        [field: SerializeField, HideInInspector]
        public List<ElementSpawner> ElementSpawner { get; private set; }

        public void SetElementSpawner(Dictionary<RoomElementTyp, ElementSpawner> elementSpawnerByType)
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