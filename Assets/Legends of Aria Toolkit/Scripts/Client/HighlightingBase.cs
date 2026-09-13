// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// Originally part of the Shards Engine Toolkit; released under the MIT License.
// See LICENSE in the repository root.
using UnityEngine;

namespace HighlightingSystem
{
    // Placeholder for the highlighting component used by the game client, so the toolkit's
    // camera prefabs resolve it instead of showing a missing script. The real implementation
    // is a third-party package that is not distributed with this toolkit, and the toolkit does
    // not use this component for anything -- the fields exist only so values already stored on
    // those prefabs still bind.
    public class HighlightingBase : MonoBehaviour
    {
        public float offsetFactor;
        public float offsetUnits;

        [SerializeField]
        protected int _downsampleFactor;

        [SerializeField]
        protected int _iterations;

        [SerializeField]
        protected float _blurMinSpread;

        [SerializeField]
        protected float _blurSpread;

        [SerializeField]
        protected float _blurIntensity;
    }
}
