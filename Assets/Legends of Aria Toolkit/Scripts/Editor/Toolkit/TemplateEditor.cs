// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// Originally part of the Shards Engine Toolkit; released under the MIT License.
// See LICENSE in the repository root.
using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using CoreUtil;
using ShardsXML.ObjectTemplate;
using ShardsXML.TagDefinitions;

public class TemplateEditor : EditorWindow
{    
    public int ModIndex { get { return saveModIndex; } }

    public void Initialize()
    {
        RefreshModList();
        RefreshScriptList();
        RefreshSaveCategories();
        LoadClientIds();
        RefreshObjectTags();
    }

    public void OnFocus()
    {
        Initialize();
    }    
    
    void RefreshSaveCategories()
    {
        List<string> categoryList = new List<string>();

        string templatesRoot = Path.Combine(ModHelpers.GetRundirPath(saveModIndex), "templates");
        DirectoryInfo di = new DirectoryInfo(templatesRoot);
        if (!di.Exists)
        {
            Debug.LogError("Failed to load template list.  Template path does not exist: " + templatesRoot);
            return;
        }

        DirectoryInfo[] templateDirs = di.GetDirectories();
        foreach (var templateDir in templateDirs)
        {
            string category = templateDir.Name;
            categoryList.Add(category);
        }

        popupSaveCategories = categoryList.ToArray();

        if(saveCategoryIndex >= popupSaveCategories.Length + 1)
        {
            saveCategoryIndex = 0;
        }
    }

    void RefreshModList()
    {
        string[] newMods = (new string[] { "Default" }).Concat(ModHelpers.RefreshList()).ToArray();
        if (popupMods == null || (newMods != null && newMods.Length != popupMods.Length))
        {
            popupMods = newMods;
            if (loadModIndex != 0 && loadModIndex >= popupMods.Length)
            {
                loadModIndex = 0;
            }
        }
    }

    void RefreshScriptList()
    {
        List<string> scriptNames = new List<string>();
                
        // load default scripts always
        string scriptsRoot = Path.Combine(ModHelpers.GetRundirPath(), "scripts");
        DirectoryInfo di = new DirectoryInfo(scriptsRoot);
        if (!di.Exists)
        {
            Debug.LogError("Failed to load template list.  Script path does not exist: " + scriptsRoot);
            return;
        }

        FileInfo[] scriptFiles = di.GetFiles("*.lua");
        scriptList = scriptFiles.Select(info => Path.GetFileNameWithoutExtension(info.Name)).OrderBy(s => s).ToArray();

        // if we have a mod selected add the mod scripts
        if (saveModIndex > 0)
        {
            string modScriptsRoot = Path.Combine(ModHelpers.GetRundirPath(saveModIndex), "scripts");
            di = new DirectoryInfo(modScriptsRoot);
            if (!di.Exists)
            {
                Debug.LogError("Failed to load mod script list.  Script path does not exist: " + modScriptsRoot);
                return;
            }

            scriptFiles = di.GetFiles("*.lua");
            scriptList = scriptList.Concat(scriptFiles.Select(info => Path.GetFileNameWithoutExtension(info.Name))).Distinct().OrderBy(s => s).ToArray();
        }
    }

