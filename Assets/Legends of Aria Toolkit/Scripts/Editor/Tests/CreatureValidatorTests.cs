// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// See LICENSE in the repository root.
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.Animations;
using UnityEngine;

// EditMode tests for CreatureValidator. Each test builds a valid creature in memory, breaks one
// thing, and checks that exactly the matching rule fires.
public class CreatureValidatorTests
{
    const string SoundPrefix = "event:/animals/wolf/wolf";

    readonly List<Object> created = new List<Object>();
    HashSet<string> fmodEvents;

    [SetUp]
    public void SetUp()
    {
        fmodEvents = new HashSet<string>(new[] { "_attack", "_pain", "_death", "_footsteps" }.Select(suffix => SoundPrefix + suffix));
    }

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
    public void ValidCreatureHasNoIssues()
    {
        GameObject creature = MakeCreature();
        Assert.IsEmpty(Validate(creature), string.Join("\n", Validate(creature)));
    }

    [Test]
    public void ValidMountHasNoIssues()
    {
        GameObject mount = MakeMount();
        Assert.IsEmpty(Validate(mount), string.Join("\n", Validate(mount)));
    }

    [Test]
    public void PrefabWithoutMobileIsIgnored()
    {
        GameObject prop = Track(new GameObject("Barrel"));
        prop.AddComponent<ClientObject>();
        Assert.IsEmpty(Validate(prop));
    }

    [Test]
    public void MissingModelIsAnError()
    {
        GameObject creature = MakeCreature();
        creature.transform.Find("Model").name = "Mesh";
        AssertIssue(creature, CreatureValidator.Severity.Error, "\"Model\"");
    }

    [Test]
    public void AnimatorWithoutControllerIsAnError()
    {
        GameObject creature = MakeCreature();
        creature.GetComponentInChildren<Animator>().runtimeAnimatorController = null;
        AssertIssue(creature, CreatureValidator.Severity.Error, "has no controller");
    }

    [Test]
    public void MissingOverheadAnchorIsAnError()
    {
        GameObject creature = MakeCreature();
        Object.DestroyImmediate(creature.transform.Find("OverheadAnchor").gameObject);
        AssertIssue(creature, CreatureValidator.Severity.Error, "OverheadAnchor");
    }

    [Test]
    public void OverheadAnchorUnderModelIsAccepted()
    {
        GameObject creature = MakeCreature();
        creature.transform.Find("OverheadAnchor").SetParent(creature.transform.Find("Model"));
        Assert.IsEmpty(Validate(creature));
    }

    [Test]
    public void ClientIdAboveClientLimitIsAnError()
    {
        GameObject creature = MakeCreature();
        AssertIssue(creature, CreatureValidator.Severity.Error, "ClientId", CreatureValidator.MaxClientIdForCurrentClient + 1);
    }

    [Test]
    public void HasCustomAnimationsIsAnError()
    {
        GameObject creature = MakeCreature();
        creature.GetComponent<Mobile>().HasCustomAnimations = true;
        AssertIssue(creature, CreatureValidator.Severity.Error, "HasCustomAnimations");
    }

    [Test]
    public void FootstepHandlerOnRootIsAnError()
    {
        GameObject creature = MakeCreature();
        Object.DestroyImmediate(creature.GetComponentInChildren<FootstepHandler>());
        creature.AddComponent<FootstepHandler>();
        AssertIssue(creature, CreatureValidator.Severity.Error, "FootstepHandler");
    }

    [Test]
    public void ToolkitLocalPlayerIsAnError()
    {
        GameObject creature = MakeCreature();
        creature.AddComponent<LocalPlayer>();
        AssertIssue(creature, CreatureValidator.Severity.Error, "LocalPlayer");
    }

    [Test]
    public void SpeedFactorsOfOneAreAWarning()
    {
        GameObject creature = MakeCreature();
        Mobile mobile = creature.GetComponent<Mobile>();
        mobile.WalkAnimSpeed = 1f;
        mobile.RunAnimSpeed = 1f;
        AssertIssue(creature, CreatureValidator.Severity.Warning, "both 1");
    }

    [Test]
    public void SwappedSpeedFactorsAreAWarning()
    {
        GameObject creature = MakeCreature();
        Mobile mobile = creature.GetComponent<Mobile>();
        mobile.WalkAnimSpeed = 0.1f;
        mobile.RunAnimSpeed = 0.2f;
        AssertIssue(creature, CreatureValidator.Severity.Warning, "swapped");
    }

