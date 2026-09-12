// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// Originally part of the Shards Engine Toolkit; released under the MIT License.
// See LICENSE in the repository root.
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Reflection;
using System.Text;
using CoreUtil;
using ShardsXML.CollisionData;
using ShardsXML.ObjectTemplate;
using CoreUtil.ShardEngineMath;

public class StaticCollisionEditor : EditorWindow
{
    public static Dictionary<string, bool> GroupExclusions = new Dictionary<string, bool>();

    public static readonly string c_CollisionTerrainPrefabPath = "Assets/Legends Of Aria Toolkit/EditorTools/CollisionTerrainPrefab.prefab";
    public static readonly string c_CollisionTerrainMatPath = "Assets/Legends Of Aria Toolkit/EditorTools/Materials/CollisionPaint.mat";
    public static readonly int c_ControlResolution = 2048;
    public static readonly float c_CollisionDetail = 0.25f;
    public static readonly int c_LoadBatch = 500;
    public static readonly float c_MinCollisionNodeSize = 0.5f;

    public Vector2 start;
    public Vector2 end;

    public bool initialized = false;

    public StaticCollisionEditor()
    {
        
    }

    public void OnFocus()
    {
    }    

    void OnGUI()
    {        
        EditorGUILayout.Space();

        if(!ToolUtil.ValidateSettings())
        {
            EditorGUILayout.LabelField("Invalid Settings. Please fix your paths by going to the 'Shards Toolkit/Settings' menu.");
            return;
        }
        else if(!Application.isPlaying && !ToolUtil.EditWhileStopped)
        {
            EditorGUILayout.LabelField("To begin, press play and select a map.");
            return;
        }
        else if (!MapData.Instance)
        {
            EditorGUILayout.LabelField("To begin, select a map.");
            return;
        }

        if (!initialized)
        {            
            initialized = true;
        }
        MapData mapDataComp = MapData.Instance;
        start = new Vector2((float)Math.Ceiling(-mapDataComp.MapExtents.x / 2), (float)Math.Ceiling(-mapDataComp.MapExtents.y / 2));
        end = new Vector2((float)Math.Ceiling(mapDataComp.MapExtents.x / 2), (float)Math.Ceiling(mapDataComp.MapExtents.y / 2));

        EditorGUILayout.LabelField("Current Mod: " + ToolUtil.Settings.CurrentModName, EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.Space();
        
        string collisionFileName = ShardsExtensionMethods.CombinePaths(ToolUtil.Settings.ModsPath, ToolUtil.Settings.CurrentModName, "mapdata", ToolUtil.currentScene, "StaticCollision.xml");                

        if (GUILayout.Button("New Static Collision"))
        {
            if (EditorUtility.DisplayDialog("WARNING", "This will reset any loaded static collision in the scene. Any unsaved work will be lost!", "Okay", "Cancel"))
            {
                NewStaticCollision();
                loadData.done = true;
            }
        }

        GUI.enabled = File.Exists(collisionFileName);

        if (GUILayout.Button("Load Static Collision"))
        {
            if (EditorUtility.DisplayDialog("WARNING", "This will reset any loaded static collision in the scene. Any unsaved work will be lost!", "Okay", "Cancel"))
            {
                BeginLoadStaticCollision(collisionFileName);
            }
        }

        GUI.enabled = (loadData != null && loadData.done);

        if (GUILayout.Button("Save Static Collision"))
        {
            if (EditorUtility.DisplayDialog("CONFIRM", "Save current static collision for mod " + ToolUtil.Settings.CurrentModName + "?" + "(Filename: " + collisionFileName + ")", "Okay", "Cancel"))
            {
                SaveStaticCollision(collisionFileName);                
            }
        }

        GUI.enabled = true;

        showSelected = EditorGUILayout.Toggle("Show Selected Area",showSelected);
        
        float newOpacity = opacity;
        newOpacity = EditorGUILayout.Slider("Collision Opacity:", newOpacity,0,1);
        if(newOpacity != opacity)
        {
            opacity = newOpacity;
            Material collisionMat = AssetDatabase.LoadAssetAtPath(c_CollisionTerrainMatPath, typeof(Material)) as Material;
            collisionMat.SetFloat("_Alpha",newOpacity);            
        }

        GUI.enabled = collisionObjs.Count > 0 && collisionObjs[0] != null;
        
        float newY = 0;
        if(GUI.enabled)
        {
            newY = collisionObjs[0].transform.position.y;
        }
        newY = EditorGUILayout.Slider("Collision Terrain Y:", newY, -500, 500);
        if (GUI.enabled && newY != collisionObjs[0].transform.position.y)
        {
            foreach(GameObject collisionObj in collisionObjs)
            {
                collisionObj.transform.position = new Vector3(collisionObj.transform.position.x, newY, collisionObj.transform.position.z);
            }
        }

        if (loadData != null && !loadData.done)
        {                  
            try
            {
                if(loadData.nodesToLoad != null && loadData.nodesToLoad.Count > 0)
                {
                    int itemsToLoad = Math.Min(c_LoadBatch,loadData.nodesToLoad.Count);
                    while(itemsToLoad > 0)
                    {
                        LoadElement(loadData.nodesToLoad.Dequeue());
                        itemsToLoad--;
                    }
                }
            }
            catch(Exception e)
            {
                loadData = null;
                EditorUtility.ClearProgressBar();
                throw e;
            }
            
            if (loadData.nodesToLoad != null && loadData.nodesToLoad.Count > 0)
            {
                int nodesDone = loadData.nodeCount - loadData.nodesToLoad.Count;
                float progress = (float)nodesDone / loadData.nodeCount;
                if (EditorUtility.DisplayCancelableProgressBar(
                        "Loading Static Collision",
                        "Loading Static Node " + (nodesDone) + " / " + loadData.nodeCount,
                        progress))
                {
                    CompleteLoad();
                    loadData = null;
                    EditorUtility.ClearProgressBar();
                }
            }
            else
            {
                try
                {
                    CompleteLoad();
                }
                catch (Exception e)
                {
                    loadData = null;
                    EditorUtility.ClearProgressBar();
                    throw e;
                }

                loadData.done = true;
                EditorUtility.ClearProgressBar();
            }
        }

        

        EditorGUILayout.Space();        
    }

    public void Update()
    {
        // This is necessary to make the framerate normal for the editor window.
        Repaint();
    }   
    
    private void ResetStaticCollision()
    {
        if(staticCollisionScene != null && staticCollisionScene.isLoaded)
        {
            EditorSceneManager.CloseScene(staticCollisionScene,true);
            terrainDataGrid = null;
            collisionObjs.Clear();
            terrainDataAlphaMaps = null;
        }
    }

    private void NewStaticCollision()
    {
        ResetStaticCollision();

        Scene activeScene = EditorSceneManager.GetActiveScene();
        staticCollisionScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

        int terrainSize = (int)(c_ControlResolution * c_CollisionDetail);

        int terrainStartX = (int)Math.Floor((start.x) / (double)terrainSize) * terrainSize;
        int terrainStartZ = (int)Math.Floor((start.y) / (double)terrainSize) * terrainSize;
        int terrainEndX = (int)Math.Ceiling((end.x) / (double)terrainSize) * terrainSize;
        int terrainEndZ = (int)Math.Ceiling((end.y) / (double)terrainSize) * terrainSize;

        int terrainCountX = (terrainEndX - terrainStartX) / terrainSize;
        int terrainCountZ = (terrainEndZ - terrainStartZ) / terrainSize;

        GameObject collisionTerrainPrefab = AssetDatabase.LoadAssetAtPath(c_CollisionTerrainPrefabPath, typeof(GameObject)) as GameObject;
        terrainDataGrid = new TerrainData[terrainCountX, terrainCountZ];
        terrainDataAlphaMaps = new float[terrainCountX, terrainCountZ][,,];

        int terrainIndexX = 0;
        for (int terX = terrainStartX; terX < terrainEndX; terX += terrainSize)
        {
            int terrainIndexZ = 0;
            for (int terZ = terrainStartZ; terZ < terrainEndZ; terZ += terrainSize)
            {
                GameObject terrGameObj = CopyTerrain(collisionTerrainPrefab, new Vector3(terX, 100, terZ));
                collisionObjs.Add(terrGameObj);
                TerrainData terrData = terrGameObj.GetComponent<Terrain>().terrainData;
                terrGameObj.name = "Collision|X" + terX + "|Z" + terZ + "|";
                terrGameObj.GetComponent<TerrainCollider>().terrainData = terrData;
                terrainDataGrid[terrainIndexX, terrainIndexZ] = terrData;
                terrainDataAlphaMaps[terrainIndexX, terrainIndexZ] = terrData.GetAlphamaps(0, 0, c_ControlResolution, c_ControlResolution);
                terrainIndexZ++;
            }
            terrainIndexX++;
        }

        EditorSceneManager.SetActiveScene(activeScene);

        loadData = new LoadData() { terrainStartX = terrainStartX, terrainStartZ = terrainStartZ, terrainEndX = terrainEndX, terrainEndZ = terrainEndZ, terrainSize = terrainSize };
    }

    private void BeginLoadStaticCollision(string collisionFileName)
    {
        NewStaticCollision();

        XmlSerializer ser = new XmlSerializer(typeof(StaticCollisionData));

        using (XmlReader reader = XmlReader.Create(collisionFileName))
        {
            StaticCollisionData dataRoot = ser.Deserialize(reader) as StaticCollisionData;

            if(dataRoot.Items != null && dataRoot.Items.Length > 0)
            {
                var lookup = dataRoot.Items.
                    Select(nodeData => {
                        float x1 = float.Parse(nodeData.x1);
                        float x2 = float.Parse(nodeData.x2);
                        float y1 = float.Parse(nodeData.y1);
                        float y2 = float.Parse(nodeData.y2);
                        return new UnityStaticCollisionNode() {
                            nodePos = UnityUtil.ConvertToClientPos2(new Vec3(x1, 0, y1)),
                            nodeSize = x2 - x1,
                        };
                    });                    
                    /*.ToLookup(nodeData => {                   
                        return (nodeData.nodePos.x < start.x
                            || nodeData.nodePos.y < start.y
                            || (nodeData.nodePos.x + nodeData.nodeSize) > end.x
                            || (nodeData.nodePos.y + nodeData.nodeSize) > end.y);
                    });*/                

                loadData.done = false;
                loadData.nodesToLoad = new Queue<UnityStaticCollisionNode>(lookup);
                loadData.nodeCount = loadData.nodesToLoad.Count;
            }            
            else
            {
                loadData.done = true;
            }
        }
    }

    private bool LoadElement(UnityStaticCollisionNode nodeData)
    {
        float xPos = nodeData.nodePos.x - (loadData.terrainStartX);
        float zPos = nodeData.nodePos.y - (loadData.terrainStartZ);
        int terIndexX = (int)Math.Floor(xPos / loadData.terrainSize);
        int terIndexZ = (int)Math.Floor(zPos / loadData.terrainSize);
        int terOffsetX = (int)((xPos % loadData.terrainSize) / c_CollisionDetail);
        int terOffsetZ = (int)((zPos % loadData.terrainSize) / c_CollisionDetail);

        int nodeControlSize = (int)(nodeData.nodeSize / c_CollisionDetail);
        float[,,] alphaMap = terrainDataAlphaMaps[terIndexX, terIndexZ];
        for (int x = terOffsetX; x < terOffsetX + nodeControlSize; x += 1)
        {
            for (int y = terOffsetZ; y < terOffsetZ + nodeControlSize; y += 1)
            {
                alphaMap[y, x, 0] = 0.0f;
                alphaMap[y, x, 1] = 1.0f;
            }
        }

        return true;
    }

    private void CompleteLoad()
    {
        for(int x=0;x < terrainDataGrid.GetLength(0);x++)
        {
            for (int z = 0; z < terrainDataGrid.GetLength(1); z++)
            {
                TerrainData terrData = terrainDataGrid[x,z];
                if(terrData == null)
                {
                    continue;
                }
                float[,,] alphaMap = terrainDataAlphaMaps[x, z];
                if(alphaMap == null)
                {
                    continue;
                }
                terrainDataGrid[x,z].SetAlphamaps(0,0,terrainDataAlphaMaps[x,z]);
            }
        }
    }

    private StaticCollisionDataCollisionNode NodeFromAARect(AARect2 curRect, int terrainX, int terrainZ)
    {
        curRect = new AARect2(terrainX + (curRect.Left * c_CollisionDetail), terrainZ + (curRect.Top * c_CollisionDetail), curRect.Width * c_CollisionDetail, curRect.Height * c_CollisionDetail);

        return new StaticCollisionDataCollisionNode() { height = float.MaxValue.ToString(), x1 = curRect.Left.ToString(), x2 = curRect.Right.ToString(), y1 = curRect.Top.ToString(), y2 = curRect.Bottom.ToString() };
    }

    private void ProcessAlphaMapRect(AARect2 curRect, int terrainX, int terrainZ, float[,,] alphaMap, List<StaticCollisionDataCollisionNode> allNodes)
    {        
        // node too small just add
        if (curRect.Width <= (c_MinCollisionNodeSize / c_CollisionDetail))
        {
            allNodes.Add(NodeFromAARect(curRect, terrainX, terrainZ));
        }
        else
        {
            bool allBlocked = true;
            bool allPassable = true;
            for (int x = (int)curRect.Left; x < (int)curRect.Right; x++)
            {
                for (int z = (int)curRect.Top; z < (int)curRect.Bottom; z++)
                {
                    if (alphaMap[z, x, 1] > 0)
                    {
                        allPassable = false;
                    }
                    else
                    {
                        allBlocked = false;
                    }
                }
            }

            // every location is blocked so we can add this node
            if (allBlocked)
            {
                allNodes.Add(NodeFromAARect(curRect, terrainX, terrainZ));
            }
            // if there is some blocked areas then we need to subdivide
            else if (!allPassable)
            {
                foreach (AARect2 subRect in curRect.Subdivide())
                {
                    ProcessAlphaMapRect(subRect, terrainX, terrainZ, alphaMap, allNodes);
                }
            }
            // otherwise the whole area is passable and we dont need to do anything
        }
    }

    private bool SaveStaticCollision(string collisionFileName)
    {        
        List<StaticCollisionDataCollisionNode> allNodes = new List<StaticCollisionDataCollisionNode>();

        int terrainIndexX = 0;
        for (int terX = loadData.terrainStartX; terX < loadData.terrainEndX; terX += loadData.terrainSize)
        {
            int terrainIndexZ = 0;
            for (int terZ = loadData.terrainStartZ; terZ < loadData.terrainEndZ; terZ += loadData.terrainSize)
            {
                float[,,] alphaMap = terrainDataGrid[terrainIndexX,terrainIndexZ].GetAlphamaps(0,0,c_ControlResolution,c_ControlResolution);

                AARect2 root = new AARect2(0,0, c_ControlResolution, c_ControlResolution);
                ProcessAlphaMapRect(root,terX,terZ,alphaMap,allNodes);

                terrainIndexZ++;
            }
            terrainIndexX++;
        }

        StaticCollisionData dataRoot = new StaticCollisionData() { Items = allNodes.ToArray() };        
        XmlSerializer ser = new XmlSerializer(typeof(StaticCollisionData));
        ToolUtil.WriteXML(ser, collisionFileName, dataRoot);        

        string scenePath = Path.GetDirectoryName(EditorSceneManager.GetActiveScene().path);
        string clientFileName = ShardsExtensionMethods.CombinePaths(scenePath, EditorSceneManager.GetActiveScene().name + "StaticData.xml");
        ToolUtil.WriteXML(ser, clientFileName, dataRoot);
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(clientFileName);

        MapData mapDataObj = GameObject.FindObjectOfType<MapData>();
        if (mapDataObj == null)
        {
            GameObject mapDataGameObj = new GameObject();
            mapDataGameObj.name = "MapData";
            mapDataObj = mapDataGameObj.AddComponent<MapData>();
        }

        SerializedObject mapDataSO = new SerializedObject(mapDataObj);
        mapDataSO.FindProperty("StaticCollisionData").objectReferenceValue = textAsset;
        mapDataSO.ApplyModifiedProperties();

        EditorUtility.DisplayDialog("CONFIRMATION", "Static Collision saved. (Filename: " + collisionFileName + ")", "Okay");

        return true;
    }           

    private GameObject CopyTerrain(GameObject terrainPrefab,Vector3 position)
    {        
        Terrain terrain = terrainPrefab.GetComponent<Terrain>();
        TerrainData oldTerrainData = terrain.terrainData;
        TerrainData terrainData = new TerrainData();

        PropertyInfo[] srcFields = oldTerrainData.GetType().GetProperties(
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty);

        PropertyInfo[] destFields = terrainData.GetType().GetProperties(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty);

        foreach (var property in srcFields)
        {
            var dest = destFields.FirstOrDefault(x => x.Name == property.Name);
            if (dest != null && dest.CanWrite && dest.Name.Contains("splatPrototypes") == false)
                dest.SetValue(terrainData, property.GetValue(oldTerrainData, null), null);
        }

        GameObject newTerrainGameObject = (GameObject)Instantiate(terrainPrefab, position, new Quaternion());
        Terrain newTerrain = newTerrainGameObject.GetComponent<Terrain>();
        newTerrain.terrainData = terrainData;

        return newTerrainGameObject;
    }    

    [DrawGizmo(GizmoType.NotInSelectionHierarchy)]
    static void RenderCustomGizmo(Transform objectTransform, GizmoType gizmoType)
    {
        if (window == null || !window.initialized)
            return;

        /*Gizmos.DrawLine(new Vector3(window.start.x,0,window.start.y), new Vector3(window.end.x, 0, window.start.y));
        Gizmos.DrawLine(new Vector3(window.end.x, 0, window.start.y), new Vector3(window.end.x, 0, window.end.y));
        Gizmos.DrawLine(new Vector3(window.end.x, 0, window.end.y), new Vector3(window.start.x, 0, window.end.y));
        Gizmos.DrawLine(new Vector3(window.start.x, 0, window.end.y), new Vector3(window.start.x, 0, window.start.y));*/

        if(showSelected && Selection.activeGameObject != null && Selection.activeGameObject.GetComponent<Terrain>() )
        {
            Gizmos.color = Color.yellow;
            Vector3 terrainPos = Selection.activeTransform.position;
            Gizmos.DrawLine(new Vector3(terrainPos.x, 0, terrainPos.z), new Vector3(terrainPos.x + 512, 0, terrainPos.z));
            Gizmos.DrawLine(new Vector3(terrainPos.x + 512, 0, terrainPos.z), new Vector3(terrainPos.x + 512, 0, terrainPos.z + 512));
            Gizmos.DrawLine(new Vector3(terrainPos.x + 512, 0, terrainPos.z + 512), new Vector3(terrainPos.x, 0, terrainPos.z + 512));
            Gizmos.DrawLine(new Vector3(terrainPos.x, 0, terrainPos.z + 512), new Vector3(terrainPos.x, 0, terrainPos.z));
        }
    }

    // Maintain static reference to window
    void OnEnable()
    {
        window = this;
    }
    void OnDisable()
    {
        window = null;
    }

    private static StaticCollisionEditor window;
    
    private TerrainData[,] terrainDataGrid;
    private float[,][,,] terrainDataAlphaMaps;

    private class UnityStaticCollisionNode
    {
        public Vector2 nodePos;
        public float nodeSize;
    }

    private class LoadData
    {
        public Queue<UnityStaticCollisionNode> nodesToLoad;
        public int nodeCount;
        //public IEnumerable<UnityStaticCollisionNode> filteredNodeArray;
        public int terrainStartX;
        public int terrainStartZ;
        public int terrainEndX;
        public int terrainEndZ;
        public int terrainSize;
        public bool done;
    }
    private static LoadData loadData;
    private static Scene staticCollisionScene;
    private static List<GameObject> collisionObjs = new List<GameObject>();

    private static bool showSelected;
    private static float opacity;
}