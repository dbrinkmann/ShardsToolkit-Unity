using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public static class TagsHelpersBootstrapper
{
    static TagsHelpersBootstrapper()
    {
        EditorApplication.delayCall += RestoreOnEditorLoad;
    }

    [MenuItem("LoA Toolkit/Utils/Restore LOA Required Tags and Layers", false, 27)]
    private static void RestoreFromMenu()
    {
        RestoreOnEditorLoad();
    }

    private static void RestoreOnEditorLoad()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        TagsHelpers.RestoreRequiredTagsAndLayersIfNeeded();
    }
}

public class TagsHelpers
{
    public static KeyValuePair<string, int>[] RequiredTagsAndIndex = new KeyValuePair<string, int>[]
    {
        new KeyValuePair<string, int>("Terrain", 0),
        new KeyValuePair<string, int>("Shroud Manager", 1),
        new KeyValuePair<string, int>("Audio", 2),
        new KeyValuePair<string, int>("SceneStreamer", 3),
        new KeyValuePair<string, int>("ColliderStreamerManager", 4),
        new KeyValuePair<string, int>("MiniMapWater", 5),
    };

    public static KeyValuePair<string, int>[] RequiredLayersAndIndex = new KeyValuePair<string, int>[]
    {
        new KeyValuePair<string, int>("Surface", 8),
        new KeyValuePair<string, int>("Roof", 9),
        new KeyValuePair<string, int>("NoPick", 10),
        new KeyValuePair<string, int>("TransparencyCollider", 11),
        new KeyValuePair<string, int>("NGUI", 12),
        new KeyValuePair<string, int>("WorldUI", 13),
        new KeyValuePair<string, int>("InteriorLit", 14),
        new KeyValuePair<string, int>("Outline", 15),
        new KeyValuePair<string, int>("CarriedObject", 16),
        new KeyValuePair<string, int>("Terrain", 17),
        new KeyValuePair<string, int>("Mobiles", 18),
        new KeyValuePair<string, int>("PickerCollider", 19),
        new KeyValuePair<string, int>("PhysicsObject", 20),
        new KeyValuePair<string, int>("Gizmo", 21),
        new KeyValuePair<string, int>("WorldParticles", 22),
        new KeyValuePair<string, int>("PickerCollider2", 23),
        new KeyValuePair<string, int>("MinimapIgnore", 24),
        new KeyValuePair<string, int>("RenderScene", 25),
        new KeyValuePair<string, int>("Cutout", 26),
        new KeyValuePair<string, int>("MinimapInclude", 27),
        new KeyValuePair<string, int>("MinimapBuildings", 28),
    };

    public static bool HasRequiredTagsAndLayers()
    {
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        SerializedProperty layersProp = tagManager.FindProperty("layers");

        for (int i = 0; i < RequiredTagsAndIndex.Length; i++)
            if (HasTagOrLayerAtIndex(RequiredTagsAndIndex[i].Key, RequiredTagsAndIndex[i].Value, tagsProp) == false)
                return false;

        for (int i = 0; i < RequiredLayersAndIndex.Length; i++)
            if (HasTagOrLayerAtIndex(RequiredLayersAndIndex[i].Key, RequiredLayersAndIndex[i].Value, layersProp) == false)
                return false;

        return true;
    }

    public static bool RestoreRequiredTagsAndLayersIfNeeded()
    {
        if (HasRequiredTagsAndLayers())
            return false;

        AddTagsAndLayers();
        AssetDatabase.SaveAssets();
        Debug.LogWarning("LoA Toolkit restored the required project tags and layers for this project.");
        return true;
    }

    public static void AddTagsAndLayers()
    {
        // Open tag manager
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        SerializedProperty layersProp = tagManager.FindProperty("layers");

        for (int i = 0; i < RequiredTagsAndIndex.Length; i++)
            CreateTagIfDoesntExist(RequiredTagsAndIndex[i].Key, RequiredTagsAndIndex[i].Value, tagsProp);

        for (int i = 0; i < RequiredLayersAndIndex.Length; i++)
            SetUserLayer(RequiredLayersAndIndex[i].Key, RequiredLayersAndIndex[i].Value, layersProp);

        tagManager.ApplyModifiedProperties();
    }

    public static bool HasTagOrLayerAtIndex(string tag, int index, SerializedProperty tagsProp)
    {
        if (index >= tagsProp.arraySize || tagsProp.arraySize == 0)
            return false;

        SerializedProperty t = tagsProp.GetArrayElementAtIndex(index);
        if (t.stringValue.Equals(tag))
            return true;

        return false;
    }

    public static void SetUserLayer(string tag, int index, SerializedProperty layerProp)
    {
        SerializedProperty sp = layerProp.GetArrayElementAtIndex(index);
        if (sp != null) sp.stringValue = tag;
    }

    public static void CreateTagIfDoesntExist(string tag, int index, SerializedProperty tagsProp)
    {
        bool found = false;
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            SerializedProperty t = tagsProp.GetArrayElementAtIndex(i);
            if (t.stringValue.Equals(tag) && i == index) { found = true; break; }
        }

        if (!found)
        {
            while (tagsProp.arraySize <= index)
            {
                tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            }

            SerializedProperty n = tagsProp.GetArrayElementAtIndex(index);
            n.stringValue = tag;
        }
    }
}
