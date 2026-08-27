using System;
using System.Collections.Generic;
using Prototype.Tyche.UI.Keyboard;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Samples.SpatialKeyboard;

/// <summary>
/// Explicit authoring commands for a project-owned Korean XRI Spatial Keyboard prefab.
/// Existing generated assets are preserved so Inspector-authored values remain authoritative.
/// </summary>
public static class HangulSpatialKeyboardBuilder
{
    const string SampleRoot = "Assets/Samples/XR Interaction Toolkit/3.4.1/Spatial Keyboard";
    const string SourceAlphaLayoutPath = SampleRoot + "/Layouts/LayoutAlphaNumeric.asset";
    const string SourceSymbolLayoutPath = SampleRoot + "/Layouts/LayoutSymbols.asset";
    const string SourcePrefabPath = SampleRoot + "/Prefabs/XRI Spatial Keyboard.prefab";
    const string KoreanFontPath = "Assets/Font/Pretendard-Medium SDF.asset";

    const string OutputRoot = "Assets/Prefabs/UI/Hangul Keyboard";
    const string FunctionsFolder = OutputRoot + "/Key Functions";
    const string LayoutsFolder = OutputRoot + "/Layouts";
    const string PrefabPath = OutputRoot + "/Hangul Spatial Keyboard.prefab";

    const string HangulLayoutPath = LayoutsFolder + "/LayoutHangul.asset";
    const string EnglishLayoutPath = LayoutsFolder + "/LayoutEnglish.asset";
    const string SymbolLayoutPath = LayoutsFolder + "/LayoutSymbols.asset";

    const string CharacterFunctionPath = FunctionsFolder + "/Hangul Character Key Function.asset";
    const string LanguageFunctionPath = FunctionsFolder + "/Hangul Language Toggle Key Function.asset";
    const string SymbolFunctionPath = FunctionsFolder + "/Hangul Symbol Toggle Key Function.asset";
    const string BackspaceFunctionPath = FunctionsFolder + "/Hangul Backspace Key Function.asset";

    static readonly string[] s_PhysicalLetters =
    {
        "q", "w", "e", "r", "t", "y", "u", "i", "o", "p",
        "a", "s", "d", "f", "g", "h", "j", "k", "l",
        "z", "x", "c", "v", "b", "n", "m",
    };

    static readonly string[] s_ShiftLetters =
    {
        "Q", "W", "E", "R", "T", "Y", "U", "I", "O", "P",
        "A", "S", "D", "F", "G", "H", "J", "K", "L",
        "Z", "X", "C", "V", "B", "N", "M",
    };

    static readonly string[] s_HangulLetters =
    {
        "ㅂ", "ㅈ", "ㄷ", "ㄱ", "ㅅ", "ㅛ", "ㅕ", "ㅑ", "ㅐ", "ㅔ",
        "ㅁ", "ㄴ", "ㅇ", "ㄹ", "ㅎ", "ㅗ", "ㅓ", "ㅏ", "ㅣ",
        "ㅋ", "ㅌ", "ㅊ", "ㅍ", "ㅠ", "ㅜ", "ㅡ",
    };

    static readonly Dictionary<int, string> s_ShiftLegends = new Dictionary<int, string>
    {
        [10] = "ㅃ",
        [11] = "ㅉ",
        [12] = "ㄸ",
        [13] = "ㄲ",
        [14] = "ㅆ",
        [18] = "ㅒ",
        [19] = "ㅖ",
    };

