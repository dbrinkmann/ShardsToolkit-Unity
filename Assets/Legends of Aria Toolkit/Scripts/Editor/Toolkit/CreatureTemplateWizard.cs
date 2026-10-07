// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// See LICENSE in the repository root.
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEngine;

// Writes the server templates for a creature prefab from one of the mod's libraries: the creature
// itself, copied from a base game creature that behaves the way you want, and for a mount also the
// statue item that summons it. Modders previously copied and edited these by hand.
public class CreatureTemplateWizard : EditorWindow
{
    const string DefaultMobBase = "wolf";
    const string DefaultMountBase = "bay_horse_saddled";
    const string StatueBaseRelativePath = "templates/items/item_statue_mount_bear.xml";

    GameObject creaturePrefab;
    string templateName = "";
    string displayName = "";
    float bodyOffset = 0.75f;
    int baseTemplateIndex;
    string[] baseTemplates = new string[0];
    bool makeStatue = true;

    // resolved from the mod's libraries when a prefab is picked
    int clientId;
    string bundleName;
    bool isMount;

    [MenuItem("LoA Toolkit/Custom Assets/Create Creature Templates", false, 8)]
    public static void ShowWindow()
    {
        CreatureTemplateWizard window = GetWindow<CreatureTemplateWizard>();
        window.titleContent = new GUIContent("Creature Templates");
        window.LoadBaseTemplates();
    }

    void OnGUI()
    {
        if (!ToolUtil.ValidateSettings() || !ToolUtil.ValidateModName())
        {
            EditorGUILayout.HelpBox("Set the base game folder, mods folder and mod name under LoA Toolkit/Settings first.", MessageType.Error);
            return;
        }

        EditorGUILayout.HelpBox(
            "Writes server templates for a creature prefab into the mod's templates folder: the creature, copied from a base game creature, "
            + "and for a mount also the statue item that summons it. The prefab must be in one of the mod's client object libraries.",
            MessageType.Info);

        GameObject picked = (GameObject)EditorGUILayout.ObjectField("Creature Prefab", creaturePrefab, typeof(GameObject), false);
        if (picked != creaturePrefab)
        {
            creaturePrefab = picked;
            OnPrefabPicked();
        }

        if (creaturePrefab == null)
        {
            return;
        }

        if (creaturePrefab.GetComponent<Mobile>() == null)
        {
            EditorGUILayout.HelpBox("This prefab has no Mobile component on its root, so it is not a creature.", MessageType.Error);
            return;
        }

        if (clientId <= 0)
        {
            EditorGUILayout.HelpBox("This prefab is not in any of the mod's client object libraries (LoA Toolkit/Settings). Add it to a library first; its slot there is its ClientId.", MessageType.Error);
            return;
        }

        EditorGUILayout.LabelField("ClientId", clientId + " in bundle \"" + bundleName + "\"" + (isMount ? " (mount)" : ""));
        templateName = EditorGUILayout.TextField(new GUIContent("Template Name", "File name of the template, without .xml. Used with /create."), templateName);
        displayName = EditorGUILayout.TextField("Display Name", displayName);
        bodyOffset = EditorGUILayout.FloatField(new GUIContent("Body Offset", "The creature's body radius in metres. The server uses it for collision and for melee, spell and chase ranges."), bodyOffset);

        if (baseTemplates.Length == 0)
        {
            EditorGUILayout.HelpBox("No base game creature templates found under templates/mobiles in the base game folder.", MessageType.Error);
            return;
        }
        // the base game creature whose AI, stats and loot to start from
        baseTemplateIndex = EditorGUILayout.Popup("Copy From", baseTemplateIndex, baseTemplates);

        if (isMount)
        {
            makeStatue = EditorGUILayout.Toggle(new GUIContent("Create Statue", "Also write \"" + templateName + "_statue\", the item that summons and mounts this creature."), makeStatue);
        }

        EditorGUILayout.Space();
        GUI.enabled = !string.IsNullOrEmpty(templateName.Trim());
        if (GUILayout.Button("Create Templates"))
        {
            CreateTemplates();
        }
        GUI.enabled = true;
    }

