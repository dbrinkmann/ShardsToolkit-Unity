// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// See LICENSE in the repository root.
using UnityEngine;

// Placeholder for the game client's InventoryObject, with the code stripped and the serialized
// members kept identical. Put it on an item's root. In container windows the client rotates the item
// by InventoryRotation (euler degrees; backpacks, coffins and dressers) and scales it uniformly by
// InventoryScale.
public class InventoryObject : MonoBehaviour
{
	public Vector3 InventoryRotation;
    public float InventoryScale = 1;
}
