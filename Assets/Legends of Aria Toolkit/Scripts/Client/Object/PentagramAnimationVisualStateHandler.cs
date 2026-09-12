using UnityEngine;
using System.Collections;

public class PentagramAnimationVisualStateHandler : MonoBehaviour 
{
    public GameObject PentagramEffectPrefab;
    public Animator GroundEffectAnimation1;
    public Animator GroundEffectAnimation2;

    private void CreatePentagram()
    {
        if (pentagramEffect == null)
        {
            pentagramEffect = (GameObject)GameObject.Instantiate(PentagramEffectPrefab);
            pentagramEffect.transform.parent = transform;
            pentagramEffect.transform.localPosition = Vector3.zero;
        }
    }

    private void Start()
    {
        CreatePentagram();
    }

    // 0 - shown 
    // 1 - hidden
    private void SetVisualState(int _stateIndex)
    {
        if( _stateIndex != stateIndex )
        {
            switch(_stateIndex)
            {
                case 0:
                    Debug.Log("Pentagram Visual State 0: Activating Pentagram, Showing Ground Effects");
                    CreatePentagram();

                    GroundEffectAnimation1.SetBool("Visible", true);
                    GroundEffectAnimation2.SetBool("Visible", true);
                    
                    break;
                case 1:
                    Debug.Log("Pentagram Visual State 1: Deactivating Pentagram, Hiding Ground Effects");

                    if (pentagramEffect != null)
                    {
                        GameObject.Destroy(pentagramEffect);
                    }

                    GroundEffectAnimation1.SetBool("Visible", false);
                    GroundEffectAnimation2.SetBool("Visible", false);
                    
                    break;
            }
            stateIndex = _stateIndex;
        }
    }

    GameObject pentagramEffect = null;
    private int stateIndex;

}
