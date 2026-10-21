// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// See LICENSE in the repository root.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Renders a custom map's world map: the zoomable, top-down map players see in the game's map
/// window on your server. The map is shot from straight above in 512 m squares, cut into 256 px
/// tiles and halved down into zoom levels.
///
/// The output is written ready for an asset bundle, to Assets/UI/Maps/Tiles/&lt;Scene&gt;/:
/// manifest.json, and every tile as &lt;level&gt;/&lt;x&gt;_&lt;y&gt;.jpg.bytes (bundles carry
/// binary files as TextAssets). Build Mod Package ships that folder as the
/// "&lt;Scene&gt;_worldmap" client object bundle. The Region Editor adds RegionData.xml (area
/// names) to the same folder.
///
/// The world is the scene's MapData: MapExtents wide, centred on the origin, as the server reads
/// it. Lighting is the map's own: one sun from the north-west, no fog, no local lights.
/// </summary>
public static class WorldMapRenderer
{
    public const string TilesRoot = "Assets/UI/Maps/Tiles";
    public const int CellSize = 512;
    public const int PixelsPerUnit = 2;
    public const int TileSize = 256;
    const int JpgQuality = 85;
    // Each shot is this much bigger on every side and cropped back, so shadows and anti-aliasing
    // at its edges match its neighbours'.
    const int CellMargin = 32;
    const float CameraHeight = 1500f;
    const int MsaaSamples = 4;

    public static readonly Color MapWater = new Color(0.20f, 0.36f, 0.45f);
    // Anywhere nothing is drawn is open sea, so the ocean runs on past the land.
    static readonly Color Background = MapWater;
    static readonly Quaternion MapSun = Quaternion.Euler(55f, 135f, 0f);

    // Layers that never belong on a map (see LoA Toolkit > Utils > Generate LOA Required Tags and Layers).
    const int WaterLayer = 4;
    const int UiLayer = 5;
    const int MinimapIncludeLayer = 27;
    const int MinimapBuildingsLayer = 28;

    /// <summary>The square of the world one rendered map covers, in whole 512 m cells.</summary>
    public struct Layout
    {
        public int CellXMin, CellXMax, CellZMin, CellZMax;
        public float MinX { get { return CellXMin * CellSize; } }
        public float MaxX { get { return (CellXMax + 1) * CellSize; } }
        public float MinZ { get { return CellZMin * CellSize; } }
        public float MaxZ { get { return (CellZMax + 1) * CellSize; } }
        public int Shots { get { return (CellXMax - CellXMin + 1) * (CellZMax - CellZMin + 1); } }

        /// <summary>The cells covering MapExtents centred on the origin.</summary>
        public static Layout ForExtents(Vector2 _extents)
        {
            return new Layout
            {
                CellXMin = Mathf.FloorToInt(-_extents.x * 0.5f / CellSize),
                CellXMax = Mathf.CeilToInt(_extents.x * 0.5f / CellSize) - 1,
                CellZMin = Mathf.FloorToInt(-_extents.y * 0.5f / CellSize),
                CellZMax = Mathf.CeilToInt(_extents.y * 0.5f / CellSize) - 1,
            };
        }

        /// <summary>The most detailed zoom level: each level doubles the tiles across.</summary>
        public int MaxLevel
        {
            get
            {
                float tilesAcross = Mathf.Max(MaxX - MinX, MaxZ - MinZ) * PixelsPerUnit / TileSize;
                return Mathf.Max(0, Mathf.CeilToInt(Mathf.Log(tilesAcross, 2f)));
            }
        }

        public Vector2Int TileCount(int _level)
        {
            float tileWorld = (float)TileSize / PixelsPerUnit * (1 << (MaxLevel - _level));
            return new Vector2Int(Mathf.CeilToInt((MaxX - MinX) / tileWorld), Mathf.CeilToInt((MaxZ - MinZ) / tileWorld));
        }
    }

    public static string OutputFolder(string _world) { return TilesRoot + "/" + _world; }

    public static bool HasRenderedMap(string _world) { return File.Exists(OutputFolder(_world) + "/manifest.json"); }

