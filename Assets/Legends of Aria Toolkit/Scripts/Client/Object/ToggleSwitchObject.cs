using UnityEngine;
using System.Collections;

public class ToggleSwitchObject : MonoBehaviour 
{
    public Animation SwitchAnimator;

    // 0 - Full    
    // 1 - Alt
    // 2 - Hidden
    private void NotifyObjectPropertyChanged(object [] args)
    {
        string name = (string)args[0];
        bool triggered = ((bool)args[1]);
        if( name == "IsActivated")
        {
			switch(triggered)
            {
                case true:
					//Debug.Log("Activate");
					SwitchAnimator.Play("Activate");
                    break;
                case false:
					//Debug.Log("Reset");
					SwitchAnimator.Play("Reset");
                    break;
            }
        }
    }
}
