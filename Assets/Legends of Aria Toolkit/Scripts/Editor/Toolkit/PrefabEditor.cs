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
using ShardsXML.SeedObject;
using ShardsXML.ObjectTemplate;
using CoreUtil.ShardEngineMath;

public class PrefabEditor : EditorWindow
{
    public PrefabEditor()
    {
        modIndex = 0;
        InitializeWindow();
    }

    public void OnFocus()
    {
        InitializeWindow();
    }

    public void InitializeWindow()
    {
        popupMods = (new string[] { "Default" }).Concat(ModHelpers.RefreshList()).ToArray();
        templateList = ModHelpers.GetAllTemplates(modIndex, true);
    }

    public bool IsCategory(GameObject curObj)
    {
        if (curObj == null || curObj.transform.parent == null) return false;

        GameObject prefabObjectRoot = GameObject.Find("Prefabs");
        return curObj.transform.parent.gameObject == prefabObjectRoot;
    }

    public bool IsPrefabObject(GameObject curObj)
    {
        if (curObj == null) return false;

        SeedObject seedComp = curObj.GetComponent<SeedObject>();
        return seedComp != null && seedComp.IsPrefab;
    }

    void OnGUI()
    {
        EditorGUILayout.Space();

        if(!ToolUtil.ValidateSettings())
        {
            EditorGUILayout.LabelField("Invalid Settings. Please fix your paths by going to the 'LoA Toolkit/Settings' menu.");
            return;
        }
        else if(!Application.isPlaying && !ToolUtil.EditWhileStopped)
        {
            EditorGUILayout.LabelField("To begin, press play and select a map.");
            return;
        }
        else if (!MapData.Instance)
        {
            EditorGUILayout.LabelField("To begin, select a map.");
            return;
        }

        GameObject prefabObjectRoot = GameObject.Find("Prefabs");
        if (prefabObjectRoot == null)
        {
            prefabObjectRoot = new GameObject();
            prefabObjectRoot.name = "Prefabs";
        }

        // go up to the first prefab or category
        GameObject curObj = Selection.activeGameObject;
        while (curObj != null && !IsPrefabObject(curObj) && !IsCategory(curObj) && curObj.transform.parent != null)
        {
            curObj = curObj.transform.parent.gameObject;
        }

        if (curObj != lastActiveObj && IsPrefabObject(curObj))
        {
            SeedObject seedObjComp = curObj.GetComponent<SeedObject>();
            varList = ToolUtil.GetObjVarEditor(seedObjComp.ObjVars,true);
            lastActiveObj = curObj;
        }        

        EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        int newModIndex = EditorGUILayout.Popup("Mod", modIndex, popupMods);
        if(newModIndex != modIndex)
        {            
            modIndex = newModIndex;
            templateList = ModHelpers.GetAllTemplates(modIndex,true);
        }

        EditorGUILayout.Space();

        string prefabFileName = ShardsExtensionMethods.CombinePaths(ModHelpers.GetRundirPath(modIndex), "Prefabs.xml");
        
        GUI.enabled = File.Exists(prefabFileName);

        if (GUILayout.Button("Load Prefabs"))
        {
            if (EditorUtility.DisplayDialog("WARNING", "This will reset all prefabs in the scene. Any unsaved work will be lost!", "Okay", "Cancel"))
            {
                LoadPrefabObjects(prefabFileName, modIndex);
            }
        }

        GUI.enabled = true;

        if (GUILayout.Button("Save Prefabs"))
        {
            if (modIndex == 0 && !ToolUtil.CanWriteToDefault)
            {
                EditorUtility.DisplayDialog("ERROR", "You can not make changes to the default ruleset.", "Okay");
            }
            else if (EditorUtility.DisplayDialog("CONFIRM", "Save current prefabs for mod " + ModHelpers.GetModName(modIndex) + "? (Filename: " + prefabFileName + ")", "Okay", "Cancel"))
            {
                SavePrefabObjects(prefabFileName, modIndex);                
            }
        }
        if (GUILayout.Button("Reset Prefabs"))
        {
            if (EditorUtility.DisplayDialog("WARNING", "This will reset all prefabs in the scene. Any unsaved work will be lost!", "Okay", "Cancel"))
            {
                ResetPrefabObjects();
            }
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Create Prefab Object"))
        {
            var view = SceneView.currentDrawingSceneView;
            Vector3 pos = Vector3.zero;
            if(view != null)
            {
                pos = view.pivot;
            }
            SeedObject newSeed = CreatePrefabObject(2, 0, pos, Vector3.one, Vector3.zero, "new_prefab", "New Prefab");
            Selection.activeGameObject = newSeed.gameObject;
        }
        if (GUILayout.Button("Create New Prefab"))
        {
            GameObject category = new GameObject();
            category.transform.parent = prefabObjectRoot.transform;
            Selection.activeGameObject = category;
        }

        ToolUtil.DrawSeparator();
        
        if (IsPrefabObject(curObj))
        {
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Prefab Object Selected", EditorStyles.boldLabel);

            SeedObject seedObjComp = curObj.GetComponent<SeedObject>();

            EditorGUILayout.Space();

            GUI.enabled = false;
            EditorGUILayout.TextField(new GUIContent("Name: ","Rename prefab object in heirarchy window."), seedObjComp.transform.name);
            GUI.enabled = true;

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = false;
            seedObjComp.ServerTemplateId = EditorGUILayout.TextField(new GUIContent("Creation Template:","Select a server creation template for prefab object."), seedObjComp.ServerTemplateId);
            GUI.enabled = true;
            if (GUILayout.Button("Select"))
            {
                createPopup = ModHelpers.OpenTemplateSelectionPopup(modIndex, OnCreateTemplateSelected);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            if (varList != null)
            {
                varList.DoLayoutList();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Move To View"))
            {
                var view = SceneView.currentDrawingSceneView;
                if (view != null)
                {
                    curObj.transform.position = view.pivot;
                }
            }
            if (GUILayout.Button("Snap to Terrain/Surface"))
            {
                ToolUtil.TagTerrains();
                curObj.transform.position = new Vector3(curObj.transform.position.x, UnityUtil.GetStaticSurfaceY(curObj.transform.position), curObj.transform.position.z);
            }
            if (GUILayout.Button("Duplicate"))
            {
                GameObject newSeed = ToolUtil.DuplicateObject(curObj);
                newSeed.name = curObj.name + "1";
                newSeed.transform.parent = curObj.transform.parent;
                Selection.activeGameObject = newSeed;
            }
            if (GUILayout.Button("Destroy"))
            {
                GameObject.DestroyImmediate(curObj);
            }
        } 
        if (GUILayout.Button("Snap all selected to Terrain/Surface"))
        {
            ToolUtil.TagTerrains();
            var objects = Selection.gameObjects;
            foreach (GameObject _object in objects)
            {
                if (_object.GetComponent<SeedObject>() != null)
                {
                    _object.transform.position = new Vector3(_object.transform.position.x, UnityUtil.GetStaticSurfaceY(_object.transform.position), _object.transform.position.z);
                }
            }
        }
        else if(IsCategory(curObj))
        {
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Prefab Selected", EditorStyles.boldLabel);            

            EditorGUILayout.Space();

            GUI.enabled = false;
            EditorGUILayout.TextField("Name: ", curObj.name);
            GUI.enabled = true;
        }
        else
        {
            EditorGUILayout.Space();
            GUIStyle centertext = new GUIStyle(GUI.skin.label);
            centertext.alignment = TextAnchor.MiddleCenter;
            EditorGUILayout.LabelField("Create New or Select a Prefab or Prefab Object", centertext);            
        }
        
        EditorGUILayout.Space();
        ToolUtil.DrawSeparator();
        EditorGUILayout.Space();               
    }

    public void Update()
    {
        // This is necessary to make the framerate normal for the editor window.
        Repaint();
    }

    private void OnCreateTemplateSelected(string category, string templateName)
    {
        Debug.Log("Selected " + templateName);

        GameObject activeObj = Selection.activeGameObject;
        if (activeObj != null)
        {
            SeedObject oldSeedComp = activeObj.GetComponent<SeedObject>();

            if (templateName != null && templateName != "" && oldSeedComp.ServerTemplateId != templateName)
            {
                UInt32 clientId = GetClientIdForTemplate(templateName);
                SeedObject newSeedComp = CreatePrefabObject(clientId, 0, oldSeedComp.transform.position, oldSeedComp.transform.localScale, oldSeedComp.transform.localEulerAngles,templateName,oldSeedComp.name);
                newSeedComp.ObjVars = oldSeedComp.ObjVars;
                Selection.activeGameObject = newSeedComp.gameObject;
                GameObject.DestroyImmediate(oldSeedComp.gameObject);
            }
        }

        createPopup = null;
    }

    private UInt32 GetClientIdForTemplate(string templateId)
    {
        if( templateId == null || templateId == "")
        {
            return 0;
        }

        // retrieve the client id from the template
        UInt32 clientId = 0;

        string curTemplatePath = ModHelpers.GetTemplatePath(templateId, modIndex, templateList);
        if(curTemplatePath == null)
        {
            Debug.Log("Category/Template not found for " + templateId);
            return 0;
        }

        XmlSerializer templateSer = new XmlSerializer(typeof(ObjectTemplate));
        using (XmlReader templateReader = XmlReader.Create(curTemplatePath))
        {
            ObjectTemplate tempXML = templateSer.Deserialize(templateReader) as ObjectTemplate;
            clientId = UInt32.Parse(tempXML.ClientId);
        }

        return clientId;
    }

    private SeedObject CreatePrefabObject(UInt32 clientId, UInt32 seedId, Vector3 position, Vector3 scale, Vector3 rotation, string templateId, string seedName, string seedCategory=null)
    {
        GameObject prefabObjectRoot = GameObject.Find("Prefabs");
        if (seedCategory != null)
        {
            Transform catTrans = prefabObjectRoot.transform.Find(seedCategory);
            if (catTrans != null)
            {
                prefabObjectRoot = catTrans.gameObject;
            }
            else
            {
                GameObject newObjectRoot = new GameObject();
                newObjectRoot.transform.parent = prefabObjectRoot.transform;
                newObjectRoot.name = seedCategory;
                prefabObjectRoot = newObjectRoot;
            }
        }

        GameObject clientObjPrefab = ToolUtil.ResolvePrefabForClientId(clientId);
        GameObject clientObjInstance;
        if (clientObjPrefab != null)
        {
            clientObjInstance = Instantiate(clientObjPrefab, position, Quaternion.Euler(rotation.x, rotation.y, rotation.z)) as GameObject;
        }
        else
        {
            // No client prefab in any available library; place a marker so authoring still works.
            Debug.LogWarning("No client prefab for clientId " + clientId + " (template '" + templateId + "'). Using a placeholder marker.");
            clientObjInstance = ToolUtil.CreateClientIdPlaceholder(templateId);
            clientObjInstance.transform.position = position;
            clientObjInstance.transform.rotation = Quaternion.Euler(rotation.x, rotation.y, rotation.z);
        }

        if (seedName != null)
        {
            clientObjInstance.name = seedName;
        }

        clientObjInstance.transform.parent = prefabObjectRoot.transform;
        clientObjInstance.transform.localScale = scale;
        SeedObject seedComp = clientObjInstance.AddComponent<SeedObject>();
        seedComp.ServerTemplateId = templateId;
        seedComp.Id = seedId;

        Component clientComp = clientObjInstance.GetComponent("ClientObject");
        if(clientComp != null)
        {
            DestroyImmediate(clientComp);
        }
        Component contComp = clientObjInstance.GetComponent("ContainerObject");
        if (contComp != null)
        {
            DestroyImmediate(contComp);
        }
        Component nodrawComp = clientObjInstance.GetComponent("NoDraw");
        if (nodrawComp != null)
        {
            DestroyImmediate(nodrawComp);
        }
        AudioSource audioComp = clientObjInstance.GetComponent<AudioSource>();
        if (audioComp != null)
        {
            DestroyImmediate(audioComp);
        }

        return seedComp;
    }

    private void ResetPrefabObjects()
    {
        GameObject prefabObjectRoot = GameObject.Find("Prefabs");
        if (prefabObjectRoot != null)
        {
            GameObject.DestroyImmediate(prefabObjectRoot);
        }
        prefabObjectRoot = new GameObject();
        prefabObjectRoot.name = "Prefabs";
    }    

    private void LoadPrefabObjects(string prefabFileName, int modIndex)
    {
        ResetPrefabObjects();

        GameObject prefabObjectRoot = GameObject.Find("Prefabs");
        if (prefabObjectRoot == null)        
        {
            prefabObjectRoot = new GameObject();
            prefabObjectRoot.name = "Prefabs";
        }

        XmlSerializer ser = new XmlSerializer(typeof(WorldTemplateObjects));

        using (XmlReader reader = XmlReader.Create(prefabFileName))
        {
            WorldTemplateObjects objCreates = ser.Deserialize(reader) as WorldTemplateObjects;

            foreach (var groupXML in objCreates.Items)
            {
                WorldTemplateObjectsGroup groupRef = groupXML as WorldTemplateObjectsGroup;                

                if (groupRef.DynamicObject != null)
                {
                    foreach (WorldTemplateObjectsGroupDynamicObject dynamicRef in groupRef.DynamicObject)
                    {
                        string[] createArgs = dynamicRef.ObjectCreationParams.Split(new string[] { " " }, StringSplitOptions.None);

                        string templateId = createArgs[0];

                        Vector3 position = UnityUtil.ConvertToClientPos(new Vec3(float.Parse(createArgs[1]),
                                                float.Parse(createArgs[2]),
                                                float.Parse(createArgs[3])));

                        Vector3 rotation = new Vector3(float.Parse(createArgs[4]),
                                                float.Parse(createArgs[5]),
                                                float.Parse(createArgs[6]));
                        Vector3 scale = new Vector3(float.Parse(createArgs[7]),
                                                float.Parse(createArgs[8]),
                                                float.Parse(createArgs[9]));

                        uint seedId = uint.MaxValue;
                        if (dynamicRef.Id != null)
                        {
                            seedId = UInt32.Parse(dynamicRef.Id);
                        }

                        // retrieve the client id from the template
                        UInt32 clientId = GetClientIdForTemplate(templateId);
                        if (clientId != 0)
                        {
                            SeedObject seedComp = CreatePrefabObject(clientId, seedId, position, scale, rotation, templateId, dynamicRef.Name, groupRef.Name);
                            if (dynamicRef.ObjVarOverrides != null && dynamicRef.ObjVarOverrides.Length > 0)
                            {
                                WorldTemplateObjectsGroupDynamicObjectObjVarOverrides overrideXML = dynamicRef.ObjVarOverrides[0];
                                if (overrideXML.BoolVariable != null)
                                {
                                    foreach (WorldTemplateObjectsGroupDynamicObjectObjVarOverridesBoolVariable varXML in overrideXML.BoolVariable)
                                    {
                                        seedComp.ObjVars.Add(new ShardsObjVar() { Type = ShardsObjVar.ObjVarType.Boolean, Name = varXML.Name, BoolValue = bool.Parse(varXML.Value) });
                                    }
                                }
                                if (overrideXML.LocVariable != null)
                                {
                                    foreach (WorldTemplateObjectsGroupDynamicObjectObjVarOverridesLocVariable varXML in overrideXML.LocVariable)
                                    {
                                        string[] locComps = varXML.Value.Split(new char[] { ',' });
                                        Vector3 locValue = new Vector3(float.Parse(locComps[0]),float.Parse(locComps[1]),float.Parse(locComps[2]));
                                        seedComp.ObjVars.Add(new ShardsObjVar() { Type = ShardsObjVar.ObjVarType.Loc, Name = varXML.Name, LocValue =locValue  });
                                    }
                                }
                                if (overrideXML.DoubleVariable != null)
                                {
                                    foreach (WorldTemplateObjectsGroupDynamicObjectObjVarOverridesDoubleVariable varXML in overrideXML.DoubleVariable)
                                    {
                                        seedComp.ObjVars.Add(new ShardsObjVar() { Type = ShardsObjVar.ObjVarType.Number, Name = varXML.Name, DoubleValue = float.Parse(varXML.Value) });
                                    }
                                }
                                if (overrideXML.StringVariable != null)
                                {
                                    foreach (WorldTemplateObjectsGroupDynamicObjectObjVarOverridesStringVariable varXML in overrideXML.StringVariable)
                                    {
                                        seedComp.ObjVars.Add(new ShardsObjVar() { Type = ShardsObjVar.ObjVarType.String, Name = varXML.Name, StrValue = varXML.Value });
                                    }
                                }
                            }
                            seedComp.Id = seedId;
                        }
                    }
                }
            }
        }
    }

    private bool SaveChildren(List<WorldTemplateObjectsGroup> dynGroups, Transform parent, Transform prefabObjectRoot)
    {
        foreach (Transform child in parent)
        {            
            SeedObject seedObjComp = child.GetComponent<SeedObject>();
            if (seedObjComp == null)
            {
                // save empty categories
                if (parent == prefabObjectRoot && child.childCount == 0)
                {
                    string category = child.name;
                    WorldTemplateObjectsGroup dynGroup = dynGroups.FirstOrDefault(item => item.Name == category);
                    if (dynGroup == null)
                    {
                        dynGroup = new WorldTemplateObjectsGroup() { Name = category, Exclude = "False" };
                        dynGroups.Add(dynGroup);
                    }
                }
                else if(!SaveChildren(dynGroups, child, prefabObjectRoot))
                {
                    return false;
                }
            }
            else
            {
                // Get highest parent with no seed object script
                string category = "Main";
                Transform catChild = child;
                while (catChild.parent != prefabObjectRoot)
                {
                    catChild = catChild.parent;
                }
                if (catChild != prefabObjectRoot && catChild.GetComponent<SeedObject>() == null)
                {
                    category = catChild.gameObject.name;
                } 

                WorldTemplateObjectsGroup dynGroup = dynGroups.FirstOrDefault(item => item.Name == category);
                if (dynGroup == null)
                {
                    dynGroup = new WorldTemplateObjectsGroup() { Name = category };
                    dynGroups.Add(dynGroup);
                }

                int nextId = 1;
                if(dynGroup.DynamicObject != null)
                {
                    nextId = dynGroup.DynamicObject.Length + 1;
                }

                string templateId = seedObjComp.ServerTemplateId;
                UInt32 clientId = GetClientIdForTemplate(templateId);
                if (templateId == null || templateId == "" || clientId == 0)
                {
                    EditorUtility.DisplayDialog("ERROR", "Seed Object has invalid template: " + child.gameObject.name + ". Fix or destroy object and try again.", "Okay");
                    return false;
                }  

                // only save the name if its different from the template
                string seedName = null;
                if (seedObjComp.name != seedObjComp.ServerTemplateId)
                {
                    seedName = seedObjComp.name;
                }

                string createStr = ToolUtil.GenerateDynamicCreateString(templateId, child, catChild.transform.position);
                WorldTemplateObjectsGroupDynamicObject dynEntry = new WorldTemplateObjectsGroupDynamicObject() { Id = nextId.ToString(), Name = seedName, ObjectCreationParams = createStr };
                if (seedObjComp.ObjVars != null && seedObjComp.ObjVars.Count > 0)
                {
                    WorldTemplateObjectsGroupDynamicObjectObjVarOverrides overridesEntry = new WorldTemplateObjectsGroupDynamicObjectObjVarOverrides();
                    foreach (ShardsObjVar objVar in seedObjComp.ObjVars)
                    {
                        switch (objVar.Type)
                        {
                            case ShardsObjVar.ObjVarType.Boolean:
                                WorldTemplateObjectsGroupDynamicObjectObjVarOverridesBoolVariable boolVar = new WorldTemplateObjectsGroupDynamicObjectObjVarOverridesBoolVariable() { Name = objVar.Name, Value = objVar.BoolValue.ToString() };
                                if (overridesEntry.BoolVariable == null)
                                {
                                    overridesEntry.BoolVariable = new WorldTemplateObjectsGroupDynamicObjectObjVarOverridesBoolVariable[] { boolVar };
                                }
                                else
                                {
                                    overridesEntry.BoolVariable = overridesEntry.BoolVariable.Concat(new WorldTemplateObjectsGroupDynamicObjectObjVarOverridesBoolVariable[] { boolVar }).ToArray();
                                }
                                break;
                            case ShardsObjVar.ObjVarType.Loc:
                                string locStr = objVar.LocValue.x + "," + objVar.LocValue.y + "," + objVar.LocValue.z;
                                WorldTemplateObjectsGroupDynamicObjectObjVarOverridesLocVariable locVar = new WorldTemplateObjectsGroupDynamicObjectObjVarOverridesLocVariable() { Name = objVar.Name, Value = locStr };
                                if (overridesEntry.LocVariable == null)
                                {
                                    overridesEntry.LocVariable = new WorldTemplateObjectsGroupDynamicObjectObjVarOverridesLocVariable[] { locVar };
                                }
                                else
                                {
                                    overridesEntry.LocVariable = overridesEntry.LocVariable.Concat(new WorldTemplateObjectsGroupDynamicObjectObjVarOverridesLocVariable[] { locVar }).ToArray();
                                }
                                break;
                            case ShardsObjVar.ObjVarType.Number:
                                WorldTemplateObjectsGroupDynamicObjectObjVarOverridesDoubleVariable numVar = new WorldTemplateObjectsGroupDynamicObjectObjVarOverridesDoubleVariable() { Name = objVar.Name, Value = objVar.DoubleValue.ToString() };
                                if (overridesEntry.DoubleVariable == null)
                                {
                                    overridesEntry.DoubleVariable = new WorldTemplateObjectsGroupDynamicObjectObjVarOverridesDoubleVariable[] { numVar };
                                }
                                else
                                {
                                    overridesEntry.DoubleVariable = overridesEntry.DoubleVariable.Concat(new WorldTemplateObjectsGroupDynamicObjectObjVarOverridesDoubleVariable[] { numVar }).ToArray();
                                }
                                break;
                            case ShardsObjVar.ObjVarType.String:
                                WorldTemplateObjectsGroupDynamicObjectObjVarOverridesStringVariable strVar = new WorldTemplateObjectsGroupDynamicObjectObjVarOverridesStringVariable() { Name = objVar.Name, Value = objVar.StrValue };
                                if (overridesEntry.StringVariable == null)
                                {
                                    overridesEntry.StringVariable = new WorldTemplateObjectsGroupDynamicObjectObjVarOverridesStringVariable[] { strVar };
                                }
                                else
                                {
                                    overridesEntry.StringVariable = overridesEntry.StringVariable.Concat(new WorldTemplateObjectsGroupDynamicObjectObjVarOverridesStringVariable[] { strVar }).ToArray();
                                }
                                break;
                        }
                    }
                    dynEntry.ObjVarOverrides = new WorldTemplateObjectsGroupDynamicObjectObjVarOverrides[] { overridesEntry };
                }

                if (dynGroup.DynamicObject == null)
                {
                    dynGroup.DynamicObject = new WorldTemplateObjectsGroupDynamicObject[] { dynEntry };
                }
                else
                {
                    dynGroup.DynamicObject = dynGroup.DynamicObject.Concat(new WorldTemplateObjectsGroupDynamicObject[] { dynEntry }).ToArray();
                }
            }
        }

        return true;
    }

    private bool SavePrefabObjects(string prefabFileName, int modIndex)
    {
        GameObject prefabObjectRoot = GameObject.Find("Prefabs");
        if (prefabObjectRoot == null)
        {
            EditorUtility.DisplayDialog("ERROR", "Seed Object Root has been deleted!", "Okay");
            return false;
        }        

        if(prefabObjectRoot.transform.childCount == 0)
        {
            EditorUtility.DisplayDialog("ERROR", "No Seed Objects to save!", "Okay");
            return false;
        }

        List<WorldTemplateObjectsGroup> dynGroups = new List<WorldTemplateObjectsGroup>();

        SaveChildren(dynGroups, prefabObjectRoot.transform, prefabObjectRoot.transform);

        WorldTemplateObjects objCreates = new WorldTemplateObjects() { Items = dynGroups.ToArray() };        
        XmlSerializer ser = new XmlSerializer(typeof(WorldTemplateObjects));
        ToolUtil.WriteXML(ser, prefabFileName, objCreates);

        EditorUtility.DisplayDialog("CONFIRMATION", "Prefabs saved. (Filename: " + prefabFileName + ")", "Okay");

        return true;
    }
            
    private string[] popupMods;
    private int modIndex = 0;
    
    private TemplateSelectorPopup createPopup;

    public static Dictionary<string, List<string>> templateList;

    private GameObject lastActiveObj;
    private UnityEditorInternal.ReorderableList varList;
}