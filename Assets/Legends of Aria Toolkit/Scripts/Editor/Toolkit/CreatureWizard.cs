// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Emergent Worlds
// See LICENSE in the repository root.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Turns a rigged model and its animation clips into a creature or mount prefab that matches what
// the game client expects: Mobile on the root, the model as "Model" with a generated animator
// controller, name plate and effect anchors, a picking volume, and for mounts the rider point.
// The result is checked with CreatureValidator.
public class CreatureWizard : EditorWindow
{
    // Clips and settings the prefab and controller are built from.
    public class CreatureSettings
    {
        public string Name = "";
        public AnimationClip Idle;
        public AnimationClip CombatIdle;
        public AnimationClip Walk;
        public AnimationClip Run;
        public AnimationClip Die;
        public AnimationClip WasHit;
        public AnimationClip Roar;
        public List<AnimationClip> Attacks = new List<AnimationClip>();
        public float WalkSpeed = 1.5f;
        public float RunSpeed = 4.5f;
        public string SoundPrefix = "";
        public string AmbientSound = "";
        public bool IsMount;
        public string MountBonePath = "";
        public float RiderHipHeight = 1.0f;
    }

    // Walk/run switch in the generated controller. The client also switches between the walk and
    // run animation speed factors at 2 units/s, so the two agree.
    public const float RunThreshold = 2f;
    const float MovingThreshold = 0.01f;
    const int PickerColliderLayer = 19;

    GameObject model;
    GameObject targetLibrary;
    CreatureSettings settings = new CreatureSettings();
    string[] bonePaths = new string[0];
    int mountBoneIndex;
    string[] soundPrefixes = new string[0];
    int soundIndex;
    Vector2 scroll;

    [MenuItem("LoA Toolkit/Custom Assets/Create Creature", false, 7)]
    public static void ShowWindow()
    {
        CreatureWizard window = GetWindow<CreatureWizard>();
        window.titleContent = new GUIContent("Create Creature");
        window.LoadSoundPrefixes();
    }

    void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.HelpBox(
            "Builds a creature or mount prefab from a rigged model: Mobile on the root, the model as \"Model\" with a generated animator controller, "
            + "name plate and effect anchors, a picking volume, and for a mount the rider point. Select a folder in the Project window first; "
            + "the prefab and controller are created in a new subfolder there.",
            MessageType.Info);

        GameObject pickedModel = (GameObject)EditorGUILayout.ObjectField("Model", model, typeof(GameObject), false);
        if (pickedModel != model)
        {
            model = pickedModel;
            OnModelPicked();
        }

        if (model == null)
        {
            EditorGUILayout.EndScrollView();
            return;
        }

