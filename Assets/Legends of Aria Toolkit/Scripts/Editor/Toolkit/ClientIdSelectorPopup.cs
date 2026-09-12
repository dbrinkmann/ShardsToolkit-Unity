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

public class ClientIdSelectorPopup : EditorWindow 
{
    public string[] ClientIdList;
    public Action<uint> CallbackFunc;

    public void OnGUI()
    {
        if(ClientIdList == null)
        {
            ClientIdList = ToolUtil.ClientIdLibrary.Select(item => item == null ? "" : item.name).ToArray();
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        for(uint index=1;index<ClientIdList.Length;index++)
        {           
            if (GUILayout.Button(ClientIdList[index]) && CallbackFunc != null)
            {
                CallbackFunc(index);
                CallbackFunc = null;
                Close();
            }
        }

        EditorGUILayout.EndScrollView();
    }

    public void OnDestroy()
    {
        if(CallbackFunc != null)
        {
            CallbackFunc(0);
        }
    }

    Vector2 scrollPos;
}
