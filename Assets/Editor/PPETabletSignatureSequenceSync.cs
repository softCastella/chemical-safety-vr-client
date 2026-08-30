using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Rebuilds the tablet <see cref="HandwrittenSignatureSequence"/> step list so it matches the
/// checklist and signature objects that are currently authored under Signature_Test_Sequence.
/// Only the step list is written. Transforms, materials and existing per-step authored values
/// are preserved, and no scene object is created or destroyed.
/// </summary>
public static class PPETabletSignatureSequenceSync
{
    /// <summary>Playback order: left column top to bottom, then right column, then signatures.</summary>
    static readonly string[] OrderedStepNames =
    {
        "Checklist_Check_01",
        "Checklist_Check_02",
        "Checklist_Check_03",
        "Checklist_Check_04",
        "Checklist_Check_05",
        "Checklist_Check_06",
        "Checklist_Check_07",
        "Player_Signature",
        "Conductor_Signature"
    };

    /// <summary>Step whose authored sound and timing are copied when a new checklist step is added.</summary>
    const string CheckDefaultsSourceName = "Checklist_Check_05";

    const string StepsProperty = "signatures";
    const string TargetRendererProperty = "targetRenderer";
    const string SoundProperty = "sound";
    const string SoundVolumeProperty = "soundVolume";
    const string AnimateRevealProperty = "animateReveal";
    const string DelayBeforeProperty = "delayBefore";
    const string DurationProperty = "duration";
    const string RevealCurveProperty = "revealCurve";

    [MenuItem("Tools/PPE/Sync Tablet Signature Sequence Steps")]
    public static void Sync()
    {
        if (!TryResolveSequence(out HandwrittenSignatureSequence sequence))
            return;

        if (!TryResolveOrderedRenderers(sequence, out List<Renderer> orderedRenderers))
            return;

        SerializedObject serializedSequence = new(sequence);
        SerializedProperty steps = serializedSequence.FindProperty(StepsProperty);
        if (steps == null)
        {
            Debug.LogError(
                $"[PPE Tablet] '{StepsProperty}' not found on {Describe(sequence)}. " +
                "HandwrittenSignatureSequence may have been renamed.",
                sequence);
            return;
        }

        Dictionary<Renderer, StepData> authoredByRenderer = new();
        List<StepData> authoredSteps = new();
        for (int index = 0; index < steps.arraySize; index++)
        {
            StepData step = StepData.Read(steps.GetArrayElementAtIndex(index));
            authoredSteps.Add(step);
            if (step.TargetRenderer != null)
                authoredByRenderer.TryAdd(step.TargetRenderer, step);
        }

        StepData checkDefaults = ResolveCheckDefaults(sequence, authoredByRenderer);
        List<StepData> syncedSteps = new(orderedRenderers.Count);
        List<string> addedNames = new();

        foreach (Renderer renderer in orderedRenderers)
        {
            if (authoredByRenderer.TryGetValue(renderer, out StepData authored))
            {
                syncedSteps.Add(authored);
                continue;
            }

            StepData added = checkDefaults != null ? checkDefaults.Clone() : StepData.CreateFallback();
            added.TargetRenderer = renderer;
            added.AnimateReveal = true;
            syncedSteps.Add(added);
            addedNames.Add(renderer.name);
        }

        List<string> droppedNames = new();
        HashSet<Renderer> syncedRenderers = new(orderedRenderers);
        foreach (StepData step in authoredSteps)
        {
            if (step.TargetRenderer == null)
                droppedNames.Add("(empty reference)");
            else if (!syncedRenderers.Contains(step.TargetRenderer))
                droppedNames.Add(step.TargetRenderer.name);
        }

        if (addedNames.Count == 0 && droppedNames.Count == 0 && IsAlreadyOrdered(authoredSteps, orderedRenderers))
        {
            Debug.Log($"[PPE Tablet] Step list on {Describe(sequence)} already matches the authored layout.", sequence);
            return;
        }

        steps.arraySize = syncedSteps.Count;
        for (int index = 0; index < syncedSteps.Count; index++)
            syncedSteps[index].Write(steps.GetArrayElementAtIndex(index));

        // ApplyModifiedProperties registers its own undo entry, so do not add a second one.
        serializedSequence.ApplyModifiedProperties();
        Undo.SetCurrentGroupName("Sync Tablet Signature Sequence Steps");
        EditorUtility.SetDirty(sequence);
        EditorSceneManager.MarkSceneDirty(sequence.gameObject.scene);

        StringBuilder report = new();
        report.AppendLine($"[PPE Tablet] Synced {syncedSteps.Count} steps on {Describe(sequence)}.");
        if (addedNames.Count > 0)
            report.AppendLine($"  added: {string.Join(", ", addedNames)}");
        if (droppedNames.Count > 0)
            report.AppendLine($"  removed from list: {string.Join(", ", droppedNames)}");
        report.Append("  order: ");
        report.Append(DescribeOrder(syncedSteps));
        report.Append("\n  Save the scene to persist this change.");
        Debug.Log(report.ToString(), sequence);
    }

