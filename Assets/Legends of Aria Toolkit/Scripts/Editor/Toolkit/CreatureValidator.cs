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

// Checks creature and mount prefabs (prefabs with a Mobile on the root) against what the game
// client expects of them. Errors are things that break the creature in game; warnings are things
// that usually look wrong. Each message says what the client does and how to fix it.
public static class CreatureValidator
{
    public enum Severity
    {
        Error,
        Warning,
    }

    public class Issue
    {
        public Severity Severity;
        public GameObject Prefab;
        public int ClientId;
        public string Message;

        public override string ToString()
        {
            string idText = ClientId > 0 ? " (ClientId " + ClientId + ")" : "";
            return (Prefab != null ? Prefab.name : "?") + idText + ": " + Message;
        }
    }

    // The current client silently drops any object whose ClientId is above the length of its own
    // base object library, even when the object comes from a custom bundle.
    public const int MaxClientIdForCurrentClient = 2879;

    const int PickerColliderLayer = 19;
    const int PickerCollider2Layer = 23;

    static readonly string[] RequiredStates = { "idle", "walk", "run", "die", "attack" };
    static readonly string[] SoundSuffixes = { "_attack", "_pain", "_death", "_footsteps" };

    // Shaders known to render on creatures in the current client. Others may still work, but are
    // untested; render-pipeline and Shader Graph shaders do not exist in the client at all.
    static readonly string[] SafeShaderPrefixes =
    {
        "Standard",
        "Legacy Shaders/",
        "Custom/Diffuse-Hue",
        "Mobile/Diffuse",
        "Mobile/Bumped Diffuse",
        "Unlit/Texture",
        "Unlit/Transparent",
    };

    public static List<Issue> ValidateLibrary(ClientObjectLibrary library)
    {
        List<Issue> issues = new List<Issue>();
        if (library == null || library.ClientIdPrefabs == null)
        {
            return issues;
        }

        HashSet<string> fmodEvents = LoadFmodEventList();
        for (int i = 1; i < library.ClientIdPrefabs.Length; i++)
        {
            GameObject prefab = library.ClientIdPrefabs[i];
            if (prefab != null)
            {
                issues.AddRange(ValidatePrefab(prefab, i, fmodEvents));
            }
        }

        return issues;
    }

    // clientId 0 means "not in a library yet" and skips the ClientId check.
    public static List<Issue> ValidatePrefab(GameObject prefab, int clientId, HashSet<string> fmodEvents = null)
    {
        List<Issue> issues = new List<Issue>();
        Action<Severity, string> add = (severity, message) =>
            issues.Add(new Issue() { Severity = severity, Prefab = prefab, ClientId = clientId, Message = message });

        if (prefab.GetComponentInChildren<LocalPlayer>(true) != null)
        {
            add(Severity.Error, "Has the toolkit's LocalPlayer component. That class shares its name with a different class in the game client; remove it from library prefabs.");
        }

        Mobile mobile = prefab.GetComponent<Mobile>();
        EquipmentObject mountEquipment = prefab.GetComponents<EquipmentObject>().FirstOrDefault(item => item.EquipmentSlot == "Mount");
        if (mobile == null)
        {
            if (mountEquipment != null)
            {
                add(Severity.Error, "Has an EquipmentObject with EquipmentSlot \"Mount\" but no Mobile on the root. A mount must be a creature: add Mobile to the root.");
            }
            return issues;
        }

        if (clientId > MaxClientIdForCurrentClient)
        {
            add(Severity.Error, "ClientId " + clientId + " is above " + MaxClientIdForCurrentClient + ". The current game client silently ignores such objects; move the prefab to a lower slot in the library.");
        }

        Transform root = prefab.transform;
        Transform model = root.Find("Model");
        Animator animator = null;
        if (model == null)
        {
            add(Severity.Error, "No direct child named \"Model\". The client looks for the Animator under a child with exactly that name; without it the creature never animates.");
        }
        else
        {
            animator = model.GetComponent<Animator>();
            if (animator == null)
            {
                animator = model.GetComponentInChildren<Animator>(true);
            }

            if (animator == null)
            {
                add(Severity.Error, "No Animator on \"Model\" or its children. The creature will not animate.");
            }
            else if (animator.runtimeAnimatorController == null)
            {
                add(Severity.Error, "The Animator on \"" + animator.name + "\" has no controller. The client ignores animators without one, so the creature will not animate.");
                animator = null;
            }
        }

        CheckMobileSettings(mobile, add);
        CheckFootstepHandlers(prefab, root, animator, add);
        CheckAnchorsAndPicking(root, mountEquipment != null, add);

        if (animator != null)
        {
            CheckAnimator(mobile, animator, prefab.GetComponentInChildren<FootstepHandler>(true) != null, add);
        }

        if (mountEquipment != null)
        {
            CheckMount(root, mountEquipment, add);
        }

        CheckMaterials(prefab, add);
        CheckSounds(prefab, mobile, fmodEvents ?? LoadFmodEventList(), add);

        return issues;
    }

