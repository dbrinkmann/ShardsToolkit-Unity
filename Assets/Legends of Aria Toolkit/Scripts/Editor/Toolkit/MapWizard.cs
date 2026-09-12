// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// Originally part of the Shards Engine Toolkit; released under the MIT License.
// See LICENSE in the repository root.
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Text.RegularExpressions;

public class MapWizard : EditorWindow
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

        string[] options = new string[] { "Outdoor", "Dungeon" };
        mapTypeIndex = EditorGUILayout.Popup("Map Type",mapTypeIndex,mapTypes);

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        if( GUILayout.Button("Create Map") )
        {
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject mapDataGameObj = new GameObject();
            mapDataGameObj.name = "MapData";
            MapData mapData = mapDataGameObj.AddComponent<MapData>();

            if(mapTypes[mapTypeIndex] == "Outdoor")
            {
                GameObject lightingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Legends of Aria Toolkit/Prefabs/DefaultOutdoorLighting.prefab");
                GameObject lightingInst = PrefabUtility.InstantiatePrefab(lightingPrefab) as GameObject;

                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(0.4666667f, 0.454902f, 0.5647059f);

                GameObject cameraPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Legends of Aria Toolkit/Prefabs/DefaultOutdoorCamera.prefab");
                GameObject cameraInst = PrefabUtility.InstantiatePrefab(cameraPrefab) as GameObject;
            }
            else if(mapTypes[mapTypeIndex] == "Dungeon")
            {
                GameObject cameraPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Legends of Aria Toolkit/Prefabs/DefaultDungeonCamera.prefab");
                GameObject cameraInst = PrefabUtility.InstantiatePrefab(cameraPrefab) as GameObject;
            }

            TagsHelpers.AddTagsAndLayers();

            EditorSceneManager.SaveOpenScenes();
        }
    }

    private string mapName = "";
    string[] mapTypes = new string[] { "Outdoor", "Dungeon" };
    private int mapTypeIndex;
}
