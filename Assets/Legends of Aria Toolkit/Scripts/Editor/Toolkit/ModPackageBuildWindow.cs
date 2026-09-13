using System.Linq;
using UnityEditor;
using UnityEngine;

public class ModPackageBuildWindow : EditorWindow
{
    SerializedObject settingsObject;
    string lastBuildPath;
    bool lastBuildSucceeded;
    bool uploadInProgress;

    string basePath;
    string modsPath;
    string[] popupMods;
    int modIndex = 0;
    Vector2 scrollPos;

    void OnEnable()
    {
        settingsObject = new SerializedObject(ToolUtil.Settings);
        basePath = ToolUtil.Settings.BasePath;
        modsPath = ToolUtil.Settings.ModsPath;
        RefreshModList();
    }

    void OnFocus()
    {
        RefreshModList();
    }

    void OnDestroy()
    {
        WorkshopUploader.ShutdownSteam();
    }

    void RefreshModList()
    {
        popupMods = (new string[] { "Default" }).Concat(ModHelpers.RefreshList()).ToArray();
        modIndex = 0;
        for (int i = 0; i < popupMods.Length; ++i)
        {
            if (popupMods[i].Equals(ToolUtil.Settings.CurrentModName))
            {
                modIndex = i;
                break;
            }
        }
    }

    void OnGUI()
    {
        if (settingsObject == null)
        {
            settingsObject = new SerializedObject(ToolUtil.Settings);
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        DrawSettingsSection();
        DrawBuildSection();
        DrawWorkshopSection();

        EditorGUILayout.EndScrollView();
    }

    void DrawSettingsSection()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Toolkit Settings", EditorStyles.boldLabel);

        EditorGUIUtility.labelWidth = 120;

        EditorGUILayout.BeginHorizontal();
        GUI.SetNextControlName("BasePath");
        basePath = EditorGUILayout.TextField("Base Path", basePath);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            GUI.FocusControl("ModsPath");
            string selected = EditorUtility.OpenFolderPanel("Select Base path folder", basePath, "");
            if (!string.IsNullOrEmpty(selected))
            {
                basePath = selected;
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        GUI.SetNextControlName("ModsPath");
        modsPath = EditorGUILayout.TextField("Mods Path", modsPath);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            GUI.FocusControl("BasePath");
            string selected = EditorUtility.OpenFolderPanel("Select Mods path folder", modsPath, "");
            if (!string.IsNullOrEmpty(selected))
            {
                modsPath = selected;
            }
        }
        EditorGUILayout.EndHorizontal();

        int newModIndex = EditorGUILayout.Popup("Current Mod", modIndex, popupMods);
        if (newModIndex != modIndex)
        {
            modIndex = newModIndex;
        }

        bool settingsChanged = ToolUtil.Settings.BasePath != basePath
            || ToolUtil.Settings.ModsPath != modsPath
            || (popupMods.Length > modIndex && ToolUtil.Settings.CurrentModName != popupMods[modIndex]);

        if (settingsChanged)
        {
            if (GUILayout.Button("Save Settings"))
            {
                ToolUtil.Settings.BasePath = basePath;
                ToolUtil.Settings.ModsPath = modsPath;

                if (popupMods.Length > modIndex)
                {
                    ToolUtil.Settings.CurrentModName = popupMods[modIndex];
                }

                ToolUtil.Settings.Save();
                RefreshModList();
            }
        }
    }

