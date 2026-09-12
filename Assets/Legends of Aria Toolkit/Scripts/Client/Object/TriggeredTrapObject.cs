using UnityEngine;
using System.Collections;

public class TriggeredTrapObject : MonoBehaviour 
{
    public Animation TrapAnimator;

    // 0 - Full    
    // 1 - Alt
    // 2 - Hidden
    private void NotifyObjectPropertyChanged(object [] args)
    {
        string name = (string)args[0];
        bool triggered = ((bool)args[1]);
        if( name == "IsTriggered")
        {
			switch(triggered)
            {
                case true:
					TrapAnimator.Play("Trigger");
                    break;
                case false:
					TrapAnimator.Play("Reset");
                    break;
            }
        }
    }
}