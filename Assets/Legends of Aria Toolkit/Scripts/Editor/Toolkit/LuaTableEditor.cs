// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// Originally part of the Shards Engine Toolkit; released under the MIT License.
// See LICENSE in the repository root.
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class LuaTableItemFormat
{
    public string Name;
    public bool Required;
    public string ValueType;
    public string KeyDesc = "";
    public string Desc = "";

    public LuaTableItemFormat[] SubItems;

    // Need to build xml with a dictdict reference 
    // Need to build xml with a desc
    public LuaTableItemFormat(InitializerXML.Key xmlFormat)
    {
        Name = xmlFormat.name;
        if (xmlFormat.required != null)
        {
            Required = bool.Parse(xmlFormat.required);
        }
        ValueType = xmlFormat.valueType;            
  
        if( xmlFormat.Key1 != null && xmlFormat.Key1.Length > 0 )
        {
            SubItems = xmlFormat.Key1.Select(item => new LuaTableItemFormat(item)).ToArray();
        }
    }

    public LuaTableItemFormat(DataTablesXML.Key xmlFormat)
    {
        Name = xmlFormat.name;
        if (xmlFormat.required != null)
        {
            Required = bool.Parse(xmlFormat.required);
        }
        ValueType = xmlFormat.valueType;
        KeyDesc = xmlFormat.keyDesc;
        Desc = xmlFormat.desc;

        if (xmlFormat.Key1 != null && xmlFormat.Key1.Length > 0)
        {
            SubItems = xmlFormat.Key1.Select(item => new LuaTableItemFormat(item)).ToArray();
        }
    }
}

public class LuaTableItem
{
    public static string StripComments(string inputStr)
    {
        int dashCount = 0;
        bool inComment = false;
        bool newLineFound = false;
        string outStr = "";
        foreach (char c in inputStr)
        {
            if (c == '-')
            {
                if (!inComment)
                {
                    dashCount = dashCount + 1;
                    if (dashCount == 2)
                    {
                        dashCount = 0;
                        inComment = true;
                        outStr = outStr.Substring(0, outStr.Length - 1);
                    }
                    else
                    {
                        outStr = outStr + c;
                    }
                }
                else if( newLineFound )
                {
                    dashCount = dashCount + 1;
                    inComment = false;
                    newLineFound = false;
                    outStr = outStr + c;
                }
            }
            else
            {
                dashCount = 0;
                if (inComment)
                {
                    if (c == '\n' || c == '\r')
                    {
                        newLineFound = true;
                    }
                    else if (newLineFound)
                    {
                        inComment = false;
                        newLineFound = false;
                        outStr = outStr + c;
                    }
                }
                else
                {
                    outStr = outStr + c;
                }
            }
        }

        return outStr;
    }

    // TODO: Fix this so they use the same function
    public static List<KeyValuePair<string, string>> GetLuaListEntries(string source)
    {
        source = StripComments(source);

        List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();
        bool inDict = false;
        int keyStartIndex = 0;
        int dictStartIndex = 0;
        int curIndex = 0;
        int nestCount = 0;
        int listIndex = 0;
        string key = "";
        string value = "";
        foreach (char c in source)
        {
            if (!inDict && c == '{')
            {
                inDict = true;
                dictStartIndex = curIndex + 1;
            }
            else if (inDict)
            {                
                if (c == '{')
                {
                    nestCount++;
                }
                else if (c == '}')
                {
                    nestCount--;
                }                
                
                // if nest count is negative then there is only one entry
                if ((nestCount == -1 && result.Count == 0) || (nestCount == 0 && c == ','))
                {
                    value = source.Substring(dictStartIndex, curIndex - dictStartIndex).Trim();
                    result.Add(new KeyValuePair<string, string>(listIndex.ToString(), value));
                    listIndex++;
                    dictStartIndex = curIndex + 2;
                }
            }

            curIndex++;
        }

        return result;
    }

