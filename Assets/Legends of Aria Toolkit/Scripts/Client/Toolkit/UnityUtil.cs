// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// Originally part of the Shards Engine Toolkit; released under the MIT License.
// See LICENSE in the repository root.
using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using CoreUtil.ShardEngineMath;

public class ReadOnlyAttribute : PropertyAttribute
{

}

public class CustomCursor
{
	public Texture2D CursorTexture;
	public Vector2 Hotspot;
}

[System.Serializable]
public class ShardsObjVar
{
    public enum RestrictedType
    {
        Number,
        String,
    }

    public enum ObjVarType
    {
        Number,
        String,
        Boolean,
        Loc,
    }
    public ObjVarType Type;
    public string Name;
    public string StrValue;
    public double DoubleValue;
    public bool BoolValue;
    public Vector3 LocValue;
}

public static class UnityUtil 
{
    public enum SurfaceHitType
    {
        None,
        Terrain,
        Object
    }

    [Flags]
    public enum SurfaceType
    {
        Static = 1,
        Dynamic = 2,
        Placement = 4,
        StaticAndDynamic = 3,
        All = 7,
    }

    public static int DefaultLayer { get { return LayerMask.NameToLayer("Default"); } }
    public static int InteriorLitLayer { get { return LayerMask.NameToLayer("InteriorLit"); } }
    public static int NoPickLayer { get { return LayerMask.NameToLayer("NoPick"); } }
    public static int CarriedObjectLayer { get { return LayerMask.NameToLayer("CarriedObject"); } }
    public static int SurfaceLayer { get { return LayerMask.NameToLayer("Surface"); } }
    public static int RoofLayer { get { return LayerMask.NameToLayer("Roof"); } }
    public static int OutlineLayer { get { return LayerMask.NameToLayer("Outline"); } }
    public static int MobilesLayer { get { return LayerMask.NameToLayer("Mobiles"); } }
    public static int PickerColliderLayer { get { return LayerMask.NameToLayer("PickerCollider"); } }
    public static int PickerCollider2Layer { get { return LayerMask.NameToLayer("PickerCollider2"); } }
    public static int TransparentFXLayer { get { return LayerMask.NameToLayer("TransparentFX"); } }

    public static Vector3 OriginOffset { get { return MapData.Instance.OriginOffset; } }
	
	// DAB NOTE: The y for all positions on the server are offsets from the client surface height
	
    public static Vector3 ConvertToClientPos(Vec3 position, bool ignoreSurfaces = false)
    {		
        Vector3 clientSurfaceLoc = new Vector3(position.X + OriginOffset.x, 0, position.Z + OriginOffset.z);

        return new Vector3(clientSurfaceLoc.x, (ignoreSurfaces ? 0 : GetDynamicSurfaceY(clientSurfaceLoc)) + position.Y, clientSurfaceLoc.z);
    }

    public static Vector2 ConvertToClientPos2(Vec3 position)
    {
        Vector2 clientSurfaceLoc = new Vector2(position.X + OriginOffset.x, position.Z + OriginOffset.z);

        return clientSurfaceLoc;
    }

    public static Vector3 ConvertToClientPos(Vec2 position, bool ignoreSurfaces = false)
	{		
		Vector3 clientSurfaceLoc = new Vector3(position.X + OriginOffset.x, 0, position.Z + OriginOffset.z);

        return new Vector3(clientSurfaceLoc.x, (ignoreSurfaces ? 0 : GetDynamicSurfaceY(clientSurfaceLoc)), clientSurfaceLoc.z);
	}
	
	public static Vec3 ConvertToClientVec3(Vec3 serverVec3)
	{
		Vec3 clientSurfaceLoc = new Vec3(serverVec3.X + OriginOffset.x, 0, serverVec3.Z + OriginOffset.z);

        return new Vec3(clientSurfaceLoc.X, GetDynamicSurfaceY(CreateFromVec3(clientSurfaceLoc)) + serverVec3.Y, clientSurfaceLoc.Z);
	}