    [MenuItem("LoA Toolkit/Custom Assets/Render World Map", false, 8)]
    static void RenderOpenScene()
    {
        const string title = "Render World Map";
        Scene scene = SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(scene.path))
        {
            EditorUtility.DisplayDialog(title, "Save the map scene first.", "OK");
            return;
        }
        MapData mapData = FindMapData(scene);
        string problem = Check(scene, mapData);
        if (problem != null)
        {
            EditorUtility.DisplayDialog(title, problem, "OK");
            return;
        }
        Layout layout = Layout.ForExtents(mapData.MapExtents);
        if (!EditorUtility.DisplayDialog(title,
            "Render the world map of " + scene.name + " (" + (layout.MaxX - layout.MinX) + " x " + (layout.MaxZ - layout.MinZ) + " m, " + layout.Shots + " shots) to " + OutputFolder(scene.name) + "?\n\n" +
            "The scene is reopened for the render and your open scenes are put back afterwards.", "Render", "Cancel")) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string error;
        if (Render(scene.path, true, out error))
        {
            EditorUtility.DisplayDialog(title, "Rendered " + scene.name + "'s world map to " + OutputFolder(scene.name) + ".\n\n" +
                "Build Mod Package ships it with the mod as the \"" + scene.name + "_worldmap\" bundle. " +
                "Save your regions in the Region Editor to get area names on the map.", "OK");
        }
        else if (error != null)
        {
            EditorUtility.DisplayDialog(title, error, "OK");
        }
    }

    static MapData FindMapData(Scene _scene)
    {
        return _scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MapData>(true)).FirstOrDefault();
    }

    static string Check(Scene _scene, MapData _mapData)
    {
        if (_mapData == null) return "The scene " + _scene.name + " has no MapData. Add one and set its Map Extents (the world's size in X and Z).";
        if (_mapData.MapExtents.x <= 0 || _mapData.MapExtents.y <= 0) return "Set the MapData's Map Extents (the world's size in X and Z) first.";
        if (_mapData.OriginOffset != Vector3.zero) return "The MapData has an Origin Offset. The world map assumes world positions are scene positions, so it can't be rendered.";
        return null;
    }

    /// <summary>
    /// Renders the world map of the scene at _scenePath (saved; opened on its own for the render,
    /// changed for the map in memory and closed unsaved). The editor's open scenes are put back
    /// afterwards. Returns false with an error, or with none if cancelled.
    /// </summary>
    public static bool Render(string _scenePath, bool _showProgress, out string _error)
    {
        _error = null;
        // (an untitled scene can't be put back, and the caller has had the chance to save it)
        SceneSetup[] originalScenes = EditorSceneManager.GetSceneManagerSetup().Where(setup => !string.IsNullOrEmpty(setup.path)).ToArray();
        if (originalScenes.Length > 0 && !originalScenes.Any(setup => setup.isActive)) originalScenes[0].isActive = true;
        float previousShadowDistance = QualitySettings.shadowDistance;
        Material waterMaterial = new Material(Shader.Find("Unlit/Color")) { hideFlags = HideFlags.HideAndDontSave, color = MapWater };
        GameObject cameraObject = new GameObject("WorldMapRenderer Camera") { hideFlags = HideFlags.HideAndDontSave };
        try
        {
            Scene scene = EditorSceneManager.OpenScene(_scenePath, OpenSceneMode.Single);
            MapData mapData = FindMapData(scene);
            _error = Check(scene, mapData);
            if (_error != null) return false;
            if (scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true)).Any(b => b != null && b.GetType().Name == "Streamer"))
            {
                Debug.LogWarning("[WorldMapRenderer] " + scene.name + " streams parts of the world from other scenes (World Streamer). Only what's in " + scene.name + " itself is on the map.");
            }

            Layout layout = Layout.ForExtents(mapData.MapExtents);
            int maxLevel = layout.MaxLevel;
            string outDir = OutputFolder(scene.name);
            ClearOldTiles(outDir);
            string levelDir = outDir + "/" + maxLevel;
            Directory.CreateDirectory(levelDir);

            PrepareForMap(scene, waterMaterial);
            RenderSettings.fog = false; // the opened scene's own setting; it's closed unsaved
            QualitySettings.shadowDistance = CameraHeight + 1000f;
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;

            int cellPx = CellSize * PixelsPerUnit;
            int marginPx = CellMargin * PixelsPerUnit;
            int shot = 0;
            for (int z = layout.CellZMax; z >= layout.CellZMin; z--)
            {
                for (int x = layout.CellXMin; x <= layout.CellXMax; x++)
                {
                    if (_showProgress && EditorUtility.DisplayCancelableProgressBar("Render World Map", scene.name + ": shot " + (shot + 1) + " of " + layout.Shots, (float)shot / layout.Shots))
                    {
                        return false;
                    }
                    Vector2 centre = new Vector2((x + 0.5f) * CellSize, (z + 0.5f) * CellSize);
                    Texture2D image = Shoot(camera, centre, CellSize + CellMargin * 2, cellPx + marginPx * 2);
                    Texture2D cropped = new Texture2D(cellPx, cellPx, TextureFormat.RGB24, false);
                    cropped.SetPixels(image.GetPixels(marginPx, marginPx, cellPx, cellPx));
                    cropped.Apply();
                    SliceIntoTiles(layout, cropped, x * CellSize, (z + 1) * CellSize, levelDir);
                    UnityEngine.Object.DestroyImmediate(cropped);
                    UnityEngine.Object.DestroyImmediate(image);
                    shot++;
                }
            }

            BuildLowerLevels(layout, outDir, maxLevel, _showProgress);
            WriteManifest(scene.name, layout, outDir, maxLevel);
            Debug.Log("[WorldMapRenderer] " + scene.name + " rendered to " + outDir + " (" + (maxLevel + 1) + " zoom levels).");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            _error = "The render failed: " + e.Message;
            return false;
        }
        finally
        {
            if (_showProgress) EditorUtility.ClearProgressBar();
            UnityEngine.Object.DestroyImmediate(cameraObject);
            UnityEngine.Object.DestroyImmediate(waterMaterial);
            QualitySettings.shadowDistance = previousShadowDistance;
            // Drop the changed copy unsaved, then put the editor's scenes back.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            if (originalScenes.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(originalScenes);
            AssetDatabase.Refresh();
        }
    }

    /// <summary>Removes a previous render's tiles and manifest; region files the Region Editor wrote stay.</summary>
    static void ClearOldTiles(string _outDir)
    {
        if (!Directory.Exists(_outDir)) return;
        foreach (string dir in Directory.GetDirectories(_outDir))
        {
            int level;
            if (int.TryParse(Path.GetFileName(dir), out level))
            {
                Directory.Delete(dir, true);
                if (File.Exists(dir + ".meta")) File.Delete(dir + ".meta");
            }
        }
        if (File.Exists(_outDir + "/manifest.json")) File.Delete(_outDir + "/manifest.json");
    }

    /// <summary>Changes the freshly opened scene for the map; it's closed unsaved afterwards.</summary>
    static void PrepareForMap(Scene _scene, Material _waterMaterial)
    {
        GameObject[] roots = _scene.GetRootGameObjects();
        foreach (Terrain terrain in roots.SelectMany(root => root.GetComponentsInChildren<Terrain>(true)))
        {
            // Full detail everywhere: from the map camera's height every terrain is "far".
            terrain.basemapDistance = 100000f;
            terrain.heightmapPixelError = 1f;
            terrain.treeDistance = 100000f;
            terrain.treeBillboardDistance = 100000f;
            terrain.detailObjectDistance = 0f;
            // The game's terrain shader fades to a softer splat with distance; keep it close.
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            terrain.GetSplatMaterialPropertyBlock(block);
            block.SetFloat("_RTP_distance_start", 1000000f);
            terrain.SetSplatMaterialPropertyBlock(block);
        }
        foreach (Renderer renderer in roots.SelectMany(root => root.GetComponentsInChildren<Renderer>(true)))
        {
            if (renderer is ParticleSystemRenderer || renderer is TrailRenderer || renderer is LineRenderer)
            {
                renderer.enabled = false;
            }
            else if (renderer.gameObject.layer == WaterLayer)
            {
                renderer.sharedMaterials = Enumerable.Repeat(_waterMaterial, renderer.sharedMaterials.Length).ToArray();
            }
        }
        foreach (Behaviour halo in roots.SelectMany(root => root.GetComponentsInChildren<Behaviour>(true)).Where(b => b != null && (b.GetType().Name == "Halo" || b is LensFlare)))
        {
            halo.enabled = false;
        }

        // One sun: the light named "Sun", else the brightest directional light. Local lights
        // (lamps, glows) would blotch the map, and other directional lights would wash it out.
        Light[] lights = roots.SelectMany(root => root.GetComponentsInChildren<Light>(true)).ToArray();
        Light sun = lights.FirstOrDefault(l => l.type == LightType.Directional && l.name == "Sun")
            ?? lights.Where(l => l.type == LightType.Directional && l.enabled && l.gameObject.activeInHierarchy).OrderByDescending(l => l.intensity).FirstOrDefault();
        foreach (Light light in lights)
        {
            light.flare = null;
            if (light != sun) light.enabled = false;
        }
        if (sun == null)
        {
            GameObject sunObject = new GameObject("Sun");
            SceneManager.MoveGameObjectToScene(sunObject, _scene);
            sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
        }
        sun.gameObject.SetActive(true);
        sun.enabled = true;
        sun.transform.rotation = MapSun;
        sun.shadows = LightShadows.Soft;
    }

    /// <summary>Shoots a square of the world from straight above.</summary>
    static Texture2D Shoot(Camera _camera, Vector2 _centre, float _worldSize, int _pixels)
    {
        _camera.orthographic = true;
        _camera.orthographicSize = _worldSize * 0.5f;
        _camera.nearClipPlane = 1f;
        _camera.farClipPlane = CameraHeight + 1500f;
        _camera.clearFlags = CameraClearFlags.SolidColor;
        _camera.backgroundColor = Background;
        _camera.cullingMask = ~((1 << UiLayer) | (1 << MinimapIncludeLayer) | (1 << MinimapBuildingsLayer));
        _camera.transform.position = new Vector3(_centre.x, CameraHeight, _centre.y);
        _camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        RenderTexture rt = RenderTexture.GetTemporary(_pixels, _pixels, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB, MsaaSamples);
        _camera.targetTexture = rt;
        _camera.Render();
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;
        Texture2D shot = new Texture2D(_pixels, _pixels, TextureFormat.RGB24, false);
        shot.ReadPixels(new Rect(0, 0, _pixels, _pixels), 0, 0);
        shot.Apply();
        RenderTexture.active = previous;
        _camera.targetTexture = null;
        RenderTexture.ReleaseTemporary(rt);
        return shot;
    }

    static string TilePath(string _levelDir, int _x, int _y) { return _levelDir + "/" + _x + "_" + _y + ".jpg.bytes"; }

    /// <summary>Cuts a full-detail shot (its top-left at world minX, maxZ) into tiles.</summary>
    static void SliceIntoTiles(Layout _layout, Texture2D _shot, float _minX, float _maxZ, string _levelDir)
    {
        int firstX = Mathf.RoundToInt((_minX - _layout.MinX) * PixelsPerUnit / TileSize);
        int firstY = Mathf.RoundToInt((_layout.MaxZ - _maxZ) * PixelsPerUnit / TileSize);
        int across = _shot.width / TileSize;
        int down = _shot.height / TileSize;
        Texture2D tile = new Texture2D(TileSize, TileSize, TextureFormat.RGB24, false);
        for (int j = 0; j < down; j++)
        {
            for (int i = 0; i < across; i++)
            {
                // Texture rows run bottom-up; tile rows run north-down.
                tile.SetPixels(_shot.GetPixels(i * TileSize, _shot.height - (j + 1) * TileSize, TileSize, TileSize));
                tile.Apply();
                File.WriteAllBytes(TilePath(_levelDir, firstX + i, firstY + j), tile.EncodeToJPG(JpgQuality));
            }
        }
        UnityEngine.Object.DestroyImmediate(tile);
    }

    /// <summary>Each lower level: every tile is its four children, halved.</summary>
    static void BuildLowerLevels(Layout _layout, string _outDir, int _maxLevel, bool _showProgress)
    {
        Texture2D child = new Texture2D(2, 2, TextureFormat.RGB24, false);
        Texture2D quad = new Texture2D(TileSize * 2, TileSize * 2, TextureFormat.RGB24, false);
        Texture2D tile = new Texture2D(TileSize, TileSize, TextureFormat.RGB24, false);
        Color[] background = Enumerable.Repeat(Background, TileSize * TileSize).ToArray();
        try
        {
            for (int level = _maxLevel - 1; level >= 0; level--)
            {
                string childDir = _outDir + "/" + (level + 1);
                string levelDir = _outDir + "/" + level;
                Directory.CreateDirectory(levelDir);
                Vector2Int count = _layout.TileCount(level);
                for (int y = 0; y < count.y; y++)
                {
                    if (_showProgress) EditorUtility.DisplayProgressBar("Render World Map", "Zoom level " + level, (float)y / count.y);
                    for (int x = 0; x < count.x; x++)
                    {
                        bool any = false;
                        for (int dy = 0; dy < 2; dy++)
                        {
                            for (int dx = 0; dx < 2; dx++)
                            {
                                string path = TilePath(childDir, x * 2 + dx, y * 2 + dy);
                                Color[] pixels = background;
                                if (File.Exists(path) && child.LoadImage(File.ReadAllBytes(path)))
                                {
                                    pixels = child.GetPixels();
                                    any = true;
                                }
                                // dy = 0 is the northern child, which is the top half (higher rows).
                                quad.SetPixels(dx * TileSize, (1 - dy) * TileSize, TileSize, TileSize, pixels);
                            }
                        }
                        if (!any) continue;
                        quad.Apply();
                        Halve(quad, tile);
                        File.WriteAllBytes(TilePath(levelDir, x, y), tile.EncodeToJPG(JpgQuality));
                    }
                }
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(child);
            UnityEngine.Object.DestroyImmediate(quad);
            UnityEngine.Object.DestroyImmediate(tile);
        }
    }

    /// <summary>A 2x box filter: each output pixel is the average of four.</summary>
    static void Halve(Texture2D _source, Texture2D _target)
    {
        Color[] src = _source.GetPixels();
        int w = _source.width;
        Color[] dst = new Color[_target.width * _target.height];
        for (int y = 0; y < _target.height; y++)
        {
            for (int x = 0; x < _target.width; x++)
            {
                int i = (y * 2) * w + x * 2;
                dst[y * _target.width + x] = (src[i] + src[i + 1] + src[i + w] + src[i + w + 1]) * 0.25f;
            }
        }
        _target.SetPixels(dst);
        _target.Apply();
    }

    /// <summary>What the game client reads to place the tiles (its TiledMapView.Manifest).</summary>
    [Serializable]
    public class Manifest
    {
        public string World;
        public float MinX, MinZ, MaxX, MaxZ;
        public int PixelsPerUnit;
        public int TileSize;
        public int MaxLevel;
        public string Rendered;
    }

    static void WriteManifest(string _world, Layout _layout, string _outDir, int _maxLevel)
    {
        Manifest manifest = new Manifest
        {
            World = _world,
            MinX = _layout.MinX, MinZ = _layout.MinZ, MaxX = _layout.MaxX, MaxZ = _layout.MaxZ,
            PixelsPerUnit = PixelsPerUnit,
            TileSize = TileSize,
            MaxLevel = _maxLevel,
            Rendered = DateTime.UtcNow.ToString("u"),
        };
        File.WriteAllText(_outDir + "/manifest.json", JsonUtility.ToJson(manifest, true));
    }
}
