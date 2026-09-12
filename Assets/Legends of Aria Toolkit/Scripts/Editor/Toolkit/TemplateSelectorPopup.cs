// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// Originally part of the Shards Engine Toolkit; released under the MIT License.
// See LICENSE in the repository root.
using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class TemplateSelectorPopup : EditorWindow 
{
    public Dictionary<string, List<string>> TemplateList;
    public Action<string, string> CallbackFunc;
    public GameObject ActiveObject;

    public void OnGUI()
    {
        if( ActiveObject != Selection.activeGameObject || TemplateList == null)
        {
            Close();
            return;
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        if (foldOuts == null || foldOuts.Length != TemplateList.Count)
        {
            foldOuts = new bool[TemplateList.Count];
        }

        int index = 0;
        foreach (string category in TemplateList.Keys.OrderBy(key => key))
        {
            foldOuts[index] = EditorGUILayout.Foldout(foldOuts[index], category);

            if(foldOuts[index])
            {
                foreach (var entry in TemplateList[category].OrderBy(item => item))
                {
                    if (GUILayout.Button(entry) && CallbackFunc != null)
                    {
                        CallbackFunc(category, entry);
                        CallbackFunc = null;
                        Close();
                    }
                }
            }

            index++;
        }

        EditorGUILayout.EndScrollView();
    }

    public void OnDestroy()
    {
        if(CallbackFunc != null)
        {
            CallbackFunc("", "");
        }
    }

    Vector2 scrollPos;
    bool[] foldOuts;    
}