    public static Vec3 ConvertToServerPos(Vector3 position, bool ignoreSurfaces = false)
    {
        float serverY = position.y;
        if (!ignoreSurfaces)
        {
            serverY = serverY - GetDynamicSurfaceY(position);
        }
						
        return new Vec3()
        {
            X = position.x - OriginOffset.x,
            Y = serverY,
            Z = position.z - OriginOffset.z
        };
    }

    public static Vec2 ConvertToServerPos2(Vector3 position)
    {
        return new Vec2()
        {
            X = position.x - OriginOffset.x,
            Z = position.z - OriginOffset.z
        };
    }

	public static Vec2 ConvertToServerPos(Vec2 position)
	{
		return new Vec2(position.X - OriginOffset.x, position.Z - OriginOffset.z);
	}
	
	public static Rect3 ConvertToClientRect(Rect3 serverRect)
	{
		return new Rect3(new Vec3[]
		{
			UnityUtil.ConvertToClientVec3(serverRect.Points[0]),	
			UnityUtil.ConvertToClientVec3(serverRect.Points[1]),
			UnityUtil.ConvertToClientVec3(serverRect.Points[2]),
			UnityUtil.ConvertToClientVec3(serverRect.Points[3]),
			UnityUtil.ConvertToClientVec3(serverRect.Points[4]),
			UnityUtil.ConvertToClientVec3(serverRect.Points[5]),
			UnityUtil.ConvertToClientVec3(serverRect.Points[6]),
			UnityUtil.ConvertToClientVec3(serverRect.Points[7]),
		});			
	}
	
	public static float GetStaticSurfaceY(Vector3 position)
	{
		RaycastHit hit;
		if( GetSurfaceHit(position, out hit, SurfaceType.Static) != UnityUtil.SurfaceHitType.None)
		{
			return hit.point.y;
		}
        return 0;
	}

    public static float GetDynamicSurfaceY(Vector3 position)
    {
        RaycastHit hit;
        if (GetSurfaceHit(position, out hit, SurfaceType.StaticAndDynamic) != UnityUtil.SurfaceHitType.None)
        {
            return hit.point.y;
        }        
        return 0;
    }

    public static float GetPlacementSurfaceY(Vector3 position)
    {
        RaycastHit hit;
        if (GetSurfaceHit(position, out hit, SurfaceType.All) != UnityUtil.SurfaceHitType.None)
        {
            return hit.point.y;
        }
        return 0;
    }

    public static Vector2 CreateFromVec2(Vec2 position)
    {
        return new Vector2(position.X, position.Z);
    }

	public static Vector3 CreateFromVec3(Vec3 position)
	{
		return new Vector3(position.X, position.Y, position.Z);
	}
	
	public static Vec3 CreateFromVector3(Vector3 position)
	{
		return new Vec3(position.x, position.y, position.z);
	}

	public static void DoGameLabel(Rect _labelRect, string _text, GUIStyle _labelStyle)
	{	
	    _labelStyle.normal.textColor = new Color(0.0f,0.0f,0.0f,_labelStyle.normal.textColor.a);
	    _labelRect.x +=2;	    
	    _labelRect.y +=2;
	    GUI.Label(_labelRect, _text, _labelStyle);
		_labelRect.x-=2;
	    _labelRect.y-=2;
	    _labelStyle.normal.textColor = new Color(1.0f,1.0f,1.0f,_labelStyle.normal.textColor.a);
	    GUI.Label(_labelRect, _text, _labelStyle);
	}	

    public static Vector3 GetColliderLocalVertexPosition(Transform _colliderTransform, Bounds _bounds)
    {
        Vector3 oldPos = _colliderTransform.position;
        _colliderTransform.position = _colliderTransform.position - _colliderTransform.parent.position;

        Matrix4x4 thisMatrix = _colliderTransform.localToWorldMatrix;

        _colliderTransform.position = oldPos;

        return thisMatrix.MultiplyPoint3x4(_bounds.center);
    }
	
