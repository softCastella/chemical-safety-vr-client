using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs;

[InitializeOnLoad]
internal static class ScenarioDetailModalSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/3_PPE_Room.unity";
    private const string ModalName = "Scenario Detail Modal";

    static ScenarioDetailModalSceneBuilder()
    {
        EditorApplication.delayCall += BuildLoadedTargetSceneOnce;
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (scene.path == ScenePath)
            EditorApplication.delayCall += BuildLoadedTargetSceneOnce;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
            EditorApplication.delayCall += BuildLoadedTargetSceneOnce;
    }

    [MenuItem("Tools/Scenario HUD/Create Scenario Detail Modal")]
    private static void BuildLoadedTargetSceneOnce()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        ScenarioSelectionHud hud = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<ScenarioSelectionHud>(true))
            .FirstOrDefault();
        if (hud == null)
            return;

        ScenarioDetailModal existingController = hud.GetComponent<ScenarioDetailModal>();
        if (existingController != null)
        {
            EnsureSerializedTrainingChoiceButtons(scene, existingController);
            return;
        }

        Canvas canvas = hud.GetComponent<Canvas>();
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/Font/Pretendard-Medium SDF.asset");

        GameObject modalRoot = UiObject(ModalName, canvas.transform);
        SetRect(modalRoot, Vector2.zero, new Vector2(6000f, 4000f));
        Canvas modalCanvas = modalRoot.AddComponent<Canvas>();
        modalCanvas.overrideSorting = true;
        modalCanvas.sortingOrder = 1000;
        modalRoot.AddComponent<GraphicRaycaster>();
        Image overlay = modalRoot.AddComponent<Image>();
        overlay.color = new Color(0.02f, 0.035f, 0.05f, 0.68f);
        overlay.raycastTarget = true;

        GameObject panel = UiObject("Modal Panel", modalRoot.transform);
        SetRect(panel, Vector2.zero, new Vector2(1050f, 520f));
        RoundedRectangleGraphic panelGraphic = panel.AddComponent<RoundedRectangleGraphic>();
        panelGraphic.color = new Color(0.86f, 0.9f, 0.92f, 0.94f);
        panelGraphic.CornerRadius = 42f;
        Shadow shadow = panel.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.45f);
        shadow.effectDistance = new Vector2(0f, -12f);

        GameObject numberPlateObject = UiObject("Scenario Number Plate", panel.transform);
        SetRect(numberPlateObject, new Vector2(0f, 205f), new Vector2(110f, 80f));
        Image numberPlate = numberPlateObject.AddComponent<Image>();
        numberPlate.color = new Color(0.48f, 0.52f, 0.55f, 1f);
        numberPlate.raycastTarget = false;
        TMP_Text number = Text("Scenario Number", numberPlateObject.transform, font, "1", 36f,
            FontStyles.Bold, TextAlignmentOptions.Center, Vector2.zero, new Vector2(110f, 80f));

        TMP_Text title = Text("Scenario Title", panel.transform, font,
            "화학물질 화재 발생 시\n소화 약제 선택 및 진압 훈련", 27f, FontStyles.Bold,
            TextAlignmentOptions.Center, new Vector2(0f, 110f), new Vector2(760f, 90f));
        TMP_Text description = Text("Scenario Description", panel.transform, font,
            "시나리오 카드를 선택하면 상세 설명이 표시됩니다.", 22f, FontStyles.Normal,
            TextAlignmentOptions.TopLeft, new Vector2(0f, -5f), new Vector2(760f, 130f));

        Button trainingButton = Button("Training Select Button", panel.transform, font,
            "훈련 선택", new Vector2(-205f, -190f), new Vector2(330f, 72f));
        Button backButton = Button("Back Button", panel.transform, font,
            "돌아가기", new Vector2(205f, -190f), new Vector2(330f, 72f));
        TMP_Text trainingLabel = trainingButton.GetComponentInChildren<TMP_Text>();

        ScenarioDetailModal controller = hud.gameObject.AddComponent<ScenarioDetailModal>();
        SerializedObject serialized = new(controller);
        serialized.FindProperty("modalRoot").objectReferenceValue = modalRoot;
        serialized.FindProperty("numberText").objectReferenceValue = number;
        serialized.FindProperty("titleText").objectReferenceValue = title;
        serialized.FindProperty("descriptionText").objectReferenceValue = description;
        serialized.FindProperty("trainingButtonText").objectReferenceValue = trainingLabel;
        serialized.FindProperty("trainingButton").objectReferenceValue = trainingButton;
        serialized.FindProperty("backButton").objectReferenceValue = backButton;

        Button incompletePpeButton = CreateTrainingChoiceButton(trainingButton, "Incomplete PPE Scenario Button",
            "PPE 불완전 착용 시나리오", new Vector2(0f, -105f));
        Button standardTrainingButton = CreateTrainingChoiceButton(trainingButton, "Standard Training Scenario Button",
            "정상 교육 시나리오", new Vector2(0f, -195f));
        Button trainingChoiceBackButton = CreateTrainingChoiceButton(backButton, "Training Choice Back Button",
            "돌아가기", new Vector2(0f, -285f));
        serialized.FindProperty("trainingChoiceTitle").stringValue = "훈련 시나리오 선택";
        serialized.FindProperty("trainingChoiceDescription").stringValue = "진행할 교육 시나리오를 선택해 주세요.";
        serialized.FindProperty("incompletePpeButton").objectReferenceValue = incompletePpeButton;
        serialized.FindProperty("standardTrainingButton").objectReferenceValue = standardTrainingButton;
        serialized.FindProperty("trainingChoiceBackButton").objectReferenceValue = trainingChoiceBackButton;

        string[] titles =
        {
            "화학물질 화재 발생 시\n소화 약제 선택 및 진압 훈련",
            "밀폐공간 작업 전\n개인보호구 선택 및 착용 훈련",
            "비상 상황 발생 시\n대피 절차 및 보고 훈련"
        };
        string[] descriptions =
        {
            "화학물질 화재 발생 시 소화 약제 선택 및 진압 훈련\n특정 유기용제나 금속성(물과 반응하는) 화학물질 보관소에서 화재가 발생한 상황.\n불이 났을 때 무조건 물을 뿌리는 것이 아니라, 화학물질 종류에 맞는 소화기(CO2, 분말, 팽창질석 등)를 정확히 선택하여 초기 진압을 체험합니다.",
            "작업 환경의 유해 요인을 확인하고 필요한 개인보호구를 선택합니다.\n보호구의 손상 여부와 밀착 상태를 점검한 뒤 올바른 순서로 착용합니다.",
            "경보와 현장 위험 요소를 확인하고 지정된 대피 경로로 이동합니다.\n안전 구역 도착 후 인원을 확인하고 비상 연락 체계에 따라 상황을 보고합니다."
        };

        SerializedProperty scenarios = serialized.FindProperty("scenarios");
        scenarios.arraySize = 3;
        Button[] allButtons = canvas.GetComponentsInChildren<Button>(true)
            .Where(button => !button.transform.IsChildOf(modalRoot.transform)).ToArray();
        for (int i = 0; i < scenarios.arraySize; i++)
        {
            SerializedProperty item = scenarios.GetArrayElementAtIndex(i);
            List<Button> matches = allButtons.Where(button => MatchesScenario(button.name, i + 1)).ToList();
            SerializedProperty buttons = item.FindPropertyRelative("selectionButtons");
            buttons.arraySize = matches.Count;
            for (int j = 0; j < matches.Count; j++)
                buttons.GetArrayElementAtIndex(j).objectReferenceValue = matches[j];
            item.FindPropertyRelative("number").stringValue = (i + 1).ToString();
            item.FindPropertyRelative("title").stringValue = titles[i];
            item.FindPropertyRelative("description").stringValue = descriptions[i];
            item.FindPropertyRelative("trainingButtonLabel").stringValue = "교육 선택";
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();

        modalRoot.SetActive(false);
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Created and serialized Scenario Detail Modal. Inspector values are now authoritative.", controller);
    }

    private static void EnsureSerializedTrainingChoiceButtons(Scene scene, ScenarioDetailModal controller)
    {
        SerializedObject serialized = new(controller);
        Button trainingButton = serialized.FindProperty("trainingButton").objectReferenceValue as Button;
        Button backButton = serialized.FindProperty("backButton").objectReferenceValue as Button;
        if (trainingButton == null || backButton == null)
            return;

        Transform parent = trainingButton.transform.parent;
        Button incompletePpeButton = FindButton(parent, "Incomplete PPE Scenario Button");
        Button standardTrainingButton = FindButton(parent, "Standard Training Scenario Button");
        Button trainingChoiceBackButton = FindButton(parent, "Training Choice Back Button");
        bool changed = false;

        if (incompletePpeButton == null)
        {
            incompletePpeButton = CreateTrainingChoiceButton(trainingButton, "Incomplete PPE Scenario Button",
                "PPE 불완전 착용 시나리오", new Vector2(0f, -105f));
            changed = true;
        }
        if (standardTrainingButton == null)
        {
            standardTrainingButton = CreateTrainingChoiceButton(trainingButton, "Standard Training Scenario Button",
                "정상 교육 시나리오", new Vector2(0f, -195f));
            changed = true;
        }
        if (trainingChoiceBackButton == null)
        {
            trainingChoiceBackButton = CreateTrainingChoiceButton(backButton, "Training Choice Back Button",
                "돌아가기", new Vector2(0f, -285f));
            changed = true;
        }

        SerializedProperty title = serialized.FindProperty("trainingChoiceTitle");
        SerializedProperty description = serialized.FindProperty("trainingChoiceDescription");
        if (string.IsNullOrEmpty(title.stringValue))
        {
            title.stringValue = "훈련 시나리오 선택";
            changed = true;
        }
        if (string.IsNullOrEmpty(description.stringValue))
        {
            description.stringValue = "진행할 교육 시나리오를 선택해 주세요.";
            changed = true;
        }

        changed |= AssignObjectReference(serialized, "incompletePpeButton", incompletePpeButton);
        changed |= AssignObjectReference(serialized, "standardTrainingButton", standardTrainingButton);
        changed |= AssignObjectReference(serialized, "trainingChoiceBackButton", trainingChoiceBackButton);
        TMP_Text typographySource = serialized.FindProperty("trainingButtonText").objectReferenceValue as TMP_Text;
        changed |= MatchFixedButtonTypography(typographySource, incompletePpeButton);
        changed |= MatchFixedButtonTypography(typographySource, standardTrainingButton);
        changed |= MatchFixedButtonTypography(typographySource, trainingChoiceBackButton);
        if (!changed)
            return;

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Migrated Scenario Detail Modal to serialized, scene-authored training choice buttons.", controller);
    }

    private static Button CreateTrainingChoiceButton(Button template, string objectName, string label, Vector2 position)
    {
        Button button = Object.Instantiate(template, template.transform.parent);
        button.name = objectName;
        button.onClick.RemoveAllListeners();
        SetRect(button.gameObject, position, new Vector2(760f, 72f));
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            text.text = label;
            text.enableAutoSizing = false;
        }
        button.gameObject.SetActive(false);
        return button;
    }

    private static Button FindButton(Transform parent, string objectName)
    {
        Transform child = parent.Find(objectName);
        return child != null ? child.GetComponent<Button>() : null;
    }

    private static bool AssignObjectReference(SerializedObject serialized, string propertyName, Object value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property.objectReferenceValue == value)
            return false;
        property.objectReferenceValue = value;
        return true;
    }

    private static bool MatchFixedButtonTypography(TMP_Text source, Button targetButton)
    {
        TMP_Text target = targetButton != null ? targetButton.GetComponentInChildren<TMP_Text>(true) : null;
        if (source == null || target == null)
            return false;
        if (!target.enableAutoSizing && Mathf.Approximately(target.fontSize, source.fontSize))
            return false;

        Undo.RecordObject(target, "Match serialized training button typography");
        target.enableAutoSizing = false;
        target.fontSize = source.fontSize;
        EditorUtility.SetDirty(target);
        return true;
    }

    private static bool AddQuestControllerInteractors(Scene scene)
    {
        Transform xrOrigin = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
            .FirstOrDefault(candidate => candidate.name == "XR Origin (VR)");
        Transform cameraOffset = xrOrigin != null ? xrOrigin.Find("Camera Offset") : null;
        if (cameraOffset == null)
            return false;

        bool changed = false;
        if (xrOrigin.GetComponent<XRInteractionManager>() == null)
        {
            xrOrigin.gameObject.AddComponent<XRInteractionManager>();
            changed = true;
        }

        InputActionManager actionManager = xrOrigin.GetComponent<InputActionManager>();
        if (actionManager == null)
        {
            actionManager = xrOrigin.gameObject.AddComponent<InputActionManager>();
            InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/XRI Default Input Actions.inputactions");
            SerializedObject serializedManager = new(actionManager);
            SerializedProperty actionAssets = serializedManager.FindProperty("m_ActionAssets");
            actionAssets.arraySize = actions != null ? 1 : 0;
            if (actions != null)
                actionAssets.GetArrayElementAtIndex(0).objectReferenceValue = actions;
            serializedManager.ApplyModifiedPropertiesWithoutUndo();
            changed = true;
        }

        // Controller objects are authored directly in the scene. Do not create
        // standalone interactors during script reloads or overwrite scene edits.
        return changed;
    }

    private static bool AddInteractor(Transform parent, string objectName, string prefabPath)
    {
        if (parent.Find(objectName) != null)
            return false;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"Quest controller interactor prefab was not found: {prefabPath}");
            return false;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = objectName;
        instance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        instance.transform.localScale = Vector3.one;
        return true;
    }

    private static bool MatchesScenario(string objectName, int number)
    {
        return objectName == $"Scenario Card {number}"
            || objectName.EndsWith($"_Card_{number}");
    }

    private static GameObject UiObject(string name, Transform parent)
    {
        GameObject gameObject = new(name, typeof(RectTransform));
        gameObject.layer = 5;
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static void SetRect(GameObject gameObject, Vector2 position, Vector2 size)
    {
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static TMP_Text Text(string name, Transform parent, TMP_FontAsset font, string value,
        float size, FontStyles style, TextAlignmentOptions alignment, Vector2 position, Vector2 dimensions)
    {
        GameObject gameObject = UiObject(name, parent);
        SetRect(gameObject, position, dimensions);
        TextMeshProUGUI text = gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = new Color(0.04f, 0.055f, 0.065f, 1f);
        text.textWrappingMode = TextWrappingModes.Normal;
        text.raycastTarget = false;
        return text;
    }

    private static Button Button(string name, Transform parent, TMP_FontAsset font, string label,
        Vector2 position, Vector2 dimensions)
    {
        GameObject gameObject = UiObject(name, parent);
        SetRect(gameObject, position, dimensions);
        Image image = gameObject.AddComponent<Image>();
        image.color = new Color(0.43f, 0.46f, 0.48f, 1f);
        Button button = gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.84f, 0.84f, 0.84f, 1f);
        colors.pressedColor = new Color(0.62f, 0.62f, 0.62f, 1f);
        colors.selectedColor = Color.white;
        button.colors = colors;
        Text(name + " Label", gameObject.transform, font, label, 24f, FontStyles.Bold,
            TextAlignmentOptions.Center, Vector2.zero, dimensions);
        return button;
    }
}
