using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectTextureVariation : MonoBehaviour
{
    public string[] VariationNames;
    public Texture[] MainTextures;
    public Texture[] SubTexture1;
    public Texture[] SubTexture2;
    public Texture[] MainNormals;
    public Texture[] SubNormals1;
    public Texture[] SubNormals2;
    public Texture[] MainOcclusion;
    public Texture[] SubOcclusion1;
    public Texture[] SubOcclusion2;
    public GameObject MainTexturesObject;
    public GameObject SubTextureObject1;
    public GameObject SubTextureObject2;
    private string CurrentVariation;
    public void NotifyObjectPropertyChanged(object [] args)
    {
        string _name = (string)args[0];

        if (_name == "Variation")
        {
            CurrentVariation = (string)args[1];
            int index = 0;
            for (int i = 0; i < MainTextures.Length; i++)
            {
                if (VariationNames[i] == CurrentVariation)
                {
                    index = i;
                }
            }

            if (MainTexturesObject != null && MainTextures[index] != null)
            {
                Texture normObj = null;
                Texture occObj = null;
                if (MainNormals != null && MainNormals.Length > index)
                {
                    normObj = MainNormals[index];                
                }
                if (MainOcclusion != null && MainOcclusion.Length > index)
                {
                    occObj = MainOcclusion[index];
                }
                SetTexture(MainTexturesObject, MainTextures[index], normObj, occObj);
            }
            if (SubTextureObject1 != null && SubTexture1[index] != null)
            {
                Texture normObj = null;
                Texture occObj = null;
                if (SubNormals1 != null && SubNormals1.Length > index)
                {
                    normObj = SubNormals1[index];
                }
                if (SubOcclusion1 != null && SubOcclusion1.Length > index)
                {
                    occObj = SubOcclusion1[index];
                }
                SetTexture(SubTextureObject1, SubTexture1[index]);
            }
            if (SubTextureObject2 != null && SubTexture2[index] != null)
            {
                Texture normObj = null;
                Texture occObj = null;
                if (SubNormals2 != null && SubNormals2.Length > index)
                {
                    normObj = SubNormals2[index];
                }
                if (SubOcclusion2 != null && SubOcclusion2.Length > index)
                {
                    occObj = SubOcclusion2[index];
                }
                SetTexture(SubTextureObject2, SubTexture2[index]);
            }
        }
    }

    private void SetTexture(GameObject gameObj, Texture texObj, Texture normObj = null, Texture occObj = null)
    {
        Renderer rendererComp = gameObj.GetComponent<Renderer>();
        if (rendererComp != null)
        {
            rendererComp.material.mainTexture = texObj;
            if(normObj != null)
            {
                rendererComp.material.SetTexture("_BumpMap",normObj);
            }
            if(occObj != null)
            {
                rendererComp.material.SetTexture("_Occlusion", occObj);
            }
        }
        else
        {
            foreach(Renderer rendComp in gameObj.transform.GetComponentsInChildren<Renderer>())
            {
                rendComp.material.mainTexture = texObj;
                if (normObj != null)
                {
                    rendererComp.material.SetTexture("_BumpMap", normObj);
                }
                if (occObj != null)
                {
                    rendererComp.material.SetTexture("_Occlusion", occObj);
                }
            }
        }
    }
}
