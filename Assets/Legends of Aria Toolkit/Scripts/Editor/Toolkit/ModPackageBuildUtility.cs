using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using CoreUtil.ShardEngineMath;
using ShardsXML.TagDefinitions;
using ShardsXML.WorldData;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ModPackageBuildUtility
{
    public static bool BuildCurrentModPackage(out string packageRootPath, out string errorMessage)
    {
        packageRootPath = GetPackageRootPath();
        errorMessage = ValidateBuildSettings();
        if (!string.IsNullOrEmpty(errorMessage))
        {
            return false;
        }

        string tempBuildPath = GetTempBuildPath();
        SceneSetup[] originalScenes = EditorSceneManager.GetSceneManagerSetup();

        try
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                errorMessage = "Build cancelled.";
                return false;
            }

            ResetDirectory(tempBuildPath);
            ResetDirectory(packageRootPath);

            string sceneOutputPath = Path.Combine(packageRootPath, "Bundles", "Scenes");
            string objectOutputPath = Path.Combine(packageRootPath, "Bundles", "Objects");
            Directory.CreateDirectory(sceneOutputPath);
            Directory.CreateDirectory(objectOutputPath);

            // Client ids must be assigned before any scene is built. Building a scene writes each
            // placed object's ClientId into PermanentObjects.xml, so if ids were assigned afterwards
            // a newly added object would be recorded as client id 0 and only pick up its real id on
            // the next build.
            List<ClientObjectLibrary> libraries = new List<ClientObjectLibrary>();
            foreach (GameObject libraryObject in (ToolUtil.Settings.CustomObjectLibraries ?? new GameObject[0]).Where(item => item != null))
            {
                ClientObjectLibrary library = libraryObject.GetComponent<ClientObjectLibrary>();
                if (library == null)
                {
                    throw new Exception("Selected object library entry is missing a ClientObjectLibrary component.");
                }

                UpdateClientIdOnClientObjects(library);
                libraries.Add(library);
            }
            AssetDatabase.SaveAssets();

            List<ModPackageBundleManifest> sceneBundles = new List<ModPackageBundleManifest>();
            foreach (SceneAsset sceneAsset in (ToolUtil.Settings.ModScenes ?? new SceneAsset[0]).Where(item => item != null))
            {
                sceneBundles.Add(BuildSceneBundle(sceneAsset, tempBuildPath, sceneOutputPath));
            }

            List<ModPackageBundleManifest> objectBundles = new List<ModPackageBundleManifest>();
            foreach (ClientObjectLibrary library in libraries)
            {
                objectBundles.Add(BuildClientObjectLibrary(library, tempBuildPath, objectOutputPath));
            }

            WriteManifest(packageRootPath, sceneBundles, objectBundles);
            AssetDatabase.Refresh();
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }
        finally
        {
            if (originalScenes != null && originalScenes.Length > 0)
            {
                EditorSceneManager.RestoreSceneManagerSetup(originalScenes);
            }
        }
    }

    public static string GetPackageRootPath()
    {
        string rawPath;
        if (!string.IsNullOrEmpty(ToolUtil.Settings.WorkshopPackageOutputPath))
        {
            rawPath = ToolUtil.Settings.WorkshopPackageOutputPath;
        }
        else
        {
            rawPath = Path.Combine(ToolUtil.Settings.ModsPath, ToolUtil.Settings.CurrentModName, "workshop-package");
        }

        if (!Path.IsPathRooted(rawPath))
        {
            rawPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), rawPath);
        }

        return Path.GetFullPath(rawPath);
    }

    static string ValidateBuildSettings()
    {
        if (!ToolUtil.ValidateSettings())
        {
            return "Toolkit settings paths are invalid. Configure them from LoA Toolkit/Settings.";
        }

        if (!ToolUtil.ValidateModName())
        {
            return "Toolkit settings mod name is not set.";
        }

        bool hasScenes = ToolUtil.Settings.ModScenes != null && ToolUtil.Settings.ModScenes.Any(item => item != null);
        bool hasLibraries = ToolUtil.Settings.CustomObjectLibraries != null && ToolUtil.Settings.CustomObjectLibraries.Any(item => item != null);
        if (!hasScenes && !hasLibraries)
        {
            return "Assign at least one scene or one client object library to build a mod package.";
        }

        if (hasScenes)
        {
            string[] duplicateScenes = ToolUtil.Settings.ModScenes
                .Where(item => item != null)
                .GroupBy(item => item.name)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToArray();
            if (duplicateScenes.Any())
            {
                return "Duplicate scene entries are assigned to this mod package: " + string.Join(", ", duplicateScenes);
            }
        }

        if (hasLibraries)
        {
            string[] duplicateLibraries = ToolUtil.Settings.CustomObjectLibraries
                .Where(item => item != null && item.GetComponent<ClientObjectLibrary>() != null)
                .Select(item => item.GetComponent<ClientObjectLibrary>().BundleName)
                .Where(item => !string.IsNullOrEmpty(item))
                .GroupBy(item => item)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToArray();
            if (duplicateLibraries.Any())
            {
                return "Duplicate client object library bundle names are assigned to this mod package: " + string.Join(", ", duplicateLibraries);
            }
        }

        return null;
    }

    static ModPackageBundleManifest BuildSceneBundle(SceneAsset sceneAsset, string tempBuildPath, string outputPath)
    {
        string scenePath = AssetDatabase.GetAssetPath(sceneAsset);
        if (string.IsNullOrEmpty(scenePath))
        {
            throw new Exception("Unable to resolve scene path for " + sceneAsset.name);
        }

        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        MapData mapData = UnityEngine.Object.FindObjectOfType<MapData>();
        if (mapData == null)
        {
            throw new Exception("Scene '" + scene.name + "' is missing a MapData component.");
        }

        int bundleVersion = mapData.BundleVersion + 1;
        SetSerializedInt(mapData, "BundleVersion", bundleVersion);

        EditorSceneManager.SaveOpenScenes();
        ClientTools.GenerateIds(true, true);
        SaveMapExtents(mapData);

        List<string> scenesToLoad = new List<string>();
#if WORLD_STREAMER_ENABLED
        Streamer[] streamers = GameObject.FindObjectsOfType<Streamer>();
        if (streamers != null && streamers.Any())
        {
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
        }
#endif

        AssetBundleBuild build = new AssetBundleBuild();
        build.assetBundleName = scene.name;
        build.assetNames = new string[1 + scenesToLoad.Count];
        build.assetNames[0] = scene.path;
        for (int i = 0; i < scenesToLoad.Count; i++)
        {
            build.assetNames[i + 1] = scenesToLoad[i];
        }

        BuildAssetBundle(tempBuildPath, build);
        CopyBuiltBundle(tempBuildPath, scene.name, outputPath, bundleVersion);

        return new ModPackageBundleManifest()
        {
            Name = scene.name,
            RelativePath = NormalizeRelativePath(Path.Combine("Bundles", "Scenes", scene.name)),
            BundleVersion = bundleVersion
        };
    }

    static ModPackageBundleManifest BuildClientObjectLibrary(ClientObjectLibrary library, string tempBuildPath, string outputPath)
    {
        if (string.IsNullOrEmpty(library.BundleName))
        {
            throw new Exception("Client object library is missing a BundleName.");
        }

        int bundleVersion = library.BundleVersion + 1;
        SetSerializedInt(library, "BundleVersion", bundleVersion);

        UpdateClientIdOnClientObjects(library);
        GenerateObjectCollisionData(library);
        CreateObjectTagDefinitionsFile(library);

        HashSet<string> assets = new HashSet<string>(AssetDatabase.GetAssetPathsFromAssetBundle(library.BundleName));
        assets.Add(AssetDatabase.GetAssetPath(library));

        AssetBundleBuild build = new AssetBundleBuild();
        build.assetBundleName = library.BundleName;
        build.assetNames = assets.ToArray();

        BuildAssetBundle(tempBuildPath, build);
        CopyBuiltBundle(tempBuildPath, library.BundleName, outputPath, bundleVersion);

        return new ModPackageBundleManifest()
        {
            Name = library.BundleName,
            RelativePath = NormalizeRelativePath(Path.Combine("Bundles", "Objects", library.BundleName)),
            BundleVersion = bundleVersion
        };
    }

    static void BuildAssetBundle(string outputPath, AssetBundleBuild build)
    {
        Directory.CreateDirectory(outputPath);
        BuildPipeline.BuildAssetBundles(outputPath, new[] { build }, BuildAssetBundleOptions.None, BuildTarget.StandaloneWindows);
    }

    static void CopyBuiltBundle(string tempBuildPath, string bundleName, string outputPath, int bundleVersion)
    {
        string sourceBundlePath = Path.Combine(tempBuildPath, bundleName);
        if (!File.Exists(sourceBundlePath))
        {
            throw new Exception("Expected built asset bundle was not found: " + sourceBundlePath);
        }

        string targetBundlePath = Path.Combine(outputPath, bundleName);
        Directory.CreateDirectory(outputPath);
        File.Copy(sourceBundlePath, targetBundlePath, true);

        // Web-hosted bundles need a '.version' sidecar: the client fetches it, parses it as an
        // int, and passes that to WWW.LoadFromCacheOrDownload as the cache key. Without the file
        // the client reports a load error, and without the number changing it keeps serving its
        // cached copy. Steam Workshop packages ignore this file.
        File.WriteAllText(targetBundlePath + ".version", bundleVersion.ToString());
    }

    static void WriteManifest(string packageRootPath, List<ModPackageBundleManifest> sceneBundles, List<ModPackageBundleManifest> objectBundles)
    {
        ModPackageManifest manifest = new ModPackageManifest()
        {
            ModName = ToolUtil.Settings.CurrentModName,
            PackageNamespace = ToolUtil.Settings.CurrentModName,
            BuildVersion = DateTime.UtcNow.ToString("o"),
            SceneBundles = sceneBundles.ToArray(),
            ClientObjectBundles = objectBundles.ToArray()
        };

        string manifestPath = Path.Combine(packageRootPath, "mod-manifest.json");
        File.WriteAllText(manifestPath, JsonUtility.ToJson(manifest, true));
    }

    static void SaveMapExtents(MapData mapData)
    {
        string worldDataFileName = ShardsExtensionMethods.CombinePaths(ToolUtil.Settings.ModsPath, ToolUtil.Settings.CurrentModName, "mapdata", ToolUtil.currentScene, "WorldData.xml");
        WorldData worldData;
        XmlSerializer serializer = new XmlSerializer(typeof(WorldData));
        if (File.Exists(worldDataFileName))
        {
            using (XmlReader reader = XmlReader.Create(worldDataFileName))
            {
                worldData = serializer.Deserialize(reader) as WorldData;
            }
        }
        else
        {
            worldData = new WorldData();
            worldData.Items = new object[0];
        }

        WorldDataMapDefinition mapDefinition = new WorldDataMapDefinition()
        {
            ExtentsX = ((int)mapData.MapExtents.x).ToString(),
            ExtentsZ = ((int)mapData.MapExtents.y).ToString()
        };

        if (worldData.Items.Length > 0)
        {
            for (int i = 0; i < worldData.Items.Length; i++)
            {
                if (worldData.Items[i] is WorldDataMapDefinition)
                {
                    worldData.Items[i] = mapDefinition;
                }
            }
        }
        else
        {
            worldData.Items = worldData.Items.Concat(new object[] { mapDefinition }).ToArray();
        }

        ToolUtil.WriteXML(serializer, worldDataFileName, worldData);
    }

    static void UpdateClientIdOnClientObjects(ClientObjectLibrary library)
    {
        for (int i = 1; i < library.ClientIdPrefabs.Length; i++)
        {
            GameObject objectPrefab = library.ClientIdPrefabs[i];
            if (objectPrefab == null)
            {
                continue;
            }

            ClientObject clientObject = objectPrefab.GetComponent<ClientObject>();
            if (clientObject == null)
            {
                throw new Exception("Client object library '" + library.BundleName + "' contains a prefab without a ClientObject component at index " + i + ".");
            }

            if (clientObject.ClientId != i || clientObject.CustomObjectLibrary != library.BundleName)
            {
                SerializedObject serializedObject = new SerializedObject(clientObject);
                serializedObject.FindProperty("ClientId").intValue = i;
                serializedObject.FindProperty("CustomObjectLibrary").stringValue = library.BundleName;
                serializedObject.ApplyModifiedProperties();
            }
        }
    }

    static void CreateObjectTagDefinitionsFile(ClientObjectLibrary library)
    {
        string bundlePath = GetServerBundlePath(library.BundleName);
        string filePath = Path.Combine(bundlePath, "ObjectTagDefinitions.xml");
        if (!File.Exists(filePath))
        {
            ObjectTags tags = new ObjectTags();
            tags.Items = new ObjectTagsObjectTag[0];
            ToolUtil.WriteXML(new XmlSerializer(typeof(ObjectTags)), filePath, tags);
        }
    }

    static void GenerateObjectCollisionData(ClientObjectLibrary library)
    {
        XmlDocument xmlDoc = new XmlDocument();
        XmlNode xmlNode = xmlDoc.CreateNode(XmlNodeType.XmlDeclaration, "", "");
        xmlDoc.AppendChild(xmlNode);
        XmlElement rootElement = xmlDoc.CreateElement("", "ObjectTags", "");
        xmlDoc.AppendChild(rootElement);

        for (int i = 1; i < library.ClientIdPrefabs.Length; i++)
        {
            GameObject objectPrefab = library.ClientIdPrefabs[i];
            if (objectPrefab == null)
            {
                continue;
            }

            ClientObject newObject = (UnityEngine.Object.Instantiate(objectPrefab, Vector3.zero, new Quaternion()) as GameObject).GetComponent<ClientObject>();
            List<Rect3> allCollisionBounds = CalculateCollisionBounds(newObject);

            List<Transform> allObjectBoundsTrans = new List<Transform>();
            foreach (Transform child in newObject.transform)
            {
                if (child.name == "ObjectBounds")
                {
                    allObjectBoundsTrans.Add(child);
                }
            }

            List<Transform> allRoofBoundsTrans = new List<Transform>();
            foreach (Transform child in newObject.transform)
            {
                if (child.name == "RoofBounds")
                {
                    allRoofBoundsTrans.Add(child);
                }
            }

            if (allCollisionBounds.Any() || allObjectBoundsTrans.Any() || allRoofBoundsTrans.Any())
            {
                XmlElement objectTagElement = xmlDoc.CreateElement("", "ObjectTag", "");

                XmlElement clientIdElement = xmlDoc.CreateElement("", "ClientId", "");
                clientIdElement.AppendChild(xmlDoc.CreateTextNode(i.ToString()));
                objectTagElement.AppendChild(clientIdElement);

                foreach (Rect3 bound in allCollisionBounds)
                {
                    XmlElement collisionElement = xmlDoc.CreateElement("", "CollisionBounds", "");
                    collisionElement.AppendChild(xmlDoc.CreateTextNode(ToolUtil.ConvertBoundsToTagString(bound)));
                    objectTagElement.AppendChild(collisionElement);
                }

                foreach (Transform objectBoundsTrans in allObjectBoundsTrans)
                {
                    BoxCollider boundsCollider = objectBoundsTrans.GetComponent<Collider>() as BoxCollider;
                    Vector3[] colliderVerts = UnityUtil.GetColliderLocalVertexPositions(boundsCollider.transform, new Bounds(boundsCollider.center, boundsCollider.size));
                    Rect3 boundsRect = new Rect3(colliderVerts.Select(item => UnityUtil.CreateFromVector3(item)).ToArray());
                    XmlElement boundsElement = xmlDoc.CreateElement("", "ObjectBounds", "");
                    boundsElement.AppendChild(xmlDoc.CreateTextNode(ToolUtil.ConvertBoundsToTagString(boundsRect)));
                    objectTagElement.AppendChild(boundsElement);
                }

                foreach (Transform roofBoundsTrans in allRoofBoundsTrans)
                {
                    BoxCollider boundsCollider = roofBoundsTrans.GetComponent<Collider>() as BoxCollider;
                    Vector3[] colliderVerts = UnityUtil.GetColliderLocalVertexPositions(boundsCollider.transform, new Bounds(boundsCollider.center, boundsCollider.size));
                    Rect3 boundsRect = new Rect3(colliderVerts.Select(item => UnityUtil.CreateFromVector3(item)).ToArray());
                    XmlElement boundsElement = xmlDoc.CreateElement("", "RoofBounds", "");
                    boundsElement.AppendChild(xmlDoc.CreateTextNode(ToolUtil.ConvertBoundsToTagString(boundsRect)));
                    objectTagElement.AppendChild(boundsElement);
                }

                rootElement.AppendChild(objectTagElement);
            }

            UnityEngine.Object.DestroyImmediate(newObject.gameObject);
        }

        string xmlFilePath = GetServerBundlePath(library.BundleName);
        string xmlFileName = ShardsExtensionMethods.CombinePaths(xmlFilePath, "ObjectCollisionData.xml");
        xmlDoc.Save(xmlFileName);
    }

    static List<Rect3> CalculateCollisionBounds(ClientObject target)
    {
        List<Rect3> bounds = new List<Rect3>();
        foreach (BoxCollider child in target.GetComponentsInChildren<BoxCollider>().Where(item => item.name == "Collider"))
        {
            Vector3[] colliderVerts = UnityUtil.GetColliderLocalVertexPositions(child.transform, new Bounds(child.center, child.size));
            bounds.Add(new Rect3(colliderVerts.Select(item => UnityUtil.CreateFromVector3(item)).ToArray()));
        }

        return bounds;
    }

    static string GetServerBundlePath(string bundleName)
    {
        string bundlePath = ShardsExtensionMethods.CombinePaths(ToolUtil.Settings.ModsPath, ToolUtil.Settings.CurrentModName, "assetbundles", bundleName);
        Directory.CreateDirectory(bundlePath);
        return bundlePath;
    }

    static void SetSerializedInt(UnityEngine.Object target, string propertyName, int value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        serializedObject.FindProperty(propertyName).intValue = value;
        serializedObject.ApplyModifiedProperties();
    }

    static void ResetDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, true);
        }

        Directory.CreateDirectory(path);
    }

    static string NormalizeRelativePath(string path)
    {
        return path.Replace('\\', '/');
    }

    static string GetTempBuildPath()
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "LoAToolkitBuild"));
    }

#if WORLD_STREAMER_ENABLED
    static string GetSceneName(int x, int z, SceneCollection sceneCollectionRef)
    {
        if (sceneCollectionRef != null)
        {
            return sceneCollectionRef.prefixScene + "_x" + x + "_z" + z;
        }

        return null;
    }
#endif
}