    void ParseObjVars()
    {
        curObjVars = new List<ShardsObjVar>();

        // for now just put the object variable component on everything
        if (curTemplate.ObjectVariableComponent != null && curTemplate.ObjectVariableComponent.Length > 0)
        {
            ObjectTemplateObjectVariableComponent objVarComp = curTemplate.ObjectVariableComponent[0];

            if(objVarComp.BoolVariable != null)
            {
                foreach(ObjectTemplateObjectVariableComponentBoolVariable boolVar in objVarComp.BoolVariable)
                {
                    bool value = Boolean.Parse(boolVar.Value);
                    ShardsObjVar objVar = new ShardsObjVar() { Name = boolVar.Name, Type = ShardsObjVar.ObjVarType.Boolean, BoolValue = value };
                    curObjVars.Add(objVar);
                }
            }
            if (objVarComp.DoubleVariable != null)
            {
                foreach (ObjectTemplateObjectVariableComponentDoubleVariable doubleVar in objVarComp.DoubleVariable)
                {
                    double value = double.Parse(doubleVar.Value);
                    ShardsObjVar objVar = new ShardsObjVar() { Name = doubleVar.Name, Type = ShardsObjVar.ObjVarType.Number, DoubleValue = value };
                    curObjVars.Add(objVar);
                }
            }
            if (objVarComp.LocVariable != null)
            {
                foreach (ObjectTemplateObjectVariableComponentLocVariable locVar in objVarComp.LocVariable)
                {
                    string[] values = locVar.Value.Trim().Split(new char[] { ',' });
                    Vector3 value = new Vector3(float.Parse(values[0]), float.Parse(values[1]), float.Parse(values[2]));

                    ShardsObjVar objVar = new ShardsObjVar() { Name = locVar.Name, Type = ShardsObjVar.ObjVarType.Loc, LocValue = value };
                    curObjVars.Add(objVar);
                }
            }
            if (objVarComp.StringVariable != null)
            {
                foreach (ObjectTemplateObjectVariableComponentStringVariable stringVar in objVarComp.StringVariable)
                {
                    ShardsObjVar objVar = new ShardsObjVar() { Name = stringVar.Name, Type = ShardsObjVar.ObjVarType.String, StrValue = stringVar.Value };
                    curObjVars.Add(objVar);
                }
            }
        }

        objVarEditor = ToolUtil.GetObjVarEditor(curObjVars);
    }

    void WriteObjVars()
    {
        // for now just put the object variable component on everything
        if (curTemplate.ObjectVariableComponent == null || curTemplate.ObjectVariableComponent.Length == 0)
        {
            curTemplate.ObjectVariableComponent = new ObjectTemplateObjectVariableComponent[1] { new ObjectTemplateObjectVariableComponent() };
        }        

        if (curObjVars != null)
        {
            ObjectTemplateObjectVariableComponent objVarComp = curTemplate.ObjectVariableComponent[0];

            IEnumerable<ShardsObjVar> boolVars = curObjVars.Where(item => item.Name != "" && item.Type == ShardsObjVar.ObjVarType.Boolean);
            objVarComp.BoolVariable = null;
            if( boolVars.Any() )
            {
                objVarComp.BoolVariable = boolVars.Select(item => new ObjectTemplateObjectVariableComponentBoolVariable() { Name = item.Name, Value = item.BoolValue.ToString() }).ToArray();
            }

            IEnumerable<ShardsObjVar> doubleVars = curObjVars.Where(item => item.Name != "" && item.Type == ShardsObjVar.ObjVarType.Number);
            objVarComp.DoubleVariable = null;
            if (doubleVars.Any())
            {
                objVarComp.DoubleVariable = doubleVars.Select(item => new ObjectTemplateObjectVariableComponentDoubleVariable() { Name = item.Name, Value = item.DoubleValue.ToString() }).ToArray();
            }

            IEnumerable<ShardsObjVar> locVars = curObjVars.Where(item => item.Name != "" && item.Type == ShardsObjVar.ObjVarType.Loc);
            objVarComp.LocVariable = null;
            if (locVars.Any())
            {
                objVarComp.LocVariable = locVars.Select(item => new ObjectTemplateObjectVariableComponentLocVariable() { Name = item.Name, Value = (item.LocValue.x.ToString() + "," + item.LocValue.y.ToString() + "," + item.LocValue.z.ToString()) }).ToArray();
            }

            IEnumerable<ShardsObjVar> stringVars = curObjVars.Where(item => item.Name != "" && item.Type == ShardsObjVar.ObjVarType.String);
            objVarComp.StringVariable = null;
            if (stringVars.Any())
            {
                objVarComp.StringVariable = stringVars.Select(item => new ObjectTemplateObjectVariableComponentStringVariable() { Name = item.Name, Value = item.StrValue }).ToArray();
            }
        }
    }

