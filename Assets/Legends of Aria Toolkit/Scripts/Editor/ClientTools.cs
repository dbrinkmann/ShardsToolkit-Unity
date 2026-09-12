using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.IO;
using System.Text;
using CoreUtil;
using CoreUtil.ShardEngineMath;
using UnityEngine.SceneManagement;

public class ClientTools : ScriptableObject
{    
    // collision data for static objects on the map
    public static void GenerateStaticMapObjectData(int nextPermId)
    {
        Debug.Log("Generating Static Map Collision Data...");

        ToolUtil.TagTerrains();

        // make sure the dynamics are disabled
        GameObject seedObjectRoot = GameObject.Find("SeedObjects");
        if (seedObjectRoot)
        {
            seedObjectRoot.SetActive(false);
        }

        GeneratePermanentIds(nextPermId);
    }    

    #region Private Methods

    private static ShardsToolsData LoadToolsData()
    {
        ShardsToolsData toolsData = (ShardsToolsData)AssetDatabase.LoadAssetAtPath(@"Assets\Legends of Aria Toolkit\Scripts\Editor\Toolkit\ToolsData.asset", typeof(ShardsToolsData));
        if (toolsData == null)
        {
            toolsData = ScriptableObject.CreateInstance<ShardsToolsData>();
        }

        return toolsData;
    }

    private static void SaveToolsData(ShardsToolsData toolData)
    {
        if (!AssetDatabase.Contains(toolData))
        {
            AssetDatabase.CreateAsset(toolData, @"Assets\Legends of Aria Toolkit\Scripts\Editor\Toolkit\ToolsData.asset");
        }
        EditorUtility.SetDirty(toolData);
        AssetDatabase.SaveAssets();
    }

    static private int ProcessLoadedPermanents(int _startId, List<string> permStrings, HashSet<string> missingPrefabs,HashSet<int> processedObjects)
    {
        int nextId = _startId;
        IEnumerable<ClientObject> staticObjects = FindObjectsOfType(typeof(ClientObject))
            .Select(obj => obj as ClientObject)
            .Where(obj => obj.IsPermanent)
            .Where(obj => !processedObjects.Contains(obj.gameObject.GetInstanceID()));

        if (staticObjects.Any())
        {
            foreach (var curObj in staticObjects)
            {
                processedObjects.Add(curObj.GetInstanceID());

                GameObject prefab = PrefabUtility.GetCorrespondingObjectFromSource(curObj.gameObject) as GameObject;

                if (prefab == null)
                {
                    missingPrefabs.Add(curObj.name);
                }
                else
                {
                    string assetPath = AssetDatabase.GetAssetPath(prefab);

                    assetPath = assetPath.Replace("Assets/Resources/", "");
                    assetPath = assetPath.Replace(Path.GetExtension(assetPath), "");
                   
                    ClientObject prefabComp = prefab.GetComponent<ClientObject>();
                    if (prefabComp == null)
                    {
                        Debug.Log("Prefab has no ClientObject script! " + curObj.name);
                        continue;
                    }
                    if (prefabComp.PermanentId != 0)
                    {
                        Debug.Log("ClientObject Prefab has permanent id! Clearing - Name: " + prefabComp.name);
                        if (curObj.PermanentId == prefabComp.PermanentId)
                        {
                            curObj.PermanentId = 0;
                        }
                        prefabComp.PermanentId = 0;
                        EditorUtility.SetDirty(prefab);
                    }

                    SerializedObject so = new SerializedObject(curObj);
                    so.FindProperty("PermanentId").intValue = nextId++;
                    so.ApplyModifiedProperties();

                    string createStr = GenerateStaticCreateString(curObj);
                    permStrings.Add(createStr);
                }
            }
        }

        return nextId;
    }

#if WORLD_STREAMER_ENABLED
    private static string GetSceneName(int x, int z, SceneCollection sceneCollectionRef)
    {
        if (sceneCollectionRef != null)
        {
            return sceneCollectionRef.prefixScene + "_x" + x + "_z" + z;
        }

        return null;
    }
#endif

