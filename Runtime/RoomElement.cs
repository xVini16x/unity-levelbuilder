using UnityEngine;
using UnityLevelEditor.RoomExtension;

namespace UnityLevelEditor.Model
{
    public class RoomElement : MonoBehaviour
    {
        [field: SerializeField, HideInInspector]
        public RoomElementTyp Type { get; set; }

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

        public void ConnectLeftElement(RoomElement left)
        {
            ElementLeft = left;
            left.ElementRight = this;
        }

        public void ConnectFrontElement(RoomElement front)
        {
            ElementFront = front;
            front.ElementBack = this;
        }
        
        public void ConnectBackElement(RoomElement back)
        {
            ElementBack = back;
            back.ElementFront = this;
        }

        public void ConnectRightElement(RoomElement right)
        {
            ElementRight = right;
            right.ElementLeft = this;
        }
    }
}