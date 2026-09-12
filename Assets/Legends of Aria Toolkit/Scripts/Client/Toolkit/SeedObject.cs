// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// Originally part of the Shards Engine Toolkit; released under the MIT License.
// See LICENSE in the repository root.
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SeedObject : MonoBehaviour 
{    
    // For dynamic editor objects, this tells the server what template to create the object from
    public string ServerTemplateId = "";
    // ObjVars to automatically attach to this dynamic object
    public List<ShardsObjVar> ObjVars = new List<ShardsObjVar>();

    [ReadOnly]
    public uint Id = uint.MaxValue;

    public bool IsPrefab { get { return transform.root.name == "Prefabs"; } }
}
