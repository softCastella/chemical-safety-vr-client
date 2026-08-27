using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Builds an AnimatorController that preserves the normal open-to-grip blend and adds a second blend
/// for the recorded poke pose. Doing this from code keeps the setup reproducible - the controller can
/// always be regenerated from the clips instead of being wired up by hand.
/// </summary>
public static class HandAnimatorBuilder
{
    const string k_ClipFolder = "Assets/HandPoses";
    const string k_OutputFolder = "Assets/HandPoses";
    const string k_GripParameter = "Grip";
    const string k_PokeParameter = "Poke";
    const string k_GripTreeName = "Grip Blend";
    const string k_PoseTreeName = "Hand Pose Blend";

    /// <summary>Parameter name Unity injects when it creates a blend tree for us.</summary>
    const string k_DefaultParameter = "Blend";

    [MenuItem("Tools/Hand Pose/Build Hand Animator")]
    public static void Build()
    {
        var built = 0;
        foreach (var side in new[] { "L", "R" })
        {
            var openClip = LoadClip($"HandPose_Open_{side}");
            var gripClip = LoadClip($"HandPose_Grip_{side}");
            var pokeClip = LoadClip($"HandPose_Poke_{side}");

            if (openClip == null || gripClip == null || pokeClip == null)
            {
                Debug.LogWarning(
                    $"[HandPose] {side} hand clips are incomplete. Open, Grip, and Poke clips are all required.");
                continue;
            }

            BuildController(side, openClip, gripClip, pokeClip);
            built++;
        }

        if (built == 0)
        {
            Debug.LogError(
                $"[HandPose] No clip pairs found in {k_ClipFolder}. Record HandPose_Open_L and " +
                "HandPose_Grip_L and HandPose_Poke_L with Tools > Hand Pose > Clip Recorder first.");
            return;
        }

        AssetDatabase.SaveAssets();
    }

    static AnimationClip LoadClip(string name)
        => AssetDatabase.LoadAssetAtPath<AnimationClip>($"{k_ClipFolder}/{name}.anim");

    static void BuildController(string side, AnimationClip openClip, AnimationClip gripClip,
        AnimationClip pokeClip)
    {
        var path = $"{k_OutputFolder}/HandAnimator_{side}.controller";

        // Keep an existing controller asset in place so every Animator reference keeps the same GUID.
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(path);

        EnsureFloatParameter(controller, k_GripParameter);
        EnsureFloatParameter(controller, k_PokeParameter);

        var stateMachine = controller.layers[0].stateMachine;
        var state = EnsurePoseState(controller);
        var gripTree = FindBlendTree(path, k_GripTreeName);
        if (gripTree == null && state.motion is BlendTree existingTree &&
            existingTree.name != k_PoseTreeName)
        {
            gripTree = existingTree;
            gripTree.name = k_GripTreeName;
        }

        gripTree ??= CreateBlendTree(controller, k_GripTreeName);
        ConfigureGripTree(gripTree, openClip, gripClip);

        var poseTree = FindBlendTree(path, k_PoseTreeName) ??
            CreateBlendTree(controller, k_PoseTreeName);
        ConfigurePoseTree(poseTree, gripTree, pokeClip);

        state.name = k_PoseTreeName;
        state.motion = poseTree;
        stateMachine.defaultState = state;

        RemoveUnusedDefaultParameter(controller);

        EditorUtility.SetDirty(gripTree);
        EditorUtility.SetDirty(poseTree);
        EditorUtility.SetDirty(state);
        EditorUtility.SetDirty(stateMachine);
        EditorUtility.SetDirty(controller);
        Debug.Log(
            $"[HandPose] Built {path}: {k_GripParameter}=0..1 blends {openClip.name} to {gripClip.name}; " +
            $"{k_PokeParameter}=1 blends to {pokeClip.name}.");
    }

    static AnimatorState EnsurePoseState(AnimatorController controller)
    {
        var stateMachine = controller.layers[0].stateMachine;
        foreach (var child in stateMachine.states)
        {
            if (child.state.name == k_PoseTreeName || child.state.name == k_GripTreeName)
                return child.state;
        }

        if (stateMachine.states.Length > 0)
            return stateMachine.states[0].state;

        return stateMachine.AddState(k_PoseTreeName);
    }

    static BlendTree FindBlendTree(string controllerPath, string treeName)
    {
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(controllerPath))
        {
            if (asset is BlendTree tree && tree.name == treeName)
                return tree;
        }

        return null;
    }

    static BlendTree CreateBlendTree(AnimatorController controller, string treeName)
    {
        var tree = new BlendTree { name = treeName };
        AssetDatabase.AddObjectToAsset(tree, controller);
        return tree;
    }

    static void ConfigureGripTree(BlendTree gripTree, AnimationClip openClip, AnimationClip gripClip)
    {
        gripTree.blendType = BlendTreeType.Simple1D;
        gripTree.blendParameter = k_GripParameter;
        gripTree.useAutomaticThresholds = false;
        gripTree.children = new[]
        {
            new ChildMotion { motion = openClip, threshold = 0f, timeScale = 1f },
            new ChildMotion { motion = gripClip, threshold = 1f, timeScale = 1f },
        };
    }

    static void ConfigurePoseTree(BlendTree poseTree, BlendTree gripTree, AnimationClip pokeClip)
    {
        poseTree.blendType = BlendTreeType.Simple1D;
        poseTree.blendParameter = k_PokeParameter;
        poseTree.useAutomaticThresholds = false;
        poseTree.children = new[]
        {
            new ChildMotion { motion = gripTree, threshold = 0f, timeScale = 1f },
            new ChildMotion { motion = pokeClip, threshold = 1f, timeScale = 1f },
        };
    }

    static void EnsureFloatParameter(AnimatorController controller, string parameterName)
    {
        for (var i = 0; i < controller.parameters.Length; i++)
        {
            var parameter = controller.parameters[i];
            if (parameter.name != parameterName)
                continue;

            if (parameter.type == AnimatorControllerParameterType.Float)
                return;

            controller.RemoveParameter(i);
            break;
        }

        controller.AddParameter(parameterName, AnimatorControllerParameterType.Float);
    }

    /// <summary>
    /// CreateBlendTreeInController adds a float parameter named "Blend" when the controller has none.
    /// The authored trees use the explicit Grip and Poke parameters instead, so drop the leftover.
    /// </summary>
    static void RemoveUnusedDefaultParameter(AnimatorController controller)
    {
        var parameters = new List<AnimatorControllerParameter>(controller.parameters);
        for (var i = parameters.Count - 1; i >= 0; i--)
        {
            if (parameters[i].name == k_DefaultParameter)
                controller.RemoveParameter(i);
        }
    }
}