    void ClearTemplate()
    {
        curTemplate = null;
        curObjVars = new List<ShardsObjVar>();
        objVarEditor = ToolUtil.GetObjVarEditor(curObjVars);
        curInitializers = new Dictionary<string, ModuleInitializer>();
        saveModIndex = loadModIndex;
        RefreshSaveCategories();
    }

    void LoadTemplate(string category,string template)
    {
        string curTemplatePath = ModHelpers.GetTemplatePath(category, template, loadModIndex);
        XmlSerializer ser = new XmlSerializer(typeof(ObjectTemplate));
        using (XmlReader reader = XmlReader.Create(curTemplatePath))
        {
            curTemplate = ser.Deserialize(reader) as ObjectTemplate;
            ParseObjVars();
            curInitializers = ModuleInitializer.ParseInitializers(curTemplate, loadModIndex);
        }
        newTemplateName = template;
    }

    void SaveTemplate()
    {
        if(saveModIndex == 0 && !ToolUtil.CanWriteToDefault)
        {
            EditorUtility.DisplayDialog("ERROR", "You can not make changes to the default ruleset.", "OK");
            return;
        }

        string category = popupSaveCategories[saveCategoryIndex-1];
        if (EditorUtility.DisplayDialog("Save Template", "Do you wish to save " + newTemplateName + " to category " + category + "?", "Save", "Cancel"))
        {
            WriteObjVars();            

            string templatesRoot = Path.Combine(ModHelpers.GetRundirPath(saveModIndex), "templates");
            string categoryPath = Path.Combine(templatesRoot, category);
            string curTemplatePath = Path.Combine(categoryPath, newTemplateName + ".xml");

            if (curInitializers != null)
            {
                foreach (var initializer in curInitializers)
                {
                    var moduleNode = curTemplate.ScriptEngineComponent[0].LuaModule.First(item => item.Name == initializer.Key);
                    initializer.Value.WriteToTemplate(moduleNode);
                }
            }

            ToolUtil.WriteXML(new XmlSerializer(typeof(ObjectTemplate)), curTemplatePath, curTemplate);            

            EditorUtility.DisplayDialog("Saved", newTemplateName + " saved.", "Ok");
        }
    }

    void LoadClientIds()
    {
        if (ToolUtil.ClientIdLibrary != null)
        {
            clientIdList = ToolUtil.ClientIdLibrary.Select(item => item == null ? "" : item.name).ToArray();
        }
    }

    void RefreshObjectTags()
    {
        string objectTagPath = Path.Combine(ModHelpers.GetRundirPath(), "ObjectTagDefinitions.xml");        

        XmlSerializer ser = new XmlSerializer(typeof(ObjectTags));
        using (XmlReader reader = XmlReader.Create(objectTagPath))
        {
            objectTags = ser.Deserialize(reader) as ObjectTags;
        }
    }

    void CreateCategory()
    {
        string templatePath = ShardsExtensionMethods.CombinePaths(ModHelpers.GetRundirPath(saveModIndex), "templates", newCategoryName);
        Directory.CreateDirectory(templatePath);
        RefreshSaveCategories();
        saveCategoryIndex = Array.IndexOf(popupSaveCategories, newCategoryName) + 1;
    }

