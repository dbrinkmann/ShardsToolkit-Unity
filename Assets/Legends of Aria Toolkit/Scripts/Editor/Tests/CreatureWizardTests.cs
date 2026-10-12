// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// See LICENSE in the repository root.
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.Animations;
using UnityEngine;

// EditMode tests for CreatureWizard: a creature and a mount built from a primitive "model" must
// pass CreatureValidator without issues, and the generated controller must have the layout the
// client expects.
public class CreatureWizardTests
{
    const string SoundPrefix = "event:/animals/wolf/wolf";

    readonly List<Object> created = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        foreach (Object item in created.Where(item => item != null))
        {
            Object.DestroyImmediate(item);
        }
        created.Clear();
    }

    [Test]
    public void CreatureFromWizardPassesValidation()
    {
        GameObject root = Track(CreatureWizard.BuildCreature(MakeModel(), MakeController(Settings(false)), Settings(false)));
        List<CreatureValidator.Issue> issues = CreatureValidator.ValidatePrefab(root, 1, Events());
        Assert.IsEmpty(issues, string.Join("\n", issues));
    }

    [Test]
    public void MountFromWizardPassesValidation()
    {
        CreatureWizard.CreatureSettings settings = Settings(true);
        GameObject root = Track(CreatureWizard.BuildCreature(MakeModel(), MakeController(settings), settings));
        List<CreatureValidator.Issue> issues = CreatureValidator.ValidatePrefab(root, 1, Events());
        Assert.IsEmpty(issues, string.Join("\n", issues));

        Transform mountPoint = root.transform.Find("Model/Hips/Spine/MountPoint");
        Assert.IsNotNull(mountPoint, "rider point under the chosen bone");
        Assert.AreEqual("MountPoint", root.GetComponent<EquipmentObject>().MountBone);
        Assert.Less(Vector3.Angle(mountPoint.up, Vector3.up), 0.1f);
    }

    [Test]
    public void SpeedFactorsAreTheInverseOfTheClipSpeeds()
    {
        CreatureWizard.CreatureSettings settings = Settings(false);
        settings.WalkSpeed = 1.5f;
        settings.RunSpeed = 5f;
        Mobile mobile = Track(CreatureWizard.BuildCreature(MakeModel(), MakeController(settings), settings)).GetComponent<Mobile>();
        Assert.AreEqual(0.67f, mobile.WalkAnimSpeed, 0.001f);
        Assert.AreEqual(0.2f, mobile.RunAnimSpeed, 0.001f);
    }

    [Test]
    public void ControllerHasTheClientLayout()
    {
        AnimatorController controller = MakeController(Settings(false));
        Assert.AreEqual(1, controller.layers.Length);
        Assert.AreEqual("Base", controller.layers[0].name);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        string[] states = machine.states.Select(item => item.state.name).ToArray();
        CollectionAssert.IsSubsetOf(new[] { "idle", "walk", "run", "attack", "attack1", "attack2", "was_hit", "die", "die_pose" }, states);
        Assert.AreEqual("idle", machine.defaultState.name);

        AnimatorState attack = machine.states.First(item => item.state.name == "attack").state;
        Assert.AreEqual(2, attack.behaviours.OfType<RandomAnimIndexBehaviour>().Single().numAnims);

        AnimatorState walk = machine.states.First(item => item.state.name == "walk").state;
        AnimatorStateTransition toRun = walk.transitions.Single(item => item.destinationState.name == "run");
        Assert.AreEqual(CreatureWizard.RunThreshold, toRun.conditions[0].threshold);

        AnimatorState diePose = machine.states.First(item => item.state.name == "die_pose").state;
        Assert.AreEqual(0f, diePose.speed);
        Assert.IsEmpty(machine.anyStateTransitions, "the client plays die itself");
    }

    [Test]
    public void SingleAttackNeedsNoPicker()
    {
        CreatureWizard.CreatureSettings settings = Settings(false);
        settings.Attacks = new List<AnimationClip> { Clip("bite") };
        AnimatorStateMachine machine = MakeController(settings).layers[0].stateMachine;
        Assert.IsFalse(machine.states.Any(item => item.state.name == "attack1"));
        Assert.IsEmpty(machine.states.First(item => item.state.name == "attack").state.behaviours);
    }

    // ---- helpers ----

    T Track<T>(T item) where T : Object
    {
        created.Add(item);
        return item;
    }

    HashSet<string> Events()
    {
        return new HashSet<string>(new[] { "_attack", "_pain", "_death", "_footsteps" }.Select(suffix => SoundPrefix + suffix));
    }

    CreatureWizard.CreatureSettings Settings(bool isMount)
    {
        return new CreatureWizard.CreatureSettings()
        {
            Name = isMount ? "TestMount" : "TestCreature",
            Idle = Clip("idle"),
            Walk = Clip("walk"),
            Run = Clip("run"),
            Die = Clip("die"),
            WasHit = Clip("hit"),
            Attacks = new List<AnimationClip> { Clip("attack_a"), Clip("attack_b") },
            SoundPrefix = SoundPrefix,
            IsMount = isMount,
            MountBonePath = isMount ? "Hips/Spine" : "",
        };
    }

    AnimationClip Clip(string name)
    {
        return Track(new AnimationClip() { name = name });
    }

    AnimatorController MakeController(CreatureWizard.CreatureSettings settings)
    {
        AnimatorController controller = Track(new AnimatorController());
        CreatureWizard.BuildController(controller, settings);
        return controller;
    }

    // A box "body" with a small bone chain, standing on the origin.
    GameObject MakeModel()
    {
        GameObject model = new GameObject("TestModel");
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(body.GetComponent<Collider>());
        body.name = "Body";
        body.transform.SetParent(model.transform, false);
        body.transform.localPosition = new Vector3(0, 0.8f, 0);
        body.transform.localScale = new Vector3(0.6f, 0.8f, 1.6f);

        GameObject hips = new GameObject("Hips");
        hips.transform.SetParent(model.transform, false);
        hips.transform.localPosition = new Vector3(0, 0.9f, -0.4f);
        GameObject spine = new GameObject("Spine");
        spine.transform.SetParent(hips.transform, false);
        spine.transform.localPosition = new Vector3(0, 0.1f, 0.4f);
        GameObject head = new GameObject("Head_Bone");
        head.transform.SetParent(spine.transform, false);
        head.transform.localPosition = new Vector3(0, 0.3f, 0.6f);
        return model;
    }
}