    [MenuItem("Tools/XR/Hangul Keyboard/Build Project Prefab")]
    public static void BuildProjectPrefab()
    {
        HangulComposerValidationHarness.Validate();
        EnsureFolder(OutputRoot);
        EnsureFolder(FunctionsFolder);
        EnsureFolder(LayoutsFolder);

        var characterFunction = LoadOrCreateFunction<HangulCharacterKeyFunction>(CharacterFunctionPath);
        var languageFunction = LoadOrCreateFunction<HangulLanguageToggleKeyFunction>(LanguageFunctionPath);
        var symbolFunction = LoadOrCreateFunction<HangulSymbolToggleKeyFunction>(SymbolFunctionPath);
        var backspaceFunction = LoadOrCreateFunction<HangulBackspaceKeyFunction>(BackspaceFunctionPath);

        var hangulLayout = LoadOrCreateLayout(
            HangulLayoutPath,
            SourceAlphaLayoutPath,
            layout => ConfigureHangulLayout(layout, characterFunction, languageFunction, symbolFunction));
        var englishLayout = LoadOrCreateLayout(
            EnglishLayoutPath,
            SourceAlphaLayoutPath,
            layout => ConfigureUtilityKeys(layout, languageFunction, symbolFunction, "기호"));
        var symbolLayout = LoadOrCreateLayout(
            SymbolLayoutPath,
            SourceSymbolLayoutPath,
            layout => ConfigureUtilityKeys(layout, languageFunction, symbolFunction, "문자"));

        ApplyCurrentHangulShiftMapping(hangulLayout);

        var existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (existingPrefab == null)
        {
            CreatePrefab(hangulLayout, englishLayout, symbolLayout, backspaceFunction);
            existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        }

        ApplyPrimaryShiftPresentationToProjectPrefab();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ValidateGeneratedAssets(false);
        Selection.activeObject = existingPrefab;
        Debug.Log($"[Hangul Keyboard] Project prefab ready: {PrefabPath}", existingPrefab);
    }

    [MenuItem("Tools/XR/Hangul Keyboard/Validate Project Prefab")]
    public static void ValidateProjectPrefab()
    {
        HangulComposerValidationHarness.Validate();
        ValidateGeneratedAssets(true);
    }

    [MenuItem("Tools/XR/Hangul Keyboard/Apply Current Shift Presentation")]
    public static void ApplyPrimaryShiftPresentationToProjectPrefab()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
            throw new InvalidOperationException($"Generated keyboard prefab was not found at '{PrefabPath}'.");

        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        var changed = false;
        try
        {
            foreach (var legend in root.GetComponentsInChildren<HangulShiftLegend>(true))
            {
                var serializedLegend = new SerializedObject(legend);
                var shiftTextProperty = serializedLegend.FindProperty("m_ShiftLegendText");
                var shiftText = shiftTextProperty != null
                    ? shiftTextProperty.objectReferenceValue as TMP_Text
                    : null;
                if (shiftText == null || !shiftText.gameObject.activeSelf)
                    continue;

                // The current UX replaces the primary key label while Shift is active.
                // Keep the legacy secondary label serialized but hidden for compatibility.
                shiftText.gameObject.SetActive(false);
                changed = true;
            }

            if (changed)
            {
                var saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out var success);
                if (!success || saved == null)
                    throw new InvalidOperationException($"Failed to save Shift presentation at '{PrefabPath}'.");
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        if (changed)
            Debug.Log("[Hangul Keyboard] Applied primary-label Shift presentation to the project prefab.", prefab);
    }