    [MenuItem("Tools/PPE/Validate Tablet Signature Sequence Steps")]
    public static void Validate()
    {
        if (!TryResolveSequence(out HandwrittenSignatureSequence sequence))
            return;

        SerializedObject serializedSequence = new(sequence);
        SerializedProperty steps = serializedSequence.FindProperty(StepsProperty);
        if (steps == null)
        {
            Debug.LogError($"[PPE Tablet] '{StepsProperty}' not found on {Describe(sequence)}.", sequence);
            return;
        }

        StringBuilder report = new();
        report.AppendLine($"[PPE Tablet] {Describe(sequence)} has {steps.arraySize} steps.");
        bool healthy = true;

        for (int index = 0; index < steps.arraySize; index++)
        {
            StepData step = StepData.Read(steps.GetArrayElementAtIndex(index));
            if (step.TargetRenderer == null)
            {
                report.AppendLine($"  [{index}] MISSING renderer reference");
                healthy = false;
                continue;
            }

            bool activeInHierarchy = step.TargetRenderer.gameObject.activeInHierarchy;
            report.AppendLine(
                $"  [{index}] {step.TargetRenderer.name} animate={step.AnimateReveal} " +
                $"delay={step.DelayBefore} duration={step.Duration} active={activeInHierarchy}");
            if (!activeInHierarchy)
                healthy = false;
        }

        foreach (string name in OrderedStepNames)
        {
            Renderer renderer = FindStepRenderer(sequence, name);
            if (renderer == null)
            {
                report.AppendLine($"  authored object '{name}' has no Renderer under {sequence.name}");
                healthy = false;
                continue;
            }

            bool referenced = false;
            for (int index = 0; index < steps.arraySize && !referenced; index++)
            {
                SerializedProperty element = steps.GetArrayElementAtIndex(index);
                referenced = element.FindPropertyRelative(TargetRendererProperty).objectReferenceValue == renderer;
            }

            if (!referenced)
            {
                report.AppendLine($"  authored object '{name}' is NOT referenced by any step");
                healthy = false;
            }
        }

        if (healthy)
            Debug.Log(report.ToString(), sequence);
        else
            Debug.LogWarning(report.ToString(), sequence);
    }

    [MenuItem("Tools/PPE/Validate Immediate Tablet Checklist Reveal")]
    public static void ValidateImmediateChecklistReveal()
    {
        Scene scene = SceneManager.GetActiveScene();
        PPETabletChecklistController controller = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (PPETabletChecklistController candidate in
                root.GetComponentsInChildren<PPETabletChecklistController>(true))
            {
                if (controller != null)
                    throw new InvalidOperationException("The active scene contains multiple Tablet checklist controllers.");
                controller = candidate;
            }
        }

        if (controller == null || controller.SignatureSequence == null)
            throw new InvalidOperationException("The active scene requires one configured Tablet checklist controller.");

        int checklistCount = 0;
        while (checklistCount < OrderedStepNames.Length &&
            OrderedStepNames[checklistCount].StartsWith("Checklist_Check_", StringComparison.Ordinal))
        {
            checklistCount++;
        }

        if (controller.InstantChecklistStepCount != checklistCount)
        {
            throw new InvalidOperationException(
                $"Tablet must reveal {checklistCount} checklist steps immediately; " +
                $"configured value is {controller.InstantChecklistStepCount}.");
        }

        SerializedObject serializedController = new(controller);
        HashSet<HandwrittenSignatureSequence> sequences = new();
        AddSequenceReference(serializedController, "confinedSignatureSequence", sequences);
        AddSequenceReference(serializedController, "leakSignatureSequence", sequences);
        sequences.Add(controller.SignatureSequence);

        foreach (HandwrittenSignatureSequence sequence in sequences)
            ValidateOrderedSequence(sequence, checklistCount);

