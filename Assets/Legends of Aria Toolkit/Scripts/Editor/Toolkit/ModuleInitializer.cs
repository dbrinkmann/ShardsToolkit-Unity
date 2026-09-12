// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// Originally part of the Shards Engine Toolkit; released under the MIT License.
// See LICENSE in the repository root.
using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using System.Linq;
using ShardsXML.ObjectTemplate;

public class ModuleInitializer
{
    public List<LuaTableItem> LuaTableItems { get { return luaTableItems; } }

    public static ObjectTemplateScriptEngineComponent GetScriptComponent(ObjectTemplate curTemplate)
    {
        if (curTemplate == null)
            return null;

        if (curTemplate.ScriptEngineComponent == null || curTemplate.ScriptEngineComponent.Length == 0)
            return null;

        return curTemplate.ScriptEngineComponent[0];
    }

    public static Dictionary<string,ModuleInitializer> ParseInitializers(ObjectTemplate curTemplate, int modIndex)
    {
        Dictionary<string, ModuleInitializer>  curInitializers = new Dictionary<string, ModuleInitializer>();

        // first find the mobile script        
        ObjectTemplateScriptEngineComponent scriptComp = GetScriptComponent(curTemplate);
        if( scriptComp != null && scriptComp.LuaModule != null )
        {
            foreach(var module in scriptComp.LuaModule )
            {
                curInitializers.Add(module.Name, new ModuleInitializer(module, modIndex));
            }
        }

        return curInitializers;
    }

    public static void UpdateInitializers(Dictionary<string, ModuleInitializer> curInitializers, ObjectTemplate curTemplate)
    {
        if (curInitializers != null)
        {
            foreach (var initializer in curInitializers)
            {
                var moduleNode = curTemplate.ScriptEngineComponent[0].LuaModule.First(item => item.Name == initializer.Key);
                initializer.Value.WriteToTemplate(moduleNode);
            }
        }
    }

    public void ProcessModule(string scriptName,int modIndex)
    {
        string path = ModHelpers.GetScriptPath(scriptName + ".lua", modIndex);
        if (path == null || path == "" || !File.Exists(path))
            return;

        string pat = "require.*['\"](.*)['\"]";
        Regex r = new Regex(pat, RegexOptions.IgnoreCase);

        // TODO: handle recursive includes
        using (StreamReader sr = new StreamReader(path))
        {
            while (sr.Peek() >= 0)
            {
                string line = sr.ReadLine();
                if (line.Contains("require"))
                {
                    Match reqMatch = r.Match(line);
                    if (reqMatch.Success)
                    {
                        string reqScriptName = reqMatch.Groups[1].Value;
                        ProcessModule(reqScriptName, modIndex);
                    }
                }
            }
        }

        // check for xml file
        FileInfo xmlPath = new FileInfo(ModHelpers.GetScriptPath(scriptName + ".xml", modIndex));
        if( xmlPath.Exists )
        {
            ParseInitializerTemplate(xmlPath.FullName);
        }
    }

    public ModuleInitializer(ObjectTemplateScriptEngineComponentLuaModule moduleRef, int modIndex)
    {
        initializer = moduleRef.Initializer;

        // recursively read the intializers
        ProcessModule(moduleRef.Name,modIndex);

        // parse initializer string
        string initalizerStr = moduleRef.Initializer != null ? moduleRef.Initializer : "";

        List<KeyValuePair<string, string>> entries = LuaTableItem.GetLuaDictEntries(initalizerStr);
        foreach (var entry in entries)
        {
            LuaTableItem item = luaTableItems.Where(initItem => initItem.KeyName == entry.Key).FirstOrDefault();
            if (item != null)
            {
                item.ParseString(entry.Value);
            }
        }
    }

    public void WriteToTemplate(ObjectTemplateScriptEngineComponentLuaModule _moduleNode) 
    {
        if (luaTableItems.Count > 0)
        {
            string initStr = "";

            int writeCount = 0;
            int tabCount = 4;
            initStr = "\r\n" + new string('\t', tabCount) + "{\r\n";
            tabCount++;
            foreach (LuaTableItem tableItem in luaTableItems)
            {
                string itemStr = tableItem.Write(tabCount);
                if (itemStr != null)
                {
                    initStr = initStr + tableItem.Write(tabCount) + ",\r\n";
                    writeCount++;
                }
            }
            tabCount--;
            initStr = initStr + new string('\t', tabCount) + "}\r\n";
            tabCount--;
            initStr = initStr + new string('\t', tabCount);

            _moduleNode.Initializer = initStr;
        }
        else
        {
            _moduleNode.Initializer = initializer;
        }
    }    

    public void OnGUI()
    {
        EditorGUI.indentLevel++;

        if (luaTableItems.Count > 0)
        {
            foreach (LuaTableItem item in luaTableItems)
            {
                item.OnGUI();
            }
        }
        else
        {
            initializer = EditorGUILayout.TextArea(initializer, GUILayout.Height(100));
        }

        EditorGUI.indentLevel--;
    }

    private void ParseInitializerTemplate(string templatePath)
    {
        try
        {
            XmlSerializer ser = new XmlSerializer(typeof(InitializerXML.Initializer));
            using (XmlReader reader = XmlReader.Create(templatePath))
            {
                InitializerXML.Initializer curTemplate = ser.Deserialize(reader) as InitializerXML.Initializer;
                if (curTemplate.Items != null)
                {
                    foreach (InitializerXML.Key entry in curTemplate.Items)
                    {
                        LuaTableItem tableItem = LuaTableItem.Parse(new LuaTableItemFormat(entry));
                        if (!luaTableItems.Any(item => item.KeyName == tableItem.KeyName))
                        {
                            tableItem.Required = true;
                            luaTableItems.Add(tableItem);
                        }
                    }
                }
            }
        }
        catch( System.InvalidOperationException)
        {
            // ignore, just means this xml file is not for initializers
        }
    }

    List<LuaTableItem> luaTableItems = new List<LuaTableItem>();
    string initializer = "";
}