	public static Vector3[] GetColliderWorldVertexPositions (Transform _colliderTransform, Bounds _bounds)
	{
	    Vector3[] vertices = new Vector3[8];		
		
		Matrix4x4 thisMatrix = _colliderTransform.localToWorldMatrix;	
		
		float x = _bounds.center.x - (_bounds.size.x / 2f);
		float y = _bounds.center.y - (_bounds.size.y / 2f);
		float z = _bounds.center.z - (_bounds.size.z / 2f);
		float x2 = x + _bounds.size.x;
		float y2 = y + _bounds.size.y;
		float z2 = z + _bounds.size.z;
		
		vertices[0] = thisMatrix.MultiplyPoint3x4(new Vector3(x,y2,z));
		vertices[1] = thisMatrix.MultiplyPoint3x4(new Vector3(x2,y2,z));
		vertices[2] = thisMatrix.MultiplyPoint3x4(new Vector3(x2,y2,z2));
		vertices[3] = thisMatrix.MultiplyPoint3x4(new Vector3(x,y2,z2));
		vertices[4] = thisMatrix.MultiplyPoint3x4(new Vector3(x,y,z));
		vertices[5] = thisMatrix.MultiplyPoint3x4(new Vector3(x2,y,z));
		vertices[6] = thisMatrix.MultiplyPoint3x4(new Vector3(x2,y,z2));
		vertices[7] = thisMatrix.MultiplyPoint3x4(new Vector3(x,y,z2));
		
	    return vertices;
	}
	
	public static Vector3[] GetColliderLocalVertexPositions (Transform _colliderTransform, Bounds _bounds)
	{
	    Vector3[] vertices = new Vector3[8];		
		
		Vector3 oldPos = _colliderTransform.position;
		_colliderTransform.position = _colliderTransform.position - _colliderTransform.parent.position;
		
		Matrix4x4 thisMatrix = _colliderTransform.localToWorldMatrix;
		
		float x = _bounds.center.x - (_bounds.size.x / 2f);
		float y = _bounds.center.y - (_bounds.size.y / 2f);
		float z = _bounds.center.z - (_bounds.size.z / 2f);
		float x2 = x + _bounds.size.x;
		float y2 = y + _bounds.size.y;
		float z2 = z + _bounds.size.z;
		
		vertices[0] = thisMatrix.MultiplyPoint3x4(new Vector3(x,y2,z));
		vertices[1] = thisMatrix.MultiplyPoint3x4(new Vector3(x2,y2,z));
		vertices[2] = thisMatrix.MultiplyPoint3x4(new Vector3(x2,y2,z2));
		vertices[3] = thisMatrix.MultiplyPoint3x4(new Vector3(x,y2,z2));
		vertices[4] = thisMatrix.MultiplyPoint3x4(new Vector3(x,y,z));
		vertices[5] = thisMatrix.MultiplyPoint3x4(new Vector3(x2,y,z));
		vertices[6] = thisMatrix.MultiplyPoint3x4(new Vector3(x2,y,z2));
		vertices[7] = thisMatrix.MultiplyPoint3x4(new Vector3(x,y,z2));
		
		_colliderTransform.position = oldPos;
		
	    return vertices;
	}
	
	public static bool GetTerrainTextureAtLocation(Vector3 position, out Int32 textureIndex, out float weight)
	{
        TerrainData activeTerrain = null;
        Vector3 terPos = Vector3.zero, terMax = Vector3.zero;
        foreach(var terrainObj in MapData.Instance.TerrainColliders)
        {
            terPos = terrainObj.transform.position;
            terMax = terPos + terrainObj.terrainData.size;
            if (position.x >= terPos.x && position.x < terMax.x && position.z >= terPos.z && position.z < terMax.z)
            {
                activeTerrain = terrainObj.terrainData;
                break;
            }
        }

        if( activeTerrain == null )
        {
            textureIndex = 0;
            weight = 0;
            return false;
        }
		
		// Set up:
        Vector3 lookupPos = position - terPos;
		Vector3 TS; // terrain size
		Vector2 AS; // control texture size

        TS = activeTerrain.size;
        AS.x = activeTerrain.alphamapWidth;
        AS.y = activeTerrain.alphamapHeight;
		 		 
		// Lookup texture we are standing on:
        int AX = (int)((lookupPos.x / TS.x) * AS.x + 0.5f);
        int AY = (int)((lookupPos.z / TS.z) * AS.y + 0.5f);

        float[, ,] TerrCntrl = activeTerrain.GetAlphamaps(AX, AY, 1, 1);
		// This can get a grid. Since we are only getting 1, we have a 1x1 array
		// The 3rd is the 0-1 weight of that texture (if you have 4 textures, the 3rd
		//   has size 4. TerrCntrl[0,0,0] is the weigth of texture#0.)
		// TC[0,0, 0-??] add to 1
		weight = TerrCntrl[0,0,0];
		textureIndex = 0;
		for(int i = 1; i < TerrCntrl.GetLength(2); i++)
		{
			float curWeight = TerrCntrl[0,0,i];
			if( curWeight > weight )
			{
				weight = curWeight;
				textureIndex = i;
			}
		}
		
		return weight > 0;
	}
	
