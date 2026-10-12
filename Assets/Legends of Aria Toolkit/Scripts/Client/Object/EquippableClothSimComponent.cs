// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// See LICENSE in the repository root.
using UnityEngine;

// Placeholder for the game client's EquippableClothSimComponent, with the code stripped and the
// serialized members kept identical. Put it on the root of a worn mesh prefab that uses Unity Cloth
// (e.g. a cloak) and point clothSim at the Cloth. While worn, the client gives the cloth the wearer's
// body capsule colliders and those of a mount being ridden, unless useSharedBodyColliders is off.
public class EquippableClothSimComponent : MonoBehaviour
{
	[SerializeField] private Cloth clothSim;
	[SerializeField] private bool useSharedBodyColliders = true;
}
