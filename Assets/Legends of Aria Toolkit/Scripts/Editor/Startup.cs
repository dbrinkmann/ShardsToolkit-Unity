using UnityEngine;
using UnityEditor;
using System.Threading;
 
[InitializeOnLoad]
public class Startup {
    static Startup() {
        Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        EditorApplication.delayCall += RestoreRequiredProjectLayers;
    }

    private static void RestoreRequiredProjectLayers()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        TagsHelpers.RestoreRequiredTagsAndLayersIfNeeded();
    }
}