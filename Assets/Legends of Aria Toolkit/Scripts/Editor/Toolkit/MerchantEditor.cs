// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// Originally part of the Shards Engine Toolkit; released under the MIT License.
// See LICENSE in the repository root.
using UnityEngine;
using UnityEditor;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Text;
using CoreUtil;
using ShardsXML.SeedObject;
using ShardsXML.ObjectTemplate;
using CoreUtil.ShardEngineMath;
using System.Xml;
using System.Xml.Serialization;
using ShardsXML.TagDefinitions;

public class MerchantEditor : EditorWindow
{
    public class ContainerItem 
    {
        public GameObject gameObjRef;
        public string container;
        public string tooltip;
        public ContainerItem(GameObject gameobj)
        {
            gameObjRef = gameobj;
            container = "Container Name";
            tooltip = "Mouseover Tooltip";
        }
    }

    public class MerchantItem 
    {
        public Boolean local = false;
        public GameObject gameObjRef;
        public string container;
        public string templateName;
        public int price;
        public Vec3 location;
        public Boolean unlimitedStock;
        //public Vec3 rotation;
        public MerchantItem()
        {
        }
        public MerchantItem(GameObject gameobj, String template)
            {
                this.gameObjRef = gameobj;
                this.templateName = template;
                this.container = "Container Name";
                this.price = 20;
                this.location.X = gameobj.transform.position.x;
                this.location.Y = gameobj.transform.position.y;
                this.location.Z = gameobj.transform.position.z;
                this.unlimitedStock = false;
               // this.rotation.X = gameobj.transform.rotation.x;
               // this.rotation.Y = gameobj.transform.rotation.y;
               // this.rotation.Z = gameobj.transform.rotation.z;
                return;
            }
        public void AssignLocalLocation(GameObject otherObject)
            {
                this.location.X = gameObjRef.transform.position.x - otherObject.transform.position.x;
                this.location.Y = gameObjRef.transform.position.y - otherObject.transform.position.y;
                this.location.Z = gameObjRef.transform.position.z - otherObject.transform.position.z;
               // this.rotation.X = gameObjRef.transform.rotation.x;
               // this.rotation.Y = gameObjRef.transform.rotation.y;
               // this.rotation.Z = gameObjRef.transform.rotation.z;
                this.local = true;
            }
    
    }
    public MerchantEditor()
    {
        RefreshModList();
        ResetEditor();
    }

    void RefreshModList()
    {
        string[] newMods = (new string[] { "Default" }).Concat(ModHelpers.RefreshList()).ToArray();
        if (popupMods == null || (newMods != null && newMods.Length != popupMods.Length))
        {
            popupMods = newMods;
            if (currentMod >= popupMods.Length)
            {
                currentMod = 0;
            }
        }
    }


    private object GetCurrentItemFromSelection()
    {
        if (Selection.activeGameObject != null)
        {
            for (int i = 0; i < merchantItemList.Count;i++)
            {
                if (merchantItemList[i].gameObjRef == Selection.activeGameObject)
                {
                    return merchantItemList[i];
                }
            }
            for (int i = 0; i < containerItemList.Count; i++)
            {
                if (containerItemList[i].gameObjRef == Selection.activeGameObject)
                {
                    return containerItemList[i];
                }
            }
        }
        return null;
    }

