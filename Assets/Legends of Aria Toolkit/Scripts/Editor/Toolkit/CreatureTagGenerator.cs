// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// See LICENSE in the repository root.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEngine;

// Keeps the "Mobile" and "Mount" tags in a bundle's ObjectTagDefinitions.xml in step with the
// library. Custom bundles do not inherit the base game's tags, so without these the server does
// not give custom creatures the shared state they need (BodyOffset, IsDead, Pose, ...) and does
// not treat custom mounts as mountable.
//
// The toolkit owns the ClientId lists of these two tags: they are regenerated from the library on
// every build. Shared state entries the base game defines are added if missing; entries already in
// the file (including changed values) are kept. Every other tag and comment in the file is left
// as it is.
public static class CreatureTagGenerator
{
    public const string MobileTagName = "Mobile";
    public const string MountTagName = "Mount";

    public static void UpdateTags(ClientObjectLibrary library, string tagFilePath)
    {
        List<int> creatureIds = new List<int>();
        List<int> mountIds = new List<int>();
        for (int i = 1; i < library.ClientIdPrefabs.Length; i++)
        {
            GameObject prefab = library.ClientIdPrefabs[i];
            if (prefab == null || prefab.GetComponent<Mobile>() == null)
            {
                continue;
            }

            creatureIds.Add(i);
            if (prefab.GetComponents<EquipmentObject>().Any(item => item.EquipmentSlot == "Mount"))
            {
                mountIds.Add(i);
            }
        }

        // An ObjectTag without ClientIds applies to every object, so never write an empty list.
        if (creatureIds.Count == 0)
        {
            return;
        }

        XmlDocument document = new XmlDocument();
        if (File.Exists(tagFilePath))
        {
            document.Load(tagFilePath);
        }
        if (document.DocumentElement == null)
        {
            document.RemoveAll();
            document.AppendChild(document.CreateXmlDeclaration("1.0", "utf-8", null));
            document.AppendChild(document.CreateElement("ObjectTags"));
        }

        Dictionary<string, List<XmlElement>> baseEntries = LoadBaseTagEntries();
        if (!baseEntries.ContainsKey(MobileTagName))
        {
            Debug.LogWarning("[Creature tags] The base game's Mobile tag was not found under the base game folder (LoA Toolkit/Settings). "
                + "The bundle's Mobile tag lists the creature ClientIds but may be missing shared state entries such as BodyOffset.");
        }

        UpdateDocument(document, creatureIds, mountIds, baseEntries);

        XmlWriterSettings settings = new XmlWriterSettings() { Indent = true, IndentChars = "    " };
        using (XmlWriter writer = XmlWriter.Create(tagFilePath, settings))
        {
            document.Save(writer);
        }
    }

    // Applies the creature and mount ClientIds to a tag document (public for tests).
    public static void UpdateDocument(XmlDocument document, List<int> creatureIds, List<int> mountIds, Dictionary<string, List<XmlElement>> baseEntries)
    {
        if (creatureIds.Count > 0)
        {
            UpdateTag(document, MobileTagName, creatureIds, baseEntries);
        }
        if (mountIds.Count > 0)
        {
            UpdateTag(document, MountTagName, mountIds, baseEntries);
        }
    }

    static void UpdateTag(XmlDocument document, string tagName, List<int> clientIds, Dictionary<string, List<XmlElement>> baseEntries)
    {
        XmlElement tag = FindTag(document.DocumentElement, tagName);
        if (tag == null)
        {
            tag = document.CreateElement("ObjectTag");
            XmlElement name = document.CreateElement("Name");
            name.InnerText = tagName;
            tag.AppendChild(name);
            document.DocumentElement.AppendChild(tag);
        }

        HashSet<string> existingEntries = new HashSet<string>(tag.ChildNodes.OfType<XmlElement>()
            .Where(item => item.Name == "SharedStateEntry")
            .Select(item => item.GetAttribute("name")));

        List<XmlElement> entries;
        if (baseEntries.TryGetValue(tagName, out entries))
        {
            foreach (XmlElement entry in entries.Where(item => !existingEntries.Contains(item.GetAttribute("name"))))
            {
                tag.AppendChild(document.ImportNode(entry, true));
            }
        }

        foreach (XmlElement oldId in tag.ChildNodes.OfType<XmlElement>().Where(item => item.Name == "ClientId").ToList())
        {
            tag.RemoveChild(oldId);
        }

        foreach (int clientId in clientIds)
        {
            XmlElement idElement = document.CreateElement("ClientId");
            idElement.InnerText = clientId.ToString();
            tag.AppendChild(idElement);
        }
    }

    // Shared state entries of the base game's Mobile and Mount tags. The base game has two tag
    // files whose Mobile tags differ (StatusIconOverride/Guild/GuildWars in one, IsJumping in the
    // other), so this takes the union, the main file winning where both define an entry.
    static Dictionary<string, List<XmlElement>> LoadBaseTagEntries()
    {
        Dictionary<string, List<XmlElement>> result = new Dictionary<string, List<XmlElement>>();
        string basePath = ToolUtil.Settings != null ? ToolUtil.Settings.BasePath : null;
        if (string.IsNullOrEmpty(basePath))
        {
            return result;
        }

        string[] files =
        {
            Path.Combine(basePath, "ObjectTagDefinitions.xml"),
            Path.Combine(basePath, "assetbundles", "Official", "ObjectTagDefinitions.xml"),
        };

        List<XmlDocument> documents = new List<XmlDocument>();
        foreach (string file in files.Where(File.Exists))
        {
            XmlDocument document = new XmlDocument();
            try
            {
                document.Load(file);
                documents.Add(document);
            }
            catch (XmlException ex)
            {
                Debug.LogWarning("[Creature tags] Could not read " + file + ": " + ex.Message);
            }
        }

        return CollectBaseTagEntries(documents);
    }

    // The union of the Mobile and Mount shared state entries in the given tag documents, earlier
    // documents winning (public for tests).
    public static Dictionary<string, List<XmlElement>> CollectBaseTagEntries(IEnumerable<XmlDocument> documents)
    {
        Dictionary<string, List<XmlElement>> result = new Dictionary<string, List<XmlElement>>();
        foreach (XmlDocument document in documents)
        {
            foreach (string tagName in new[] { MobileTagName, MountTagName })
            {
                XmlElement tag = FindTag(document.DocumentElement, tagName);
                if (tag == null)
                {
                    continue;
                }

                List<XmlElement> entries;
                if (!result.TryGetValue(tagName, out entries))
                {
                    entries = new List<XmlElement>();
                    result[tagName] = entries;
                }

                foreach (XmlElement entry in tag.ChildNodes.OfType<XmlElement>().Where(item => item.Name == "SharedStateEntry"))
                {
                    if (!entries.Any(item => item.GetAttribute("name") == entry.GetAttribute("name")))
                    {
                        entries.Add(entry);
                    }
                }
            }
        }

        return result;
    }

    static XmlElement FindTag(XmlElement root, string tagName)
    {
        if (root == null)
        {
            return null;
        }

        return root.ChildNodes.OfType<XmlElement>().FirstOrDefault(tag =>
            tag.Name == "ObjectTag"
            && tag.ChildNodes.OfType<XmlElement>().Any(child => child.Name == "Name" && child.InnerText.Trim() == tagName));
    }
}
