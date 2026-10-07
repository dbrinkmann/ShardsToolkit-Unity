// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// See LICENSE in the repository root.
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using NUnit.Framework;

// EditMode tests for the creature tag generator and the creature template transforms. They work
// on XML in memory, so they need no base game folder.
public class CreatureServerDataTests
{
    const string BaseTags =
        "<ObjectTags>"
        + "<ObjectTag><Name>Mobile</Name>"
        + "<SharedStateEntry name=\"IsDead\" type=\"bool\" value=\"False\"/>"
        + "<SharedStateEntry name=\"BodyOffset\" type=\"double\" value=\"0.8\"/>"
        + "<SharedStateEntry name=\"StatusIconOverride\" type=\"string\" value=\"\"/>"
        + "<ClientId>1</ClientId></ObjectTag>"
        + "<ObjectTag><Name>Mount</Name>"
        + "<SharedStateEntry name=\"EquipSlot\" type=\"string\" value=\"Mount\" restriction=\"readonly\"/>"
        + "<ClientId>59</ClientId></ObjectTag>"
        + "</ObjectTags>";

    const string OfficialTags =
        "<ObjectTags>"
        + "<ObjectTag><Name>Mobile</Name>"
        + "<SharedStateEntry name=\"BodyOffset\" type=\"double\" value=\"1.0\"/>"
        + "<SharedStateEntry name=\"IsJumping\" type=\"bool\" value=\"False\"/>"
        + "<ClientId>2561</ClientId></ObjectTag>"
        + "</ObjectTags>";

    const string CreatureBase =
        "<ObjectTemplate>"
        + "<ClientId>22</ClientId>"
        + "<Name>Wolf</Name>"
        + "<SharedStateEntry name=\"BodyOffset\" type=\"double\" value=\".75\"/>"
        + "<ScaleModifier>.6</ScaleModifier>"
        + "<MobileComponent><BaseRunSpeed>0.9</BaseRunSpeed><MobileType>Animal</MobileType></MobileComponent>"
        + "<ObjectVariableComponent><StringVariable Name=\"PurchaseTemplate\">item_statue_mount_bay_horse</StringVariable></ObjectVariableComponent>"
        + "</ObjectTemplate>";

    const string StatueBase =
        "<ObjectTemplate>"
        + "<ClientId>1110</ClientId>"
        + "<Name>[FF9500]Saddled Bear Statue[-]</Name>"
        + "<ObjectVariableComponent>"
        + "<StringVariable Name=\"ResourceType\">MountStatue</StringVariable>"
        + "<StringVariable Name=\"StatueMountTemplate\">ridable_bear_saddled</StringVariable>"
        + "</ObjectVariableComponent>"
        + "</ObjectTemplate>";

    // ---- tags ----

    [Test]
    public void BaseEntriesAreTheUnionWithTheMainFileWinning()
    {
        Dictionary<string, List<XmlElement>> entries = CreatureTagGenerator.CollectBaseTagEntries(new[] { Parse(BaseTags), Parse(OfficialTags) });

        CollectionAssert.AreEquivalent(new[] { "IsDead", "BodyOffset", "StatusIconOverride", "IsJumping" }, entries["Mobile"].Select(item => item.GetAttribute("name")));
        Assert.AreEqual("0.8", entries["Mobile"].First(item => item.GetAttribute("name") == "BodyOffset").GetAttribute("value"));
        CollectionAssert.AreEqual(new[] { "EquipSlot" }, entries["Mount"].Select(item => item.GetAttribute("name")));
    }

