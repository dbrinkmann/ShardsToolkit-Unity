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

public class SeedObjectEditor : EditorWindow
{
    public static Dictionary<string, bool> GroupExclusions = new Dictionary<string, bool>();

    public SeedObjectEditor()
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
        UpdateAvailableGroups();
    }

    public bool IsCategory(GameObject curObj)
    {
        if (curObj == null || curObj.transform.parent == null) return false;

        GameObject seedObjectRoot = GameObject.Find("SeedObjects");
        return curObj.transform.parent.gameObject == seedObjectRoot;
    }

    public bool IsSeedObject(GameObject curObj)
    {
        if (curObj == null) return false;

        return curObj.GetComponent<SeedObject>() != null;
    }

    void OnGUI()
    {
        EditorGUILayout.Space();

        if(!ToolUtil.ValidateSettings())
        {
            EditorGUILayout.LabelField("Invalid Settings. Please fix your paths by going to the 'Shards Toolkit/Settings' menu.");
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

        GameObject seedObjectRoot = GameObject.Find("SeedObjects");
        if (seedObjectRoot == null)
        {
            seedObjectRoot = new GameObject();
            seedObjectRoot.name = "SeedObjects";
        }

        // go up to the first seed object or category
        GameObject curObj = Selection.activeGameObject;
        while (curObj != null && !IsSeedObject(curObj) && !IsCategory(curObj) && curObj.transform.parent != null)
        {
            curObj = curObj.transform.parent.gameObject;
        }

        if(curObj != lastActiveObj && IsSeedObject(curObj))
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
            UpdateAvailableGroups();
        }

        groupIndex = EditorGUILayout.Popup("Group",groupIndex, availableGroups.ToArray());

        surfaceProtection = EditorGUILayout.Toggle("Surface Protection",surfaceProtection);

        EditorGUILayout.Space();

        string seedObjectFileName = ShardsExtensionMethods.CombinePaths(ModHelpers.GetRundirPath(modIndex), "mapdata", ToolUtil.currentScene, "SeedObjects.xml");
        
        GUI.enabled = File.Exists(seedObjectFileName);

        string buttonStr = (curGroup == "All") ? "Load All Seed Objects" : ("Load Group (" + curGroup + ")");
        if (GUILayout.Button(buttonStr))
        {
            if (EditorUtility.DisplayDialog("WARNING", "This will reset all seed objects. ALSO make sure that you have all permanent objects visible as this can corrupt Y values!!! Any unsaved work will be lost!", "Okay", "Cancel"))
            {
                LoadSeedObjects(seedObjectFileName, modIndex);
            }
        }

        GUI.enabled = true;

        buttonStr = (curGroup == "All") ? "Save All Seed Objects" : ("Save Group (" + curGroup + ")");
        if (GUILayout.Button(buttonStr))
        {
            if (modIndex == 0 && !ToolUtil.CanWriteToDefault)
            {
                EditorUtility.DisplayDialog("ERROR", "You can not make changes to the default ruleset.", "Okay");
            }
            else if (EditorUtility.DisplayDialog("CONFIRM", "Save current seed objects for mod " + ModHelpers.GetModName(modIndex) + "(WARNING: MAKE SURE YOU ENABLE ALL PERMANENT OBJECTS OR YOUR Y VALUES WILL BE CORRUPTED!!!) ? (Filename: " + seedObjectFileName + ")", "Okay", "Cancel"))
            {
                SaveSeedObjects(seedObjectFileName, modIndex);                
            }
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Create New Seed Object"))
        {
            var view = SceneView.currentDrawingSceneView;
            Vector3 pos = Vector3.zero;
            if(view != null)
            {
                pos = view.pivot;
            }
            SeedObject newSeed = CreateSeedObject(2, 0, pos, Vector3.one, Vector3.zero, "new_seed_object", "New Seed");
            Selection.activeGameObject = newSeed.gameObject;
        }
        if (GUILayout.Button("Create New Category"))
        {
            GameObject category = new GameObject();
            category.transform.parent = seedObjectRoot.transform;
            Selection.activeGameObject = category;
        }

        ToolUtil.DrawSeparator();
        
        if (IsSeedObject(curObj))
        {
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Seed Object Selected", EditorStyles.boldLabel);

            SeedObject seedObjComp = curObj.GetComponent<SeedObject>();

            EditorGUILayout.Space();

            GUI.enabled = false;
            EditorGUILayout.TextField(new GUIContent("Name: ","Rename seed object in heirarchy window."), seedObjComp.transform.name);
            GUI.enabled = true;

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = false;
            seedObjComp.ServerTemplateId = EditorGUILayout.TextField(new GUIContent("Creation Template:","Select a server creation template for seed object."), seedObjComp.ServerTemplateId);
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

            EditorGUILayout.LabelField("Category Selected", EditorStyles.boldLabel);

            

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
            EditorGUILayout.LabelField("Create New or Select a Seed Object", centertext);            
        }

        if (GUILayout.Button("Reset Seed Objects"))
        {
            if (EditorUtility.DisplayDialog("WARNING", "This will reset all seed objects in the scene. Any unsaved work will be lost!", "Okay", "Cancel"))
            {
                ResetSeedObjects();
            }
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
                SeedObject newSeedComp = CreateSeedObject(clientId, 0, oldSeedComp.transform.position, oldSeedComp.transform.localScale, oldSeedComp.transform.localEulerAngles,templateName,oldSeedComp.name);
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

    private SeedObject CreateSeedObject(UInt32 clientId, UInt32 seedId, Vector3 position, Vector3 scale, Vector3 rotation, string templateId, string seedName, string seedCategory=null)
    {
        GameObject seedObjectRoot = GameObject.Find("SeedObjects");
        if (seedCategory != null)
        {
            Transform catTrans = seedObjectRoot.transform.Find(seedCategory);
            if (catTrans != null)
            {
                seedObjectRoot = catTrans.gameObject;
            }
            else
            {
                GameObject newObjectRoot = new GameObject();
                newObjectRoot.transform.parent = seedObjectRoot.transform;
                newObjectRoot.name = seedCategory;
                seedObjectRoot = newObjectRoot;
            }
        }

        GameObject clientObjPrefab = ResolveSeedPrefab(ref clientId, templateId);

        GameObject clientObjInstance;
        if (clientObjPrefab != null)
        {
            clientObjInstance = Instantiate(clientObjPrefab, position, Quaternion.Euler(rotation.x, rotation.y, rotation.z)) as GameObject;
        }
        else
        {
            // No client prefab in any available library. Seed objects are markers for server
            // templates, so fall back to a plain marker rather than skipping the seed entirely.
            clientObjInstance = ToolUtil.CreateClientIdPlaceholder(templateId);
            clientObjInstance.transform.position = position;
            clientObjInstance.transform.rotation = Quaternion.Euler(rotation.x, rotation.y, rotation.z);
        }

        if (seedName != null)
        {
            clientObjInstance.name = seedName;
        }

        clientObjInstance.transform.parent = seedObjectRoot.transform;
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

    // Resolves the preview prefab for a seed object, falling back to the 'empty' template before
    // giving up and letting the caller place a plain marker.
    private GameObject ResolveSeedPrefab(ref UInt32 clientId, string templateId)
    {
        GameObject prefab = ToolUtil.ResolvePrefabForClientId(clientId);
        if (prefab != null)
        {
            return prefab;
        }

        UInt32 emptyClientId = GetClientIdForTemplate("empty");
        prefab = ToolUtil.ResolvePrefabForClientId(emptyClientId);
        if (prefab != null)
        {
            clientId = emptyClientId;
            return prefab;
        }

        Debug.LogWarning("No client prefab for clientId " + clientId + " (template '" + templateId + "'). Using a placeholder marker.");
        return null;
    }

    private void ResetSeedObjects()
    {
        GameObject seedObjectRoot = GameObject.Find("SeedObjects");
        if (seedObjectRoot != null)
        {
            GameObject.DestroyImmediate(seedObjectRoot);
        }
        seedObjectRoot = new GameObject();
        seedObjectRoot.name = "SeedObjects";
    }    

    private void LoadSeedObjects(string seedObjectFileName, int modIndex)
    {
        if (TagsHelpers.HasRequiredTagsAndLayers() == false)
            TagsHelpers.AddTagsAndLayers();

        if(curGroup == "All")
        {
            ResetSeedObjects();
        }

        wasSurfaceError = false;

        GameObject seedObjectRoot = GameObject.Find("SeedObjects");
        if (seedObjectRoot == null)        
        {
            seedObjectRoot = new GameObject();
            seedObjectRoot.name = "SeedObjects";
        }

        XmlSerializer ser = new XmlSerializer(typeof(WorldTemplateObjects));

        using (XmlReader reader = XmlReader.Create(seedObjectFileName))
        {
            WorldTemplateObjects objCreates = ser.Deserialize(reader) as WorldTemplateObjects;

            foreach (var groupXML in objCreates.Items)
            {
                WorldTemplateObjectsGroup groupRef = groupXML as WorldTemplateObjectsGroup;
                if(curGroup == "All" || curGroup == groupRef.Name)
                {
                    if (groupRef.Exclude != null && Boolean.Parse(groupRef.Exclude) == true)
                    {
                        GroupExclusions[groupRef.Name] = true;
                    }

                    if (groupRef.DynamicObject != null)
                    {
                        foreach (WorldTemplateObjectsGroupDynamicObject dynamicRef in groupRef.DynamicObject)
                        {
                            string[] createArgs = dynamicRef.ObjectCreationParams.Split(new string[] { " " }, StringSplitOptions.None);

                            if(createArgs.Length < 10)
                            {
                                Debug.LogError("SeedObject entry in XML is corrupt: " + dynamicRef.ObjectCreationParams);
                                return;
                            }
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

                            // check for surface error
                            if(surfaceProtection)
                            {
                                RaycastHit hit;
                                wasSurfaceError = UnityUtil.GetSurfaceHit(position, out hit, UnityUtil.SurfaceType.StaticAndDynamic) == UnityUtil.SurfaceHitType.None;
                                surfaceErrorObj = "Group: " + groupRef.Name + ", Obj: " + dynamicRef.Name;
                            }

                            uint seedId = uint.MaxValue;
                            if (dynamicRef.Id != null)
                            {
                                seedId = UInt32.Parse(dynamicRef.Id);
                            }

                            // retrieve the client id from the template
                            UInt32 clientId = GetClientIdForTemplate(templateId);
                            if(clientId == 0)
                            {
                                clientId = GetClientIdForTemplate("empty");
                            }

                            if (clientId != 0)
                            {
                                SeedObject seedComp = CreateSeedObject(clientId, seedId, position, scale, rotation, templateId, dynamicRef.Name, groupRef.Name);
                                if(seedComp == null)
                                {
                                    Debug.LogError("Seed object failed to load! ClientId: " + clientId + ", TemplateId: " + templateId);
                                }
                                else
                                {
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
        }

        if(surfaceProtection && wasSurfaceError)
        {
            EditorUtility.DisplayDialog("Surface Protection Error","One or more of the seed objects loaded did not have a valid surface under them.\n\n" + surfaceErrorObj, "Ok");
        }
    }

    private bool SaveChildren(List<WorldTemplateObjectsGroup> dynGroups, Transform parent, Transform seedObjectRoot)
    {
        foreach (Transform child in parent)
        {            
            SeedObject seedObjComp = child.GetComponent<SeedObject>();
            if (seedObjComp == null)
            {
                // save empty categories
                if (parent == seedObjectRoot && child.childCount == 0)
                {
                    string category = child.name;
                    WorldTemplateObjectsGroup dynGroup = dynGroups.FirstOrDefault(item => item.Name == category);
                    if (dynGroup == null)
                    {
                        dynGroup = new WorldTemplateObjectsGroup() { Name = category, Exclude = "False" };
                        dynGroups.Add(dynGroup);
                    }
                }
                else if(!SaveChildren(dynGroups, child, seedObjectRoot))
                {
                    return false;
                }
            }
            else
            {
                // Get highest parent with no seed object script
                string category = "Main";
                Transform catChild = child;
                while (catChild.parent != seedObjectRoot)
                {
                    catChild = catChild.parent;
                }
                if (catChild != seedObjectRoot && catChild.GetComponent<SeedObject>() == null)
                {
                    category = catChild.gameObject.name;
                } 

                WorldTemplateObjectsGroup dynGroup = dynGroups.FirstOrDefault(item => item.Name == category);
                if (dynGroup == null)
                {
                    dynGroup = new WorldTemplateObjectsGroup() { Name = category, Exclude = (GroupExclusions.ContainsKey(category) && GroupExclusions[category]).ToString() };
                    dynGroups.Add(dynGroup);
                }

                int nextId = 1;
                if(dynGroup.DynamicObject != null)
                {
                    nextId = dynGroup.DynamicObject.Length + 1;
                }

                string templateId = seedObjComp.ServerTemplateId;

                // only save the name if its different from the template
                string seedName = null;
                if (seedObjComp.name != seedObjComp.ServerTemplateId)
                {
                    seedName = seedObjComp.name;
                }

                // check for surface error
                if (surfaceProtection)
                {
                    RaycastHit hit;
                    wasSurfaceError = UnityUtil.GetSurfaceHit(child.position, out hit, UnityUtil.SurfaceType.StaticAndDynamic) == UnityUtil.SurfaceHitType.None;
                    surfaceErrorObj = "Group: " + category + ", Obj: " + child.name;
                }

                string createStr = ToolUtil.GenerateDynamicCreateString(templateId, child, Vector3.zero);
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

    private bool SaveSeedObjects(string seedObjectFileName, int modIndex)
    {
        XmlSerializer ser;
        WorldTemplateObjects objCreates;

        GameObject seedObjectRoot = GameObject.Find("SeedObjects");
        if (seedObjectRoot == null)
        {
            EditorUtility.DisplayDialog("ERROR", "Seed Object Root has been deleted!", "Okay");
            return false;
        }        

        if(seedObjectRoot.transform.childCount == 0)
        {
            EditorUtility.DisplayDialog("ERROR", "No Seed Objects to save!", "Okay");
            return false;
        }

        List<WorldTemplateObjectsGroup> dynGroups = new List<WorldTemplateObjectsGroup>();

        if(curGroup != "All")
        {
            ser = new XmlSerializer(typeof(WorldTemplateObjects));

            using (XmlReader reader = XmlReader.Create(seedObjectFileName))
            {
                objCreates = ser.Deserialize(reader) as WorldTemplateObjects;

                foreach (var groupXML in objCreates.Items)
                {
                    WorldTemplateObjectsGroup groupRef = groupXML as WorldTemplateObjectsGroup;
                    if (groupRef.Name != curGroup)
                    {
                        dynGroups.Add(groupRef);
                    }
                }
            }
        }

        wasSurfaceError = false;

        SaveChildren(dynGroups, seedObjectRoot.transform, seedObjectRoot.transform);

        if (surfaceProtection && wasSurfaceError)
        {
            EditorUtility.DisplayDialog("Surface Protection Error", "One or more of the seed objects to save does not have a valid surface under them. Turn off Surface Protection if you wish to save anyway.\n\n" + surfaceErrorObj, "Ok");            
        }

        if(!wasSurfaceError)
        {
            objCreates = new WorldTemplateObjects() { Items = dynGroups.ToArray() };        
            ser = new XmlSerializer(typeof(WorldTemplateObjects));
            ToolUtil.WriteXML(ser, seedObjectFileName, objCreates);

            EditorUtility.DisplayDialog("CONFIRMATION", "Seed objects saved. (Filename: " + seedObjectFileName + ")", "Okay");        
        }

        return true;
    }

    private void UpdateAvailableGroups()
    {
        List<string> newGroups = new List<string>() { "All" };
        string seedObjectFileName = ShardsExtensionMethods.CombinePaths(ModHelpers.GetRundirPath(modIndex), "mapdata", ToolUtil.currentScene, "SeedObjects.xml");
        if (File.Exists(seedObjectFileName))
        {
            XmlSerializer ser = new XmlSerializer(typeof(WorldTemplateObjects));

            using (XmlReader reader = XmlReader.Create(seedObjectFileName))
            {
                WorldTemplateObjects objCreates = ser.Deserialize(reader) as WorldTemplateObjects;

                foreach (var groupXML in objCreates.Items)
                {
                    WorldTemplateObjectsGroup groupRef = groupXML as WorldTemplateObjectsGroup;
                    if(groupRef != null)
                    {
                        newGroups.Add(groupRef.Name);
                    }
                }
            }
        }

        availableGroups = newGroups.ToArray();
    }    
            
    private bool surfaceProtection = true;

    private bool wasSurfaceError = false;
    private string surfaceErrorObj = "";

    private string[] popupMods;
    private int modIndex = 0;

    private static string[] availableGroups = new string[] { "All" };
    private int groupIndex = 0;

    private string curGroup { get { return availableGroups[groupIndex]; } }

    private TemplateSelectorPopup createPopup;

    public static Dictionary<string, List<string>> templateList;

    private GameObject lastActiveObj;
    private UnityEditorInternal.ReorderableList varList;
}