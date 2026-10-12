// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// See LICENSE in the repository root.
using UnityEngine;
using System;

// Placeholder for the game client's ObjectMaterialVariation, with the code stripped and the
// serialized members kept identical. On an object's root: when the object's "Variation" shared
// property equals a variation's Name, each of its Renderers gets the matching entry of Materials.
public class ObjectMaterialVariation : MonoBehaviour
{
    [Serializable]
    public class MaterialVariation
    {
        public string Name;
        public Renderer[] Renderers;
        public Material[] Materials;
    }

    [SerializeField]
    private MaterialVariation[] materialVariations;
}
