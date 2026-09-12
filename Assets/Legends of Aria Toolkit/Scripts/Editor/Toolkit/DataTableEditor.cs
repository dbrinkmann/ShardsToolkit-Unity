// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// Originally part of the Shards Engine Toolkit; released under the MIT License.
// See LICENSE in the repository root.
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Linq;

public class DataFile
{
    public DataFile(string _sourceFilename, DataTablesXML.DataTables _tables, DataTablesEditor _parentEditor)
    {
        parentEditor = _parentEditor;
        sourceFilename = _sourceFilename;

        foreach (DataTablesXML.Key key in _tables.Items)
        {
            tableItems.Add(LuaTableItem.Parse(new LuaTableItemFormat(key)));
        }
        
        string sourcePath = ModHelpers.GetScriptPath(sourceFilename, _parentEditor.ModIndex);
        string sourceString = File.ReadAllText(sourcePath);
        string dictString = "";
        for(int i=0;i<sourceString.Length;i++)
        {
            if( sourceString[i] == '{' )
            {
                dataTableName = sourceString.Substring(0, i).Trim(new char[] { ' ', '\t', '=' });
                dictString = sourceString.Substring(i, sourceString.Length - i);
                break;
            }
        }

        List<KeyValuePair<string, string>> luaTableData = LuaTableItem.GetLuaDictEntries(dictString);
        foreach(var entry in luaTableData)
        {
            LuaTableItem tableItem = tableItems.First(item => item.KeyName == entry.Key);
            tableItem.ParseString(entry.Value);
        }
    }

    public void OnGUI()
    {
        isShown = EditorGUILayout.Foldout(isShown, dataTableName);
        EditorGUI.indentLevel++;

        foreach (LuaTableItem item in tableItems)
        {
            item.OnGUI();
        }

        EditorGUI.indentLevel--;

        EditorGUILayout.Space();

        if (GUILayout.Button("Save "+sourceFilename, GUILayout.Width(300)))
        {
            if (parentEditor.ModIndex == 0 && !ToolUtil.CanWriteToDefault)
            {
                EditorUtility.DisplayDialog("ERROR", "You can not make changes to the default ruleset.", "Okay");
            }
            Write();
        }

        ToolUtil.DrawSeparator();
    }

    public void Write()
    {
        if( parentEditor.ModIndex >= 0 )
        {
            if (EditorUtility.DisplayDialog("Save Data Table", "Do you wish to save " + dataTableName + " to file " + sourceFilename + " in mod " + ModHelpers.GetModName(parentEditor.ModIndex) + "?", "Save", "Cancel"))
            {
                string outStr = dataTableName + " = {\r\n";
                foreach (var item in tableItems)
                {
                    outStr = outStr + item.Write(1) + ",\r\n";
                }
                outStr = outStr + "}";

                string sourcePath = Path.Combine(parentEditor.DataTablePath, sourceFilename);
                File.WriteAllText(sourcePath, outStr);

                parentEditor.OnDataFileSaved();
            }
        }

    }

    List<LuaTableItem> tableItems = new List<LuaTableItem>();
    string sourceFilename;
    string dataTableName;
    bool isShown;
    DataTablesEditor parentEditor;
}

public class DataTablesEditor : EditorWindow
{    
    public int ModIndex { get { return popupModIndex - 1; } }

    public string DataTablePath { get 
    {
        return Path.Combine(ModHelpers.GetRundirPath(ModIndex), "scripts");
    } }

    public string DataFormatXMLPath
    {
        get
        {
            return Path.Combine(ModHelpers.GetRundirPath(), "scripts");
        }
    }

    public void Initialize()
    {        
        popupMods = (new string[] { "New", "Default" }).Concat(ModHelpers.RefreshList()).ToArray();
        popupModIndex = 0;
    }

    public void LoadData()
    {
        dataFiles.Clear();

        DirectoryInfo di = new DirectoryInfo(DataFormatXMLPath);
        foreach (var fileInfo in di.GetFiles().Where(fileInfo => fileInfo.Name.Contains("data") && fileInfo.Extension == ".xml"))
        {
            XmlSerializer ser = new XmlSerializer(typeof(DataTablesXML.DataTables));
            using (XmlReader reader = XmlReader.Create(fileInfo.FullName))
            {
                DataTablesXML.DataTables tables = null;
                try
                {
                    tables = ser.Deserialize(reader) as DataTablesXML.DataTables;
                }
                catch(System.Exception e)
                {
                    Debug.LogError("Exception parsing " + fileInfo.FullName);
                    throw e;
                }
                string sourceName = fileInfo.Name.Substring(0, fileInfo.Name.Length - 4) + ".lua";
                dataFiles.Add(new DataFile(sourceName, tables, this));
            }
        }
    }

    public void OnGUI()
    {
        EditorGUILayout.Space();

        if (!ToolUtil.ValidateSettings())
        {
            EditorGUILayout.LabelField("Invalid Settings. Please fix your paths by going to the 'LoA Toolkit/Settings' menu.");
            return;
        }

        EditorGUILayout.BeginHorizontal();
        popupModIndex = EditorGUILayout.Popup("Mod", popupModIndex, popupMods);
        if (popupModIndex != 0)
        {
            if (GUILayout.Button("Load", GUILayout.Width(100)))
            {
                LoadData();
                currentMod = ModHelpers.GetModName(ModIndex);
            }
        }

        EditorGUILayout.EndHorizontal();

        if(popupModIndex == 0)
        {
            EditorGUILayout.Space();

            GUILayout.BeginHorizontal();
            newModName = EditorGUILayout.TextField(newModName, GUILayout.Width(300));
            if (GUILayout.Button("Create Mod", GUILayout.Width(200)))
            {
                ModHelpers.CreateMod(newModName);
                popupMods = (new string[] { "New", "Default" }).Concat(ModHelpers.RefreshList()).ToArray();

                int newIndex = System.Array.IndexOf(popupMods, newModName);
                if (newIndex != -1)
                {
                    popupModIndex = newIndex;
                }
            }
            GUILayout.EndHorizontal();
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Current: " + currentMod);

        EditorGUILayout.Space();
        ToolUtil.DrawSeparator();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        foreach(var dataFile in dataFiles)
        {
            dataFile.OnGUI();
        }

        EditorGUILayout.EndScrollView();
    }

    public void OnDataFileSaved()
    {
        currentMod = ModHelpers.GetModName(ModIndex);
    }

    List<DataFile> dataFiles = new List<DataFile>();
    Vector2 scrollPos = Vector2.zero;

    private string[] popupMods;
    private int popupModIndex = 0;
    private string newModName = "";
    private string currentMod;
}
