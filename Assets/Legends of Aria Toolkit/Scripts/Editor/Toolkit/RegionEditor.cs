// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// Originally part of the Shards Engine Toolkit; released under the MIT License.
// See LICENSE in the repository root.
using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Text;
using CoreUtil;
using ShardsXML.WorldData;
using CoreUtil.ShardEngineMath;

public class RegionEditor : EditorWindow
{
    public RegionEditor()
    {
        popupMods = (new string[] { "Default" }).Concat(ModHelpers.RefreshList()).ToArray();        
        modIndex = 0;
    }

    public void OnGUI()
    {
        EditorGUILayout.Space();

        EditorStyles.textField.wordWrap = true;
        if (!ToolUtil.ValidateSettings())
        {
            EditorGUILayout.LabelField("Invalid Settings. Please fix your paths by going to the 'LoA Toolkit/Settings' menu.");
            return;
        }
        else if (!Application.isPlaying && !ToolUtil.EditWhileStopped)
        {
            EditorGUILayout.LabelField("To begin, press play and select a map.");
            return;
        }
        else if (!MapData.Instance)
        {
            EditorGUILayout.LabelField("To begin, press select a map.");
            return;
        }

        GameObject curObj = Selection.activeGameObject;

        GameObject regionRoot = GameObject.Find("Regions");
        if (regionRoot == null)
        {
            regionRoot = new GameObject();
            regionRoot.name = "Regions";
        }

        EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        int newModIndex = EditorGUILayout.Popup("Mod", modIndex, popupMods);
        if (newModIndex != modIndex)
        {
            modIndex = newModIndex;
        }

        EditorGUILayout.Space();

        string worldDataFileName = ShardsExtensionMethods.CombinePaths(ModHelpers.GetRundirPath(modIndex), "mapdata", ToolUtil.currentScene, "WorldData.xml");

        GUI.enabled = File.Exists(worldDataFileName);

        if (GUILayout.Button("Load Regions"))
        {
            if (EditorUtility.DisplayDialog("WARNING", "This will reset all regions in the scene. Any unsaved work will be lost!", "Okay", "Cancel"))
            {
                LoadRegions(worldDataFileName);
            }
        }

        GUI.enabled = true;

        if (GUILayout.Button("Save Regions"))
        {
            if (modIndex == 0 && !ToolUtil.CanWriteToDefault)
            {
                EditorUtility.DisplayDialog("ERROR", "You can not make changes to the default ruleset.", "Okay");
            }
            else if (EditorUtility.DisplayDialog("CONFIRM", "Save current regions for mod " + ModHelpers.GetModName(modIndex) + "? (Filename: " + worldDataFileName + ")", "Okay", "Cancel"))
            {
                SaveRegions(worldDataFileName);
            }
        }
        if (GUILayout.Button("Reset Regions"))
        {
            if (EditorUtility.DisplayDialog("WARNING", "This will reset all regions in the scene. Any unsaved work will be lost!", "Okay", "Cancel"))
            {
                ResetRegions();
            }
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Flatten All"))
        {
            FlattenAll();
        }
        if (GUILayout.Button("Create New Region"))
        {
            if(IsRegionCategory(curObj))
            {
                RegionVisualizer regionObj = CreateNewRegion("New Region", curObj.name);
                Selection.activeGameObject = regionObj.gameObject;
            }
            else
            {
                RegionVisualizer regionObj = CreateNewRegion("New Region", null);
                Selection.activeGameObject = regionObj.gameObject;
            }            
        }
        if( GUILayout.Button("Create New Category"))
        {
            GameObject newCategory = new GameObject();
            newCategory.name = "New Region Category";
            newCategory.transform.parent = regionRoot.transform;
            Selection.activeGameObject = newCategory;
        }

        ToolUtil.DrawSeparator();

        EditorGUILayout.Space();
        
        if(IsRegionNode(curObj))
        {
            EditorGUILayout.LabelField("Region Node Selected", EditorStyles.boldLabel);

            GUI.enabled = false;
            EditorGUILayout.TextField(new GUIContent("Node Index: ", "Automatically generated."), curObj.name);
            GUI.enabled = true;

            if (GUILayout.Button("Move To View"))
            {
                var view = SceneView.currentDrawingSceneView;
                if (view != null)
                {
                    curObj.transform.position = view.pivot;
                }
            }
            if (GUILayout.Button("Duplicate"))
            {
                GameObject newNode = DuplicateRegionNode(curObj);
                Selection.activeGameObject = newNode;
            }            
            if (GUILayout.Button("Delete Node"))
            {
                DeleteRegionNode(curObj);
            }
        }
        else if(IsRegion(curObj))
        {
            EditorGUILayout.LabelField("Region Selected", EditorStyles.boldLabel);

            RegionVisualizer regionObj = curObj.GetComponent<RegionVisualizer>();

            GUI.enabled = false;
            EditorGUILayout.TextField(new GUIContent("Region Name: ", "Rename in heirarchy"), curObj.name);
            GUI.enabled = true;

            if (GUILayout.Button("Add Node"))
            {
                GameObject newNode = AddRegionNode(regionObj);
                Selection.activeGameObject = newNode;
            }
            if (GUILayout.Button("Delete Region"))
            {
                if (EditorUtility.DisplayDialog("WARNING", "This will delete the entire region. Any unsaved work will be lost!", "Okay", "Cancel"))
                {
                    GameObject.DestroyImmediate(curObj);
                }
            }
            if (GUILayout.Button("Flatten Region"))
            {
                FlattenRegion(regionObj);
            }
            if (GUILayout.Button("Duplicate Region"))
            {
                GameObject newRegion = ToolUtil.DuplicateObject(curObj);
                newRegion.name = curObj.name + "1";
                newRegion.transform.parent = curObj.transform.parent;
                Selection.activeGameObject = newRegion;
            }
        }
        else if(IsRegionCategory(curObj))
        {
            EditorGUILayout.LabelField("Region Category Selected", EditorStyles.boldLabel);

            GUI.enabled = false;
            EditorGUILayout.TextField(new GUIContent("Region Category: ", "Rename in heirarchy"), curObj.name);
            GUI.enabled = true;
        }
        else
        {
            GUIStyle centertext = new GUIStyle(GUI.skin.label);
            centertext.alignment = TextAnchor.MiddleCenter;
            EditorGUILayout.LabelField("Create New or Select a Region", centertext);          
        }
    }

    public void Update()
    {
        // This is necessary to make the framerate normal for the editor window.
        Repaint();
    }

    private bool IsRegionNode(GameObject curObj)
    {
        if (curObj == null) return false;

        return curObj.transform.parent != null && curObj.transform.parent.GetComponent<RegionVisualizer>() != null;
    }

    private bool IsRegion(GameObject curObj)
    {
        if (curObj == null) return false;

        return curObj.GetComponent<RegionVisualizer>() != null;
    }

    private bool IsRegionCategory(GameObject curObj)
    {
        if (curObj == null || curObj.transform.parent == null) return false;

        GameObject regionRoot = GameObject.Find("Regions");
        return curObj.transform.parent.gameObject == regionRoot;
    }

    private RegionVisualizer CreateNewRegion(string regionName, string category)
    {
        GameObject regionsRoot = GameObject.Find("Regions");

        Transform catTrans = regionsRoot.transform.Find(category);
        if (catTrans != null)
        {
            regionsRoot = catTrans.gameObject;
        }
        else
        {
            GameObject newObjectRoot = new GameObject();
            newObjectRoot.transform.parent = regionsRoot.transform;
            newObjectRoot.name = category;
            regionsRoot = newObjectRoot;
        }

        GameObject newRegion = new GameObject();
        newRegion.transform.parent = regionsRoot.transform;
        newRegion.name = regionName;

        return newRegion.AddComponent<RegionVisualizer>(); 
    }

    private GameObject AddRegionNode(RegionVisualizer regionObj)
    {
        GameObject newNode = new GameObject();

        var view = SceneView.currentDrawingSceneView;

        newNode.transform.parent = regionObj.transform;
        newNode.name = "RegionArea";
        if(view != null)
        {
            newNode.transform.position = view.pivot;
        }

        newNode.AddComponent<BoxCollider>();

        return newNode;
    }

    private GameObject DuplicateRegionNode(GameObject regionNode)
    {
        GameObject newNode = ToolUtil.DuplicateObject(regionNode);
        newNode.transform.parent = regionNode.transform.parent;

        return newNode;
    }

    private void Flatten(GameObject regionNode)
    {
        ToolUtil.TagTerrains();
        regionNode.transform.localPosition = new Vector3(regionNode.transform.localPosition.x, UnityUtil.GetStaticSurfaceY(regionNode.transform.localPosition), regionNode.transform.localPosition.z);
        regionNode.transform.localScale = new Vector3(regionNode.transform.localScale.x, 1.0f, regionNode.transform.localScale.z);
        BoxCollider collider = regionNode.GetComponent<Collider>() as BoxCollider;
        collider.size = new Vector3(collider.size.x, 0.1f, collider.size.z);
        collider.center = new Vector3(collider.center.x, 0.0f, collider.center.z);        
    }

    private void FlattenRegion(RegionVisualizer regionObj)
    {
        foreach(Transform child in regionObj.transform)
        {
            Flatten(child.gameObject);
        }
    }

    private void FlattenAll()
    {
        GameObject regionsRoot = GameObject.Find("Regions");
        foreach(RegionVisualizer regionObj in regionsRoot.transform.GetComponentsInChildren<RegionVisualizer>())
        {
            FlattenRegion(regionObj);
        }
    }

    private void DeleteRegionNode(GameObject regionNode)
    {
        Transform regionTrans = regionNode.transform.parent;
        if(regionTrans == null)
        {
            Debug.Log("Invalid region node! Not child of region object.");
            return;
        }

        RegionVisualizer regionObj = regionTrans.GetComponent<RegionVisualizer>();
        if (regionObj == null)
        {
            Debug.Log("Invalid region object! Missing RegionVisualizer component.");
            return;
        }

        uint indexToDelete;
        if(!uint.TryParse(regionNode.name, out indexToDelete))
        {
            Debug.Log("Invalid region node name! Do no manually rename region nodes!!");
            GameObject.DestroyImmediate(regionNode);
            return;
        }

        GameObject.DestroyImmediate(regionNode);
    }    

    private void LoadRegions(string filePath)
    {
        ResetRegions();

        GameObject regionsRoot = GameObject.Find("Regions");
        if (regionsRoot == null)
        {
            regionsRoot = new GameObject();
            regionsRoot.name = "Regions";
        }

        XmlSerializer ser = new XmlSerializer(typeof(WorldData));

        using (XmlReader reader = XmlReader.Create(filePath))
        {
            WorldData worldData = ser.Deserialize(reader) as WorldData;

            foreach(var entry in worldData.Items)
            {
                WorldDataRegionDefinition regionDef = entry as WorldDataRegionDefinition;
                if(regionDef != null)
                {
                    RegionVisualizer newRegion = CreateNewRegion(regionDef.RegionName, regionDef.Category);
                    
                    foreach(var regionNodeEntry in regionDef.RegionBounds)
                    {
                        GameObject regionNode = AddRegionNode(newRegion);
                        BoxCollider regionBox = regionNode.GetComponent<Collider>() as BoxCollider;

                        Vec3 center, size, position, rotation, scale;
                        if( !ToolUtil.ParseVecStr(regionNodeEntry.Center,out center)
                            || !ToolUtil.ParseVecStr(regionNodeEntry.Size,out size)
                            || !ToolUtil.ParseVecStr(regionNodeEntry.Position,out position)
                            || !ToolUtil.ParseVecStr(regionNodeEntry.Rotation,out rotation)
                            || !ToolUtil.ParseVecStr(regionNodeEntry.Scale,out scale))
                        {
                            Debug.Log("Failed to parse region node for region: " + regionDef.RegionName + "Node Rect: " + regionNodeEntry.Rect);
                            continue;
                        }
                        regionNode.transform.position = UnityUtil.ConvertToClientPos(position);
                        regionNode.transform.eulerAngles = UnityUtil.CreateFromVec3(rotation);
                        regionNode.transform.localScale = UnityUtil.CreateFromVec3(scale);
                        regionBox.center = UnityUtil.CreateFromVec3(center);
                        regionBox.size = UnityUtil.CreateFromVec3(size);
                    }
                }
            }
        }

        FlattenAll();
    }

    private string GetBoundsStrFromCollider(BoxCollider collider)
    {
        Vector3[] colliderVerts = UnityUtil.GetColliderWorldVertexPositions(collider.transform, new Bounds(collider.center, collider.size));
        Rect3 colliderRect = new Rect3(colliderVerts.Select(item => UnityUtil.ConvertToServerPos(item)).ToArray());

        string boundStr = "[";
        foreach (var point in colliderRect.Points)
        {
            boundStr = boundStr + point;
        }

        boundStr = boundStr + "]";

        return boundStr;
    }

    private bool SaveRegions(string filePath)
    {
        ToolUtil.TagTerrains();

        GameObject regionsRoot = GameObject.Find("Regions");
        if (regionsRoot == null)
        {
            EditorUtility.DisplayDialog("ERROR", "Seed Object Root has been deleted!", "Okay");
            return false;
        }

        RegionVisualizer[] regionObjs = regionsRoot.GetComponentsInChildren<RegionVisualizer>();
        if (regionObjs.Length == 0)
        {
            EditorUtility.DisplayDialog("ERROR", "No Regions to save!", "Okay");
            return false;
        }

        XmlSerializer ser = new XmlSerializer(typeof(WorldData));

        WorldData worldData;
        if(File.Exists(filePath))
        {
            using (XmlReader reader = XmlReader.Create(filePath))
            {
                worldData = ser.Deserialize(reader) as WorldData;
            }
        }
        else
        {
            worldData = new WorldData();
            worldData.Items = new object[0];
        }
        
        // remove all old regions
        worldData.Items = worldData.Items.Where(item => (item as WorldDataRegionDefinition) == null).ToArray();
        List<WorldDataRegionDefinition> newRegions = new List<WorldDataRegionDefinition>();

        foreach (RegionVisualizer regionObj in regionObjs)
        {
            WorldDataRegionDefinition newRegionDef = new WorldDataRegionDefinition();
            newRegionDef.RegionName = regionObj.name;

            // Get highest parent with no region script
            string category = null;
            Transform catChild = regionObj.transform;
            while (catChild.parent != regionsRoot.transform)
            {                
                catChild = catChild.parent;
            }
            if (catChild != regionsRoot && catChild.GetComponent<RegionVisualizer>() == null)
            {
                category = catChild.gameObject.name;
            }
            newRegionDef.Category = category;

            List<WorldDataRegionDefinitionRegionBounds> regionNodes = new List<WorldDataRegionDefinitionRegionBounds>();
            foreach (Transform nodeTrans in regionObj.transform)
            {
                BoxCollider regionBox = nodeTrans.GetComponent<BoxCollider>();
                if(regionBox == null)
                {
                    Debug.Log("Region " + regionObj.name + " has node with missing box collider!");
                    continue;
                }

                Vector3 scale = regionBox.transform.localScale;
                Vector3 rotation = regionBox.transform.eulerAngles;
                Transform curTrans = regionBox.transform.parent;
                while(curTrans != null)
                {
                    scale = new Vector3(scale.x * curTrans.localScale.x,scale.y * curTrans.localScale.y,scale.z * curTrans.localScale.z);
                    rotation = new Vector3(rotation.x + curTrans.eulerAngles.x, rotation.y + curTrans.eulerAngles.y, rotation.z + curTrans.eulerAngles.z);
                    curTrans = curTrans.parent;
                }

                WorldDataRegionDefinitionRegionBounds newBounds = new WorldDataRegionDefinitionRegionBounds();
                newBounds.Rect = GetBoundsStrFromCollider(regionBox);
                newBounds.Size = UnityUtil.CreateFromVector3(regionBox.size).ToString();
                newBounds.Center = UnityUtil.CreateFromVector3(regionBox.center).ToString();
                newBounds.Position = UnityUtil.ConvertToServerPos(regionBox.transform.position).ToString();
                newBounds.Rotation = UnityUtil.CreateFromVector3(regionBox.transform.eulerAngles).ToString();
                newBounds.Scale = UnityUtil.CreateFromVector3(scale).ToString();
                regionNodes.Add(newBounds);
            }
            newRegionDef.RegionBounds = regionNodes.ToArray();

            newRegions.Add(newRegionDef);
        }

        worldData.Items = worldData.Items.Concat(newRegions.Select(item => (object)item)).ToArray();
        ToolUtil.WriteXML(ser, filePath, worldData);

        EditorUtility.DisplayDialog("CONFIRMATION", "Regions saved. (Filename: " + filePath + ")", "Okay");

        return true;
    }

    private void ResetRegions()
    {
        GameObject regionsRoot = GameObject.Find("Regions");
        if (regionsRoot != null)
        {
            GameObject.DestroyImmediate(regionsRoot);
        }
        regionsRoot = new GameObject();
        regionsRoot.name = "Regions";
    }

    private string[] popupMods;
    private int modIndex = 0;
}
