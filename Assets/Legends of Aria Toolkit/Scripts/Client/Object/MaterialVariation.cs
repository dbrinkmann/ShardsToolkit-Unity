// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// See LICENSE in the repository root.
using UnityEngine;

// Placeholder for the game client's MaterialVariation, with the code stripped and the serialized
// members kept identical. On an object's root (or a worn mesh prefab's root): when the object's
// "Variation" shared property equals a variationName, each of the renderers gets that variation's
// materials.
public class MaterialVariation : MonoBehaviour
{
    [System.Serializable]
    public class MaterialVariationDef
    {
        public string variationName;
        public Material[] variationMaterials;
    }

    public Renderer[] renderers;
    public MaterialVariationDef[] Variations;
}
