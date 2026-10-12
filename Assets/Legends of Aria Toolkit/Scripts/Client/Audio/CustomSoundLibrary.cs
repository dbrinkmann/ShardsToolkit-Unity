// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// See LICENSE in the repository root.
using UnityEngine;
using System;

// Placeholder for the game client's CustomSoundLibrary, with identical serialized members (it has
// no code in the client either). Build Mod Package fills it on the library prefab from the audio
// files in the library's Sounds folder; see CustomSoundBuilder. Each variant holds the file's raw
// bytes (.ogg, .wav, ...) as a TextAsset, which FMOD decodes itself in game. Sounds are addressed
// as "mod:/<bundle>/<name>" wherever the game takes an FMOD event path. The serialized layout must
// match the client's class.
public class CustomSoundLibrary : MonoBehaviour
{
    [Serializable]
    public class SoundEntry
    {
        // lowercase, e.g. "wolf_attack"; numbered files (wolf_attack_1, wolf_attack_2) are variants
        public string Name;
        public TextAsset[] Variants;
        public float Volume = 1f;
        public float PitchMin = 1f;
        public float PitchMax = 1f;
        public float MinDistance = 1f;
        public float MaxDistance = 30f;
        public bool Loop;
        public bool Is3D = true;
        // which game volume slider applies: "sfx", "ambience" or "music"
        public string Group = "sfx";
    }

    public SoundEntry[] Sounds;
}
