// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// Originally part of the Shards Engine Toolkit; released under the MIT License.
// See LICENSE in the repository root.
using UnityEngine;
using UnityEditor;
using System.Collections;
using CoreUtil.ShardEngineMath;

public class ShardsToolkitMenu
{
    [MenuItem("LoA Toolkit/Settings", false,1)]
    public static void ShowSettingsWindow()
    {
        ModPackageBuildWindow buildWindow = EditorWindow.GetWindow(typeof(ModPackageBuildWindow)) as ModPackageBuildWindow;
        buildWindow.titleContent = new GUIContent("Build Mod Package");
    }

    [MenuItem("LoA Toolkit/Create Mod", false, 2)]
    public static void ShowCreateModWindow()
    {
        ModWizard modWizardWindow = EditorWindow.GetWindow(typeof(ModWizard)) as ModWizard;
        modWizardWindow.title = "Create Mod";
    }

    [MenuItem("LoA Toolkit/Build Mod Package", false, 3)]
    public static void ShowBuildModPackageWindow()
    {
        ModPackageBuildWindow buildWindow = EditorWindow.GetWindow(typeof(ModPackageBuildWindow)) as ModPackageBuildWindow;
        buildWindow.titleContent = new GUIContent("Build Mod Package");
    }

    [MenuItem("LoA Toolkit/Custom Assets/Create New Custom Map", false, 4)]
    public static void CreateNewCustomMap()
    {
        MapWizard mapWizardWindow = EditorWindow.GetWindow(typeof(MapWizard)) as MapWizard;
        mapWizardWindow.title = "Create New Map";
    }

    [MenuItem("LoA Toolkit/Custom Assets/Create New Custom Object Library", false, 5)]
    public static void CreateNewCustomObjectLibrary()
    {
        LibraryWizard libraryWizardWindow = EditorWindow.GetWindow(typeof(LibraryWizard)) as LibraryWizard;
        libraryWizardWindow.title = "Create New Object Library";
    }

    [MenuItem("LoA Toolkit/Custom Assets/Create New UI Texture Library", false, 6)]
    public static void CreateNewUITextureLibrary()
    {
        UILibraryWizard libraryWizardWindow = EditorWindow.GetWindow(typeof(UILibraryWizard)) as UILibraryWizard;
        libraryWizardWindow.title = "Create New UI Texture Library";
    }

    /*[MenuItem("LoA Toolkit/Template Editor", false, 3)]
    public static void ShowTemplateEditorWindow()
    {
        TemplateEditor templateEditor = EditorWindow.GetWindow(typeof(TemplateEditor)) as TemplateEditor;
        templateEditor.Initialize();
        templateEditor.title = "Template Editor";
    }*/

    /*[MenuItem("LoA Toolkit/Merchant Editor", false, 3)]
    public static void ShowMerchantEditorWindow()
    {
        MerchantEditor merchantEditor = EditorWindow.GetWindow(typeof(MerchantEditor)) as MerchantEditor;
        merchantEditor.title = "Merchant Editor";
    }*/

    [MenuItem("LoA Toolkit/Dynamic Editors/Seed Object Editor", false,5)]
    public static void ShowSeedObjectWindow()
    {
        SeedObjectEditor seedObjectEditorWindow = EditorWindow.GetWindow(typeof(SeedObjectEditor)) as SeedObjectEditor;
        seedObjectEditorWindow.title = "Seed Object Editor";
    }

    [MenuItem("LoA Toolkit/Dynamic Editors/Prefab Editor", false, 6)]
    public static void ShowPrefabWindow()
    {
        PrefabEditor prefabEditorWindow = EditorWindow.GetWindow(typeof(PrefabEditor)) as PrefabEditor;
        prefabEditorWindow.title = "Prefab Editor";
    }

    [MenuItem("LoA Toolkit/Dynamic Editors/Region Editor", false, 7)]
    public static void ShowRegionEditorWindow()
    {
        RegionEditor regionEditorWindow = EditorWindow.GetWindow(typeof(RegionEditor)) as RegionEditor;
        regionEditorWindow.title = "Region Editor";
    }

    [MenuItem("LoA Toolkit/Dynamic Editors/Path Editor", false, 8)]
    public static void ShowPathEditorWindow()
    {
        PathEditor pathEditorWindow = EditorWindow.GetWindow(typeof(PathEditor)) as PathEditor;
        pathEditorWindow.title = "Path Editor";
    }

    [MenuItem("LoA Toolkit/Dynamic Editors/Collision Editor", false, 9)]
    public static void ShowCollisionEditorWindow()
    {
        StaticCollisionEditor collisionEditorWindow = StaticCollisionEditor.GetWindow(typeof(StaticCollisionEditor)) as StaticCollisionEditor;
        collisionEditorWindow.title = "Static Collision Editor";
    }

    /*[MenuItem("LoA Toolkit/Data Table Editor", false, 7)]
    public static void ShowDataTableWindow()
    {
        DataTablesEditor dataTableWindow = EditorWindow.GetWindow(typeof(DataTablesEditor)) as DataTablesEditor;
        dataTableWindow.Initialize();
        dataTableWindow.title = "Data Table Editor";
    }*/

    [MenuItem("LoA Toolkit/Utils/Copy Local Position To Clipboard", false, 20)]
    static void CopyLocalPositionToClipboard()
    {
        Vector3 pos = Selection.activeTransform.localPosition;
        string posStr = pos.x + ", " + pos.y + ", " + pos.z;
        ToolUtil.CopyToClipboard(posStr);
    }