        string controllerSource = File.ReadAllText("Assets/Scripts/PPETabletChecklistController.cs");
        string sequenceSource = File.ReadAllText("Assets/Scripts/HandwrittenSignatureSequence.cs");
        if (!controllerSource.Contains("PlayRangeWithInstantPrefix(", StringComparison.Ordinal) ||
            !sequenceSource.Contains("public bool PlayRangeWithInstantPrefix(", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Tablet Trigger must use the instant-prefix sequence playback path.");
        }

        Debug.Log(
            $"[PPE Tablet] PASS: Trigger reveals all {checklistCount} checklist marks immediately; " +
            "the remaining signature steps keep their authored playback.",
            controller);
    }

    static void AddSequenceReference(
        SerializedObject controller,
        string propertyName,
        HashSet<HandwrittenSignatureSequence> sequences)
    {
        HandwrittenSignatureSequence sequence = controller.FindProperty(propertyName)
            ?.objectReferenceValue as HandwrittenSignatureSequence;
        if (sequence != null)
            sequences.Add(sequence);
    }

    static void ValidateOrderedSequence(
        HandwrittenSignatureSequence sequence,
        int checklistCount)
    {
        SerializedProperty steps = new SerializedObject(sequence).FindProperty(StepsProperty);
        if (steps == null || steps.arraySize != OrderedStepNames.Length)
        {
            throw new InvalidOperationException(
                $"Tablet sequence '{Describe(sequence)}' must contain exactly " +
                $"{OrderedStepNames.Length} ordered steps.");
        }

        for (int index = 0; index < OrderedStepNames.Length; index++)
        {
            Renderer renderer = steps.GetArrayElementAtIndex(index)
                .FindPropertyRelative(TargetRendererProperty).objectReferenceValue as Renderer;
            if (renderer == null || renderer.name != OrderedStepNames[index])
            {
                throw new InvalidOperationException(
                    $"Tablet sequence '{Describe(sequence)}' step {index} must reference " +
                    $"'{OrderedStepNames[index]}'.");
            }
        }

        if (checklistCount != 7)
            throw new InvalidOperationException("Tablet checklist contract must contain seven checks.");
    }

    static bool TryResolveSequence(out HandwrittenSignatureSequence sequence)
    {
        sequence = null;

        HandwrittenSignatureSequence[] found =
            UnityEngine.Object.FindObjectsByType<HandwrittenSignatureSequence>(FindObjectsInactive.Include);

        List<HandwrittenSignatureSequence> candidates = new();
        foreach (HandwrittenSignatureSequence candidate in found)
        {
            if (candidate.enabled && CountAssignedRenderers(candidate) > 0)
                candidates.Add(candidate);
        }

        if (Selection.activeGameObject != null)
        {
            List<HandwrittenSignatureSequence> selected = new();
            foreach (HandwrittenSignatureSequence candidate in candidates)
            {
                if (candidate.gameObject == Selection.activeGameObject)
                    selected.Add(candidate);
            }

            if (selected.Count == 1)
            {
                sequence = selected[0];
                return true;
            }
        }

        if (candidates.Count == 1)
        {
            sequence = candidates[0];
            return true;
        }

        if (candidates.Count == 0)
        {
            Debug.LogError(
                "[PPE Tablet] No enabled HandwrittenSignatureSequence with assigned renderers was found " +
                "in the open scenes. Open the tablet scene before running this command.");
            return false;
        }

        StringBuilder paths = new();
        foreach (HandwrittenSignatureSequence candidate in candidates)
            paths.Append("\n  ").Append(Describe(candidate));

        Debug.LogError(
            "[PPE Tablet] Multiple enabled HandwrittenSignatureSequence components were found. " +
            "Select the one to sync in the Hierarchy and run the command again." + paths);
        return false;
    }

    static bool TryResolveOrderedRenderers(
        HandwrittenSignatureSequence sequence,
        out List<Renderer> orderedRenderers)
    {
        orderedRenderers = new List<Renderer>(OrderedStepNames.Length);
        List<string> missing = new();

        foreach (string name in OrderedStepNames)
        {
            Renderer renderer = FindStepRenderer(sequence, name);
            if (renderer == null)
                missing.Add(name);
            else
                orderedRenderers.Add(renderer);
        }

        if (missing.Count == 0)
            return true;

        Debug.LogError(
            $"[PPE Tablet] Missing Renderer for: {string.Join(", ", missing)} under {Describe(sequence)}. " +
            "Create or rename these objects in the scene first. This command does not create scene objects.",
            sequence);
        orderedRenderers = null;
        return false;
    }

    static Renderer FindStepRenderer(HandwrittenSignatureSequence sequence, string name)
    {
        Transform child = sequence.transform.Find(name);
        return child != null ? child.GetComponent<Renderer>() : null;
    }

