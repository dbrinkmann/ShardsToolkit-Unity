using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HueStateHandler : MonoBehaviour, IStateHandler
{
    public int[] Hues;
    public GameObject HueObject;

    /* TeravisionGames::It's way better to cach the renderers */
    Renderer[] _renderers;

    void Awake()
    {
        _renderers = HueObject.GetComponentsInChildren<Renderer>();
    }

    private void SetVisualState(int _stateIndex)
    {
        if (_stateIndex != stateIndex)
        {
            if (HueObject != null && Hues.Length > _stateIndex)
            {
                SetHue(HueObject, Hues[_stateIndex]);
            }
        }
    }

    private void SetHue(GameObject gameObj, int _hue)
    {
        //foreach (Renderer rendComp in gameObj.transform.GetComponentsInChildren<Renderer>())
        //{
        //    rendComp.material.SetInt("_Hue", _hue);
        //}

        foreach (Renderer rendComp in _renderers)
        {
            rendComp.material.SetInt("_Hue", _hue);
        }
    }

    /* TeravisionGames::Added for updating better */
    public void UpdateVisualState(int _stateIndex)
    {
        SetVisualState(_stateIndex);
    }

    private int stateIndex;
}