    public static List<KeyValuePair<string, string>> GetLuaDictEntries(string source)
    {
        source = StripComments(source);

        List<KeyValuePair<string, string>> result = new List<KeyValuePair<string, string>>();
        bool inDict = false;
        bool inString = false;
        int keyStartIndex = 0;
        int dictStartIndex = -1;
        int nestCount = 0;
        string key = "";
        string value = "";
        for (int i=0; i<source.Length;i++)
        {
            char c = source[i];

            if (!inDict && source[i] == '{')
            {
                inDict = true;
                keyStartIndex = i + 1;
            }
            else if (inDict)
            {
                if (dictStartIndex == -1 && source[i] == '=')
                {
                    dictStartIndex = i + 1;
                    key = source.Substring(keyStartIndex, i - keyStartIndex).Trim();
                }
                else if(dictStartIndex != -1 && (source[i] == '\"'))
                {
                    if (inString)
                    {
                        if(source[i-1] != '\\')
                        {
                            inString = false;
                        }                        
                    }
                    else
                    {
                        inString = true;
                    }
                }
                else if (dictStartIndex != -1 && (source[i] == '{' || source[i] == '('))
                {
                    nestCount++;
                }
                else if (dictStartIndex != -1 && nestCount > 0 && (source[i] == '}' || source[i] == ')'))
                {
                    nestCount--;
                }
                else if (dictStartIndex != -1 && nestCount == 0 && inString == false && (source[i] == ',' || source[i] == '}'))
                {
                    value = source.Substring(dictStartIndex, i - dictStartIndex).Trim();
                    result.Add(new KeyValuePair<string, string>(key, value));
                    keyStartIndex = i + 2;
                    dictStartIndex = -1;
                }
            }
        }

        return result;
    }

    public static LuaTableItem Parse(LuaTableItemFormat entry)
    {
        switch (entry.ValueType)
        {
            case "dict":
                return new LuaTableDict(entry);
            case "dictdict":
                return new LuaTableDictDict(entry);
            case "string":
                return new LuaTableString(entry);
            case "number":
                return new LuaTableNumber(entry);
            case "bool":
                return new LuaTableBool(entry);
        }

        return null;
    }

    public string KeyName;
    public bool Required = false;
    public string Desc = "";
    public bool WriteKey = true;

    public LuaTableItem(LuaTableItemFormat entry)
    {
        KeyName = entry.Name;
        Required = entry.Required;
        Desc = entry.Desc;
    }

    public virtual bool OnGUI() { return true; }
    public virtual void ParseString(string initString) { }
    public virtual string Write(int tabCount) { return ""; }
}

public class LuaTableString : LuaTableItem
{
    public LuaTableString(LuaTableItemFormat entry)
        : base(entry)
    {
    }

    public override bool OnGUI()
    {
        bool returnVal = true;

        if (!Required)
        {
            EditorGUILayout.BeginHorizontal();
        }

        GUIContent content = new GUIContent(KeyName, Desc);
        Value = EditorGUILayout.TextField(content, Value, GUILayout.Width(400));

        if (!Required)
        {
            if (GUILayout.Button("Remove", GUILayout.Width(100)))
            {
                returnVal = false;
            }
            EditorGUILayout.EndHorizontal();
        }        

        return returnVal;
    }

    public override void ParseString(string initString)
    {
        Value = initString.Trim(new char[] { '\"' });
    }

    public override string Write(int tabCount)
    {
        string outValue = (Value != null) ? Value : "";
        if (WriteKey)
        {
            return new string('\t', tabCount) + KeyName + " = \"" + outValue + "\"";
        }
        else
        {
            return new string('\t', tabCount) + "\"" + outValue + "\"";
        }
    }

    public string Value;
}

public class LuaTableNumber : LuaTableItem
{
    public LuaTableNumber(LuaTableItemFormat entry)
        : base(entry)
    {
    }

    public override bool OnGUI()
    {
        bool returnVal = true;

        if (!Required)
        {
            EditorGUILayout.BeginHorizontal();
        }

        GUIContent content = new GUIContent(KeyName, Desc);
        Value = EditorGUILayout.TextField(content, Value, GUILayout.Width(400));

        if (!Required)
        {
            if (GUILayout.Button("Remove", GUILayout.Width(100)))
            {
                returnVal = false;
            }
            EditorGUILayout.EndHorizontal();
        }        

        return returnVal;
    }

    public override void ParseString(string initString)
    {
        Value = initString;
    }

