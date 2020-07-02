using System;
using System.Collections.Generic;
using UnityEngine;
using static System.String;

[CreateAssetMenu(fileName = "RoomData", menuName = "LevelEditor/RoomData", order = 1)]
public class RoomData : ScriptableObject
{
    [SerializeField] private List<RoomElement> roomElements;
    
    public List<RoomElement> RoomElements 
    { 
        get => roomElements; 
        set => roomElements = value; 
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
    }
    
    public RoomElementTyp Type
    {
        get => type;
        set => type = value;
    }

    public Guid GuidFront => GetGuid(guidFront, ref realGuidFront);
    public Guid GuidLeft => GetGuid(guidLeft, ref realGuidLeft);
    public Guid GuidBack => GetGuid(guidBack, ref realGuidBack);
    public Guid GuidRight => GetGuid(guidRight, ref realGuidRight);

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

public enum RoomElementTyp {
    Wall,
    Floor,
    Corner
}
