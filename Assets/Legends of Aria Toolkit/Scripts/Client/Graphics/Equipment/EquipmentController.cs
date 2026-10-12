// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// See LICENSE in the repository root.
using UnityEngine;

// Placeholder for the game client's EquipmentController, with the code stripped and the serialized
// members kept identical. The client adds one to a wearer's Model when needed, so most prefabs don't
// need it. A creature prefab that should wear skinned equipment puts one on its Model with
// UsesHumanRig on, or lists the allowed slots in SkinnedOverride; otherwise the client refuses
// skinned items on it.
public class EquipmentController : MonoBehaviour {

    public bool UsesHumanRig = false;
    // this is for non-human mobs that support custom equipment slots
    public string[] SkinnedOverride;
}
