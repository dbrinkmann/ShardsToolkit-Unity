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
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Text;
using CoreUtil;
using ShardsXML.WorldData;
using CoreUtil.ShardEngineMath;

public class PathEditor : EditorWindow
{    
    public PathEditor()
    {
        popupMods = (new string[] { "Default" }).Concat(ModHelpers.RefreshList()).ToArray();        
        modIndex = 0;
    }

    public void OnGUI()
    {
        EditorGUILayout.Space();

        if (!ToolUtil.ValidateSettings())
        {
            EditorGUILayout.LabelField("Invalid Settings. Please fix your paths by going to the 'LoA Toolkit/Settings' menu.");
            return;
        }
        else if (!Application.isPlaying && !ToolUtil.EditWhileStopped)
        {
            EditorGUILayout.LabelField("To begin, press play and select a map.");
            return;
        }
        else if (!MapData.Instance)
        {
            EditorGUILayout.LabelField("To begin, press select a map.");
            return;
        }

        GameObject curObj = Selection.activeGameObject;

        GameObject pathRoot = GameObject.Find("Paths");
        if (pathRoot == null)
        {
            pathRoot = new GameObject();
            pathRoot.name = "Paths";
        }        

        EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        int newModIndex = EditorGUILayout.Popup("Mod", modIndex, popupMods);
        if (newModIndex != modIndex)
        {
            modIndex = newModIndex;
        }

        EditorGUILayout.Space();

        string worldDataFileName = ShardsExtensionMethods.CombinePaths(ModHelpers.GetRundirPath(modIndex), "mapdata", ToolUtil.currentScene, "WorldData.xml");

        GUI.enabled = File.Exists(worldDataFileName);

        if (GUILayout.Button("Load Paths"))
        {
            if (EditorUtility.DisplayDialog("WARNING", "This will reset all paths in the scene. Any unsaved work will be lost!", "Okay", "Cancel"))
            {
                LoadPaths(worldDataFileName);
            }
        }

        GUI.enabled = true;

        if (GUILayout.Button("Save Paths"))
        {
            if (modIndex == 0 && !ToolUtil.CanWriteToDefault)
            {
                EditorUtility.DisplayDialog("ERROR", "You can not make changes to the default ruleset.", "Okay");
            }
            else if (EditorUtility.DisplayDialog("CONFIRM", "Save current paths for mod " + ModHelpers.GetModName(modIndex) + "? (Filename: " + worldDataFileName + ")", "Okay", "Cancel"))
            {
                SavePaths(worldDataFileName);
            }
        }
        if (GUILayout.Button("Reset Paths"))
        {
            if (EditorUtility.DisplayDialog("WARNING", "This will reset all paths in the scene. Any unsaved work will be lost!", "Okay", "Cancel"))
            {
                ResetPaths();
            }
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Flatten All"))
        {
            FlattenAll();
        }
        if (GUILayout.Button("Create New Path"))
        {
            if(IsPathCategory(curObj))
            {
                PathVisualizer pathObj = CreateNewPath("New Path", curObj.name);
                Selection.activeGameObject = pathObj.gameObject;
            }
            else
            {
                PathVisualizer pathObj = CreateNewPath("New Path", null);
                Selection.activeGameObject = pathObj.gameObject;
            }            
        }
        if( GUILayout.Button("Create New Category"))
        {
            GameObject newCategory = new GameObject();
            newCategory.name = "New Path Category";
            newCategory.transform.parent = pathRoot.transform;
            Selection.activeGameObject = newCategory;
        }

        ToolUtil.DrawSeparator();

        EditorGUILayout.Space();
        
        if(IsPathNode(curObj))
        {
            EditorGUILayout.LabelField("Path Node Selected", EditorStyles.boldLabel);

            GUI.enabled = false;
            EditorGUILayout.TextField(new GUIContent("Node Index: ", "Automatically generated."), curObj.name);
            GUI.enabled = true;

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Insert Before"))
            {
                GameObject newNode = InsertPathNode(curObj, true);
                Selection.activeGameObject = newNode;
            }
            if (GUILayout.Button("Insert After"))
            {
                GameObject newNode = InsertPathNode(curObj, false);
                Selection.activeGameObject = newNode;
            }
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Move To View"))
            {
                var view = SceneView.currentDrawingSceneView;
                if (view != null)
                {
                    curObj.transform.position = view.pivot;
                }
            }
            if (GUILayout.Button("Delete Node"))
            {
                DeletePathNode(curObj);
            }
        }
        else if(IsPath(curObj))
        {
            EditorGUILayout.LabelField("Path Selected", EditorStyles.boldLabel);

            PathVisualizer pathObj = curObj.GetComponent<PathVisualizer>();

            GUI.enabled = false;
            EditorGUILayout.TextField(new GUIContent("Path Name: ", "Rename in heirarchy"), curObj.name);
            GUI.enabled = true;

            pathObj.pathColor = EditorGUILayout.ColorField(new GUIContent("Visual Color: ", "Rename in heirarchy"), pathObj.pathColor);

            if (GUILayout.Button("Add Node"))
            {
                GameObject newNode = AddPathNode(pathObj);
                Selection.activeGameObject = newNode;
            }
            if (GUILayout.Button("Delete Path"))
            {
                if (EditorUtility.DisplayDialog("WARNING", "This will delete the entire path. Any unsaved work will be lost!", "Okay", "Cancel"))
                {
                    GameObject.DestroyImmediate(curObj);
                }
            }
            if (GUILayout.Button("Flatten Path"))
            {
                FlattenPath(pathObj);
            }
            if (GUILayout.Button("Duplicate Path"))
            {
                GameObject newPath = ToolUtil.DuplicateObject(curObj);
                newPath.name = curObj.name + "1";
                newPath.transform.parent = curObj.transform.parent;
                Selection.activeGameObject = newPath;
            }
        }
        else if(IsPathCategory(curObj))
        {
            EditorGUILayout.LabelField("Path Category Selected", EditorStyles.boldLabel);

            GUI.enabled = false;
            EditorGUILayout.TextField(new GUIContent("Path Category: ", "Rename in heirarchy"), curObj.name);
            GUI.enabled = true;
        }
        else
        {
            GUIStyle centertext = new GUIStyle(GUI.skin.label);
            centertext.alignment = TextAnchor.MiddleCenter;
            EditorGUILayout.LabelField("Create New or Select a Path", centertext);          
        }
    }

    public void Update()
    {
        // This is necessary to make the framerate normal for the editor window.
        Repaint();
    }

    private bool IsPathNode(GameObject curObj)
    {
        if (curObj == null) return false;

        return curObj.transform.parent != null && curObj.transform.parent.GetComponent<PathVisualizer>() != null;
    }

    private bool IsPath(GameObject curObj)
    {
        if (curObj == null) return false;

        return curObj.GetComponent<PathVisualizer>() != null;
    }

    private bool IsPathCategory(GameObject curObj)
    {
        if (curObj == null || curObj.transform.parent == null) return false;

        GameObject pathRoot = GameObject.Find("Paths");
        return curObj.transform.parent.gameObject == pathRoot;
    }

    private PathVisualizer CreateNewPath(string pathName, string category)
    {
        GameObject pathsRoot = GameObject.Find("Paths");

        Transform catTrans = pathsRoot.transform.Find(category);
        if (catTrans != null)
        {
            pathsRoot = catTrans.gameObject;
        }
        else
        {
            GameObject newObjectRoot = new GameObject();
            newObjectRoot.transform.parent = pathsRoot.transform;
            newObjectRoot.name = category;
            pathsRoot = newObjectRoot;
        }

        GameObject newPath = new GameObject();
        newPath.transform.parent = pathsRoot.transform;
        newPath.name = pathName;

        return newPath.AddComponent<PathVisualizer>(); 
    }

    private GameObject AddPathNode(PathVisualizer pathObj)
    {
        GameObject prevNode = null;
        if (pathObj.PathNodes.Length > 0)
        {
            prevNode = pathObj.PathNodes.Last().gameObject;

            return InsertPathNode(prevNode, false);
        }
        else
        {
            var view = SceneView.currentDrawingSceneView;

            GameObject newNode = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            newNode.transform.parent = pathObj.transform;
            if (view != null)
            {
                newNode.transform.position = view.pivot;
            }            
            newNode.name = "1";

            return newNode;
        }        
    }

    private GameObject InsertPathNode(GameObject pathNode, bool isBefore)
    {
        Transform pathTrans = pathNode.transform.parent;
        if (pathTrans == null)
        {
            Debug.Log("Invalid path node! Not child of path object.");
            return null;
        }

        PathVisualizer pathObj = pathTrans.GetComponent<PathVisualizer>();
        if (pathObj == null)
        {
            Debug.Log("Invalid path object! Missing PathVisualizer component.");
            return null;
        }

        uint indexToAdd;

        if (!uint.TryParse(pathNode.name, out indexToAdd))
        {
            Debug.Log("Invalid path node name! Do no manually rename path nodes!!");
            return null;
        }
        if (!isBefore)
        {
            indexToAdd++;
        }

        foreach (Transform pathNodeTrans in pathObj.PathNodes)
        {
            uint curIndex;
            if (!uint.TryParse(pathNodeTrans.name, out curIndex))
            {
                Debug.Log("Invalid path node name! Do no manually rename path nodes!!");
                GameObject.DestroyImmediate(pathNodeTrans.gameObject);
            }

            if (curIndex >= indexToAdd)
            {
                pathNodeTrans.name = (curIndex + 1).ToString();
            }
        }

        GameObject newNode = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        newNode.transform.parent = pathObj.transform;
        newNode.transform.position = pathNode.transform.position;
        newNode.name = indexToAdd.ToString();        

        return newNode;
    }

    private void Flatten(GameObject pathNode)
    {
        float newY = UnityUtil.GetStaticSurfaceY(pathNode.transform.position);        
        pathNode.transform.position = new Vector3(pathNode.transform.position.x, newY, pathNode.transform.position.z);
    }

    private void FlattenPath(PathVisualizer pathObj)
    {
        foreach(Transform child in pathObj.transform)
        {
            Flatten(child.gameObject);
        }
    }

    private void FlattenAll()
    {
        GameObject pathsRoot = GameObject.Find("Paths");
        foreach(PathVisualizer pathObj in pathsRoot.transform.GetComponentsInChildren<PathVisualizer>())
        {
            FlattenPath(pathObj);
        }
    }

    private void DeletePathNode(GameObject pathNode)
    {
        Transform pathTrans = pathNode.transform.parent;
        if(pathTrans == null)
        {
            Debug.Log("Invalid path node! Not child of path object.");
            return;
        }

        PathVisualizer pathObj = pathTrans.GetComponent<PathVisualizer>();
        if (pathObj == null)
        {
            Debug.Log("Invalid path object! Missing PathVisualizer component.");
            return;
        }

        uint indexToDelete;
        if(!uint.TryParse(pathNode.name, out indexToDelete))
        {
            Debug.Log("Invalid path node name! Do no manually rename path nodes!!");
            GameObject.DestroyImmediate(pathNode);
            return;
        }

        GameObject.DestroyImmediate(pathNode);

        foreach(Transform pathNodeTrans in pathObj.PathNodes)
        {
            uint curIndex;
            if (!uint.TryParse(pathNodeTrans.name, out curIndex))
            {
                Debug.Log("Invalid path node name! Do no manually rename path nodes!!");
                GameObject.DestroyImmediate(pathNodeTrans.gameObject);
            }

            if(curIndex > indexToDelete)
            {
                pathNodeTrans.name = (curIndex - 1).ToString();
            }
        }
    }

    private void LoadPaths(string filePath)
    {
        ToolUtil.TagTerrains();

        ResetPaths();

        GameObject pathsRoot = GameObject.Find("Paths");
        if (pathsRoot == null)
        {
            pathsRoot = new GameObject();
            pathsRoot.name = "Paths";
        }

        XmlSerializer ser = new XmlSerializer(typeof(WorldData));

        using (XmlReader reader = XmlReader.Create(filePath))
        {
            WorldData worldData = ser.Deserialize(reader) as WorldData;

            foreach(var entry in worldData.Items)
            {
                WorldDataPathDefinition pathDef = entry as WorldDataPathDefinition;
                if(pathDef != null)
                {
                    PathVisualizer newPath = CreateNewPath(pathDef.PathName, pathDef.Category);

                    // attempt to parse color
                    if(pathDef.VisualColor != null && pathDef.VisualColor.Length > 0)
                    {
                        float r, g, b;
                        if(float.TryParse(pathDef.VisualColor[0].r, out r)
                            && float.TryParse(pathDef.VisualColor[0].g, out g)
                            && float.TryParse(pathDef.VisualColor[0].b, out b))
                        {
                            newPath.pathColor = new Color(r/255, g/255, b/255);
                        }
                    }

                    if (pathDef.PathNode.Any(item =>
                        {
                            uint pathIndex;
                            return !uint.TryParse(item.nodeName, out pathIndex);                            
                        }))
                    {
                        Debug.Log("Invalid path node name! Path node names are always incrementing numbers!! PathName: " + pathDef.PathName);
                        return;
                    }

                    foreach(var pathNodeEntry in pathDef.PathNode.OrderBy(item => uint.Parse(item.nodeName)))
                    {
                        GameObject pathNode = AddPathNode(newPath);
                        Vec3 serverPosition;
                        if(!ToolUtil.ParseVecStr(pathNodeEntry.Value, out serverPosition))
                        {
                            Debug.Log("Invalid path node loc! PathName: " + pathDef.PathName + " Node: " + pathNodeEntry.nodeName + " Loc: " + pathNodeEntry.Value);
                            return;
                        }
                        
                        Vector3 localPos = UnityUtil.ConvertToClientPos(serverPosition);
                        pathNode.transform.position = localPos;
                    }
                }
            }
        }

        FlattenAll();
    }

    private bool SavePaths(string filePath)
    {
        ToolUtil.TagTerrains();

        GameObject pathsRoot = GameObject.Find("Paths");
        if (pathsRoot == null)
        {
            EditorUtility.DisplayDialog("ERROR", "Seed Object Root has been deleted!", "Okay");
            return false;
        }

        PathVisualizer[] pathObjs = pathsRoot.GetComponentsInChildren<PathVisualizer>();
        if (pathObjs.Length == 0)
        {
            EditorUtility.DisplayDialog("ERROR", "No Paths to save!", "Okay");
            return false;
        }

        XmlSerializer ser = new XmlSerializer(typeof(WorldData));

        WorldData worldData;
        using (XmlReader reader = XmlReader.Create(filePath))
        {
            worldData = ser.Deserialize(reader) as WorldData;
        }

        // remove all old paths
        worldData.Items = worldData.Items.Where(item => (item as WorldDataPathDefinition) == null).ToArray();
        List<WorldDataPathDefinition> newPaths = new List<WorldDataPathDefinition>();

        foreach (PathVisualizer pathObj in pathObjs)
        {
            WorldDataPathDefinition newPathDef = new WorldDataPathDefinition();
            newPathDef.PathName = pathObj.name;
            newPathDef.VisualColor = new WorldDataPathDefinitionVisualColor[] { new WorldDataPathDefinitionVisualColor() 
                { 
                    r=(pathObj.pathColor.r*255.0f).ToString(), 
                    g=(pathObj.pathColor.g*255.0f).ToString(), 
                    b=(pathObj.pathColor.b*255.0f).ToString(), 
                } };

            // Get highest parent with no path script
            string category = null;
            Transform catChild = pathObj.transform;
            while (catChild.parent != pathsRoot.transform)
            {                
                catChild = catChild.parent;
            }
            if (catChild != pathsRoot && catChild.GetComponent<PathVisualizer>() == null)
            {
                category = catChild.gameObject.name;
            }
            newPathDef.Category = category;

            int nodeIndex = 1;
            List<WorldDataPathDefinitionPathNode> pathNodes = new List<WorldDataPathDefinitionPathNode>();
            foreach (Transform nodeTrans in pathObj.PathNodes)
            {
                pathNodes.Add(new WorldDataPathDefinitionPathNode() { nodeName=nodeIndex.ToString(), Value=UnityUtil.ConvertToServerPos(nodeTrans.position).ToString() });
                nodeIndex++;
            }
            newPathDef.PathNode = pathNodes.ToArray();

            newPaths.Add(newPathDef);
        }

        worldData.Items = worldData.Items.Concat(newPaths.Select(item => (object)item)).ToArray();
        ToolUtil.WriteXML(ser, filePath, worldData);

        EditorUtility.DisplayDialog("CONFIRMATION", "Paths saved. (Filename: " + filePath + ")", "Okay");

        return true;
    }

    private void ResetPaths()
    {
        GameObject pathsRoot = GameObject.Find("Paths");
        if (pathsRoot != null)
        {
            GameObject.DestroyImmediate(pathsRoot);
        }
        pathsRoot = new GameObject();
        pathsRoot.name = "Paths";
    }

    private string[] popupMods;
    private int modIndex = 0;
}