    static void CheckMobileSettings(Mobile mobile, Action<Severity, string> add)
    {
        if (mobile.HasCustomAnimations)
        {
            add(Severity.Error, "Mobile.HasCustomAnimations is on. It forwards animations to client scripts that a bundle cannot contain, and the client logs an error for every animation. Turn it off.");
        }

        if (Mathf.Approximately(mobile.WalkAnimSpeed, 1f) && Mathf.Approximately(mobile.RunAnimSpeed, 1f))
        {
            add(Severity.Warning, "WalkAnimSpeed and RunAnimSpeed are both 1. While walking or running, the client sets the animation speed to movement speed x factor, so a creature chasing at 4.5 units/s plays its run at 4.5x. Use about 1 / the ground speed each clip was made for (the client defaults are 0.66 walk, 0.2 run).");
        }
        else if (mobile.WalkAnimSpeed < mobile.RunAnimSpeed)
        {
            add(Severity.Warning, "WalkAnimSpeed (" + mobile.WalkAnimSpeed + ") is lower than RunAnimSpeed (" + mobile.RunAnimSpeed + "). Walking is slower than running, so its factor is normally the larger one; check whether the two are swapped.");
        }

        if (mobile.WalkAnimSpeed <= 0f || mobile.RunAnimSpeed <= 0f)
        {
            add(Severity.Warning, "WalkAnimSpeed or RunAnimSpeed is 0 or negative, which freezes or reverses the walk or run animation in game.");
        }

        if (mobile.transform.localScale != Vector3.one)
        {
            add(Severity.Warning, "The root's scale is " + mobile.transform.localScale + ". The server sets the root's scale and overwrites it; scale \"Model\" instead.");
        }
    }

    static void CheckFootstepHandlers(GameObject prefab, Transform root, Animator animator, Action<Severity, string> add)
    {
        foreach (FootstepHandler handler in prefab.GetComponentsInChildren<FootstepHandler>(true))
        {
            if (handler.GetComponent<Animator>() == null || handler.transform.parent != root)
            {
                add(Severity.Error, "FootstepHandler on \"" + handler.name + "\" must sit on the GameObject that holds the Animator, and that GameObject must be a direct child of the root (normally \"Model\"). Elsewhere it throws errors in game.");
            }
        }

        if (animator != null && animator.GetComponent<FootstepHandler>() == null)
        {
            bool hasFootstepEvents = animator.runtimeAnimatorController.animationClips
                .Where(clip => clip != null)
                .SelectMany(clip => AnimationUtility.GetAnimationEvents(clip))
                .Any(evt => evt.functionName == "PlayFootstep");
            if (hasFootstepEvents)
            {
                add(Severity.Error, "Animation clips have PlayFootstep events, but there is no FootstepHandler next to the Animator to receive them. Unity logs an error for each event in game.");
            }
        }
    }

