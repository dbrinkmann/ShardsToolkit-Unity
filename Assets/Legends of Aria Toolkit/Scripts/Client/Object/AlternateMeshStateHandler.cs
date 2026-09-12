using UnityEngine;
using System.Collections;

public class AlternateMeshStateHandler : MonoBehaviour 
{
	public GameObject[] StateMeshes;

	private void SetVisualState(int _stateIndex)
	{
		if( _stateIndex != stateIndex )
		{
			for(int i=0;i<StateMeshes.Length;i++)
			{
                if (StateMeshes[i] != null)
                {
                    if (_stateIndex == i)
                    {
                        StateMeshes[i].SetActive(true);
                    }
                    else
                    {
                        StateMeshes[i].SetActive(false);
                    }
                }
			}
		}
        stateIndex = _stateIndex;
	}
	
	private int stateIndex;
}