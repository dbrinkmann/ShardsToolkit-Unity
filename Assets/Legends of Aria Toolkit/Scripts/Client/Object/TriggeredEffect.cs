using UnityEngine;
using System.Collections;

public class TriggeredEffect : MonoBehaviour 
{
    public GameObject flameObject;

    // 0 - Full    
    // 1 - Alt
    // 2 - Hidden
    private void NotifyObjectPropertyChanged(object [] args)
    {
        if (args[1] is bool)
        {
            string name = (string)args[0];

            bool triggered = ((bool)args[1]);
            if (name == "IsTriggered")
            {
                switch (triggered)
                {
                    case true:
                        foreach (ParticleSystem ps in flameObject.GetComponentsInChildren<ParticleSystem>())
                        {
                            ps.enableEmission = true;
                        }
                        foreach (Light light in flameObject.GetComponentsInChildren<Light>())
                        {
                            light.gameObject.SetActive(true);
                        }
                        break;
                    case false:
                        foreach (ParticleSystem ps in flameObject.GetComponentsInChildren<ParticleSystem>())
                        {
                            ps.enableEmission = false;
                        }
                        foreach (Light light in flameObject.GetComponentsInChildren<Light>())
                        {
                            light.gameObject.SetActive(false);
                        }
                        break;
                }
            }
        }
    }
}