    void LoadBaseTemplates()
    {
        string folder = Path.Combine(ToolUtil.Settings.BasePath ?? "", "templates", "mobiles");
        baseTemplates = Directory.Exists(folder)
            ? Directory.GetFiles(folder, "*.xml").Select(Path.GetFileNameWithoutExtension).OrderBy(item => item).ToArray()
            : new string[0];
    }

    void OnPrefabPicked()
    {
        clientId = 0;
        bundleName = null;
        isMount = false;
        if (creaturePrefab == null)
        {
            return;
        }

        foreach (GameObject libraryObject in (ToolUtil.Settings.CustomObjectLibraries ?? new GameObject[0]).Where(item => item != null))
        {
            ClientObjectLibrary library = libraryObject.GetComponent<ClientObjectLibrary>();
            int index = library != null && library.ClientIdPrefabs != null ? Array.IndexOf(library.ClientIdPrefabs, creaturePrefab) : -1;
            if (index > 0)
            {
                clientId = index;
                bundleName = library.BundleName;
                break;
            }
        }

        isMount = creaturePrefab.GetComponents<EquipmentObject>().Any(item => item.EquipmentSlot == "Mount");
        displayName = ObjectNames.NicifyVariableName(creaturePrefab.name);
        templateName = "mod_" + creaturePrefab.name.ToLowerInvariant().Replace(' ', '_');
        bodyOffset = EstimateBodyOffset(creaturePrefab);

        LoadBaseTemplates();
        int defaultIndex = Array.IndexOf(baseTemplates, isMount ? DefaultMountBase : DefaultMobBase);
        baseTemplateIndex = Mathf.Max(0, defaultIndex);
    }

