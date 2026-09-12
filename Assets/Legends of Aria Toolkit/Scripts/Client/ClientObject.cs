using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using CoreUtil;
using CoreUtil.ShardEngineMath;
public class ClientObject : MonoBehaviour 
{			
	// These values can be set in the inspector
	// Surface type to determine footstep sounds (not used atm)	
	public string SurfaceType = "None";	
	public string AudioIdentifier;		
	public Int32 ClientId;
    public string CustomObjectLibrary;

    public bool IsPermanent { get { return true; } }
    public Shader CloakedShader = null;

    public bool HideObjectHandle;

    public bool NoInteract;
    public bool PlacementSurface;

    public bool IsResource;

    public GameObject TargetSplat;

    public enum ObjectCategory
    {
        Misc,
        Weapons,
        Tools,
        ClothingArmor,
        Consumables,
        Resources,
        Documents,
        Decorations,
        Structures,
    }
    public ObjectCategory Category;
	
    [ReadOnly]
    public Int32 PermanentId;
    public bool doNotTransparent = false;
    

}