    static StepData ResolveCheckDefaults(
        HandwrittenSignatureSequence sequence,
        Dictionary<Renderer, StepData> authoredByRenderer)
    {
        Renderer defaultsSource = FindStepRenderer(sequence, CheckDefaultsSourceName);
        return defaultsSource != null && authoredByRenderer.TryGetValue(defaultsSource, out StepData step)
            ? step
            : null;
    }

    static int CountAssignedRenderers(HandwrittenSignatureSequence sequence)
    {
        SerializedProperty steps = new SerializedObject(sequence).FindProperty(StepsProperty);
        if (steps == null)
            return 0;

        int assigned = 0;
        for (int index = 0; index < steps.arraySize; index++)
        {
            SerializedProperty element = steps.GetArrayElementAtIndex(index);
            if (element.FindPropertyRelative(TargetRendererProperty).objectReferenceValue != null)
                assigned++;
        }

        return assigned;
    }

    static bool IsAlreadyOrdered(List<StepData> authoredSteps, List<Renderer> orderedRenderers)
    {
        if (authoredSteps.Count != orderedRenderers.Count)
            return false;

        for (int index = 0; index < authoredSteps.Count; index++)
        {
            if (authoredSteps[index].TargetRenderer != orderedRenderers[index])
                return false;
        }

        return true;
    }

    static string DescribeOrder(List<StepData> steps)
    {
        StringBuilder order = new();
        for (int index = 0; index < steps.Count; index++)
        {
            if (index > 0)
                order.Append(" -> ");

            Renderer renderer = steps[index].TargetRenderer;
            order.Append(renderer != null ? renderer.name : "(none)");
            if (!steps[index].AnimateReveal)
                order.Append("(pre-revealed)");
        }

        return order.ToString();
    }

    static string Describe(Component component)
    {
        Transform current = component.transform;
        StringBuilder path = new(current.name);
        while (current.parent != null)
        {
            current = current.parent;
            path.Insert(0, current.name + "/");
        }

        return path.ToString();
    }

    sealed class StepData
    {
        public Renderer TargetRenderer;
        public AudioClip Sound;
        public float SoundVolume = 1f;
        public bool AnimateReveal = true;
        public float DelayBefore;
        public float Duration = 0.85f;
        public AnimationCurve RevealCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public static StepData CreateFallback()
        {
            return new StepData();
        }

        public static StepData Read(SerializedProperty element)
        {
            return new StepData
            {
                TargetRenderer = element.FindPropertyRelative(TargetRendererProperty).objectReferenceValue as Renderer,
                Sound = element.FindPropertyRelative(SoundProperty).objectReferenceValue as AudioClip,
                SoundVolume = element.FindPropertyRelative(SoundVolumeProperty).floatValue,
                AnimateReveal = element.FindPropertyRelative(AnimateRevealProperty).boolValue,
                DelayBefore = element.FindPropertyRelative(DelayBeforeProperty).floatValue,
                Duration = element.FindPropertyRelative(DurationProperty).floatValue,
                RevealCurve = element.FindPropertyRelative(RevealCurveProperty).animationCurveValue
            };
        }

        public void Write(SerializedProperty element)
        {
            element.FindPropertyRelative(TargetRendererProperty).objectReferenceValue = TargetRenderer;
            element.FindPropertyRelative(SoundProperty).objectReferenceValue = Sound;
            element.FindPropertyRelative(SoundVolumeProperty).floatValue = SoundVolume;
            element.FindPropertyRelative(AnimateRevealProperty).boolValue = AnimateReveal;
            element.FindPropertyRelative(DelayBeforeProperty).floatValue = DelayBefore;
            element.FindPropertyRelative(DurationProperty).floatValue = Duration;
            element.FindPropertyRelative(RevealCurveProperty).animationCurveValue = RevealCurve;
        }

        public StepData Clone()
        {
            return new StepData
            {
                TargetRenderer = TargetRenderer,
                Sound = Sound,
                SoundVolume = SoundVolume,
                AnimateReveal = AnimateReveal,
                DelayBefore = DelayBefore,
                Duration = Duration,
                RevealCurve = RevealCurve != null
                    ? new AnimationCurve(RevealCurve.keys)
                    {
                        preWrapMode = RevealCurve.preWrapMode,
                        postWrapMode = RevealCurve.postWrapMode
                    }
                    : AnimationCurve.EaseInOut(0f, 0f, 1f, 1f)
            };
        }
    }
}