    void DrawBuildSection()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Build Mod Package", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Build the current mod into a single Workshop package containing all assigned scene bundles and client object libraries.", MessageType.Info);

        EditorGUILayout.LabelField("Package Output", ModPackageBuildUtility.GetPackageRootPath());

        settingsObject.Update();
        EditorGUILayout.PropertyField(settingsObject.FindProperty("ModScenes"), true);
        EditorGUILayout.PropertyField(settingsObject.FindProperty("CustomObjectLibraries"), true);
        EditorGUILayout.PropertyField(settingsObject.FindProperty("WorkshopPackageOutputPath"));
        settingsObject.ApplyModifiedProperties();
        if (GUI.changed)
        {
            ToolUtil.Settings.Save();
        }

        EditorGUILayout.Space();
        EditorGUI.BeginDisabledGroup(uploadInProgress);
        if (GUILayout.Button("Build Mod Package"))
        {
            string packageRootPath;
            string errorMessage;
            if (ModPackageBuildUtility.BuildCurrentModPackage(out packageRootPath, out errorMessage))
            {
                lastBuildPath = packageRootPath;
                lastBuildSucceeded = true;
                EditorUtility.DisplayDialog("Build Complete", "Built mod package successfully.\n\nOutput: " + packageRootPath, "OK");
            }
            else
            {
                lastBuildPath = null;
                lastBuildSucceeded = false;
                EditorUtility.DisplayDialog("Build Failed", errorMessage, "OK");
            }
        }
        EditorGUI.EndDisabledGroup();
    }

    void DrawWorkshopSection()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Steam Workshop Upload", EditorStyles.boldLabel);

        settingsObject.Update();
        EditorGUILayout.PropertyField(settingsObject.FindProperty("WorkshopPublishedFileId"), new GUIContent("Published File ID", "Leave empty to create a new Workshop item, or enter an existing ID to update it."));
        settingsObject.ApplyModifiedProperties();
        if (GUI.changed)
        {
            ToolUtil.Settings.Save();
        }

        string currentFileId = ToolUtil.Settings.WorkshopPublishedFileId;
        if (string.IsNullOrEmpty(currentFileId))
        {
            EditorGUILayout.HelpBox("No Published File ID set. Uploading will create a new Workshop item and save the ID here.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("Will update existing Workshop item: " + currentFileId, MessageType.Info);
        }

        string uploadPath = lastBuildSucceeded ? lastBuildPath : ModPackageBuildUtility.GetPackageRootPath();
        bool packageExists = !string.IsNullOrEmpty(uploadPath) && System.IO.Directory.Exists(uploadPath) && System.IO.File.Exists(System.IO.Path.Combine(uploadPath, "mod-manifest.json"));

        if (!packageExists)
        {
            EditorGUILayout.HelpBox("Build a mod package first before uploading.", MessageType.Warning);
        }

        if (!WorkshopUploader.IsAvailable)
        {
            EditorGUILayout.HelpBox(
                "Steam Workshop upload is unavailable. Install the Steamworks.NET SDK into this "
                + "project, then add STEAMWORKS_NET to Scripting Define Symbols in Player Settings. "
                + "Everything else in the toolkit works without it.", MessageType.Info);
        }

        EditorGUI.BeginDisabledGroup(!WorkshopUploader.IsAvailable || !packageExists || uploadInProgress || string.IsNullOrEmpty(ToolUtil.Settings.CurrentModName));
        if (GUILayout.Button(uploadInProgress ? "Uploading..." : "Upload to Steam Workshop"))
        {
            uploadInProgress = true;

            ulong existingId = 0;
            if (!string.IsNullOrEmpty(currentFileId))
            {
                ulong.TryParse(currentFileId.Trim(), out existingId);
            }

            WorkshopUploader.UploadPackage(uploadPath, ToolUtil.Settings.CurrentModName, existingId, OnUploadComplete);
        }
        EditorGUI.EndDisabledGroup();
    }

    void OnUploadComplete(bool success, string error, ulong publishedFileId)
    {
        uploadInProgress = false;

        if (success)
        {
            ToolUtil.Settings.WorkshopPublishedFileId = publishedFileId.ToString();
            ToolUtil.Settings.Save();
            settingsObject.Update();

            EditorUtility.DisplayDialog("Upload Complete",
                "Successfully uploaded to Steam Workshop.\n\nPublished File ID: " + publishedFileId,
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Upload Failed", error, "OK");
        }

        Repaint();
    }
}