    public void OnGUI()
    {
        if (scrollPosition == null)
        {
            scrollPosition = new Vector2(0, 0);
        }
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        EditorGUILayout.Space();
        //make sure their settings are correct.
        if (!ToolUtil.ValidateSettings())
        {
            EditorGUILayout.LabelField("Invalid Settings. Please fix your paths by going to the 'LoA Toolkit/Settings' menu.");
            return;
        }
        else if (!MapData.Instance)
        {
            EditorGUILayout.LabelField("To begin, select a map.");
            return;
        }
        //pick the item in the other window
        GameObject curObj = Selection.activeGameObject;


        //select the merchant spawner
        if (GUILayout.Button("Set Selected Object As Merchant Spawner"))
        {
            merchantObject = curObj;
            merchantSeed = merchantObject.GetComponent<SeedObject>();
            if (merchantSeed == null)
            {
                EditorUtility.DisplayDialog("ERROR", "Merchant spawner must be created using the seed object editor!", "OK");
            }
            else
            {
                template = merchantObject.GetComponent<SeedObject>().ServerTemplateId;
                if (template == "simple_mob_spawner")
                {
                    ShardsObjVar spawner = merchantObject.GetComponent<SeedObject>().ObjVars.Single<ShardsObjVar>(item => { if (item.Name == "spawnTemplate") return true; else return false; });
                    if (spawner == null)
                    {
                        EditorUtility.DisplayDialog("ERROR", "Merchant spawner must have spawnTemplate objvar!", "OK");
                        merchantObject = null;
                    }
                    else
                    {
                        template = spawner.StrValue;
                    }
                }
                else
                {
                    //in case in the future we have other types of spawners.
                    EditorUtility.DisplayDialog("Warning", "Merchant spawner SHOULD be a simple_mob_spawner!", "OK");
                    String spawner = merchantObject.GetComponent<SeedObject>().ServerTemplateId;
                    if (spawner == null || spawner == "")
                    {
                        EditorUtility.DisplayDialog("ERROR", "Designated object doesn't have a spawn template!", "OK");
                      merchantObject = null;
                    }
                    else
                    {
                       template = spawner;
                    }
                }
            }
            if (merchantObject != null && template != null)
            {
                LoadTemplate();
            }
        }        

        if (merchantObject != null && template != null && merchantSeed != null)
        {
            if (GUILayout.Button("Set Selected Object As Container Scene"))
            {
                sceneObj = curObj;
            }
            EditorGUILayout.Space();

            if (GUILayout.Button("Reload Merchant From Template"))
            {
                if (EditorUtility.DisplayDialog("WARNING", "Warning, this will reset all items, containers, and scene renderers!", "Reload Template", "Cancel"))
                {
                    LoadMerchantItems();
                }
            }
            //Tell the player what the selected merchant spawner is
            EditorGUILayout.LabelField("Merchant Spawner is set to " + merchantObject.name);

            if (sceneObj != null)
            { 
                EditorGUILayout.LabelField("Container Scene is set to " + sceneObj.name);
            }

            currentMod = EditorGUILayout.Popup("Mod", currentMod, popupMods);
            EditorGUILayout.Space();

            /*EditorGUILayout.LabelField("Export Properties:");
            outputName = EditorGUILayout.TextField("Template Filename", outputName);
            outputId = EditorGUILayout.TextField("Client Library ID", outputId);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Template Properties:");
            merchantName = EditorGUILayout.TextField("Name", merchantName);
            merchantHealth = EditorGUILayout.TextField("Health", merchantHealth);
            merchantScript = EditorGUILayout.TextField("Script", merchantScript);
            merchantTeamType = EditorGUILayout.TextField("Mobile Team", merchantTeamType);
            merchantHairType = EditorGUILayout.TextField("Hair", merchantHairType);
            merchantHeadType = EditorGUILayout.TextField("Head", merchantHeadType);
            merchantChestType = EditorGUILayout.TextField("Chest", merchantChestType);
            merchantLegsType = EditorGUILayout.TextField("Legs", merchantLegsType);*/

            //selecting a template...
            //if (GUILayout.Button("Select Template"))
            //{
            //    //Get the template
            //    ModHelpers.OpenTemplateSelectionPopup(currentMod, OnLoadTemplateSelected, false);
            //    //set the recieved template name to the last used template field - DFB NOTE: Is there a better way of doing this?
            //    if (merchantItemList.Count() > 0)
            //    { 
            //    merchantItemList[currentElement].templateName = currentTemplate;
            //    }
            //}

            //List the items
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Item List:");
            if (listOfItems == null)
            {
                //if the list doesn't exist create it
                listOfItems = new UnityEditorInternal.ReorderableList(merchantItemList, typeof(MerchantItem), true, true, true, true); 
                //the callback function for drawing it
                listOfItems.drawElementCallback =
                 (Rect rect, int index, bool isActive, bool isFocused) =>
                 {
                     //store the next item
                     MerchantItem element = merchantItemList[index] as MerchantItem;
                     //if there's elements
                     if (merchantItemList.Count > 0)
                     {
                         //if it's object hasn't been deleted
                         if (element.gameObjRef != null)
                         {
                             //create the label with the name of the gameobject.
                             EditorGUI.LabelField(new Rect(rect.xMin, rect.yMin, rect.width / 4, rect.height), new GUIContent(element.gameObjRef.name));
                             String lastTemplateName = element.templateName;
                             //get the user implemented template name
                             element.templateName = (EditorGUI.TextField(new Rect(rect.xMin + rect.width / 4, rect.yMin, (( rect.width) / 4), rect.height), element.templateName));
                             //set that as the last used for purposes of the template loader
                             if (lastTemplateName != element.templateName)
                             {
                                 currentElement = index;
                             }
                             element.price = Int16.Parse(EditorGUI.TextField(new Rect(rect.xMin + ((2 * rect.width) / 4), rect.yMin, ((rect.width) / 4), rect.height), "" + element.price + ""));
                             element.container = (EditorGUI.TextField(new Rect(rect.xMin + ((3 * rect.width) / 4), rect.yMin, ((rect.width) / 4), rect.height), element.container));
                         }
                         else
                         {
                             //remove the item if the object is deleted
                             merchantItemList.Remove(element);
                         }
                     }
                 };
            }
            //draw the list
            if (listOfItems != null)
            {
                listOfItems.DoLayoutList();
            }
            //add items to the list
            if (GUILayout.Button("Add Selected Object(s) to Item List"))
            {
                for (int i = 0; i < Selection.objects.Length; i++)
                {
                    if (Selection.objects[i] is GameObject)
                    {
                        merchantItemList.Add(new MerchantItem(Selection.objects[i] as GameObject, "Template Filename"));
                    }
                }
            }
            //remove all items from the list
            if (GUILayout.Button("Remove all objects from item list"))
            {
                if (EditorUtility.DisplayDialog("Remove all items", "Warning, this will remove all items from the list!", "Clear Items", "Cancel"))
                {
                    merchantItemList.Clear();
                }
            }
            //Assign the locations of selected items relative to container scene
            if (GUILayout.Button("Update existing container locations"))
            {
                if (merchantItemList.Count() == 0)
                {
                    EditorUtility.DisplayDialog("ERROR", "No items in item list! Make sure you add the objects you want to put in the scene before you run this utility!", "Ok");
                }
                UpdateExistingContainerSceneLocations();
            }
            if (GUILayout.Button("Assign Position relative to Container Scene"))
            {
                if (sceneObj == null)
                {
                    EditorUtility.DisplayDialog("ERROR", "No container scene assigned! Make sure you create a container scene and place items respective to it before running this utility!", "Ok");
                }
                else if (Selection.objects.Count() == 0)
                {
                    EditorUtility.DisplayDialog("ERROR", "No objects selected!", "Ok");
                }
                else if (merchantItemList.Count() == 0)
                {
                    EditorUtility.DisplayDialog("ERROR", "No items in item list! Make sure you add the objects you want to put in the scene before you run this utility!", "Ok");
                }
                else
                {
                    for (int i = 0; i < Selection.objects.Count();i++)
                    {
                        Boolean inItemList = false;
                        for (int n = 0; n < merchantItemList.Count();n++)
                        {
                            if (Selection.objects[i] == merchantItemList[n].gameObjRef)
                            {
                                merchantItemList[n].AssignLocalLocation(sceneObj);
                                inItemList = true;
                            }
                        }
                        if (inItemList == false)
                        {
                            EditorUtility.DisplayDialog("ERROR", "Selected object" + Selection.objects[i].name + " not in item list, make sure to add the items to the list before running!", "Ok");
                        }
                    }
                }
            }

            //Show location respective to render scene
            object currentItem = GetCurrentItemFromSelection();
            if (currentItem != null && currentItem is MerchantItem)
            {
                EditorGUILayout.LabelField("Current object location is: "+((currentItem as MerchantItem).location.ToString()) );
                EditorGUILayout.LabelField("Current object coordinates are "+((currentItem as MerchantItem).local?"Local":"Global"));
            }

            //Container list
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Container List:");
            if (listOfContainers == null)
            {
                //if the list doesn't exist create it
                listOfContainers = new UnityEditorInternal.ReorderableList(containerItemList, typeof(ContainerItem), true, true, true, true); 
                //the callback function for drawing it
                listOfContainers.drawElementCallback =
                 (Rect rect, int index, bool isActive, bool isFocused) =>
                 {
                     //store the next item
                     ContainerItem element = containerItemList[index] as ContainerItem;
                     //if there's elements
                     if (containerItemList.Count > 0)
                     {
                         //if it's object hasn't been deleted
                             //create the label with the name of the gameobject.
                         if (element.gameObjRef != null)
                         {
                             EditorGUI.LabelField(new Rect(rect.xMin, rect.yMin, rect.width / 3, rect.height), new GUIContent(element.gameObjRef.name));
                         }
                         element.tooltip = EditorGUI.TextField(new Rect(rect.xMin + ((1 * rect.width) / 3), rect.yMin, ((rect.width) / 3), rect.height), element.tooltip);
                         element.container = (EditorGUI.TextField(new Rect(rect.xMin + ((2 * rect.width) / 3), rect.yMin, ((rect.width) / 3), rect.height), element.container));
                         
                     }
                 };
            }
            //draw the list
            if (listOfContainers != null)
            {
                listOfContainers.DoLayoutList();
            }
            //-----------------------------
            //add items to the list
            if (GUILayout.Button("Add Selected Object(s) to Container List"))
            {
                for (int i = 0; i < Selection.objects.Length; i++)
                {
                    if (Selection.objects[i] is GameObject)
                    {
                        containerItemList.Add(new ContainerItem(Selection.objects[i] as GameObject));
                    }
                }
            }

            //remove all items from the list
            if (GUILayout.Button("Remove all objects from container list"))
            {
                if (EditorUtility.DisplayDialog("Remove all containers", "Warning, this will remove all containers from the list!", "Clear Containers", "Cancel"))
                {
                    containerItemList.Clear();
                }
            }

            //Create render scene
            if (GUILayout.Button("Create Container Scene Object"))
            {
                createObjectScene = true;
                //get a list of container scenes from a prompt and return them
                EditorGUIUtility.ShowObjectPicker<UnityEngine.GameObject>(
                     null,
                     false,
                     "ContainerScene",
                     EditorGUIUtility.GetControlID(FocusType.Passive));
            }
            if (Event.current.commandName == "ObjectSelectorUpdated" && createObjectScene)
            {
                Instantiate(EditorGUIUtility.GetObjectPickerObject(),
                    SceneView.lastActiveSceneView.camera.transform.position + SceneView.lastActiveSceneView.camera.transform.forward * 7 ,
                    Quaternion.identity);
                createObjectScene = false;
            }
            EditorGUILayout.Space();
            //TODO: Create template
            if (GUILayout.Button("Save Merchant to Template"))
            {
                //SaveMerchantToTemplate(EditorUtility.SaveFilePanel("Save Template to File",ToolUtil.Settings.BasePath,this.outputName,"xml"),merchantObject);
                UpdateExistingContainerSceneLocations(); //do this in case they missed anything.
                SaveMerchantToTemplate();
            }

            if (GUILayout.Button("Reset Editor"))
            {
                if (EditorUtility.DisplayDialog("Reset Editor", "Warning, this will remove everything and make you start over! Continue?", "Reset", "Cancel"))
                {
                    ResetEditor();
                }
            }
        }
        EditorGUILayout.EndScrollView();

    }

