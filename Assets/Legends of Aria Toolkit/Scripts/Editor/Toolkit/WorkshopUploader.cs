using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Steamworks;

public class WorkshopUploader
{
    static bool steamInitialized = false;
    static CallResult<CreateItemResult_t> createItemResult;
    static CallResult<SubmitItemUpdateResult_t> submitItemResult;
    static Action<bool, string, ulong> currentCallback;

    public static bool InitSteam()
    {
        if (steamInitialized)
        {
            return true;
        }

        if (!File.Exists(Path.Combine(Application.dataPath, "..", "steam_appid.txt")))
        {
            File.WriteAllText(Path.Combine(Application.dataPath, "..", "steam_appid.txt"), "3944440");
        }

        try
        {
            steamInitialized = SteamAPI.Init();
            if (steamInitialized)
            {
                EditorApplication.update += RunCallbacks;
                Debug.Log("[WorkshopUploader] SteamAPI initialized. SteamID: " + SteamUser.GetSteamID());
            }
            else
            {
                Debug.LogError("[WorkshopUploader] SteamAPI.Init() returned false. Is Steam running?");
            }

            return steamInitialized;
        }
        catch (Exception e)
        {
            Debug.LogError("[WorkshopUploader] SteamAPI.Init failed: " + e.Message);
            return false;
        }
    }

    public static void ShutdownSteam()
    {
        if (steamInitialized)
        {
            EditorApplication.update -= RunCallbacks;
            SteamAPI.Shutdown();
            steamInitialized = false;
            Debug.Log("[WorkshopUploader] SteamAPI shutdown.");
        }
    }

    static void RunCallbacks()
    {
        if (steamInitialized)
        {
            SteamAPI.RunCallbacks();
        }
    }

    public static void UploadPackage(string packageRootPath, string modName, ulong existingFileId, Action<bool, string, ulong> callback)
    {
        if (!InitSteam())
        {
            string error = "Steam is not initialized. Make sure Steam is running and you are logged in.";
            Debug.LogError("[WorkshopUploader] " + error);
            callback(false, error, 0);
            return;
        }

        string absolutePath = Path.GetFullPath(packageRootPath);
        if (!Directory.Exists(absolutePath))
        {
            string error = "Package directory does not exist: " + absolutePath;
            Debug.LogError("[WorkshopUploader] " + error);
            callback(false, error, 0);
            return;
        }

        string manifestPath = Path.Combine(absolutePath, "mod-manifest.json");
        if (!File.Exists(manifestPath))
        {
            string error = "Package directory is missing mod-manifest.json: " + absolutePath;
            Debug.LogError("[WorkshopUploader] " + error);
            callback(false, error, 0);
            return;
        }

        EnsurePreviewImage(absolutePath);

        Debug.Log("[WorkshopUploader] Starting upload. Path: " + absolutePath + ", Mod: " + modName + ", ExistingFileId: " + existingFileId);

        currentCallback = callback;

        if (existingFileId == 0)
        {
            CreateNewItem(absolutePath, modName);
        }
        else
        {
            UpdateExistingItem(new PublishedFileId_t(existingFileId), absolutePath, modName);
        }
    }

    static void EnsurePreviewImage(string packageRootPath)
    {
        string previewPath = Path.Combine(packageRootPath, "preview.png");
        if (File.Exists(previewPath))
        {
            return;
        }

        Texture2D placeholder = new Texture2D(512, 512);
        Color[] pixels = new Color[512 * 512];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = new Color(0.2f, 0.2f, 0.3f, 1.0f);
        }
        placeholder.SetPixels(pixels);
        placeholder.Apply();
        File.WriteAllBytes(previewPath, placeholder.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(placeholder);
        Debug.Log("[WorkshopUploader] Generated placeholder preview.png");
    }

    static void CreateNewItem(string packageRootPath, string modName)
    {
        EditorUtility.DisplayProgressBar("Workshop Upload", "Creating new Workshop item...", 0.1f);
        Debug.Log("[WorkshopUploader] Creating new Workshop item for AppId 3944440...");

        SteamAPICall_t call = SteamUGC.CreateItem(new AppId_t(3944440), EWorkshopFileType.k_EWorkshopFileTypeCommunity);
        createItemResult = CallResult<CreateItemResult_t>.Create((result, bIOFailure) =>
        {
            if (bIOFailure || result.m_eResult != EResult.k_EResultOK)
            {
                EditorUtility.ClearProgressBar();
                string error = bIOFailure ? "IO failure" : result.m_eResult.ToString();
                Debug.LogError("[WorkshopUploader] Failed to create Workshop item: " + error);
                currentCallback?.Invoke(false, "Failed to create Workshop item: " + error, 0);
                return;
            }

            PublishedFileId_t fileId = result.m_nPublishedFileId;
            Debug.Log("[WorkshopUploader] Created Workshop item: " + fileId.m_PublishedFileId);
            UpdateExistingItem(fileId, packageRootPath, modName);
        });
        createItemResult.Set(call);
    }

    static void UpdateExistingItem(PublishedFileId_t fileId, string packageRootPath, string modName)
    {
        EditorUtility.DisplayProgressBar("Workshop Upload", "Preparing update...", 0.3f);
        Debug.Log("[WorkshopUploader] Starting item update. FileId: " + fileId.m_PublishedFileId + ", ContentPath: " + packageRootPath);

        UGCUpdateHandle_t updateHandle = SteamUGC.StartItemUpdate(new AppId_t(3944440), fileId);

        bool titleOk = SteamUGC.SetItemTitle(updateHandle, modName);
        bool descOk = SteamUGC.SetItemDescription(updateHandle, "Mod package for " + modName);
        bool contentOk = SteamUGC.SetItemContent(updateHandle, packageRootPath);
        bool visOk = SteamUGC.SetItemVisibility(updateHandle, ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityUnlisted);

        Debug.Log("[WorkshopUploader] SetItemTitle: " + titleOk + ", SetItemDescription: " + descOk + ", SetItemContent: " + contentOk + ", SetItemVisibility: " + visOk);

        string previewPath = Path.Combine(packageRootPath, "preview.png");
        if (File.Exists(previewPath))
        {
            bool previewOk = SteamUGC.SetItemPreview(updateHandle, previewPath);
            Debug.Log("[WorkshopUploader] SetItemPreview: " + previewOk + " (" + previewPath + ")");
        }
        else
        {
            Debug.LogWarning("[WorkshopUploader] No preview.png found at " + previewPath);
        }

        EditorUtility.DisplayProgressBar("Workshop Upload", "Uploading content...", 0.5f);
        Debug.Log("[WorkshopUploader] Submitting item update...");

        SteamAPICall_t call = SteamUGC.SubmitItemUpdate(updateHandle, "Mod package update");
        submitItemResult = CallResult<SubmitItemUpdateResult_t>.Create((result, bIOFailure) =>
        {
            EditorUtility.ClearProgressBar();

            if (bIOFailure || result.m_eResult != EResult.k_EResultOK)
            {
                string error = bIOFailure ? "IO failure" : result.m_eResult.ToString();
                Debug.LogError("[WorkshopUploader] Failed to upload Workshop item: " + error + " (FileId: " + fileId.m_PublishedFileId + ")");
                currentCallback?.Invoke(false, "Failed to upload Workshop item: " + error, fileId.m_PublishedFileId);
                return;
            }

            Debug.Log("[WorkshopUploader] Successfully uploaded Workshop item: " + fileId.m_PublishedFileId);
            currentCallback?.Invoke(true, null, fileId.m_PublishedFileId);
        });
        submitItemResult.Set(call);
    }
}
