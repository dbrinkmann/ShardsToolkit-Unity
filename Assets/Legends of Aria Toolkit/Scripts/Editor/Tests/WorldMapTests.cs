// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// See LICENSE in the repository root.
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// EditMode tests for the world map: the layout of shots and zoom levels, and a small world rendered,
// built into its "_worldmap" bundle and read back the way the game client reads it.
public class WorldMapTests
{
    const string SceneName = "WorldMapTestWorld";
    const string SceneFolder = "Assets/WorldMapTestTemp";
    static string ScenePath { get { return SceneFolder + "/" + SceneName + ".unity"; } }

    [Test]
    public void ExtentsAreCoveredInWhole512mCellsCentredOnTheOrigin()
    {
        WorldMapRenderer.Layout square = WorldMapRenderer.Layout.ForExtents(new Vector2(1024, 1024));
        Assert.AreEqual(-512f, square.MinX);
        Assert.AreEqual(512f, square.MaxX);
        Assert.AreEqual(4, square.Shots);

        // An odd size is rounded out to the cells around it.
        WorldMapRenderer.Layout odd = WorldMapRenderer.Layout.ForExtents(new Vector2(1500, 700));
        Assert.AreEqual(-1024f, odd.MinX);
        Assert.AreEqual(1024f, odd.MaxX);
        Assert.AreEqual(-512f, odd.MinZ);
        Assert.AreEqual(512f, odd.MaxZ);
        Assert.AreEqual(8, odd.Shots);
    }

    [Test]
    public void EachZoomLevelHalvesTheTilesAcross()
    {
        // 1024 m at 2 px per metre is 2048 px: 8 tiles of 256 across, so levels 0 to 3.
        WorldMapRenderer.Layout layout = WorldMapRenderer.Layout.ForExtents(new Vector2(1024, 1024));
        Assert.AreEqual(3, layout.MaxLevel);
        Assert.AreEqual(new Vector2Int(8, 8), layout.TileCount(3));
        Assert.AreEqual(new Vector2Int(1, 1), layout.TileCount(0));
    }

    [Test]
    public void ARenderedMapShipsInABundleTheClientCanRead()
    {
        string tilesFolder = WorldMapRenderer.OutputFolder(SceneName);
        string buildFolder = Path.Combine(Path.GetTempPath(), "WorldMapTests-" + System.Guid.NewGuid().ToString("N"));
        bool uiFolderExisted = AssetDatabase.IsValidFolder("Assets/UI");
        AssetBundle bundle = null;
        try
        {
            CreateTestWorld();

            string error;
            Assert.IsTrue(WorldMapRenderer.Render(ScenePath, false, out error), error);
            Assert.IsTrue(WorldMapRenderer.HasRenderedMap(SceneName));
            Assert.AreEqual(64, Directory.GetFiles(tilesFolder + "/3", "*.jpg.bytes").Length, "8 x 8 tiles at full detail");
            Assert.AreEqual(1, Directory.GetFiles(tilesFolder + "/0", "*.jpg.bytes").Length, "one tile for the whole world");

            WorldMapRenderer.Manifest manifest = JsonUtility.FromJson<WorldMapRenderer.Manifest>(File.ReadAllText(tilesFolder + "/manifest.json"));
            Assert.AreEqual(SceneName, manifest.World);
            Assert.AreEqual(-512f, manifest.MinX);
            Assert.AreEqual(3, manifest.MaxLevel);

            // The land (a white plane over the middle of the world) is drawn, the sea around it isn't.
            Texture2D whole = new Texture2D(2, 2);
            Assert.IsTrue(whole.LoadImage(File.ReadAllBytes(tilesFolder + "/0/0_0.jpg.bytes")));
            Color land = whole.GetPixel(128, 128);
            Color sea = whole.GetPixel(4, 4);
            Assert.Greater(land.r, 0.5f, "land in the middle: " + land);
            Assert.Less(Mathf.Abs(sea.r - WorldMapRenderer.MapWater.r), 0.08f, "sea at the edge: " + sea);
            Object.DestroyImmediate(whole);

            ModPackageBundleManifest built = ModPackageBuildUtility.BuildWorldMapBundle(SceneName, 7, Path.Combine(buildFolder, "temp"), Path.Combine(buildFolder, "out"));
            Assert.AreEqual(SceneName + "_worldmap", built.Name);
            Assert.AreEqual(7, built.BundleVersion);
            string bundlePath = Path.Combine(buildFolder, "out", built.Name);
            Assert.AreEqual("7", File.ReadAllText(bundlePath + ".version"));

            // The game client's lookups (TiledMapView.MapFiles), with the world name's own case.
            bundle = AssetBundle.LoadFromFile(bundlePath);
            Assert.IsNotNull(bundle);
            string root = "Assets/UI/Maps/Tiles/" + SceneName + "/";
            Assert.IsTrue(bundle.Contains(root + "manifest.json"));
            TextAsset manifestAsset = bundle.LoadAsset<TextAsset>(root + "manifest.json");
            Assert.AreEqual(SceneName, JsonUtility.FromJson<WorldMapRenderer.Manifest>(manifestAsset.text).World);
            TextAsset tile = bundle.LoadAsset<TextAsset>(root + "3/4_4.jpg.bytes");
            Assert.IsNotNull(tile);
            Texture2D image = new Texture2D(2, 2);
            Assert.IsTrue(image.LoadImage(tile.bytes));
            Assert.AreEqual(256, image.width);
            Object.DestroyImmediate(image);
        }
        finally
        {
            if (bundle != null) bundle.Unload(true);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            AssetDatabase.DeleteAsset(SceneFolder);
            AssetDatabase.DeleteAsset(tilesFolder);
            if (!uiFolderExisted) AssetDatabase.DeleteAsset("Assets/UI");
            if (Directory.Exists(buildFolder)) Directory.Delete(buildFolder, true);
        }
    }

    /// <summary>A 1024 m world: a white 512 m plane in the middle, a sun, and its MapData.</summary>
    static void CreateTestWorld()
    {
        Directory.CreateDirectory(SceneFolder);
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject mapObject = new GameObject("MapData");
        MapData mapData = mapObject.AddComponent<MapData>();
        mapData.MapExtents = new Vector2(1024, 1024);

        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.transform.localScale = new Vector3(51.2f, 1f, 51.2f); // a plane is 10 m across
        Material white = new Material(Shader.Find("Unlit/Color")) { color = Color.white };
        AssetDatabase.CreateAsset(white, SceneFolder + "/White.mat");
        plane.GetComponent<Renderer>().sharedMaterial = white;

        GameObject sun = new GameObject("Sun");
        sun.AddComponent<Light>().type = LightType.Directional;

        Assert.IsTrue(EditorSceneManager.SaveScene(scene, ScenePath));
    }
}