    static void CheckAnchorsAndPicking(Transform root, bool isMount, Action<Severity, string> add)
    {
        if (root.Find("OverheadAnchor") == null && root.Find("Model/OverheadAnchor") == null)
        {
            add(Severity.Error, "No \"OverheadAnchor\" (a direct child of the root, or of \"Model\"). It sets the name plate height, and the current client throws an error without it.");
        }

        if (isMount && root.Find("MountedOverheadAnchor") == null)
        {
            add(Severity.Warning, "No \"MountedOverheadAnchor\" as a direct child of the root. It places the rider's name plate while riding.");
        }

        Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
        Collider[] pickers = colliders.Where(item => item.name == "PickerBounds").ToArray();
        if (pickers.Length == 0)
        {
            add(Severity.Warning, "No collider named \"PickerBounds\". Players will not be able to click or target the creature.");
        }

        foreach (Collider picker in pickers.Where(item => item.gameObject.layer != PickerColliderLayer))
        {
            add(Severity.Warning, "\"PickerBounds\" is on layer " + picker.gameObject.layer + ". The client only picks layer " + PickerColliderLayer + " (PickerCollider).");
        }

        foreach (Collider picker in colliders.Where(item => item.name == "PickerBounds2" && item.gameObject.layer != PickerCollider2Layer))
        {
            add(Severity.Warning, "\"PickerBounds2\" is on layer " + picker.gameObject.layer + ". The client only picks it on layer " + PickerCollider2Layer + " (PickerCollider2).");
        }

        if (colliders.Any(item => item.name == "Collider"))
        {
            add(Severity.Warning, "Has a collider named \"Collider\". The client ignores it on creatures (the server uses the template's BodyOffset for creature collision); remove it.");
        }

        bool hasHitPoint = false;
        foreach (Transform child in root)
        {
            if (child.name == "EffectHitPoint")
            {
                hasHitPoint = true;
            }
        }

        if (!hasHitPoint)
        {
            add(Severity.Warning, "No \"EffectHitPoint\" as a direct child of the root. Spells and hits then attach 1 m above the ground; add one at chest height.");
        }
    }

    static void CheckAnimator(Mobile mobile, Animator animator, bool hasFootstepHandler, Action<Severity, string> add)
    {
        if (animator.applyRootMotion)
        {
            add(Severity.Warning, "Apply Root Motion is on. The client moves creatures itself; clips with root motion would drift the model away from its position. Turn it off unless every clip moves in place.");
        }

        AnimatorController controller = GetController(animator.runtimeAnimatorController);
        if (controller == null)
        {
            return;
        }

        if (controller.layers.Length == 0)
        {
            add(Severity.Error, "The animator controller has no layers.");
            return;
        }

        if (controller.layers.Length > 1)
        {
            add(Severity.Warning, "The animator controller has " + controller.layers.Length + " layers. The client sets the weights of layers 1 and 2 itself (meant for the player's upper-body layers); keep creatures to one layer.");
        }

        AnimatorControllerLayer baseLayer = controller.layers[0];
        if (baseLayer.name != "Base")
        {
            add(Severity.Warning, "The first layer is named \"" + baseLayer.name + "\". Rename it to \"Base\": the client only scales walk and run animation speed in the states Base.walk, Base.run and Base.Movement.");
        }

        HashSet<string> topLevel = new HashSet<string>(baseLayer.stateMachine.states.Select(item => item.state.name));
        if (!topLevel.Contains("Movement") && (!topLevel.Contains("walk") || !topLevel.Contains("run")))
        {
            add(Severity.Warning, "\"walk\" and \"run\" are not states directly in the first layer (not in a sub-state machine). The client's walk and run animation speed scaling will not apply.");
        }

        List<AnimatorState> allStates = controller.layers.SelectMany(layer => GetAllStates(layer.stateMachine)).ToList();
        HashSet<string> stateNames = new HashSet<string>(allStates.Select(item => item.name));

        List<string> missing = new List<string>();
        foreach (string name in RequiredStates)
        {
            // a "Movement" blend tree can stand in for separate walk and run states
            bool coveredByMovement = (name == "walk" || name == "run") && stateNames.Contains("Movement");
            if (!stateNames.Contains(name) && !coveredByMovement)
            {
                missing.Add(name);
            }
        }

        if (missing.Count > 0)
        {
            add(Severity.Warning, "Missing animator states: " + string.Join(", ", missing) + ". The client and server play these by name; a missing state is skipped silently.");
        }

        HashSet<string> parameters = new HashSet<string>(controller.parameters.Select(item => item.name));
        if (hasFootstepHandler && (!parameters.Contains("WalkCycle") || !parameters.Contains("RunCycle")))
        {
            add(Severity.Warning, "FootstepHandler is present but the controller lacks the float parameters WalkCycle and RunCycle. The handler reads both every frame and Unity warns every frame when they are missing; add them even if only animation events are used.");
        }

        if (!parameters.Contains("Speed"))
        {
            add(Severity.Warning, "The controller has no \"Speed\" float parameter. The client drives walk and run through it.");
        }

        if (!parameters.Contains("IsDead"))
        {
            add(Severity.Warning, "The controller has no \"IsDead\" bool parameter. The client sets it on death and resurrection.");
        }

        AnimatorState die = allStates.FirstOrDefault(item => item.name == "die");
        AnimatorState diePose = allStates.FirstOrDefault(item => item.name == "die_pose");
        if (die != null)
        {
            AnimationClip dieClip = die.motion as AnimationClip;
            if (dieClip != null && dieClip.isLooping)
            {
                add(Severity.Warning, "The \"die\" clip loops. The client plays \"die\" on death and again when an already-dead creature comes into view; it should end lying down and stay there.");
            }

            bool canRevive = new[] { die, diePose }.Where(item => item != null)
                .SelectMany(item => item.transitions)
                .Any(transition => transition.conditions.Any(condition => condition.parameter == "IsDead" && condition.mode == AnimatorConditionMode.IfNot));
            if (!canRevive)
            {
                add(Severity.Warning, "No transition out of \"die\" (or \"die_pose\") on IsDead = false. On resurrection the client only clears IsDead, so the creature would stay lying down.");
            }
        }

        int attackVariants = allStates.Count(item => item.name.StartsWith("attack") && item.name.Length > 6 && char.IsDigit(item.name[6]));
        AnimatorState attack = allStates.FirstOrDefault(item => item.name == "attack");
        if (attack != null && attackVariants > 0)
        {
            RandomAnimIndexBehaviour picker = attack.behaviours.OfType<RandomAnimIndexBehaviour>().FirstOrDefault();
            if (picker == null)
            {
                add(Severity.Warning, "There are " + attackVariants + " attackN states, but \"attack\" has no RandomAnimIndexBehaviour. The server only plays \"attack\"; without the behaviour AnimIndex stays 0 and the creature always uses the first variant.");
            }
            else if (picker.numAnims != attackVariants)
            {
                add(Severity.Warning, "RandomAnimIndexBehaviour.numAnims is " + picker.numAnims + " but there are " + attackVariants + " attackN states.");
            }
        }
    }