    [Test]
    public void MissingTagsAreCreatedWithBaseEntriesAndIds()
    {
        XmlDocument document = Parse("<ObjectTags/>");
        CreatureTagGenerator.UpdateDocument(document, new List<int> { 6, 7 }, new List<int> { 7 }, BaseEntries());

        XmlElement mobile = FindTag(document, "Mobile");
        CollectionAssert.AreEqual(new[] { "6", "7" }, Ids(mobile));
        CollectionAssert.AreEquivalent(new[] { "IsDead", "BodyOffset", "StatusIconOverride", "IsJumping" }, EntryNames(mobile));

        XmlElement mount = FindTag(document, "Mount");
        CollectionAssert.AreEqual(new[] { "7" }, Ids(mount));
        CollectionAssert.AreEqual(new[] { "EquipSlot" }, EntryNames(mount));
    }

    [Test]
    public void ExistingTagKeepsChangedValuesAndOtherContentButGetsNewIds()
    {
        XmlDocument document = Parse(
            "<ObjectTags>"
            + "<!-- hand-written -->"
            + "<ObjectTag><Name>Trade</Name><ClientId>3</ClientId></ObjectTag>"
            + "<ObjectTag><Name>Mobile</Name>"
            + "<SharedStateEntry name=\"BodyOffset\" type=\"double\" value=\"1.5\"/>"
            + "<ClientId>4</ClientId><ClientId>5</ClientId></ObjectTag>"
            + "</ObjectTags>");

        CreatureTagGenerator.UpdateDocument(document, new List<int> { 9 }, new List<int>(), BaseEntries());

        XmlElement mobile = FindTag(document, "Mobile");
        CollectionAssert.AreEqual(new[] { "9" }, Ids(mobile));
        Assert.AreEqual("1.5", mobile.ChildNodes.OfType<XmlElement>().First(item => item.GetAttribute("name") == "BodyOffset").GetAttribute("value"));
        Assert.AreEqual(1, EntryNames(mobile).Count(item => item == "BodyOffset"));
        CollectionAssert.Contains(EntryNames(mobile), "IsDead");

        CollectionAssert.AreEqual(new[] { "3" }, Ids(FindTag(document, "Trade")));
        Assert.AreEqual(1, document.DocumentElement.ChildNodes.OfType<XmlComment>().Count());
        Assert.IsNull(FindTag(document, "Mount"), "no Mount tag without mounts");
    }

    [Test]
    public void NoCreaturesLeavesTheDocumentAlone()
    {
        XmlDocument document = Parse("<ObjectTags><ObjectTag><Name>Mobile</Name><ClientId>4</ClientId></ObjectTag></ObjectTags>");
        CreatureTagGenerator.UpdateDocument(document, new List<int>(), new List<int>(), BaseEntries());
        CollectionAssert.AreEqual(new[] { "4" }, Ids(FindTag(document, "Mobile")));
    }

    // ---- templates ----

    [Test]
    public void CreatureTemplatePointsAtTheCustomPrefab()
    {
        XmlDocument template = CreatureTemplateWizard.BuildCreatureTemplate(Parse(CreatureBase), 11, "mymod", "Blockhound", 0.6f, null);
        XmlElement root = template.DocumentElement;

        Assert.AreEqual("11", Child(root, "ClientId").InnerText);
        Assert.AreEqual("mymod", Child(root, "CustomAssetBundle").InnerText);
        Assert.AreEqual("CustomAssetBundle", Child(root, "ClientId").NextSibling.Name, "CustomAssetBundle follows ClientId");
        Assert.AreEqual("Blockhound", Child(root, "Name").InnerText);
        Assert.AreEqual("0.6", BodyOffset(root).GetAttribute("value"));
        Assert.IsNull(Child(root, "ScaleModifier"));
        Assert.AreEqual("0.9", root.SelectSingleNode("MobileComponent/BaseRunSpeed").InnerText, "AI and stats come from the base");
        Assert.AreEqual("item_statue_mount_bay_horse", root.SelectSingleNode("ObjectVariableComponent/StringVariable").InnerText, "PurchaseTemplate untouched for a mob");
    }

