using UnityEngine;
using System.Collections;

public class EffectZone : MonoBehaviour {

    public float TransitionSize = 10.0f;

    public float Radius
    {
        get
        {
            if (zoneCollider != null)
            {
                return zoneCollider.radius;
            }

            return 0.0f;
        }
    }

	// Use this for initialization
    protected virtual void Start() 
    {
        zoneCollider = gameObject.GetComponent<Collider>() as SphereCollider;
	}

    public float GetZoneWeight(Vector3 _gameCenter)
    {
        if (Radius > 0.0f)
        {
            float curDistance = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(_gameCenter.x, _gameCenter.z));
            if (curDistance <= Radius)
            {
                float transitionStartDist = Radius - TransitionSize;

                if (curDistance < transitionStartDist)
                {
                    return 1.0f;
                }
                else
                {
                    return 1.0f - ((curDistance - transitionStartDist) / TransitionSize);
                }
            }
        }

        return 0.0f;
    }

    SphereCollider zoneCollider;
}