	public static Color ConvertToColor(UInt32 _id)
	{		
        float a = ((byte)(_id >> 24)) / 255.0f;
        float r = ((byte)(_id >> 16)) / 255.0f;
        float g = ((byte)(_id >> 8)) / 255.0f;
        float b = ((byte)_id) / 255.0f;
		
		return new Color(r,g,b,a);
	}
	
	public static UInt32 GetIdFromColor(Color _color)
	{
		UInt32 a = (UInt32)(_color.a * 255);
        UInt32 r = (UInt32)(_color.r * 255);
		UInt32 g = (UInt32)(_color.g * 255);
		UInt32 b = (UInt32)(_color.b * 255);
		
		return (a << 24) | (r << 16) | (g << 8) | b;
	}

    // DAB Optimization: This is a prime candidate for optimization
    public static SurfaceHitType GetSurfaceHit(Ray ray, out RaycastHit hit, SurfaceType surfaceTypes)
    {
        SurfaceHitType hitType = SurfaceHitType.None;        

        float highestY = float.MinValue;

        hit = new RaycastHit();

        // first try terrain
        RaycastHit terrainHit;
        bool includeTerrain = (surfaceTypes & SurfaceType.Static) == SurfaceType.Static;
        if (includeTerrain)
        {
            IEnumerable<TerrainCollider> terrainCollection;
            if (MapData.Instance.TerrainColliders.Count > 0)
            {
                terrainCollection = MapData.Instance.TerrainColliders;
            }
            else
            {
                terrainCollection = GameObject.FindGameObjectsWithTag("Terrain").Select(item => item.GetComponent<TerrainCollider>());
            }

            foreach (TerrainCollider terrainObj in terrainCollection)
            {
                if (terrainObj != null && terrainObj.Raycast(ray, out terrainHit, 1000.0f))
                {
                    hitType = SurfaceHitType.Terrain;
                    highestY = terrainHit.point.y;
                    hit = terrainHit;
                    break;
                }
            }
        }

        // next try surface objects
        RaycastHit[] surfaceHits = Physics.RaycastAll(ray, Mathf.Infinity, 1 << SurfaceLayer);
        if (surfaceHits != null && surfaceHits.Length > 0)
        {
            foreach (RaycastHit surfaceHit in surfaceHits)
            {
                // DAB HACK: Ignore dynamic objects, when getting dynamic positions they were hitting themselves
                Transform rootTransform = surfaceHit.transform.root;
                bool isIgnored = rootTransform.gameObject != null && (rootTransform.gameObject.name == "SeedObjects" || rootTransform.gameObject.name == "PlacementPreview");
                if (!isIgnored && (hitType == SurfaceHitType.None || surfaceHit.point.y > highestY))
                {
                    bool includeStaticSurfaces = (surfaceTypes & SurfaceType.Static) == SurfaceType.Static;
                    bool includeDynamicSurfaces = (surfaceTypes & SurfaceType.Dynamic) == SurfaceType.Dynamic;
                    bool includePlacementSurfaces = (surfaceTypes & SurfaceType.Placement) == SurfaceType.Placement;
                    bool skipSurfaceHit = false;
                    // only need to check this if one of these flags is not set
                    if (!includeDynamicSurfaces || !includeStaticSurfaces || !includePlacementSurfaces)
                    {

                        ClientObject clientObj = surfaceHit.transform.gameObject.GetComponentInParent<ClientObject>();
                        if (!includeStaticSurfaces && (clientObj == null || clientObj.IsPermanent))
                        {
                            skipSurfaceHit = true;
                        }
                        else if (!includeDynamicSurfaces && clientObj != null && !clientObj.IsPermanent)
                        {
                            skipSurfaceHit = true;
                        }
                        else if (!includePlacementSurfaces && clientObj != null && clientObj.PlacementSurface)
                        {
                            skipSurfaceHit = true;
                        }
                    }

                    // if the surface is higher than the terrain then go with that
                    if (!skipSurfaceHit && (hitType == SurfaceHitType.None || surfaceHit.point.y > highestY))
                    {
                        highestY = surfaceHit.point.y;
                        hitType = SurfaceHitType.Object;
                        hit = surfaceHit;
                    }
                }
            }
        }

        return hitType;
    }

