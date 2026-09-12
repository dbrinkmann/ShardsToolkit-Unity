using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;
using CoreUtil.ShardEngineMath;
using System.Text.RegularExpressions;
using ShardsXML.TagDefinitions;

[CustomEditor(typeof(UITextureLibrary))]
public class UITextureLibraryEditor : Editor
{
    private UITextureLibrary myTarget;
   
    
    private void OnEnable()
    {
       
    }

    public override void OnInspectorGUI()
    {
        myTarget = (UITextureLibrary)target;
        DrawDefaultInspector();
        EditorGUILayout.Separator();

        EditorGUILayout.LabelField("Library", EditorStyles.boldLabel);
        serializedObject.Update();
        EditorGUILayout.Separator();

        EditorGUILayout.LabelField("Build UI Texture Library", EditorStyles.boldLabel);

        bool validSettings = true;

        // also check bundle name
        if(validSettings)
        {
            Regex r = new Regex("^[a-zA-Z0-9 _-]*$");
            if (myTarget.BundleName == null || myTarget.BundleName == "" || !r.IsMatch(myTarget.BundleName))
            {
                validSettings = false;
                EditorGUILayout.HelpBox("Bundle Name is not valid. Please only use alpha numberic characters and spaces, underscores and dashes.", MessageType.Error);
            }
        }

        EditorGUI.BeginDisabledGroup(!validSettings);
        EditorGUI.indentLevel++;

        if (GUILayout.Button("Build"))
        {
            int bundleVersion = myTarget.BundleVersion+1;

            SerializedObject so = new SerializedObject(myTarget);
            so.FindProperty("BundleVersion").intValue = bundleVersion;
            so.ApplyModifiedProperties();

            if (Directory.Exists("Assets/AssetBundles") == false)
                Directory.CreateDirectory("Assets/AssetBundles");

            string versionFileName = "Assets/AssetBundles/" + myTarget.BundleName.ToLower() + ".version";
            File.WriteAllText(versionFileName, bundleVersion.ToString());
            AssetBundleBuild[] buildMap = new AssetBundleBuild[1];

            HashSet<string> assets = new HashSet<string>();
            if (Directory.Exists("Assets/UI/Textures/"))
            {
                foreach (string file in Directory.GetFiles("Assets/UI/Textures/"))
                    assets.Add(file);
            }

            if (Directory.Exists("Assets/UI/Maps/"))
            {
                foreach (string file in Directory.GetFiles("Assets/UI/Maps/"))
                    assets.Add(file);
            }

            if (Directory.Exists("Assets/UI/Loading/"))
            {
                foreach (string file in Directory.GetFiles("Assets/UI/Loading/"))
                    assets.Add(file);
            }

            buildMap[0].assetBundleName = myTarget.BundleName;
            buildMap[0].assetNames = assets.ToArray();
            BuildPipeline.BuildAssetBundles("Assets/AssetBundles", buildMap, BuildAssetBundleOptions.None,
                BuildTarget.StandaloneWindows);
        }
        EditorGUI.indentLevel--;
        EditorGUI.EndDisabledGroup();
    }
}
