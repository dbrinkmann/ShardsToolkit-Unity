// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// Originally part of the Shards Engine Toolkit; released under the MIT License.
// See LICENSE in the repository root.
using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using CoreUtil;
using CoreUtil.ShardEngineMath;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.Text.RegularExpressions;
using ShardsXML.ObjectTemplate;

public partial class ToolUtil
{    
    public static string currentScene
    {
        get
        {
            if(Application.isPlaying)
            {
                return Application.loadedLevelName;
            }
            else 
            {
                string[] path = EditorApplication.currentScene.Split('/');
                string fileName = path[path.Length - 1];
                if (fileName == "")
                {
                    return "";
                }
                return fileName.Substring(0, fileName.Length - 6);
            }            
        }
    } 
    
    private static ToolkitSettingsData settingsInstance;
    public static ToolkitSettingsData Settings
    {
        get 
        { 
            if(settingsInstance == null)
            {
                settingsInstance = ToolkitSettingsData.Load();
            }
            return settingsInstance;
        }
        set { settingsInstance = value; }
    }

    public static bool ValidateSettings()
    {
        if(!Directory.Exists(Settings.ModsPath)
            || !Directory.Exists(Path.Combine(Settings.BasePath,"templates"))
            || !Directory.Exists(Path.Combine(Settings.BasePath, "scripts"))
            || !Directory.Exists(Path.Combine(Settings.BasePath, "mapdata"))
            || !Directory.Exists(Settings.BasePath))
        {
            return false;
        }

        return true;
    }

    public static bool ValidateModName()
    {
        string curModName = ToolUtil.Settings.CurrentModName;
        Regex r = new Regex("^[a-zA-Z0-9 _-]*$");
        if (curModName == null || curModName == "" || !r.IsMatch(curModName))
        {
            return false;
        }

        return true;
    }

    public static GameObject[] GetCustomObjectLibrary(string libraryName)
    {
        if (ToolUtil.Settings.CustomObjectLibraries != null)
        {
            GameObject libraryGameObj = ToolUtil.Settings.CustomObjectLibraries
                .FirstOrDefault(item => item.GetComponent<ClientObjectLibrary>() != null && item.GetComponent<ClientObjectLibrary>().BundleName == libraryName);
            if(libraryGameObj != null)
            {
                return libraryGameObj.GetComponent<ClientObjectLibrary>().ClientIdPrefabs;
            }
        }
        return null;
    }

    public static GameObject GetPrefabFromLibrary(GameObject[] library, UInt32 clientId)
    {
        if (library == null || clientId >= library.Length)
        {
            return null;
        }

        return library[clientId];
    }

    // Looks up the client prefab for a clientId, preferring the default object library and falling
    // back to the mod's custom libraries. The default library is not distributed with the public
    // toolkit, so a null result is expected rather than exceptional.
    public static GameObject ResolvePrefabForClientId(UInt32 clientId)
    {
        GameObject prefab = GetPrefabFromLibrary(ClientIdLibrary, clientId);
        if (prefab != null)
        {
            return prefab;
        }

        if (Settings.CustomObjectLibraries != null)
        {
            foreach (GameObject libraryObj in Settings.CustomObjectLibraries)
            {
                if (libraryObj == null) continue;

                ClientObjectLibrary library = libraryObj.GetComponent<ClientObjectLibrary>();
                if (library == null) continue;

                prefab = GetPrefabFromLibrary(library.ClientIdPrefabs, clientId);
                if (prefab != null) return prefab;
            }
        }

        return null;
    }

    // Stand-in for a clientId with no prefab in any available library, so authoring and export
    // still work without the base game's object library installed.
    public static GameObject CreateClientIdPlaceholder(string templateId)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = string.IsNullOrEmpty(templateId) ? "Placeholder" : templateId;

