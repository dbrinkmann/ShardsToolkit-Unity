using UnityEngine;
using System.Collections;

public class RayViewer : MonoBehaviour {
    public Ray[] LastRays;

    void OnDrawGizmos()
    {
        if (LastRays != null)
        {
            for (int i = 0; i < LastRays.Length; i++)
            {
                Gizmos.DrawRay(LastRays[i]);
            }
        }
    }
}
