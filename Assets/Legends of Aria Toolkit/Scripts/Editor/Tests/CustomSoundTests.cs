// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// See LICENSE in the repository root.
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.Animations;
using UnityEngine;

// EditMode tests for mod sounds: file naming, default settings by name, and the validator's checks
// of mod:/ sound paths.
public class CustomSoundTests
{
    [TestCase("wolf_attack.ogg", "wolf_attack")]
    [TestCase("Wolf_Attack_2.ogg", "wolf_attack")]
    [TestCase("wolf_attack_12.wav", "wolf_attack")]
    [TestCase("tavern music.ogg", "tavern_music")]
    [TestCase("hound_v2.ogg", "hound_v2")]
    public void FileNamesBecomeLowercaseSoundNamesWithoutVariantNumbers(string fileName, string soundName)
    {
        Assert.AreEqual(soundName, CustomSoundBuilder.GetSoundName(fileName));
    }

    [Test]
    public void DefaultsDependOnTheName()
    {
        CustomSoundLibrary.SoundEntry ambient = CustomSoundBuilder.DefaultsFor("wolf_environment");
        Assert.IsTrue(ambient.Loop);
        Assert.AreEqual("ambience", ambient.Group);

        CustomSoundLibrary.SoundEntry music = CustomSoundBuilder.DefaultsFor("tavern_music");
        Assert.AreEqual("music", music.Group);
        Assert.IsFalse(music.Is3D);
        Assert.IsTrue(music.Loop);

        CustomSoundLibrary.SoundEntry attack = CustomSoundBuilder.DefaultsFor("wolf_attack");
        Assert.AreEqual("sfx", attack.Group);
        Assert.IsTrue(attack.Is3D);
        Assert.IsFalse(attack.Loop);
        Assert.Less(attack.PitchMin, attack.PitchMax, "slight pitch variation for repeated effects");

        Assert.Less(CustomSoundBuilder.DefaultsFor("wolf_footsteps").MaxDistance, attack.MaxDistance);
    }

    [Test]
    public void ModPathIsBundleAndName()
    {
        Assert.AreEqual("mod:/mymod/wolf_attack", CustomSoundBuilder.ModPath("mymod", "wolf_attack"));
    }

    [Test]
    public void ValidatorChecksModSoundsAgainstTheLibrary()
    {
        GameObject creature = MakeCreature("mod:/mymod/hound");
        try
        {
            HashSet<string> modSounds = new HashSet<string>(new[] { "_attack", "_death", "_footsteps" }.Select(suffix => "mod:/mymod/hound" + suffix));
            List<CreatureValidator.Issue> issues = CreatureValidator.ValidatePrefab(creature, 1, new HashSet<string>(), modSounds);
            Assert.AreEqual(1, issues.Count, string.Join("\n", issues));
            StringAssert.Contains("not in the library's Sounds folder: mod:/mymod/hound_pain", issues[0].Message);

            modSounds.Add("mod:/mymod/hound_pain");
            Assert.IsEmpty(CreatureValidator.ValidatePrefab(creature, 1, new HashSet<string>(), modSounds));
        }
        finally
        {
            Object.DestroyImmediate(creature);
        }
    }

    [Test]
    public void ModSoundsAreNotCheckedWithoutTheLibrary()
    {
        GameObject creature = MakeCreature("mod:/mymod/hound");
        try
        {
            Assert.IsEmpty(CreatureValidator.ValidatePrefab(creature, 1, new HashSet<string>(), null));
        }
        finally
        {
            Object.DestroyImmediate(creature);
        }
    }

    // a minimal valid creature (see CreatureValidatorTests) with the given sound prefix
    static GameObject MakeCreature(string soundPrefix)
    {
        GameObject root = new GameObject("SoundTestCreature");
        root.AddComponent<Mobile>().FMODAudioPath = soundPrefix;

        GameObject model = new GameObject("Model");
        model.transform.SetParent(root.transform);
        AnimatorController controller = new AnimatorController();
        controller.AddLayer("Base");
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);
        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState idle = machine.AddState("idle");
        machine.AddState("walk");
        machine.AddState("run");
        machine.AddState("attack");
        machine.AddState("die").AddTransition(idle).AddCondition(AnimatorConditionMode.IfNot, 0f, "IsDead");
        Animator animator = model.AddComponent<Animator>();
        animator.applyRootMotion = false;
        animator.runtimeAnimatorController = controller;

        new GameObject("OverheadAnchor").transform.SetParent(root.transform);
        new GameObject("EffectHitPoint").transform.SetParent(root.transform);
        GameObject picker = new GameObject("PickerBounds");
        picker.transform.SetParent(root.transform);
        picker.layer = 19;
        picker.AddComponent<BoxCollider>();
        return root;
    }
}
