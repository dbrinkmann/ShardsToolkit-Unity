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

public class LibraryWizard : EditorWindow
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

        if( GUILayout.Button("Create Custom Object Library") )
        {
            string assetPath = "Assets/Custom Object Library.prefab";
            string selectionPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (selectionPath != "")
            {
                if (Path.GetExtension(selectionPath) != "")
                {
                    selectionPath = selectionPath.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
                }
                assetPath = selectionPath + "/Custom Object Library.prefab";
            }

            if (EditorUtility.DisplayDialog("Confirm", "Do you want to create a custom object library in the project at \"" + assetPath + "\"?", "Ok", "Cancel"))
            {                
                GameObject libraryGameObj = new GameObject();
                libraryGameObj.name = "Custom Object Library";
                ClientObjectLibrary libraryComp = libraryGameObj.AddComponent<ClientObjectLibrary>();

                SerializedObject so = new SerializedObject(libraryComp);
                SerializedProperty prefabProp = so.FindProperty("ClientIdPrefabs");
                prefabProp.InsertArrayElementAtIndex(0);
                so.ApplyModifiedProperties();

                PrefabUtility.CreatePrefab(assetPath, libraryGameObj);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                DestroyImmediate(libraryGameObj);
            }
        }
    }    
}