    [Test]
    public void RootScaleIsAWarning()
    {
        GameObject creature = MakeCreature();
        creature.transform.localScale = Vector3.one * 2f;
        AssertIssue(creature, CreatureValidator.Severity.Warning, "scale");
    }

    [Test]
    public void BaseLayerNameIsAWarning()
    {
        GameObject creature = MakeCreature();
        AnimatorController controller = (AnimatorController)creature.GetComponentInChildren<Animator>().runtimeAnimatorController;
        AnimatorControllerLayer[] layers = controller.layers;
        layers[0].name = "Base Layer";
        controller.layers = layers;
        AssertIssue(creature, CreatureValidator.Severity.Warning, "Rename it to \"Base\"");
    }

    [Test]
    public void MissingStateIsAWarning()
    {
        GameObject creature = MakeCreature();
        AnimatorStateMachine stateMachine = GetController(creature).layers[0].stateMachine;
        stateMachine.RemoveState(stateMachine.states.First(item => item.state.name == "attack").state);
        AssertIssue(creature, CreatureValidator.Severity.Warning, "Missing animator states: attack");
    }

    [Test]
    public void MissingCycleParametersWithFootstepsAreAWarning()
    {
        GameObject creature = MakeCreature();
        AnimatorController controller = GetController(creature);
        controller.RemoveParameter(controller.parameters.First(item => item.name == "WalkCycle"));
        AssertIssue(creature, CreatureValidator.Severity.Warning, "WalkCycle");
    }

    [Test]
    public void DieWithoutReviveTransitionIsAWarning()
    {
        GameObject creature = MakeCreature();
        AnimatorState die = GetController(creature).layers[0].stateMachine.states.First(item => item.state.name == "die").state;
        foreach (AnimatorStateTransition transition in die.transitions.ToArray())
        {
            die.RemoveTransition(transition);
        }
        AssertIssue(creature, CreatureValidator.Severity.Warning, "IsDead = false");
    }

    [Test]
    public void AttackVariantsWithoutPickerAreAWarning()
    {
        GameObject creature = MakeCreature();
        AnimatorStateMachine stateMachine = GetController(creature).layers[0].stateMachine;
        stateMachine.AddState("attack1");
        stateMachine.AddState("attack2");
        AssertIssue(creature, CreatureValidator.Severity.Warning, "RandomAnimIndexBehaviour");
    }

    [Test]
    public void AttackVariantsWithPickerAreAccepted()
    {
        GameObject creature = MakeCreature();
        AnimatorStateMachine stateMachine = GetController(creature).layers[0].stateMachine;
        stateMachine.AddState("attack1");
        stateMachine.AddState("attack2");
        AnimatorState attack = stateMachine.states.First(item => item.state.name == "attack").state;
        attack.AddStateMachineBehaviour<RandomAnimIndexBehaviour>().numAnims = 2;
        Assert.IsEmpty(Validate(creature), string.Join("\n", Validate(creature)));
    }

    [Test]
    public void PickerOnWrongLayerIsAWarning()
    {
        GameObject creature = MakeCreature();
        creature.transform.Find("PickerBounds").gameObject.layer = 0;
        AssertIssue(creature, CreatureValidator.Severity.Warning, "layer 19");
    }

    [Test]
    public void ColliderChildIsAWarning()
    {
        GameObject creature = MakeCreature();
        GameObject collider = new GameObject("Collider");
        collider.transform.SetParent(creature.transform);
        collider.AddComponent<BoxCollider>();
        AssertIssue(creature, CreatureValidator.Severity.Warning, "\"Collider\"");
    }

    [Test]
    public void MissingEffectHitPointIsAWarning()
    {
        GameObject creature = MakeCreature();
        Object.DestroyImmediate(creature.transform.Find("EffectHitPoint").gameObject);
        AssertIssue(creature, CreatureValidator.Severity.Warning, "EffectHitPoint");
    }

    [Test]
    public void MissingSoundEventIsAWarning()
    {
        GameObject creature = MakeCreature();
        fmodEvents.Remove(SoundPrefix + "_pain");
        AssertIssue(creature, CreatureValidator.Severity.Warning, SoundPrefix + "_pain");
    }

    [Test]
    public void StandardMaterialIsAccepted()
    {
        GameObject creature = MakeCreature();
        AddRenderer(creature, "Standard");
        Assert.IsEmpty(Validate(creature), string.Join("\n", Validate(creature)));
    }

