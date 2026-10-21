// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// See LICENSE in the repository root.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

// Builds the "Collider" box children that the build exports as server collision, from the other
// colliders already on a prefab: mesh, box, sphere and capsule colliders anywhere in it.
//
// The server only collides against upright boxes: each one becomes a footprint rectangle, rotated
// about Y, plus a height. So every box made here is a direct child of the prefab root, rotated only
// about Y. Direct children matter: the export measures a Collider's position from its parent, so a
// Collider nested under an offset child would land in the wrong place.
//
// Mesh colliders are covered by projecting their triangles onto a grid on the ground plane and
// merging the filled cells into as few boxes as possible.
public class CollisionBoxGenerator : EditorWindow
{
    public enum MeshMode
    {
        Footprint,
        SingleBox,
    }

    public void OnGUI()
    {
        EditorGUILayout.Space();

        EditorGUILayout.HelpBox(
            "Adds \"Collider\" box children to the selected prefabs, fitted to the mesh, box, sphere and "
            + "capsule colliders they already have. Only boxes on children named \"Collider\" become "
            + "server collision, so this turns other Unity collision into something the build exports.\n\n"
            + "Your original colliders are left as they are.",
            MessageType.Info);

        EditorGUILayout.Space();

        meshMode = (MeshMode)EditorGUILayout.EnumPopup(
            new GUIContent("Mesh Colliders", "Footprint: several boxes that follow the mesh's outline on the ground. "
                + "Single Box: one box around the whole mesh."),
            meshMode);

        if (meshMode == MeshMode.Footprint)
        {
            EditorGUI.indentLevel++;
            cellSize = Mathf.Max(0.05f, EditorGUILayout.FloatField(
                new GUIContent("Cell Size (m)", "Grid resolution for the footprint. Smaller follows the outline more closely but makes more boxes."),
                cellSize));
            maxBoxesPerMesh = Mathf.Max(1, EditorGUILayout.IntField(
                new GUIContent("Max Boxes Per Mesh", "If a mesh needs more boxes than this, the grid is made coarser until it fits."),
                maxBoxesPerMesh));
            EditorGUI.indentLevel--;
        }

        ignoreAbove = Mathf.Max(0f, EditorGUILayout.FloatField(
            new GUIContent("Ignore Above (m)", "Mesh triangles that start higher than this above the prefab's lowest point are skipped, "
                + "so overhangs such as tree canopies and roofs don't block the ground under them. 0 keeps everything."),
            ignoreAbove));

        skipFloors = EditorGUILayout.Toggle(
            new GUIContent("Skip Floors", "Mesh triangles that face up or down and sit no higher than Floor Height above the lowest point "
                + "are skipped, so a building's floor doesn't make its whole interior solid."),
            skipFloors);

        if (skipFloors)
        {
            EditorGUI.indentLevel++;
            floorHeight = Mathf.Max(0f, EditorGUILayout.FloatField(
                new GUIContent("Floor Height (m)", "How high above the lowest point a flat triangle can be and still count as floor."),
                floorHeight));
            EditorGUI.indentLevel--;
        }

        includeTriggers = EditorGUILayout.Toggle(
            new GUIContent("Include Triggers", "Also convert colliders marked Is Trigger. They don't block anything in Unity, so they are skipped by default."),
            includeTriggers);

        replaceExisting = EditorGUILayout.Toggle(
            new GUIContent("Replace Existing Boxes", "Delete the prefab's existing \"Collider\" children first. Leave this off to keep boxes you placed by hand."),
            replaceExisting);

        disableSources = EditorGUILayout.Toggle(
            new GUIContent("Disable Source Colliders", "Turn off the colliders the boxes were made from. Leave this on only if nothing else on the object needs them."),
            disableSources);

        EditorGUILayout.Space();

        List<string> prefabPaths = GetSelectedPrefabPaths();
        if (prefabPaths.Count == 0)
        {
            EditorGUILayout.HelpBox("Select one or more prefabs in the Project window.", MessageType.None);
        }
        else
        {
            EditorGUILayout.LabelField("Selected prefabs: " + prefabPaths.Count);
        }

        EditorGUILayout.Space();

        GUI.enabled = prefabPaths.Count > 0;
        if (GUILayout.Button("Generate Collision Boxes"))
        {
            Generate(prefabPaths);
        }
        GUI.enabled = true;
    }

