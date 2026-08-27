using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// Adds the imported XRI Device Simulator as an explicitly enabled test object.
/// The simulator is disabled by default so it cannot affect headset sessions.
/// </summary>
public static class RightControllerTestSimulatorSetup
{
    private const string TargetScenePath = "Assets/Scenes/3_PPE_Room_HandTest_scale_0.unity";
    private const string SimulatorGateName = "Game View XR Test Input Gate";
    private const string SimulatorRootName = "Right Controller Test Simulator";
    private const string SimulatorPrefabPath =
        "Assets/Samples/XR Interaction Toolkit/3.4.1/XR Device Simulator/XR Device Simulator.prefab";
    private const string SimulatorGateScriptPath = "Assets/Scripts/PhysicalHmdSimulatorGate.cs";
    private const string ClickPanelBindingsScriptPath = "Assets/Scripts/RightControllerClickPanelBindings.cs";
    private const string ClickPanelName = "Right Controller Click Panel";
    private const string ProjectInputActionsPath = "Assets/InputSystem_Actions.inputactions";
    private const string UiActionMapName = "UI";
    private const string MousePointActionName = "Point";
    private const string MouseClickActionName = "Click";

    [MenuItem("Tools/XR/Add Right Controller Test Simulator")]
    private static void AddToHandTestScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != TargetScenePath)
        {
            Debug.LogError(
                $"Open only '{TargetScenePath}' before adding the right controller test simulator.");
            return;
        }

        GameObject simulatorGate = FindRootObject(scene, SimulatorGateName);
        if (simulatorGate == null)
        {
            simulatorGate = new GameObject(SimulatorGateName);
            Undo.RegisterCreatedObjectUndo(simulatorGate, "Add Game View XR Test Input Gate");
        }

        GameObject simulatorRoot = FindSceneObject(scene, SimulatorRootName);
        if (simulatorRoot == null)
        {
            GameObject simulatorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SimulatorPrefabPath);
            if (simulatorPrefab == null)
            {
                Debug.LogError(
                    "XR Device Simulator sample prefab was not found. Import XR Interaction Toolkit > Samples > XR Device Simulator first.");
                return;
            }

            simulatorRoot = (GameObject)PrefabUtility.InstantiatePrefab(simulatorPrefab, scene);
            Undo.RegisterCreatedObjectUndo(simulatorRoot, "Add Right Controller Test Simulator");
            simulatorRoot.name = SimulatorRootName;
        }

        Undo.SetTransformParent(simulatorRoot.transform, simulatorGate.transform, "Configure Game View XR Test Simulator");
        simulatorRoot.SetActive(false);

        MonoScript gateScript = AssetDatabase.LoadAssetAtPath<MonoScript>(SimulatorGateScriptPath);
        System.Type gateType = gateScript != null ? gateScript.GetClass() : null;
        if (gateType == null)
        {
            Debug.LogError(
                "PhysicalHmdSimulatorGate is not compiled yet. Wait for Unity script compilation to finish, then run this command again.");
            return;
        }

        Component gate = simulatorGate.GetComponent(gateType);
        if (gate == null)
            gate = Undo.AddComponent(simulatorGate, gateType);

        SerializedObject serializedGate = new(gate);
        serializedGate.FindProperty("m_SimulatorRoot").objectReferenceValue = simulatorRoot;
        serializedGate.FindProperty("m_EnableSimulatorWhenNoPhysicalHmd").boolValue = true;
        serializedGate.ApplyModifiedPropertiesWithoutUndo();

        if (!ConfigureUnifiedMouseInput(scene, gate))
            return;

        DisableSampleSimulatorUi(simulatorRoot);
        ConfigureClickPanel(simulatorRoot);

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = simulatorGate;
        Debug.Log(
            $"Configured '{SimulatorGateName}'. At Play start it enables '{SimulatorRootName}' only when no physical XRHMD exists. Disable the gate GameObject to turn off the automatic test path entirely, then save the scene when you want to keep it.",
            simulatorGate);
    }

    [MenuItem("Tools/XR/Configure Unified Game View Mouse Input")]
    private static void ConfigureUnifiedMouseInput()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != TargetScenePath)
        {
            Debug.LogError(
                $"Open only '{TargetScenePath}' before configuring Game View mouse input.");
            return;
        }

        GameObject simulatorGate = FindRootObject(scene, SimulatorGateName);
        if (simulatorGate == null)
        {
            Debug.LogError(
                $"'{SimulatorGateName}' is missing. Run Tools > XR > Add Right Controller Test Simulator first.");
            return;
        }

        PhysicalHmdSimulatorGate gate = simulatorGate.GetComponent<PhysicalHmdSimulatorGate>();
        if (gate == null || !ConfigureUnifiedMouseInput(scene, gate))
            return;

        GameObject simulatorRoot = FindSceneObject(scene, SimulatorRootName);
        if (simulatorRoot == null)
        {
            Debug.LogError($"'{SimulatorRootName}' is missing.", simulatorGate);
            return;
        }

        ConfigureClickPanel(simulatorRoot);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = simulatorGate;
        Debug.Log(
            "Configured Game View mouse Point/Click input for all scene-authored UI Buttons. " +
            "The physical-HMD path continues to use the existing XR interactors and UI Press actions.",
            simulatorGate);
    }

    [MenuItem("Tools/XR/Validate Unified Game View Mouse Input")]
    private static void ValidateUnifiedMouseInput()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != TargetScenePath)
        {
            Debug.LogError(
                $"Open only '{TargetScenePath}' before validating Game View mouse input.");
            return;
        }

        List<string> failures = new();
        PhysicalHmdSimulatorGate gate = FindSceneComponent<PhysicalHmdSimulatorGate>(scene);
        XRUIInputModule uiModule = FindSceneComponent<XRUIInputModule>(scene);
        NearFarInteractor mouseSelectInteractor = FindRightNearFarInteractor(scene);
        InputActionReference point = FindActionReference(
            ProjectInputActionsPath,
            UiActionMapName,
            MousePointActionName);
        InputActionReference click = FindActionReference(
            ProjectInputActionsPath,
            UiActionMapName,
            MouseClickActionName);

        if (gate == null)
        {
            failures.Add($"Missing {nameof(PhysicalHmdSimulatorGate)}.");
        }
        else
        {
            SerializedObject serializedGate = new(gate);
            if (serializedGate.FindProperty("m_XrUiInputModule")?.objectReferenceValue != uiModule)
                failures.Add("PhysicalHmdSimulatorGate has the wrong XRUIInputModule reference.");
            if (serializedGate.FindProperty("m_MousePointAction")?.objectReferenceValue != point)
                failures.Add("PhysicalHmdSimulatorGate has the wrong UI/Point reference.");
            if (serializedGate.FindProperty("m_MouseLeftClickAction")?.objectReferenceValue != click)
                failures.Add("PhysicalHmdSimulatorGate has the wrong UI/Click reference.");
            if (serializedGate.FindProperty("m_GameViewMouseSelectInteractor")?.objectReferenceValue != mouseSelectInteractor)
                failures.Add("PhysicalHmdSimulatorGate has the wrong right-hand Near-Far Interactor reference.");
        }

        if (uiModule == null)
            failures.Add("Missing XRUIInputModule.");
        if (mouseSelectInteractor == null)
            failures.Add("Missing the authored right-hand Near-Far Interactor.");
        if (point == null)
            failures.Add($"Missing {UiActionMapName}/{MousePointActionName} in '{ProjectInputActionsPath}'.");
        if (click == null)
            failures.Add($"Missing {UiActionMapName}/{MouseClickActionName} in '{ProjectInputActionsPath}'.");

        foreach (Canvas canvas in FindSceneComponents<Canvas>(scene))
        {
            if (OwnsButton(canvas) && canvas.GetComponent<GraphicRaycaster>() == null)
                failures.Add($"Button canvas '{GetPath(canvas.transform)}' has no GraphicRaycaster for mouse input.");
        }

        List<ScenarioDetailModal> modals = FindSceneComponents<ScenarioDetailModal>(scene);
        if (modals.Count == 0)
        {
            failures.Add("Missing ScenarioDetailModal.");
        }
        else
        {
            foreach (ScenarioDetailModal modal in modals)
            {
                if (new SerializedObject(modal).FindProperty("enableMousePhysicsFallback")?.boolValue ?? false)
                {
                    failures.Add(
                        $"ScenarioDetailModal '{GetPath(modal.transform)}' still has its legacy mouse physics fallback enabled.");
                }
            }
        }

        ValidateClickPanel(scene, failures);

        if (failures.Count > 0)
        {
            foreach (string failure in failures)
                Debug.LogError(failure, gate);
            return;
        }

        Debug.Log(
            "Unified Game View mouse input validation passed: serialized UI Point/Click references, " +
            "Button GraphicRaycasters, and duplicate-fallback isolation are correct.",
            gate);
    }

    private static bool ConfigureUnifiedMouseInput(Scene scene, Component gate)
    {
        XRUIInputModule uiModule = FindSceneComponent<XRUIInputModule>(scene);
        NearFarInteractor mouseSelectInteractor = FindRightNearFarInteractor(scene);
        InputActionReference point = FindActionReference(
            ProjectInputActionsPath,
            UiActionMapName,
            MousePointActionName);
        InputActionReference click = FindActionReference(
            ProjectInputActionsPath,
            UiActionMapName,
            MouseClickActionName);

        if (uiModule == null || mouseSelectInteractor == null || point == null || click == null)
        {
            Debug.LogError(
                "Unified Game View mouse input requires one XRUIInputModule, the right-hand Near-Far Interactor, and the project UI/Point and UI/Click InputActionReferences.",
                gate);
            return false;
        }

        Undo.RecordObject(gate, "Configure Unified Game View Mouse Input");
        SerializedObject serializedGate = new(gate);
        serializedGate.FindProperty("m_XrUiInputModule").objectReferenceValue = uiModule;
        serializedGate.FindProperty("m_MousePointAction").objectReferenceValue = point;
        serializedGate.FindProperty("m_MouseLeftClickAction").objectReferenceValue = click;
        serializedGate.FindProperty("m_GameViewMouseSelectInteractor").objectReferenceValue = mouseSelectInteractor;
        serializedGate.ApplyModifiedPropertiesWithoutUndo();

        EnsureButtonCanvasMouseRaycasters(scene);
        DisableLegacyScenarioMouseFallback(scene);
        return true;
    }

    private static void EnsureButtonCanvasMouseRaycasters(Scene scene)
    {
        foreach (Canvas canvas in FindSceneComponents<Canvas>(scene))
        {
            if (!OwnsButton(canvas) || canvas.GetComponent<GraphicRaycaster>() != null)
                continue;

            Undo.AddComponent<GraphicRaycaster>(canvas.gameObject);
            Debug.Log(
                $"Added GraphicRaycaster to Button canvas '{GetPath(canvas.transform)}' for Game View mouse input.",
                canvas);
        }
    }

    private static bool OwnsButton(Canvas canvas)
    {
        foreach (Button button in canvas.GetComponentsInChildren<Button>(true))
        {
            Canvas[] parentCanvases = button.GetComponentsInParent<Canvas>(true);
            if (parentCanvases.Length > 0 && parentCanvases[0] == canvas)
                return true;
        }

        return false;
    }

    private static void DisableLegacyScenarioMouseFallback(Scene scene)
    {
        foreach (ScenarioDetailModal modal in FindSceneComponents<ScenarioDetailModal>(scene))
        {
            SerializedObject serializedModal = new(modal);
            SerializedProperty fallback = serializedModal.FindProperty("enableMousePhysicsFallback");
            if (fallback == null || !fallback.boolValue)
                continue;

            Undo.RecordObject(modal, "Disable Legacy Scenario Mouse Fallback");
            fallback.boolValue = false;
            serializedModal.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void ValidateClickPanel(Scene scene, List<string> failures)
    {
        GameObject simulatorRoot = FindSceneObject(scene, SimulatorRootName);
        Transform panel = simulatorRoot != null
            ? FindChild(simulatorRoot.transform, ClickPanelName)
            : null;
        if (panel == null)
        {
            failures.Add($"Missing '{ClickPanelName}'.");
            return;
        }

        OnScreenButton[] buttons = panel.GetComponentsInChildren<OnScreenButton>(true);
        if (buttons.Length != 1
            || buttons[0].gameObject.name != "StickClick"
            || buttons[0].controlPath != "<Gamepad>/leftStickPress")
        {
            failures.Add("Right Controller Click Panel must contain only the StickClick on-screen button.");
        }

        if (panel.GetComponentInChildren<OnScreenStick>(true) != null)
            failures.Add("Right Controller Click Panel still contains the removed Teleport Stick.");
    }

    private static InputActionReference FindActionReference(
        string assetPath,
        string actionMapName,
        string actionName)
    {
        InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);
        InputAction action = asset?.FindActionMap(actionMapName)?.FindAction(actionName);
        if (action == null)
            return null;

        foreach (Object candidate in AssetDatabase.LoadAllAssetsAtPath(assetPath))
        {
            if (candidate is InputActionReference reference
                && reference.action != null
                && reference.action.id == action.id)
                return reference;
        }

        return null;
    }

    private static NearFarInteractor FindRightNearFarInteractor(Scene scene)
    {
        foreach (PPEControllerTeleportModeManager manager in FindSceneComponents<PPEControllerTeleportModeManager>(scene))
        {
            if (manager.gameObject.name != "Right Controller")
                continue;

            return new SerializedObject(manager)
                .FindProperty("m_NearFarInteractor")
                ?.objectReferenceValue as NearFarInteractor;
        }

        return null;
    }

    private static T FindSceneComponent<T>(Scene scene) where T : Component
    {
        foreach (T component in FindSceneComponents<T>(scene))
            return component;

        return null;
    }

    private static List<T> FindSceneComponents<T>(Scene scene) where T : Component
    {
        List<T> results = new();
        foreach (GameObject root in scene.GetRootGameObjects())
            results.AddRange(root.GetComponentsInChildren<T>(true));
        return results;
    }

    private static string GetPath(Transform target)
    {
        string path = target.name;
        while (target.parent != null)
        {
            target = target.parent;
            path = target.name + "/" + path;
        }

        return path;
    }

    private static GameObject FindRootObject(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == objectName)
                return root;
        }

        return null;
    }

    private static GameObject FindSceneObject(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name == objectName)
                    return candidate.gameObject;
            }
        }

        return null;
    }

    private static void ConfigureClickPanel(GameObject simulatorRoot)
    {
        MonoScript bindingsScript = AssetDatabase.LoadAssetAtPath<MonoScript>(ClickPanelBindingsScriptPath);
        System.Type bindingsType = bindingsScript != null ? bindingsScript.GetClass() : null;
        if (bindingsType == null)
        {
            Debug.LogError(
                "RightControllerClickPanelBindings is not compiled yet. Wait for Unity script compilation to finish, then run this command again.");
            return;
        }

        Component bindings = simulatorRoot.GetComponent(bindingsType);
        if (bindings == null)
            bindings = Undo.AddComponent(simulatorRoot, bindingsType);

        SerializedObject serializedBindings = new(bindings);
        serializedBindings.FindProperty("m_Simulator").objectReferenceValue = simulatorRoot.GetComponent("XRDeviceSimulator");
        serializedBindings.ApplyModifiedPropertiesWithoutUndo();

        Transform existingPanel = FindChild(simulatorRoot.transform, ClickPanelName);
        if (existingPanel != null)
        {
            SimplifyClickPanel(existingPanel);
            return;
        }

        GameObject panel = CreateUiObject(ClickPanelName, simulatorRoot.transform);
        Undo.RegisterCreatedObjectUndo(panel, "Add Right Controller Click Panel");

        Canvas canvas = panel.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2;
        CanvasScaler scaler = panel.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 1f;
        panel.AddComponent<GraphicRaycaster>();

        GameObject background = CreateUiObject("Panel", panel.transform);
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(1f, 0f);
        backgroundRect.anchorMax = new Vector2(1f, 0f);
        backgroundRect.pivot = new Vector2(1f, 0f);
        backgroundRect.anchoredPosition = new Vector2(-20f, 20f);
        backgroundRect.sizeDelta = new Vector2(250f, 130f);
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(0.06f, 0.09f, 0.14f, 0.94f);

        CreateLabel("Title", "RIGHT CONTROLLER TEST", background.transform, new Vector2(0f, -12f), new Vector2(230f, 30f), 16);
        CreateLabel("Hint", "Mouse handles scene actions", background.transform, new Vector2(0f, -39f), new Vector2(230f, 22f), 12);
        CreateOnScreenButton("StickClick", "STICK CLICK", "<Gamepad>/leftStickPress", background.transform, new Vector2(0f, -78f));
    }

    private static void DisableSampleSimulatorUi(GameObject simulatorRoot)
    {
        Component simulator = simulatorRoot.GetComponent("XRDeviceSimulator");
        if (simulator == null)
        {
            Debug.LogError(
                "Right controller test simulator is missing its XRDeviceSimulator component.",
                simulatorRoot);
            return;
        }

        SerializedObject serializedSimulator = new(simulator);
        SerializedProperty sampleUi = serializedSimulator.FindProperty("m_DeviceSimulatorUI");
        if (sampleUi == null)
        {
            Debug.LogError(
                "XRDeviceSimulator no longer exposes m_DeviceSimulatorUI; the test setup was not changed.",
                simulatorRoot);
            return;
        }

        if (sampleUi.objectReferenceValue == null)
            return;

        Undo.RecordObject(simulator, "Disable Sample XR Device Simulator UI");
        sampleUi.objectReferenceValue = null;
        serializedSimulator.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(simulator);

        Debug.Log(
            "Disabled the sample XR Device Simulator UI for this scene. The scene-owned Right Controller Click Panel is the only simulator UI, preventing the sample UI from indexing bindings changed by the click panel.",
            simulatorRoot);
    }

    private static void SimplifyClickPanel(Transform panelRoot)
    {
        string[] removedControls =
        {
            "ActivateRight",
            "Trigger",
            "Grip",
            "Primary",
            "Secondary",
            "Teleport Stick",
        };

        foreach (string controlName in removedControls)
        {
            Transform control = FindChild(panelRoot, controlName);
            if (control != null)
                Undo.DestroyObjectImmediate(control.gameObject);
        }

        Transform background = FindChild(panelRoot, "Panel");
        if (background == null)
        {
            Debug.LogError("Right Controller Click Panel is missing its authored Panel child.", panelRoot);
            return;
        }

        RectTransform backgroundRect = background as RectTransform;
        Undo.RecordObject(backgroundRect, "Simplify Right Controller Click Panel");
        backgroundRect.sizeDelta = new Vector2(250f, 130f);

        SetLabelText(background, "Title", "RIGHT CONTROLLER TEST");
        SetLabelText(background, "Hint", "Mouse handles scene actions");

        Transform stickClick = FindChild(background, "StickClick");
        if (stickClick == null)
        {
            CreateOnScreenButton(
                "StickClick",
                "STICK CLICK",
                "<Gamepad>/leftStickPress",
                background,
                new Vector2(0f, -78f));
            return;
        }

        RectTransform stickClickRect = stickClick as RectTransform;
        Undo.RecordObject(stickClickRect, "Position Stick Click Button");
        stickClickRect.anchoredPosition = new Vector2(0f, -78f);
    }

    private static void SetLabelText(Transform parent, string objectName, string value)
    {
        Transform labelTransform = FindChild(parent, objectName);
        Text label = labelTransform != null ? labelTransform.GetComponent<Text>() : null;
        if (label == null || label.text == value)
            return;

        Undo.RecordObject(label, "Update Controller Test Panel Label");
        label.text = value;
    }

    private static GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject result = new(objectName, typeof(RectTransform));
        result.transform.SetParent(parent, false);
        return result;
    }

    private static void CreateLabel(string objectName, string text, Transform parent, Vector2 anchoredPosition, Vector2 size, int fontSize)
    {
        GameObject label = CreateUiObject(objectName, parent);
        RectTransform rect = label.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Text uiText = label.AddComponent<Text>();
        uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        uiText.text = text;
        uiText.fontSize = fontSize;
        uiText.alignment = TextAnchor.MiddleCenter;
        uiText.color = Color.white;
        uiText.raycastTarget = false;
    }

    private static void CreateOnScreenButton(string objectName, string label, string controlPath, Transform parent, Vector2 anchoredPosition)
    {
        GameObject buttonObject = CreateUiObject(objectName, parent);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(220f, 36f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.16f, 0.34f, 0.56f, 1f);
        buttonObject.AddComponent<Button>();
        OnScreenButton onScreenButton = buttonObject.AddComponent<OnScreenButton>();
        SerializedObject serializedButton = new(onScreenButton);
        serializedButton.FindProperty("m_ControlPath").stringValue = controlPath;
        serializedButton.ApplyModifiedPropertiesWithoutUndo();

        CreateLabel("Label", label, buttonObject.transform, Vector2.zero, new Vector2(210f, 32f), 14);
    }

    private static Transform FindChild(Transform root, string objectName)
    {
        foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
        {
            if (candidate.name == objectName)
                return candidate;
        }

        return null;
    }
}
