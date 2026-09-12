using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;
using CoreUtil.ShardEngineMath;
using System.Text.RegularExpressions;
using ShardsXML.TagDefinitions;

[CustomEditor(typeof(ClientObjectLibrary))]
public class ClientObjectLibraryEditor : Editor
{
    private ClientObjectLibrary myTarget;
    private SerializedProperty clientIdPrefabs;
    private const int maxArraySize = 50;
    private int page = 0;
    private int totalPages = 0;
    
    private void OnEnable()
    {
        page = 0;
        clientIdPrefabs = serializedObject.FindProperty("ClientIdPrefabs");
        RefreshTotalPages();
    }

    private void RefreshTotalPages()
    {
        int arraySize = clientIdPrefabs.arraySize;
        totalPages = arraySize / maxArraySize;
        if (arraySize % maxArraySize > 0)
        {
            totalPages++;
        }

        page = Mathf.Clamp(page, 0, totalPages - 1);
    }
    
    public override void OnInspectorGUI()
    {
        myTarget = (ClientObjectLibrary)target;
        DrawDefaultInspector();
        EditorGUILayout.Separator();

        EditorGUILayout.LabelField("Library", EditorStyles.boldLabel);
        serializedObject.Update();
        
        // Show default inspector property editor
        //DrawDefaultInspector();

        //EditorGUILayout.HelpBox("Make sure you leave entry 0 empty (None) in your library. Id 0 is reserved as an invalid id.",MessageType.Warning);
        if (clientIdPrefabs.arraySize == 0)
        {
            clientIdPrefabs.InsertArrayElementAtIndex(0);
            RefreshTotalPages();
        }

        if (clientIdPrefabs.GetArrayElementAtIndex(0).objectReferenceValue != null)
        {
            clientIdPrefabs.GetArrayElementAtIndex(0).objectReferenceValue = null;
        }
        
        for (int i = 1; i < maxArraySize; ++i)
        {
            int absoluteIdx = (page * maxArraySize) + i;
            if (absoluteIdx >= clientIdPrefabs.arraySize)
            {
                break;
            }
			
            SerializedProperty arrayElem = clientIdPrefabs.GetArrayElementAtIndex(absoluteIdx);
            GameObject target = arrayElem.objectReferenceValue as GameObject;

            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            Object gObj = EditorGUILayout.ObjectField("Element " + absoluteIdx, target, typeof(GameObject));
            if (EditorGUI.EndChangeCheck())
            {
                clientIdPrefabs.GetArrayElementAtIndex(absoluteIdx).objectReferenceValue = gObj;
            }

            if (GUILayout.Button("X", GUILayout.ExpandWidth(false)))
            {
                clientIdPrefabs.GetArrayElementAtIndex(absoluteIdx).objectReferenceValue = null;
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.Separator();

        if (GUILayout.Button("Add Field"))
        {
            clientIdPrefabs.InsertArrayElementAtIndex(clientIdPrefabs.arraySize);
            RefreshTotalPages();
            page = totalPages - 1;
        }

        if (clientIdPrefabs.arraySize > 1 && GUILayout.Button("Remove Last Field"))
        {
            clientIdPrefabs.arraySize = clientIdPrefabs.arraySize - 1;
            RefreshTotalPages();
        }
        
        EditorGUILayout.Separator();
			
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Prev Page"))
        {
            if (page > 0)
            {
                page--;
            }
            else
            {
                page = totalPages - 1;
            }	
        }

        EditorGUI.BeginChangeCheck();
        GUILayout.Label("Page:", GUILayout.ExpandWidth(false));
        string pageNum = EditorGUILayout.TextField("", (page + 1).ToString(), GUILayout.ExpandWidth(false), GUILayout.Width(100));
        if (EditorGUI.EndChangeCheck())
        {
            int newPage = page;
            if (int.TryParse(pageNum, out newPage))
            {
                newPage--;
            }

            page = Mathf.Clamp(newPage, 0, totalPages - 1);
        }
		
        GUILayout.Label("/" + totalPages, GUILayout.ExpandWidth(false));
		
        //GUILayout.Label("Page " + (page + 1));
		
        if (GUILayout.Button("Next Page"))
        {
            if (page < totalPages - 1)
            {
                page++;
            }
            else
            {
                page = 0;
            }
        }
        
        EditorGUILayout.EndHorizontal();
        
        serializedObject.ApplyModifiedProperties();
        
        EditorGUILayout.Separator();
        EditorGUILayout.HelpBox("Use LoA Toolkit -> Build Mod Package to build.", MessageType.Info);
    }
}
