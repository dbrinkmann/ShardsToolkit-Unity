// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// See LICENSE in the repository root.
using UnityEngine;

// Placeholder for the game client's ClothConstraintTuning, with the code stripped and the serialized
// members kept identical. Optional, next to a cloak's EquippableClothSimComponent: in game it scales
// the Cloth's max distances by height, so the top of the cloak stays close to the body and the
// lower cape swings more.
public class ClothConstraintTuning : MonoBehaviour
{
	[SerializeField] private Cloth cloth;
	[SerializeField] private SkinnedMeshRenderer skinnedMesh;
	[Tooltip("Blend separate Max Distance scales from the upper attachment into the lower cape.")]
	[SerializeField] private bool useVerticalGradient = true;
	[Tooltip("Fallback scale when the vertical gradient is disabled.")]
	[Range(0f, 2f)]
	[SerializeField] private float maxDistanceScale = 1f;
	[Tooltip("Max Distance scale for the upper attachment band, measured from the top of the cloak.")]
	[Range(0f, 2f)]
	[SerializeField] private float upperMaxDistanceScale = 0.1f;
	[Tooltip("Max Distance scale for the lower cape.")]
	[Range(0f, 2f)]
	[SerializeField] private float lowerMaxDistanceScale = 0.25f;
	[Tooltip("Fraction of the cloak height that uses the upper scale before blending begins.")]
	[Range(0f, 1f)]
	[SerializeField] private float upperBandHeight = 0.15f;
	[Tooltip("Fraction of the cloak height used to blend from upper to lower scale.")]
	[Range(0f, 1f)]
	[SerializeField] private float blendHeight = 0.25f;
	[Tooltip("Apply the Surface Penetration value below to every cloth vertex.")]
	[SerializeField] private bool useSurfacePenetration;
	[Range(0f, 0.5f)]
	[SerializeField] private float surfacePenetration;
}