        settings.Name = EditorGUILayout.TextField("Name", settings.Name);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Animations", EditorStyles.boldLabel);
        if (GUILayout.Button("Guess Clips From Names"))
        {
            GuessClips();
        }
        settings.Idle = ClipField("Idle", settings.Idle);
        settings.Walk = ClipField("Walk", settings.Walk);
        settings.Run = ClipField("Run", settings.Run);
        settings.Die = ClipField("Die (ends lying down)", settings.Die);
        settings.WasHit = ClipField("Was Hit", settings.WasHit);
        settings.CombatIdle = ClipField("Combat Idle (optional)", settings.CombatIdle);
        settings.Roar = ClipField("Roar (optional)", settings.Roar);
        for (int i = 0; i < settings.Attacks.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            settings.Attacks[i] = ClipField("Attack " + (i + 1), settings.Attacks[i]);
            if (GUILayout.Button("-", GUILayout.Width(22)))
            {
                settings.Attacks.RemoveAt(i);
                EditorGUILayout.EndHorizontal();
                break;
            }
            EditorGUILayout.EndHorizontal();
        }
        if (GUILayout.Button("Add Attack"))
        {
            settings.Attacks.Add(null);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Movement", EditorStyles.boldLabel);
        settings.WalkSpeed = EditorGUILayout.FloatField(new GUIContent("Walk Clip Speed", "Ground speed in units (metres) per second at which the walk clip looks right."), settings.WalkSpeed);
        settings.RunSpeed = EditorGUILayout.FloatField(new GUIContent("Run Clip Speed", "Ground speed in units (metres) per second at which the run clip looks right."), settings.RunSpeed);
        EditorGUILayout.LabelField(" ", "Animation speed factors: walk " + Factor(settings.WalkSpeed) + ", run " + Factor(settings.RunSpeed));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Sound", EditorStyles.boldLabel);
        if (soundPrefixes.Length > 1)
        {
            soundIndex = EditorGUILayout.Popup("Sound Set", soundIndex, soundPrefixes);
        }
        else
        {
            EditorGUILayout.HelpBox("FMODAudioReference.txt was not found in the base game folder, so no sound sets can be listed.", MessageType.None);
        }

        EditorGUILayout.Space();
        settings.IsMount = EditorGUILayout.Toggle("Is Mount", settings.IsMount);
        if (settings.IsMount)
        {
            mountBoneIndex = EditorGUILayout.Popup(new GUIContent("Rider Bone", "The spine bone the rider sits on. The rider point is created under it so the rider moves with the animation."), mountBoneIndex, bonePaths.Select(item => new GUIContent(item)).ToArray());
            settings.RiderHipHeight = EditorGUILayout.FloatField(new GUIContent("Rider Hip Height", "How far below the top of the back the rider's feet point goes. About 1 m for the player's riding pose."), settings.RiderHipHeight);
        }

        EditorGUILayout.Space();
        targetLibrary = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Add To Library", "Optional. The client object library to register the creature in."), targetLibrary, typeof(GameObject), false);

        EditorGUILayout.Space();
        string problem = GetInputProblem();
        if (problem != null)
        {
            EditorGUILayout.HelpBox(problem, MessageType.Warning);
        }
        GUI.enabled = problem == null;
        if (GUILayout.Button("Create Creature"))
        {
            CreateCreature();
        }
        GUI.enabled = true;
        EditorGUILayout.EndScrollView();
    }

    static AnimationClip ClipField(string label, AnimationClip clip)
    {
        return (AnimationClip)EditorGUILayout.ObjectField(label, clip, typeof(AnimationClip), false);
    }

    static string Factor(float speed)
    {
        return speed > 0f ? (1f / speed).ToString("0.##") : "?";
    }

    string GetInputProblem()
    {
        if (string.IsNullOrEmpty(settings.Name.Trim())) return "Enter a name.";
        if (settings.Idle == null || settings.Walk == null || settings.Run == null || settings.Die == null) return "Idle, Walk, Run and Die clips are required.";
        if (settings.Attacks.Count == 0 || settings.Attacks.Any(item => item == null)) return "Add at least one attack clip, and fill or remove empty attack slots.";
        if (settings.WalkSpeed <= 0f || settings.RunSpeed <= 0f) return "Walk and run clip speeds must be above 0.";
        if (settings.IsMount && bonePaths.Length == 0) return "The model has no bones to put the rider point on.";
        return null;
    }

    void OnModelPicked()
    {
        settings = new CreatureSettings();
        if (model == null)
        {
            return;
        }

        settings.Name = model.name;
        bonePaths = GetBonePaths(model.transform);
        mountBoneIndex = Mathf.Max(0, Array.FindIndex(bonePaths, item => item.ToLowerInvariant().EndsWith("spine") || item.ToLowerInvariant().EndsWith("spine1")));
        GuessClips();
        LoadSoundPrefixes();
    }