    public static bool RaycastDown(Vector3 _position, int _surfaceMask, out RaycastHit _hit)
    {
        Ray downwardRay = new Ray(new Vector3(_position.x, 255f, _position.z), new Vector3(0f, -1.0f, 0f));

        return Physics.Raycast(downwardRay, out _hit, Mathf.Infinity, 1 << _surfaceMask);
    }
	
	public static UnityUtil.SurfaceHitType GetSurfaceHit(Vector3 position, out RaycastHit hit, SurfaceType surfaceTypes)
	{
		Ray ray = new Ray(new Vector3(position.x,255f,position.z),new Vector3(0f,-1.0f,0f));
        return GetSurfaceHit(ray, out hit, surfaceTypes);	
	}

    public static void SetLayerRecursively(GameObject targetObj, int newLayer, Func<GameObject, bool> filterFunc = null)
	{
        if (filterFunc == null || filterFunc(targetObj))
        {
            targetObj.layer = newLayer;
        }

        foreach (Transform child in targetObj.transform)
    	{
            SetLayerRecursively(child.gameObject, newLayer, filterFunc);	
    	}
	}
    
    public static void SetRendererLayerRecursively(GameObject targetObj, int newLayer)
    {
        SetLayerRecursively(targetObj, newLayer, obj => obj.GetComponent<Renderer>() != null);
    }

	public static Transform SearchHierarchyForTransform(Transform current, string name)   
	{
		// check if the current  is the transform we're looking for, if so return it
		if (current.name.Contains(name))
			return current;
		
		// search through child transforms for the transform we're looking for
		for (int i = 0; i < current.childCount; ++i)
		{
			// the recursive step; repeat the search one step deeper in the hierarchy
			Transform found = SearchHierarchyForTransform(current.GetChild(i), name);
			
			// a transform was returned by the search above that is not null,
			// it must be the transform we're looking for
			if (found != null)
				return found;
		}
		
		// transform with name was not found
		return null;
	}

    public static IEnumerable<Transform> SearchHierarchyForTransforms(Transform current, string name)
    {
        // check if the current  is the transform we're looking for, if so return it
        if (current.name.Contains(name))
            yield return current;

        // search through child transforms for the transform we're looking for
        for (int i = 0; i < current.childCount; ++i)
        {
            // the recursive step; repeat the search one step deeper in the hierarchy
            foreach(Transform found in SearchHierarchyForTransforms(current.GetChild(i), name))
            {
                yield return found;
            }
        }
    }

    public static IEnumerable<Transform> SearchForClientObjects(Transform current)
    {
        // check if the current  is the transform we're looking for, if so return it
        if (current.gameObject.GetComponent<ClientObject>())
            yield return current;

        // search through child transforms for the transform we're looking for
        for (int i = 0; i < current.transform.childCount; ++i)
        {
            // the recursive step; repeat the search one step deeper in the hierarchy
            foreach (Transform found in SearchForClientObjects(current.GetChild(i)))
            {
                yield return found;
            }
        }
    }

    public static string ExtractEffectArg(string _effectArgs, string _argName)
    {
        if (_effectArgs != null && _effectArgs.Length > 0)
        {
            string[] argStrs = _effectArgs.Split(',');
            foreach (var argStr in argStrs)
            {
                int sep = argStr.IndexOf('=');
                if (sep != -1)
                {
                    string argName = argStr.Substring(0, sep);
                    if (argName == _argName)
                    {
                        return argStr.Substring(sep + 1, argStr.Length - (sep + 1));
                    }
                }
            }
        }

        return null;
    }
}
