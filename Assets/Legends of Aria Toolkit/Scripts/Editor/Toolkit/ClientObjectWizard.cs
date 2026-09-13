// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// Originally part of the Shards Engine Toolkit; released under the MIT License.
// See LICENSE in the repository root.
using UnityEngine;
using UnityEditor;
using System.IO;

public class ClientObjectWizard : EditorWindow
{
    public void OnGUI()
    {
        EditorGUILayout.Space();

        if (!ToolUtil.ValidateSettings())
        {
            EditorGUILayout.LabelField("Invalid Settings. Please fix your paths by going to the 'LoA Toolkit/Settings' menu.");
            return;
        }

        EditorGUILayout.HelpBox(
            "Creates a client object prefab, either from a model or empty. The new prefab gets a "
            + "ClientObject component, which every entry in a client object library must have.",
            MessageType.Info);

        EditorGUILayout.Space();

        sourceObject = (GameObject)EditorGUILayout.ObjectField(
            new GUIContent("Source Model", "Optional. The model or prefab to turn into a client object. Leave empty to create an empty one."),
            sourceObject, typeof(GameObject), false);

        objectName = EditorGUILayout.TextField(
            new GUIContent("Object Name", "Name of the created prefab. Defaults to the source name."),
            objectName);

        if (sourceObject == null)
        {
            EditorGUILayout.HelpBox(
                "No source model, so an empty client object will be created. Useful for spawners, "
                + "triggers and other objects with no visible mesh. Add the mesh later if you need one.",
                MessageType.None);
        }

        category = (ClientObject.ObjectCategory)EditorGUILayout.EnumPopup(
            new GUIContent("Category", "Used by the client to group objects."), category);

        addCollider = EditorGUILayout.Toggle(
            new GUIContent("Add Box Collider", "Adds a box collider fitted to the renderers if the source has no collider."),
            addCollider);

        EditorGUILayout.Space();

        targetLibrary = (GameObject)EditorGUILayout.ObjectField(
            new GUIContent("Add To Library", "Optional. The client object library prefab to register this object in."),
            targetLibrary, typeof(GameObject), false);

        if (targetLibrary != null && targetLibrary.GetComponent<ClientObjectLibrary>() == null)
        {
            EditorGUILayout.HelpBox("That prefab has no ClientObjectLibrary component.", MessageType.Error);
        }

        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "Select a folder in the Project window to choose where the prefab is created. "
            + "If nothing is selected it is created at the top level of the project.",
            MessageType.Info);

        EditorGUILayout.Space();

