// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// See LICENSE in the repository root.
using UnityEngine;

// Placeholder for the game client's ManualBoneMap, with the code stripped and the serialized members
// kept identical. For rigid (non-skinned) items: put it on the worn mesh prefab's root. When the item
// is equipped, each object in ObjectsToReassign is re-parented to the chosen bone of the wearer,
// keeping its local position, rotation and scale. RootBone names the bone when RootBoneType is
// ManualEntry (e.g. "Bip001 Head" for a rigid hat).
public class ManualBoneMap : MonoBehaviour {
    public enum RootBones
    {
        ManualEntry,
        Bip001,
        Bone_Attach_Bow,
        Bone_Attach_HeavyWeapon,
        Bone_Attach_Shield,
        Bone_Attach_Staff,
        Bone_L_Attach_Weapon,
        Bone_L_Attach_Generic,
        Bone_L_Hand_VFX,
        Bone_R_Attach_Weapon,
        Bone_R_Attach_Generic,
        Bone_R_Hand_VFX,
        Bone_Attach_Orb,
    };
    public RootBones RootBoneType;
    public string RootBone;

	public GameObject[] ObjectsToReassign;
}
