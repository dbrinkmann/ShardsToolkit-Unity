using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ShardsToolsData : ScriptableObject
{
    public class MapEntry
    {
        public string SceneName;
        public int NextAvailablePermanentId = 1;
    }
    [SerializeField]
    public List<MapEntry> MapData = new List<MapEntry>();
}