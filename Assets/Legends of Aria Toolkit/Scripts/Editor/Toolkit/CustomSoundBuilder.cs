// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// See LICENSE in the repository root.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

// Builds a library's CustomSoundLibrary from the audio files in a "Sounds" folder next to the
// library prefab. In game the sounds are addressed as "mod:/<BundleName>/<name>", where name is
// the file name in lowercase without extension; numbered files (wolf_attack_1.ogg,
// wolf_attack_2.ogg) become variants of one sound (wolf_attack) picked at random.
//
// FMOD decodes the files itself, so they go into the bundle as raw bytes (TextAssets in a
// "GeneratedSounds" folder that this class rewrites on every build). Settings changed on the
// library's CustomSoundLibrary component (volume, distances, ...) are kept across builds.
public static class CustomSoundBuilder
{
    public const string SoundsFolderName = "Sounds";
    public const string GeneratedFolderName = "GeneratedSounds";
    public const long LargeFileBytes = 20 * 1024 * 1024;

    public static readonly string[] SupportedExtensions = { ".ogg", ".wav", ".mp3", ".flac", ".aif", ".aiff" };

    static readonly Regex VariantSuffix = new Regex(@"_\d+$");

    // "Wolf_Attack_2.ogg" -> "wolf_attack"
    public static string GetSoundName(string fileName)
    {
        string name = Path.GetFileNameWithoutExtension(fileName).ToLowerInvariant().Replace(' ', '_');
        return VariantSuffix.Replace(name, "");
    }

    public static string GetSoundsFolder(ClientObjectLibrary library)
    {
        string libraryPath = AssetDatabase.GetAssetPath(library);
        return string.IsNullOrEmpty(libraryPath) ? null : Path.GetDirectoryName(libraryPath).Replace("\\", "/") + "/" + SoundsFolderName;
    }

    // Sound name -> its files (sorted), from the library's Sounds folder.
    public static Dictionary<string, List<string>> CollectSoundFiles(ClientObjectLibrary library)
    {
        Dictionary<string, List<string>> result = new Dictionary<string, List<string>>();
        string folder = GetSoundsFolder(library);
        if (folder == null || !Directory.Exists(folder))
        {
            return result;
        }

        foreach (string file in Directory.GetFiles(folder, "*", SearchOption.AllDirectories).OrderBy(item => item))
        {
            if (!SupportedExtensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
            {
                continue;
            }

            string name = GetSoundName(file);
            List<string> files;
            if (!result.TryGetValue(name, out files))
            {
                files = new List<string>();
                result[name] = files;
            }
            files.Add(file);
        }
        return result;
    }

    // "mod:/<bundle>/<name>" for every sound in the library's Sounds folder.
    public static HashSet<string> GetSoundPaths(ClientObjectLibrary library)
    {
        return new HashSet<string>(CollectSoundFiles(library).Keys.Select(name => ModPath(library.BundleName, name)));
    }

    public static string ModPath(string bundleName, string soundName)
    {
        return "mod:/" + bundleName + "/" + soundName;
    }

    // Starting settings for a sound, from its name.
    public static CustomSoundLibrary.SoundEntry DefaultsFor(string name)
    {
        CustomSoundLibrary.SoundEntry entry = new CustomSoundLibrary.SoundEntry() { Name = name, PitchMin = 0.95f, PitchMax = 1.05f };
        if (name.Contains("music"))
        {
            entry.Group = "music";
            entry.Is3D = false;
            entry.Loop = true;
            entry.PitchMin = entry.PitchMax = 1f;
        }
        else if (name.EndsWith("_environment") || name.Contains("ambience") || name.Contains("ambient"))
        {
            entry.Group = "ambience";
            entry.Loop = true;
            entry.MaxDistance = 20f;
            entry.PitchMin = entry.PitchMax = 1f;
        }
        else if (name.Contains("_footsteps"))
        {
            entry.Volume = 0.8f;
            entry.MaxDistance = 15f;
        }
        return entry;
    }

    // Rewrites the generated sound assets and the library prefab's CustomSoundLibrary. Does
    // nothing when the library has no Sounds folder.
    public static void UpdateSoundLibrary(ClientObjectLibrary library)
    {
        Dictionary<string, List<string>> soundFiles = CollectSoundFiles(library);
        string libraryPath = AssetDatabase.GetAssetPath(library);
        if (soundFiles.Count == 0 || string.IsNullOrEmpty(libraryPath))
        {
            return;
        }

        string generatedFolder = Path.GetDirectoryName(libraryPath).Replace("\\", "/") + "/" + GeneratedFolderName;
        if (AssetDatabase.IsValidFolder(generatedFolder))
        {
            AssetDatabase.DeleteAsset(generatedFolder);
        }
        AssetDatabase.CreateFolder(Path.GetDirectoryName(libraryPath).Replace("\\", "/"), GeneratedFolderName);

        // write all the byte copies first, then import them in one go
        Dictionary<string, List<string>> generatedPaths = new Dictionary<string, List<string>>();
        foreach (KeyValuePair<string, List<string>> sound in soundFiles)
        {
            List<string> paths = new List<string>();
            for (int i = 0; i < sound.Value.Count; i++)
            {
                string source = sound.Value[i];
                if (new FileInfo(source).Length > LargeFileBytes)
                {
                    Debug.LogWarning("[Mod sounds] " + source + " is over " + (LargeFileBytes / (1024 * 1024)) + " MB. Every player downloads it with the bundle; consider a lower bitrate .ogg.");
                }

                string target = generatedFolder + "/" + sound.Key + "__" + (i + 1) + Path.GetExtension(source).ToLowerInvariant() + ".bytes";
                File.Copy(source, target, true);
                paths.Add(target);
            }
            generatedPaths[sound.Key] = paths;
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        GameObject libraryRoot = PrefabUtility.LoadPrefabContents(libraryPath);
        try
        {
            CustomSoundLibrary soundLibrary = libraryRoot.GetComponent<CustomSoundLibrary>();
            if (soundLibrary == null)
            {
                soundLibrary = libraryRoot.AddComponent<CustomSoundLibrary>();
            }

            Dictionary<string, CustomSoundLibrary.SoundEntry> previous = (soundLibrary.Sounds ?? new CustomSoundLibrary.SoundEntry[0])
                .Where(item => item != null && !string.IsNullOrEmpty(item.Name))
                .GroupBy(item => item.Name)
                .ToDictionary(group => group.Key, group => group.First());

            List<CustomSoundLibrary.SoundEntry> entries = new List<CustomSoundLibrary.SoundEntry>();
            foreach (KeyValuePair<string, List<string>> sound in generatedPaths.OrderBy(item => item.Key))
            {
                CustomSoundLibrary.SoundEntry entry;
                if (!previous.TryGetValue(sound.Key, out entry))
                {
                    entry = DefaultsFor(sound.Key);
                }
                entry.Name = sound.Key;
                entry.Variants = sound.Value.Select(path => AssetDatabase.LoadAssetAtPath<TextAsset>(path)).Where(item => item != null).ToArray();
                entries.Add(entry);
            }

            soundLibrary.Sounds = entries.ToArray();
            PrefabUtility.SaveAsPrefabAsset(libraryRoot, libraryPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(libraryRoot);
        }
    }
}