    static private void GeneratePermanentIds(int currentId = 1)
    {
        int curPermId = currentId;

        MapData mapDataObj = GameObject.FindObjectOfType<MapData>();
        if (mapDataObj == null)
        {
            GameObject mapDataGameObj = new GameObject();
            mapDataGameObj.name = "MapData";
            mapDataObj = mapDataGameObj.AddComponent<MapData>();
        }

        List<string> permStrings = new List<string>();
        HashSet<string> missingPrefabs = new HashSet<string>();
        HashSet<int> processedObjects = new HashSet<int>();

        // first do the loaded scene
        EditorUtility.DisplayProgressBar("Updating permanents...", "", 0.0f);
        curPermId = ProcessLoadedPermanents(curPermId, permStrings, missingPrefabs, processedObjects);

#if WORLD_STREAMER_ENABLED
        // now lets see if this is a streamed scene
        Streamer[] streamers = GameObject.FindObjectsOfType<Streamer>();
        if (streamers != null && streamers.Any())
        {
            List<string> scenesToLoad = new List<string>();

            foreach (var streamer in streamers)
            {
                SceneCollection sceneCollectionRef = streamer.sceneCollection;
                for (int z = sceneCollectionRef.zLimitsx; z <= sceneCollectionRef.zLimitsy; z++)
                {
                    for (int x = sceneCollectionRef.xLimitsx; x <= sceneCollectionRef.xLimitsy; x++)
                    {
                        scenesToLoad.Add(sceneCollectionRef.path + GetSceneName(x, z, sceneCollectionRef) + ".unity");
                    }
                }
            }

            int index = 0;
            foreach (string scene in scenesToLoad)
            {
                try
                {
                    Scene curScene = EditorSceneManager.OpenScene(scene, OpenSceneMode.Additive);
                    curPermId = ProcessLoadedPermanents(curPermId, permStrings, missingPrefabs, processedObjects);
                    EditorUtility.DisplayProgressBar("Updating permanents...", "", ((float)index) / ((float)scenesToLoad.Count));
                    EditorSceneManager.SaveScene(curScene);
                    EditorSceneManager.UnloadScene(curScene);
                    index++;
                }
                catch (ArgumentException e)
                {
                    Debug.LogException(e);
                }
            }
        }
#endif

        foreach (string missingPrefab in missingPrefabs.OrderBy(item => item))
        {
            Debug.Log("ClientObject with no Prefab! Name: " + missingPrefab);
        }
       
        if (permStrings.Any())
        {
            EditorUtility.DisplayProgressBar("Saving...", "", 1.0f);
            XmlDocument xmlDoc = new XmlDocument();
            XmlNode xmlNode = xmlDoc.CreateNode(XmlNodeType.XmlDeclaration, "", "");
            xmlDoc.AppendChild(xmlNode);
            XmlElement rootElement = xmlDoc.CreateElement("", "WorldTemplateObjects", "");
            xmlDoc.AppendChild(rootElement);
            foreach (string permString in permStrings)
            {
                XmlElement objectRootElement = xmlDoc.CreateElement("", "StaticObject", "");

                XmlElement objectCreateElement = xmlDoc.CreateElement("", "ObjectCreationParams", "");
                objectCreateElement.AppendChild(xmlDoc.CreateTextNode(permString));
                objectRootElement.AppendChild(objectCreateElement);

                rootElement.AppendChild(objectRootElement);
            }
            Debug.Log("Update permanent ids for " + permStrings.Count + " objects.");

            string staticFilePath = ShardsExtensionMethods.CombinePaths(ToolUtil.Settings.ModsPath, ToolUtil.Settings.CurrentModName, "mapdata", EditorSceneManager.GetActiveScene().name);
            Directory.CreateDirectory(staticFilePath);
            string staticFileName = ShardsExtensionMethods.CombinePaths(staticFilePath, "PermanentObjects.xml");
            xmlDoc.Save(staticFileName);

            // DAB ADD BUNDLE NAME!        
            string scenePath = Path.GetDirectoryName(EditorSceneManager.GetActiveScene().path);
            string clientFileName = ShardsExtensionMethods.CombinePaths(scenePath, EditorSceneManager.GetActiveScene().name + "PermanentObjects.xml");
            xmlDoc.Save(clientFileName);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(clientFileName);

            SerializedObject mapDataSO = new SerializedObject(mapDataObj);
            mapDataSO.FindProperty("PermanentObjectsData").objectReferenceValue = textAsset;
            mapDataSO.ApplyModifiedProperties();

            EditorUtility.DisplayDialog("Build Results", "Commited " + permStrings.Count + " objects to " + staticFileName + " (Client: " + clientFileName + ")", "Ok");
        }
        else
        {
            Debug.Log("Update permanents failed. No permanents found.");
        }

        EditorUtility.ClearProgressBar();
    }

