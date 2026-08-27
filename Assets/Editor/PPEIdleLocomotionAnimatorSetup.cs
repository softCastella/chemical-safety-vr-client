using System;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

/// <summary>
/// Wires Generic idle/walk animation onto PPE_D_Player_Idle in the locomotion
/// trial copy only. Does not modify 3_PPE_Room_Train_Test_mask.unity or invent
/// scene FileIDs. Idle Transform and worn PPE parents are left as authored.
/// </summary>
public static class PPEIdleLocomotionAnimatorSetup
{
    const string ScenePath = "Assets/Scenes/3_PPE_Room_Train_Test_mask_locomotion.unity";
    const string IdleName = "PPE_D_Player_Idle";
    const string OriginName = "XR Origin (VR)";
    const string LocomotionRootName = "PPE Teleport-Only Locomotion";
    const string MoveChildName = "Move";
    const string SpeedParameter = "Speed";
    const string WalkFbxPath = "Assets/FBX/PPE_D_Player/PPE_D_Player.fbx";
    const string OutputFolder = "Assets/Animations";
    const string AvatarPath = OutputFolder + "/PPE_D_Player_Idle.asset";
    const string MaskPath = OutputFolder + "/PPE_D_Player_Idle_LowerBody.mask";
    const string IdleClipPath = OutputFolder + "/PPE_D_Player_Idle_Idle.anim";
    const string WalkClipPath = OutputFolder + "/PPE_D_Player_Idle_Walk.anim";
    const string ControllerPath = OutputFolder + "/PPE_D_Player_Idle.controller";

    static readonly HashSet<string> LowerBodyBones = new(StringComparer.Ordinal)
    {
        "Hip",
        "Pelvis",
        "L_Thigh",
        "L_Calf",
        "L_Foot",
        "L_ToeBase",
        "L_CalfTwist01",
        "L_CalfTwist02",
        "L_ThighTwist01",
        "L_ThighTwist02",
        "R_Thigh",
        "R_Calf",
        "R_Foot",
        "R_ToeBase",
        "R_CalfTwist01",
        "R_CalfTwist02",
        "R_ThighTwist01",
        "R_ThighTwist02",
    };