    void OnGUI()
    {
        EditorGUILayout.Space();

        if (!ToolUtil.ValidateSettings())
        {
            EditorGUILayout.LabelField("Invalid Settings. Please fix your paths by going to the 'LoA Toolkit/Settings' menu");
            return;
        }
        else if (!Application.isPlaying && !ToolUtil.EditWhileStopped)
        {
            EditorGUILayout.LabelField("To begin, press play.");
            return;
        }
        else if (clientIdList == null)
        {
            EditorGUILayout.LabelField("Toolkit general error. Make sure you are running the 'Toolkit Controller' scene.");
            return;
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Load", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        // The actual window code goes here        

        GUILayout.BeginHorizontal();
        loadModIndex = EditorGUILayout.Popup("Mod", loadModIndex, popupMods);
        
        if (GUILayout.Button("Load"))
        {
            if(curTemplate == null || EditorUtility.DisplayDialog("Load Template", "Loading will replace your current template settings. Are you sure?", "Yes", "Cancel") )
            {
                ModHelpers.OpenTemplateSelectionPopup(loadModIndex, OnLoadTemplateSelected, false);
            }
        }
        GUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Create New"))
        {
            if (curTemplate == null || EditorUtility.DisplayDialog("New Template", "This will reset your current template settings. Are you sure?", "Yes", "Cancel"))
            {
                ClearTemplate();
                curTemplate = new ObjectTemplate();
                curObjVars = new List<ShardsObjVar>();
                objVarEditor = ToolUtil.GetObjVarEditor(curObjVars);
                return;
            }
        }

        EditorGUILayout.Space();
        ToolUtil.DrawSeparator();
        EditorGUILayout.Space();

        if (curTemplate == null)
        {
            GUIStyle centertext = new GUIStyle(GUI.skin.label);
            centertext.alignment = TextAnchor.MiddleCenter;
            EditorGUILayout.LabelField("Create New or Load a Template", centertext);       
        }
        else
        {
            EditorGUILayout.LabelField("Current Template", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            EditorGUILayout.Space();        

            int newSaveModIndex = EditorGUILayout.Popup("Mod", saveModIndex, popupMods.ToArray());
            if (newSaveModIndex != saveModIndex)
            {
                saveModIndex = newSaveModIndex;
                RefreshSaveCategories();
                RefreshScriptList();
            }

            if (curTemplate.ClientId == "" || curTemplate.ClientId == null)
            {
                curTemplate.ClientId = "0";
            }

            int curClientId = Int32.Parse(curTemplate.ClientId);

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = false;
            EditorGUILayout.TextField(new GUIContent("Client Id:", "Select a client id to use for this template."), clientIdList[curClientId]);
            GUI.enabled = true;
            if (GUILayout.Button("Select") && clientIdPopup == null)
            {
                clientIdPopup = ScriptableObject.CreateInstance<ClientIdSelectorPopup>();
                clientIdPopup.CallbackFunc = OnClientIdSelected;
                clientIdPopup.Show();
            }
            EditorGUILayout.EndHorizontal();          

            // dont allow any other editing until they have selected a client id
            if (curTemplate.ClientId != "0")
            {
                string curName = curTemplate.Name != null ? curTemplate.Name : "";
                string newName = EditorGUILayout.TextField("Display Name", curTemplate.Name);
                if (newName != curName)
                {
                    curTemplate.Name = newName == "" ? null : newName;
                }

                if (curTemplate.Hue == null || curTemplate.Hue == "")
                {
                    curTemplate.Hue = "0xFFFFFFFF";
                }
                Color oldColor = ToolUtil.ParseColor(curTemplate.Hue.Substring(4), 0);
                Color newColor = EditorGUILayout.ColorField("Object Hue", oldColor);
                if (oldColor != newColor)
                {
                    curTemplate.Hue = "0xFF" + ToolUtil.EncodeColor(newColor);
                }

                float scale = (curTemplate.ScaleModifier != null && curTemplate.ScaleModifier != "") ? float.Parse(curTemplate.ScaleModifier) : 1.0f;
                scale = EditorGUILayout.FloatField("Scale Modifier", scale);
                curTemplate.ScaleModifier = scale.ToString();

                if (curTemplate.MobileComponent != null && curTemplate.MobileComponent.Length != 0)
                {
                    float runSpeed = 6.0f;
                    if (curTemplate.MobileComponent[0].BaseRunSpeed != null && curTemplate.MobileComponent[0].BaseRunSpeed != "")
                    {
                        runSpeed = float.Parse(curTemplate.MobileComponent[0].BaseRunSpeed);
                    }
                    float newRunSpeed = EditorGUILayout.FloatField("Base Speed:", runSpeed);
                    if (newRunSpeed == 6.0f)
                    {
                        curTemplate.MobileComponent[0].BaseRunSpeed = null;
                    }
                    else
                    {
                        curTemplate.MobileComponent[0].BaseRunSpeed = newRunSpeed.ToString();
                    }

                    string[] mobileTypes = { "Friendly", "Animal", "Monster" };
                    int curType = 0;
                    if (mobileTypes.Contains(curTemplate.MobileComponent[0].MobileType))
                    {
                        curType = Array.IndexOf(mobileTypes, curTemplate.MobileComponent[0].MobileType);
                    }
                    int newMobileType = EditorGUILayout.Popup("Mobile Type:", curType, mobileTypes);
                    curTemplate.MobileComponent[0].MobileType = mobileTypes[newMobileType];
                }

                foreach (var objectTag in GetTagsForClientId(curTemplate.ClientId))
                {
                    if (objectTag.SharedStateEntry != null)
                    {
                        foreach (var sharedStateEntry in objectTag.SharedStateEntry)
                        {
                            // some properties can not be overriden in the template
                            if (sharedStateEntry.restriction == "readonly" || sharedStateEntry.restriction == "runtime")
                                continue;

                            // hide the weight property for mobiles
                            if (IsMobile(curTemplate.ClientId) && sharedStateEntry.name == "Weight")
                                continue;

                            string curValue = sharedStateEntry.value;
                            ObjectTemplateSharedStateEntry templateEntry = null;

                            // if the value is overriden in the template then set it
                            if (curTemplate.SharedStateEntry != null
                                && curTemplate.SharedStateEntry.Any(item => item.name == sharedStateEntry.name))
                            {
                                templateEntry = curTemplate.SharedStateEntry.First(item => item.name == sharedStateEntry.name);
                                curValue = templateEntry.value;
                            }

                            string newValueStr = curValue;

                            switch (sharedStateEntry.type)
                            {
                                case "int":
                                    GUILayout.BeginHorizontal();
                                    GUILayout.Label(sharedStateEntry.name, GUILayout.Width(150));
                                    int newValue = EditorGUILayout.IntField(Int32.Parse(curValue));
                                    newValueStr = newValue.ToString();
                                    GUILayout.EndHorizontal();
                                    break;
                                case "bool":
                                    GUILayout.BeginHorizontal();
                                    GUILayout.Label(sharedStateEntry.name, GUILayout.Width(150));

                                    string[] popupOptions = new string[] { "False", "True" };
                                    bool oldBoolValue = bool.Parse(curValue);
                                    int oldValue = oldBoolValue ? 1 : 0;
                                    int popupValue = EditorGUILayout.Popup(oldValue, popupOptions);
                                    newValueStr = popupValue == 0 ? "False" : "True";
                                    GUILayout.EndHorizontal();
                                    break;
                                case "string":
                                    GUILayout.BeginHorizontal();
                                    GUILayout.Label(sharedStateEntry.name, GUILayout.Width(150));

                                    newValueStr = EditorGUILayout.TextField(curValue);
                                    GUILayout.EndHorizontal();
                                    break;
                            }

                            // value has changed
                            if (newValueStr != curValue)
                            {
                                // if we currently override this value
                                if (templateEntry != null)
                                {
                                    // value is not default just set it
                                    if (newValueStr != sharedStateEntry.value)
                                    {
                                        templateEntry.value = newValueStr;
                                    }
                                    // value is default so we dont need this override anymore, remove it
                                    else
                                    {
                                        curTemplate.SharedStateEntry = curTemplate.SharedStateEntry.Where(item => item.name != sharedStateEntry.name).ToArray();
                                    }
                                }
                                // we have not override this value so create an override if its not default
                                else if (newValueStr != sharedStateEntry.value)
                                {
                                    ObjectTemplateSharedStateEntry newEntry = new ObjectTemplateSharedStateEntry() { name = sharedStateEntry.name, type = sharedStateEntry.type, value = newValueStr };
                                    if (curTemplate.SharedStateEntry == null)
                                    {
                                        curTemplate.SharedStateEntry = new ObjectTemplateSharedStateEntry[] { newEntry };
                                    }
                                    else
                                    {
                                        curTemplate.SharedStateEntry = curTemplate.SharedStateEntry.Concat(new ObjectTemplateSharedStateEntry[] { newEntry }).ToArray();
                                    }
                                }
                            }
                        }
                    }
                }

                EditorGUILayout.Space();
                EditorGUILayout.Space();

                if (objVarEditor != null)
                {
                    objVarEditor.DoLayoutList();
                }               

                
                GUILayout.Label("Behaviors:");
                EditorGUILayout.Space();
                EditorGUILayout.Space();

                ObjectTemplateScriptEngineComponentLuaModule[] curModules = new ObjectTemplateScriptEngineComponentLuaModule[0];

                if (curTemplate.ScriptEngineComponent != null
                    && curTemplate.ScriptEngineComponent.Length != 0
                    && curTemplate.ScriptEngineComponent[0].LuaModule != null)
                {
                    curModules = curTemplate.ScriptEngineComponent[0].LuaModule.ToArray();
                    foreach (ObjectTemplateScriptEngineComponentLuaModule luaModule in curModules)
                    {
                        GUILayout.BeginHorizontal();

                        bool oldIsShown = initializerFoldouts.ContainsKey(luaModule.Name) ? initializerFoldouts[luaModule.Name] : false;
                        bool newIsShown = EditorGUILayout.Foldout(oldIsShown, luaModule.Name);
                        initializerFoldouts[luaModule.Name] = newIsShown;

                        if (GUILayout.Button("Detach", GUILayout.Width(100)))
                        {
                            curTemplate.ScriptEngineComponent[0].LuaModule = curTemplate.ScriptEngineComponent[0].LuaModule.Where(item => item.Name != luaModule.Name).ToArray();
                            curInitializers.Remove(luaModule.Name);
                        }
                        GUILayout.EndHorizontal();

                        if (newIsShown && curInitializers != null && curInitializers.ContainsKey(luaModule.Name))
                        {
                            curInitializers[luaModule.Name].OnGUI();
                        }

                        EditorGUILayout.Space();
                    }
                }                
                
                if (GUILayout.Button("Attach", GUILayout.Width(100)))
                {
                    string[] scriptsToShow = scriptList.Where(item => !item.Contains("base") && !item.Contains("incl") && !curModules.Any(curMod => curMod.Name == item)).ToArray();
                    StringListSelectorPopup scriptSelector = ScriptableObject.CreateInstance<StringListSelectorPopup>();
                    scriptSelector.StringList = scriptsToShow;
                    scriptSelector.CallbackFunc = OnScriptAttachSelected;
                    scriptSelector.Show();
                }
                
                // remove script component if no scripts
                if (curTemplate.ScriptEngineComponent != null
                    && curTemplate.ScriptEngineComponent.Length != 0
                    && curTemplate.ScriptEngineComponent[0].LuaModule != null
                    && curTemplate.ScriptEngineComponent[0].LuaModule.Length == 0)
                {
                    curTemplate.ScriptEngineComponent = null;
                }

                EditorGUILayout.Space();
                ToolUtil.DrawSeparator();
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Save", EditorStyles.boldLabel);
                EditorGUILayout.Space();
                EditorGUILayout.Space();

                newTemplateName = EditorGUILayout.TextField("Template Name:", newTemplateName);

                if (popupSaveCategories != null)
                {
                    saveCategoryIndex = EditorGUILayout.Popup("Category", saveCategoryIndex, (new string[] { "New..." }).Concat(popupSaveCategories).ToArray());
                }
                else
                {
                    saveCategoryIndex = EditorGUILayout.Popup("Category", saveCategoryIndex, new string[] { "New..." });
                }
                if (saveCategoryIndex == 0)
                {
                    GUILayout.BeginHorizontal();
                    newCategoryName = EditorGUILayout.TextField("Category Name:", newCategoryName);
                    if (GUILayout.Button("Create"))
                    {
                        CreateCategory();
                    }
                    GUILayout.EndHorizontal();
                }

                GUI.enabled = saveCategoryIndex != 0;
                if (GUILayout.Button("Save", GUILayout.Width(200)))
                {
                    SaveTemplate();
                }
                GUI.enabled = true;

                EditorGUILayout.Space();
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void OnLoadTemplateSelected(string category, string template)
    {
        if (category != "" && template != "")
        {
            LoadTemplate(category, template);
        }
        saveModIndex = loadModIndex;
        RefreshSaveCategories();
    }

    private void OnScriptAttachSelected(string script)
    {
        if(script != "")
        {
            if (curTemplate.ScriptEngineComponent == null)
            {
                curTemplate.ScriptEngineComponent = new ObjectTemplateScriptEngineComponent[1] { new ObjectTemplateScriptEngineComponent() };
            }

            ObjectTemplateScriptEngineComponentLuaModule newModule = new ObjectTemplateScriptEngineComponentLuaModule() { Name = script };

            if (curTemplate.ScriptEngineComponent[0].LuaModule == null)
            {
                curTemplate.ScriptEngineComponent[0].LuaModule = new ObjectTemplateScriptEngineComponentLuaModule[1] { newModule };
            }
            else
            {
                curTemplate.ScriptEngineComponent[0].LuaModule = curTemplate.ScriptEngineComponent[0].LuaModule.Concat(new ObjectTemplateScriptEngineComponentLuaModule[1] { newModule }).ToArray();
            }

            // add initializer structure
            if(curInitializers == null)
            {
                curInitializers = new Dictionary<string, ModuleInitializer>();
            }
            curInitializers.Add(newModule.Name, new ModuleInitializer(newModule,loadModIndex));
        }
    }

    private void OnClientIdSelected(uint newClientId)
    {
        int curClientId = Int32.Parse(curTemplate.ClientId);
        if(newClientId != 0 && curClientId != newClientId)
        {            
            curTemplate.ClientId = newClientId.ToString();

            // update mobile component
            if (IsMobile(curTemplate.ClientId)
                && (curTemplate.MobileComponent == null || curTemplate.MobileComponent.Length == 0))
            {
                curTemplate.MobileComponent = new ObjectTemplateMobileComponent[] { new ObjectTemplateMobileComponent() };
            }
            else if (!IsMobile(curTemplate.ClientId)
                && (curTemplate.MobileComponent != null))
            {
                curTemplate.MobileComponent = null;
            }
        }

        Repaint();
    }

    // Object Tag Helpers
    IEnumerable<ObjectTagsObjectTag> GetTagsForClientId(string clientId)
    {
        // 0 is invalid
        if( clientId == "0" )
        {
            return new ObjectTagsObjectTag[0];
        }
        return objectTags.Items.Where(item => item.ClientId == null || item.ClientId.Any(element => element.Value == clientId));
    }

    bool IsMobile(string clientId)
    {
        IEnumerable<ObjectTagsObjectTag> tags = GetTagsForClientId(clientId);
        return tags.Any(item => item != null && item.Name == "Mobile");
    }

    private string[] clientIdList;
    private string[] scriptList;

    private string[] popupMods;
    private string[] popupSaveCategories;

    private int loadModIndex = 0;
    
    private int saveModIndex = 0;
    private int saveCategoryIndex = 0;

    private ObjectTemplate curTemplate;
    private List<ShardsObjVar> curObjVars;
    private UnityEditorInternal.ReorderableList objVarEditor;
    private Dictionary<string,ModuleInitializer> curInitializers;

    private ObjectTags objectTags;	

    private Vector2 scrollPos = new Vector2(0, 0);
    private Dictionary<string, bool> initializerFoldouts = new Dictionary<string, bool>();
    private int newObjVarType = 0;
    private string newTemplateName = "";
    private string newCategoryName = "";
    private string newModName = "";

    private ClientIdSelectorPopup clientIdPopup;
}
