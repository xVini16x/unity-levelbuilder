using System;
using UnityEngine;

namespace UnityLevelEditor.RoomExtension
{

    public class MaterialSlotMapper : MonoBehaviour
    {
        [SerializeField] private int frontMaterialIndex;
        [SerializeField] private int backMaterialIndex;
        [SerializeField] private int topMaterialIndex;
        [SerializeField] private int bottomMaterialIndex;
        [SerializeField] private int leftMaterialIndex;
        [SerializeField] private int rightMaterialIndex;

        public int GetMaterialSlotIndex(MaterialSlotType materialSlotType)
        {
            switch (materialSlotType)
            {
                case MaterialSlotType.Back:
                    return backMaterialIndex;
                case MaterialSlotType.Bottom:
                    return bottomMaterialIndex;
                case MaterialSlotType.Front:
                    return frontMaterialIndex;
                case MaterialSlotType.Left:
                    return leftMaterialIndex;
                case MaterialSlotType.Right:
                    return rightMaterialIndex;
                case MaterialSlotType.Top:
                    return topMaterialIndex;
                default:
                    throw new ArgumentOutOfRangeException(nameof(materialSlotType), materialSlotType, null);
            }
        }
    }
}