    [Test]
    public void MountTemplatePointsPurchaseAtItsStatue()
    {
        XmlDocument template = CreatureTemplateWizard.BuildCreatureTemplate(Parse(CreatureBase), 12, "mymod", "Strider", 1.1f, "mod_strider_statue");
        Assert.AreEqual("mod_strider_statue", template.DocumentElement.SelectSingleNode("ObjectVariableComponent/StringVariable").InnerText);
    }

    [Test]
    public void BodyOffsetIsAddedWhenTheBaseHasNone()
    {
        XmlDocument baseTemplate = Parse("<ObjectTemplate><ClientId>1</ClientId><Name>Thing</Name></ObjectTemplate>");
        XmlElement root = CreatureTemplateWizard.BuildCreatureTemplate(baseTemplate, 5, "mymod", "Thing", 0.75f, null).DocumentElement;
        Assert.AreEqual("0.75", BodyOffset(root).GetAttribute("value"));
        Assert.AreEqual("double", BodyOffset(root).GetAttribute("type"));
    }

    [Test]
    public void BaseTemplateIsNotModified()
    {
        XmlDocument baseTemplate = Parse(CreatureBase);
        CreatureTemplateWizard.BuildCreatureTemplate(baseTemplate, 11, "mymod", "Blockhound", 0.6f, null);
        Assert.AreEqual("22", Child(baseTemplate.DocumentElement, "ClientId").InnerText);
        Assert.IsNull(Child(baseTemplate.DocumentElement, "CustomAssetBundle"));
    }

    [Test]
    public void StatueSummonsTheCreatureTemplate()
    {
        XmlElement root = CreatureTemplateWizard.BuildStatueTemplate(Parse(StatueBase), "Strider", "mod_strider").DocumentElement;
        Assert.AreEqual("[FF9500]Strider Statue[-]", Child(root, "Name").InnerText);
        Assert.AreEqual("1110", Child(root, "ClientId").InnerText, "statue keeps the base game's statue art");
        XmlElement mountTemplate = root.SelectNodes("ObjectVariableComponent/StringVariable").OfType<XmlElement>().First(item => item.GetAttribute("Name") == "StatueMountTemplate");
        Assert.AreEqual("mod_strider", mountTemplate.InnerText);
        Assert.IsTrue(root.SelectNodes("ObjectVariableComponent/StringVariable").OfType<XmlElement>().Any(item => item.GetAttribute("Name") == "ResourceType" && item.InnerText == "MountStatue"));
    }

    // ---- helpers ----

    Dictionary<string, List<XmlElement>> BaseEntries()
    {
        return CreatureTagGenerator.CollectBaseTagEntries(new[] { Parse(BaseTags), Parse(OfficialTags) });
    }

    static XmlDocument Parse(string xml)
    {
        XmlDocument document = new XmlDocument();
        document.LoadXml(xml);
        return document;
    }

    static XmlElement FindTag(XmlDocument document, string name)
    {
        return document.DocumentElement.ChildNodes.OfType<XmlElement>()
            .FirstOrDefault(tag => tag.Name == "ObjectTag" && Child(tag, "Name") != null && Child(tag, "Name").InnerText == name);
    }

    static XmlElement Child(XmlElement parent, string name)
    {
        return parent.ChildNodes.OfType<XmlElement>().FirstOrDefault(item => item.Name == name);
    }

    static XmlElement BodyOffset(XmlElement root)
    {
        return root.ChildNodes.OfType<XmlElement>().First(item => item.Name == "SharedStateEntry" && item.GetAttribute("name") == "BodyOffset");
    }

    static List<string> Ids(XmlElement tag)
    {
        return tag.ChildNodes.OfType<XmlElement>().Where(item => item.Name == "ClientId").Select(item => item.InnerText).ToList();
    }

    static List<string> EntryNames(XmlElement tag)
    {
        return tag.ChildNodes.OfType<XmlElement>().Where(item => item.Name == "SharedStateEntry").Select(item => item.GetAttribute("name")).ToList();
    }
}
