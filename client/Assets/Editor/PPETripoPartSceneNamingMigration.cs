using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Renames only scene-authored GameObjects whose complete name is
/// "tripo_part_{number}". FBX assets, bones, transforms, render state,
/// materials, components, and serialized references are not modified.
/// </summary>
public static class PPETripoPartSceneNamingMigration
{
    const string ScenePath = "Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity";
    static readonly Regex PartNamePattern = new Regex(
        "^tripo_part_(?<number>[0-9]+)$",
        RegexOptions.CultureInvariant);

    [MenuItem("Tools/PPE/Rename Scene Tripo Parts Only")]
    public static void ApplyFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before renaming scene tripo parts.");

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
            throw new InvalidOperationException($"Open '{ScenePath}' before renaming scene tripo parts.");

        Transform[] allTransforms = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .ToArray();
        PartRename[] renames = allTransforms
            .Select(CreateRename)
            .Where(rename => rename != null)
            .ToArray();

        if (renames.Length == 0)
        {
            Debug.Log("PPE tripo part naming: no exact tripo_part_{number} scene objects remain.");
            return;
        }

        ValidateOwners(renames);
        ValidateSiblingNameCollisions(renames);
        ValidateAnimationBindings();

        PartSnapshot[] snapshots = renames
            .Select(rename => new PartSnapshot(rename.Target))
            .ToArray();

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Rename PPE tripo part scene objects");
        foreach (PartRename rename in renames)
        {
            Undo.RecordObject(rename.Target.gameObject, "Rename PPE tripo part");
            rename.Target.gameObject.name = rename.NewName;
            EditorUtility.SetDirty(rename.Target.gameObject);
        }

        VerifySnapshots(snapshots);
        int remainingLegacyCount = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .Count(transform => PartNamePattern.IsMatch(transform.name));
        if (remainingLegacyCount != 0)
            throw new InvalidOperationException($"{remainingLegacyCount} exact tripo_part_{{number}} names remain after migration.");

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException($"Failed to save '{ScenePath}'.");