    void LoadMerchantItems()
    {
        containerItemList.Clear();
        merchantItemList.Clear();
        List<GameObject> sceneItems = new List<GameObject>();
        if (merchantSeed != null)
        {
            string curTemplatePath = ModHelpers.GetTemplatePath(template, currentMod);
            // Load template and parse initializers
            if (curInitializers != null && curInitializers.Any(item => item.Value.LuaTableItems.Any(tableItem => tableItem.KeyName == "ItemInventory")))
            {
                var merchantInitializerEntry = curInitializers.First(item => item.Value.LuaTableItems.Any(tableItem => tableItem.KeyName == "ItemInventory")).Value;

                LuaTableDictDict merchantContainersItem = merchantInitializerEntry.LuaTableItems.First(tableItem => tableItem.KeyName == "MerchantContainers") as LuaTableDictDict;
                LuaTableDictDict itemInventoryItem = merchantInitializerEntry.LuaTableItems.First(tableItem => tableItem.KeyName == "ItemInventory") as LuaTableDictDict;

                foreach (var dictItem in merchantContainersItem.dictDict)
                    {
                        ContainerItem newItem = new ContainerItem(null); //DFB TODO FIND OBJECT BASED ON CONTAINER OBJ VAR
                        foreach (var value in (dictItem.Value as LuaTableDict).entries)
                        {
                            LuaTableString stringItem = value as LuaTableString;
                            if (value.KeyName == "Name")
                            {
                                newItem.container = stringItem.Value;
                            }
                            else if (value.KeyName == "DisplayName")
                            {
                                newItem.tooltip = stringItem.Value;
                            }
                        }
                        //SeedObject[] objects = FindObjectsOfType(typeof(SeedObject)) as SeedObject[];
                        //foreach (var gameObject in objects)
                        //{
                      //      if (gameObject.)
                        //}
                        containerItemList.Add(newItem);
                    }



                foreach (var dictItem in itemInventoryItem.dictDict)
                {
                       ObjectTemplate item_template = null;
                       string itemTemplate = "";
                       Vec3 rotation = new Vec3(0,0,0);
                       MerchantItem newItem = new MerchantItem(); 
                       foreach (var value in (dictItem.Value as LuaTableDict).entries)
                        {
                            LuaTableString stringItem = value as LuaTableString;
                            LuaTableNumber numberItem = value as LuaTableNumber;
                            LuaTableBool boolItem = value as LuaTableBool;

                            if (value.KeyName == "Template" && stringItem != null && stringItem.Value != null)
                            {
                            itemTemplate = stringItem.Value;
                            newItem.templateName = itemTemplate;
                            string itemTemplatePath = ModHelpers.GetTemplatePath(itemTemplate, currentMod);
                            if (itemTemplatePath == null)
                                {
                                    EditorUtility.DisplayDialog("Error!", "TEMPLATE ERROR: Template " + itemTemplate + " not exist in template!", "OK");
                                    return;
                                }
                            else
                                {
                                XmlSerializer ser = new XmlSerializer(typeof(ObjectTemplate));
                                using (XmlReader reader = XmlReader.Create(itemTemplatePath))
                                    {
                                       item_template = ser.Deserialize(reader) as ObjectTemplate;
                                    }
                                }
                            }
                            else if (value.KeyName == "Price" && numberItem != null && numberItem.Value != null)
                            {
                                newItem.price = int.Parse(numberItem.Value);
                            }
                            else if (value.KeyName == "UnlimitedStock" && boolItem != null && boolItem.Value != null)
                            {
                                newItem.unlimitedStock = boolItem.Value;
                            }
                            else if (value.KeyName == "RelativeLoc" && stringItem != null && stringItem.Value != null)
                            {
                                newItem.location = Vec3.ConvertFrom(stringItem.Value);
                                newItem.local = true;
                            }
                            else if (value.KeyName == "Loc" && stringItem != null && stringItem.Value != null)
                            {
                                newItem.location = Vec3.ConvertFrom(stringItem.Value);
                                newItem.local = false;
                            }
                            else if (value.KeyName == "Rotation" && stringItem != null && stringItem.Value != null)
                            {
                                rotation = Vec3.ConvertFrom(stringItem.Value);
                            }
                            else if (value.KeyName == "Container" && stringItem != null && stringItem.Value != null)
                            {
                                
                                newItem.container = stringItem.Value;
                                Boolean sceneExists = false;
                                foreach (var scene in sceneItems)
                                {
                                    if (scene.name == newItem.container + "SCENE")
                                        sceneExists = true;
                                }
                                if (!sceneExists)
                                {
                                    //create a scene object if it exists
                                    Vector3 placePosition = new Vector3(merchantObject.transform.position.x, merchantObject.transform.position.y, merchantObject.transform.position.z);
                                    placePosition.y += 15;
                                    placePosition.z += sceneItems.Count*15;
                                    GameObject scene = (Instantiate(AssetDatabase.LoadAssetAtPath("Assets/Prefabs/RenderScenes/ContainerSceneCrate.prefab", typeof(GameObject)) as GameObject, placePosition, new Quaternion(0, 0, 0, 0)) as GameObject);
                                    scene.name = newItem.container + "SCENE";
                                    sceneItems.Add(scene);
                                }
                                newItem.local = true;
                            }
                       }
                       //assign local coordinates to global coordinates
                       if (newItem.local == true)
                       {
                           if (newItem.container == null || newItem.container == "")
                           {
                               newItem.location.X += merchantObject.transform.position.x;
                               newItem.location.Y += merchantObject.transform.position.y;
                               newItem.location.Z += merchantObject.transform.position.z;
                               newItem.local = false;
                           }
                           else
                           {
                               foreach (var scene in sceneItems)
                               {//if this is this item's scene
                                   if (scene.name == newItem.container + "SCENE")
                                   {
                                       newItem.location.X += scene.transform.position.x;
                                       newItem.location.Y += scene.transform.position.y;
                                       newItem.location.Z += scene.transform.position.z;
                                       newItem.local = false;
                                   }
                               }
                           }
                       }
                       GameObject reference = CreateMerchantObject(
                           uint.Parse(item_template.ClientId), 
                           new Vector3(newItem.location.X,newItem.location.Y,newItem.location.Z), 
                           new Vector3(1, 1, 1), 
                           new Vector3(rotation.X,rotation.Y,rotation.Z), 
                           itemTemplate, 
                           "TEMP_MerchItem" + itemTemplate,
                           "TEMP_MERCHANT");
                       newItem.gameObjRef = reference;
                       merchantItemList.Add(newItem);
                }
            }

        }
        EditorUtility.DisplayDialog("Finished","Loaded merchant items and containers. Don't forget to make sure the containers actually exist as seed objects!", "Ok"); 
    }

