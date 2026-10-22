// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// See LICENSE in the repository root.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Unity's built-in fog (Lighting window -> Environment -> Fog) in map scenes.
//
// The game client switches Unity's fog on every frame in every map, whether or not "Fog" is ticked
// in the scene, and uses the scene's fog mode, colour, density and distances. The game's own maps
// therefore keep Exponential fog with density 0, which stays invisible. A new Unity scene starts
// with grey Exponential Squared fog at density 0.01, which the editor doesn't draw (fog is off)
// but which covers the whole map in grey in game.
public static class MapFog
{
    // The values the game's own maps use.
    static readonly Color GameFogColor = new Color(0.5137255f, 0.7058824f, 0.7411765f);
    const float GameLinearFogStart = 28.1f;
    const float GameLinearFogEnd = 112.4f;

    // Sets the active scene's fog to the game's invisible default.
    public static void ApplyGameDefaults()
    {
        RenderSettings.fog = false;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0f;
        RenderSettings.fogColor = GameFogColor;
        RenderSettings.fogStartDistance = GameLinearFogStart;
        RenderSettings.fogEndDistance = GameLinearFogEnd;
    }

    // Describes the fog the active scene will show in game, or returns null if it shows none.
    public static string DescribeVisibleFog()
    {
        string colour = "colour " + ColorUtility.ToHtmlStringRGB(RenderSettings.fogColor);
        if (RenderSettings.fogMode == FogMode.Linear)
        {
            return "Linear fog from " + RenderSettings.fogStartDistance + " m to " + RenderSettings.fogEndDistance + " m, " + colour;
        }

        if (RenderSettings.fogDensity > 0f)
        {
            return RenderSettings.fogMode + " fog with density " + RenderSettings.fogDensity + ", " + colour;
        }

        return null;
    }

    [MenuItem("LoA Toolkit/Utils/Remove Fog From Open Map", false, 31)]
    public static void RemoveFogFromOpenMap()
    {
        string fog = DescribeVisibleFog();
        if (fog == null)
        {
            EditorUtility.DisplayDialog("Remove Fog", "The open scene shows no fog in game.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("Remove Fog",
                "The open scene will show " + fog + " in game.\n\nSet its fog to the game's default (Exponential, density 0), which shows no fog?",
                "Remove Fog", "Cancel"))
        {
            return;
        }

        ApplyGameDefaults();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }
}
