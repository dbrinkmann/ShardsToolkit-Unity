using UnityEngine;
using System.Collections;

public class PlayAnimationDelayed : MonoBehaviour {

    public float MinDelay;
    public float MaxDelay;
    public string AnimName;
    public bool RandomizeStartTime = true;

	// Use this for initialization
	void Start () 
    {
        if (string.IsNullOrEmpty(AnimName))
        {
            Debug.LogWarningFormat("PlayAnimationDelayed starting without AnimName set! ({0})", transform.name);
            return;
        }
        StartCoroutine(PlayAnimDelayed(Random.Range(MinDelay, MaxDelay)));
	}

    IEnumerator PlayAnimDelayed(float timeDelay)
    {
        yield return new WaitForSeconds(timeDelay);

        float startTime = RandomizeStartTime ? Random.Range(0f, 1f) : 0.0f;

        Animator animComp = GetComponent<Animator>();
        if (animComp != null)
        {
            animComp.Play(AnimName, 0, startTime);
        }
        else
        {
            Animation animComp2 = GetComponent<Animation>();
            if (animComp2 && animComp2.GetClip(AnimName) != null)
            {
                animComp2.Play(AnimName);
                animComp2[AnimName].time = animComp2[AnimName].length * startTime;
            }
            else
            {
                Debug.LogErrorFormat("Couldn't find animation {0} on PlayAnimationDelayed {1}", AnimName, transform.name);
            }
        }
    }
}