    private GameObject CreateMerchantObject(UInt32 clientId, Vector3 position, Vector3 scale, Vector3 rotation, string templateId, string seedName, string seedCategory = null)
    {
        GameObject seedObjectRoot = GameObject.Find("SeedObjects");
        if (seedCategory != null)
        {
            Transform catTrans = seedObjectRoot.transform.Find(seedCategory);
            if (catTrans != null)
            {
                seedObjectRoot = catTrans.gameObject;
            }
            else
            {
                GameObject newObjectRoot = new GameObject();
                newObjectRoot.transform.parent = seedObjectRoot.transform;
                newObjectRoot.name = seedCategory;
                seedObjectRoot = newObjectRoot;
            }
        }

        GameObject clientObjPrefab = ToolUtil.ResolvePrefabForClientId(clientId);
        GameObject clientObjInstance;
        if (clientObjPrefab != null)
        {
            clientObjInstance = Instantiate(clientObjPrefab, position, Quaternion.Euler(rotation)) as GameObject;
        }
        else
        {
            // No client prefab in any available library; place a marker so authoring still works.
            Debug.LogWarning("No client prefab for clientId " + clientId + " (template '" + templateId + "'). Using a placeholder marker.");
            clientObjInstance = ToolUtil.CreateClientIdPlaceholder(templateId);
            clientObjInstance.transform.position = position;
            clientObjInstance.transform.rotation = Quaternion.Euler(rotation);
        }

        if (seedName != null)
        {
            clientObjInstance.name = seedName;
        }

        clientObjInstance.transform.parent = seedObjectRoot.transform;
        clientObjInstance.transform.localScale = scale;

        Component clientComp = clientObjInstance.GetComponent("ClientObject");
        if (clientComp != null)
        {
            DestroyImmediate(clientComp);
        }
        Component contComp = clientObjInstance.GetComponent("ContainerObject");
        if (contComp != null)
        {
            DestroyImmediate(contComp);
        }
        Component nodrawComp = clientObjInstance.GetComponent("NoDraw");
        if (nodrawComp != null)
        {
            DestroyImmediate(nodrawComp);
        }
        AudioSource audioComp = clientObjInstance.GetComponent<AudioSource>();
        if (audioComp != null)
        {
            DestroyImmediate(audioComp);
        }


        return clientObjInstance;
    }

