// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// Originally part of the Shards Engine Toolkit; released under the MIT License.
// See LICENSE in the repository root.
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MapData : MonoBehaviour
{
    private static MapData instance;
    public static MapData Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType(typeof(MapData)) as MapData;
            }
            return instance;
        }
    }
    [HideInInspector]
    public List<TerrainCollider> TerrainColliders = new List<TerrainCollider>();
    [HideInInspector]
    public List<Terrain> Terrains = new List<Terrain>();

    public Vector3 OriginOffset = new Vector3(0, 0, 0);
    public Vector2 MapExtents = new Vector2(1024, 1024);

    public TextAsset PermanentObjectsData;
    public TextAsset StaticCollisionData;

    public int BundleVersion;
}
