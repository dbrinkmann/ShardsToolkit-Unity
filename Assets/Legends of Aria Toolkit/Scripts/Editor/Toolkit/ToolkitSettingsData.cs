// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// Originally part of the Shards Engine Toolkit; released under the MIT License.
// See LICENSE in the repository root.
using UnityEngine;
using UnityEditor;
using System.Collections;

public class ToolkitSettingsData : ScriptableObject
{
    [SerializeField]
    public string BasePath = "";
    [SerializeField]
    public string ModsPath = string.Empty;
    [SerializeField]
    public string CurrentModName = string.Empty;
    [SerializeField]
    public SceneAsset[] ModScenes;
    [SerializeField]
    public GameObject[] CustomObjectLibraries;
    [SerializeField]
    public string WorkshopPackageOutputPath = string.Empty;
    [SerializeField]
    public string WorkshopPublishedFileId = string.Empty;

    public void Save()
    {
        if (!AssetDatabase.Contains(this))
        {
            AssetDatabase.CreateAsset(this, @"Assets\Legends of Aria Toolkit\Scripts\Editor\ToolkitSettings.asset");
        }
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
    }

    public static ToolkitSettingsData Load()
    {
        ToolkitSettingsData settingsData = (ToolkitSettingsData)AssetDatabase.LoadAssetAtPath(@"Assets\Legends of Aria Toolkit\Scripts\Editor\ToolkitSettings.asset", typeof(ToolkitSettingsData));
        if (settingsData == null)
        {
            settingsData = ScriptableObject.CreateInstance<ToolkitSettingsData>();
        }

        return settingsData;
    }
}