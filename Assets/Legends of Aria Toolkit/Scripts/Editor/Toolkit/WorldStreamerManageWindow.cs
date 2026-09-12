using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

public class WorldStreamerManageWindow : EditorWindow {
    
    // Add menu item named "My Window" to the Window menu
    [MenuItem("LoA Toolkit/Utils/World Streamer Manager", false, 19)]
    public static void ShowWindow()
    {
        //Show existing window instance. If one doesn't exist, make one.
        WorldStreamerManageWindow windowRef = EditorWindow.GetWindow(typeof(WorldStreamerManageWindow)) as WorldStreamerManageWindow;

        windowRef.titleContent = new GUIContent() { text = "World Streamer Manager" };
        manageData = WorldStreamerManageUtils.LoadWorldStreamManageData(true);
    }

    void OnGUI()
    {
        if (manageData == null)
        {
            manageData = WorldStreamerManageUtils.LoadWorldStreamManageData(true);
        }
#if WORLD_STREAMER_ENABLED
        if (worldStreamers == null)
        {
            worldStreamers = FindObjectsOfType<Streamer>();
        }

        if (worldStreamers == null || worldStreamers.Length == 0)
        {
            GUILayout.Label("No streamers found in current scene");
        }
        else
        {
            foreach (Streamer streamer in worldStreamers)
            {
                if (streamer != null && streamer.sceneCollection != null)
                {
                    if (GUILayout.Button(new GUIContent(streamer.name, "Load scene collection from " + streamer.name)))
                    {
                        sceneCollectionRef = streamer.sceneCollection;
                        manageData.xLimitsx = sceneCollectionRef.xLimitsx;
                        manageData.xLimitsy = sceneCollectionRef.xLimitsy;
                        manageData.zLimitsx = sceneCollectionRef.zLimitsx;
                        manageData.zLimitsy = sceneCollectionRef.zLimitsy;
                        manageData.scenePath = sceneCollectionRef.path;
                        manageData.prefixScene = sceneCollectionRef.prefixScene;
                        WorldStreamerManageUtils.SaveWorldStreamManageData(manageData);
                    }
                }
            }
        }
#endif

        if (manageData != null && manageData.scenePath != null)
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Load All", "Load every scene in the split")))
            {
                LoadUnloadAll(true);
            }
            if (GUILayout.Button(new GUIContent("Unload All", "Unload every scene in the split")))
            {
                LoadUnloadAll(false);
            }
            EditorGUILayout.EndHorizontal();

            for (int z = manageData.zLimitsy; z >= manageData.zLimitsx; z--)
            {
                EditorGUILayout.BeginHorizontal();
                for (int x = manageData.xLimitsx; x <= manageData.xLimitsy; x++)
                {
                    int sceneIndex = WorldStreamerManageUtils.GetSceneIndex(x, z, manageData);
                    bool sceneLoaded = sceneIndex != -1;

                    if (GUILayout.Button(new GUIContent((sceneLoaded ? " L" : ""), (x + ":" + z)), GUILayout.Width(50), GUILayout.Height(50)))
                    {
                        if (!Application.isPlaying)
                        {
                            if (sceneLoaded)
                            {
                                Scene sceneObj = EditorSceneManager.GetSceneAt(sceneIndex);
                                bool completeAction = true;
                                if (sceneObj.isDirty)
                                {
                                    completeAction = EditorSceneManager.SaveModifiedScenesIfUserWantsTo(new Scene[] { sceneObj });
                                }

                                if (completeAction)
                                {
                                    EditorSceneManager.CloseScene(EditorSceneManager.GetSceneAt(sceneIndex), true);
                                }
                            }
                            else
                            {
#if WORLD_STREAMER_ENABLED
                                if(worldStreamers != null)
                                    EditorSceneManager.OpenScene(manageData.scenePath + WorldStreamerManageUtils.GetSceneName(x, z, manageData) + ".unity", OpenSceneMode.Additive);
                                else
#endif
                                    EditorSceneManager.OpenScene(/*manageData.scenePath + */WorldStreamerManageUtils.GetSceneName(x, z, manageData)/* + ".unity"*/, OpenSceneMode.Additive);
                            }
                        }
                        else
                        {
                            if (sceneLoaded)
                            {
                                SceneManager.UnloadSceneAsync(SceneManager.GetSceneAt(sceneIndex));
                            }
                            else
                            {
#if WORLD_STREAMER_ENABLED
                                if (worldStreamers != null)
                                    SceneManager.LoadScene(manageData.scenePath + WorldStreamerManageUtils.GetSceneName(x, z, manageData) + ".unity", LoadSceneMode.Additive);
                                else
#endif
                                SceneManager.LoadScene(/*manageData.scenePath + */WorldStreamerManageUtils.GetSceneName(x, z, manageData)/* + ".unity"*/, LoadSceneMode.Additive);
                            }
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Separator();
            EditorGUILayout.Separator();

            if (manageData.RegionDefinitions != null)
            {
                GUILayout.Label("Presets");

                string deleteRegion = null;
                foreach (WorldStreamManageData.RegionDefinition regionDef in manageData.RegionDefinitions)
                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button(new GUIContent(regionDef.Name, "")))
                    {
                        LoadUnloadRegionDef(regionDef);
                    }
                    if (GUILayout.Button(new GUIContent("X", ""), GUILayout.Width(20), GUILayout.Height(20)))
                    {
                        deleteRegion = regionDef.Name;
                    }
                    EditorGUILayout.EndHorizontal();
                }

                if (deleteRegion != null && EditorUtility.DisplayDialog("Confirm", "Are you sure you want to delete this preset?", "Ok", "Cancel"))
                {
                    manageData.RegionDefinitions = manageData.RegionDefinitions.Where(item => item.Name != deleteRegion).ToArray();
                    WorldStreamerManageUtils.SaveWorldStreamManageData(manageData);
                }
            }

            EditorGUILayout.Separator();
            EditorGUILayout.Separator();
            EditorGUILayout.Separator();

            EditorGUILayout.BeginHorizontal();
            newName = EditorGUILayout.TextField(newName);
            if (GUILayout.Button(new GUIContent("Save Current", "")))
            {
                WorldStreamManageData.RegionDefinition newDef = new WorldStreamManageData.RegionDefinition()
                {
                    Name = newName,
                    GridItems = GetLoadedSceneGrid().ToArray()
                };

                if (manageData.RegionDefinitions == null)
                {
                    manageData.RegionDefinitions = new WorldStreamManageData.RegionDefinition[] { newDef };
                }
                else
                {
                    manageData.RegionDefinitions = manageData.RegionDefinitions.Concat(new WorldStreamManageData.RegionDefinition[] { newDef }).ToArray();
                }
                WorldStreamerManageUtils.SaveWorldStreamManageData(manageData);
            }
            EditorGUILayout.EndHorizontal();
        }        
    }

    private IEnumerable<Vector2> GetLoadedSceneGrid()
    {
        for (int z = manageData.zLimitsy; z >= manageData.zLimitsx; z--)
        {
            for (int x = manageData.xLimitsx; x <= manageData.xLimitsy; x++)
            {
                int sceneIndex = WorldStreamerManageUtils.GetSceneIndex(x, z, manageData);
                if (sceneIndex != -1)
                {
                    yield return new Vector2(x, z);
                }
            }
        }
    }

    private void LoadUnloadAll(bool isLoad)
    {
        bool completeAction = true;
        if (!isLoad)
        {
            completeAction = EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        }

        if (completeAction)
        {
            for (int z = manageData.zLimitsx; z <= manageData.zLimitsy; z++)
            {
                for (int x = manageData.xLimitsx; x <= manageData.xLimitsy; x++)
                {
                    int sceneIndex = WorldStreamerManageUtils.GetSceneIndex(x, z, manageData);
                    bool sceneLoaded = sceneIndex != -1;

                    if (sceneLoaded && !isLoad)
                    {
                        if (!Application.isPlaying)
                        {
                            EditorSceneManager.CloseScene(EditorSceneManager.GetSceneAt(sceneIndex), true);
                        }
                        else
                        {
                            SceneManager.UnloadSceneAsync(SceneManager.GetSceneAt(sceneIndex));
                        }
                    }
                    else if (!sceneLoaded && isLoad)
                    {
                        try
                        {
                            if (!Application.isPlaying)
                            {
#if WORLD_STREAMER_ENABLED
                                if(worldStreamers != null)
                                    EditorSceneManager.OpenScene(manageData.scenePath + WorldStreamerManageUtils.GetSceneName(x, z, manageData)+ ".unity", OpenSceneMode.Additive);
                                else
#endif
                                    EditorSceneManager.OpenScene(/*manageData.scenePath + */WorldStreamerManageUtils.GetSceneName(x, z, manageData)/* + ".unity"*/, OpenSceneMode.Additive);
                            }
                            else
                            {
#if WORLD_STREAMER_ENABLED
                                if (worldStreamers != null)
                                    SceneManager.LoadScene(manageData.scenePath + WorldStreamerManageUtils.GetSceneName(x, z, manageData) + ".unity", LoadSceneMode.Additive);
                                else
#endif
                                    SceneManager.LoadScene(/*manageData.scenePath + */WorldStreamerManageUtils.GetSceneName(x, z, manageData)/* + ".unity"*/, LoadSceneMode.Additive);
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
        }
    }

    private void LoadUnloadRegionDef(WorldStreamManageData.RegionDefinition regionDef)
    {
        if (regionDef.GridItems.Length > 0)
        {
            bool isLoad = (WorldStreamerManageUtils.GetSceneIndex((int)regionDef.GridItems[0].x, (int)regionDef.GridItems[0].y, manageData) == -1);

            bool completeAction = true;
            if (!isLoad)
            {
                List<Scene> sceneObjs = new List<Scene>();
                foreach (Vector2 gridPos in regionDef.GridItems)
                {
                    int x = (int)gridPos.x;
                    int z = (int)gridPos.y;
                    int sceneIndex = WorldStreamerManageUtils.GetSceneIndex(x, z, manageData);
                    if(sceneIndex != -1)
                    {
                        Scene sceneObj = EditorSceneManager.GetSceneAt(sceneIndex);
                        if (sceneObj != null && sceneObj.isDirty)
                        {
                            sceneObjs.Add(sceneObj);
                        }
                    }
                }

                if (sceneObjs.Count > 0)
                {
                    completeAction = EditorSceneManager.SaveModifiedScenesIfUserWantsTo(sceneObjs.ToArray());
                }
            }

            if (completeAction)
            {                
                foreach (Vector2 gridPos in regionDef.GridItems)
                {
                    int x = (int)gridPos.x;
                    int z = (int)gridPos.y;
                    int sceneIndex = WorldStreamerManageUtils.GetSceneIndex(x, z, manageData);
                    bool sceneLoaded = sceneIndex != -1;
                    if (sceneLoaded && !isLoad)
                    {
                        if (!Application.isPlaying)
                        {
                            EditorSceneManager.CloseScene(EditorSceneManager.GetSceneAt(sceneIndex), true);
                        }
                        else
                        {
                            SceneManager.UnloadSceneAsync(SceneManager.GetSceneAt(sceneIndex));
                        }
                    }
                    else if (!sceneLoaded && isLoad)
                    {
                        try
                        {
                            if (!Application.isPlaying)
                            {
#if WORLD_STREAMER_ENABLED
                                if (worldStreamers != null)
                                    EditorSceneManager.OpenScene(manageData.scenePath + WorldStreamerManageUtils.GetSceneName(x, z, manageData) + ".unity", OpenSceneMode.Additive);
                                else
#endif
                                    EditorSceneManager.OpenScene(/*manageData.scenePath + */WorldStreamerManageUtils.GetSceneName(x, z, manageData)/* + ".unity"*/, OpenSceneMode.Additive);
                            }
                            else
                            {
#if WORLD_STREAMER_ENABLED
                                if (worldStreamers != null)
                                    SceneManager.LoadScene(manageData.scenePath + WorldStreamerManageUtils.GetSceneName(x, z, manageData) + ".unity", LoadSceneMode.Additive);
                                else
#endif
                                    SceneManager.LoadScene(/*manageData.scenePath + */WorldStreamerManageUtils.GetSceneName(x, z, manageData)/* + ".unity"*/, LoadSceneMode.Additive);
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
        }
    }

#if WORLD_STREAMER_ENABLED
    SceneCollection sceneCollectionRef;
    Streamer[] worldStreamers;
#endif

    string newName;

    static WorldStreamManageData manageData;

}
