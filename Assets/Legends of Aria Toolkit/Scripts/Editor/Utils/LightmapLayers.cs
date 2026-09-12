using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
 //Use this script to bake your lightmapping taking into account culling masks
public class BakeLightmaps {
    [MenuItem ("Lightmapping/Bake-use culling masks")]
    static void Bake () {    
        // Get all lights which are active in the scene
        Light[] lights = (from light in (Object.FindObjectsOfType (typeof(Light)) as Light[])
                where (light.enabled == true && light.gameObject.activeInHierarchy == true) select light).ToArray();
 
        // Get all the game objects which are active in the scene
        GameObject[] gameObjects = (from go in (Object.FindObjectsOfType (typeof(GameObject)) as GameObject[])
                where (    go.activeInHierarchy == true &&
                        (GameObjectUtility.GetStaticEditorFlags (go) & StaticEditorFlags.ContributeGI) > 0 &&
                        (go.GetComponent<Renderer>() != null || go.GetComponent<Terrain>() != null ))
                select go).ToArray();
 
        ILookup<int, GameObject> gameObjectGroups = gameObjects.ToLookup(go => (1 << go.layer));
 
        // Disable all the lights
        SetActive (lights, false);
 
        // For each group of gameObjects with a specific layer, 
        // bake them with the lights that have them in their culling mask
        foreach (IGrouping<int, GameObject> gameObjectGroup in gameObjectGroups)
        {
            int layerForGroup = gameObjectGroup.Key;
 
            GameObject[] gameObjectsForLayer = gameObjectGroup.ToArray();
            Light[] lightsForLayer =
                (from light in lights where ((light.cullingMask & layerForGroup) > 0) select light).ToArray();
 
            if (lightsForLayer.Count() > 0) {
                // Enable the lights for baking
                SetActive (lightsForLayer, true);        
 
                // Select the GameObjects and do a "Bake Selected"
                Selection.objects = gameObjectsForLayer;
                UnityEditor.Lightmapping.Bake ();
 
                // Disable the objects
                SetActive (lightsForLayer, false);
            }
        }
 
        // Enable all the lights
        SetActive (lights, true);
 
        // Set all lights to lightmapped to avoid double lighting
        foreach (Light light in lights){
            SerializedObject serializedLight = new SerializedObject (light);
            SerializedProperty actuallyLM = serializedLight.FindProperty ("m_ActuallyLightmapped");
            actuallyLM.boolValue = true;
            serializedLight.ApplyModifiedProperties ();
        }
    }
 
    static void SetActive (Light[] lights, bool active){
        foreach (Light light in lights)
            light.gameObject.SetActive (active);
    }
}