        GUI.enabled = sourceObject != null || !string.IsNullOrEmpty(objectName.Trim());
        if (GUILayout.Button(sourceObject != null ? "Create Client Object" : "Create Empty Client Object"))
        {
            CreateClientObject();
        }
        GUI.enabled = true;
    }

    private void CreateClientObject()
    {
        string prefabName = objectName.Trim();
        if (string.IsNullOrEmpty(prefabName))
        {
            prefabName = sourceObject != null ? sourceObject.name : "New Client Object";
        }
        string assetPath = GetTargetFolder() + "/" + prefabName + ".prefab";
        assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

        ClientObjectLibrary library = targetLibrary != null ? targetLibrary.GetComponent<ClientObjectLibrary>() : null;
        if (targetLibrary != null && library == null)
        {
            EditorUtility.DisplayDialog("ERROR", "The selected library prefab has no ClientObjectLibrary component.", "Okay");
            return;
        }

        string confirm = (sourceObject != null ? "Create client object at \"" : "Create empty client object at \"")
                       + assetPath + "\"?";
        if (library != null)
        {
            confirm += "\n\nIt will be added to the library \"" + targetLibrary.name + "\".";
        }
        if (!EditorUtility.DisplayDialog("Confirm", confirm, "Ok", "Cancel"))
        {
            return;
        }

        GameObject instance;
        if (sourceObject != null)
        {
            instance = PrefabUtility.InstantiatePrefab(sourceObject) as GameObject;
            if (instance == null)
            {
                instance = Instantiate(sourceObject);
            }

            // Unpack so the client object prefab owns its contents rather than nesting the source.
            if (PrefabUtility.GetPrefabInstanceStatus(instance) != PrefabInstanceStatus.NotAPrefab)
            {
                PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }
        }
        else
        {
            instance = new GameObject();
        }
        instance.name = prefabName;

        ClientObject clientObject = instance.GetComponent<ClientObject>();
        if (clientObject == null)
        {
            clientObject = instance.AddComponent<ClientObject>();
        }
        clientObject.Category = category;

        if (addCollider && instance.GetComponentInChildren<Collider>() == null)
        {
            AddFittedBoxCollider(instance);
        }

        GameObject newPrefab = PrefabUtility.SaveAsPrefabAsset(instance, assetPath);
        DestroyImmediate(instance);

        if (newPrefab == null)
        {
            EditorUtility.DisplayDialog("ERROR", "Failed to create the prefab at \"" + assetPath + "\".", "Okay");
            return;
        }

        int slot = -1;
        if (library != null)
        {
            slot = AddToLibrary(newPrefab);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = newPrefab;
        EditorGUIUtility.PingObject(newPrefab);

        string result = "Created client object \"" + prefabName + "\".";
        if (slot > 0)
        {
            result += "\n\nAdded to \"" + targetLibrary.name + "\" at client id " + slot
                    + ". The id is assigned for real when you build the mod package.";
        }
        EditorUtility.DisplayDialog("Finished", result, "Ok");
    }

    // Registers the prefab in the first free library slot, growing the array if needed.
    // Index 0 is reserved as the invalid client id and is always left empty.
    private int AddToLibrary(GameObject newPrefab)
    {
        string libraryPath = AssetDatabase.GetAssetPath(targetLibrary);
        GameObject libraryRoot = PrefabUtility.LoadPrefabContents(libraryPath);

        try
        {
            ClientObjectLibrary library = libraryRoot.GetComponent<ClientObjectLibrary>();
            GameObject[] prefabs = library.ClientIdPrefabs;
            if (prefabs == null || prefabs.Length == 0)
            {
                prefabs = new GameObject[1];
            }

            int slot = -1;
            for (int i = 1; i < prefabs.Length; ++i)
            {
                if (prefabs[i] == null)
                {
                    slot = i;
                    break;
                }
            }

            if (slot < 0)
            {
                System.Array.Resize(ref prefabs, prefabs.Length + 1);
                slot = prefabs.Length - 1;
            }

            prefabs[0] = null;
            prefabs[slot] = newPrefab;
            library.ClientIdPrefabs = prefabs;

            PrefabUtility.SaveAsPrefabAsset(libraryRoot, libraryPath);

            return slot;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(libraryRoot);
        }
    }

    private static void AddFittedBoxCollider(GameObject instance)
    {
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            // Nothing to fit to, so give an empty object a unit box to start from.
            instance.AddComponent<BoxCollider>();
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; ++i)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        BoxCollider collider = instance.AddComponent<BoxCollider>();
        collider.center = instance.transform.InverseTransformPoint(bounds.center);
        collider.size = bounds.size;
    }

    private static string GetTargetFolder()
    {
        string selectionPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (string.IsNullOrEmpty(selectionPath))
        {
            return "Assets";
        }

        if (!AssetDatabase.IsValidFolder(selectionPath))
        {
            selectionPath = Path.GetDirectoryName(selectionPath).Replace("\\", "/");
        }

        return string.IsNullOrEmpty(selectionPath) ? "Assets" : selectionPath;
    }

    private GameObject sourceObject;
    private GameObject targetLibrary;
    private string objectName = "";
    private ClientObject.ObjectCategory category = ClientObject.ObjectCategory.Misc;
    private bool addCollider = true;
}
