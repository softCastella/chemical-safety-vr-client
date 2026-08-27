using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Authored work-plan buttons after 학습/훈련/테스트. Education/Training reuse
/// 1_EduChoice. Test clones 2_Mode as 3_WorkPlan so the third label stays scene-authored.
/// </summary>
public static class PPEWorkPlanChoiceSetup
{
    const string ScenePath = "Assets/Scenes/3_PPE_Room_Train_Test_mask.unity";
    const string TestRootName = "3_WorkPlan";

    const string EducationConfinedLabel = "밀폐공간 대응 PPE착용";
    const string EducationLeakLabel = "누출사고 대응 PPE착용";
    const string TestConfinedLabel = "밀폐공간";
    const string TestLeakLabel = "누출 사고 대응 피피이";
    const string TestRandomLabel = "랜덤 테스트";

    [MenuItem("Tools/PPE/Wire Work Plan Choices")]
    public static void ConfigureFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("Play Mode를 종료한 뒤 작업계획 버튼 연결을 실행해야 합니다.");
            return;
        }

        if (!Application.isBatchMode &&
            !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("Work plan choice wiring was cancelled.");
            return;
        }

        ConfigureInternal(true);
    }

    [MenuItem("Tools/PPE/Validate Work Plan Choices")]
    public static void ValidateFromMenu()
    {
        ValidateScene(RequireTargetScene(), true);
    }

    static void ConfigureInternal(bool save)
    {
        Scene scene = RequireTargetScene();
        ScenarioDetailModal modal = FindDirectCardModal(scene);
        GameObject modeRoot = RequireModeRoot(modal);
        GameObject educationRoot = RequireEducationRoot(modal);

        Undo.SetCurrentGroupName("Wire PPE work plan choices");
        int undoGroup = Undo.GetCurrentGroup();

        try
        {
            ApplyEducationWorkPlanLabels(modal, educationRoot);
            GameObject testRoot = EnsureTestWorkPlanRoot(modeRoot);
            WireTestWorkPlanButtons(modal, testRoot);
            ValidateScene(scene, false);
            EditorSceneManager.MarkSceneDirty(scene);
            if (save && !EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException($"'{ScenePath}' 저장에 실패했습니다.");

            Debug.Log(
                "Work plan choice wiring complete: 1_EduChoice has confined/leak labels; " +
                "3_WorkPlan is the authored test three-button group.");
        }
        finally
        {
            Undo.CollapseUndoOperations(undoGroup);
        }
    }

    static void ApplyEducationWorkPlanLabels(ScenarioDetailModal modal, GameObject educationRoot)
    {
        SerializedObject serializedModal = new(modal);
        Button confined = RequireButton(serializedModal, "incompletePpeButton");
        Button leak = RequireButton(serializedModal, "standardTrainingButton");
        if (confined.transform.parent != educationRoot.transform)
            throw new InvalidOperationException("incompletePpeButton is not under 1_EduChoice.");
        if (leak.transform.parent != educationRoot.transform)
            throw new InvalidOperationException("standardTrainingButton is not under 1_EduChoice.");

        SetButtonLabel(confined, EducationConfinedLabel);
        SetButtonLabel(leak, EducationLeakLabel);
    }

    static GameObject EnsureTestWorkPlanRoot(GameObject modeRoot)
    {
        Transform parent = modeRoot.transform.parent;
        if (parent == null)
            throw new InvalidOperationException("2_Mode must have a parent panel.");

        Transform existing = parent.Find(TestRootName);
        if (existing != null)
            return existing.gameObject;

        GameObject clone = UnityEngine.Object.Instantiate(modeRoot, parent);
        clone.name = TestRootName;
        Undo.RegisterCreatedObjectUndo(clone, "Create 3_WorkPlan");
        clone.SetActive(false);

        RectTransform sourceRect = modeRoot.GetComponent<RectTransform>();
        RectTransform cloneRect = clone.GetComponent<RectTransform>();
        if (sourceRect != null && cloneRect != null)
        {
            cloneRect.anchorMin = sourceRect.anchorMin;
            cloneRect.anchorMax = sourceRect.anchorMax;
            cloneRect.pivot = sourceRect.pivot;
            cloneRect.anchoredPosition = sourceRect.anchoredPosition;
            cloneRect.sizeDelta = sourceRect.sizeDelta;
            cloneRect.localRotation = sourceRect.localRotation;
            cloneRect.localScale = sourceRect.localScale;
        }

        clone.transform.SetSiblingIndex(modeRoot.transform.GetSiblingIndex() + 1);

        Button[] buttons = GetDirectChildButtons(clone);
        if (buttons.Length != 4)
        {
            throw new InvalidOperationException(
                $"3_WorkPlan must clone 2_Mode's four buttons. Found {buttons.Length}.");
        }

        buttons[0].gameObject.name = "Confined Space Work Plan";
        buttons[1].gameObject.name = "Leak Response Work Plan";
        buttons[2].gameObject.name = "Random Test Work Plan";
        SetButtonLabel(buttons[0], TestConfinedLabel);
        SetButtonLabel(buttons[1], TestLeakLabel);
        SetButtonLabel(buttons[2], TestRandomLabel);

        foreach (Button button in buttons)
            button.onClick = new Button.ButtonClickedEvent();

        return clone;
    }

    static void WireTestWorkPlanButtons(ScenarioDetailModal modal, GameObject testRoot)
    {
        Button[] buttons = GetDirectChildButtons(testRoot);
        if (buttons.Length != 4)
        {
            throw new InvalidOperationException(
                $"3_WorkPlan must have four child buttons. Found {buttons.Length}.");
        }

        SerializedObject serializedModal = new(modal);
        serializedModal.FindProperty("testWorkPlanChoiceRoot").objectReferenceValue = testRoot;
        serializedModal.FindProperty("testConfinedSpaceButton").objectReferenceValue = buttons[0];
        serializedModal.FindProperty("testLeakResponseButton").objectReferenceValue = buttons[1];
        serializedModal.FindProperty("testRandomTestButton").objectReferenceValue = buttons[2];
        serializedModal.FindProperty("testWorkPlanBackButton").objectReferenceValue = buttons[3];
        serializedModal.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(modal);
    }

    static void ValidateScene(Scene scene, bool logSuccess)
    {
        ScenarioDetailModal modal = FindDirectCardModal(scene);
        SerializedObject serializedModal = new(modal);
        GameObject educationRoot = RequireEducationRoot(modal);
        GameObject modeRoot = RequireModeRoot(modal);
        GameObject testRoot = serializedModal.FindProperty("testWorkPlanChoiceRoot").objectReferenceValue as GameObject;

        if (testRoot == null)
            throw new InvalidOperationException("testWorkPlanChoiceRoot is not wired. Run Wire Work Plan Choices.");

        AssertLabel(RequireButton(serializedModal, "incompletePpeButton"), EducationConfinedLabel, educationRoot);
        AssertLabel(RequireButton(serializedModal, "standardTrainingButton"), EducationLeakLabel, educationRoot);
        AssertLabel(RequireButton(serializedModal, "testConfinedSpaceButton"), TestConfinedLabel, testRoot);
        AssertLabel(RequireButton(serializedModal, "testLeakResponseButton"), TestLeakLabel, testRoot);
        AssertLabel(RequireButton(serializedModal, "testRandomTestButton"), TestRandomLabel, testRoot);

        Button modeBack = RequireButton(serializedModal, "ppeModeChoiceBackButton");
        if (modeBack.transform.parent != modeRoot.transform)
            throw new InvalidOperationException("2_Mode back button was moved.");

        TMP_Text modeEducationLabel = RequireButton(serializedModal, "ppeEducationModeButton")
            .GetComponentInChildren<TMP_Text>(true);
        if (modeEducationLabel == null || modeEducationLabel.text != "교육 모드")
            throw new InvalidOperationException("2_Mode education label must stay 교육 모드.");

        if (logSuccess)
            Debug.Log("Work plan choice validation passed.");
    }

    static void AssertLabel(Button button, string expected, GameObject expectedParent)
    {
        if (button.transform.parent != expectedParent.transform)
            throw new InvalidOperationException($"{button.name} is not under {expectedParent.name}.");

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label == null || label.text != expected)
            throw new InvalidOperationException($"{button.name} label must be '{expected}'.");
    }

    static void SetButtonLabel(Button button, string text)
    {
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label == null)
            throw new InvalidOperationException($"{button.name} has no TMP label.");

        Undo.RecordObject(label, "Set work plan label");
        label.text = text;
        RectTransform labelRect = label.rectTransform;
        RectTransform buttonRect = button.transform as RectTransform;
        if (labelRect != null && buttonRect != null && labelRect.sizeDelta.x < buttonRect.sizeDelta.x - 8f)
        {
            Undo.RecordObject(labelRect, "Widen work plan label");
            labelRect.sizeDelta = new Vector2(buttonRect.sizeDelta.x - 19.5f, labelRect.sizeDelta.y);
        }

        EditorUtility.SetDirty(label);
    }

    static Button[] GetDirectChildButtons(GameObject root)
    {
        Button[] buttons = new Button[root.transform.childCount];
        int count = 0;
        for (int i = 0; i < root.transform.childCount; i++)
        {
            Button button = root.transform.GetChild(i).GetComponent<Button>();
            if (button == null)
                continue;
            buttons[count++] = button;
        }

        Array.Resize(ref buttons, count);
        return buttons;
    }

    static ScenarioDetailModal FindDirectCardModal(Scene scene)
    {
        ScenarioDetailModal found = null;
        int count = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (ScenarioDetailModal modal in root.GetComponentsInChildren<ScenarioDetailModal>(true))
            {
                SerializedObject serializedModal = new(modal);
                if (serializedModal.FindProperty("openWithPpeModeChoices")?.boolValue != true)
                    continue;
                found = modal;
                count++;
            }
        }

        if (count != 1)
        {
            throw new InvalidOperationException(
                $"Expected one ScenarioDetailModal with openWithPpeModeChoices in '{scene.path}', found {count}.");
        }

        return found;
    }

    static GameObject RequireModeRoot(ScenarioDetailModal modal)
    {
        var root = new SerializedObject(modal).FindProperty("ppeModeChoiceRoot").objectReferenceValue as GameObject;
        if (root == null)
            throw new InvalidOperationException("ppeModeChoiceRoot is missing.");
        return root;
    }

    static GameObject RequireEducationRoot(ScenarioDetailModal modal)
    {
        var root = new SerializedObject(modal).FindProperty("educationChoiceRoot").objectReferenceValue as GameObject;
        if (root == null)
            throw new InvalidOperationException("educationChoiceRoot is missing.");
        return root;
    }

    static Button RequireButton(SerializedObject serializedModal, string property)
    {
        if (serializedModal.FindProperty(property)?.objectReferenceValue is Button button)
            return button;
        throw new InvalidOperationException($"{property} is not assigned.");
    }

    static Scene RequireTargetScene()
    {
        Scene scene = EditorSceneManager.GetSceneByPath(ScenePath);
        if (scene.IsValid() && scene.isLoaded)
            return scene;

        if (!scene.IsValid() || !scene.isLoaded)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        if (!scene.IsValid() || !scene.isLoaded)
            throw new InvalidOperationException($"Failed to open '{ScenePath}'.");
        return scene;
    }
}
