using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class TerrainTreePromoter : EditorWindow
{
    private const string c_LastPromotionBackupKey = "TerrainTreePromoter.LastPromotionBackup";
    private GameObject customObjectLibraryObject;

    [MenuItem("LoA Toolkit/Utils/Promote Terrain Trees", false, 30)]
    public static void ShowWindow()
    {
        TerrainTreePromoter window = GetWindow<TerrainTreePromoter>();
        window.titleContent = new GUIContent("Terrain Tree Promoter");
        window.minSize = new Vector2(440f, 220f);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Promote Terrain Trees", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Converts painted terrain trees into prefab instances, swaps each terrain to a duplicated TerrainData asset, " +
            "and registers the unique prefabs into the selected custom object library.",
            MessageType.Info);

        customObjectLibraryObject = EditorGUILayout.ObjectField(
            "Custom Object Library",
            customObjectLibraryObject,
            typeof(GameObject),
            false) as GameObject;

        ClientObjectLibrary customObjectLibrary = GetSelectedLibrary();
        if (customObjectLibrary == null)
        {
            EditorGUILayout.HelpBox(
                "Drag in the custom object library prefab or a GameObject with a ClientObjectLibrary component.",
                MessageType.Warning);
        }
        else
        {
            EditorGUILayout.LabelField("Bundle Name", customObjectLibrary.BundleName);
            EditorGUILayout.LabelField("Current Library Size", customObjectLibrary.ClientIdPrefabs != null
                ? customObjectLibrary.ClientIdPrefabs.Length.ToString()
                : "0");
        }

        EditorGUILayout.Space();

        PromotionBackup backup = LoadBackup();
        if (backup != null && backup.Entries.Count > 0)
        {
            EditorGUILayout.HelpBox(
                "A previous promotion backup is available for " + backup.Entries.Count + " terrain(s).",
                MessageType.Info);

            EditorGUI.BeginDisabledGroup(!CanRestoreBackupForCurrentScene(backup));
            if (GUILayout.Button("Restore Last Promotion"))
            {
                RestoreLastPromotion();
            }
            EditorGUI.EndDisabledGroup();

            if (!CanRestoreBackupForCurrentScene(backup))
            {
                EditorGUILayout.HelpBox(
                    "The backup was created for a different scene. Open that scene to restore it.",
                    MessageType.Warning);
            }
        }

        EditorGUI.BeginDisabledGroup(customObjectLibrary == null);
        if (GUILayout.Button("Promote All Terrain Trees"))
        {
            PromoteAllTerrainTrees(customObjectLibrary);
        }
        EditorGUI.EndDisabledGroup();
    }

    private ClientObjectLibrary GetSelectedLibrary()
    {
        if (customObjectLibraryObject == null)
            return null;

        return customObjectLibraryObject.GetComponent<ClientObjectLibrary>();
    }

    public static void PromoteAllTerrainTrees(ClientObjectLibrary customObjectLibrary)
    {
        if (customObjectLibrary == null)
        {
            EditorUtility.DisplayDialog("Promote Terrain Trees", "Select a custom object library first.", "Ok");
            return;
        }

        Terrain[] terrains = UnityEngine.Object.FindObjectsOfType<Terrain>();
        if (terrains.Length == 0)
        {
            EditorUtility.DisplayDialog("Promote Terrain Trees", "No terrains found in the scene.", "Ok");
            return;
        }

        int totalTrees = 0;
        int terrainsWithTrees = 0;
        foreach (Terrain terrain in terrains)
        {
            if (terrain.terrainData == null)
                continue;

            int treeCount = terrain.terrainData.treeInstances.Length;
            totalTrees += treeCount;
            if (treeCount > 0)
                terrainsWithTrees++;
        }

        if (totalTrees == 0)
        {
            EditorUtility.DisplayDialog("Promote Terrain Trees", "No painted trees found on any terrain.", "Ok");
            return;
        }

        if (!EditorUtility.DisplayDialog(
            "Promote Terrain Trees",
            "This will convert " + totalTrees + " painted tree(s) across " + terrainsWithTrees +
            " terrain(s) into prefab instances, duplicate and reassign each terrain's TerrainData, remove the trees from the duplicated data, and register their prefabs in '" +
            customObjectLibrary.BundleName + "'.\n\nUse Restore Last Promotion in this window to revert the terrain swap.",
            "Promote",
            "Cancel"))
        {
            return;
        }

        HashSet<GameObject> sourcePrefabs = CollectSourcePrefabs(terrains);
        LibraryRegistrationResult registrationResult = RegisterPrefabsInLibrary(customObjectLibrary, sourcePrefabs);

        PromotionBackup backup = new PromotionBackup();
        backup.ScenePath = EditorSceneManager.GetActiveScene().path;
        backup.SceneName = EditorSceneManager.GetActiveScene().name;

        Undo.SetCurrentGroupName("Promote Terrain Trees");
        int undoGroup = Undo.GetCurrentGroup();
        int promoted = 0;
        int skipped = 0;
        int duplicatedTerrainDataCount = 0;
        bool wasCancelled = false;
        try
        {
            foreach (Terrain terrain in terrains)
            {
                if (terrain.terrainData == null || terrain.terrainData.treeInstances.Length == 0)
                    continue;

                TerrainData originalData = terrain.terrainData;
                TerrainData promotedData;
                string promotedDataPath;
                if (!TryCloneTerrainData(terrain, originalData, out promotedData, out promotedDataPath))
                {
                    Debug.LogWarning("[TerrainTreePromoter] Failed to clone TerrainData for terrain '" + terrain.name + "'.");
                    skipped += originalData.treeInstances.Length;
                    continue;
                }

                duplicatedTerrainDataCount++;
                backup.Entries.Add(new PromotionBackupEntry
                {
                    TerrainPath = GetHierarchyPath(terrain.transform),
                    OriginalTerrainDataPath = AssetDatabase.GetAssetPath(originalData),
                    PromotedTerrainDataPath = promotedDataPath,
                    ContainerName = GetContainerName(terrain)
                });
                SaveBackup(backup);

                Undo.RecordObject(terrain, "Assign promoted TerrainData");
                terrain.terrainData = promotedData;
                EditorUtility.SetDirty(terrain);

                TreeInstance[] instances = promotedData.treeInstances;
                TreePrototype[] prototypes = promotedData.treePrototypes;

                GameObject container = new GameObject(GetContainerName(terrain));
                Undo.RegisterCreatedObjectUndo(container, "Create tree container");

                for (int i = 0; i < instances.Length; i++)
                {
                    if (i % 50 == 0)
                    {
                        float progress = (float)(promoted + skipped) / totalTrees;
                        if (EditorUtility.DisplayCancelableProgressBar(
                            "Promoting Terrain Trees",
                            "Processing " + terrain.name + " (" + (i + 1) + "/" + instances.Length + ")",
                            progress))
                        {
                            wasCancelled = true;
                            break;
                        }
                    }

                    TreeInstance instance = instances[i];
                    if (instance.prototypeIndex < 0 || instance.prototypeIndex >= prototypes.Length)
                    {
                        Debug.LogWarning("[TerrainTreePromoter] Tree instance with invalid prototypeIndex " +
                            instance.prototypeIndex + " on terrain '" + terrain.name + "', skipping.");
                        skipped++;
                        continue;
                    }

                    GameObject sourcePrefab = prototypes[instance.prototypeIndex].prefab;
                    if (sourcePrefab == null)
                    {
                        Debug.LogWarning("[TerrainTreePromoter] Tree prototype " + instance.prototypeIndex +
                            " on terrain '" + terrain.name + "' has a null prefab reference, skipping.");
                        skipped++;
                        continue;
                    }

                    GameObject prefab;
                    if (!registrationResult.RegisteredPrefabs.TryGetValue(sourcePrefab, out prefab) || prefab == null)
                    {
                        Debug.LogWarning("[TerrainTreePromoter] No generated library prefab found for source prefab '" +
                            sourcePrefab.name + "', skipping.");
                        skipped++;
                        continue;
                    }

                    GameObject treeObject = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                    if (treeObject == null)
                    {
                        Debug.LogWarning("[TerrainTreePromoter] Failed to instantiate prefab '" + prefab.name + "', skipping.");
                        skipped++;
                        continue;
                    }

                    Undo.RegisterCreatedObjectUndo(treeObject, "Promote tree");
                    treeObject.transform.position = terrain.transform.position + Vector3.Scale(instance.position, promotedData.size);
                    treeObject.transform.rotation = Quaternion.Euler(0f, instance.rotation * Mathf.Rad2Deg, 0f);

                    Vector3 treeScale = treeObject.transform.localScale;
                    treeScale.x *= instance.widthScale;
                    treeScale.y *= instance.heightScale;
                    treeScale.z *= instance.widthScale;
                    treeObject.transform.localScale = treeScale;
                    treeObject.transform.SetParent(container.transform, true);

                    promoted++;
                }

                promotedData.treeInstances = new TreeInstance[0];
                terrain.Flush();
                EditorUtility.SetDirty(promotedData);

                if (wasCancelled)
                    break;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
        }
        Undo.CollapseUndoOperations(undoGroup);

        string message = "Promoted " + promoted + " tree(s) to prefab instances.";
        message += "\nDuplicated TerrainData assets: " + duplicatedTerrainDataCount + ".";
        if (skipped > 0)
            message += " Skipped " + skipped + " (see console for details).";
        message += "\nRegistered " + registrationResult.AddedToLibraryCount + " prefab(s) in the custom object library.";
        if (registrationResult.ClientObjectsAddedCount > 0)
            message += "\nAdded ClientObject to " + registrationResult.ClientObjectsAddedCount + " prefab(s).";
        if (registrationResult.GeneratedPrefabCount > 0)
            message += "\nGenerated " + registrationResult.GeneratedPrefabCount + " library prefab copy/copies.";
        if (registrationResult.CollisionBoundsAddedCount > 0 || registrationResult.ObjectBoundsAddedCount > 0)
        {
            message += "\nAdded default bounds helpers:";
            message += " Collider=" + registrationResult.CollisionBoundsAddedCount;
            message += ", ObjectBounds=" + registrationResult.ObjectBoundsAddedCount + ".";
        }
        if (wasCancelled)
            message += "\nPromotion was cancelled partway through. Use Restore Last Promotion to revert the partial changes.";
        else
            message += "\nUse Restore Last Promotion to restore the original TerrainData references.";

        Debug.Log("[TerrainTreePromoter] " + message);
        LogPrefabSummary(registrationResult.RegisteredPrefabs, customObjectLibrary);
        EditorUtility.DisplayDialog("Promote Terrain Trees", message, "Ok");
    }

    private static void RestoreLastPromotion()
    {
        PromotionBackup backup = LoadBackup();
        if (backup == null || backup.Entries.Count == 0)
        {
            EditorUtility.DisplayDialog("Restore Terrain Promotion", "No promotion backup is available.", "Ok");
            return;
        }

        if (!CanRestoreBackupForCurrentScene(backup))
        {
            EditorUtility.DisplayDialog(
                "Restore Terrain Promotion",
                "The last backup belongs to scene '" + backup.SceneName + "'. Open that scene to restore it.",
                "Ok");
            return;
        }

        if (!EditorUtility.DisplayDialog(
            "Restore Terrain Promotion",
            "This will restore the original TerrainData references and remove the promoted tree containers for this scene.",
            "Restore",
            "Cancel"))
        {
            return;
        }

        int restored = 0;
        int containersRemoved = 0;
        int terrainAssetsDeleted = 0;

        foreach (PromotionBackupEntry entry in backup.Entries)
        {
            Terrain terrain = FindTerrainByPath(entry.TerrainPath);
            TerrainData originalData = AssetDatabase.LoadAssetAtPath<TerrainData>(entry.OriginalTerrainDataPath);

            if (terrain == null || originalData == null)
            {
                Debug.LogWarning("[TerrainTreePromoter] Could not restore terrain '" + entry.TerrainPath + "'.");
                continue;
            }

            Undo.RecordObject(terrain, "Restore original TerrainData");
            terrain.terrainData = originalData;
            terrain.Flush();
            EditorUtility.SetDirty(terrain);
            restored++;

            GameObject container = GameObject.Find(entry.ContainerName);
            if (container != null)
            {
                Undo.DestroyObjectImmediate(container);
                containersRemoved++;
            }

            if (!string.IsNullOrEmpty(entry.PromotedTerrainDataPath)
                && entry.PromotedTerrainDataPath != entry.OriginalTerrainDataPath
                && AssetDatabase.DeleteAsset(entry.PromotedTerrainDataPath))
            {
                terrainAssetsDeleted++;
            }
        }

        ClearBackup();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorUtility.DisplayDialog(
            "Restore Terrain Promotion",
            "Restored " + restored + " terrain(s), removed " + containersRemoved + " promoted tree container(s), and deleted " +
            terrainAssetsDeleted + " promoted TerrainData asset(s).",
            "Ok");
    }

    private static LibraryRegistrationResult RegisterPrefabsInLibrary(
        ClientObjectLibrary customObjectLibrary,
        HashSet<GameObject> sourcePrefabs)
    {
        LibraryRegistrationResult result = new LibraryRegistrationResult();
        result.RegisteredPrefabs = new Dictionary<GameObject, GameObject>();

        if (sourcePrefabs.Count == 0)
            return result;

        string libraryPrefabPath = AssetDatabase.GetAssetPath(customObjectLibrary.gameObject);
        if (string.IsNullOrEmpty(libraryPrefabPath))
        {
            Debug.LogError("[TerrainTreePromoter] Custom object library must be a prefab asset in the project.");
            return result;
        }

        string generatedPrefabFolder = EnsureGeneratedPrefabFolder(libraryPrefabPath);
        SerializedObject libraryObject = new SerializedObject(customObjectLibrary);
        SerializedProperty prefabArray = libraryObject.FindProperty("ClientIdPrefabs");
        libraryObject.Update();

        if (prefabArray.arraySize == 0)
        {
            prefabArray.InsertArrayElementAtIndex(0);
            prefabArray.GetArrayElementAtIndex(0).objectReferenceValue = null;
        }

        Dictionary<GameObject, int> existingEntries = new Dictionary<GameObject, int>();
        for (int i = 1; i < prefabArray.arraySize; i++)
        {
            GameObject existingPrefab = prefabArray.GetArrayElementAtIndex(i).objectReferenceValue as GameObject;
            if (existingPrefab != null && !existingEntries.ContainsKey(existingPrefab))
                existingEntries.Add(existingPrefab, i);
        }

        foreach (GameObject sourcePrefab in sourcePrefabs.OrderBy(p => p.name))
        {
            string generatedPrefabPath = GetGeneratedPrefabPath(sourcePrefab, generatedPrefabFolder);
            GameObject existingGeneratedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(generatedPrefabPath);

            int clientId;
            bool replaceLegacySourceReference = existingEntries.TryGetValue(sourcePrefab, out clientId);
            bool libraryAlreadyContainsGeneratedPrefab = !replaceLegacySourceReference
                && existingGeneratedPrefab != null
                && existingEntries.TryGetValue(existingGeneratedPrefab, out clientId);

            if (!replaceLegacySourceReference && !libraryAlreadyContainsGeneratedPrefab)
                clientId = prefabArray.arraySize;

            GameObject libraryPrefab = GetOrCreateLibraryPrefabCopy(
                sourcePrefab,
                generatedPrefabPath,
                customObjectLibrary.BundleName,
                clientId,
                ref result);
            if (libraryPrefab == null)
            {
                Debug.LogWarning("[TerrainTreePromoter] Failed to generate library prefab for source prefab '" + sourcePrefab.name + "'.");
                continue;
            }

            if (replaceLegacySourceReference)
            {
                prefabArray.GetArrayElementAtIndex(clientId).objectReferenceValue = libraryPrefab;
                existingEntries.Remove(sourcePrefab);
                existingEntries[libraryPrefab] = clientId;
            }
            else if (!libraryAlreadyContainsGeneratedPrefab)
            {
                prefabArray.InsertArrayElementAtIndex(clientId);
                prefabArray.GetArrayElementAtIndex(clientId).objectReferenceValue = libraryPrefab;
                existingEntries[libraryPrefab] = clientId;
                result.AddedToLibraryCount++;
            }

            result.RegisteredPrefabs[sourcePrefab] = libraryPrefab;
        }

        libraryObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(customObjectLibrary);
        AssetDatabase.SaveAssets();
        return result;
    }

    private static GameObject GetOrCreateLibraryPrefabCopy(
        GameObject sourcePrefab,
        string generatedPrefabPath,
        string bundleName,
        int clientId,
        ref LibraryRegistrationResult result)
    {
        GameObject tempPrefab = UnityEngine.Object.Instantiate(sourcePrefab);
        tempPrefab.name = sourcePrefab.name;

        try
        {
            EnsureGeneratedPrefabContents(tempPrefab, bundleName, clientId, ref result);

            if (AssetDatabase.LoadAssetAtPath<GameObject>(generatedPrefabPath) != null)
            {
                PrefabUtility.CreatePrefab(generatedPrefabPath, tempPrefab, ReplacePrefabOptions.ReplaceNameBased);
            }
            else
            {
                PrefabUtility.CreatePrefab(generatedPrefabPath, tempPrefab);
                result.GeneratedPrefabCount++;
            }
        }
        finally
        {
            DestroyImmediate(tempPrefab);
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(generatedPrefabPath);
    }

    private static void EnsureGeneratedPrefabContents(
        GameObject prefabRoot,
        string bundleName,
        int clientId,
        ref LibraryRegistrationResult result)
    {
        ClientObject clientObject = prefabRoot.GetComponent<ClientObject>();
        if (clientObject == null)
        {
            clientObject = prefabRoot.AddComponent<ClientObject>();
            clientObject.Category = ClientObject.ObjectCategory.Misc;
            result.ClientObjectsAddedCount++;
        }

        SerializedObject clientObjectData = new SerializedObject(clientObject);
        clientObjectData.Update();
        clientObjectData.FindProperty("ClientId").intValue = clientId;
        clientObjectData.FindProperty("CustomObjectLibrary").stringValue = bundleName;
        clientObjectData.ApplyModifiedProperties();

        EnsureDefaultTreeBounds(prefabRoot, ref result);
        EditorUtility.SetDirty(prefabRoot);
    }

    private static void EnsureDefaultTreeBounds(GameObject prefab, ref LibraryRegistrationResult result)
    {
        EnsureBoundsChild(prefab.transform, "Collider", new Vector3(0f, 5f, 0f), new Vector3(0.8f, 10f, 0.8f), ref result.CollisionBoundsAddedCount);
        EnsureBoundsChild(prefab.transform, "ObjectBounds", new Vector3(0f, 5f, 0f), new Vector3(0.8f, 10f, 0.8f), ref result.ObjectBoundsAddedCount);
    }

    private static void EnsureBoundsChild(
        Transform root,
        string childName,
        Vector3 center,
        Vector3 size,
        ref int addedCount)
    {
        Transform child = root.Find(childName);
        if (child == null)
        {
            GameObject childObject = new GameObject(childName);
            child = childObject.transform;
            child.SetParent(root, false);
            addedCount++;
        }

        child.localPosition = Vector3.zero;
        child.localRotation = Quaternion.identity;
        child.localScale = Vector3.one;

        BoxCollider boxCollider = child.GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = child.gameObject.AddComponent<BoxCollider>();
            addedCount++;
        }

        boxCollider.center = center;
        boxCollider.size = size;
    }

    private static void LogPrefabSummary(Dictionary<GameObject, GameObject> registeredPrefabs, ClientObjectLibrary customObjectLibrary)
    {
        if (registeredPrefabs == null || registeredPrefabs.Count == 0)
            return;

        List<string> lines = new List<string>();
        lines.Add("[TerrainTreePromoter] Registered tree prefabs in library '" + customObjectLibrary.BundleName + "':");

        foreach (KeyValuePair<GameObject, GameObject> pair in registeredPrefabs.OrderBy(p => p.Key.name))
        {
            GameObject sourcePrefab = pair.Key;
            GameObject libraryPrefab = pair.Value;
            string path = AssetDatabase.GetAssetPath(libraryPrefab);
            ClientObject clientObject = libraryPrefab.GetComponent<ClientObject>();
            string clientIdInfo = clientObject != null ? " (ClientId: " + clientObject.ClientId + ")" : "";
            lines.Add("  " + sourcePrefab.name + " -> " + libraryPrefab.name + clientIdInfo + " -> " + path);
        }

        Debug.Log(string.Join("\n", lines.ToArray()));
    }

    private struct LibraryRegistrationResult
    {
        public int AddedToLibraryCount;
        public int ClientObjectsAddedCount;
        public int CollisionBoundsAddedCount;
        public int ObjectBoundsAddedCount;
        public int GeneratedPrefabCount;
        public Dictionary<GameObject, GameObject> RegisteredPrefabs;
    }

    [Serializable]
    private class PromotionBackup
    {
        public string ScenePath;
        public string SceneName;
        public List<PromotionBackupEntry> Entries = new List<PromotionBackupEntry>();
    }

    [Serializable]
    private class PromotionBackupEntry
    {
        public string TerrainPath;
        public string OriginalTerrainDataPath;
        public string PromotedTerrainDataPath;
        public string ContainerName;
    }

    private static bool TryCloneTerrainData(Terrain terrain, TerrainData originalData, out TerrainData promotedData, out string promotedDataPath)
    {
        promotedData = null;
        promotedDataPath = null;

        string originalPath = AssetDatabase.GetAssetPath(originalData);
        string assetDirectory = string.IsNullOrEmpty(originalPath) ? "Assets" : Path.GetDirectoryName(originalPath);
        string originalName = string.IsNullOrEmpty(originalPath) ? terrain.name + "_TerrainData" : Path.GetFileNameWithoutExtension(originalPath);
        string assetPath = Path.Combine(assetDirectory, originalName + "_PromotedTrees.asset").Replace("\\", "/");
        promotedDataPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

        promotedData = Instantiate(originalData);
        if (promotedData == null)
            return false;

        promotedData.name = Path.GetFileNameWithoutExtension(promotedDataPath);
        AssetDatabase.CreateAsset(promotedData, promotedDataPath);
        return true;
    }

    private static HashSet<GameObject> CollectSourcePrefabs(Terrain[] terrains)
    {
        HashSet<GameObject> sourcePrefabs = new HashSet<GameObject>();

        foreach (Terrain terrain in terrains)
        {
            if (terrain.terrainData == null || terrain.terrainData.treeInstances.Length == 0)
                continue;

            TreePrototype[] prototypes = terrain.terrainData.treePrototypes;
            foreach (TreeInstance treeInstance in terrain.terrainData.treeInstances)
            {
                if (treeInstance.prototypeIndex < 0 || treeInstance.prototypeIndex >= prototypes.Length)
                    continue;

                GameObject sourcePrefab = prototypes[treeInstance.prototypeIndex].prefab;
                if (sourcePrefab != null)
                    sourcePrefabs.Add(sourcePrefab);
            }
        }

        return sourcePrefabs;
    }

    private static string EnsureGeneratedPrefabFolder(string libraryPrefabPath)
    {
        string libraryDirectory = Path.GetDirectoryName(libraryPrefabPath).Replace("\\", "/");
        string generatedFolderName = "GeneratedTerrainTreePrefabs";
        string generatedFolderPath = libraryDirectory + "/" + generatedFolderName;
        if (!AssetDatabase.IsValidFolder(generatedFolderPath))
            AssetDatabase.CreateFolder(libraryDirectory, generatedFolderName);

        return generatedFolderPath;
    }

    private static string GetGeneratedPrefabPath(GameObject sourcePrefab, string generatedPrefabFolder)
    {
        string sourcePath = AssetDatabase.GetAssetPath(sourcePrefab);
        string sourceGuid = AssetDatabase.AssetPathToGUID(sourcePath);
        string guidSuffix = string.IsNullOrEmpty(sourceGuid) ? "noguid" : sourceGuid.Substring(0, Mathf.Min(8, sourceGuid.Length));
        string fileName = SanitizeFileName(sourcePrefab.name) + "_" + guidSuffix + ".prefab";
        return (generatedPrefabFolder + "/" + fileName).Replace("\\", "/");
    }

    private static string SanitizeFileName(string rawName)
    {
        char[] invalidChars = Path.GetInvalidFileNameChars();
        foreach (char invalidChar in invalidChars)
            rawName = rawName.Replace(invalidChar.ToString(), "_");

        return string.IsNullOrEmpty(rawName) ? "TerrainTree" : rawName;
    }

    private static string GetContainerName(Terrain terrain)
    {
        return "PromotedTrees_" + GetHierarchyPath(terrain.transform).Replace("/", "_");
    }

    private static bool CanRestoreBackupForCurrentScene(PromotionBackup backup)
    {
        if (backup == null)
            return false;

        return backup.ScenePath == EditorSceneManager.GetActiveScene().path;
    }

    private static void SaveBackup(PromotionBackup backup)
    {
        EditorPrefs.SetString(c_LastPromotionBackupKey, JsonUtility.ToJson(backup));
    }

    private static PromotionBackup LoadBackup()
    {
        if (!EditorPrefs.HasKey(c_LastPromotionBackupKey))
            return null;

        string json = EditorPrefs.GetString(c_LastPromotionBackupKey);
        if (string.IsNullOrEmpty(json))
            return null;

        return JsonUtility.FromJson<PromotionBackup>(json);
    }

    private static void ClearBackup()
    {
        EditorPrefs.DeleteKey(c_LastPromotionBackupKey);
    }

    private static string GetHierarchyPath(Transform target)
    {
        string path = target.name;
        while (target.parent != null)
        {
            target = target.parent;
            path = target.name + "/" + path;
        }

        return path;
    }

    private static Terrain FindTerrainByPath(string hierarchyPath)
    {
        foreach (Terrain terrain in UnityEngine.Object.FindObjectsOfType<Terrain>())
        {
            if (GetHierarchyPath(terrain.transform) == hierarchyPath)
                return terrain;
        }

        return null;
    }
}
