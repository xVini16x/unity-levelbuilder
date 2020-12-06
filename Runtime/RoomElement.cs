using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityLevelEditor.RoomExtension;

namespace UnityLevelEditor.Model
{
    public class RoomElement : MonoBehaviour
    {
        [field: SerializeField, HideInInspector]
        public RoomElementType Type { get; set; }

        [field: SerializeField, HideInInspector]
        public RoomElement ElementFront { get; private set; }

        [field: SerializeField, HideInInspector]
        public RoomElement ElementLeft { get; private set; }

        [field: SerializeField, HideInInspector]
        public RoomElement ElementBack { get; private set; }

        [field: SerializeField, HideInInspector]
        public RoomElement ElementRight { get; private set; }

        [field: SerializeField, HideInInspector]
        public ExtendableRoom ExtendableRoom { get; set; }

        [field: SerializeField, HideInInspector]
        public SpawnOrientation SpawnOrientation { get; set; }

        private MeshRenderer MeshRenderer
        {
            get
            {
                if (meshRenderer == null)
                {
                    meshRenderer = GetComponentInChildren<MeshRenderer>();
                }

                return meshRenderer;
            }
        }

        private MeshRenderer meshRenderer;

        private MaterialSlotMapper MaterialSlotMapper
        {
            get
            {
                if (materialSlotMapper == null)
                {
                    materialSlotMapper = GetComponent<MaterialSlotMapper>();
                }

                return materialSlotMapper;
            }
        }

        private MaterialSlotMapper materialSlotMapper;

        #region Neigbor Handling

        public void ConnectLeftElement(RoomElement left)
        {
            if (ElementLeft != null && ElementLeft.ElementRight != null && ElementLeft.ElementRight.Equals(this))
            {
                ElementLeft.ElementRight = null;
            }

            ElementLeft = left;
            if (left != null)
            {
                left.ElementRight = this;
            }
        }

        public void ConnectFrontElement(RoomElement front)
        {
            if (ElementFront != null && ElementFront.ElementBack != null && ElementFront.ElementBack.Equals(this))
            {
                ElementFront.ElementBack = null;
            }

            ElementFront = front;
            if (front != null)
            {
                front.ElementBack = this;
            }
        }

        private void ConnectBackElement(RoomElement back)
        {
            if (ElementBack != null && ElementBack.ElementFront != null && ElementBack.ElementFront.Equals(this))
            {
                ElementBack.ElementFront = null;
            }

            ElementBack = back;
            if (back != null)
            {
                back.ElementFront = this;
            }
        }

        private void ConnectRightElement(RoomElement right)
        {
            if (ElementRight != null && ElementRight.ElementLeft != null && ElementRight.ElementLeft.Equals(this))
            {
                ElementRight.ElementLeft = null;
            }

            ElementRight = right;
            if (right != null)
            {
                right.ElementLeft = this;
            }
        }

        public RoomElement GetRoomElementByDirection(Direction direction)
        {
            switch (direction)
            {
                case Direction.Front:
                    return ElementFront;
                case Direction.Right:
                    return ElementRight;
                case Direction.Back:
                    return ElementBack;
                case Direction.Left:
                    return ElementLeft;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, $"Unsupported direction {direction}");
            }
        }

        public void ConnectElementByDirection(RoomElement roomElement, Direction direction)
        {
            switch (direction)
            {
                case Direction.Front:
                    ConnectFrontElement(roomElement);
                    return;
                case Direction.Right:
                    ConnectRightElement(roomElement);
                    return;
                case Direction.Back:
                    ConnectBackElement(roomElement);
                    return;
                case Direction.Left:
                    ConnectLeftElement(roomElement);
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(direction), direction, $"Unsupported direction {direction}");
            }
        }

#if UNITY_EDITOR
        public void CopyNeighbors(RoomElement copySource)
        {
            Undo.RecordObject(copySource, "");

            foreach (var direction in Enum.GetValues(typeof(Direction)).Cast<Direction>())
            {
                var element = copySource.GetRoomElementByDirection(direction);
                if (element == null)
                {
                    continue;
                }

                Undo.RecordObject(element, "");
                ConnectElementByDirection(element, direction);
                copySource.ConnectElementByDirection(null, direction);
            }
        }

        public void DisconnectFromAllNeighbors()
        {
            Undo.RecordObject(this, "");
            foreach (var direction in Enum.GetValues(typeof(Direction)).Cast<Direction>())
            {
                var neighbor = GetRoomElementByDirection(direction);
                if (neighbor != null)
                {
                    Undo.RecordObject(neighbor, "");
                    var neighborCounterDirectionElement = neighbor.GetRoomElementByDirection(direction.Opposite());
                    if (neighborCounterDirectionElement != null && neighborCounterDirectionElement.Equals(this))
                    {
                        neighbor.ConnectElementByDirection(null, direction.Opposite());
                    }

                    ConnectElementByDirection(null, direction);
                }
            }
        }
#endif

        #endregion

        #region Material Handling
        
        #if UNITY_EDITOR
        public void SetAllMaterialsTransparent()
        {
            var material = ExtendableRoom.TransparentMaterial;
            var materials = MeshRenderer.sharedMaterials;
            Undo.RecordObject(MeshRenderer, "");

            for (var i = 0; i < materials.Length; i++)
            {
                materials[i] = material;
            }

            MeshRenderer.sharedMaterials = materials;
        }

        public void SetTransparentMaterial(MaterialSlotType materialSlotType)
        {
            SetMaterial(ExtendableRoom.TransparentMaterial, materialSlotType);
        }

        public void SetWallSideMaterial(MaterialSlotType materialSlotType)
        {
            SetMaterial(ExtendableRoom.WallSideMaterial, materialSlotType);
        }

        private void SetMaterial(Material material, MaterialSlotType materialSlotType)
        {
            var materials = MeshRenderer.sharedMaterials;
            Undo.RecordObject(MeshRenderer, "");
            materials[MaterialSlotMapper.GetMaterialSlotIndex(materialSlotType)] = material;
            MeshRenderer.sharedMaterials = materials;
        }

        public void CopySideAndTopMaterials(RoomElement copySource)
        {
            var materials = MeshRenderer.sharedMaterials;
            Undo.RecordObject(MeshRenderer, "");
            var copySourceMaterials = copySource.MeshRenderer.sharedMaterials;
            materials[MaterialSlotMapper.GetMaterialSlotIndex(MaterialSlotType.Left)] = copySourceMaterials[copySource.MaterialSlotMapper.GetMaterialSlotIndex(MaterialSlotType.Left)];
            materials[MaterialSlotMapper.GetMaterialSlotIndex(MaterialSlotType.Right)] = copySourceMaterials[copySource.MaterialSlotMapper.GetMaterialSlotIndex(MaterialSlotType.Right)];
            materials[MaterialSlotMapper.GetMaterialSlotIndex(MaterialSlotType.Top)] = copySourceMaterials[copySource.MaterialSlotMapper.GetMaterialSlotIndex(MaterialSlotType.Top)];
            MeshRenderer.sharedMaterials = materials;
        }
#endif
        
        #endregion
    }
}