    [MenuItem("LoA Toolkit/Utils/Copy Server Position To Clipboard", false, 21)]
    static void CopyPositionToClipboard()
    {
        Vector3 worldPosition = Selection.activeTransform.position;
        Vec3 serverPos = UnityUtil.ConvertToServerPos(worldPosition);
        string posStr = serverPos.X + ", " + serverPos.Y + ", " + serverPos.Z;
        ToolUtil.CopyToClipboard(posStr);
    }

    [MenuItem("LoA Toolkit/Utils/Copy Rotation To Clipboard", false, 22)]
    static void CopyRotationToClipboard()
    {
        Vector3 rot = Selection.activeTransform.rotation.eulerAngles;
        string posStr = rot.x + ", " + rot.y + ", " + rot.z;
        ToolUtil.CopyToClipboard(posStr);
    }

    [MenuItem("LoA Toolkit/Utils/Merchant Item XML To Clipboard", false, 23)]
    static void CopyMerchantItemToClipboard()
    {
        SeedObject seedObj = Selection.activeGameObject.GetComponent<SeedObject>();
        if (seedObj != null)
        {
            Vector3 worldPosition = Selection.activeTransform.position;
            Vec3 serverPos = UnityUtil.ConvertToServerPos(worldPosition);
            Vector3 rot = Selection.activeTransform.rotation.eulerAngles;

            string templateStr = seedObj.ServerTemplateId;
            int price = 0;

            string rotationStr = "";
            if (rot != Vector3.zero)
            {
                rotationStr = string.Format("Rotation=\"{6},{7},{8}\"", rot.x, rot.y, rot.z);
            }

            string outputStr = string.Format("{{ Template=\"{0}\", Price=\"{1}\", Loc=\"{2},{3},{4}\", {5} }},", templateStr, price, serverPos.X, serverPos.Y, serverPos.Z, rotationStr);

            ToolUtil.CopyToClipboard(outputStr);
        }
    }

    [MenuItem("LoA Toolkit/Utils/Copy Selected Permanent Object ID's", false, 24)]
    static void CopyPermanentIDsToClipboard()
    {
        var objects = Selection.gameObjects;
        string str = "";
        foreach (GameObject _object in objects )
        {
            if (_object.GetComponent<ClientObject>() != null)
            {
                if (_object.activeInHierarchy)
                str = str + _object.GetComponent<ClientObject>().PermanentId + ",";
            }
            //get all the permanet ids in the selected objects
            var subObjects = UnityUtil.SearchForClientObjects(_object.transform);
            foreach (Transform clientObject in subObjects)
            {
                if (clientObject.gameObject.activeInHierarchy)
                str = str + ((clientObject.GetComponent<ClientObject>())).PermanentId + ",";
            }
        }
        //return the string
        ToolUtil.CopyToClipboard(str);
    }

    [MenuItem("LoA Toolkit/Utils/Copy All Selected Positions To Clipboard", false, 26)]
    static void CopyAllPositionToClipboard()
    {
        string posString = "";
        foreach (var curObject in Selection.gameObjects)
        {
            Vector3 worldPosition = curObject.transform.position;
            Vec3 serverPos = UnityUtil.ConvertToServerPos(worldPosition);
            posString = posString + "\n" + serverPos.X + ", " + serverPos.Y + ", " + serverPos.Z;
        }
        ToolUtil.CopyToClipboard(posString);
    }

    [MenuItem("LoA Toolkit/Utils/Copy Server Camp LuaTable from positions", false, 27)]
    static void CreateCampLocationsFromSelection()
    {
        string posString = "";
        foreach (var curObject in Selection.gameObjects)
        {
            Vector3 worldPosition = curObject.transform.position;
            Vec3 serverPos = UnityUtil.ConvertToServerPos(worldPosition);
            posString = posString + "\n{Loc=Loc(" + serverPos.X + ", " + 0 + ", " + serverPos.Z + "), Types = {\"\"}},";
        }
        ToolUtil.CopyToClipboard(posString);
    }

    [MenuItem("LoA Toolkit/Utils/Generate LOA Required Tags and Layers", false, 28)]
    static void GenerateProjectWideTags()
    {
        TagsHelpers.AddTagsAndLayers();
    }


    [MenuItem("LoA Toolkit/Utils/Print Asset Bundle Contents", false, 29)]
    static void PrintContents()
    {
        if (Selection.activeObject == null)
            return;

        AssetBundle bundle = AssetBundle.LoadFromFile(Application.dataPath + AssetDatabase.GetAssetPath(Selection.activeObject).Remove(0, 6));

        if (bundle != null)
        {
            string[] assets = AssetDatabase.GetAssetBundleDependencies(bundle.name, false);

            for (int i = 0; i < assets.Length; i++)
                Debug.Log(assets[i]);

            SerializedObject so = new SerializedObject(bundle);
            System.Text.StringBuilder str = new System.Text.StringBuilder();

            str.Append("Preload table:\n");
            foreach (SerializedProperty d in so.FindProperty("m_PreloadTable"))
            {
                if (d.objectReferenceValue != null)
                    str.Append("\t<color=green>" + d.objectReferenceValue.name + " " + d.objectReferenceValue.GetType().ToString() + "</color>\n");
            }

            str.Append("Container:\n");
            foreach (SerializedProperty d in so.FindProperty("m_Container"))
                str.Append("\t" + d.displayName + "\n");

            Debug.Log(str.ToString());
            bundle.Unload(false);
        }
    }
}
