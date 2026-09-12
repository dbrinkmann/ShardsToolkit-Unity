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

public class StringListSelectorPopup : EditorWindow
{
    public string[] StringList;
    public Action<string> CallbackFunc;

    public void OnGUI()
    {
        if (StringList == null || StringList.Length == 0)
        {
            Close();
            return;
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        for (uint index = 1; index < StringList.Length; index++)
        {
            if (GUILayout.Button(StringList[index]) && CallbackFunc != null)
            {
                CallbackFunc(StringList[index]);
                CallbackFunc = null;
                Close();
            }
        }

        EditorGUILayout.EndScrollView();
    }

    public void OnDestroy()
    {
        if (CallbackFunc != null)
        {
            CallbackFunc("");
        }
    }

    Vector2 scrollPos;
}