    void ResetEditor()
    {
        containerItemList.Clear();
        merchantItemList.Clear();
        merchantObject = null;
        sceneObj = null;
        listOfItems = null;
        listOfContainers = null;
        template = null;
        merchantSeed = null;
    }

    List<GameObject> GetContainerScenes()
    {
        List<GameObject> list = new List<GameObject>();//GameObject.FindObjectsOfType<GameObject>().Where<List<GameObject>>(item => item.name.Contains("SCENE"));
        GameObject[] array = GameObject.FindObjectsOfType<GameObject>();
        foreach (var _item in array )
        {
            if (_item.name.Contains("SCENE"))
            {
                list.Add(_item);
            }
        }
        return list;//(item => GameObject.Find("SCENE"))
    }

    void UpdateExistingContainerSceneLocations()
    {

        List<GameObject> containerSceneList = GetContainerScenes();

        if (containerSceneList.Count == 0) {
            return;
        }
        Debug.Log("Container scene count is "+containerSceneList.Count.ToString());
        foreach (var containerItem in containerSceneList)
        {
            foreach (var merchantItem in merchantItemList)
            {
                string containerName = containerItem.name.Replace("SCENE","");
                if (merchantItem.container != null && merchantItem.container.Contains(containerName))
                {
                    merchantItem.AssignLocalLocation(containerItem);
                }
            }
        }

        EditorUtility.DisplayDialog("ERROR", "Existing container locations updated.", "Ok");
    }
    //get some information on the scripts so we know if to replace or not
    /*
    int PollScripts()
    {
        if (oldScriptEngine == null)
        {
            return 2;
        }

        int index = 0;
        for (index = 0; index < oldScriptEngine.LuaModule.Count(); index++)
        {
            if (oldScriptEngine.LuaModule[index].Name == "guard_protect")
            {
                hasGuardProtect = true;
            }
            if (oldScriptEngine.LuaModule[index].Name == merchantScript)
            {
                if (EditorUtility.DisplayDialog("Warning", "The old template had an initalizer for the current script! Do you wish to replace it with a generated one?", "Yes", "No"))
                {
                    replaceOldScript = true;
                }
            }
        }
        if (hasGuardProtect == false)
        {
            index++;
        }
        if (replaceOldScript == false)
        {
            index++;
        }
        return index;
    }*/
    //assign the scripts to the file
    /*void AssignScripts(ObjectTemplateScriptEngineComponentLuaModule[] luaModules)
    {
        int index = 0;
        if (oldScriptEngine != null)
            {
            for (index = 0; index < oldScriptEngine.LuaModule.Count(); index++)
                {
                    if (oldScriptEngine.LuaModule[index].Name == merchantScript)
                    {
                        if (replaceOldScript)
                        {
                            luaModules[index] = new ObjectTemplateScriptEngineComponentLuaModule();
                            luaModules[index].Initializer = initializer;
                            luaModules[index].Name = merchantScript;
                        }
                        else
                        {
                            luaModules[index] = oldScriptEngine.LuaModule[index];
                        }
                    }
                    else 
                    {
                        luaModules[index] = oldScriptEngine.LuaModule[index];
                    }
                }
            }
        else
        {
           luaModules[index] = new ObjectTemplateScriptEngineComponentLuaModule();
           luaModules[index].Initializer = initializer;
           luaModules[index].Name = merchantScript;
        }
        if (hasGuardProtect == false)
        {
           index ++;
           luaModules[index] = new ObjectTemplateScriptEngineComponentLuaModule();
           luaModules[index].Name = "guard_protect";
        }
    }
    void LoadMerchantFromTemplate(string filename)
    {
        EditorUtility.DisplayDialog("Warning", "Loading templates from XML not supported at this time.", "Ok");
    }*/

