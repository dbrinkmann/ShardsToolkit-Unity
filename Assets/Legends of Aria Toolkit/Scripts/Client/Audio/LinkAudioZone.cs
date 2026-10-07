// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// See LICENSE in the repository root.
using UnityEngine;

// Placeholder for the game client's LinkAudioZone component, with the code stripped and the
// serialized members kept identical. An extra area linked from an FMODAudioZone's LinkAudioZones
// list. Only the editor gizmo is kept, so the area can be seen while placing it.
public class LinkAudioZone : MonoBehaviour
{
    public float ZoneRadius = 10.0f;

    public bool OverrideAttenuation = false;
    public float OverrideMinDistance = -1.0f;
    public float OverrideMaxDistance = -1.0f;

    public bool CubeZone = false;
    public float CubeX = 10.0f;
    public float CubeY = 10.0f;
    public float CubeZ = 10.0f;

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
