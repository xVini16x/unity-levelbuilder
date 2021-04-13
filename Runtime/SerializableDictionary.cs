using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityLevelEditor.RoomExtension;

namespace UnityLevelEditor.Model
{
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver, ICloneable
    {
        [SerializeField] private List<TKey> keys = new List<TKey>();

        [SerializeField] private List<TValue> values = new List<TValue>();

        // save the dictionary to lists
        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();
            foreach (var pair in this)
            {
                keys.Add(pair.Key);
                values.Add(pair.Value);
            }
        }

        // load dictionary from lists
        public void OnAfterDeserialize()
        {
            this.Clear();

            if (keys.Count != values.Count)
            {
                throw new System.Exception(string.Format("there are {0} keys and {1} values after deserialization. Make sure that both key and value types are serializable."));
            }
        
            for (int i = 0; i < keys.Count; i++)
            {
                this.Add(keys[i], values[i]);
            }
        }

        object ICloneable.Clone() => Clone();
        
        public virtual SerializableDictionary<TKey, TValue> Clone()
        {
            var newDictionary = new SerializableDictionary<TKey, TValue>();
            
            return CloneInto(newDictionary);
        }

        protected SerializableDictionary<TKey, TValue> CloneInto(SerializableDictionary<TKey, TValue> newDictionary)
        {
            if (typeof(ICloneable).IsAssignableFrom(typeof(TKey)))
            {
                for (var i = 0; i < keys.Count; i++)
                {
                    newDictionary.keys.Add((TKey)((ICloneable)keys[i]).Clone());
                }
            }
            else
            {
                newDictionary.keys = keys;
            }

            if (typeof(ICloneable).IsAssignableFrom(typeof(TValue)))
            {
                for (var i = 0; i < values.Count; i++)
                {
                    newDictionary.values.Add((TValue)((ICloneable)values[i]).Clone());
                }
            }
            else
            {
                newDictionary.values = values;
            }
            
            //To load list values into dictionary
            newDictionary.OnAfterDeserialize();

            return newDictionary;
        }
    }
    
    [Serializable]
    public class FloorGridDictionary : SerializableDictionary<Vector2Int, FloorElement>
    {
    }
    
    [Serializable]
    public class MaterialSlotsDictionary : SerializableDictionary<MaterialSlotType, int>
    {
    }

    [Serializable]
    public class MaterialSlotMappingsPerMesh : SerializableDictionary<Mesh, MaterialSlotsDictionary>
    {
        
    }

    [Serializable]
    public class MaterialSelectionDictionary : SerializableDictionary<MaterialSlotType, MaterialListWrapper>
    {
        public override SerializableDictionary<MaterialSlotType, MaterialListWrapper> Clone()
        {
            var newDictionary = new MaterialSelectionDictionary();
            return CloneInto(newDictionary);
        }
    }
    
    [Serializable]
    public class MaterialListWrapper
    {
        [SerializeField] private List<Material> serializedList = new List<Material>();

        public Material this[int index]
        {
            get => serializedList[index];
            set => serializedList[index] = value;
        }

        public int Count => serializedList.Count;

        public Material PickRandom()
        {
            return serializedList.PickRandom();
        }
    }

    [Serializable]
    public class PrefabsPerSide : SerializableDictionary<RoomSide, RoomElementSpawnSettings>
    {
        public override SerializableDictionary<RoomSide, RoomElementSpawnSettings> Clone()
        {
            var newDictionary = new PrefabsPerSide();
            return CloneInto(newDictionary);
        }
    }
    
    [Serializable]
    public class WallsPerDirection : SerializableDictionary<Direction, WallElement>
    {
        
    }
    
    [Serializable]
    public class CornerPerDirection : SerializableDictionary<Direction4Diagonal, CornerElement>
    {
       
    }
}