    /*void SaveMerchantToTemplate(string filename, GameObject spawner)//save a template
    {
        if (filename == null) return;
        if (spawner == null) return;
        //Write the XML
        ObjectTemplate template = new ObjectTemplate();
        //save our user entered items to the xml template to be written
        template.ClientId = outputId;
        template.Hue = "0xFFFFFFFF";
        template.Name = merchantName;
        template.MobileComponent = new ObjectTemplateMobileComponent[] { new ObjectTemplateMobileComponent() };
        template.MobileComponent[0].BaseRunSpeed = "1";
        template.MobileComponent[0].MobileType = "Friendly";
        template.ObjectVariableComponent = new ObjectTemplateObjectVariableComponent[] {new ObjectTemplateObjectVariableComponent()};

        //Replace the mobile team type
        ShardsObjVar mobileTeamType = new ShardsObjVar();
        mobileTeamType.StrValue = merchantTeamType;
        mobileTeamType.Name = "MobileTeamType";
        for (int i = 0; i < curObjVars.Count(); i++)
        {
            if (curObjVars[i].Name == "MobileTeamType")
            curObjVars.Remove(curObjVars[i]);
        }
        curObjVars.Add(mobileTeamType);

        //Replace the base health
        ShardsObjVar baseHealth = new ShardsObjVar();
        baseHealth.StrValue = merchantTeamType;
        baseHealth.Name = "BaseHealth";
        for (int i = 0; i < curObjVars.Count(); i++)
        {
            if (curObjVars[i].Name == "BaseHealth")
            curObjVars.Remove(curObjVars[i]);
        }
        curObjVars.Add(baseHealth);

        //save object variables
        if (curObjVars != null)
        {
            ObjectTemplateObjectVariableComponent objVarComp = template.ObjectVariableComponent[0];

            IEnumerable<ShardsObjVar> boolVars = curObjVars.Where(item => item.Name != "" && item.Type == ShardsObjVar.ObjVarType.Boolean);
            objVarComp.BoolVariable = null;
            if( boolVars.Any() )
            {
                objVarComp.BoolVariable = boolVars.Select(item => new ObjectTemplateObjectVariableComponentBoolVariable() { Name = item.Name, Value = item.BoolValue.ToString() }).ToArray();
            }

            IEnumerable<ShardsObjVar> doubleVars = curObjVars.Where(item => item.Name != "" && item.Type == ShardsObjVar.ObjVarType.Number);
            objVarComp.DoubleVariable = null;
            if (doubleVars.Any())
            {
                objVarComp.DoubleVariable = doubleVars.Select(item => new ObjectTemplateObjectVariableComponentDoubleVariable() { Name = item.Name, Value = item.DoubleValue.ToString() }).ToArray();
            }

            IEnumerable<ShardsObjVar> locVars = curObjVars.Where(item => item.Name != "" && item.Type == ShardsObjVar.ObjVarType.Loc);
            objVarComp.LocVariable = null;
            if (locVars.Any())
            {
                objVarComp.LocVariable = locVars.Select(item => new ObjectTemplateObjectVariableComponentLocVariable() { Name = item.Name, Value = (item.LocValue.x.ToString() + "," + item.LocValue.y.ToString() + "," + item.LocValue.z.ToString()) }).ToArray();
            }

            IEnumerable<ShardsObjVar> stringVars = curObjVars.Where(item => item.Name != "" && item.Type == ShardsObjVar.ObjVarType.String);
            objVarComp.StringVariable = null;
            if (stringVars.Any())
            {
                objVarComp.StringVariable = stringVars.Select(item => new ObjectTemplateObjectVariableComponentStringVariable() { Name = item.Name, Value = item.StrValue }).ToArray();
            }
        }
        //generate an initializer if we don't have one.
        if (initializer == null || initializer == "")
            {
            initializer = "{\r\n";
            initializer = initializer + "\t\t\t\tStats = { Str=150, Agi=80, Int=20 },\r\n";
            initializer = initializer + "\t\t\t\tSkills = { Melee=50, Slashing=50, Bashing=50, Piercing=50, Endurance=50, Regeneration=50, Blocking=45 , Cooking=31, Salvage=31, Channeling=31, Foraging=31, Butchery=31, Fabrication=31, Mining=31, Lumberjack=31, Metalsmith=31, Rogue=31,},\r\n";
            initializer = initializer + "\t\t\t\tEquipTable = {\r\n";
            initializer = initializer + "\t\t\t\t\tBodyPartHead = { \""+ merchantHeadType+"\"},\r\n";
            initializer = initializer + "\t\t\t\t\tBodyPartHair = { \""+ merchantHairType+"\"},\r\n";
            initializer = initializer + "\t\t\t\t\tChest = { \""+ merchantChestType+"\"},\r\n";
            initializer = initializer + "\t\t\t\t\tLegs = { \""+ merchantLegsType+"\"},\r\n";
            initializer = initializer + "\t\t\t\t\tBackpack = { \"backpack\"},\r\n";
            initializer = initializer + "\t\t\t},\r\n";
            initializer = initializer + "\t\t\tMerchantContainers = {\r\n";
            for (int i = 0; i < containerItemList.Count();i++)//Merchant containers
            initializer = initializer + "\t\t\t\t{ Name = \""+containerItemList[i].container+"\", DisplayName = \""+containerItemList[i].tooltip+"\" },\r\n";
            initializer = initializer + "\t\t\t},\r\n";
            initializer = initializer + "\t\t\tItemInventory = {\r\n"; 
            for (int i = 0; i < merchantItemList.Count();i++)
            {
                if (merchantItemList[i].container == null || merchantItemList[i].container.Length == 0 || merchantItemList[i].container == "")
                {
                    if (merchantItemList[i].local == false)
                        {
                        merchantItemList[i].AssignLocalLocation(spawner);
                        }
                   initializer = initializer + "\t\t\t\t{ Template = \""+merchantItemList[i].templateName +"\", Price = \""+merchantItemList[i].price+"\", RelativeLoc = \""+merchantItemList[i].location.X+","+merchantItemList[i].location.Y+","+merchantItemList[i].location.Z+"\", Rotation = \""+ merchantItemList[i].gameObjRef.transform.rotation.x+","+merchantItemList[i].gameObjRef.transform.rotation.y+","+merchantItemList[i].gameObjRef.transform.rotation.z + "\"},\r\n";
                    //each item output the whole class:  output template name, price, (container if it has one), relativelocation relative to spawn item, and rotation
                }
                else
                {
                    if (merchantItemList[i].local == false)
                    {
                        EditorUtility.DisplayDialog("ERROR", "Object "+merchantItemList[i].gameObjRef.name+" has a container, but it uses global coordinates! Assign it to a container scene and rerun, or leave container field empty!", "Ok");
                        return;
                    }
                    //merchantItemList[i].AssignLocalLocation(spawner);
                    initializer = initializer + "\t\t\t\t{ Template = \"" + merchantItemList[i].templateName + "\", Container = \"" + merchantItemList[i].container + "\", Price = \"" + merchantItemList[i].price + "\", Loc = \"" + merchantItemList[i].location.X + "," + merchantItemList[i].location.Y + "," + merchantItemList[i].location.Z + "\", Rotation = \"" + merchantItemList[i].gameObjRef.transform.rotation.x + "," + merchantItemList[i].gameObjRef.transform.rotation.y + "," + merchantItemList[i].gameObjRef.transform.rotation.z + "\"},\r\n";
                }
            }
            initializer = initializer + "\t\t\t},\r\n";
            initializer = initializer + "\t\t\t}\r\n\t\t\t";
        }
        //add the old scripts back with their initializers
        int amount = PollScripts();
        //since we are potentially adding two new scripts, add two to the size of the array
        template.ScriptEngineComponent = new ObjectTemplateScriptEngineComponent[] { new ObjectTemplateScriptEngineComponent() };
        template.ScriptEngineComponent[0].LuaModule = new ObjectTemplateScriptEngineComponentLuaModule[amount];
        //assign the scripts to the template
        AssignScripts(template.ScriptEngineComponent[0].LuaModule);

        //everything's assigned, go forth and write!
        ToolUtil.WriteXML(new XmlSerializer(typeof(ObjectTemplate)), filename, template);



        ResetEditor();

        Component seedObject = spawner.GetComponent("SeedObject");//at spawner
        if (!EditorUtility.DisplayDialog("Save Complete", "Template saved! Don't forget to delete or hide the merchant items, and update the Seed Object scripts for the containers!", "Ok", "Help"))
        {
            EditorUtility.DisplayDialog("Help", "To make your merchant have his containers useable with buyable items inside them, a Seed Object component has to be placed on the container's ingame object. The template name in the Seed Object component must be set to the template file the server is to spawn the item with, and the object variable \"merchantContainer\" has to be set to the container name specified in this editor.", "Ok");
        }
        if (seedObject == null)//check if it has a seed object component
        {
            EditorUtility.DisplayDialog("ERROR", "The spawner isn't a seed object! Add a Seed Object script to it with the template name \"simple_mob_spawner\" and the spawnTemplate object variable to the filename of the template you just saved.", "Ok");
        }
    }*/