    public override string Write(int tabCount)
    {
        string outValue = (Value != null) ? Value : "0";
        if (WriteKey)
        {
            return new string('\t', tabCount) + KeyName + " = " + outValue;
        }
        else
        {
            return new string('\t', tabCount) + outValue;
        }
    }

    public string Value = "0";
}

public class LuaTableBool : LuaTableItem
{
    public LuaTableBool(LuaTableItemFormat entry)
        : base(entry)
    {
    }

    public override bool OnGUI()
    {
        bool returnVal = true;

        if (!Required)
        {
            EditorGUILayout.BeginHorizontal();
        }

        GUIContent[] popupOptions = new GUIContent[] { new GUIContent("False"), new GUIContent("True") };

        int oldValue = Value ? 1 : 0;
        GUIContent content = new GUIContent(KeyName, Desc);
        int popupValue = EditorGUILayout.Popup(content, oldValue, popupOptions, GUILayout.Width(400));
        Value = popupValue == 0 ? false : true;

        if (!Required)
        {
            if (GUILayout.Button("Remove", GUILayout.Width(100)))
            {
                returnVal = false;
            }
            EditorGUILayout.EndHorizontal();
        }        

        return returnVal;
    }

    public override void ParseString(string initString)
    {
        Value = bool.Parse(initString);
    }

    public override string Write(int tabCount)
    {
        if (WriteKey)
        {
            return new string('\t', tabCount) + KeyName + " = " + Value.ToString().ToLower();
        }
        else
        {
            return new string('\t', tabCount) + Value.ToString().ToLower();
        }
    }

    public bool Value;
}

public class LuaTableDict : LuaTableItem
{    
    public List<LuaTableItem> entries = new List<LuaTableItem>();
    public List<bool> added = new List<bool>();

    public LuaTableDict(LuaTableItemFormat entry)
        : base(entry)
    {
        if (entry != null)
        {
            foreach (LuaTableItemFormat keyEntry in entry.SubItems)
            {
                LuaTableItem subEntry = LuaTableItem.Parse(keyEntry);
                entries.Add(subEntry);
                added.Add(subEntry.Required);
            }
        }
    }

    public override bool OnGUI()
    {
        bool returnVal = true;

        if (!Required)
        {
            EditorGUILayout.BeginHorizontal();
        }

        GUIContent content = new GUIContent(KeyName, Desc);
        isShown = EditorGUILayout.Foldout(isShown, content);

        if (!Required)
        {
            if (GUILayout.Button("Remove", GUILayout.Width(100)))
            {
                returnVal = false;
            }
            EditorGUILayout.EndHorizontal();    
        }                

        if (isShown)
        {
            EditorGUI.indentLevel++;

            // TODO: Need be able to able to remove optional entries
            for (int i = 0; i < entries.Count; i++)
            {
                if (added[i])
                {                    
                    if( !entries[i].OnGUI() )                        
                    {
                        added[i] = false;
                    }
                }
            }

            List<string> popupOptions = new List<string>();
            List<int> popupIndices = new List<int>();
            for (int i = 0; i < entries.Count; i++)
            {
                if (!added[i])
                {
                    popupOptions.Add(entries[i].KeyName);
                    popupIndices.Add(i);
                }
            }
            if (popupOptions.Any())
            {
                GUILayout.BeginHorizontal();
                popupValue = EditorGUILayout.Popup(popupValue, popupOptions.ToArray(), GUILayout.Width(200));
                GUILayout.Space(200);
                if (GUILayout.Button("Add", GUILayout.Width(100)))
                {
                    added[popupIndices[popupValue]] = true;
                    popupValue = 0;
                }
                GUILayout.EndHorizontal();
            }

            EditorGUI.indentLevel--;
        }

        return returnVal;
    }

    public override void ParseString(string initString)
    {
        List<KeyValuePair<string, string>> initEntries = LuaTableItem.GetLuaDictEntries(initString);
        foreach (var initEntry in initEntries)
        {
            int itemIndex = 0;
            foreach (var templateEntry in entries)
            {
                if (templateEntry.KeyName == initEntry.Key)
                {
                    templateEntry.ParseString(initEntry.Value);
                    added[itemIndex] = true;
                }
                itemIndex++;
            }
        }
    }

