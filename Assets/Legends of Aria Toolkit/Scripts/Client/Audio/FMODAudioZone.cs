// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// See LICENSE in the repository root.
using UnityEngine;
using System.Collections.Generic;

// Placeholder for the game client's FMODAudioZone component, with the code stripped and the
// serialized members kept identical. The client plays EventPath (3D if the event is 3D) and/or
// MusicEventPath while the local player is inside the zone's radius, or its box if CubeZone is set.
// Only the editor gizmo is kept, so the zone can be seen while placing it.
public class FMODAudioZone : MonoBehaviour
{
    public float ZoneRadius = 10.0f;

    public string EventPath = "";
    public string MusicEventPath = "";

    public bool OverrideAttenuation = false;
    public float OverrideMinDistance = -1.0f;
    public float OverrideMaxDistance = -1.0f;

    public bool CubeZone = false;
    public float CubeX = 10.0f;
    public float CubeY = 10.0f;
    public float CubeZ = 10.0f;

    public List<LinkAudioZone> LinkAudioZones = new List<LinkAudioZone>();

    void OnDrawGizmosSelected()
    {
        if (CubeZone == false)
        {
            //If OverrideAttenuation is turned on, draw overriden zones only
            if (!OverrideAttenuation)
            {
                Gizmos.DrawWireSphere(transform.position, ZoneRadius);
            }
            else
            {
                Gizmos.DrawWireSphere(transform.position, OverrideMinDistance);
                Gizmos.DrawWireSphere(transform.position, OverrideMaxDistance);
            }
        }
        else
        {
            Gizmos.DrawWireCube(transform.position, new Vector3(CubeX, CubeY, CubeZ));
        }
    }
}