    static void CheckMount(Transform root, EquipmentObject mountEquipment, Action<Severity, string> add)
    {
        if (string.IsNullOrEmpty(mountEquipment.MountBone))
        {
            add(Severity.Error, "The mount's EquipmentObject has no MountBone. Set it to the name of the transform the rider's feet are placed on (for example \"MountPoint\").");
            return;
        }

        // The client searches the mount's hierarchy for the first transform whose name contains
        // MountBone. The root is renamed at runtime, so only its children are searched here.
        Transform mountBone = null;
        foreach (Transform child in root)
        {
            mountBone = FindByNameContains(child, mountEquipment.MountBone);
            if (mountBone != null)
            {
                break;
            }
        }

        if (mountBone == null)
        {
            add(Severity.Error, "No transform whose name contains \"" + mountEquipment.MountBone + "\" (EquipmentObject.MountBone). The rider cannot be seated.");
            return;
        }

        float upError = Vector3.Angle(mountBone.up, root.up);
        float forwardError = Vector3.Angle(mountBone.forward, root.forward);
        if (upError > 20f || forwardError > 20f)
        {
            add(Severity.Warning, "\"" + mountBone.name + "\" is rotated " + Mathf.RoundToInt(Mathf.Max(upError, forwardError)) + " degrees away from the creature's up and forward. The rider copies its rotation exactly; rotate it so Y points up and Z towards the head.");
        }
    }

    static void CheckMaterials(GameObject prefab, Action<Severity, string> add)
    {
        HashSet<string> reported = new HashSet<string>();
        foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
        {
            Material[] materials = renderer.sharedMaterials;
            for (int slot = 0; slot < materials.Length; slot++)
            {
                Material material = materials[slot];
                if (material == null || material.shader == null)
                {
                    continue;
                }

                string shaderName = material.shader.name;
                string key = shaderName + "|" + slot;
                if (!reported.Add(key))
                {
                    continue;
                }

                if (shaderName.StartsWith("Universal Render Pipeline") || shaderName.StartsWith("HDRP") || shaderName.StartsWith("Shader Graphs"))
                {
                    add(Severity.Error, "Material \"" + material.name + "\" uses \"" + shaderName + "\". The game does not use a render pipeline, so this renders wrongly or not at all; switch to Standard.");
                }
                else if (shaderName.StartsWith("Particles/Standard"))
                {
                    add(Severity.Warning, "Material \"" + material.name + "\" uses \"" + shaderName + "\", which has rendered as dark squares in game. Use Legacy Shaders/Particles/Additive or Alpha Blended.");
                }
                else if (!SafeShaderPrefixes.Any(prefix => shaderName.StartsWith(prefix)))
                {
                    add(Severity.Warning, "Material \"" + material.name + "\" uses \"" + shaderName + "\", which is not known to work on creatures in the current client. Test it in game, or use Standard.");
                }

                if (slot > 0 && !shaderName.StartsWith("Legacy Shaders/") && !(renderer is ParticleSystemRenderer))
                {
                    add(Severity.Warning, "\"" + renderer.name + "\" uses \"" + shaderName + "\" in material slot " + slot + ". The current client only re-links the shader in slot 0 of bundle materials; test it in game or split the mesh.");
                }
            }
        }
    }

