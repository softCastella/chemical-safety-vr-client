using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PPEScenarioQuizValidationHarness
{
    private const string ScenePath = "Assets/Scenes/4_PPE_Room.unity";

    [MenuItem("Tools/PPE/Validate Scenario Quiz Pools")]
    public static void Validate()
    {
        List<string> failures = new();
        Scene previewScene = default;

        try
        {
            previewScene = EditorSceneManager.OpenPreviewScene(ScenePath);
            PPEQuizController[] controllers =
                UnityEngine.Object.FindObjectsByType<PPEQuizController>(FindObjectsInactive.Include);
            List<PPEQuizController> sceneControllers = new();
            foreach (PPEQuizController controller in controllers)
                if (controller.gameObject.scene == previewScene)
                    sceneControllers.Add(controller);

            if (sceneControllers.Count != 1)
            {
                failures.Add($"Expected one PPEQuizController in '{ScenePath}', found {sceneControllers.Count}.");
            }
            else
            {
                ValidateController(sceneControllers[0], failures);
            }
        }
        finally
        {
            if (previewScene.IsValid())
                EditorSceneManager.ClosePreviewScene(previewScene);
        }

        ValidateWorkPlanForwarding(failures);

        if (failures.Count > 0)
            throw new InvalidOperationException("[PPE Scenario Quiz] FAIL\n- " + string.Join("\n- ", failures));

        Debug.Log("[PPE Scenario Quiz] PASS: six scenario/mode pools, option counts, topic routing, and work-plan forwarding are valid.");
    }

    private static void ValidateController(PPEQuizController controller, List<string> failures)
    {
        SerializedObject serialized = new(controller);
        SerializedProperty catalogProperty = serialized.FindProperty("questionCatalog");
        PPEQuizQuestionCatalog catalog = catalogProperty?.objectReferenceValue as PPEQuizQuestionCatalog;
        if (catalog == null)
        {
            failures.Add("PPEQuizController.questionCatalog is unassigned.");
            return;
        }

        HashSet<(ScenarioDetailModal.PpeWorkPlan, ScenarioDetailModal.PpeLearningMode)> found = new();
        foreach (PPEQuizQuestionPool pool in catalog.Pools ?? Array.Empty<PPEQuizQuestionPool>())
        {
            if (pool == null)
            {
                failures.Add("Question catalog contains a null pool.");
                continue;
            }

            var key = (pool.WorkPlan, pool.LearningMode);
            if (!found.Add(key))
                failures.Add($"Duplicate pool: {pool.WorkPlan}/{pool.LearningMode}.");
            ValidatePool(pool, failures);
        }

        foreach (ScenarioDetailModal.PpeWorkPlan workPlan in new[]
                 {
                     ScenarioDetailModal.PpeWorkPlan.ConfinedSpace,
                     ScenarioDetailModal.PpeWorkPlan.LeakResponse,
                 })
        {
            foreach (ScenarioDetailModal.PpeLearningMode mode in Enum.GetValues(typeof(ScenarioDetailModal.PpeLearningMode)))
                if (!found.Contains((workPlan, mode)))
                    failures.Add($"Missing pool: {workPlan}/{mode}.");
        }
    }

    private static void ValidatePool(PPEQuizQuestionPool pool, List<string> failures)
    {
        PPEQuizQuestion[] questions = pool.Questions;
        string label = $"{pool.WorkPlan}/{pool.LearningMode}";
        if (questions == null || questions.Length < 6)
        {
            failures.Add($"{label} must contain at least six questions so each session can randomly select five.");
            return;
        }

        int expectedOptions = pool.LearningMode == ScenarioDetailModal.PpeLearningMode.Test ? 4 : 3;
        HashSet<string> prompts = new(StringComparer.Ordinal);
        HashSet<PPEQuizTopic> topics = new();
        foreach (PPEQuizQuestion question in questions)
        {
            if (question == null || string.IsNullOrWhiteSpace(question.prompt))
            {
                failures.Add($"{label} contains an empty question.");
                continue;
            }

            if (!prompts.Add(question.prompt))
                failures.Add($"{label} contains a duplicate prompt: {question.prompt}");
            if (question.prompt.StartsWith("Q", StringComparison.OrdinalIgnoreCase))
                failures.Add($"{label} stores a fixed Q-number in the randomized prompt: {question.prompt}");
            if (question.options == null || question.options.Length != expectedOptions)
                failures.Add($"{label} question '{question.prompt}' requires {expectedOptions} options.");
            else if (question.correctOption < 0 || question.correctOption >= question.options.Length)
                failures.Add($"{label} question '{question.prompt}' has an invalid correctOption.");
            if (pool.LearningMode != ScenarioDetailModal.PpeLearningMode.Test &&
                string.IsNullOrWhiteSpace(question.feedback))
                failures.Add($"{label} question '{question.prompt}' requires feedback.");

            topics.Add(question.topic);
            if (pool.WorkPlan == ScenarioDetailModal.PpeWorkPlan.ConfinedSpace &&
                (question.topic == PPEQuizTopic.SafetyGoggles || question.topic == PPEQuizTopic.FaceShield))
                failures.Add($"{label} contains leak-only topic {question.topic}.");
            if (pool.WorkPlan == ScenarioDetailModal.PpeWorkPlan.LeakResponse &&
                (question.topic == PPEQuizTopic.TacticalHarness || question.topic == PPEQuizTopic.SuppliedAirMask))
                failures.Add($"{label} contains confined-space-only topic {question.topic}.");

            if (question.prompt.Contains("김이 서", StringComparison.Ordinal) ||
                question.prompt.Contains("김서림", StringComparison.Ordinal))
            {
                string correct = question.options[question.correctOption];
                if (!correct.Contains("작업을 멈추고", StringComparison.Ordinal) ||
                    !correct.Contains("밀착", StringComparison.Ordinal))
                    failures.Add($"{label} fogging answer must stop work and restore a snug fit.");
            }
        }

        RequireTopic(topics, PPEQuizTopic.ChemicalGloves, label, failures);
        if (pool.WorkPlan == ScenarioDetailModal.PpeWorkPlan.LeakResponse)
        {
            RequireTopic(topics, PPEQuizTopic.SafetyGoggles, label, failures);
            RequireTopic(topics, PPEQuizTopic.FaceShield, label, failures);
        }
    }

    private static void RequireTopic(
        HashSet<PPEQuizTopic> topics,
        PPEQuizTopic topic,
        string label,
        List<string> failures)
    {
        if (!topics.Contains(topic))
            failures.Add($"{label} is missing required topic {topic}.");
    }

    private static void ValidateWorkPlanForwarding(List<string> failures)
    {
        string source = File.ReadAllText("Assets/Scripts/PPEVoiceFlowDirector.cs");
        if (!source.Contains(
                "BeginQuiz(ActiveLearningMode, ActiveWorkPlan)",
                StringComparison.Ordinal))
            failures.Add("PPEVoiceFlowDirector does not forward ActiveWorkPlan to PPEQuizController.");
    }
}
