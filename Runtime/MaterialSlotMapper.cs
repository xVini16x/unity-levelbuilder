using UnityEngine;
using UnityLevelEditor.Model;

namespace UnityLevelEditor.RoomExtension
{
    public class MaterialSlotMapper : MonoBehaviour
    {
        [SerializeField] private MaterialSlotsDictionary materialSlots;

        public int GetMaterialSlotIndex(MaterialSlotType materialSlotType)
        {
            return materialSlots[materialSlotType];
        }
    }
}