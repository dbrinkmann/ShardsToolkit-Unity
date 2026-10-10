// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// See LICENSE in the repository root.
using UnityEngine;

// Placeholder for the game client's Mobile component, with the code stripped and the serialized
// members kept identical so creature prefabs built here bind to the client's class. Put it on the
// root of a creature or mount prefab; the client then treats the object as a mobile.
public class Mobile : ClientObject
{
    // Animator.speed while walking or running = movement speed (units/s) * factor. Set each to
    // roughly 1 / the ground speed the walk or run clip was authored for.
    public float RunAnimSpeed = 0.2f;
    public float WalkAnimSpeed = 0.66f;

    public bool HasCustomAnimations = false;
    public CapsuleCollider[] clothSimColliders;
}