    public override string Write(int tabCount)
    {
        if (!added.Any(item => item == true))
        {
            return null;
        }

        string result;
        if (WriteKey)
        {
            result = new string('\t', tabCount) + KeyName + " = {\r\n";
        }
        else
        {
            result = new string('\t', tabCount) + "{\r\n";
        }
        tabCount++;
        int index = 0;
        foreach(var entry in entries)
        {
            if (added[index])
            {
                result = result + entry.Write(tabCount) + ",\r\n";
            }
            index++;
        }
        tabCount--;
        result = result + new string('\t', tabCount) + "}";

        return result;
    }

    bool isShown;
    int popupValue;
}

public class LuaTableDictDict : LuaTableItem
{
    public bool IsArrayType { get { return FormatEntry.SubItems[0].Name == null; } }

    public LuaTableDictDict(LuaTableItemFormat entry)
        : base(entry)
    {
        FormatEntry = entry;
    }

    public override bool OnGUI()
    {
        bool returnVal = true;

        if (!Required)
        {
            EditorGUILayout.BeginHorizontal();
        }

        GUIContent content = new GUIContent(KeyName, Desc);
        isShown = EditorGUILayout.Foldout(isShown, content);

        if (!Required)
        {
            if (GUILayout.Button("Remove", GUILayout.Width(100)))
            {
                returnVal = false;
            }
            EditorGUILayout.EndHorizontal();  
        }
         
        
        if (!isShown)
            return returnVal;

        EditorGUI.indentLevel++;

        string itemToRemove = null;
        foreach (var entry in dictDict)
        {
            if (!entry.Value.OnGUI())
            {
                itemToRemove = entry.Key;
            }
        }

        if (itemToRemove != null)
        {
            dictDict.Remove(itemToRemove);
        }
             
        ToolUtil.DrawSeparator();
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space((EditorGUI.indentLevel + 1) * 10);       

        if (!IsArrayType)
        {
            addKey = EditorGUILayout.TextField(addKey, GUILayout.Width(300));
        }
        else
        {
            addKey = dictDict.Count.ToString();
        }
         
        if (GUILayout.Button("Add " + FormatEntry.SubItems[0].Name, GUILayout.Width(300)))
        {
            dictDict.Add(addKey, LuaTableItem.Parse(FormatEntry.SubItems[0]));
            dictDict[addKey].KeyName = addKey;
            if( IsArrayType )
            {
                dictDict[addKey].WriteKey = false;                
            }
        }

        EditorGUILayout.EndHorizontal();

        EditorGUI.indentLevel--;

        return returnVal;
    }

    public LuaTableItem AddEntry(string key = null)
    {
        if(key == null)
        {
            key = dictDict.Count.ToString();
        }

        LuaTableItem newEntry = LuaTableItem.Parse(FormatEntry.SubItems[0]);
        dictDict.Add(key,newEntry);

        return newEntry;
    }

    public override void ParseString(string initString)
    {
        List<KeyValuePair<string,string>> initEntries;
        if (IsArrayType)
        {
            initEntries = LuaTableItem.GetLuaListEntries(initString);
        }
        else
        {
            initEntries = LuaTableItem.GetLuaDictEntries(initString);
        }
        foreach (var initEntry in initEntries)
        {
            LuaTableItem dictEntry = LuaTableItem.Parse(FormatEntry.SubItems[0]);
            dictEntry.ParseString(initEntry.Value);
            if (IsArrayType)
            {
                dictEntry.KeyName = dictDict.Count.ToString();
                dictEntry.WriteKey = false;
            }
            else
            {
                dictEntry.KeyName = initEntry.Key;
            }
            dictDict.Add(dictEntry.KeyName, dictEntry);
        }
    }

    public override string Write(int tabCount)
    {
        if( dictDict.Count == 0 )
        {
            return null;
        }

        string result = new string('\t', tabCount) + KeyName + "= {\r\n";
        tabCount++;
        foreach (var entry in dictDict)
        {
            result = result + entry.Value.Write(tabCount) + ",\r\n";
        }
        tabCount--;
        result = result + new string('\t', tabCount) + "}";

        return result;
    }

    public Dictionary<string, LuaTableItem> dictDict = new Dictionary<string, LuaTableItem>();
    private bool isShown = false;
    private string addKey = "";

    public LuaTableItemFormat FormatEntry;
}