    public void LoadTemplate()
    {
        string curTemplatePath = ModHelpers.GetTemplatePath(template, currentMod);
        XmlSerializer ser = new XmlSerializer(typeof(ObjectTemplate));
        using (XmlReader reader = XmlReader.Create(curTemplatePath))
            {
                curTemplate = ser.Deserialize(reader) as ObjectTemplate;
                curInitializers = ModuleInitializer.ParseInitializers(curTemplate, currentMod);
            }
        Boolean hasScript = (curInitializers != null && curInitializers.Any(item => item.Value.LuaTableItems.Any(tableItem => tableItem.KeyName == "ItemInventory")));
        if (!hasScript)
        {
            ObjectTemplateScriptEngineComponentLuaModule script = new ObjectTemplateScriptEngineComponentLuaModule();
            script.Name = "base_merchant";
            EditorUtility.DisplayDialog("Warning", "Template does not have a merchant script! Adding a base_merchant script to the template!", "Ok");
            curTemplate.ScriptEngineComponent[0].LuaModule = curTemplate.ScriptEngineComponent[0].LuaModule.Concat(new ObjectTemplateScriptEngineComponentLuaModule[]{ script}).ToArray();            
            curInitializers = ModuleInitializer.ParseInitializers(curTemplate, currentMod);
        }
    }


