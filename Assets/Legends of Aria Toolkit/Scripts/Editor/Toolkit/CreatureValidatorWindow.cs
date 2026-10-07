// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// See LICENSE in the repository root.
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Lists CreatureValidator results for the mod's client object libraries, or for the prefabs
// selected in the Project window.
public class CreatureValidatorWindow : EditorWindow
{
    List<CreatureValidator.Issue> issues = new List<CreatureValidator.Issue>();
    int checkedCount;
    bool hasRun;
    Vector2 scroll;

    [MenuItem("LoA Toolkit/Validate Creatures", false, 3)]
    public static void ShowWindow()
    {
        CreatureValidatorWindow window = GetWindow<CreatureValidatorWindow>();
        window.titleContent = new GUIContent("Validate Creatures");
        window.ValidateLibraries();
    }

    void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "Checks creature and mount prefabs (prefabs with a Mobile on the root) against what the game client expects. "
            + "Errors break the creature in game and stop Build Mod Package; warnings usually look wrong in game.",
            MessageType.Info);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Validate Mod Libraries"))
        {
            ValidateLibraries();
        }
        if (GUILayout.Button("Validate Selected Prefabs"))
        {
            ValidateSelection();
        }
        EditorGUILayout.EndHorizontal();

        if (!hasRun)
        {
            return;
        }

        int errors = issues.Count(item => item.Severity == CreatureValidator.Severity.Error);
        int warnings = issues.Count - errors;
        EditorGUILayout.LabelField("Checked " + checkedCount + " creature prefab(s): " + errors + " error(s), " + warnings + " warning(s).", EditorStyles.boldLabel);
        if (CreatureValidator.LoadFmodEventList() == null)
        {
            EditorGUILayout.HelpBox("Sound checks skipped: FMODAudioReference.txt was not found in the base game folder (LoA Toolkit/Settings).", MessageType.None);
        }

        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (IGrouping<GameObject, CreatureValidator.Issue> group in issues.GroupBy(item => item.Prefab))
        {
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            CreatureValidator.Issue first = group.First();
            EditorGUILayout.LabelField(group.Key.name + (first.ClientId > 0 ? "  (ClientId " + first.ClientId + ")" : ""), EditorStyles.boldLabel);
            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                Selection.activeObject = group.Key;
                EditorGUIUtility.PingObject(group.Key);
            }
            EditorGUILayout.EndHorizontal();

            foreach (CreatureValidator.Issue issue in group.OrderBy(item => item.Severity))
            {
                EditorGUILayout.HelpBox(issue.Message, issue.Severity == CreatureValidator.Severity.Error ? MessageType.Error : MessageType.Warning);
            }
        }
        EditorGUILayout.EndScrollView();
    }

    void ValidateLibraries()
    {
        issues.Clear();
        checkedCount = 0;
        foreach (GameObject libraryObject in (ToolUtil.Settings.CustomObjectLibraries ?? new GameObject[0]).Where(item => item != null))
        {
            ClientObjectLibrary library = libraryObject.GetComponent<ClientObjectLibrary>();
            if (library == null || library.ClientIdPrefabs == null)
            {
                continue;
            }

            checkedCount += library.ClientIdPrefabs.Skip(1).Count(item => item != null && item.GetComponent<Mobile>() != null);
            issues.AddRange(CreatureValidator.ValidateLibrary(library));
        }
        hasRun = true;
    }

    void ValidateSelection()
    {
        issues.Clear();
        checkedCount = 0;
        HashSet<string> fmodEvents = CreatureValidator.LoadFmodEventList();
        foreach (GameObject prefab in Selection.gameObjects.Where(item => EditorUtility.IsPersistent(item)))
        {
            if (prefab.GetComponent<Mobile>() != null)
            {
                checkedCount++;
            }
            issues.AddRange(CreatureValidator.ValidatePrefab(prefab, FindClientId(prefab), fmodEvents));
        }
        hasRun = true;
    }

    // The prefab's slot in one of the mod's libraries, or 0 if it is in none.
    static int FindClientId(GameObject prefab)
    {
        foreach (GameObject libraryObject in (ToolUtil.Settings.CustomObjectLibraries ?? new GameObject[0]).Where(item => item != null))
        {
            ClientObjectLibrary library = libraryObject.GetComponent<ClientObjectLibrary>();
            if (library != null && library.ClientIdPrefabs != null)
            {
                int index = System.Array.IndexOf(library.ClientIdPrefabs, prefab);
                if (index > 0)
                {
                    return index;
                }
            }
        }
        return 0;
    }
}
