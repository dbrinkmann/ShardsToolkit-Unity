// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// Originally part of the Shards Engine Toolkit; released under the MIT License.
// See LICENSE in the repository root.
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Linq;

public class SettingsWindow : EditorWindow 
{    
    public SettingsWindow()
    {
        basePath = ToolUtil.Settings.BasePath;
        modsPath = ToolUtil.Settings.ModsPath;
        InitializeWindow();
    }

    public void OnFocus()
    {
        InitializeWindow();
    }

    public void InitializeWindow()
    {
        popupMods = (new string[] { "Default" }).Concat(ModHelpers.RefreshList()).ToArray();
        for (int i = 0; i < popupMods.Length; ++i)
        {
            if (popupMods[i].Equals(ToolUtil.Settings.CurrentModName))
            {
                modIndex = i;
                break;
            }
        }
    }
    
    public void OnGUI()
    {
        EditorGUILayout.Space();        
        EditorGUILayout.Space();
        EditorGUIUtility.labelWidth = 80;
        EditorGUILayout.BeginHorizontal();
        GUI.SetNextControlName("BasePath");
        basePath = EditorGUILayout.TextField("Base Path: ", basePath);
        if(GUILayout.Button("Browse"))
        {
            GUI.FocusControl("ModsPath");
            basePath = EditorUtility.OpenFolderPanel("Select Base path folder", basePath, "");
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        GUI.SetNextControlName("ModsPath");
        modsPath = EditorGUILayout.TextField("Mods Path: ", modsPath);
        if (GUILayout.Button("Browse"))
        {
            GUI.FocusControl("BasePath");
            modsPath = EditorUtility.OpenFolderPanel("Select Base path folder", modsPath, "");            
        }
        EditorGUILayout.EndHorizontal();

        int newModIndex = EditorGUILayout.Popup("Mod", modIndex, popupMods);
        if(newModIndex != modIndex)
        {            
            modIndex = newModIndex;
        }

        GUI.enabled = !(ToolUtil.Settings.BasePath == basePath && ToolUtil.Settings.ModsPath == modsPath && ToolUtil.Settings.CurrentModName == currentMod);
        if (GUILayout.Button("Save"))
        {
            ToolUtil.Settings.BasePath = basePath;
            ToolUtil.Settings.ModsPath = modsPath;
            
            if (popupMods.Length > modIndex)
            {
                ToolUtil.Settings.CurrentModName = popupMods[modIndex];
            }
            else
            {
                ModHelpers.CreateMod(currentMod);
                InitializeWindow();
            }

            /*
            ToolUtil.Settings.CurrentModName = currentMod;
            if(!string.IsNullOrEmpty(currentMod))
            {
                ModHelpers.CreateMod(currentMod);
            }
            */
            ToolUtil.Settings.Save();
        }
        GUI.enabled = true;
    }

    public void Update()
    {
        Repaint();
    }

    private string basePath;
    private string modsPath;
    private string currentMod;
    private string[] popupMods;
    private int modIndex = 0;
}