    public static void GenerateIds(bool reset, bool forceSave)
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog("ERROR", "You can not assign/reset permanent ids while the game is running.", "Okay");
            return;
        }

        /*
        if (reset)
        {
            if (!EditorUtility.DisplayDialog("WARNING", "This will reset all permanent object ids. This requires a permanent object reset on the server which is not yet supported. Until then this requires a map backup wipe!", "Confirm", "Cancel"))
            {
                return;
            }
        }

        int nextAvailableId = 1;
        ShardsToolsData toolsData = LoadToolsData();
        ShardsToolsData.MapEntry mapDataEntry = toolsData.MapData.FirstOrDefault(item => item.SceneName == ToolUtil.currentScene);
        if (mapDataEntry == null)
        {
            if (!reset)
            {
                ClientObject highestPerm = FindObjectsOfType(typeof(ClientObject))
                    .Select(obj => obj as ClientObject)
                    .Where(obj => obj.IsPermanent)
                    .OrderByDescending(obj => obj.PermanentId)
                    .FirstOrDefault();

                if (highestPerm != null)
                {
                    nextAvailableId = highestPerm.PermanentId + 1;
                }
#if WORLD_STREAMER_ENABLED
                //check if this is a worldstreamer map
                Streamer[] streamers = GameObject.FindObjectsOfType<Streamer>();
                if (streamers != null && streamers.Any())
                {
                    List<string> scenesToLoad = new List<string>();

                    foreach (var streamer in streamers)
                    {
                        SceneCollection sceneCollectionRef = streamer.sceneCollection;
                        for (int z = sceneCollectionRef.zLimitsx; z <= sceneCollectionRef.zLimitsy; z++)
                        {
                            for (int x = sceneCollectionRef.xLimitsx; x <= sceneCollectionRef.xLimitsy; x++)
                            {
                                scenesToLoad.Add(sceneCollectionRef.path + GetSceneName(x, z, sceneCollectionRef) + ".unity");
                            }
                        }
                    }

                    int index = 0;
                    foreach (string scene in scenesToLoad)
                    {
                        try
                        {
                            Scene curScene = EditorSceneManager.OpenScene(scene, OpenSceneMode.Additive);

                            highestPerm = FindObjectsOfType(typeof(ClientObject))
                            .Select(obj => obj as ClientObject)
                            .Where(obj => obj.IsPermanent)
                            .OrderByDescending(obj => obj.PermanentId)
                            .FirstOrDefault();

                            if (highestPerm != null)
                            {
                                if (highestPerm.PermanentId > nextAvailableId)
                                    nextAvailableId = highestPerm.PermanentId + 1;
                            }

                            EditorSceneManager.SaveScene(curScene);
                            EditorSceneManager.UnloadScene(curScene);
                            index++;
                        }
                        catch (ArgumentException e)
                        {
                            Debug.LogException(e);
                        }
                    }
                }
#endif

                if (nextAvailableId == 1 && !EditorUtility.DisplayDialog("ERROR", "No permanent id data was found for this scene. Do you want to start at the next highest Id (" + nextAvailableId + ") ? To start at 1 use reset.", "Okay", "Cancel"))
                {
                    Debug.Log("Update Permanent Ids Cancelled");
                    return;
                }
            }
            mapDataEntry = new ShardsToolsData.MapEntry() { SceneName = ToolUtil.currentScene, NextAvailablePermanentId = nextAvailableId };
            toolsData.MapData.Add(mapDataEntry);
        }
        */
        GenerateStaticMapObjectData(1);

        //SaveToolsData(toolsData);
    }

    private static string GenerateStaticCreateString(ClientObject _obj)
    {
        //Debug.Log("CreateString Y for obj: " + _obj.gameObject.name + " at: " + _obj.transform.position + " surfaceY: " + UnityUtil.GetDynamicSurfaceY(_obj.transform.position));

        Transform curObj = _obj.transform;
        Vec3 position = UnityUtil.ConvertToServerPos(curObj.position);
        Vector3 scale = curObj.localScale;
        Vector3 rotation = curObj.localRotation.eulerAngles;

        string createString = _obj.ClientId.ToString() + " " + _obj.PermanentId + " " +
                                +ToolUtil.LimitPrecision(position.X) + " " + ToolUtil.LimitPrecision(position.Y) + " " + ToolUtil.LimitPrecision(position.Z) + " "
                                + ToolUtil.LimitPrecision(rotation.x) + " " + ToolUtil.LimitPrecision(rotation.y) + " " + ToolUtil.LimitPrecision(rotation.z) + " "
                                + ToolUtil.LimitPrecision(scale.x) + " " + ToolUtil.LimitPrecision(scale.y) + " " + ToolUtil.LimitPrecision(scale.z);

        if (_obj.CustomObjectLibrary != null && _obj.CustomObjectLibrary != "")
        {
            createString += " " + _obj.CustomObjectLibrary;
        }
        return createString;
    }

    #endregion
}
