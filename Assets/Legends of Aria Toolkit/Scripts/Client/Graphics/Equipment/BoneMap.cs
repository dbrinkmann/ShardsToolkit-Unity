// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// See LICENSE in the repository root.
using UnityEngine;

// Placeholder for the game client's BoneMap, with the code stripped and the serialized members kept
// identical. Put it on the GameObject of each SkinnedMeshRenderer in a worn mesh prefab. When the
// item is equipped, the client looks up RootBone and every entry of Bones by name on the wearer's
// skeleton and assigns them to the renderer, so Bones must list the bone names in the same order as
// the mesh's bind poses (the order of SkinnedMeshRenderer.bones after import). If any name is not
// found on the wearer, the renderer is switched off.
public class BoneMap : MonoBehaviour {

	public string RootBone;
	public string[] Bones;
}
