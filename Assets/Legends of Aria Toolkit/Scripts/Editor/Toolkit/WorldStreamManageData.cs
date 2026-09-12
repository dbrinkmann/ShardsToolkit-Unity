using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

public class WorldStreamManageData : ScriptableObject 
{    
    [Serializable]
    public class RegionDefinition
    {
        [SerializeField]
        public Vector2[] GridItems;
        [SerializeField]
        public string Name;
    }

    [SerializeField]
    public int xLimitsx;
    public int xLimitsy;
    public int zLimitsx;
    public int zLimitsy;
    public string prefixScene;
    public string scenePath;

    [SerializeField]
    public RegionDefinition[] RegionDefinitions = new RegionDefinition[0];
}
