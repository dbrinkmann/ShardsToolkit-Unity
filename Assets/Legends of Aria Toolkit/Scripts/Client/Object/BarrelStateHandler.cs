using UnityEngine;
using System.Collections;

public class BarrelStateHandler : MonoBehaviour 
{
    public GameObject FullMesh;
    public GameObject DepletedMesh;

    // 0 - Full    
    // 1 - Depleted
    private void SetVisualState(int _stateIndex)
    {
        if( _stateIndex != stateIndex )
        {
            switch(_stateIndex)
            {
                case 0:
                    DepletedMesh.SetActive(false);
                    FullMesh.SetActive(true);
                    break;
                case 1:
                    DepletedMesh.SetActive(true);
                    FullMesh.SetActive(false);
                    break;
            }
            stateIndex = _stateIndex;
        }
    }

    private int stateIndex;
}