    public void OnSelectionChange()
    {
        Repaint();
    }

    private static List<string> GetSelectedPrefabPaths()
    {
        return Selection.GetFiltered<GameObject>(SelectionMode.Assets)
            .Select(item => AssetDatabase.GetAssetPath(item))
            .Where(path => path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .ToList();
    }

    private void Generate(List<string> prefabPaths)
    {
        string confirm = "Generate collision boxes for " + prefabPaths.Count + " prefab(s)?\n\n"
                       + "The prefabs are saved straight away and this can't be undone from the Edit menu. "
                       + "Use version control if you want to roll back.";
        if (replaceExisting)
        {
            confirm += "\n\nExisting \"Collider\" boxes will be deleted first.";
        }
        if (!EditorUtility.DisplayDialog("Confirm", confirm, "Ok", "Cancel"))
        {
            return;
        }

        StringBuilder report = new StringBuilder();
        int totalBoxes = 0;

        foreach (string path in prefabPaths)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                report.AppendLine(root.name + ":");
                int boxes = GenerateForPrefab(root, report);
                if (boxes > 0 || replaceExisting)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                totalBoxes += boxes;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        AssetDatabase.SaveAssets();

        Debug.Log("[CollisionBoxGenerator]\n" + report);
        EditorUtility.DisplayDialog("Finished",
            "Made " + totalBoxes + " collision box(es) across " + prefabPaths.Count + " prefab(s).\n\n" + report,
            "Ok");
    }

    private int GenerateForPrefab(GameObject root, StringBuilder report)
    {
        // Creatures collide through their template's BodyOffset, and the build skips their boxes.
        if (root.GetComponent<Mobile>() != null)
        {
            report.AppendLine("  skipped: creatures and mounts don't use static collision.");
            return 0;
        }

        if (replaceExisting)
        {
            List<Transform> existing = root.transform.Cast<Transform>().Where(child => child.name == c_ColliderName).ToList();
            foreach (Transform child in existing)
            {
                DestroyImmediate(child.gameObject);
            }
            if (existing.Count > 0)
            {
                report.AppendLine("  deleted " + existing.Count + " existing box(es).");
            }
        }

        foreach (BoxCollider nested in root.GetComponentsInChildren<BoxCollider>(true)
                     .Where(item => item.name == c_ColliderName && item.transform.parent != root.transform))
        {
            report.AppendLine("  warning: \"" + GetPath(root.transform, nested.transform) + "\" is nested; "
                + "the export loses its parent's offset. Use a direct child of the root instead.");
        }

        List<Collider> sources = root.GetComponentsInChildren<Collider>(true)
            .Where(item => item.enabled && item.gameObject.activeInHierarchy)
            .Where(item => !c_ExportedNames.Contains(item.name))
            .Where(item => includeTriggers || !item.isTrigger)
            .ToList();

        if (sources.Count == 0)
        {
            report.AppendLine("  no colliders to convert.");
            return 0;
        }

        // Gather every source's geometry in the root's space first, so the ignore-above height is
        // measured from the lowest point of the whole object, not of each part.
        List<SourceShape> shapes = new List<SourceShape>();
        foreach (Collider source in sources)
        {
            SourceShape shape = ReadShape(root.transform, source, report);
            if (shape != null)
            {
                shapes.Add(shape);
            }
        }

        if (shapes.Count == 0)
        {
            return 0;
        }

        float baseY = shapes.Min(shape => shape.Points.Min(point => point.y));
        float cutoffY = ignoreAbove > 0f ? baseY + ignoreAbove : float.MaxValue;

        int made = 0;
        foreach (SourceShape shape in shapes)
        {
            List<UprightBox> boxes;
            string detail = "";
            if (shape.Triangles == null)
            {
                boxes = new List<UprightBox> { FitUprightBox(shape.Points, shape.Yaw) };
            }
            else
            {
                List<Vector3> kept = KeepBlockingTriangles(shape, baseY, cutoffY);
                if (kept.Count == 0)
                {
                    report.AppendLine("  " + shape.Label + ": no triangles left after skipping floors and overhangs.");
                    continue;
                }

                if (meshMode == MeshMode.SingleBox)
                {
                    boxes = new List<UprightBox> { FitUprightBox(kept, shape.Yaw) };
                }
                else
                {
                    float usedCellSize;
                    boxes = BuildFootprintBoxes(kept, shape.Yaw, out usedCellSize);
                    detail = " (cell " + usedCellSize.ToString("0.##") + " m)";
                }
            }

            foreach (UprightBox box in boxes)
            {
                AddBox(root.transform, box);
            }
            made += boxes.Count;

            if (disableSources)
            {
                shape.Source.enabled = false;
            }

            report.AppendLine("  " + shape.Label + ": " + boxes.Count + " box(es)" + detail + ".");
        }

        return made;
    }

    private class SourceShape
    {
        public Collider Source;
        public string Label;
        public List<Vector3> Points;
        // Mesh colliders only: three points per triangle, indexing Points.
        public int[] Triangles;
        public float Yaw;
    }

    private struct UprightBox
    {
        public Vector3 Center;
        public Vector3 Size;
        public float Yaw;
    }

    private static SourceShape ReadShape(Transform root, Collider source, StringBuilder report)
    {
        Matrix4x4 toRoot = root.worldToLocalMatrix * source.transform.localToWorldMatrix;
        SourceShape shape = new SourceShape
        {
            Source = source,
            Label = source.GetType().Name + " on \"" + GetPath(root, source.transform) + "\"",
            Yaw = GetYaw(toRoot),
        };

        if (source is MeshCollider)
        {
            Mesh mesh = ((MeshCollider)source).sharedMesh;
            if (mesh == null)
            {
                report.AppendLine("  " + shape.Label + ": no mesh assigned, skipped.");
                return null;
            }

            Vector3[] vertices;
            int[] triangles;
            try
            {
                vertices = mesh.vertices;
                triangles = mesh.triangles;
            }
            catch (Exception ex)
            {
                report.AppendLine("  " + shape.Label + ": couldn't read mesh \"" + mesh.name + "\" (" + ex.Message
                    + "). Turn on Read/Write in its import settings.");
                return null;
            }

            if (vertices.Length == 0 || triangles.Length == 0)
            {
                report.AppendLine("  " + shape.Label + ": mesh \"" + mesh.name + "\" has no triangles. "
                    + "If it isn't empty, turn on Read/Write in its import settings.");
                return null;
            }

            shape.Points = vertices.Select(vertex => toRoot.MultiplyPoint3x4(vertex)).ToList();
            shape.Triangles = triangles;
            return shape;
        }

        Vector3 center;
        Vector3 extents;
        if (source is BoxCollider)
        {
            BoxCollider box = (BoxCollider)source;
            center = box.center;
            extents = box.size * 0.5f;
        }
        else if (source is SphereCollider)
        {
            SphereCollider sphere = (SphereCollider)source;
            center = sphere.center;
            extents = Vector3.one * sphere.radius;
        }
        else if (source is CapsuleCollider)
        {
            CapsuleCollider capsule = (CapsuleCollider)source;
            center = capsule.center;
            extents = Vector3.one * capsule.radius;
            extents[capsule.direction] = Mathf.Max(capsule.height * 0.5f, capsule.radius);
        }
        else
        {
            report.AppendLine("  " + shape.Label + ": this collider type isn't supported, skipped.");
            return null;
        }

        shape.Points = new List<Vector3>();
        for (int i = 0; i < 8; ++i)
        {
            Vector3 corner = new Vector3(
                (i & 1) == 0 ? -extents.x : extents.x,
                (i & 2) == 0 ? -extents.y : extents.y,
                (i & 4) == 0 ? -extents.z : extents.z);
            shape.Points.Add(toRoot.MultiplyPoint3x4(center + corner));
        }
        return shape;
    }

    // The heading of the source's own axes on the ground plane, folded into -45..45 degrees since a
    // box looks the same every 90. Boxes follow it so a turned model doesn't get a loose fit.
    private static float GetYaw(Matrix4x4 toRoot)
    {
        Vector3 forward = toRoot.MultiplyVector(Vector3.forward);
        float offset = 0f;
        if (new Vector2(forward.x, forward.z).sqrMagnitude < 0.0001f)
        {
            forward = toRoot.MultiplyVector(Vector3.right);
            offset = -90f;
        }
        if (new Vector2(forward.x, forward.z).sqrMagnitude < 0.0001f)
        {
            return 0f;
        }

        float yaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg + offset;
        yaw = Mathf.Repeat(yaw + 45f, 90f) - 45f;
        return Mathf.Abs(yaw) < 0.01f ? 0f : yaw;
    }

    private List<Vector3> KeepBlockingTriangles(SourceShape shape, float baseY, float cutoffY)
    {
        float floorTopY = baseY + floorHeight;
        List<Vector3> kept = new List<Vector3>();
        for (int i = 0; i + 2 < shape.Triangles.Length; i += 3)
        {
            Vector3 a = shape.Points[shape.Triangles[i]];
            Vector3 b = shape.Points[shape.Triangles[i + 1]];
            Vector3 c = shape.Points[shape.Triangles[i + 2]];
            if (Mathf.Min(a.y, b.y, c.y) > cutoffY)
            {
                continue;
            }

            if (skipFloors && Mathf.Max(a.y, b.y, c.y) <= floorTopY
                && Mathf.Abs(Vector3.Cross(b - a, c - a).normalized.y) > c_FlatNormalY)
            {
                continue;
            }

            kept.Add(a);
            kept.Add(b);
            kept.Add(c);
        }
        return kept;
    }

    private static UprightBox FitUprightBox(List<Vector3> points, float yaw)
    {
        Quaternion toBox = Quaternion.Euler(0f, -yaw, 0f);
        Vector3 min = Vector3.positiveInfinity;
        Vector3 max = Vector3.negativeInfinity;
        foreach (Vector3 point in points)
        {
            Vector3 local = toBox * point;
            min = Vector3.Min(min, local);
            max = Vector3.Max(max, local);
        }

        return MakeBox(min, max, yaw);
    }

    // Projects the triangles onto a grid on the ground, in the box frame, and merges filled cells
    // into rectangles. Each rectangle's height runs from the lowest to the highest triangle in it.
    // Coarsens the grid until the result fits in maxBoxesPerMesh.
    private List<UprightBox> BuildFootprintBoxes(List<Vector3> trianglePoints, float yaw, out float usedCellSize)
    {
        Quaternion toBox = Quaternion.Euler(0f, -yaw, 0f);
        Vector3[] points = trianglePoints.Select(point => toBox * point).ToArray();

        Vector3 min = Vector3.positiveInfinity;
        Vector3 max = Vector3.negativeInfinity;
        foreach (Vector3 point in points)
        {
            min = Vector3.Min(min, point);
            max = Vector3.Max(max, point);
        }

        float size = cellSize;
        for (int attempt = 0; ; ++attempt)
        {
            int countX = Mathf.Max(1, Mathf.CeilToInt((max.x - min.x) / size));
            int countZ = Mathf.Max(1, Mathf.CeilToInt((max.z - min.z) / size));
            if ((long)countX * countZ > c_MaxCells)
            {
                size *= 1.5f;
                continue;
            }

            List<UprightBox> boxes = MergeCells(RasterizeFootprint(points, min, size, countX, countZ), min, size, yaw);
            if (boxes.Count <= maxBoxesPerMesh || attempt >= c_MaxCoarsenAttempts)
            {
                usedCellSize = size;
                return boxes;
            }
            size *= 1.5f;
        }
    }

    private class FootprintGrid
    {
        public int CountX;
        public int CountZ;
        public bool[,] Filled;
        public float[,] MinY;
        public float[,] MaxY;
    }

    private static FootprintGrid RasterizeFootprint(Vector3[] points, Vector3 min, float size, int countX, int countZ)
    {
        FootprintGrid grid = new FootprintGrid
        {
            CountX = countX,
            CountZ = countZ,
            Filled = new bool[countX, countZ],
            MinY = new float[countX, countZ],
            MaxY = new float[countX, countZ],
        };

        Vector2[] triangle = new Vector2[3];
        for (int i = 0; i + 2 < points.Length; i += 3)
        {
            float triMinY = Mathf.Min(points[i].y, points[i + 1].y, points[i + 2].y);
            float triMaxY = Mathf.Max(points[i].y, points[i + 1].y, points[i + 2].y);
            for (int corner = 0; corner < 3; ++corner)
            {
                triangle[corner] = new Vector2(points[i + corner].x - min.x, points[i + corner].z - min.z);
            }

            int x0 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(triangle[0].x, triangle[1].x, triangle[2].x) / size), 0, countX - 1);
            int x1 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Max(triangle[0].x, triangle[1].x, triangle[2].x) / size), 0, countX - 1);
            int z0 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(triangle[0].y, triangle[1].y, triangle[2].y) / size), 0, countZ - 1);
            int z1 = Mathf.Clamp(Mathf.FloorToInt(Mathf.Max(triangle[0].y, triangle[1].y, triangle[2].y) / size), 0, countZ - 1);

            for (int x = x0; x <= x1; ++x)
            {
                for (int z = z0; z <= z1; ++z)
                {
                    if (!TriangleOverlapsCell(triangle, x * size, z * size, size))
                    {
                        continue;
                    }

                    if (!grid.Filled[x, z])
                    {
                        grid.Filled[x, z] = true;
                        grid.MinY[x, z] = triMinY;
                        grid.MaxY[x, z] = triMaxY;
                    }
                    else
                    {
                        grid.MinY[x, z] = Mathf.Min(grid.MinY[x, z], triMinY);
                        grid.MaxY[x, z] = Mathf.Max(grid.MaxY[x, z], triMaxY);
                    }
                }
            }
        }

        return grid;
    }

    // Separating-axis test between a triangle and a square cell on the ground plane. Also right for
    // triangles seen edge-on (walls), which project to a line. Touching edges don't count, or every
    // wall would also fill the cells beside it.
    private static bool TriangleOverlapsCell(Vector2[] triangle, float cellX, float cellZ, float size)
    {
        float epsilon = size * 0.001f;
        Vector2[] cell =
        {
            new Vector2(cellX, cellZ),
            new Vector2(cellX + size, cellZ),
            new Vector2(cellX + size, cellZ + size),
            new Vector2(cellX, cellZ + size),
        };

        if (Separated(Vector2.right, triangle, cell, epsilon) || Separated(Vector2.up, triangle, cell, epsilon))
        {
            return false;
        }

        for (int i = 0; i < 3; ++i)
        {
            Vector2 edge = triangle[(i + 1) % 3] - triangle[i];
            if (edge.sqrMagnitude < 1e-10f)
            {
                continue;
            }
            Vector2 axis = new Vector2(-edge.y, edge.x).normalized;
            if (Separated(axis, triangle, cell, epsilon))
            {
                return false;
            }
        }

        return true;
    }

    private static bool Separated(Vector2 axis, Vector2[] a, Vector2[] b, float epsilon)
    {
        float aMin = float.MaxValue, aMax = float.MinValue, bMin = float.MaxValue, bMax = float.MinValue;
        foreach (Vector2 point in a)
        {
            float d = Vector2.Dot(point, axis);
            aMin = Mathf.Min(aMin, d);
            aMax = Mathf.Max(aMax, d);
        }
        foreach (Vector2 point in b)
        {
            float d = Vector2.Dot(point, axis);
            bMin = Mathf.Min(bMin, d);
            bMax = Mathf.Max(bMax, d);
        }
        return aMax <= bMin + epsilon || bMax <= aMin + epsilon;
    }

    // Greedy merge: take the first free filled cell, grow right as far as the row allows, then grow
    // down while every cell in that span is free and filled.
    private static List<UprightBox> MergeCells(FootprintGrid grid, Vector3 min, float size, float yaw)
    {
        List<UprightBox> boxes = new List<UprightBox>();
        bool[,] used = new bool[grid.CountX, grid.CountZ];

        for (int z = 0; z < grid.CountZ; ++z)
        {
            for (int x = 0; x < grid.CountX; ++x)
            {
                if (!grid.Filled[x, z] || used[x, z])
                {
                    continue;
                }

                int width = 1;
                while (x + width < grid.CountX && grid.Filled[x + width, z] && !used[x + width, z])
                {
                    ++width;
                }

                int depth = 1;
                while (z + depth < grid.CountZ && RowFree(grid, used, x, width, z + depth))
                {
                    ++depth;
                }

                float minY = float.MaxValue;
                float maxY = float.MinValue;
                for (int dz = 0; dz < depth; ++dz)
                {
                    for (int dx = 0; dx < width; ++dx)
                    {
                        used[x + dx, z + dz] = true;
                        minY = Mathf.Min(minY, grid.MinY[x + dx, z + dz]);
                        maxY = Mathf.Max(maxY, grid.MaxY[x + dx, z + dz]);
                    }
                }

                Vector3 boxMin = new Vector3(min.x + x * size, minY, min.z + z * size);
                Vector3 boxMax = new Vector3(min.x + (x + width) * size, maxY, min.z + (z + depth) * size);
                boxes.Add(MakeBox(boxMin, boxMax, yaw));
            }
        }

        return boxes;
    }

    private static bool RowFree(FootprintGrid grid, bool[,] used, int x, int width, int z)
    {
        for (int dx = 0; dx < width; ++dx)
        {
            if (!grid.Filled[x + dx, z] || used[x + dx, z])
            {
                return false;
            }
        }
        return true;
    }

    // min and max are in the box frame (the root's space turned by -yaw).
    private static UprightBox MakeBox(Vector3 min, Vector3 max, float yaw)
    {
        Vector3 size = max - min;
        // A flat mesh such as a floor plate would otherwise make a box with no height.
        size.y = Mathf.Max(size.y, c_MinBoxHeight);
        Vector3 center = (min + max) * 0.5f;

        return new UprightBox
        {
            Center = Quaternion.Euler(0f, yaw, 0f) * center,
            Size = size,
            Yaw = yaw,
        };
    }

    private static void AddBox(Transform root, UprightBox box)
    {
        GameObject colliderObject = new GameObject(c_ColliderName);
        colliderObject.transform.SetParent(root, false);
        colliderObject.transform.localPosition = box.Center;
        colliderObject.transform.localRotation = Quaternion.Euler(0f, box.Yaw, 0f);
        colliderObject.transform.localScale = Vector3.one;

        BoxCollider collider = colliderObject.AddComponent<BoxCollider>();
        collider.center = Vector3.zero;
        collider.size = box.Size;
    }

    private static string GetPath(Transform root, Transform target)
    {
        if (target == root)
        {
            return root.name + " (root)";
        }

        string path = target.name;
        for (Transform parent = target.parent; parent != null && parent != root; parent = parent.parent)
        {
            path = parent.name + "/" + path;
        }
        return path;
    }

    private const string c_ColliderName = "Collider";
    private static readonly string[] c_ExportedNames = { "Collider", "ObjectBounds", "RoofBounds" };
    private const int c_MaxCells = 512 * 512;
    private const int c_MaxCoarsenAttempts = 12;
    private const float c_MinBoxHeight = 0.1f;
    // About 45 degrees: steeper than this counts as a wall, flatter as a floor.
    private const float c_FlatNormalY = 0.7f;

    private MeshMode meshMode = MeshMode.Footprint;
    private float cellSize = 0.25f;
    private int maxBoxesPerMesh = 16;
    private float ignoreAbove = 3f;
    private bool skipFloors = true;
    private float floorHeight = 0.5f;
    private bool includeTriggers = false;
    private bool replaceExisting = false;
    private bool disableSources = false;
}
