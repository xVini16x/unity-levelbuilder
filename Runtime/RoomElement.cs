using System;
using System.Linq;
using UnityEngine;
using UnityLevelEditor.RoomExtension;

namespace UnityLevelEditor.Model
{
    public class RoomElement : MonoBehaviour
    {
        [field: SerializeField, HideInInspector]
        public RoomElementTyp Type { get; set; }

        [field: SerializeField]
        public RoomElement ElementFront { get; private set; }

        [field: SerializeField]
        public RoomElement ElementLeft { get; private set; }

        [field: SerializeField]
        public RoomElement ElementBack { get; private set; }

        [field: SerializeField]
        public RoomElement ElementRight { get; private set; }

        [field: SerializeField, HideInInspector]
        public ExtendableRoom ExtendableRoom { get; set; }

        [field: SerializeField, HideInInspector]
        public SpawnOrientation SpawnOrientation { get; set; }

        public void ConnectLeftElement(RoomElement left)
        {
            ElementLeft = left;
            if (left != null)
            {
                left.ElementRight = this;
            }
        }

        public void ConnectFrontElement(RoomElement front)
        {
            ElementFront = front;
            if (front != null)
            {
                front.ElementBack = this;
            }
        }
        
        public void ConnectBackElement(RoomElement back)
        {
            ElementBack = back;
            if (back != null)
            {
                back.ElementFront = this;  
            }
        }

        public void ConnectRightElement(RoomElement right)
        {
            ElementRight = right;
            if (right != null)
            {
                right.ElementLeft = this;
            }
        }

        public RoomElement GetRoomElementByDirection(Direction direction)
        {
            switch(direction)
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

        public void CopyNeighbors(RoomElement toCopy)
        {
            foreach (var direction in Enum.GetValues(typeof(Direction)).Cast<Direction>())
            {
                ConnectElementByDirection(toCopy.GetRoomElementByDirection(direction), direction);
            }
        }
    }
}