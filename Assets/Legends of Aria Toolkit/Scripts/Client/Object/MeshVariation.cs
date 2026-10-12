// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// See LICENSE in the repository root.
using UnityEngine;

// Placeholder for the game client's MeshVariation, with the code stripped and the serialized members
// kept identical. On an object's root (or a worn mesh prefab's root): when the object's "Variation"
// shared property equals a VariationName, its VisibleMeshes are shown and its HiddenMeshes hidden.
public class MeshVariation : MonoBehaviour
{
    [System.Serializable]
    public class MeshVariationDef
    {
        public string VariationName;
        public GameObject[] VisibleMeshes;
        public GameObject[] HiddenMeshes;
    }
    public MeshVariationDef[] Variations;
}
