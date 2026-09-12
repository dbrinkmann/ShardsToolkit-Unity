// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// Originally part of the Shards Engine Toolkit; released under the MIT License.
// See LICENSE in the repository root.
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;

public class ModWizard : EditorWindow
{
    public static uint NextId = 0;    

    public void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        if (!ToolUtil.ValidateSettings())
        {
            EditorGUILayout.LabelField("Invalid Settings. Please fix your paths by going to the 'LoA Toolkit/Settings' menu.");
            return;
        }

        modName = EditorGUILayout.TextField("Name: ",modName);

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        string modPath = Path.Combine(ToolUtil.Settings.ModsPath, modName);
        Regex r = new Regex("^[a-zA-Z0-9 _-]*$");
        if(modName == "" || !r.IsMatch(modName))
        {
            EditorGUILayout.LabelField("Invalid mod name. Please only use alpha numberic characters and spaces, underscores and dashes.");
        }
        else if(Directory.Exists(modPath))
        {
            EditorGUILayout.LabelField("Mod already exists.");
            if (GUILayout.Button("Fix Directory Structure"))
            {
                ModHelpers.CreateMod(modName);
            }
        }
        else
        {
            if( GUILayout.Button("Create Mod") )
            {
                ModHelpers.CreateMod(modName);
            }

            if (modName != "")
            {
                EditorGUILayout.LabelField("Directory structure to create: ");
                EditorGUILayout.Space();
                EditorGUILayout.LabelField((modPath + "/mapdata").Replace("/","\\"));
                EditorGUILayout.LabelField((modPath + "/scripts").Replace("/", "\\"));
                EditorGUILayout.LabelField((modPath + "/templates").Replace("/", "\\"));
            }
        }        
    }

    private string modName = "";
}