    // Half the smaller horizontal size of the creature's renderers, as a starting point.
    static float EstimateBodyOffset(GameObject prefab)
    {
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        try
        {
            instance.transform.position = Vector3.zero;
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return 0.75f;
            }

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1))
            {
                bounds.Encapsulate(renderer.bounds);
            }

            float radius = Mathf.Min(bounds.size.x, bounds.size.z) * 0.5f;
            return Mathf.Max(0.3f, Mathf.Round(radius * 20f) / 20f);
        }
        finally
        {
            DestroyImmediate(instance);
        }
    }

    void CreateTemplates()
    {
        string templatesFolder = Path.Combine(ToolUtil.Settings.ModsPath, ToolUtil.Settings.CurrentModName, "templates");
        string name = templateName.Trim();
        string statueName = name + "_statue";
        bool writeStatue = isMount && makeStatue;

        string creaturePath = Path.Combine(templatesFolder, name + ".xml");
        string statuePath = Path.Combine(templatesFolder, statueName + ".xml");
        string[] existing = new[] { creaturePath, writeStatue ? statuePath : null }.Where(item => item != null && File.Exists(item)).ToArray();

        string confirm = "Write " + creaturePath + (writeStatue ? "\nand " + statuePath : "") + "?";
        if (existing.Length > 0)
        {
            confirm += "\n\nThis overwrites: " + string.Join(", ", existing.Select(Path.GetFileName).ToArray());
        }
        if (!EditorUtility.DisplayDialog("Create Creature Templates", confirm, "Write", "Cancel"))
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(templatesFolder);

            XmlDocument baseCreature = new XmlDocument();
            baseCreature.Load(Path.Combine(ToolUtil.Settings.BasePath, "templates", "mobiles", baseTemplates[baseTemplateIndex] + ".xml"));
            Save(BuildCreatureTemplate(baseCreature, clientId, bundleName, displayName, bodyOffset, writeStatue ? statueName : null), creaturePath);

            if (writeStatue)
            {
                XmlDocument baseStatue = new XmlDocument();
                baseStatue.Load(Path.Combine(ToolUtil.Settings.BasePath, StatueBaseRelativePath));
                Save(BuildStatueTemplate(baseStatue, displayName, name), statuePath);
            }

            EditorUtility.DisplayDialog("Creature Templates",
                "Written. Reload templates or restart the server, then spawn with /create " + (writeStatue ? statueName : name) + ".\n\n"
                + "AI, stats and loot come from \"" + baseTemplates[baseTemplateIndex] + "\"; edit the template to tune them.", "OK");
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("Creature Templates", "Could not write the templates: " + ex.Message, "OK");
        }
    }

    static void Save(XmlDocument document, string path)
    {
        XmlWriterSettings settings = new XmlWriterSettings() { Indent = true, IndentChars = "\t", OmitXmlDeclaration = true };
        using (XmlWriter writer = XmlWriter.Create(path, settings))
        {
            document.Save(writer);
        }
    }

    // ---- template transforms (public for tests) ----

    // A copy of a base creature template that spawns the given custom prefab. ScaleModifier is
    // removed because a custom prefab is sized in the prefab itself. For a mount, PurchaseTemplate
    // (used by stables) is pointed at the statue.
    public static XmlDocument BuildCreatureTemplate(XmlDocument baseTemplate, int clientId, string bundleName, string displayName, float bodyOffset, string statueTemplateName)
    {
        XmlDocument document = (XmlDocument)baseTemplate.CloneNode(true);
        XmlElement root = document.DocumentElement;

        XmlElement clientIdElement = SetChild(root, "ClientId", clientId.ToString(CultureInfo.InvariantCulture), null);
        SetChild(root, "CustomAssetBundle", bundleName, clientIdElement);
        XmlElement nameElement = SetChild(root, "Name", displayName, root.ChildNodes.OfType<XmlElement>().First(item => item.Name == "CustomAssetBundle"));

        XmlElement bodyOffsetEntry = root.ChildNodes.OfType<XmlElement>()
            .FirstOrDefault(item => item.Name == "SharedStateEntry" && item.GetAttribute("name") == "BodyOffset");
        if (bodyOffsetEntry == null)
        {
            bodyOffsetEntry = document.CreateElement("SharedStateEntry");
            bodyOffsetEntry.SetAttribute("name", "BodyOffset");
            bodyOffsetEntry.SetAttribute("type", "double");
            root.InsertAfter(bodyOffsetEntry, nameElement);
        }
        bodyOffsetEntry.SetAttribute("value", bodyOffset.ToString("0.##", CultureInfo.InvariantCulture));

        foreach (XmlElement scale in root.ChildNodes.OfType<XmlElement>().Where(item => item.Name == "ScaleModifier").ToList())
        {
            root.RemoveChild(scale);
        }

        if (!string.IsNullOrEmpty(statueTemplateName))
        {
            XmlElement purchase = root.SelectNodes("ObjectVariableComponent/StringVariable").OfType<XmlElement>()
                .FirstOrDefault(item => item.GetAttribute("Name") == "PurchaseTemplate");
            if (purchase != null)
            {
                purchase.InnerText = statueTemplateName;
            }
        }

        return document;
    }

    // A copy of a base mount statue that summons the given creature template.
    public static XmlDocument BuildStatueTemplate(XmlDocument baseStatue, string creatureDisplayName, string creatureTemplateName)
    {
        XmlDocument document = (XmlDocument)baseStatue.CloneNode(true);
        XmlElement root = document.DocumentElement;
        SetChild(root, "Name", "[FF9500]" + creatureDisplayName + " Statue[-]", null);

        XmlElement variables = root.ChildNodes.OfType<XmlElement>().FirstOrDefault(item => item.Name == "ObjectVariableComponent");
        if (variables == null)
        {
            variables = document.CreateElement("ObjectVariableComponent");
            root.AppendChild(variables);
        }

        XmlElement mountTemplate = variables.ChildNodes.OfType<XmlElement>()
            .FirstOrDefault(item => item.Name == "StringVariable" && item.GetAttribute("Name") == "StatueMountTemplate");
        if (mountTemplate == null)
        {
            mountTemplate = document.CreateElement("StringVariable");
            mountTemplate.SetAttribute("Name", "StatueMountTemplate");
            variables.AppendChild(mountTemplate);
        }
        mountTemplate.InnerText = creatureTemplateName;
        return document;
    }

    // Sets the text of root's first child element with this name, creating it after insertAfter
    // (or first) if it does not exist.
    static XmlElement SetChild(XmlElement root, string name, string value, XmlElement insertAfter)
    {
        XmlElement element = root.ChildNodes.OfType<XmlElement>().FirstOrDefault(item => item.Name == name);
        if (element == null)
        {
            element = root.OwnerDocument.CreateElement(name);
            if (insertAfter != null)
            {
                root.InsertAfter(element, insertAfter);
            }
            else
            {
                root.PrependChild(element);
            }
        }
        element.InnerText = value;
        return element;
    }
}
