using UnityEngine;
using System.Collections;

public class DayNightObject : MonoBehaviour 
{    
    public bool DaytimeVisible = true;
    public bool NighttimeVisible = true;
    private const float minDelay = 0.4f;
    private const float maxDelay = 2.2f;
    private Coroutine delayCoroutine;
    void Start()
    {
        if (DayNightCycle.Instance != null)
        {
            DayNightCycle.Instance.AddDayNightObject(this);
		}
    }

    void OnDestroy()
    {
        if (DayNightCycle.Instance != null)
        {
            DayNightCycle.Instance.DayNightObjects.Remove(this);
		}
    }

    public void SetActive(bool bActive)
    {        
    }
    
    private IEnumerator DelayActivation(bool bActive)
    {
        yield return new WaitForSeconds(Random.Range(minDelay, maxDelay));
        gameObject.SetActive(bActive);
        delayCoroutine = null;
    }
}
