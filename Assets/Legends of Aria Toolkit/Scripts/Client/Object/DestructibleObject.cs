using UnityEngine;
using System.Collections;

public class DestructibleObject : MonoBehaviour 
{
    public GameObject FullMesh;
    public GameObject AlternateMesh;
	public GameObject DestroyEffect;

    // 0 - Full    
    // 1 - Alt
    // 2 - Hidden
    private void NotifyObjectPropertyChanged(object [] args)
    {
        string name = (string)args[0];
        bool isDestroyed = ((bool)args[1]);
        if( name == "IsDestroyed")
        {
            StartCoroutine(ChooseMesh(isDestroyed));
        }
    }

	private void PlayDestroyEffect()
	{
		GameObject effectObj = Instantiate(DestroyEffect, Vector3.zero, new Quaternion()) as GameObject;

		AutoDestroyParticle destroyComp = effectObj.GetComponent<AutoDestroyParticle>();
		if( destroyComp == null )
		{
			destroyComp = effectObj.AddComponent<AutoDestroyParticle>();
		}

		effectObj.transform.parent = transform;
		effectObj.transform.localPosition = Vector3.zero;
		effectObj.transform.localRotation = Quaternion.Euler(Vector3.zero);
	}

    IEnumerator ChooseMesh(bool isDestroyed)
    {
        yield return new WaitForSeconds(0.35f);
        switch (isDestroyed)
        {
            case true:
                AlternateMesh.SetActive(true);
                FullMesh.SetActive(false);
                PlayDestroyEffect();
                break;
            case false:
                AlternateMesh.SetActive(false);
                FullMesh.SetActive(true);
                break;
        }
        stateIndex = isDestroyed;
    }

    private bool stateIndex;
}