    public void SaveMerchantToTemplate()
    {
        if (merchantObject == null)
        {
            // TODO THROW ERROR DIALOG
            EditorUtility.DisplayDialog("ERROR", "Merchant spawner is null! Update the merchant spawner and reload the editor.", "Ok");
            return;
        }
        //SeedObject merchantSeed = merchantObject.GetComponent<SeedObject>();        
        if (merchantSeed != null)
        {
            string curTemplatePath = ModHelpers.GetTemplatePath(template, currentMod);
            // Load template and parse initializers
            if(curInitializers != null && curInitializers.Any(item => item.Value.LuaTableItems.Any(tableItem => tableItem.KeyName == "ItemInventory")))
            {
                var merchantInitializerEntry = curInitializers.First(item => item.Value.LuaTableItems.Any(tableItem => tableItem.KeyName == "ItemInventory")).Value;

                LuaTableDictDict merchantContainersItem = merchantInitializerEntry.LuaTableItems.First(tableItem => tableItem.KeyName == "MerchantContainers") as LuaTableDictDict;
                LuaTableDictDict itemInventoryItem = merchantInitializerEntry.LuaTableItems.First(tableItem => tableItem.KeyName == "ItemInventory") as LuaTableDictDict;
                LuaTableDictDict equipTable = merchantInitializerEntry.LuaTableItems.First(tableItem => tableItem.KeyName == "EquipTable") as LuaTableDictDict;
                Debug.Log("EquipTable: " + equipTable.ToString());
                
                merchantContainersItem.dictDict.Clear();
                foreach(ContainerItem contItem in containerItemList)
                {
                    LuaTableDict contItemDict = merchantContainersItem.AddEntry() as LuaTableDict;
                    contItemDict.WriteKey = false;
                    foreach(var dictItem in contItemDict.entries)
                    {
                        LuaTableString stringItem = dictItem as LuaTableString;
                        if(dictItem.KeyName == "Name")
                        {                            
                            stringItem.Value = contItem.container;
                        }
                        else if (dictItem.KeyName == "DisplayName")
                        {
                            stringItem.Value = contItem.tooltip;
                        }
                    }
                }
                
                itemInventoryItem.dictDict.Clear();
                foreach (MerchantItem merchItem in merchantItemList)
                {
                    if(merchItem.container != null && merchItem.container != "" && merchItem.local == false)
                    {
                        EditorUtility.DisplayDialog("ERROR", "Merchant item " + merchItem.gameObjRef.name + " is in a container but does not have its position assigned.", "OK");
                        return;
                    }
                    int index = 0;
                    LuaTableDict merchItemDict = itemInventoryItem.AddEntry() as LuaTableDict;
                    merchItemDict.WriteKey = false;
                    foreach (var dictItem in merchItemDict.entries)
                    {
                        LuaTableString stringItem = dictItem as LuaTableString;
                        LuaTableNumber numberItem = dictItem as LuaTableNumber;
                        LuaTableBool boolItem = dictItem as LuaTableBool;
                        
                        if (dictItem.KeyName == "Template")
                        {
                            stringItem.Value = merchItem.templateName;
                        }
                        else if (dictItem.KeyName == "Price")
                        {
                            numberItem.Value = merchItem.price.ToString();
                        }
                        else if (dictItem.KeyName == "UnlimitedStock")
                        {
                            boolItem.Value = merchItem.unlimitedStock;
                            merchItemDict.added[index] = true;
                        }
                        else if (dictItem.KeyName == "RelativeLoc")
                        {
                            if (merchItem.container == null || merchItem.container == "")
                            {                             
                                merchItem.AssignLocalLocation(merchantObject);
                            }
                            stringItem.Value = string.Format("{0}, {1}, {2}", merchItem.location.X, merchItem.location.Y, merchItem.location.Z);
                            merchItemDict.added[index] = true;
                        }
                        else if (dictItem.KeyName == "Rotation" && merchItem.gameObjRef.transform.eulerAngles != Vector3.zero)
                        {
                            Vector3 rotation = merchItem.gameObjRef.transform.localRotation.eulerAngles;
                            stringItem.Value = string.Format("{0}, {1}, {2}", rotation.x, rotation.y, rotation.z);
                            merchItemDict.added[index] = true;
                        }
                        else if (dictItem.KeyName == "Container" && !(merchItem.container == null || merchItem.container == ""))
                        {
                            stringItem.Value = merchItem.container;
                            merchItemDict.added[index] = true;
                        }
                        index++;
                    }                    
                }

                ModuleInitializer.UpdateInitializers(curInitializers, curTemplate);
                ToolUtil.WriteXML(new XmlSerializer(typeof(ObjectTemplate)), curTemplatePath, curTemplate, true);
                EditorUtility.DisplayDialog("Saved", curTemplate.Name + " saved.", "Ok"); 
            }

        }


    }

    public void Update()
    {
        Repaint();
    }
    private GameObject merchantObject;
    private GameObject sceneObj;
    private int currentMod = 0;
    private Boolean createObjectScene = false;
    private int currentElement = 0;
    private string template = null;
    private SeedObject merchantSeed = null;
    private Vector2 scrollPosition;
    private string merchantScript   = "traveling_merchant";
    /*
    private string merchantName     = "Bob the Merchant";
    private string merchantTeamType   = "";
    private string merchantHeadType = "head_merchant";
    private string merchantHairType = "hair_merchant";
    private string merchantLegsType = "merchant_clothing_legs";
    private string merchantChestType = "merchant_clothing_chest";
    private string merchantHealth = "150";
    private string outputName = "merchant_example";
    private string outputId = "1";*/
    private string[] popupMods;
    ObjectTemplate curTemplate;
    Dictionary<string, ModuleInitializer> curInitializers = null;
    //private Boolean hasGuardProtect = false;
    //private Boolean replaceOldScript = false;
    //private string initializer;
    UnityEditorInternal.ReorderableList listOfItems;
    UnityEditorInternal.ReorderableList listOfContainers;
    //private static Dictionary<string, List<string>> templateList;
    private List<MerchantItem> merchantItemList = new List<MerchantItem>();
    private List<ContainerItem> containerItemList = new List<ContainerItem>();
    //private Dictionary<GameObject, List<GameObject>> merchantContainerList = new List<GameObject>();
}
