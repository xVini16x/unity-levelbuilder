using UnityEngine;

namespace UnityLevelEditor.Model
{
    public class RoomElement : MonoBehaviour
    {
        [SerializeField] private RoomElementTyp type;
        [SerializeField] private RoomElement elementFront;
        [SerializeField] private RoomElement elementLeft;
        [SerializeField] private RoomElement elementBack;
        [SerializeField] private RoomElement elementRight;

        public RoomElementTyp Type
        {
            get => type;
            set => type = value;
        }

        public void ConnectLeftElement(RoomElement left)
        {
            elementLeft = left;
            left.elementRight = this;
        }

        public void ConnectBackElement(RoomElement back)
        {
            elementBack = back;
            back.elementFront = this;
        }

        public void ConnectRightElement(RoomElement right)
        {
            elementRight = right;
            right.elementLeft = this;
        }

        public void ConnectFrontElement(RoomElement front)
        {
            elementFront = front;
            front.elementBack = this;
        }
    }
}