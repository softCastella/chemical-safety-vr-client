using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ControllerHandAnimatorClipInstaller
{
    const string TargetScenePath = "Assets/Scenes/5_MixerRoom_Unlit.unity";
    const string LeftControllerPath = "Assets/HandPoses/HandAnimator_L.controller";
    const string RightControllerPath = "Assets/HandPoses/HandAnimator_R.controller";

    [MenuItem("Tools/Hand Pose/Install Animator Clips On Controller Hands")]
    public static void Install()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded || scene.path != TargetScenePath)
        {
            Debug.LogError($"Open '{TargetScenePath}' before installing controller hand Animators.");
            return;
        }

        var left = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(LeftControllerPath);
        var right = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(RightControllerPath);
        int installed = 0;
        installed += InstallSide(scene, "LeftHand_", left);
        installed += InstallSide(scene, "RightHand_", right);

        if (installed == 0)
        {
            Debug.LogWarning("No authored controller hand models were found.");
            return;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log($"Installed Animator clips on {installed} controller hand model roots.");
    }

    static int InstallSide(UnityEngine.SceneManagement.Scene scene, string prefix,
        RuntimeAnimatorController controller)
    {
        int count = 0;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (!child.name.StartsWith(prefix, System.StringComparison.Ordinal) ||
                    (!child.name.EndsWith("BareHand", System.StringComparison.Ordinal) &&
                     !child.name.EndsWith("Glove_Suit", System.StringComparison.Ordinal) &&
                     !child.name.EndsWith("Glove_Suit_Tape", System.StringComparison.Ordinal)))
                    continue;

                var animator = child.GetComponent<Animator>();
                if (animator == null)
                    animator = Undo.AddComponent<Animator>(child.gameObject);

                Undo.RecordObject(animator, "Install Controller Hand Animator Clips");
                animator.runtimeAnimatorController = controller;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                EditorUtility.SetDirty(animator);
                count++;
            }
        }

        return count;
    }
}
