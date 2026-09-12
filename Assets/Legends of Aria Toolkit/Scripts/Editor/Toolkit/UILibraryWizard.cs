// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// Originally part of the Shards Engine Toolkit; released under the MIT License.
// See LICENSE in the repository root.
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Text.RegularExpressions;

public class UILibraryWizard : EditorWindow
{    
    public void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        if (!ToolUtil.ValidateSettings())
        {
            EditorGUILayout.LabelField("Invalid Settings. Please fix your paths by going to the 'LoA Toolkit/Settings' menu.");
            return;
        }

        EditorGUILayout.HelpBox("Click a folder in the 'Project' window where you want the library prefab created before you hit create. If no folder is selected, it will be created in the top level of the project.",MessageType.Info);        

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        if( GUILayout.Button("Create Custom UI Texture Library") )
        {
            string assetPath = "Assets/UI Texture Library.prefab";
            string selectionPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (selectionPath != "")
            {
                if (Path.GetExtension(selectionPath) != "")
                {
                    selectionPath = selectionPath.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
                }
                assetPath = selectionPath + "/UI Texture Library.prefab";
            }

            if (EditorUtility.DisplayDialog("Confirm", "Do you want to create a ui texture library in the project at \"" + assetPath + "\"?", "Ok", "Cancel"))
            { 
                GameObject libraryGameObj = new GameObject();
                libraryGameObj.name = "UI Texture Library";
                UITextureLibrary libraryComp = libraryGameObj.AddComponent<UITextureLibrary>();

                PrefabUtility.CreatePrefab(assetPath, libraryGameObj);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                try
                {
                    if (Directory.Exists("Assets/UI") == false)
                        Directory.CreateDirectory("Assets/UI");

                    if (Directory.Exists("Assets/UI/Textures") == false)
                        Directory.CreateDirectory("Assets/UI/Textures");

                    if (Directory.Exists("Assets/UI/Maps") == false)
                        Directory.CreateDirectory("Assets/UI/Maps");

                    if (Directory.Exists("Assets/UI/Loading") == false)
                        Directory.CreateDirectory("Assets/UI/Loading");
                }
                catch
                {
                    Debug.LogError("Failed to create folder structure.");
                }
                DestroyImmediate(libraryGameObj);
            }
        }
    }    
}
