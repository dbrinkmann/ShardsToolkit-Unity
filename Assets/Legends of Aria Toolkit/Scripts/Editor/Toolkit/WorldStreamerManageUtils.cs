using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class WorldStreamerManageUtils
{        
    public static WorldStreamManageData LoadWorldStreamManageData(bool createIfNotFound)
    {
        string sceneName = EditorSceneManager.GetActiveScene().name;
        WorldStreamManageData manageData = (WorldStreamManageData)AssetDatabase.LoadAssetAtPath(@"Assets\Legends of Aria Toolkit\Scripts\Editor\Toolkit\WorldStreamManageData" + sceneName + ".asset", typeof(WorldStreamManageData));
        if (manageData == null && createIfNotFound)
        {
            manageData = ScriptableObject.CreateInstance<WorldStreamManageData>();
        }

        return manageData;
    }

    public static void SaveWorldStreamManageData(WorldStreamManageData manageData)
    {
        if (!AssetDatabase.Contains(manageData))
        {
            string sceneName = EditorSceneManager.GetActiveScene().name;
            AssetDatabase.CreateAsset(manageData, @"Assets\Legends of Aria Toolkit\Scripts\Editor\Toolkit\WorldStreamManageData" + sceneName + ".asset");
        }
        EditorUtility.SetDirty(manageData);
        AssetDatabase.SaveAssets();
    }    

    public static int GetSceneIndex(int x, int z, WorldStreamManageData manageData)
    {
        if (manageData != null)
        {
            string sceneName = GetSceneName(x, z, manageData);
            for (int i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                Scene sceneObj = EditorSceneManager.GetSceneAt(i);
                if (sceneObj.name == sceneName)
                {
                    return i;
                }
            }
        }
        return -1;
    }

    public static string GetSceneName(int x, int z, WorldStreamManageData manageData)
    {
        if (manageData != null)
        {
            return manageData.prefixScene + "_x" + x + "_z" + z;
        }

        return null;
    }
}