    // Fills empty clip slots from clips in the model's file and folder whose names match.
    void GuessClips()
    {
        string modelPath = AssetDatabase.GetAssetPath(model);
        List<AnimationClip> clips = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>()
            .Where(item => !item.name.StartsWith("__preview__")).ToList();

        string folder = Path.GetDirectoryName(modelPath);
        if (!string.IsNullOrEmpty(folder))
        {
            foreach (string guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { folder.Replace("\\", "/") }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                clips.AddRange(AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(item => !item.name.StartsWith("__preview__")));
            }
        }
        clips = clips.Distinct().ToList();

        Func<string[], string[], AnimationClip> find = (include, exclude) => clips.FirstOrDefault(clip =>
        {
            string name = clip.name.ToLowerInvariant();
            return include.Any(part => name.Contains(part)) && !exclude.Any(part => name.Contains(part));
        });

        if (settings.Idle == null) settings.Idle = find(new[] { "idle" }, new[] { "combat", "fight", "battle" });
        if (settings.CombatIdle == null) settings.CombatIdle = find(new[] { "combat_idle", "combatidle", "fight_idle", "battle_idle", "idle_combat" }, new string[0]);
        if (settings.Walk == null) settings.Walk = find(new[] { "walk" }, new[] { "back", "left", "right" });
        if (settings.Run == null) settings.Run = find(new[] { "run", "gallop", "sprint" }, new[] { "attack", "back", "left", "right" });
        if (settings.Die == null) settings.Die = find(new[] { "die", "death", "dead" }, new string[0]);
        if (settings.WasHit == null) settings.WasHit = find(new[] { "hit", "damage", "hurt" }, new[] { "attack" });
        if (settings.Roar == null) settings.Roar = find(new[] { "roar", "howl", "shout" }, new string[0]);
        if (settings.Attacks.Count == 0)
        {
            settings.Attacks = clips.Where(clip => clip.name.ToLowerInvariant().Contains("attack") || clip.name.ToLowerInvariant().Contains("bite"))
                .OrderBy(clip => clip.name).Take(4).ToList();
        }

        if (settings.Walk != null && ClipGroundSpeed(settings.Walk) > 0.05f) settings.WalkSpeed = ClipGroundSpeed(settings.Walk);
        if (settings.Run != null && ClipGroundSpeed(settings.Run) > 0.05f) settings.RunSpeed = ClipGroundSpeed(settings.Run);
    }

    static float ClipGroundSpeed(AnimationClip clip)
    {
        Vector3 speed = clip.averageSpeed;
        return new Vector2(speed.x, speed.z).magnitude;
    }

    void LoadSoundPrefixes()
    {
        HashSet<string> events = CreatureValidator.LoadFmodEventList();
        List<string> prefixes = new List<string>() { "(none)" };
        if (events != null)
        {
            prefixes.AddRange(events.Where(item => item.EndsWith("_attack"))
                .Select(item => item.Substring(0, item.Length - "_attack".Length))
                .Where(prefix => events.Contains(prefix + "_death"))
                .OrderBy(item => item));
        }
        soundPrefixes = prefixes.ToArray();
        soundIndex = Mathf.Max(0, Array.FindIndex(soundPrefixes, item => item.EndsWith("/wolf/wolf")));
    }

