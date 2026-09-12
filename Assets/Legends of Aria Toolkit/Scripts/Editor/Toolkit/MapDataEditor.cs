using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using ShardsXML.WorldData;

[CustomEditor(typeof(MapData))]
public class MapDataInspector : Editor
{

    public override void OnInspectorGUI()
    {
        myTarget = (MapData)target;

        DrawDefaultInspector();

        EditorGUILayout.Separator();
        EditorGUILayout.HelpBox("Use LoA Toolkit -> Build Mod Package to build.", MessageType.Info);
    }    

    private MapData myTarget;
}
