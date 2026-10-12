// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// See LICENSE in the repository root.
using UnityEngine;

// Placeholder for the game client's MeshReplacement, with the code stripped and the serialized
// members kept identical. Put it on a renderer's GameObject in a worn mesh prefab. While the item is
// equipped, every renderer under the wearer's Model whose name contains an entry of ReplaceMeshes is
// hidden, e.g. "Male_NudeBody_body" (torso and upper arms), "Male_NudeBody_LG" (legs and feet),
// "Male_NudeBody_AM" (forearms and hands), "Male_NudeBody_hair"; "Female_NudeBody_..." for women.
public class MeshReplacement : MonoBehaviour {

	public string[] ReplaceMeshes;
}