    [MenuItem("Tools/XR/Hangul Keyboard/Apply Current Shift Input Mapping")]
    public static void ApplyCurrentHangulShiftMapping()
    {
        var layout = AssetDatabase.LoadAssetAtPath<XRKeyboardConfig>(HangulLayoutPath);
        if (layout == null)
            throw new InvalidOperationException($"Hangul layout was not found at '{HangulLayoutPath}'.");

        ApplyCurrentHangulShiftMapping(layout);
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Tools/XR/Hangul Keyboard/Create Test Instance In Current Scene")]
    public static void CreateTestInstanceInCurrentScene()
    {
        BuildProjectPrefab();

        var scene = SceneManager.GetActiveScene();
        foreach (var root in scene.GetRootGameObjects())
        {
            var existing = root.GetComponentInChildren<HangulKeyboardController>(true);
            if (existing == null)
                continue;

            Selection.activeGameObject = existing.gameObject;
            Debug.Log("[Hangul Keyboard] The active scene already contains a Hangul keyboard instance.", existing);
            return;
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
            throw new InvalidOperationException($"Generated keyboard prefab was not found at '{PrefabPath}'.");

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.name = "Hangul Spatial Keyboard Test";
        Undo.RegisterCreatedObjectUndo(instance, "Create Hangul Spatial Keyboard Test");
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = instance;
        Debug.Log("[Hangul Keyboard] Test instance created without changing its authored prefab Transform. Position it in the Scene view.", instance);
    }

    [MenuItem("Tools/XR/Hangul Keyboard/Export Package To Desktop")]
    public static void ExportPackageToDesktop()
    {
        BuildProjectPrefab();

        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (string.IsNullOrWhiteSpace(desktopPath) || !System.IO.Directory.Exists(desktopPath))
            throw new InvalidOperationException("The current Windows Desktop folder could not be resolved.");

        var packageName = $"Tyche_Hangul_XRI_Keyboard_{DateTime.Now:yyyyMMdd_HHmmss}.unitypackage";
        var outputPath = System.IO.Path.Combine(desktopPath, packageName);
        var exportRoots = new[]
        {
            "Assets/Scripts/HangulKeyboard",
            "Assets/Editor/HangulComposerValidationHarness.cs",
            "Assets/Editor/HangulSpatialKeyboardBuilder.cs",
            OutputRoot,
            KoreanFontPath,
            SampleRoot,
            "Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/StarterAssets.asmdef",
            "Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/Scripts/RotationAxisLockGrabTransformer.cs",
            "Assets/Samples/XR Interaction Toolkit/3.4.1/Starter Assets/Scripts/XRPokeFollowAffordance.cs",
        };

        AssetDatabase.ExportPackage(
            exportRoots,
            outputPath,
            ExportPackageOptions.Recurse | ExportPackageOptions.IncludeDependencies);

        if (!System.IO.File.Exists(outputPath))
            throw new InvalidOperationException($"Unity did not create the package at '{outputPath}'.");

        var sizeMegabytes = new System.IO.FileInfo(outputPath).Length / (1024d * 1024d);
        Debug.Log($"[Hangul Keyboard] Exported reusable package: {outputPath} ({sizeMegabytes:0.##} MB)");
    }

    public static void BuildFromCommandLine()
    {
        try
        {
            BuildProjectPrefab();
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    static T LoadOrCreateFunction<T>(string assetPath) where T : ScriptableObject
    {
        var existing = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        if (existing != null)
            return existing;

        if (AssetDatabase.LoadMainAssetAtPath(assetPath) != null)
            throw new InvalidOperationException($"An asset of an unexpected type already exists at '{assetPath}'.");

        var function = ScriptableObject.CreateInstance<T>();
        function.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        AssetDatabase.CreateAsset(function, assetPath);
        return function;
    }

    static XRKeyboardConfig LoadOrCreateLayout(
        string assetPath,
        string sourcePath,
        Action<XRKeyboardConfig> configure)
    {
        var existing = AssetDatabase.LoadAssetAtPath<XRKeyboardConfig>(assetPath);
        if (existing != null)
            return existing;

        var source = AssetDatabase.LoadAssetAtPath<XRKeyboardConfig>(sourcePath);
        if (source == null)
            throw new InvalidOperationException($"XRI source layout was not found at '{sourcePath}'.");

        var layout = UnityEngine.Object.Instantiate(source);
        layout.name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        configure(layout);
        AssetDatabase.CreateAsset(layout, assetPath);
        EditorUtility.SetDirty(layout);
        return layout;
    }

    static void ConfigureHangulLayout(
        XRKeyboardConfig layout,
        HangulCharacterKeyFunction characterFunction,
        HangulLanguageToggleKeyFunction languageFunction,
        HangulSymbolToggleKeyFunction symbolFunction)
    {
        RequireMappingCount(layout);
        layout.defaultKeyFunction = characterFunction;

        for (var letterIndex = 0; letterIndex < s_PhysicalLetters.Length; ++letterIndex)
        {
            // The XRI layout inserts Shift between the home and bottom letter rows.
            var mappingIndex = letterIndex < 19 ? 10 + letterIndex : 11 + letterIndex;
            var mapping = layout.keyMappings[mappingIndex];
            mapping.character = s_PhysicalLetters[letterIndex];
            mapping.shiftCharacter = s_ShiftLetters[letterIndex];
            mapping.displayCharacter = s_HangulLetters[letterIndex];
            mapping.shiftDisplayCharacter = GetShiftDisplayCharacter(mappingIndex, s_HangulLetters[letterIndex]);
            mapping.displayIcon = null;
            mapping.shiftDisplayIcon = null;
            mapping.overrideDefaultKeyFunction = false;
            mapping.disabled = false;
        }

        ConfigureUtilityKeys(layout, languageFunction, symbolFunction, "기호");
    }

    static void ApplyCurrentHangulShiftMapping(XRKeyboardConfig layout)
    {
        RequireMappingCount(layout);
        var changed = false;
        for (var letterIndex = 0; letterIndex < s_PhysicalLetters.Length; ++letterIndex)
        {
            var mappingIndex = letterIndex < 19 ? 10 + letterIndex : 11 + letterIndex;
            var mapping = layout.keyMappings[mappingIndex];
            var expectedShiftInput = s_ShiftLetters[letterIndex];
            var expectedShiftDisplay = GetShiftDisplayCharacter(mappingIndex, s_HangulLetters[letterIndex]);
            if (mapping.shiftCharacter == expectedShiftInput
                && mapping.shiftDisplayCharacter == expectedShiftDisplay)
                continue;

            // Input remains the physical uppercase QWERTY key required by HangulComposer.
            // Only the authored key legend uses the resulting compatibility Jamo.
            mapping.shiftCharacter = expectedShiftInput;
            mapping.shiftDisplayCharacter = expectedShiftDisplay;
            changed = true;
        }

        if (!changed)
            return;

        EditorUtility.SetDirty(layout);
        Debug.Log("[Hangul Keyboard] Updated Shift input values and primary-label Shift display values.", layout);
    }

    static string GetShiftDisplayCharacter(int mappingIndex, string baseDisplayCharacter)
    {
        return s_ShiftLegends.TryGetValue(mappingIndex, out var shiftedDisplay)
            ? shiftedDisplay
            : baseDisplayCharacter;
    }

    static void ConfigureUtilityKeys(
        XRKeyboardConfig layout,
        HangulLanguageToggleKeyFunction languageFunction,
        HangulSymbolToggleKeyFunction symbolFunction,
        string symbolDisplay)
    {
        RequireMappingCount(layout);
        ConfigureActionMapping(layout.keyMappings[38], "\\lang", "한/영", languageFunction);
        ConfigureActionMapping(layout.keyMappings[39], "\\sym", symbolDisplay, symbolFunction);
    }

    static void ConfigureActionMapping(
        XRKeyboardConfig.KeyMapping mapping,
        string character,
        string display,
        KeyFunction function)
    {
        mapping.character = character;
        mapping.shiftCharacter = character;
        mapping.displayCharacter = display;
        mapping.shiftDisplayCharacter = display;
        mapping.displayIcon = null;
        mapping.shiftDisplayIcon = null;
        mapping.overrideDefaultKeyFunction = true;
        mapping.keyFunction = function;
        mapping.keyCode = KeyCode.None;
        mapping.disabled = false;
    }

    static void RequireMappingCount(XRKeyboardConfig layout)
    {
        if (layout.keyMappings == null || layout.keyMappings.Count != 42)
        {
            throw new InvalidOperationException(
                $"Expected the XRI full keyboard layout to contain 42 mappings, but '{layout.name}' contains " +
                $"{layout.keyMappings?.Count ?? 0}.");
        }
    }

    static void CreatePrefab(
        XRKeyboardConfig hangulLayout,
        XRKeyboardConfig englishLayout,
        XRKeyboardConfig symbolLayout,
        HangulBackspaceKeyFunction backspaceFunction)
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
        if (font == null)
            throw new InvalidOperationException($"Korean TMP font was not found at '{KoreanFontPath}'.");

        var root = PrefabUtility.LoadPrefabContents(SourcePrefabPath);
        if (root == null)
            throw new InvalidOperationException($"XRI source prefab was not found at '{SourcePrefabPath}'.");

        try
        {
            root.name = "Hangul Spatial Keyboard";
            var keyboard = root.GetComponentInChildren<XRKeyboard>(true);
            var layout = root.GetComponentInChildren<XRKeyboardLayout>(true);
            if (keyboard == null || layout == null)
                throw new InvalidOperationException("The XRI source prefab does not contain XRKeyboard and XRKeyboardLayout.");

            var controller = keyboard.GetComponent<HangulKeyboardController>();
            if (controller == null)
                controller = keyboard.gameObject.AddComponent<HangulKeyboardController>();
            controller.Configure(keyboard, layout, hangulLayout, englishLayout, symbolLayout, true, true);

            layout.defaultKeyMapping = hangulLayout;
            layout.activeKeyMapping = hangulLayout;

            foreach (var textComponent in root.GetComponentsInChildren<TMP_Text>(true))
                textComponent.font = font;

            var layoutKeys = layout.GetComponentsInChildren<XRKeyboardKey>(true);
            if (layoutKeys.Length < 42)
                throw new InvalidOperationException($"Expected at least 42 layout keys, but found {layoutKeys.Length}.");

            foreach (var legend in s_ShiftLegends)
                AddShiftLegend(layoutKeys[legend.Key], legend.Value, font);

            var assignedBackspace = false;
            foreach (var key in root.GetComponentsInChildren<XRKeyboardKey>(true))
            {
                if (key.keyCode != KeyCode.Backspace)
                    continue;

                key.keyFunction = backspaceFunction;
                assignedBackspace = true;
            }

            if (!assignedBackspace)
                throw new InvalidOperationException("The XRI source prefab's Backspace key could not be identified.");

            var savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out var success);
            if (!success || savedPrefab == null)
                throw new InvalidOperationException($"Failed to save the generated prefab at '{PrefabPath}'.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void AddShiftLegend(XRKeyboardKey key, string legend, TMP_FontAsset font)
    {
        var primaryText = key.textComponent;
        if (primaryText == null)
            throw new InvalidOperationException($"Keyboard key '{key.name}' has no primary TMP text component.");

        var legendObject = new GameObject("Shift Legend", typeof(RectTransform), typeof(CanvasRenderer));
        var legendRect = (RectTransform)legendObject.transform;
        legendRect.SetParent(key.transform, false);
        legendRect.anchorMin = new Vector2(0.56f, 0.54f);
        legendRect.anchorMax = new Vector2(0.92f, 0.92f);
        legendRect.offsetMin = Vector2.zero;
        legendRect.offsetMax = Vector2.zero;
        legendRect.localScale = Vector3.one;

        var legendText = legendObject.AddComponent<TextMeshProUGUI>();
        legendText.font = font;
        legendText.text = legend;
        legendText.fontSize = Mathf.Max(1f, primaryText.fontSize * 0.55f);
        legendText.alignment = TextAlignmentOptions.TopRight;
        legendText.raycastTarget = false;
        legendText.textWrappingMode = TextWrappingModes.NoWrap;

        var primaryNormal = primaryText.color;
        var primaryShifted = WithAlpha(primaryNormal, primaryNormal.a * 0.45f);
        var legendNormal = WithAlpha(primaryNormal, primaryNormal.a * 0.55f);
        var legendShifted = primaryNormal;
        legendText.color = legendNormal;

        var shiftLegend = key.gameObject.AddComponent<HangulShiftLegend>();
        shiftLegend.Configure(
            key,
            primaryText,
            legendText,
            legend,
            primaryNormal,
            primaryShifted,
            legendNormal,
            legendShifted);
    }

    static void ValidateGeneratedAssets(bool logSuccess)
    {
        var characterFunction = RequireAsset<HangulCharacterKeyFunction>(CharacterFunctionPath);
        var languageFunction = RequireAsset<HangulLanguageToggleKeyFunction>(LanguageFunctionPath);
        var symbolFunction = RequireAsset<HangulSymbolToggleKeyFunction>(SymbolFunctionPath);
        var backspaceFunction = RequireAsset<HangulBackspaceKeyFunction>(BackspaceFunctionPath);
        var hangulLayout = RequireAsset<XRKeyboardConfig>(HangulLayoutPath);
        var englishLayout = RequireAsset<XRKeyboardConfig>(EnglishLayoutPath);
        var symbolLayout = RequireAsset<XRKeyboardConfig>(SymbolLayoutPath);
        var font = RequireAsset<TMP_FontAsset>(KoreanFontPath);
        var prefab = RequireAsset<GameObject>(PrefabPath);

        RequireMappingCount(hangulLayout);
        RequireMappingCount(englishLayout);
        RequireMappingCount(symbolLayout);
        Require(hangulLayout.defaultKeyFunction == characterFunction,
            "The Hangul layout does not use the project Hangul character function by default.");

        for (var letterIndex = 0; letterIndex < s_PhysicalLetters.Length; ++letterIndex)
        {
            var mappingIndex = letterIndex < 19 ? 10 + letterIndex : 11 + letterIndex;
            var mapping = hangulLayout.keyMappings[mappingIndex];
            Require(mapping.character == s_PhysicalLetters[letterIndex],
                $"Hangul mapping {mappingIndex} has an unexpected physical character.");
            Require(mapping.shiftCharacter == s_ShiftLetters[letterIndex],
                $"Hangul mapping {mappingIndex} has an unexpected Shift character.");
            Require(mapping.displayCharacter == s_HangulLetters[letterIndex],
                $"Hangul mapping {mappingIndex} has an unexpected display character.");
            Require(mapping.shiftDisplayCharacter == GetShiftDisplayCharacter(mappingIndex, s_HangulLetters[letterIndex]),
                $"Hangul mapping {mappingIndex} has an unexpected Shift display character.");
        }

        ValidateUtilityKeys(hangulLayout, languageFunction, symbolFunction, "기호");
        ValidateUtilityKeys(englishLayout, languageFunction, symbolFunction, "기호");
        ValidateUtilityKeys(symbolLayout, languageFunction, symbolFunction, "문자");

        var root = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(prefab));
        try
        {
            var keyboard = root.GetComponentInChildren<XRKeyboard>(true);
            var layout = root.GetComponentInChildren<XRKeyboardLayout>(true);
            Require(keyboard != null, "The generated prefab has no XRKeyboard.");
            Require(layout != null, "The generated prefab has no XRKeyboardLayout.");

            var controller = keyboard.GetComponent<HangulKeyboardController>();
            Require(controller != null, "HangulKeyboardController is not on the XRKeyboard GameObject.");
            Require(layout.defaultKeyMapping == hangulLayout && layout.activeKeyMapping == hangulLayout,
                "The generated prefab is not serialized with Hangul as its default and active layout.");

            var controllerData = new SerializedObject(controller);
            Require(controllerData.FindProperty("m_Keyboard").objectReferenceValue == keyboard,
                "HangulKeyboardController has an invalid XRKeyboard reference.");
            Require(controllerData.FindProperty("m_KeyboardLayout").objectReferenceValue == layout,
                "HangulKeyboardController has an invalid XRKeyboardLayout reference.");
            Require(controllerData.FindProperty("m_HangulLayout").objectReferenceValue == hangulLayout,
                "HangulKeyboardController has an invalid Hangul layout reference.");
            Require(controllerData.FindProperty("m_EnglishLayout").objectReferenceValue == englishLayout,
                "HangulKeyboardController has an invalid English layout reference.");
            Require(controllerData.FindProperty("m_SymbolLayout").objectReferenceValue == symbolLayout,
                "HangulKeyboardController has an invalid symbol layout reference.");
            Require(controllerData.FindProperty("m_LogKeyPressesToConsole").boolValue,
                "HangulKeyboardController console key logging is disabled.");

            var legends = root.GetComponentsInChildren<HangulShiftLegend>(true);
            Require(legends.Length == s_ShiftLegends.Count,
                $"Expected {s_ShiftLegends.Count} Hangul Shift legends, but found {legends.Length}.");
            foreach (var legend in legends)
            {
                var serializedLegend = new SerializedObject(legend);
                var shiftTextProperty = serializedLegend.FindProperty("m_ShiftLegendText");
                var shiftText = shiftTextProperty != null
                    ? shiftTextProperty.objectReferenceValue as TMP_Text
                    : null;
                Require(shiftText != null,
                    $"Hangul Shift legend on '{legend.name}' has no serialized secondary label reference.");
                Require(!shiftText.gameObject.activeSelf,
                    $"Hangul Shift legend on '{legend.name}' still shows the legacy secondary glyph.");
            }

            foreach (var textComponent in root.GetComponentsInChildren<TMP_Text>(true))
            {
                Require(textComponent.font == font,
                    $"TMP text '{textComponent.name}' is not using the serialized Korean font.");
            }

            var foundBackspace = false;
            foreach (var key in root.GetComponentsInChildren<XRKeyboardKey>(true))
            {
                if (key.keyCode != KeyCode.Backspace)
                    continue;

                foundBackspace = true;
                Require(key.keyFunction == backspaceFunction,
                    "The Backspace key is not using the Hangul-aware Backspace function.");
            }

            Require(foundBackspace, "The generated prefab has no identifiable Backspace key.");

            // Exercise the serialized controller/XRI integration without saving changes to the prefab contents.
            keyboard.Clear();
            controller.CommitComposition();
            foreach (var physicalKey in "gksrmf")
                controller.ProcessCharacter(physicalKey.ToString());
            Require(keyboard.text == "한글", "The prefab controller failed to compose '한글' through XRKeyboard.");

            controller.ProcessBackspace();
            Require(keyboard.text == "한그", "The prefab controller failed staged Hangul Backspace.");
            controller.ProcessCharacter(" ");
            Require(keyboard.text == "한그 ", "The prefab controller failed to commit composition before Space.");

            controller.ToggleLanguage();
            Require(layout.activeKeyMapping == englishLayout,
                "The prefab controller failed to switch from Hangul to English.");
            controller.ToggleSymbols();
            Require(layout.activeKeyMapping == symbolLayout,
                "The prefab controller failed to switch to the symbol layout.");
            controller.ToggleSymbols();
            Require(layout.activeKeyMapping == englishLayout,
                "The prefab controller failed to return from symbols to the prior language.");
            controller.ToggleLanguage();
            Require(layout.activeKeyMapping == hangulLayout,
                "The prefab controller failed to switch back to Hangul.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        if (logSuccess)
            Debug.Log("[Hangul Keyboard] Generated asset validation passed.", prefab);
    }

    static void ValidateUtilityKeys(
        XRKeyboardConfig layout,
        HangulLanguageToggleKeyFunction languageFunction,
        HangulSymbolToggleKeyFunction symbolFunction,
        string expectedSymbolDisplay)
    {
        var languageMapping = layout.keyMappings[38];
        Require(!languageMapping.disabled && languageMapping.displayCharacter == "한/영"
            && languageMapping.keyFunction == languageFunction,
            $"Layout '{layout.name}' has an invalid 한/영 mapping.");

        var symbolMapping = layout.keyMappings[39];
        Require(!symbolMapping.disabled && symbolMapping.displayCharacter == expectedSymbolDisplay
            && symbolMapping.keyFunction == symbolFunction,
            $"Layout '{layout.name}' has an invalid symbol toggle mapping.");
    }

    static T RequireAsset<T>(string assetPath) where T : UnityEngine.Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        if (asset == null)
            throw new InvalidOperationException($"Required asset was not found at '{assetPath}'.");
        return asset;
    }

    static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
            return;

        var parentPath = System.IO.Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
        var folderName = System.IO.Path.GetFileName(folderPath);
        if (string.IsNullOrEmpty(parentPath) || string.IsNullOrEmpty(folderName))
            throw new InvalidOperationException($"Invalid Unity asset folder path: '{folderPath}'.");

        EnsureFolder(parentPath);
        AssetDatabase.CreateFolder(parentPath, folderName);
    }
}
