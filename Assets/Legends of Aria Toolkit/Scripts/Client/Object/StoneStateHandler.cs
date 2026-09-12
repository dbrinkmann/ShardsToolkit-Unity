using UnityEngine;
using System.Collections;

public class StoneStateHandler : MonoBehaviour, IStateHandler
{
    public GameObject DefaultMesh;
    public GameObject DepletedMesh;

    // 0 - Full    
    // 1 - Depleted    
    private void SetVisualState(int _stateIndex)
    {
        if (_stateIndex != stateIndex)
        {
            switch (_stateIndex)
            {
                case 0:
                default:
                    DefaultMesh.SetActive(true);
                    DepletedMesh.SetActive(false);
                    break;
                case 1:
                    DefaultMesh.SetActive(false);
                    DepletedMesh.SetActive(true);
                    break;
            }
            stateIndex = _stateIndex;
        }
    }

    /* TeravisionGames::Added for updating better */
    public void UpdateVisualState(int _stateIndex)
    {
        SetVisualState(_stateIndex);
    }

    private int stateIndex;
}
