using System;
using System.Collections.Generic;
using UnityEngine;
using static System.String;

public class RoomDataComponent : MonoBehaviour
{
    public RoomData roomData { get; set; }
}

[CreateAssetMenu(fileName = "RoomData", menuName = "LevelEditor/RoomData", order = 1)]
public class RoomData : ScriptableObject
{
    [SerializeField] private List<RoomElement> roomElements = new List<RoomElement>();

    public List<RoomElement> RoomElements
    {
        get => roomElements;
    }

    public void AddRoomElement(RoomElement roomElement)
    {
        roomElements.Add(roomElement);
    }
}

[Serializable]
public class RoomElement
{
    [SerializeField] private string guid;
    [SerializeField] private RoomElementTyp type;
    [SerializeField] private string guidFront;
    [SerializeField] private string guidLeft;
    [SerializeField] private string guidBack;
    [SerializeField] private string guidRight;

    public Guid Guid
    {
        get
        {
            if (realGuid == Guid.Empty)
            {
                if (IsNullOrEmpty(guid))
                {
                    return realGuid;
                }

                realGuid = Guid.Parse(guid);
            }

            return realGuid;
        }

        set
        {
            realGuid = value;
            guid = value.ToString();
        }
    }

    public RoomElementTyp Type
    {
        get => type;
        set => type = value;
    }

    public Guid GuidFront
    {
        get => GetGuid(guidFront, ref realGuidFront);
        set
        {
            realGuidFront = value;
            guidFront = value.ToString();
        }
    }
    public Guid GuidLeft
    {
        get => GetGuid(guidLeft, ref realGuidLeft);
        set
        {
            realGuidLeft = value;
            guidLeft = value.ToString();
        }
    }
    public Guid GuidBack
    {
        get => GetGuid(guidBack, ref realGuidBack);
        set
        {
            realGuidBack = value;
            guidBack = value.ToString();
        }
    }
    public Guid GuidRight 
    { 
        get => GetGuid(guidRight, ref realGuidRight); 
        set 
        { 
            realGuidRight = value;
            guidRight = value.ToString();
        } 
    }

    private Guid realGuid;
    private Guid realGuidFront;
    private Guid realGuidLeft;
    private Guid realGuidBack;
    private Guid realGuidRight;

    private Guid GetGuid(string serializedGuid, ref Guid guidField)
    {
        if (guidField == Guid.Empty)
        {
            if (IsNullOrEmpty(serializedGuid))
            {
                return guidField;
            }

            guidField = Guid.Parse(serializedGuid);
        }

        return guidField;
    }
}

public enum RoomElementTyp
{
    Wall,
    Floor,
    Corner
}