    [MenuItem("Tools/PPE/Setup Idle Locomotion Animator")]
    public static void ConfigureFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("Play Mode를 종료한 뒤 idle 로코모션 애니메이터 연결을 실행해야 합니다.");
            return;
        }

        if (!Application.isBatchMode &&
            !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("Idle locomotion animator setup was cancelled.");
            return;
        }

        Configure(RequireTargetScene(), true);
    }

    public static void Configure(Scene scene, bool save)
    {
        if (scene.path != ScenePath)
        {
            throw new InvalidOperationException(
                $"Idle locomotion animator can only be wired in '{ScenePath}'. Current scene: '{scene.path}'.");
        }

        Undo.SetCurrentGroupName("Setup idle locomotion animator");
        int undoGroup = Undo.GetCurrentGroup();

        try
        {
            GameObject idle = RequireNamed(scene, IdleName).gameObject;
            Transform armature = FindChild(idle.transform, "Armature");
            if (armature == null)
                throw new InvalidOperationException($"{IdleName} is missing Armature.");

            SkinnedMeshRenderer skin = idle.GetComponentInChildren<SkinnedMeshRenderer>(true);
            if (skin == null || skin.bones == null || skin.bones.Length == 0)
                throw new InvalidOperationException($"{IdleName} is missing a skinned mesh with bones.");

            CompareSkeletons(idle.transform);

            Animator animator = idle.GetComponent<Animator>();
            if (animator == null)
                animator = Undo.AddComponent<Animator>(idle);

            AnimationClip sourceWalk = PrepareWalkSourceClip();
            AnimationClip idleClip = BakeLowerBodyPoseClip(idle.transform, IdleClipPath, "Idle");
            AnimationClip walkClip = sourceWalk != null
                ? RemapLowerBodyClip(sourceWalk, idle.transform, WalkClipPath)
                : BakeWalkPoseFromModel(idle.transform, WalkClipPath);
            Avatar avatar = BuildIdleAvatar(idle);
            AvatarMask mask = BuildLowerBodyMask(idle.transform);
            AnimatorController controller = BuildController(idleClip, walkClip, mask);

            Undo.RecordObject(animator, "Configure idle Animator");
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.avatar = avatar;
            animator.runtimeAnimatorController = controller;
            animator.enabled = true;
            EditorUtility.SetDirty(animator);

            PPEIdleLocomotionAnimator driver = idle.GetComponent<PPEIdleLocomotionAnimator>();
            if (driver == null)
                driver = Undo.AddComponent<PPEIdleLocomotionAnimator>(idle);

            XROrigin origin = RequireNamed(scene, OriginName).GetComponent<XROrigin>();
            if (origin == null)
                throw new InvalidOperationException("XR Origin (VR) is missing XROrigin.");

            Undo.RecordObject(driver, "Configure idle locomotion driver");
            driver.ConfigureForEditor(animator, origin.transform, FindMoveProvider(scene));
            EditorUtility.SetDirty(driver);

            EditorSceneManager.MarkSceneDirty(scene);
            if (save && !EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException($"'{ScenePath}' 저장에 실패했습니다.");

            string walkSource = sourceWalk != null
                ? $"walk clip '{sourceWalk.name}' from PPE_D_Player.fbx"
                : "walk bind pose fallback (FBX take did not import as a clip)";
            Debug.Log(
                $"Idle locomotion animator wired on {IdleName}: lower-body Speed blend, {walkSource}. " +
                "Worn PPE stays on authored local poses and will not follow bones. Original mask scene was not changed.");
        }
        finally
        {
            Undo.CollapseUndoOperations(undoGroup);
        }
    }

    static void CompareSkeletons(Transform idleRoot)
    {
        GameObject walkAsset = AssetDatabase.LoadAssetAtPath<GameObject>(WalkFbxPath);
        if (walkAsset == null)
            throw new InvalidOperationException($"'{WalkFbxPath}' was not found.");

        GameObject walkInstance = UnityEngine.Object.Instantiate(walkAsset);
        walkInstance.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            HashSet<string> idleBones = CollectBoneNames(idleRoot);
            HashSet<string> walkBones = CollectBoneNames(walkInstance.transform);
            if (!idleBones.Contains("L_Thigh") || !idleBones.Contains("R_Thigh") ||
                !idleBones.Contains("L_Calf") || !idleBones.Contains("R_Calf"))
            {
                throw new InvalidOperationException($"{IdleName} is missing thigh/calf bones.");
            }

            List<string> missing = new();
            foreach (string bone in LowerBodyBones)
            {
                if (!idleBones.Contains(bone))
                    continue;
                if (!walkBones.Contains(bone))
                    missing.Add(bone);
            }

            if (missing.Count > 0)
            {
                throw new InvalidOperationException(
                    "Idle and walk skeletons do not share required lower-body bones: " +
                    string.Join(", ", missing));
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(walkInstance);
        }
    }

    static AnimationClip PrepareWalkSourceClip()
    {
        ModelImporter importer = AssetImporter.GetAtPath(WalkFbxPath) as ModelImporter;
        if (importer == null)
            throw new InvalidOperationException($"'{WalkFbxPath}' is not a model importer.");

        bool changed = false;
        if (importer.animationType != ModelImporterAnimationType.Generic)
        {
            importer.animationType = ModelImporterAnimationType.Generic;
            changed = true;
        }

        if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
        {
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            changed = true;
        }

        if (!importer.importAnimation)
        {
            importer.importAnimation = true;
            changed = true;
        }

        if (changed)
            importer.SaveAndReimport();

        ModelImporterClipAnimation[] defaults = importer.defaultClipAnimations;
        if (defaults != null && defaults.Length > 0)
        {
            List<ModelImporterClipAnimation> selected = new();
            for (int i = 0; i < defaults.Length; i++)
            {
                ModelImporterClipAnimation clip = defaults[i];
                bool isLift = clip.name != null &&
                    clip.name.IndexOf("lift", StringComparison.OrdinalIgnoreCase) >= 0;
                bool isWalk = clip.name != null &&
                    clip.name.IndexOf("walk", StringComparison.OrdinalIgnoreCase) >= 0;

                if (isLift)
                    continue;

                if (isWalk)
                {
                    clip.loopTime = true;
                    clip.lockRootRotation = true;
                    clip.lockRootHeightY = true;
                    clip.lockRootPositionXZ = true;
                    clip.keepOriginalOrientation = true;
                    clip.keepOriginalPositionY = true;
                    clip.keepOriginalPositionXZ = true;
                }

                selected.Add(clip);
            }

            importer.clipAnimations = selected.ToArray();
            importer.SaveAndReimport();
        }

        AnimationClip walk = null;
        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(WalkFbxPath))
        {
            if (asset is not AnimationClip clip || clip.name.IndexOf("__preview__", StringComparison.Ordinal) >= 0)
                continue;
            if (clip.name.IndexOf("lift", StringComparison.OrdinalIgnoreCase) >= 0)
                continue;
            if (clip.name.IndexOf("walk", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                walk = clip;
                break;
            }

            walk ??= clip;
        }

        return walk;
    }

    static AnimationClip BakeWalkPoseFromModel(Transform idleRoot, string assetPath)
    {
        GameObject walkAsset = AssetDatabase.LoadAssetAtPath<GameObject>(WalkFbxPath);
        GameObject walkInstance = UnityEngine.Object.Instantiate(walkAsset);
        walkInstance.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            Dictionary<string, Transform> walkBones = IndexBones(walkInstance.transform);
            AnimationClip clip = LoadOrCreateClip(assetPath, "Walk");
            ClearCurves(clip);
            foreach (Transform idleBone in idleRoot.GetComponentsInChildren<Transform>(true))
            {
                if (!LowerBodyBones.Contains(idleBone.name) ||
                    !walkBones.TryGetValue(idleBone.name, out Transform walkBone))
                {
                    continue;
                }

                string path = AnimationUtility.CalculateTransformPath(idleBone, idleRoot);
                SetConstantTransform(clip, path, walkBone);
            }

            LoopClip(clip);
            EditorUtility.SetDirty(clip);
            return clip;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(walkInstance);
        }
    }

    static AnimationClip BakeLowerBodyPoseClip(Transform idleRoot, string assetPath, string clipName)
    {
        AnimationClip clip = LoadOrCreateClip(assetPath, clipName);
        ClearCurves(clip);
        foreach (Transform bone in idleRoot.GetComponentsInChildren<Transform>(true))
        {
            if (!LowerBodyBones.Contains(bone.name))
                continue;

            string path = AnimationUtility.CalculateTransformPath(bone, idleRoot);
            SetConstantTransform(clip, path, bone);
        }

        LoopClip(clip);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    static AnimationClip RemapLowerBodyClip(AnimationClip source, Transform idleRoot, string assetPath)
    {
        Dictionary<string, Transform> idleBones = IndexBones(idleRoot);
        AnimationClip dest = LoadOrCreateClip(assetPath, "Walk");
        ClearCurves(dest);

        int copied = 0;
        foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(source))
        {
            if (binding.type != typeof(Transform))
                continue;

            string boneName = BoneNameFromPath(binding.path);
            if (!LowerBodyBones.Contains(boneName) || !idleBones.TryGetValue(boneName, out Transform idleBone))
                continue;

            EditorCurveBinding remapped = binding;
            remapped.path = AnimationUtility.CalculateTransformPath(idleBone, idleRoot);
            AnimationUtility.SetEditorCurve(dest, remapped, AnimationUtility.GetEditorCurve(source, binding));
            copied++;
        }

        if (copied == 0)
        {
            throw new InvalidOperationException(
                $"Walk clip '{source.name}' had no lower-body transform curves that match {IdleName}.");
        }

        LoopClip(dest);
        dest.frameRate = source.frameRate > 0f ? source.frameRate : 30f;
        EditorUtility.SetDirty(dest);
        return dest;
    }

    static Avatar BuildIdleAvatar(GameObject idle)
    {
        Avatar built = AvatarBuilder.BuildGenericAvatar(idle, string.Empty);
        if (built == null || !built.isValid)
            throw new InvalidOperationException($"Generic avatar could not be built from {IdleName}.");

        built.name = IdleName;
        Avatar existing = AssetDatabase.LoadAssetAtPath<Avatar>(AvatarPath);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(built, AvatarPath);
            return AssetDatabase.LoadAssetAtPath<Avatar>(AvatarPath);
        }

        EditorUtility.CopySerialized(built, existing);
        UnityEngine.Object.DestroyImmediate(built);
        EditorUtility.SetDirty(existing);
        return existing;
    }

    static AvatarMask BuildLowerBodyMask(Transform idleRoot)
    {
        AvatarMask mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(MaskPath);
        if (mask == null)
        {
            mask = new AvatarMask();
            AssetDatabase.CreateAsset(mask, MaskPath);
            mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(MaskPath);
        }

        mask.AddTransformPath(idleRoot, true);
        for (int i = 0; i < mask.transformCount; i++)
        {
            string boneName = BoneNameFromPath(mask.GetTransformPath(i));
            mask.SetTransformActive(i, LowerBodyBones.Contains(boneName));
        }

        EditorUtility.SetDirty(mask);
        return mask;
    }

    static AnimatorController BuildController(AnimationClip idleClip, AnimationClip walkClip, AvatarMask mask)
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        EnsureFloatParameter(controller, SpeedParameter);
        RemoveUnusedDefaultParameter(controller);

        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        AnimatorState state = EnsureLocomotionState(stateMachine);
        BlendTree tree = EnsureBlendTree(controller);
        tree.blendType = BlendTreeType.Simple1D;
        tree.blendParameter = SpeedParameter;
        tree.useAutomaticThresholds = false;
        tree.children = new[]
        {
            new ChildMotion { motion = idleClip, threshold = 0f, timeScale = 1f },
            new ChildMotion { motion = walkClip, threshold = 1f, timeScale = 1f },
        };

        state.name = "Locomotion";
        state.motion = tree;
        stateMachine.defaultState = state;

        AnimatorControllerLayer[] layers = controller.layers;
        layers[0].name = "Lower Body";
        layers[0].avatarMask = mask;
        layers[0].defaultWeight = 1f;
        controller.layers = layers;

        EditorUtility.SetDirty(tree);
        EditorUtility.SetDirty(state);
        EditorUtility.SetDirty(stateMachine);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        return controller;
    }

    static AnimatorState EnsureLocomotionState(AnimatorStateMachine stateMachine)
    {
        foreach (ChildAnimatorState child in stateMachine.states)
        {
            if (child.state != null && child.state.name == "Locomotion")
                return child.state;
        }

        if (stateMachine.states.Length > 0)
            return stateMachine.states[0].state;

        return stateMachine.AddState("Locomotion");
    }

    static BlendTree EnsureBlendTree(AnimatorController controller)
    {
        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(ControllerPath))
        {
            if (asset is BlendTree existing && existing.name == "Idle Walk")
                return existing;
        }

        BlendTree tree = new() { name = "Idle Walk" };
        AssetDatabase.AddObjectToAsset(tree, controller);
        return tree;
    }

    static void EnsureFloatParameter(AnimatorController controller, string parameterName)
    {
        for (int i = 0; i < controller.parameters.Length; i++)
        {
            if (controller.parameters[i].name != parameterName)
                continue;
            if (controller.parameters[i].type == AnimatorControllerParameterType.Float)
                return;
            controller.RemoveParameter(i);
            break;
        }

        controller.AddParameter(parameterName, AnimatorControllerParameterType.Float);
    }

    static void RemoveUnusedDefaultParameter(AnimatorController controller)
    {
        for (int i = controller.parameters.Length - 1; i >= 0; i--)
        {
            if (controller.parameters[i].name == "Blend")
                controller.RemoveParameter(i);
        }
    }

    static AnimationClip LoadOrCreateClip(string assetPath, string clipName)
    {
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        if (clip != null)
        {
            clip.name = clipName;
            return clip;
        }

        if (!AssetDatabase.IsValidFolder(OutputFolder))
            AssetDatabase.CreateFolder("Assets", "Animations");

        clip = new AnimationClip
        {
            name = clipName,
            frameRate = 30f,
            legacy = false,
        };
        AssetDatabase.CreateAsset(clip, assetPath);
        return AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
    }

    static void ClearCurves(AnimationClip clip)
    {
        foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
            AnimationUtility.SetEditorCurve(clip, binding, null);
        foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            AnimationUtility.SetObjectReferenceCurve(clip, binding, null);
    }

    static void LoopClip(AnimationClip clip)
    {
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        settings.loopBlend = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        clip.wrapMode = WrapMode.Loop;
    }

    static void SetConstantTransform(AnimationClip clip, string path, Transform source)
    {
        Vector3 position = source.localPosition;
        Quaternion rotation = source.localRotation;
        Vector3 scale = source.localScale;
        float end = 1f / 30f;
        clip.SetCurve(path, typeof(Transform), "m_LocalPosition.x", AnimationCurve.Constant(0f, end, position.x));
        clip.SetCurve(path, typeof(Transform), "m_LocalPosition.y", AnimationCurve.Constant(0f, end, position.y));
        clip.SetCurve(path, typeof(Transform), "m_LocalPosition.z", AnimationCurve.Constant(0f, end, position.z));
        clip.SetCurve(path, typeof(Transform), "m_LocalRotation.x", AnimationCurve.Constant(0f, end, rotation.x));
        clip.SetCurve(path, typeof(Transform), "m_LocalRotation.y", AnimationCurve.Constant(0f, end, rotation.y));
        clip.SetCurve(path, typeof(Transform), "m_LocalRotation.z", AnimationCurve.Constant(0f, end, rotation.z));
        clip.SetCurve(path, typeof(Transform), "m_LocalRotation.w", AnimationCurve.Constant(0f, end, rotation.w));
        clip.SetCurve(path, typeof(Transform), "m_LocalScale.x", AnimationCurve.Constant(0f, end, scale.x));
        clip.SetCurve(path, typeof(Transform), "m_LocalScale.y", AnimationCurve.Constant(0f, end, scale.y));
        clip.SetCurve(path, typeof(Transform), "m_LocalScale.z", AnimationCurve.Constant(0f, end, scale.z));
    }

    static HashSet<string> CollectBoneNames(Transform root)
    {
        HashSet<string> names = new(StringComparer.Ordinal);
        foreach (Transform bone in root.GetComponentsInChildren<Transform>(true))
            names.Add(bone.name);
        return names;
    }

    static Dictionary<string, Transform> IndexBones(Transform root)
    {
        Dictionary<string, Transform> bones = new(StringComparer.Ordinal);
        foreach (Transform bone in root.GetComponentsInChildren<Transform>(true))
        {
            if (!bones.ContainsKey(bone.name))
                bones.Add(bone.name, bone);
        }

        return bones;
    }

    static string BoneNameFromPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return string.Empty;

        int slash = path.LastIndexOf('/');
        return slash >= 0 ? path[(slash + 1)..] : path;
    }

    static Transform FindChild(Transform parent, string childName)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
                return child;
        }

        return null;
    }

    static Behaviour FindMoveProvider(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name != MoveChildName ||
                    candidate.parent == null ||
                    candidate.parent.name != LocomotionRootName)
                {
                    continue;
                }

                DynamicMoveProvider move = candidate.GetComponent<DynamicMoveProvider>();
                if (move != null)
                    return move;
            }
        }

        return null;
    }

    static Transform RequireNamed(Scene scene, string objectName)
    {
        Transform match = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name != objectName)
                    continue;
                if (match != null)
                    throw new InvalidOperationException($"'{objectName}' must be unique.");
                match = candidate;
            }
        }

        if (match == null)
            throw new InvalidOperationException($"'{objectName}' was not found.");
        return match;
    }

    static Scene RequireTargetScene()
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
            scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
        {
            throw new InvalidOperationException(
                $"Open '{ScenePath}' before setting up the idle locomotion animator. Current scene: '{scene.path}'.");
        }

        return scene;
    }
}