    [Test]
    public void ParticleStandardMaterialIsAWarning()
    {
        GameObject creature = MakeCreature();
        AddRenderer(creature, "Particles/Standard Unlit");
        AssertIssue(creature, CreatureValidator.Severity.Warning, "dark squares");
    }

    [Test]
    public void MountWithoutMountBoneMatchIsAnError()
    {
        GameObject mount = MakeMount();
        mount.GetComponent<EquipmentObject>().MountBone = "Saddle";
        AssertIssue(mount, CreatureValidator.Severity.Error, "\"Saddle\"");
    }

    [Test]
    public void RotatedMountBoneIsAWarning()
    {
        GameObject mount = MakeMount();
        mount.transform.Find("Model/Spine/MountPoint").localRotation = Quaternion.Euler(90f, 0f, 0f);
        AssertIssue(mount, CreatureValidator.Severity.Warning, "rotated");
    }

    [Test]
    public void MountWithoutMobileIsAnError()
    {
        GameObject mount = MakeMount();
        Object.DestroyImmediate(mount.GetComponent<Mobile>());
        AssertIssue(mount, CreatureValidator.Severity.Error, "no Mobile");
    }

    // ---- helpers ----

    List<CreatureValidator.Issue> Validate(GameObject prefab, int clientId = 1)
    {
        return CreatureValidator.ValidatePrefab(prefab, clientId, fmodEvents);
    }

    void AssertIssue(GameObject prefab, CreatureValidator.Severity severity, string messagePart, int clientId = 1)
    {
        List<CreatureValidator.Issue> issues = Validate(prefab, clientId);
        Assert.AreEqual(1, issues.Count, "Expected exactly one issue, got:\n" + string.Join("\n", issues));
        Assert.AreEqual(severity, issues[0].Severity, issues[0].ToString());
        StringAssert.Contains(messagePart, issues[0].Message);
    }

    T Track<T>(T item) where T : Object
    {
        created.Add(item);
        return item;
    }

    static AnimatorController GetController(GameObject creature)
    {
        return (AnimatorController)creature.GetComponentInChildren<Animator>().runtimeAnimatorController;
    }

    GameObject MakeCreature()
    {
        GameObject root = Track(new GameObject("TestCreature"));
        Mobile mobile = root.AddComponent<Mobile>();
        mobile.FMODAudioPath = SoundPrefix;

        GameObject model = new GameObject("Model");
        model.transform.SetParent(root.transform);
        Animator animator = model.AddComponent<Animator>();
        animator.applyRootMotion = false;
        animator.runtimeAnimatorController = MakeController();
        model.AddComponent<FootstepHandler>();

        AddChild(root, "OverheadAnchor");
        AddChild(root, "EffectHitPoint");
        GameObject picker = AddChild(root, "PickerBounds");
        picker.layer = 19;
        picker.AddComponent<BoxCollider>();
        return root;
    }

    GameObject MakeMount()
    {
        GameObject root = MakeCreature();
        GameObject spine = AddChild(root.transform.Find("Model").gameObject, "Spine");
        AddChild(spine, "MountPoint");
        AddChild(root, "MountedOverheadAnchor");
        EquipmentObject equipment = root.AddComponent<EquipmentObject>();
        equipment.EquipmentSlot = "Mount";
        equipment.MountBone = "MountPoint";
        return root;
    }

    void AddRenderer(GameObject creature, string shaderName)
    {
        Shader shader = Shader.Find(shaderName);
        Assert.IsNotNull(shader, "Shader " + shaderName + " not found in this project");
        GameObject mesh = AddChild(creature.transform.Find("Model").gameObject, "Body");
        mesh.AddComponent<MeshFilter>();
        mesh.AddComponent<MeshRenderer>().sharedMaterial = Track(new Material(shader) { name = "TestMaterial" });
    }

    static GameObject AddChild(GameObject parent, string name)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent.transform, false);
        return child;
    }

    AnimatorController MakeController()
    {
        AnimatorController controller = Track(new AnimatorController());
        controller.AddLayer("Base");
        foreach (string parameter in new[] { "Speed", "WalkCycle", "RunCycle" })
        {
            controller.AddParameter(parameter, AnimatorControllerParameterType.Float);
        }
        controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState idle = stateMachine.AddState("idle");
        stateMachine.AddState("walk");
        stateMachine.AddState("run");
        stateMachine.AddState("attack");
        AnimatorState die = stateMachine.AddState("die");
        die.AddTransition(idle).AddCondition(AnimatorConditionMode.IfNot, 0f, "IsDead");
        return controller;
    }
}
