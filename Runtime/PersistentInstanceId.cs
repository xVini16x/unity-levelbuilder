﻿using UnityEngine;
using System;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class PersistentInstanceId : MonoBehaviour
{

    [SerializeField]
    private string guidAsString;

    private Guid _guid;

    public Guid Guid
    {
        get
        {
            //Recreate guid in newly loaded game
            if (_guid == Guid.Empty && !String.IsNullOrEmpty(guidAsString))
            {
                _guid = new Guid(guidAsString);
            }
            return _guid;
        }
    }

    void Awake()
    {    
        
    }
    
    public void CreateNewId()
    {
        _guid = Guid.NewGuid();
        guidAsString = Guid.ToString();
    }
}
