// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// See LICENSE in the repository root.
using UnityEngine;
using System;

// Placeholder for the game client's EquipmentObject component, with the code stripped and the
// serialized members kept identical. On a mount prefab: EquipmentSlot = "Mount" and MountBone =
// the name of the transform the rider's feet are placed on.
public class EquipmentObject : MonoBehaviour {

    public enum WeaponGripTypes
    {
        Unarmed,
        OneHanded,
        TwoHanded,
        Staves,
		Spear,
        Bow,
        Orb,
    }
    public WeaponGripTypes WeaponGrip;

    [Serializable]
    public struct ClientIdMeshPrefabPair
    {
        public Int32 ClientId;
        public GameObject MeshPrefab;
    }

    // maps client id to equipment prefab (0 is default)
    public GameObject DefaultMeshPrefab;
    public ClientIdMeshPrefabPair[] EquipmentMeshPrefabs;

	public string EquipmentSlot;

    public string MountBone;
    public Vector3 MountBoneOffset = new Vector3(0, 0, 0);

    public string EquipmentMaterial;

    public bool HideTorch;
}
