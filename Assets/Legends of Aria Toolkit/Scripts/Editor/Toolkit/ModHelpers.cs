// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// Originally part of the Shards Engine Toolkit; released under the MIT License.
// See LICENSE in the repository root.
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CoreUtil;

public static class ModHelpers 
{
    public static List<string> AvailableMods
    {
        get
        {
            if (availableMods == null)
            {
                if (!Directory.Exists(ToolUtil.Settings.ModsPath))
                {
                    Debug.LogError("Failed to load mod dir. path does not exist: " + ToolUtil.Settings.ModsPath);
                    return new List<string>();
                }
                DirectoryInfo di = new DirectoryInfo(ToolUtil.Settings.ModsPath);
                DirectoryInfo[] modDirs = di.GetDirectories();
                availableMods = modDirs.Select(item => item.Name).ToList();
            }
            return availableMods;
        }
    }

    public static List<string> RefreshList()
    {
        availableMods = null;

        return AvailableMods;
    }

    public static string GetRundirPath(int modIndex = 0)
    {
        if (modIndex == 0)
        {
            return ToolUtil.Settings.BasePath;
        }
        else
        {
            // avaialble mods does not include default
            modIndex = modIndex - 1;
            return ShardsExtensionMethods.CombinePaths(ToolUtil.Settings.ModsPath, AvailableMods[modIndex]);
        }
    }

    public static string GetScriptPath(string scriptName, int modIndex = 0)
    {
        string scriptRoot = Path.Combine(ModHelpers.GetRundirPath(), "scripts");
        string scriptPath = Path.Combine(scriptRoot, scriptName);

        if (modIndex > 0)
        {
            string modScriptRoot = Path.Combine(ModHelpers.GetRundirPath(modIndex), "scripts");
            string modScriptPath = Path.Combine(modScriptRoot, scriptName);
            if (File.Exists(modScriptPath))
            {
                scriptPath = modScriptPath;
            }
        }

        return scriptPath;
    }

    public static string GetTemplatePath(string category, string templateName, int modIndex = 0)
    {
        string templatesRoot = Path.Combine(ModHelpers.GetRundirPath(modIndex), "templates");
        string categoryPath = Path.Combine(templatesRoot, category);
        string templatePath = Path.Combine(categoryPath, templateName + ".xml");
        if(modIndex != 0 && !File.Exists(templatePath))
        {
            templatesRoot = Path.Combine(ModHelpers.GetRundirPath(), "templates");
            categoryPath = Path.Combine(templatesRoot, category);
            return Path.Combine(categoryPath, templateName + ".xml");
        }

        return templatePath;
    }

    public static string GetTemplatePath(string templateName, int modIndex = 0, Dictionary<string, List<string>> templateList = null)
    {
        var allTemplates = templateList != null ? templateList : GetAllTemplates(modIndex,true);
        if(allTemplates.Any(item => item.Value.Contains(templateName)))
        {
            var categoryEntry = allTemplates.First(item => item.Value.Contains(templateName));            
            string category = categoryEntry.Key;
            return GetTemplatePath(category, templateName, modIndex);
        }

        return null;
    }

    public static void CreateMod(string newModName)
    {
        string modPath = Path.Combine(ToolUtil.Settings.ModsPath, newModName);
        Directory.CreateDirectory(modPath);
        Directory.CreateDirectory(modPath + "/mapdata");
        Directory.CreateDirectory(modPath + "/scripts");
        Directory.CreateDirectory(modPath + "/templates");
        ToolUtil.Settings.CurrentModName = newModName;
    }

    public static string GetModName(int modIndex)
    {
        string modName = "Default";
        if (modIndex > 0)
        {
            // available mods does not include default
            modName = ModHelpers.AvailableMods[modIndex - 1];
        }

        return modName;
    }

    public static Dictionary<string, List<string>> GetAllTemplates(int modIndex, bool includeDefaults = false)
    {
        Dictionary<string, List<string>> templateList = new Dictionary<string, List<string>>();

        if (modIndex != 0 && includeDefaults)
        {
            templateList = GetAllTemplates(0);
        }

        string templatesRoot = Path.Combine(ModHelpers.GetRundirPath(modIndex), "templates");
        DirectoryInfo di = new DirectoryInfo(templatesRoot);
        if (!di.Exists)
        {
            Debug.LogError("Failed to load template list.  Template path does not exist: " + templatesRoot);
            return null;
        }

        DirectoryInfo[] templateDirs = di.GetDirectories();
        foreach (var templateDir in templateDirs)
        {
            string category = templateDir.Name;
            List<string> categoryTemplates = new List<string>();
            if (templateList.ContainsKey(category))
            {
                categoryTemplates = templateList[category];
            }

            FileInfo[] templateFiles = templateDir.GetFiles("*.xml");
            foreach (var templateFile in templateFiles)
            {
                string templateName = Path.GetFileNameWithoutExtension(templateFile.Name);                

                // remove any entries from the default
                foreach(var key in templateList.Keys)
                {
                    templateList[key].RemoveAll(item => item == templateName);
                }

                categoryTemplates.Add(templateName);
            }

            categoryTemplates.Sort();

            templateList[category] = categoryTemplates;
        }        

        return templateList;
    }

    public static TemplateSelectorPopup OpenTemplateSelectionPopup(int modIndex, Action<string, string> callbackFunc, bool includeDefaults = true)
    {
        TemplateSelectorPopup popup = ScriptableObject.CreateInstance<TemplateSelectorPopup>();
        popup.TemplateList = GetAllTemplates(modIndex, includeDefaults);
        popup.CallbackFunc = callbackFunc;
        popup.ActiveObject = Selection.activeGameObject;        
        popup.Show();

        return popup;
    }

    private static List<string> availableMods = new List<string>();
}
