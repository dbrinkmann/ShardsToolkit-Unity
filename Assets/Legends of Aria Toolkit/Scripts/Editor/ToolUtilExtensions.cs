using UnityEngine;
using UnityEditor;
using System.Collections;

public partial class ToolUtil
{
    public static GameObject[] ClientIdLibrary
    {
        get
        {
            if(clientIdLibrary == null)
            {         
                ClientObjectLibrary prefabLibrary = (ClientObjectLibrary)AssetDatabase.LoadAssetAtPath(@"Assets\Legends of Aria Toolkit\Default Object Library\ClientObjectLibrary.prefab", typeof(ClientObjectLibrary));
                // The default object library is not distributed with the public toolkit. Fall back to an
                // empty library so editors degrade to placeholders rather than throwing.
                clientIdLibrary = (prefabLibrary != null && prefabLibrary.ClientIdPrefabs != null)
                    ? prefabLibrary.ClientIdPrefabs
                    : new GameObject[0];
            }
            return clientIdLibrary;
        }
    }
    private static GameObject[] clientIdLibrary;

    public static bool CanWriteToDefault { get { return false; } }
    public static bool EditWhileStopped { get { return true; } }
}