    static void CheckSounds(GameObject prefab, Mobile mobile, HashSet<string> fmodEvents, Action<Severity, string> add)
    {
        if (fmodEvents == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(mobile.FMODAudioPath))
        {
            add(Severity.Warning, "Mobile.FMODAudioPath is empty, so the creature makes no attack, pain, death or footstep sounds. Pick a sound set from the game, for example event:/animals/wolf/wolf.");
        }
        else if (mobile.FMODAudioPath.StartsWith("event:/"))
        {
            string[] missing = SoundSuffixes
                .Where(suffix => !(suffix == "_footsteps" && !string.IsNullOrEmpty(mobile.OptionalFMODFootstepPath)))
                .Where(suffix => !fmodEvents.Contains(mobile.FMODAudioPath + suffix))
                .ToArray();
            if (missing.Length > 0)
            {
                add(Severity.Warning, "These sounds do not exist in the game: " + string.Join(", ", missing.Select(suffix => mobile.FMODAudioPath + suffix)) + ". Those events stay silent.");
            }
        }

        if (!string.IsNullOrEmpty(mobile.OptionalFMODFootstepPath) && !fmodEvents.Contains(mobile.OptionalFMODFootstepPath))
        {
            add(Severity.Warning, "OptionalFMODFootstepPath \"" + mobile.OptionalFMODFootstepPath + "\" does not exist in the game.");
        }

        foreach (FMODPlayEnvironmentSound ambient in prefab.GetComponentsInChildren<FMODPlayEnvironmentSound>(true))
        {
            if (!string.IsNullOrEmpty(ambient.FMODPath) && !fmodEvents.Contains(ambient.FMODPath))
            {
                add(Severity.Warning, "FMODPlayEnvironmentSound path \"" + ambient.FMODPath + "\" does not exist in the game.");
            }
        }
    }

    // Reads the game's list of FMOD events (FMODAudioReference.txt in the base game folder).
    // Returns null when the file cannot be found, which skips the sound checks.
    public static HashSet<string> LoadFmodEventList()
    {
        try
        {
            string basePath = ToolUtil.Settings != null ? ToolUtil.Settings.BasePath : null;
            if (string.IsNullOrEmpty(basePath))
            {
                return null;
            }

            string path = Path.Combine(basePath, "FMODAudioReference.txt");
            if (!File.Exists(path))
            {
                return null;
            }

            // lines look like "animals : event:/animals/wolf/wolf_attack"
            HashSet<string> events = new HashSet<string>();
            foreach (string line in File.ReadAllLines(path))
            {
                int index = line.IndexOf("event:/", StringComparison.Ordinal);
                if (index >= 0)
                {
                    events.Add(line.Substring(index).Trim());
                }
            }

            return events;
        }
        catch (Exception)
        {
            return null;
        }
    }

    static AnimatorController GetController(RuntimeAnimatorController runtimeController)
    {
        AnimatorOverrideController overrideController = runtimeController as AnimatorOverrideController;
        if (overrideController != null)
        {
            return overrideController.runtimeAnimatorController as AnimatorController;
        }

        return runtimeController as AnimatorController;
    }

    static IEnumerable<AnimatorState> GetAllStates(AnimatorStateMachine stateMachine)
    {
        foreach (ChildAnimatorState child in stateMachine.states)
        {
            yield return child.state;
        }

        foreach (ChildAnimatorStateMachine child in stateMachine.stateMachines)
        {
            foreach (AnimatorState state in GetAllStates(child.stateMachine))
            {
                yield return state;
            }
        }
    }

    static Transform FindByNameContains(Transform current, string name)
    {
        if (current.name.Contains(name))
        {
            return current;
        }

        for (int i = 0; i < current.childCount; i++)
        {
            Transform found = FindByNameContains(current.GetChild(i), name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