    static string[] GetBonePaths(Transform root)
    {
        List<string> paths = new List<string>();
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child == root || child.GetComponent<Renderer>() != null)
            {
                continue;
            }
            paths.Add(AnimationUtility.CalculateTransformPath(child, root));
        }
        return paths.ToArray();
    }

    void CreateCreature()
    {
        settings.SoundPrefix = soundIndex > 0 && soundIndex < soundPrefixes.Length ? soundPrefixes[soundIndex] : "";
        HashSet<string> events = CreatureValidator.LoadFmodEventList();
        settings.AmbientSound = !string.IsNullOrEmpty(settings.SoundPrefix) && events != null && events.Contains(settings.SoundPrefix + "_environment")
            ? settings.SoundPrefix + "_environment" : "";
        settings.MountBonePath = settings.IsMount && bonePaths.Length > 0 ? bonePaths[mountBoneIndex] : "";

        string name = settings.Name.Trim();
        string parentFolder = GetTargetFolder();
        string folder = AssetDatabase.GenerateUniqueAssetPath(parentFolder + "/" + name);
        if (!EditorUtility.DisplayDialog("Create Creature", "Create \"" + name + "\" in " + folder + "?", "Create", "Cancel"))
        {
            return;
        }

        AssetDatabase.CreateFolder(parentFolder, Path.GetFileName(folder));
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(folder + "/" + name + "_controller.controller");
        BuildController(controller, settings);

        GameObject modelInstance = PrefabUtility.InstantiatePrefab(model) as GameObject;
        if (modelInstance == null)
        {
            modelInstance = Instantiate(model);
        }
        if (PrefabUtility.GetPrefabInstanceStatus(modelInstance) != PrefabInstanceStatus.NotAPrefab)
        {
            PrefabUtility.UnpackPrefabInstance(modelInstance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }

        GameObject root = BuildCreature(modelInstance, controller, settings);
        string prefabPath = folder + "/" + name + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        DestroyImmediate(root);

        int slot = 0;
        if (prefab != null && targetLibrary != null && targetLibrary.GetComponent<ClientObjectLibrary>() != null)
        {
            slot = AddToLibrary(targetLibrary, prefab);
        }

        AssetDatabase.SaveAssets();
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);

        List<CreatureValidator.Issue> issues = prefab != null ? CreatureValidator.ValidatePrefab(prefab, slot) : new List<CreatureValidator.Issue>();
        string result = "Created " + prefabPath + (slot > 0 ? "\nAdded to \"" + targetLibrary.name + "\" at ClientId " + slot + "." : "") + "\n\n"
            + "Footsteps: add WalkCycle / RunCycle curves to the walk and run clips (see the docs). "
            + (settings.IsMount ? "Ride it and move \"MountPoint\" to fine-tune the seat. " : "")
            + "Next: LoA Toolkit/Custom Assets/Create Creature Templates.";
        if (issues.Count > 0)
        {
            result += "\n\nChecks:\n- " + string.Join("\n- ", issues.Select(item => item.Message).ToArray());
        }
        EditorUtility.DisplayDialog("Create Creature", result, "OK");
    }

    // ---- building blocks (public for tests) ----

    // Fills a controller laid out the way the client expects: one layer named "Base", movement
    // states at top level driven by Speed, death that holds its pose and returns on IsDead = false,
    // and "attack" picking a random variant through RandomAnimIndexBehaviour and AnimIndex.
    public static void BuildController(AnimatorController controller, CreatureSettings settings)
    {
        if (controller.layers.Length == 0)
        {
            controller.AddLayer("Base");
        }
        AnimatorControllerLayer[] layers = controller.layers;
        layers[0].name = "Base";
        controller.layers = layers;

        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsDead", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Combat", AnimatorControllerParameterType.Bool);
        controller.AddParameter("AnimIndex", AnimatorControllerParameterType.Int);
        // read by FootstepHandler; driven by curves on the walk and run clips
        controller.AddParameter("WalkCycle", AnimatorControllerParameterType.Float);
        controller.AddParameter("RunCycle", AnimatorControllerParameterType.Float);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState idle = AddState(machine, "idle", settings.Idle, new Vector3(300, 0));
        machine.defaultState = idle;
        AnimatorState walk = AddState(machine, "walk", settings.Walk, new Vector3(300, 120));
        AnimatorState run = AddState(machine, "run", settings.Run, new Vector3(300, 240));

        Transition(idle, walk).AddCondition(AnimatorConditionMode.Greater, MovingThreshold, "Speed");
        Transition(walk, idle).AddCondition(AnimatorConditionMode.Less, MovingThreshold, "Speed");
        Transition(walk, run).AddCondition(AnimatorConditionMode.Greater, RunThreshold, "Speed");
        Transition(run, walk).AddCondition(AnimatorConditionMode.Less, RunThreshold, "Speed");
        Transition(run, idle).AddCondition(AnimatorConditionMode.Less, MovingThreshold, "Speed");

        if (settings.CombatIdle != null)
        {
            AnimatorState combatIdle = AddState(machine, "combat_idle", settings.CombatIdle, new Vector3(0, 0));
            Transition(idle, combatIdle).AddCondition(AnimatorConditionMode.If, 0, "Combat");
            Transition(combatIdle, idle).AddCondition(AnimatorConditionMode.IfNot, 0, "Combat");
            Transition(combatIdle, walk).AddCondition(AnimatorConditionMode.Greater, MovingThreshold, "Speed");
        }

        // "attack" is what the server plays. With several variants it hands over at once to
        // attack1..N on the random AnimIndex; with one it simply plays that clip.
        List<AnimationClip> attacks = settings.Attacks.Where(item => item != null).ToList();
        AnimatorState attack = AddState(machine, "attack", attacks.Count > 0 ? attacks[0] : null, new Vector3(600, 0));
        if (attacks.Count > 1)
        {
            attack.AddStateMachineBehaviour<RandomAnimIndexBehaviour>().numAnims = attacks.Count;
            for (int i = 0; i < attacks.Count; i++)
            {
                AnimatorState variant = AddState(machine, "attack" + (i + 1), attacks[i], new Vector3(900, i * 70));
                AnimatorStateTransition toVariant = attack.AddTransition(variant);
                toVariant.hasExitTime = false;
                toVariant.duration = 0f;
                toVariant.AddCondition(AnimatorConditionMode.Equals, i, "AnimIndex");
                ReturnToIdle(variant, idle);
            }
        }
        else
        {
            ReturnToIdle(attack, idle);
        }

        if (settings.WasHit != null)
        {
            ReturnToIdle(AddState(machine, "was_hit", settings.WasHit, new Vector3(600, 120)), idle);
        }
        if (settings.Roar != null)
        {
            ReturnToIdle(AddState(machine, "roar", settings.Roar, new Vector3(600, 240)), idle);
        }

        // The client plays "die" itself, so no transition leads into it. die_pose holds the last
        // frame for corpses that come into view already dead.
        AnimatorState die = AddState(machine, "die", settings.Die, new Vector3(0, 240));
        AnimatorState diePose = AddState(machine, "die_pose", settings.Die, new Vector3(0, 360));
        diePose.speed = 0f;
        diePose.cycleOffset = 0.99f;
        Transition(die, idle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
        Transition(diePose, idle).AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
    }

    // Builds the creature hierarchy around a model instance (which becomes "Model"). Returns the
    // root; the caller saves it as a prefab.
    public static GameObject BuildCreature(GameObject modelInstance, RuntimeAnimatorController controller, CreatureSettings settings)
    {
        GameObject root = new GameObject(settings.Name);
        Mobile mobile = root.AddComponent<Mobile>();
        mobile.WalkAnimSpeed = Mathf.Round(100f / settings.WalkSpeed) / 100f;
        mobile.RunAnimSpeed = Mathf.Round(100f / settings.RunSpeed) / 100f;
        mobile.FMODAudioPath = settings.SoundPrefix ?? "";
        mobile.clothSimColliders = new CapsuleCollider[0];

        modelInstance.name = "Model";
        modelInstance.transform.SetParent(root.transform, false);
        modelInstance.transform.localPosition = Vector3.zero;
        modelInstance.transform.localRotation = Quaternion.identity;

        Animator animator = modelInstance.GetComponent<Animator>();
        if (animator == null)
        {
            animator = modelInstance.AddComponent<Animator>();
        }
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
        modelInstance.AddComponent<FootstepHandler>();

        Bounds bounds = GetBounds(root);

        AddChild(root.transform, "OverheadAnchor", new Vector3(0, bounds.max.y + 0.2f, 0));
        AddChild(root.transform, "EffectHitPoint", new Vector3(0, bounds.min.y + bounds.size.y * 0.6f, bounds.center.z));

        // a capsule around the body, along the creature's longest horizontal-or-vertical axis
        GameObject picker = AddChild(root.transform, "PickerBounds", Vector3.zero);
        picker.layer = PickerColliderLayer;
        CapsuleCollider capsule = picker.AddComponent<CapsuleCollider>();
        capsule.center = bounds.center;
        capsule.direction = bounds.size.z > bounds.size.y ? 2 : 1;
        capsule.height = Mathf.Max(bounds.size.y, bounds.size.z);
        capsule.radius = Mathf.Max(0.1f, Mathf.Min(bounds.size.x, capsule.direction == 2 ? bounds.size.y : bounds.size.z) * 0.5f);

        Transform headBone = modelInstance.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(item => item.GetComponent<Renderer>() == null && IsHeadBone(item.name));
        if (headBone != null)
        {
            AddChild(headBone, "Head", Vector3.zero);
        }

        if (!string.IsNullOrEmpty(settings.AmbientSound))
        {
            root.AddComponent<FMODPlayEnvironmentSound>().FMODPath = settings.AmbientSound;
        }

        if (settings.IsMount)
        {
            Transform spine = string.IsNullOrEmpty(settings.MountBonePath) ? null : modelInstance.transform.Find(settings.MountBonePath);
            if (spine == null)
            {
                spine = modelInstance.transform;
            }

            // The rider's feet go on this point with its rotation, so keep it world-aligned
            // (Y up, Z forward) and about one hip height below the top of the back.
            float backTop = spine.position.y + 0.2f;
            GameObject mountPoint = new GameObject("MountPoint");
            mountPoint.transform.SetParent(spine, true);
            mountPoint.transform.position = new Vector3(spine.position.x, backTop - settings.RiderHipHeight, spine.position.z);
            mountPoint.transform.rotation = root.transform.rotation;

            EquipmentObject equipment = root.AddComponent<EquipmentObject>();
            equipment.EquipmentSlot = "Mount";
            equipment.MountBone = "MountPoint";
            equipment.EquipmentMeshPrefabs = new EquipmentObject.ClientIdMeshPrefabPair[0];

            AddChild(root.transform, "MountedOverheadAnchor", new Vector3(0, backTop + 1.9f, 0));
        }

        return root;
    }

    static bool IsHeadBone(string name)
    {
        string lower = name.ToLowerInvariant();
        return lower.Contains("head") && !lower.Contains("end") && !lower.Contains("nub") && !lower.Contains("mesh");
    }

    static AnimatorState AddState(AnimatorStateMachine machine, string name, Motion motion, Vector3 position)
    {
        AnimatorState state = machine.AddState(name, position);
        state.motion = motion;
        return state;
    }

    static AnimatorStateTransition Transition(AnimatorState from, AnimatorState to)
    {
        AnimatorStateTransition transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.15f;
        return transition;
    }

    static void ReturnToIdle(AnimatorState state, AnimatorState idle)
    {
        AnimatorStateTransition transition = state.AddTransition(idle);
        transition.hasExitTime = true;
        transition.exitTime = 0.9f;
        transition.duration = 0.1f;
    }

    static GameObject AddChild(Transform parent, string name, Vector3 localPosition)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = localPosition;
        return child;
    }

    static Bounds GetBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return new Bounds(new Vector3(0, 1, 0), new Vector3(1, 2, 1));
        }

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers.Skip(1))
        {
            bounds.Encapsulate(renderer.bounds);
        }
        // root sits at the origin with no rotation, so world bounds are the local bounds
        return bounds;
    }

    static int AddToLibrary(GameObject libraryObject, GameObject prefab)
    {
        string libraryPath = AssetDatabase.GetAssetPath(libraryObject);
        GameObject libraryRoot = PrefabUtility.LoadPrefabContents(libraryPath);
        try
        {
            ClientObjectLibrary library = libraryRoot.GetComponent<ClientObjectLibrary>();
            GameObject[] prefabs = library.ClientIdPrefabs;
            if (prefabs == null || prefabs.Length == 0)
            {
                prefabs = new GameObject[1];
            }

            int slot = Array.FindIndex(prefabs, 1, item => item == null);
            if (slot < 0)
            {
                Array.Resize(ref prefabs, prefabs.Length + 1);
                slot = prefabs.Length - 1;
            }

            prefabs[0] = null;
            prefabs[slot] = prefab;
            library.ClientIdPrefabs = prefabs;
            PrefabUtility.SaveAsPrefabAsset(libraryRoot, libraryPath);
            return slot;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(libraryRoot);
        }
    }

    static string GetTargetFolder()
    {
        string selectionPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (string.IsNullOrEmpty(selectionPath))
        {
            return "Assets";
        }
        if (!AssetDatabase.IsValidFolder(selectionPath))
        {
            selectionPath = Path.GetDirectoryName(selectionPath).Replace("\\", "/");
        }
        return string.IsNullOrEmpty(selectionPath) ? "Assets" : selectionPath;
    }
}