        Undo.CollapseUndoOperations(undoGroup);
        PPE3DAssetRenameDependencyValidation.ValidateScene(scene, true);
        Debug.Log($"PPE tripo part naming applied: renamed={renames.Length}, non-name property changes=0.");
    }

    static PartRename CreateRename(Transform target)
    {
        Match match = PartNamePattern.Match(target.name);
        if (!match.Success)
            return null;

        Transform owner = FindOwner(target.parent);
        string newName = owner == null
            ? null
            : owner.name + "_Part_" + match.Groups["number"].Value;
        return new PartRename(target, owner, newName);
    }

    static Transform FindOwner(Transform current)
    {
        Transform nearestParent = current;
        for (; current != null; current = current.parent)
        {
            if (current.name.StartsWith("PPE_", StringComparison.Ordinal) ||
                current.name == "mask" ||
                current.name == "mask1")
            {
                return current;
            }
        }

        // Validation/test presentations may not be nested under a standardized
        // PPE root. Preserve their authored parent name and use only that nearest
        // parent as the part prefix.
        return nearestParent;
    }

    static void ValidateOwners(IEnumerable<PartRename> renames)
    {
        PartRename[] missing = renames
            .Where(rename => rename.Owner == null || string.IsNullOrEmpty(rename.NewName))
            .ToArray();
        if (missing.Length == 0)
            return;

        throw new InvalidOperationException(
            "Tripo part owner resolution failed before making changes:\n" +
            string.Join("\n", missing.Select(rename => GetPath(rename.Target))));
    }

    static void ValidateSiblingNameCollisions(IEnumerable<PartRename> renames)
    {
        foreach (IGrouping<Transform, PartRename> parentGroup in renames.GroupBy(rename => rename.Target.parent))
        {
            HashSet<Transform> targets = parentGroup
                .Select(rename => rename.Target)
                .ToHashSet();
            foreach (PartRename rename in parentGroup)
            {
                bool collidesWithRename = parentGroup.Any(other =>
                    other != rename &&
                    string.Equals(other.NewName, rename.NewName, StringComparison.Ordinal));
                bool collidesWithExisting = rename.Target.parent.Cast<Transform>().Any(sibling =>
                    !targets.Contains(sibling) &&
                    string.Equals(sibling.name, rename.NewName, StringComparison.Ordinal));
                if (collidesWithRename || collidesWithExisting)
                {
                    throw new InvalidOperationException(
                        $"Tripo part name collision before making changes: '{GetPath(rename.Target)}' -> '{rename.NewName}'.");
                }
            }
        }
    }

    static void ValidateAnimationBindings()
    {
        foreach (string dependency in AssetDatabase.GetDependencies(ScenePath, true))
        {
            if (!string.Equals(Path.GetExtension(dependency), ".anim", StringComparison.OrdinalIgnoreCase))
                continue;

            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(dependency);
            if (clip == null)
                continue;

            IEnumerable<EditorCurveBinding> bindings = AnimationUtility.GetCurveBindings(clip)
                .Concat(AnimationUtility.GetObjectReferenceCurveBindings(clip));
            foreach (EditorCurveBinding binding in bindings)
            {
                string affected = binding.path
                    .Split('/')
                    .FirstOrDefault(segment => PartNamePattern.IsMatch(segment));
                if (affected == null)
                    continue;

                throw new InvalidOperationException(
                    $"AnimationClip '{dependency}::{clip.name}' binds tripo part '{affected}' in '{binding.path}'. " +
                    "No scene names were changed.");
            }
        }
    }

    static void VerifySnapshots(IReadOnlyList<PartSnapshot> snapshots)
    {
        foreach (PartSnapshot snapshot in snapshots)
        {
            Transform target = snapshot.Target;
            if (target == null ||
                target.parent != snapshot.Parent ||
                target.GetSiblingIndex() != snapshot.SiblingIndex ||
                target.localPosition != snapshot.LocalPosition ||
                target.localRotation != snapshot.LocalRotation ||
                target.localScale != snapshot.LocalScale ||
                target.gameObject.activeSelf != snapshot.ActiveSelf ||
                target.gameObject.layer != snapshot.Layer ||
                target.gameObject.tag != snapshot.Tag ||
                !target.GetComponents<Component>()
                    .Select(component => component == null ? "<missing>" : component.GetType().AssemblyQualifiedName)
                    .SequenceEqual(snapshot.ComponentTypes))
            {
                throw new InvalidOperationException(
                    $"A non-name property changed while renaming tripo part instance {snapshot.InstanceId}.");
            }
        }
    }

    static string GetPath(Transform transform)
    {
        List<string> names = new List<string>();
        for (Transform current = transform; current != null; current = current.parent)
            names.Add(current.name);
        names.Reverse();
        return string.Join("/", names);
    }

    sealed class PartRename
    {
        public PartRename(Transform target, Transform owner, string newName)
        {
            Target = target;
            Owner = owner;
            NewName = newName;
        }

        public Transform Target { get; }
        public Transform Owner { get; }
        public string NewName { get; }
    }

    sealed class PartSnapshot
    {
        public PartSnapshot(Transform target)
        {
            Target = target;
            Parent = target.parent;
            SiblingIndex = target.GetSiblingIndex();
            LocalPosition = target.localPosition;
            LocalRotation = target.localRotation;
            LocalScale = target.localScale;
            ActiveSelf = target.gameObject.activeSelf;
            Layer = target.gameObject.layer;
            Tag = target.gameObject.tag;
            InstanceId = target.GetInstanceID();
            ComponentTypes = target.GetComponents<Component>()
                .Select(component => component == null ? "<missing>" : component.GetType().AssemblyQualifiedName)
                .ToArray();
        }

        public Transform Target { get; }
        public Transform Parent { get; }
        public int SiblingIndex { get; }
        public Vector3 LocalPosition { get; }
        public Quaternion LocalRotation { get; }
        public Vector3 LocalScale { get; }
        public bool ActiveSelf { get; }
        public int Layer { get; }
        public string Tag { get; }
        public int InstanceId { get; }
        public string[] ComponentTypes { get; }
    }
}