        return marker;
    }

    // Unity likes to screw up 0's and turn them into super low numbers which uglies up the XML
    public static float LimitPrecision(float _value)
    {
        return (float)((int)(_value * 1000.0f)) / 1000.0f;
    }

    public static string GenerateDynamicCreateString(string templateName, Transform objTrans, Vector3 relPos)
    {
        //Debug.Log("CreateString Y for obj: " + objTrans.gameObject.name + " at: " + objTrans.transform.position + " surfaceY: " + UnityUtil.GetStaticSurfaceY(objTrans.transform.position));
        Vector3 clientPos = objTrans.position;
        Vec3 serverPos;
        if (relPos != Vector3.zero)
        {
            clientPos = clientPos - relPos;
            serverPos = UnityUtil.CreateFromVector3(clientPos);
        }
        else
        {
            serverPos = UnityUtil.ConvertToServerPos(clientPos);
        }

        //DFB HACK: Some prefabs have stuff create with negative y values which leads them to be created under the map.
        //This fixes it
        if (serverPos.Y < 0)
        {
            serverPos.Y = 0;
        }

        Vector3 scale = objTrans.localScale;
        Vector3 rotation = objTrans.localRotation.eulerAngles;
        string createString = templateName + " "
                                + LimitPrecision(serverPos.X) + " " + LimitPrecision(serverPos.Y) + " " + LimitPrecision(serverPos.Z) + " "
                                + LimitPrecision(rotation.x) + " " + LimitPrecision(rotation.y) + " " + LimitPrecision(rotation.z) + " "
                                + LimitPrecision(scale.x) + " " + LimitPrecision(scale.y) + " " + LimitPrecision(scale.z);
        return createString;
    }

    public static void WriteXML(XmlSerializer ser, string path, object output, bool bSanitizeTable = false)
    {
        XmlWriterSettings settings = new XmlWriterSettings();
        settings.IndentChars = "\t";
        settings.Indent = true;

        string dirPath = Path.GetDirectoryName(path);
        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }
        //using (TextWriter writer = new StreamWriter(path))
        using(var sw = new UTF8StringWriter())
        using (XmlWriter xw = XmlWriter.Create(sw, settings))
        {
            ser.Serialize(xw, output);
            string xml = sw.ToString();
            if (bSanitizeTable)
            {
                xml = xml.Replace("\"{\"", "{\"").Replace("\"}\"", "\"}").Replace("}\"", "}");
            }
            //Debug.Log("XML: " + xml);
            File.WriteAllText(path, xml);
            //string xml = writer.ToString();
            //Console.WriteLine(xml);
        } 
    }
    
    public class UTF8StringWriter : StringWriter
    {
        public override Encoding Encoding
        {
            get
            {
                return Encoding.UTF8;
            }
        }
    }

    public static GameObject DuplicateObject(GameObject go)
    {
        UnityEngine.Object prefabRoot = PrefabUtility.GetCorrespondingObjectFromSource(go);

        if (prefabRoot != null)
            return PrefabUtility.InstantiatePrefab(prefabRoot) as GameObject;
        else
            return GameObject.Instantiate(go) as GameObject;
    }

    public static bool ParseVecStr(string vecStr, out Vec3 result)
    {
        string[] locArgs = vecStr.Trim('(', ')').Split(',');
        if (locArgs.Length < 2 || locArgs.Length > 3)
        {
            result = Vec3.Zero;
            return false;
        }

        if (locArgs.Length == 2)
        {
            float x = 0, z = 0;
            if (!float.TryParse(locArgs[0], out x)
                || !float.TryParse(locArgs[1], out z))
            {
                result = Vec3.Zero;
                return false;
            }
            result = new Vec3(x, 0.0f, z);
        }
        else
        {
            float x = 0, y = 0, z = 0;
            if (!float.TryParse(locArgs[0], out x)
                || !float.TryParse(locArgs[1], out y)
                || !float.TryParse(locArgs[2], out z))
            {
                result = Vec3.Zero;
                return false;
            }
            result = new Vec3(x, y, z);
        }

        return true;
    }

    static public void DrawSeparator()
    {
        GUILayout.Space(12f);

        if (Event.current.type == EventType.Repaint)
        {
            Texture2D tex = EditorGUIUtility.whiteTexture;
            Rect rect = GUILayoutUtility.GetLastRect();
            GUI.color = new Color(0f, 0f, 0f, 0.25f);
            GUI.DrawTexture(new Rect(0f, rect.yMin + 6f, Screen.width, 4f), tex);
            GUI.DrawTexture(new Rect(0f, rect.yMin + 6f, Screen.width, 1f), tex);
            GUI.DrawTexture(new Rect(0f, rect.yMin + 9f, Screen.width, 1f), tex);
            GUI.color = Color.white;
        }
    }

    static public int HexToDecimal(char ch)
    {
        switch (ch)
        {
            case '0': return 0x0;
            case '1': return 0x1;
            case '2': return 0x2;
            case '3': return 0x3;
            case '4': return 0x4;
            case '5': return 0x5;
            case '6': return 0x6;
            case '7': return 0x7;
            case '8': return 0x8;
            case '9': return 0x9;
            case 'a':
            case 'A': return 0xA;
            case 'b':
            case 'B': return 0xB;
            case 'c':
            case 'C': return 0xC;
            case 'd':
            case 'D': return 0xD;
            case 'e':
            case 'E': return 0xE;
            case 'f':
            case 'F': return 0xF;
        }
        return 0xF;
    }

    static public int ColorToInt(Color c)
    {
        int retVal = 0;
        retVal |= Mathf.RoundToInt(c.r * 255f) << 24;
        retVal |= Mathf.RoundToInt(c.g * 255f) << 16;
        retVal |= Mathf.RoundToInt(c.b * 255f) << 8;
        retVal |= Mathf.RoundToInt(c.a * 255f);
        return retVal;
    }

    static public string DecimalToHex(int num)
    {
        num &= 0xFFFFFF;
        return num.ToString("X6");
    }

    static public Color ParseColor(string text, int offset)
    {
        int r = (HexToDecimal(text[offset]) << 4) | HexToDecimal(text[offset + 1]);
        int g = (HexToDecimal(text[offset + 2]) << 4) | HexToDecimal(text[offset + 3]);
        int b = (HexToDecimal(text[offset + 4]) << 4) | HexToDecimal(text[offset + 5]);
        float f = 1f / 255f;
        return new Color(f * r, f * g, f * b);
    }

    static public string EncodeColor(Color c)
    {
        int i = 0xFFFFFF & (ColorToInt(c) >> 8);
        return DecimalToHex(i);
    }

    static public UnityEditorInternal.ReorderableList GetObjVarEditor(List<ShardsObjVar> objVarList, bool isSeedObj=false)
    {
        UnityEditorInternal.ReorderableList varList = new UnityEditorInternal.ReorderableList(objVarList, typeof(ShardsObjVar), true, true, true, true);

        varList.drawHeaderCallback = (Rect rect) =>
        {
            EditorGUI.LabelField(rect, "Custom Object Variables");
        };
        varList.drawElementCallback =
            (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                ShardsObjVar element = varList.list[index] as ShardsObjVar;
                rect.y += 2;
                rect.height -= 4;
                EditorGUIUtility.labelWidth = 50;
                element.Name = EditorGUI.TextField(new Rect(rect.x, rect.y, rect.width / 3, rect.height), "Name", element.Name);
                if (isSeedObj)
                {
                    element.Type = (ShardsObjVar.ObjVarType)EditorGUI.EnumPopup(new Rect(rect.x + (rect.width / 3), rect.y, 50, rect.height), (ShardsObjVar.RestrictedType)element.Type);
                }
                else
                {
                    element.Type = (ShardsObjVar.ObjVarType)EditorGUI.EnumPopup(new Rect(rect.x + (rect.width / 3), rect.y, 50, rect.height), element.Type);
                }

                element.Type = (ShardsObjVar.ObjVarType)EditorGUI.EnumPopup(new Rect(rect.x + (rect.width / 3), rect.y, 50, rect.height), element.Type);
                Rect valueRect = new Rect(rect.x + (rect.width / 3) + 50, rect.y, rect.width - (rect.width / 3) - 50, rect.height);
                if (element.Type == ShardsObjVar.ObjVarType.String)
                {
                    element.StrValue = EditorGUI.TextField(valueRect, element.StrValue);
                }
                else if(element.Type == ShardsObjVar.ObjVarType.Loc)
                {
                    element.LocValue = EditorGUI.Vector3Field(valueRect,"", element.LocValue);
                }
                else if(element.Type == ShardsObjVar.ObjVarType.Boolean)
                {
                    string[] popupOptions = new string[] { "False", "True" };                    
                    int oldValue = element.BoolValue ? 1 : 0;
                    int popupValue = EditorGUI.Popup(valueRect,oldValue, popupOptions);
                    element.BoolValue = (popupValue == 0) ? false : true;
                }
                else if (element.Type == ShardsObjVar.ObjVarType.Number)
                {
                    element.DoubleValue = EditorGUI.FloatField(valueRect, (float)element.DoubleValue);
                }
            };

        return varList;
    }

    public static void CopyToClipboard(string contents)
    {
        TextEditor te = new TextEditor();
        te.content = new GUIContent(contents);
        te.SelectAll();
        te.Copy();
    }

    public static string ConvertBoundsToTagString(Rect3 _bounds)
    {
        string boundStr = "[";
        foreach (var point in _bounds.Points)
        {
            boundStr = boundStr + point;
        }

        boundStr = boundStr + "]";

        return boundStr;
    }
    
    public static void TagTerrains()
    {
        // make sure all terrains are properly tagged!
        foreach(Terrain terr in GameObject.FindObjectsOfType<Terrain>())
        {
            terr.gameObject.tag = "Terrain";
        }
    }
}