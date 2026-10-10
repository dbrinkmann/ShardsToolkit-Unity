// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// See LICENSE in the repository root.
using UnityEngine;

// Placeholder for the game client's RandomAnimIndexBehaviour, with the code stripped and the
// serialized members kept identical. Add it to a creature controller's "attack" state; in game it
// sets the AnimIndex parameter to a random value from 0 to numAnims - 1 when the state is entered,
// so transitions on AnimIndex can pick attack1..attackN.
public class RandomAnimIndexBehaviour : StateMachineBehaviour {
    public int numAnims = 0;
}
