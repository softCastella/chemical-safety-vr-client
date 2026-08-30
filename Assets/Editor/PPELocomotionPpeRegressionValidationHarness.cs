using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public static class PPELocomotionPpeRegressionValidationHarness
{
    const string ScenePath =
        "Assets/Scenes/3_PPE_Room_3mode_loco.unity";

    const BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;

    [MenuItem("Tools/PPE/Validate Locomotion PPE Regressions")]
    public static void ValidateFromMenu()
    {
        try
        {
            Validate();
            EditorUtility.DisplayDialog(
                "PPE 회귀 검증 PASS",
                "테이프 조건·음성 1회, 방호복 중복 음성, 누출 안전대 차단, 체크리스트 초기화 검증을 통과했습니다.",
                "확인");
        }
        catch (Exception exception)
        {
            EditorUtility.DisplayDialog(
                "PPE 회귀 검증 FAIL",
                exception.Message,
                "확인");
            throw;
        }
    }

    public static void Validate()
    {
        Scene previewScene = default;
        List<string> failures = new();

        try
        {
            previewScene = EditorSceneManager.OpenPreviewScene(ScenePath);
            PPEVoiceFlowDirector director = FindSingle<PPEVoiceFlowDirector>(previewScene, failures);
            PPEEquipmentVisualController equipment =
                FindSingle<PPEEquipmentVisualController>(previewScene, failures);
            PPEEducationWearChecklist checklist =
                FindSingle<PPEEducationWearChecklist>(previewScene, failures);
            PPETabletChecklistController tabletChecklist =
                FindSingle<PPETabletChecklistController>(previewScene, failures);

            if (director != null && equipment != null)
            {
                ValidateTapePrerequisites(director, equipment, failures);
                ValidateTapeGrabNarration(director, failures);
                ValidateRequiredPpeHowToNarration(director, failures);
                ValidateLeakHarnessRouting(previewScene, director, failures);
            }

            if (director != null)
            {
                ValidateModeFeedbackPolicy(director, failures);
                ValidateHazmatGrabNarration(director, failures);
                ValidateWelcomeNarration(director, failures);
                ValidateEducationStartVoices(director, failures);
                ValidateFinaleAndExitWiring(previewScene, director, failures);
                ValidateControllerGuideVisuals(previewScene, director, failures);
                ValidateTemporaryKeyboardBypass(previewScene, director, failures);
                ValidateScenarioModalHoverColors(director, failures);
            }

            if (director != null && checklist != null)
                ValidateChecklistWorkPlanReset(director, checklist, failures);

            if (tabletChecklist != null)
                ValidateTabletChecklistSequences(tabletChecklist, failures);

            ValidateFootstepInputOwnership(previewScene, failures);
            ValidateHeadRelativeLocomotion(previewScene, failures);
            ValidateHazmatAlreadyEquippedPriority(failures);
            ValidateBodyColliderProximity(failures);
            ValidateTabletSurfaceSampling(failures);
            ValidatePosterSampling(failures);
            ValidateQuestRenderQuality(failures);
        }
        finally
        {
            if (previewScene.IsValid())
                EditorSceneManager.ClosePreviewScene(previewScene);
        }

        if (failures.Count > 0)
        {
            string message = "PPE locomotion regression validation failed:\n- " +
                string.Join("\n- ", failures);
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        Debug.Log(
            $"[PPE Locomotion Regression Validation] PASS '{ScenePath}': " +
            "tape prerequisites and one-shot narration, already-equipped hazmat priority, " +
            "scenario-required Education How-To narration, " +
            "mode-owned PPE feedback audio and presentation, exclusive mid-exit Voice, " +
            "one-shot hazmat narration, persistent PPE-area session, mirror/exit wiring, " +
            "active-camera head-relative locomotion, " +
            "leak harness rejection routing, Meta-account Welcome voices, PPE-area voices, authored controller-guide mapping " +
            "and detailed input feedback, " +
            "body-collider proximity, " +
            "Quest render scale/MSAA, " +
            "tablet/signature anti-shimmer sampling, " +
            "seven-step tablet checklists, high-resolution PPE poster sampling, neutral modal hover colors, " +
            "checklist work-plan reset, and input-owned footsteps are valid.");
    }

    static void ValidateScenarioModalHoverColors(
        PPEVoiceFlowDirector director,
        List<string> failures)
    {
        ScenarioDetailModal modal = new SerializedObject(director)
            .FindProperty("m_ScenarioDetailModal")?.objectReferenceValue as ScenarioDetailModal;
        if (modal == null)
        {
            failures.Add("PPEVoiceFlowDirector requires its authored ScenarioDetailModal reference.");
            return;
        }

        string[] buttonProperties =
        {
            "trainingButton",
            "backButton",
            "incompletePpeButton",
            "standardTrainingButton",
            "trainingChoiceBackButton",
            "ppeEducationModeButton",
            "ppeTrainingModeButton",
            "ppeTestModeButton",
            "ppeModeChoiceBackButton",
            "testConfinedSpaceButton",
            "testLeakResponseButton",
            "testWorkPlanBackButton"
        };

        SerializedObject serializedModal = new(modal);
        Color expectedNormal = Color.white;
        Color expectedHighlighted = new(0.84f, 0.84f, 0.84f, 1f);
        Color expectedPressed = new(0.62f, 0.62f, 0.62f, 1f);
        foreach (string propertyName in buttonProperties)
        {
            Button button = serializedModal.FindProperty(propertyName)
                ?.objectReferenceValue as Button;
            if (button == null)
            {
                failures.Add($"ScenarioDetailModal.{propertyName} requires an authored Button reference.");
                continue;
            }

            ColorBlock colors = button.colors;
            Color normal = colors.normalColor;
            Color highlighted = colors.highlightedColor;
            Color pressed = colors.pressedColor;
            Color selected = colors.selectedColor;
            if (normal != expectedNormal ||
                highlighted != expectedHighlighted ||
                pressed != expectedPressed ||
                selected != expectedNormal)
            {
                failures.Add(
                    $"Scenario modal button '{button.name}' must retain distinct authored " +
                    "normal, gray hover, and dark-gray pressed colors, with selected returning to normal.");
            }
        }
    }

    static void ValidateTabletChecklistSequences(
        PPETabletChecklistController controller,
        List<string> failures)
    {
        const int ChecklistCount = 7;
        string[] expectedNames =
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

        if (controller.InstantChecklistStepCount != ChecklistCount)
        {
            failures.Add(
                $"Tablet must reveal {ChecklistCount} checklist marks immediately; " +
                $"configured value is {controller.InstantChecklistStepCount}.");
        }

        SerializedObject serializedController = new(controller);
        foreach (string propertyName in new[] { "confinedSignatureSequence", "leakSignatureSequence" })
        {
            HandwrittenSignatureSequence sequence = serializedController.FindProperty(propertyName)
                ?.objectReferenceValue as HandwrittenSignatureSequence;
            if (sequence == null)
            {
                failures.Add($"Tablet {propertyName} requires an authored sequence reference.");
                continue;
            }

            SerializedProperty steps = new SerializedObject(sequence).FindProperty("signatures");
            if (steps == null || steps.arraySize != expectedNames.Length)
            {
                failures.Add(
                    $"Tablet {propertyName} must contain {expectedNames.Length} ordered steps.");
                continue;
            }

            for (int index = 0; index < expectedNames.Length; index++)
            {
                SerializedProperty step = steps.GetArrayElementAtIndex(index);
                Renderer renderer = step.FindPropertyRelative("targetRenderer")
                    .objectReferenceValue as Renderer;
                if (renderer == null || renderer.name != expectedNames[index])
                {
                    failures.Add(
                        $"Tablet {propertyName} step {index} must reference '{expectedNames[index]}'.");
                }

                bool animateReveal = step.FindPropertyRelative("animateReveal").boolValue;
                if (index == 7 && !animateReveal)
                {
                    failures.Add(
                        $"Tablet {propertyName} Player_Signature must be the single animated signature.");
                }
                else if (index == 8 && animateReveal)
                {
                    failures.Add(
                        $"Tablet {propertyName} Conductor_Signature must remain a static confirmation mark.");
                }
            }
        }
    }

    static void ValidatePosterSampling(List<string> failures)
    {
        const string PosterPath = "Assets/UIs/Poster/PPE_Poster_1.png";
        TextureImporter importer = AssetImporter.GetAtPath(PosterPath) as TextureImporter;
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(PosterPath);
        if (importer == null || texture == null)
        {
            failures.Add($"PPE poster texture is missing: '{PosterPath}'.");
            return;
        }

        if (importer.maxTextureSize < 4096 ||
            importer.npotScale != TextureImporterNPOTScale.None ||
            importer.filterMode != FilterMode.Trilinear ||
            importer.anisoLevel < 8 ||
            importer.compressionQuality < 100 ||
            importer.wrapModeU != TextureWrapMode.Clamp ||
            importer.wrapModeV != TextureWrapMode.Clamp)
        {
            failures.Add(
                "PPE_Poster_1 requires 4096 max size, no NPOT resize, Trilinear/aniso 8, " +
                "maximum compression quality, and Clamp wrapping for small-text readability.");
        }

        if (texture.width < 2381 || texture.height < 3402)
        {
            failures.Add(
                $"PPE_Poster_1 imported resolution is only {texture.width}x{texture.height}.");
        }
    }

    static void ValidateModeFeedbackPolicy(
        PPEVoiceFlowDirector director,
        List<string> failures)
    {
        foreach (PPEActionPanelController panel in FindAll<PPEActionPanelController>(director.gameObject.scene))
        {
            if (!panel.enabled || !panel.gameObject.activeInHierarchy)
                continue;

            SerializedObject panelSerialized = new(panel);
            if (panelSerialized.FindProperty("voiceFlowDirector")?.objectReferenceValue != director)
            {
                failures.Add(
                    $"Active PPE panel '{panel.name}' must reference the scene PPEVoiceFlowDirector " +
                    "before applying mode feedback.");
            }
        }

        Type type = typeof(PPEVoiceFlowDirector);
        PropertyInfo mode = type.GetProperty(nameof(PPEVoiceFlowDirector.ActiveLearningMode));
        PropertyInfo actionSfx = type.GetProperty(nameof(PPEVoiceFlowDirector.AllowsPpeActionSfx));
        PropertyInfo choiceVoice = type.GetProperty(nameof(PPEVoiceFlowDirector.AllowsPpeChoiceVoice));
        FieldInfo midExitExclusive = type.GetField("m_MidExitVoiceExclusive", InstancePrivate);
        if (mode == null || actionSfx == null || choiceVoice == null || midExitExclusive == null)
        {
            failures.Add("PPE mode feedback policy members could not be inspected.");
            return;
        }

        object previousMode = mode.GetValue(director);
        object previousExclusive = midExitExclusive.GetValue(director);
        try
        {
            midExitExclusive.SetValue(director, false);
            AssertFeedbackPolicy(
                director,
                mode,
                actionSfx,
                choiceVoice,
                ScenarioDetailModal.PpeLearningMode.Education,
                expectedActionSfx: true,
                expectedChoiceVoice: true,
                failures: failures);
            AssertFeedbackPolicy(
                director,
                mode,
                actionSfx,
                choiceVoice,
                ScenarioDetailModal.PpeLearningMode.Training,
                expectedActionSfx: true,
                expectedChoiceVoice: false,
                failures: failures);
            AssertFeedbackPolicy(
                director,
                mode,
                actionSfx,
                choiceVoice,
                ScenarioDetailModal.PpeLearningMode.Test,
                expectedActionSfx: false,
                expectedChoiceVoice: false,
                failures: failures);

            mode.SetValue(director, ScenarioDetailModal.PpeLearningMode.Education);
            midExitExclusive.SetValue(director, true);
            if ((bool)actionSfx.GetValue(director) || (bool)choiceVoice.GetValue(director))
                failures.Add("Mid-exit must block new PPE feedback while its Voice owns the channel.");
        }
        finally
        {
            mode.SetValue(director, previousMode);
            midExitExclusive.SetValue(director, previousExclusive);
        }

        string panelSource = File.ReadAllText("Assets/Scripts/PPEActionPanelController.cs");
        if (!panelSource.Contains(
                "!approveUseByBodyProximity && AllowsPpeFeedbackPresentation()",
                StringComparison.Ordinal))
        {
            failures.Add("Training/Test PPE choices must not show legacy feedback text or icons.");
        }
        if (!panelSource.Contains(
                "clip == null || AudioManager.Instance == null || !AllowsPpeChoiceVoice()",
                StringComparison.Ordinal))
        {
            failures.Add("Wrong-PPE Voice must be gated by the Education-only feedback policy.");
        }

        string directorSource = File.ReadAllText("Assets/Scripts/PPEVoiceFlowDirector.cs");
        if (!directorSource.Contains("StopFlowPlayback(stopSfx: false);", StringComparison.Ordinal) ||
            !directorSource.Contains("m_MidExitVoiceExclusive = true;", StringComparison.Ordinal))
        {
            failures.Add("Mid-exit must cancel competing Voice producers without stopping SFX.");
        }
        if (directorSource.Contains(
                "PlayPpeConditionalVoice(m_TrainingWrongButtonVoice)",
                StringComparison.Ordinal))
        {
            failures.Add("Training mode must not play the legacy wrong-PPE Voice.");
        }
        if (!directorSource.Contains(
                ": m_TestMoveToPpeVoice;",
                StringComparison.Ordinal) ||
            !directorSource.Contains(
                ": new[] { m_TestModeSelectedVoice, m_TestMoveToPpeVoice, m_TestEndVoice };",
                StringComparison.Ordinal) ||
            !directorSource.Contains(
                "PlayPpeConditionalVoice(m_TestEndVoice);",
                StringComparison.Ordinal))
        {
            failures.Add(
                "Test mode must retain its authored selection, move-to-PPE, and completion Voice flow.");
        }
    }

    static void AssertFeedbackPolicy(
        PPEVoiceFlowDirector director,
        PropertyInfo modeProperty,
        PropertyInfo actionSfxProperty,
        PropertyInfo choiceVoiceProperty,
        ScenarioDetailModal.PpeLearningMode mode,
        bool expectedActionSfx,
        bool expectedChoiceVoice,
        List<string> failures)
    {
        modeProperty.SetValue(director, mode);
        bool actualActionSfx = (bool)actionSfxProperty.GetValue(director);
        bool actualChoiceVoice = (bool)choiceVoiceProperty.GetValue(director);
        if (actualActionSfx != expectedActionSfx || actualChoiceVoice != expectedChoiceVoice)
        {
            failures.Add(
                $"{mode} PPE feedback policy mismatch: " +
                $"SFX={actualActionSfx}, wrong/How-To Voice={actualChoiceVoice}.");
        }
    }

    static void ValidateTabletSurfaceSampling(List<string> failures)
    {
        string[] mipmappedTextures =
        {
            "Assets/Materials/PPE/Tablet/work_confirm_tablet_readable.png.meta",
            "Assets/UIs/Things/Docs/WorkPlan.png.meta",
            "Assets/UIs/Things/Docs/WorkPlan_Leak.png.meta",
        };
        foreach (string path in mipmappedTextures)
        {
            string importer = File.ReadAllText(path);
            if (!importer.Contains("enableMipMap: 1", StringComparison.Ordinal))
                failures.Add($"Thin-text tablet texture must enable mipmaps: {path}");
        }

        string[] antiShimmerTextures =
        {
            "Assets/UIs/Logo/VrLogo_2d.png",
            "Assets/UIs/Guide/Controller_tri.png",
            "Assets/UIs/Guide/Controller_gri.png",
            "Assets/UIs/Guide/Controller_joy.png",
            "Assets/Materials/PPE/Tablet/work_confirm_tablet_readable.png",
            "Assets/UIs/sign_stamp/sign_player_rm.png",
        };
        foreach (string path in antiShimmerTextures)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            {
                failures.Add($"Anti-shimmer texture importer is missing: {path}");
                continue;
            }

            if (!importer.mipmapEnabled || importer.filterMode != FilterMode.Trilinear ||
                importer.anisoLevel < 8)
            {
                failures.Add(
                    $"Anti-shimmer texture requires mipmaps, Trilinear filtering, and aniso >= 8: {path}");
            }
        }

        string[] androidHighQualityTextures =
        {
            "Assets/UIs/Guide/Controller_tri.png",
            "Assets/UIs/Guide/Controller_gri.png",
            "Assets/UIs/Guide/Controller_joy.png",
            "Assets/UIs/sign_stamp/sign_player_rm.png",
        };
        foreach (string path in androidHighQualityTextures)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            TextureImporterPlatformSettings android =
                importer?.GetPlatformTextureSettings("Android");
            if (android == null || !android.overridden ||
                android.textureCompression != TextureImporterCompression.CompressedHQ ||
                android.compressionQuality < 100)
            {
                failures.Add($"Quest texture requires an Android high-quality override: {path}");
            }
        }

        if (AssetImporter.GetAtPath(
                "Assets/UIs/Logo/VrLogo_2d.png") is not TextureImporter titleLogo)
        {
            failures.Add("Title logo texture importer is missing.");
        }
        else
        {
            ValidateUncompressedLogoPlatform(titleLogo, "Android", failures);
            ValidateUncompressedLogoPlatform(titleLogo, "Standalone", failures);
        }

        if (AssetImporter.GetAtPath(
                "Assets/UIs/sign_stamp/sign_player_rm.png") is TextureImporter playerSignature &&
            (playerSignature.npotScale != TextureImporterNPOTScale.None ||
             playerSignature.wrapModeU != TextureWrapMode.Clamp ||
             playerSignature.wrapModeV != TextureWrapMode.Clamp ||
             !playerSignature.alphaIsTransparency))
        {
            failures.Add(
                "Player signature must preserve the one-piece NPOT source and use Clamp/transparent-edge sampling.");
        }

        ValidateYamlSpritePreserveAspect(
            "Assets/Scenes/1_Title.unity",
            "cbf5db553974ec84fb37aea653f5f1bf",
            failures);
        ValidateYamlSpritePreserveAspect(
            "Assets/Scenes/6_LoadingScene_0.unity",
            "cbf5db553974ec84fb37aea653f5f1bf",
            failures);
        ValidateYamlSpritePreserveAspect(
            "Assets/Prefabs/Title_Logo_Canvas.prefab",
            "cbf5db553974ec84fb37aea653f5f1bf",
            failures);

        string[] signatureTextures =
        {
            "Assets/UIs/sign_stamp/sign_check.png.meta",
            "Assets/UIs/sign_stamp/sign_player_rm.png.meta",
            "Assets/UIs/sign_stamp/sign_conductor_ppe.png.meta",
            "Assets/UIs/sign_stamp/sign_conductor_mixer.png.meta",
        };
        foreach (string path in signatureTextures)
        {
            string importer = File.ReadAllText(path);
            if (!importer.Contains("enableMipMap: 1", StringComparison.Ordinal) ||
                !importer.Contains("aniso: 8", StringComparison.Ordinal))
            {
                failures.Add($"Signature texture requires mipmaps and anisotropic level 8: {path}");
            }
        }

        string shader = File.ReadAllText("Assets/Shaders/HandwrittenSignatureReveal.shader");
        if (!shader.Contains("Offset -1, -1", StringComparison.Ordinal))
            failures.Add("Signature reveal shader requires a small depth offset from the document surface.");
    }

    static void ValidateYamlSpritePreserveAspect(
        string assetPath,
        string spriteGuid,
        List<string> failures)
    {
        string yaml = File.ReadAllText(assetPath);
        int spriteIndex = yaml.IndexOf($"guid: {spriteGuid}", StringComparison.Ordinal);
        int preserveIndex = spriteIndex >= 0
            ? yaml.IndexOf("m_PreserveAspect: 1", spriteIndex, StringComparison.Ordinal)
            : -1;
        if (spriteIndex < 0 || preserveIndex < 0 || preserveIndex - spriteIndex > 320)
        {
            failures.Add($"Sprite must preserve its source aspect ratio in '{assetPath}'.");
        }
    }

    static void ValidateQuestRenderQuality(List<string> failures)
    {
        string mobilePipeline = File.ReadAllText("Assets/Settings/Mobile_RPAsset.asset");
        if (!mobilePipeline.Contains("m_RenderScale: 0.9", StringComparison.Ordinal))
            failures.Add("Mobile_RPAsset render scale must remain 0.9 for distant Quest scene clarity.");
        if (!mobilePipeline.Contains("m_MSAA: 4", StringComparison.Ordinal))
            failures.Add("Mobile_RPAsset must retain 4x MSAA.");

        string qualitySettings = File.ReadAllText("ProjectSettings/QualitySettings.asset");
        if (!qualitySettings.Contains("Android: 0", StringComparison.Ordinal))
            failures.Add("Android must continue to use the Mobile quality level.");
    }

    static void ValidateUncompressedLogoPlatform(
        TextureImporter importer,
        string platformName,
        List<string> failures)
    {
        TextureImporterPlatformSettings platform =
            importer.GetPlatformTextureSettings(platformName);
        if (!platform.overridden ||
            platform.format != TextureImporterFormat.RGBA32 ||
            platform.textureCompression != TextureImporterCompression.Uncompressed)
        {
            failures.Add(
                $"Title logo requires an uncompressed RGBA32 {platformName} override.");
        }
    }

    public static void ValidateBatch()
    {
        try
        {
            Validate();
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    [MenuItem("Tools/PPE/Validate Footstep Input Loop")]
    public static void ValidateFootstepPlaybackThrottling()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
            throw new InvalidOperationException($"Open '{ScenePath}' before validating footsteps.");

        List<string> failures = new();
        ValidateFootstepInputOwnership(scene, failures);

        if (failures.Count > 0)
        {
            string message = "Footstep playback throttling validation failed:\n- " +
                string.Join("\n- ", failures);
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        Debug.Log(
            "[PPE Footstep Validation] PASS: locomotion input owns one constant-speed Footstep loop " +
            "and releasing input stops its dedicated AudioSource.");
    }

    [MenuItem("Tools/PPE/Configure Dedicated Footstep Audio Source")]
    public static void ConfigureDedicatedFootstepAudioSource()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before configuring the Footstep AudioSource.");

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
            throw new InvalidOperationException($"Open '{ScenePath}' before configuring footsteps.");

        PPEConfigurableDynamicMoveProvider moveProvider = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<PPEConfigurableDynamicMoveProvider>(true))
            .Single();
        AudioManager audioManager = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<AudioManager>(true))
            .Single();
        AudioSource sharedSfxSource = new SerializedObject(audioManager)
            .FindProperty("m_SfxSource")?.objectReferenceValue as AudioSource;
        if (sharedSfxSource == null)
            throw new InvalidOperationException("AudioManager is missing its authored shared SFX source.");

        Transform sourceTransform = audioManager.transform.Find("Footsteps");
        AudioSource footstepSource;
        if (sourceTransform == null)
        {
            GameObject sourceObject = new("Footsteps");
            Undo.RegisterCreatedObjectUndo(sourceObject, "Create dedicated Footstep AudioSource");
            sourceObject.transform.SetParent(audioManager.transform, false);
            footstepSource = Undo.AddComponent<AudioSource>(sourceObject);
            EditorUtility.CopySerialized(sharedSfxSource, footstepSource);
        }
        else
        {
            footstepSource = sourceTransform.GetComponent<AudioSource>();
            if (footstepSource == null)
                footstepSource = Undo.AddComponent<AudioSource>(sourceTransform.gameObject);
            Undo.RecordObject(footstepSource, "Configure dedicated Footstep AudioSource");
        }

        footstepSource.playOnAwake = false;
        footstepSource.loop = false;
        footstepSource.clip = null;
        footstepSource.pitch = 1f;
        EditorUtility.SetDirty(footstepSource);

        SerializedObject moveSerialized = new(moveProvider);
        moveSerialized.FindProperty("m_FootstepSource").objectReferenceValue = footstepSource;
        moveSerialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(moveProvider);
        EditorSceneManager.MarkSceneDirty(scene);

        Debug.Log(
            "[PPE Footstep] Dedicated Footsteps AudioSource configured. " +
            "Save the scene after reviewing the existing unsaved changes.",
            footstepSource);
    }

    [MenuItem("Tools/PPE/Validate Continuous Mirror Observation")]
    public static void ValidateContinuousMirrorObservation()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
            throw new InvalidOperationException($"Open '{ScenePath}' before validating the mirror observation.");

        PPEFinaleController finale = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<PPEFinaleController>(true))
            .Single();
        SerializedObject serialized = new(finale);
        string[] requiredReferences =
        {
            "m_XrOrigin",
            "m_MirrorObservationPoint",
            "m_ObservationGaugeRoot",
            "m_ObservationGaugeFill",
        };
        foreach (string propertyName in requiredReferences)
        {
            if (serialized.FindProperty(propertyName)?.objectReferenceValue == null)
                throw new InvalidOperationException($"Mirror observation is missing '{propertyName}'.");
        }

        string source = File.ReadAllText("Assets/Scripts/PPEFinaleController.cs");
        if (source.Contains("IsLookingAtMirror()", StringComparison.Ordinal) ||
            !source.Contains("m_ObservationElapsed += Time.unscaledDeltaTime;", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Mirror observation must advance continuously while the player remains in the authored radius.");
        }

        Debug.Log(
            "[PPE Mirror Observation] PASS: looking away does not pause the gauge; " +
            "leaving the authored mirror radius still aborts the check.",
            finale);
    }

    static void ValidateTapePrerequisites(
        PPEVoiceFlowDirector director,
        PPEEquipmentVisualController equipment,
        List<string> failures)
    {
        MethodInfo hasAny = typeof(PPEVoiceFlowDirector).GetMethod(
            "HasAnyTappableEquipment",
            InstancePrivate);
        FieldInfo slotsField = typeof(PPEEquipmentVisualController).GetField(
            "slots",
            InstancePrivate);
        FieldInfo usedSlotsField = typeof(PPEEquipmentVisualController).GetField(
            "usedSlots",
            InstancePrivate);
        FieldInfo tapedTypesField = typeof(PPEEquipmentVisualController).GetField(
            "tapedItemTypes",
            InstancePrivate);
        MethodInfo markAllTaped = typeof(PPEEquipmentVisualController).GetMethod(
            "MarkAllTappableEquipmentAsTaped",
            InstancePrivate);
        FieldInfo tapeVisualsField = typeof(PPEEquipmentVisualController).GetField(
            "tapeVisuals",
            InstancePrivate);
        MethodInfo setTapeVisuals = typeof(PPEEquipmentVisualController).GetMethod(
            "SetTapeVisuals",
            InstancePrivate);
        FieldInfo equipmentField = typeof(PPEVoiceFlowDirector).GetField(
            "m_EquipmentVisualController",
            InstancePrivate);

        if (hasAny == null || slotsField == null || usedSlotsField == null ||
            tapedTypesField == null || markAllTaped == null || tapeVisualsField == null ||
            setTapeVisuals == null || equipmentField == null)
        {
            failures.Add("Tape prerequisite members could not be inspected.");
            return;
        }

        equipmentField.SetValue(director, equipment);
        PPEEquipmentVisualSlot[] slots =
            (PPEEquipmentVisualSlot[])slotsField.GetValue(equipment);
        HashSet<PPEEquipmentVisualSlot> used =
            (HashSet<PPEEquipmentVisualSlot>)usedSlotsField.GetValue(equipment);
        used.Clear();

        AddUsedSlot(slots, used, PPEItemType.NitrileInnerGloveLeft, failures);
        AddUsedSlot(slots, used, PPEItemType.NitrileInnerGloveRight, failures);
        if ((bool)hasAny.Invoke(director, null))
            failures.Add("Nitrile inner gloves alone must not satisfy the tape prerequisites.");

        AddUsedSlot(slots, used, PPEItemType.RubberGloveLeft, failures);
        if (!(bool)hasAny.Invoke(director, null))
            failures.Add("One outer glove must satisfy the tape prerequisites.");

        used.Clear();
        AddUsedSlot(slots, used, PPEItemType.NitrileInnerGloveLeft, failures);
        AddUsedSlot(slots, used, PPEItemType.NitrileInnerGloveRight, failures);
        AddUsedSlot(slots, used, PPEItemType.RubberBootRight, failures);
        if (!(bool)hasAny.Invoke(director, null))
            failures.Add("One rubber boot must satisfy the tape prerequisites.");

        HashSet<PPEItemType> taped =
            (HashSet<PPEItemType>)tapedTypesField.GetValue(equipment);
        taped.Clear();
        markAllTaped.Invoke(equipment, null);
        PPEItemType[] expectedTapedTypes =
        {
            PPEItemType.RubberGloveLeft,
            PPEItemType.RubberGloveRight,
            PPEItemType.RubberBootLeft,
            PPEItemType.RubberBootRight,
        };
        if (expectedTapedTypes.Any(itemType => !taped.Contains(itemType)))
            failures.Add("One approved tape use must mark all outer gloves and boots as taped.");

        setTapeVisuals.Invoke(equipment, new object[] { true });
        PPEEquipmentTapeVisual[] tapeVisuals =
            (PPEEquipmentTapeVisual[])tapeVisualsField.GetValue(equipment);
        foreach (PPEEquipmentTapeVisual tapeVisual in tapeVisuals ?? Array.Empty<PPEEquipmentTapeVisual>())
        {
            if (tapeVisual?.Visual == null)
                continue;

            bool expectedVisible = equipment.IsItemUsed(tapeVisual.RequiredItemType);
            if (tapeVisual.Visual.activeSelf != expectedVisible)
            {
                failures.Add(
                    $"Tape visual for {tapeVisual.RequiredItemType} must be active only when that side is worn.");
            }
        }

        setTapeVisuals.Invoke(equipment, new object[] { false });
        used.Clear();
        taped.Clear();
    }

    static void AddUsedSlot(
        PPEEquipmentVisualSlot[] slots,
        HashSet<PPEEquipmentVisualSlot> used,
        PPEItemType itemType,
        List<string> failures)
    {
        PPEEquipmentVisualSlot slot = slots?.FirstOrDefault(candidate =>
            candidate?.ItemBinding?.ItemIdentity != null &&
            candidate.ItemBinding.ItemIdentity.ItemType == itemType);
        if (slot == null)
        {
            failures.Add($"Equipment slot is missing for {itemType}.");
            return;
        }

        used.Add(slot);
    }

    static void ValidateTapeGrabNarration(
        PPEVoiceFlowDirector director,
        List<string> failures)
    {
        Type type = typeof(PPEVoiceFlowDirector);
        MethodInfo consume = type.GetMethod("TryConsumeTapeGrabVoice", InstancePrivate);
        MethodInfo reset = type.GetMethod("ResetModeSessionTracking", InstancePrivate);
        FieldInfo played = type.GetField("m_TapeGrabVoicePlayed", InstancePrivate);
        PropertyInfo mode = type.GetProperty(nameof(PPEVoiceFlowDirector.ActiveLearningMode));

        if (consume == null || reset == null || played == null || mode == null)
        {
            failures.Add("Tape narration one-shot members could not be inspected.");
            return;
        }

        mode.SetValue(director, ScenarioDetailModal.PpeLearningMode.Education);
        played.SetValue(director, false);
        bool first = (bool)consume.Invoke(director, new object[] { true });
        bool second = (bool)consume.Invoke(director, new object[] { true });
        reset.Invoke(director, null);
        bool nextSession = (bool)consume.Invoke(director, new object[] { true });
        if (!first || second || !nextSession)
            failures.Add("Tape grab narration must play once per education mode session.");
    }

    static void ValidateRequiredPpeHowToNarration(
        PPEVoiceFlowDirector director,
        List<string> failures)
    {
        SerializedObject serialized = new(director);
        PPEActionPanelController goggle = serialized.FindProperty("m_GoggleActionPanel")
            ?.objectReferenceValue as PPEActionPanelController;
        PPEActionPanelController faceShield = serialized.FindProperty("m_FaceShieldActionPanel")
            ?.objectReferenceValue as PPEActionPanelController;
        SerializedProperty nitrilePanels = serialized.FindProperty("m_NitrileInnerGloveActionPanels");

        if (goggle == null || goggle.name != "PPE_A_Goggle_Clean")
            failures.Add("How-To 107 must reference PPE_A_Goggle_Clean.");
        if (faceShield == null || faceShield.name != "PPE_A_FaceShield_Clean")
            failures.Add("How-To 108 must reference PPE_A_FaceShield_Clean.");
        if (nitrilePanels == null || nitrilePanels.arraySize != 2)
        {
            failures.Add("How-To 109 must reference both nitrile inner-glove panels.");
        }
        else
        {
            HashSet<string> names = new();
            for (int index = 0; index < nitrilePanels.arraySize; index++)
            {
                if (nitrilePanels.GetArrayElementAtIndex(index).objectReferenceValue is
                    PPEActionPanelController panel)
                {
                    names.Add(panel.name);
                }
            }

            if (!names.SetEquals(new[] { "PPE_A_InnerGlove_L", "PPE_A_InnerGlove_R" }))
                failures.Add("How-To 109 nitrile references must be the authored left and right inner gloves.");
        }

        ValidateHowToClip(serialized, "m_GoggleGrabVoice", "4_VO_PPE_EDU_107_HowToGoggle", failures);
        ValidateHowToClip(serialized, "m_FaceShieldGrabVoice", "4_VO_PPE_EDU_108_HowToFaceShield", failures);
        ValidateHowToClip(serialized, "m_NitrileInnerGloveGrabVoice", "4_VO_PPE_EDU_109_HowToInneerGlove", failures);

        Type type = typeof(PPEVoiceFlowDirector);
        MethodInfo canPlay = type.GetMethod("CanPlayRequiredPpeHowTo", InstancePrivate);
        MethodInfo reset = type.GetMethod("ResetModeSessionTracking", InstancePrivate);
        FieldInfo nitrilePlayed = type.GetField("m_NitrileInnerGloveGrabVoicePlayed", InstancePrivate);
        PropertyInfo mode = type.GetProperty(nameof(PPEVoiceFlowDirector.ActiveLearningMode));
        PropertyInfo workPlan = type.GetProperty(nameof(PPEVoiceFlowDirector.ActiveWorkPlan));
        if (canPlay == null || reset == null || nitrilePlayed == null || mode == null || workPlan == null ||
            goggle == null || faceShield == null || nitrilePanels == null || nitrilePanels.arraySize == 0)
        {
            failures.Add("Scenario-required How-To policy members could not be inspected.");
            return;
        }

        PPEActionPanelController nitrile =
            nitrilePanels.GetArrayElementAtIndex(0).objectReferenceValue as PPEActionPanelController;
        if (nitrile == null)
        {
            failures.Add("The first nitrile How-To panel reference is missing.");
            return;
        }

        mode.SetValue(director, ScenarioDetailModal.PpeLearningMode.Education);
        workPlan.SetValue(director, ScenarioDetailModal.PpeWorkPlan.ConfinedSpace);
        bool confinedGoggle = (bool)canPlay.Invoke(director, new object[] { goggle });
        bool confinedFaceShield = (bool)canPlay.Invoke(director, new object[] { faceShield });
        bool confinedNitrile = (bool)canPlay.Invoke(director, new object[] { nitrile });

        workPlan.SetValue(director, ScenarioDetailModal.PpeWorkPlan.LeakResponse);
        bool leakGoggle = (bool)canPlay.Invoke(director, new object[] { goggle });
        bool leakFaceShield = (bool)canPlay.Invoke(director, new object[] { faceShield });
        bool leakNitrile = (bool)canPlay.Invoke(director, new object[] { nitrile });

        mode.SetValue(director, ScenarioDetailModal.PpeLearningMode.Training);
        bool trainingGoggle = (bool)canPlay.Invoke(director, new object[] { goggle });
        mode.SetValue(director, ScenarioDetailModal.PpeLearningMode.Education);
        workPlan.SetValue(director, ScenarioDetailModal.PpeWorkPlan.None);
        bool noPlanGoggle = (bool)canPlay.Invoke(director, new object[] { goggle });

        if (confinedGoggle || confinedFaceShield || !confinedNitrile ||
            !leakGoggle || !leakFaceShield || !leakNitrile ||
            trainingGoggle || noPlanGoggle)
        {
            failures.Add(
                "How-To narration must play only for clean, scenario-required PPE in Education mode.");
        }

        nitrilePlayed.SetValue(director, true);
        reset.Invoke(director, null);
        if ((bool)nitrilePlayed.GetValue(director))
            failures.Add("The shared nitrile inner-glove How-To state must reset for each mode session.");
    }

    static void ValidateHowToClip(
        SerializedObject serialized,
        string propertyName,
        string expectedName,
        List<string> failures)
    {
        AudioClip clip = serialized.FindProperty(propertyName)?.objectReferenceValue as AudioClip;
        if (clip == null || clip.name != expectedName)
            failures.Add($"{propertyName} must reference '{expectedName}'.");
    }

    static void ValidateLeakHarnessRouting(
        Scene scene,
        PPEVoiceFlowDirector director,
        List<string> failures)
    {
        PPEActionPanelController[] panels = FindAll<PPEActionPanelController>(scene);
        PPEActionPanelController harness = panels.SingleOrDefault(panel =>
            panel.name == "PPE_A_Backplate");
        if (harness == null)
        {
            failures.Add("PPE_A_Backplate action panel is missing.");
            return;
        }

        SerializedObject panelSerialized = new(harness);
        if (panelSerialized.FindProperty("voiceFlowDirector")?.objectReferenceValue != director)
            failures.Add("PPE_A_Backplate must reference the scene PPEVoiceFlowDirector.");

        SerializedObject directorSerialized = new(director);
        PPEActionPanelController tape = directorSerialized.FindProperty("m_TapeActionPanel")
            ?.objectReferenceValue as PPEActionPanelController;
        if (tape == null)
        {
            failures.Add("PPEVoiceFlowDirector tape action panel reference is missing.");
        }
        else
        {
            SerializedObject tapeSerialized = new(tape);
            if (tapeSerialized.FindProperty("voiceFlowDirector")?.objectReferenceValue != director)
                failures.Add("The tape action panel must reference the scene PPEVoiceFlowDirector.");
        }

        if (directorSerialized.FindProperty("m_TapeUseBeforeGlovesAndBootsVoice")
                ?.objectReferenceValue == null)
        {
            failures.Add("The nitrile-only tape rejection voice reference is missing.");
        }

        SerializedProperty required = directorSerialized.FindProperty(
            "m_LeakResponseRequiredItemTypes");
        bool containsHarness = false;
        for (int index = 0; required != null && index < required.arraySize; index++)
        {
            containsHarness |= required.GetArrayElementAtIndex(index).enumValueIndex ==
                (int)PPEItemType.TacticalHarness;
        }

        if (containsHarness)
            failures.Add("Leak-response required PPE must not include TacticalHarness.");

        AudioClip workPlanMismatchVoice = directorSerialized.FindProperty("m_WorkPlanMismatchVoice")
            ?.objectReferenceValue as AudioClip;
        if (workPlanMismatchVoice == null ||
            workPlanMismatchVoice.name != "4_VO_PPE_EDU_205_PPE_forScenario")
        {
            failures.Add(
                "The work-plan mismatch education voice must reference EDU 205 PPE_forScenario.");
        }

        string wrongSfxId = panelSerialized.FindProperty("wrongFeedbackSfxId")?.stringValue;
        if (string.IsNullOrWhiteSpace(wrongSfxId))
            failures.Add("PPE_A_Backplate is missing its wrong-feedback SFX id.");

        ValidateScenarioMismatchPanelSfx(
            scene,
            director,
            directorSerialized,
            panels,
            failures);

        MethodInfo isAllowed = typeof(PPEVoiceFlowDirector).GetMethod(
            "IsPpeTypeAllowedForActiveWorkPlan",
            InstancePrivate);
        PropertyInfo activeWorkPlan = typeof(PPEVoiceFlowDirector).GetProperty(
            nameof(PPEVoiceFlowDirector.ActiveWorkPlan));
        if (isAllowed == null || activeWorkPlan == null)
        {
            failures.Add("Leak-response PPE allowance members could not be inspected.");
        }
        else
        {
            object previous = activeWorkPlan.GetValue(director);
            activeWorkPlan.SetValue(director, ScenarioDetailModal.PpeWorkPlan.LeakResponse);
            bool harnessAllowed = (bool)isAllowed.Invoke(
                director,
                new object[] { PPEItemType.TacticalHarness });
            activeWorkPlan.SetValue(director, previous);
            if (harnessAllowed)
                failures.Add("Leak-response work plan must reject TacticalHarness use.");
        }
    }

    static void ValidateHazmatGrabNarration(
        PPEVoiceFlowDirector director,
        List<string> failures)
    {
        Type type = typeof(PPEVoiceFlowDirector);
        MethodInfo consume = type.GetMethod("TryConsumeHazmatGrabVoice", InstancePrivate);
        MethodInfo reset = type.GetMethod("ResetModeSessionTracking", InstancePrivate);
        PropertyInfo mode = type.GetProperty(nameof(PPEVoiceFlowDirector.ActiveLearningMode));
        if (consume == null || reset == null || mode == null)
        {
            failures.Add("Hazmat grab narration one-shot members could not be inspected.");
            return;
        }

        object previousMode = mode.GetValue(director);
        mode.SetValue(director, ScenarioDetailModal.PpeLearningMode.Education);
        reset.Invoke(director, null);
        bool first = (bool)consume.Invoke(director, null);
        bool second = (bool)consume.Invoke(director, null);
        reset.Invoke(director, null);
        bool nextSession = (bool)consume.Invoke(director, null);
        mode.SetValue(director, previousMode);

        if (!first || second || !nextSession)
            failures.Add("Hazmat grab narration must play once per education mode session.");
    }

    static void ValidateScenarioMismatchPanelSfx(
        Scene scene,
        PPEVoiceFlowDirector director,
        SerializedObject directorSerialized,
        PPEActionPanelController[] panels,
        List<string> failures)
    {
        AudioManager audioManager = FindAll<AudioManager>(scene).SingleOrDefault();
        if (audioManager == null)
        {
            failures.Add("A single scene AudioManager is required to validate mismatch SFX.");
            return;
        }

        SerializedProperty confined = directorSerialized.FindProperty(
            "m_ConfinedSpaceRequiredItemTypes");
        SerializedProperty leak = directorSerialized.FindProperty(
            "m_LeakResponseRequiredItemTypes");

        foreach (PPEActionPanelController panel in panels)
        {
            PPEItemIdentity identity = panel?.InspectionState?.PresentationBinding?.ItemIdentity;
            if (identity == null || identity.ItemType == PPEItemType.PackingTape)
                continue;

            bool allowedInConfined = ContainsItemType(confined, identity.ItemType);
            bool allowedInLeak = ContainsItemType(leak, identity.ItemType);
            if (allowedInConfined && allowedInLeak)
                continue;

            SerializedObject panelSerialized = new(panel);
            if (panelSerialized.FindProperty("voiceFlowDirector")?.objectReferenceValue != director)
            {
                failures.Add(
                    $"Scenario-mismatch PPE '{panel.name}' must reference the scene director.");
            }

            string mismatchSfxId = panelSerialized.FindProperty("wrongFeedbackSfxId")?.stringValue;
            if (string.IsNullOrWhiteSpace(mismatchSfxId) ||
                !HasEnabledSfx(audioManager, mismatchSfxId))
            {
                failures.Add(
                    $"Scenario-mismatch PPE '{panel.name}' needs an enabled wrong-feedback SFX.");
            }
        }
    }

    static bool ContainsItemType(SerializedProperty array, PPEItemType itemType)
    {
        for (int index = 0; array != null && index < array.arraySize; index++)
        {
            if (array.GetArrayElementAtIndex(index).enumValueIndex == (int)itemType)
                return true;
        }

        return false;
    }

    static bool HasEnabledSfx(AudioManager audioManager, string sfxId)
    {
        SerializedProperty sfx = new SerializedObject(audioManager).FindProperty("m_Sfx");
        for (int index = 0; sfx != null && index < sfx.arraySize; index++)
        {
            SerializedProperty sound = sfx.GetArrayElementAtIndex(index);
            if (sound.FindPropertyRelative("id")?.stringValue != sfxId)
                continue;

            return sound.FindPropertyRelative("clip")?.objectReferenceValue != null &&
                sound.FindPropertyRelative("enabled")?.boolValue == true &&
                sound.FindPropertyRelative("volume")?.floatValue > 0f;
        }

        return false;
    }

    static void ValidateWelcomeNarration(
        PPEVoiceFlowDirector director,
        List<string> failures)
    {
        const string firstPath =
            "Assets/Audio/Voice/0_Intro/VO_PPE_INTRO_001_Welcome_New.mp3";
        const string returningPath =
            "Assets/Audio/Voice/0_Intro/VO_PPE_INTRO_002_Welcome_Old.mp3";
        SerializedObject serialized = new(director);

        ValidateAudioReference(
            serialized.FindProperty("m_FirstMetaUserWelcomeClip"),
            firstPath,
            "m_FirstMetaUserWelcomeClip",
            failures);
        ValidateAudioReference(
            serialized.FindProperty("m_ReturningMetaUserWelcomeClip"),
            returningPath,
            "m_ReturningMetaUserWelcomeClip",
            failures);

        SerializedProperty wait = serialized.FindProperty("m_MetaWelcomeIdentityWaitSeconds");
        if (wait == null || wait.floatValue <= 0f)
            failures.Add("Meta Welcome identity wait must be authored above zero seconds.");

        SerializedProperty steps = serialized.FindProperty("m_VoiceSteps");
        SerializedProperty welcome = FindControllerStep(steps, "welcome");
        SerializedProperty clips = welcome?.FindPropertyRelative("clips");
        if (clips == null || clips.arraySize != 1)
        {
            failures.Add("The Welcome step must retain one first-user fallback clip.");
        }
        else
        {
            ValidateAudioReference(clips.GetArrayElementAtIndex(0), firstPath, "welcome.clips[0]", failures);
        }
    }

    static void ValidateEducationStartVoices(
        PPEVoiceFlowDirector director,
        List<string> failures)
    {
        const string movePath =
            "Assets/Audio/Voice/4_PPE/4_VO_PPE_EDU_001_PPE_MoveToPPE.mp3";
        const string startPath =
            "Assets/Audio/Voice/4_PPE/4_VO_PPE_EDU_002_PPE_Start.mp3";
        const string tablePath =
            "Assets/Audio/Voice/4_PPE/4_VO_PPE_EDU_003_Table.mp3";
        AudioClip expectedMove = AssetDatabase.LoadAssetAtPath<AudioClip>(movePath);
        AudioClip expectedStart = AssetDatabase.LoadAssetAtPath<AudioClip>(startPath);
        AudioClip expectedTable = AssetDatabase.LoadAssetAtPath<AudioClip>(tablePath);
        if (expectedMove == null || expectedStart == null || expectedTable == null)
        {
            failures.Add("The authored replacement EDU 001/002/003 voice assets could not be loaded.");
            return;
        }

        SerializedProperty steps = new SerializedObject(director).FindProperty("m_VoiceSteps");
        SerializedProperty educationSelected = null;
        SerializedProperty ppeArea = null;
        for (int index = 0; steps != null && index < steps.arraySize; index++)
        {
            SerializedProperty candidate = steps.GetArrayElementAtIndex(index);
            string stepId = candidate.FindPropertyRelative("stepId")?.stringValue;
            if (stepId == "education_selected")
                educationSelected = candidate;
            else if (stepId == "ppe_area")
                ppeArea = candidate;
        }

        SerializedProperty educationClips = educationSelected?.FindPropertyRelative("clips");
        if (educationClips == null || educationClips.arraySize != 1 ||
            educationClips.GetArrayElementAtIndex(0).objectReferenceValue != expectedMove)
        {
            failures.Add("The education_selected step must play replacement EDU 001 MoveToPPE.");
        }

        SerializedProperty ppeAreaClips = ppeArea?.FindPropertyRelative("clips");
        if (ppeAreaClips == null || ppeAreaClips.arraySize != 2 ||
            ppeAreaClips.GetArrayElementAtIndex(0).objectReferenceValue != expectedStart ||
            ppeAreaClips.GetArrayElementAtIndex(1).objectReferenceValue != expectedTable)
        {
            failures.Add("The ppe_area step must play replacement EDU 002 PPE Start followed by EDU 003 Table.");
        }

        if (ppeArea?.FindPropertyRelative("waitForSignal")?.boolValue != true)
            failures.Add("The ppe_area step must remain active after its narration instead of advancing to Completed.");
    }

    static void ValidateFinaleAndExitWiring(
        Scene scene,
        PPEVoiceFlowDirector director,
        List<string> failures)
    {
        PPEFinaleController finale = FindSingle<PPEFinaleController>(scene, failures);
        PPEExitTeleportMarkerRelay relay = FindAll<PPEExitTeleportMarkerRelay>(scene).SingleOrDefault();
        if (finale == null || relay == null)
        {
            if (relay == null)
                failures.Add("A single authored PPEExitTeleportMarkerRelay is required.");
            return;
        }

        SerializedObject finaleSerialized = new(finale);
        string[] mirrorReferences =
        {
            "m_XrOrigin",
            "m_MirrorObservationPoint",
            "m_ObservationGaugeRoot",
            "m_ObservationGaugeFill",
        };
        foreach (string propertyName in mirrorReferences)
        {
            if (finaleSerialized.FindProperty(propertyName)?.objectReferenceValue == null)
                failures.Add($"PPEFinaleController is missing authored mirror reference '{propertyName}'.");
        }

        string finaleSource = File.ReadAllText("Assets/Scripts/PPEFinaleController.cs");
        if (finaleSource.Contains("IsLookingAtMirror()", StringComparison.Ordinal))
        {
            failures.Add(
                "Mirror observation progress must continue after looking away while the player remains in range.");
        }

        SerializedObject relaySerialized = new(relay);
        if (relaySerialized.FindProperty("m_ReturnOnWalkEnter")?.boolValue != true)
            failures.Add("Locomotion exit relay must use authored walk-entry return.");
        if (relaySerialized.FindProperty("m_FinaleController")?.objectReferenceValue != finale)
            failures.Add("Exit relay must reference the scene PPEFinaleController.");
        if (relaySerialized.FindProperty("m_Player")?.objectReferenceValue == null ||
            relaySerialized.FindProperty("m_EnterVolume")?.objectReferenceValue == null)
        {
            failures.Add("Exit relay requires authored player and enter-volume references.");
        }

        SerializedObject directorSerialized = new(director);
        if (directorSerialized.FindProperty("m_ExitTeleportMarker")?.objectReferenceValue != relay.gameObject)
            failures.Add("PPEVoiceFlowDirector exit marker must reference the walk-entry relay GameObject.");
    }

    static void ValidateControllerGuideVisuals(
        Scene scene,
        PPEVoiceFlowDirector director,
        List<string> failures)
    {
        Transform[] guides = FindAll<Transform>(scene)
            .Where(transform => transform.name == "ControllerGuide")
            .ToArray();
        if (guides.Length != 1)
        {
            failures.Add($"Expected one ControllerGuide, found {guides.Length}.");
            return;
        }

        Transform guide = guides[0];
        Transform context = guide.Find("Context");
        Transform triggerController = context?.Find("1_Ctrl_Trigger");
        Transform gripController = context?.Find("2_Ctrl_Grip");
        Transform joystickController = context?.Find("3_Ctrl_Joystick");
        Transform ray = context?.Find("1_Ray");
        Transform marker = context?.Find("2_Marker");
        Transform exitMarker = context?.Find("3_Exit_Marker");
        if (context == null || triggerController == null || gripController == null ||
            joystickController == null || ray == null || marker == null || exitMarker == null)
        {
            failures.Add(
                "ControllerGuide/Context must contain the authored controller images and " +
                "1_Ray, 2_Marker, and 3_Exit_Marker guide groups.");
            return;
        }

        if (context.Find("3_Ray_T") != null)
            failures.Add("ControllerGuide must not restore the removed 3_Ray_T group.");
        if (FindAll<Transform>(scene).Any(transform =>
                transform.name == "Ray_T_B" || transform.name == "Ray_T_R"))
        {
            failures.Add("ControllerGuide must not restore the removed Ray_T_B/R images.");
        }

        ValidateAuthoredActive(guide, false, failures);
        ValidateAuthoredActive(context, true, failures);
        ValidateAuthoredActive(triggerController, true, failures);
        ValidateAuthoredActive(gripController, false, failures);
        ValidateAuthoredActive(joystickController, false, failures);
        ValidateAuthoredActive(ray, false, failures);
        ValidateAuthoredActive(marker, false, failures);
        ValidateAuthoredActive(exitMarker, true, failures);

        ValidateGuideImage(triggerController, "Assets/UIs/Guide/Controller_tri.png", failures);
        ValidateGuideImage(gripController, "Assets/UIs/Guide/Controller_gri.png", failures);
        ValidateGuideImage(joystickController, "Assets/UIs/Guide/Controller_joy.png", failures);
        ValidateControllerGuideArtworkUniformity(failures);
        ValidateGuideChild(ray, "Card", true, "Assets/UIs/Guide/Ray.png", failures);
        ValidateGuideChild(ray, "Panel", false, null, failures);
        ValidateGuideChild(marker, "Item", true, "Assets/UIs/Guide/Marker_Item.png", failures);
        ValidateGuideChild(marker, "Place", false, null, failures);
        ValidateGuideChild(exitMarker, "Item", true, "Assets/UIs/Guide/ExitMarker.png", failures);
        ValidateGuideChild(exitMarker, "Place", false, null, failures);

        SerializedObject serialized = new(director);
        ValidateReference(serialized, "m_ControllerGuideRoot", guide.gameObject, failures);
        ValidateReference(serialized, "m_ControllerRayStep", ray.gameObject, failures);
        ValidateReference(serialized, "m_ControllerMarkerStep", marker.gameObject, failures);
        ValidateReference(serialized, "m_ControllerRayTStep", exitMarker.gameObject, failures);
        ValidateReference(serialized, "m_ControllerPanelStep", null, failures);

        SerializedProperty educationSteps = serialized.FindProperty("m_ControllerEduVoiceSteps");
        SerializedProperty simpleSteps = serialized.FindProperty("m_ControllerSimpVoiceSteps");
        ValidateControllerStep(
            educationSteps, "controller_edu_trigger", ray.gameObject, 2,
            triggerController.gameObject, failures);
        ValidateControllerStep(
            educationSteps, "controller_edu_grip", marker.gameObject, 1,
            gripController.gameObject, failures);
        ValidateControllerStep(
            educationSteps, "controller_edu_joystick", exitMarker.gameObject, 1,
            joystickController.gameObject, failures);
        ValidateControllerStep(
            simpleSteps, "controller_simp_trigger", ray.gameObject, 2,
            triggerController.gameObject, failures);
        ValidateControllerStep(
            simpleSteps, "controller_simp_grip", marker.gameObject, 1,
            gripController.gameObject, failures);
        ValidateControllerStep(
            simpleSteps, "controller_simp_joystick", exitMarker.gameObject, 2,
            joystickController.gameObject, failures);

        ValidateDetailedControllerInputStep(
            educationSteps,
            "controller_edu_trigger",
            PPEVoiceFlowDirector.ControllerGuideInput.Trigger,
            new[]
            {
                "Assets/Audio/Voice/1_2_ContDetail/VO_PPE_CTRL_DETAIL_001_Start.mp3",
                "Assets/Audio/Voice/1_2_ContDetail/VO_PPE_CTRL_DETAIL_002_RayTrigger.mp3",
            },
            "Assets/Audio/Voice/1_2_ContDetail/VO_PPE_CTRL_DETAIL_003_RayTrigger_Wrong.mp3",
            "Assets/Audio/Voice/1_2_ContDetail/VO_PPE_CTRL_DETAIL_008_Correct_Input.mp3",
            null,
            failures);
        ValidateDetailedControllerInputStep(
            educationSteps,
            "controller_edu_grip",
            PPEVoiceFlowDirector.ControllerGuideInput.Grip,
            new[]
            {
                "Assets/Audio/Voice/1_2_ContDetail/VO_PPE_CTRL_DETAIL_004_GripGrab_Release.mp3",
            },
            "Assets/Audio/Voice/1_2_ContDetail/VO_PPE_CTRL_DETAIL_005_GripGrab_Releaser_Wrong.mp3",
            "Assets/Audio/Voice/1_2_ContDetail/VO_PPE_CTRL_DETAIL_008_Correct_Input.mp3",
            null,
            failures);
        ValidateDetailedControllerInputStep(
            educationSteps,
            "controller_edu_joystick",
            PPEVoiceFlowDirector.ControllerGuideInput.Joystick,
            new[]
            {
                "Assets/Audio/Voice/1_2_ContDetail/VO_PPE_CTRL_DETAIL_006_Joystick.mp3",
            },
            "Assets/Audio/Voice/1_2_ContDetail/VO_PPE_CTRL_DETAIL_007_Joystickr_Wrong.mp3",
            "Assets/Audio/Voice/1_2_ContDetail/VO_PPE_CTRL_DETAIL_008_Correct_Input.mp3",
            "Assets/Audio/Voice/1_2_ContDetail/VO_PPE_CTRL_DETAIL_009_GuideFollow.mp3",
            failures);
        ValidateSimpleControllerStepHasNoInputGate(
            simpleSteps, "controller_simp_trigger", failures);
        ValidateSimpleControllerStepHasNoInputGate(
            simpleSteps, "controller_simp_grip", failures);
        ValidateSimpleControllerStepHasNoInputGate(
            simpleSteps, "controller_simp_joystick", failures);
        ValidateControllerStepAudioClips(
            simpleSteps,
            "controller_simp_trigger",
            new[]
            {
                "Assets/Audio/Voice/1_1_ContSimp/VO_PPE_CTRL_SIMP_001_Start.mp3",
                "Assets/Audio/Voice/1_1_ContSimp/VO_PPE_CTRL_SIMP_002_RayTrigger.mp3",
            },
            failures);
        ValidateControllerStepAudioClips(
            simpleSteps,
            "controller_simp_grip",
            new[]
            {
                "Assets/Audio/Voice/1_1_ContSimp/VO_PPE_CTRL_SIMP_003_GripGrab_Release.mp3",
            },
            failures);
        ValidateControllerStepAudioClips(
            simpleSteps,
            "controller_simp_joystick",
            new[]
            {
                "Assets/Audio/Voice/1_1_ContSimp/VO_PPE_CTRL_SIMP_004_Joystick.mp3",
                "Assets/Audio/Voice/1_1_ContSimp/VO_PPE_CTRL_SIMP_005_GuideFollow.mp3",
            },
            failures);
    }

    static void ValidateTemporaryKeyboardBypass(
        Scene scene,
        PPEVoiceFlowDirector director,
        List<string> failures)
    {
        SerializedObject serialized = new(director);
        SerializedProperty skipKeyboard = serialized.FindProperty("m_SkipKeyboardNameInput");
        if (skipKeyboard == null || !skipKeyboard.boolValue)
            failures.Add("The temporary keyboard-name-input bypass must remain enabled in the authored scene.");

        GameObject keyboard = serialized.FindProperty("m_KeyboardPresentationRoot")?.objectReferenceValue
            as GameObject;
        if (keyboard == null)
        {
            failures.Add("The bypassed keyboard must retain its authored presentation-root reference.");
        }
        else if (keyboard.activeSelf)
        {
            failures.Add("The bypassed keyboard presentation root must remain authored inactive.");
        }

        Transform mini = FindAll<Transform>(scene)
            .FirstOrDefault(transform => transform.name == "ControllerGuide_mini");
        Transform hint = mini?.Find("Context/Controller Education Hint");
        if (hint == null)
        {
            failures.Add(
                "ControllerGuide_mini/Context requires the authored Controller Education Hint.");
            return;
        }

        ValidateAuthoredActive(hint, true, failures);
        Transform aButtonVisual = hint.Find("A Button Visual");
        if (aButtonVisual == null)
        {
            failures.Add("The mini controller-education hint requires its authored A Button Visual child.");
        }
        else
        {
            ValidateAuthoredActive(aButtonVisual, false, failures);
        }

        TMP_Text educationLabel = hint.Find("Controller Education Label")?.GetComponent<TMP_Text>();
        TMP_Text toggleHintText = mini.Find("Context/Use")?.GetComponent<TMP_Text>();
        if (educationLabel == null || educationLabel.text != "A버튼 : 컨트롤러 상세 교육")
        {
            failures.Add(
                "The mini controller-education hint requires the authored 'A버튼 : 컨트롤러 상세 교육' label.");
        }
        else
        {
            if (toggleHintText == null ||
                Mathf.Abs(educationLabel.fontSize - toggleHintText.fontSize) > 0.01f)
            {
                failures.Add(
                    "The controller-education label must use the same authored font size as the joystick-click hint.");
            }

            if (educationLabel.horizontalAlignment != HorizontalAlignmentOptions.Left)
                failures.Add("The controller-education label must remain authored left-aligned.");

            RectTransform labelRect = educationLabel.rectTransform;
            if (labelRect.sizeDelta.x < 500f ||
                labelRect.sizeDelta.y < 70f)
            {
                failures.Add(
                    "The controller-education label RectTransform must retain at least its authored 500x70 text area.");
            }

            RectTransform hintRect = hint as RectTransform;
            RectTransform titleRect = mini.Find("Context/Label") as RectTransform;
            if (hintRect == null || titleRect == null)
            {
                failures.Add("The mini controller guide requires its authored title and education-hint RectTransforms.");
            }
            else
            {
                float educationLeft = hintRect.anchoredPosition.x + labelRect.anchoredPosition.x -
                    labelRect.sizeDelta.x * labelRect.pivot.x;
                float titleLeft = titleRect.anchoredPosition.x -
                    titleRect.sizeDelta.x * titleRect.pivot.x;
                if (Mathf.Abs(educationLeft - titleLeft) > 0.01f)
                {
                    failures.Add(
                        "The A-button education label must start on the same authored left edge as the controller-guide title.");
                }
            }
        }

        if (hint.GetComponentsInChildren<Selectable>(true).Length > 0 ||
            hint.GetComponentsInChildren<PPEControllerEducationEntry>(true).Length > 0)
        {
            failures.Add(
                "The mini controller-education hint must remain display-only and must not add another input consumer.");
        }

        Graphic raycastGraphic = hint.GetComponentsInChildren<Graphic>(true)
            .FirstOrDefault(graphic => graphic.raycastTarget);
        if (raycastGraphic != null)
        {
            failures.Add(
                $"The mini controller-education hint Graphic '{raycastGraphic.name}' must not receive UI raycasts.");
        }

        RectTransform context = mini.Find("Context") as RectTransform;
        RectTransform controllerImage = mini.Find("Context/Controller_Image") as RectTransform;
        Image controllerGraphic = controllerImage?.GetComponent<Image>();
        if (context == null || controllerImage == null || controllerGraphic?.sprite == null)
        {
            failures.Add("The mini controller guide requires its authored Context/Controller_Image sprite.");
        }
        else
        {
            string spritePath = AssetDatabase.GetAssetPath(controllerGraphic.sprite);
            Texture2D texture = controllerGraphic.sprite.texture;
            if (spritePath != "Assets/UIs/Guide/controller.png")
                failures.Add($"The mini controller guide sprite is '{spritePath}', not the authored controller.png.");
            if (texture == null || texture.width != texture.height)
                failures.Add("The mini controller guide source must retain its square white canvas.");
            if (Mathf.Abs(controllerImage.sizeDelta.x - 100f) > 0.01f ||
                Mathf.Abs(controllerImage.sizeDelta.y - 100f) > 0.01f)
            {
                failures.Add("The mini Controller_Image authored RectTransform must remain 100x100.");
            }
            if (Mathf.Abs(controllerImage.anchoredPosition.y - -10f) > 0.01f)
                failures.Add("The mini Controller_Image must retain its raised authored Y position (-10).");
        }

        RectTransform useHint = mini.Find("Context/Use") as RectTransform;
        TMP_Text useText = useHint?.GetComponent<TMP_Text>();
        if (useHint == null || useText == null ||
            !useText.text.Contains("컨트롤러 가이드 온/오프"))
        {
            failures.Add("The mini guide requires its authored controller-guide toggle hint.");
        }
        else
        {
            float bottomMargin = useHint.anchoredPosition.y - useHint.sizeDelta.y * 0.5f -
                (context.anchoredPosition.y - context.sizeDelta.y * 0.5f);
            if (useHint.sizeDelta.y < 70f || bottomMargin < 25f)
            {
                failures.Add(
                    "The mini controller-guide toggle hint RectTransform is too short or too close to the panel bottom.");
            }
        }
    }

    static void ValidateControllerStepAudioClips(
        SerializedProperty steps,
        string stepId,
        string[] expectedClipPaths,
        List<string> failures)
    {
        SerializedProperty step = FindControllerStep(steps, stepId);
        SerializedProperty clips = step?.FindPropertyRelative("clips");
        if (clips == null || clips.arraySize != expectedClipPaths.Length)
        {
            failures.Add(
                $"Simple controller step '{stepId}' must have {expectedClipPaths.Length} authored clip(s).");
            return;
        }

        for (int index = 0; index < expectedClipPaths.Length; index++)
        {
            ValidateAudioReference(
                clips.GetArrayElementAtIndex(index),
                expectedClipPaths[index],
                $"{stepId}.clips[{index}]",
                failures);
        }
    }

    static void ValidateDetailedControllerInputStep(
        SerializedProperty steps,
        string stepId,
        PPEVoiceFlowDirector.ControllerGuideInput expectedInput,
        string[] expectedClipPaths,
        string wrongClipPath,
        string correctClipPath,
        string completionClipPath,
        List<string> failures)
    {
        SerializedProperty step = FindControllerStep(steps, stepId);
        if (step == null)
        {
            failures.Add($"Missing detailed controller step '{stepId}'.");
            return;
        }

        SerializedProperty clips = step.FindPropertyRelative("clips");
        if (clips == null || clips.arraySize != expectedClipPaths.Length)
        {
            failures.Add(
                $"Detailed controller step '{stepId}' must have {expectedClipPaths.Length} explanation clip(s).");
        }
        else
        {
            for (int index = 0; index < expectedClipPaths.Length; index++)
            {
                ValidateAudioReference(
                    clips.GetArrayElementAtIndex(index),
                    expectedClipPaths[index],
                    $"{stepId}.clips[{index}]",
                    failures);
            }
        }

        SerializedProperty input = step.FindPropertyRelative("controllerExpectedInput");
        if (input == null || input.enumValueIndex != (int)expectedInput)
        {
            failures.Add(
                $"Detailed controller step '{stepId}' must wait for {expectedInput} input.");
        }

        ValidateAudioReference(
            step.FindPropertyRelative("controllerWrongInputClip"),
            wrongClipPath,
            $"{stepId}.controllerWrongInputClip",
            failures);
        ValidateAudioReference(
            step.FindPropertyRelative("controllerCorrectInputClip"),
            correctClipPath,
            $"{stepId}.controllerCorrectInputClip",
            failures);
        ValidateAudioReference(
            step.FindPropertyRelative("controllerCompletionClip"),
            completionClipPath,
            $"{stepId}.controllerCompletionClip",
            failures);
    }

    static void ValidateSimpleControllerStepHasNoInputGate(
        SerializedProperty steps,
        string stepId,
        List<string> failures)
    {
        SerializedProperty step = FindControllerStep(steps, stepId);
        if (step == null)
            return;

        SerializedProperty input = step.FindPropertyRelative("controllerExpectedInput");
        bool hasInput = input != null &&
            input.enumValueIndex != (int)PPEVoiceFlowDirector.ControllerGuideInput.None;
        bool hasFeedback = step.FindPropertyRelative("controllerWrongInputClip")?.objectReferenceValue != null ||
            step.FindPropertyRelative("controllerCorrectInputClip")?.objectReferenceValue != null ||
            step.FindPropertyRelative("controllerCompletionClip")?.objectReferenceValue != null;
        if (hasInput || hasFeedback)
            failures.Add($"Simple controller step '{stepId}' must retain its ungated narration flow.");
    }

    static void ValidateAudioReference(
        SerializedProperty property,
        string expectedPath,
        string label,
        List<string> failures)
    {
        AudioClip expected = string.IsNullOrEmpty(expectedPath)
            ? null
            : AssetDatabase.LoadAssetAtPath<AudioClip>(expectedPath);
        if (!string.IsNullOrEmpty(expectedPath) && expected == null)
        {
            failures.Add($"Required controller voice asset is missing: '{expectedPath}'.");
            return;
        }

        if (property == null || property.objectReferenceValue != expected)
            failures.Add($"Controller voice reference '{label}' must be '{expectedPath ?? "null"}'.");
    }

    static SerializedProperty FindControllerStep(SerializedProperty steps, string stepId)
    {
        for (int index = 0; steps != null && index < steps.arraySize; index++)
        {
            SerializedProperty candidate = steps.GetArrayElementAtIndex(index);
            if (candidate.FindPropertyRelative("stepId")?.stringValue == stepId)
                return candidate;
        }

        return null;
    }

    static void ValidateControllerStep(
        SerializedProperty steps,
        string stepId,
        GameObject expectedVisual,
        int expectedCount,
        GameObject expectedCompanion,
        List<string> failures)
    {
        SerializedProperty step = FindControllerStep(steps, stepId);

        SerializedProperty visuals = step?.FindPropertyRelative("controllerGuideVisuals");
        if (visuals == null || visuals.arraySize != expectedCount)
        {
            failures.Add($"Controller guide step '{stepId}' must have {expectedCount} visual reference(s).");
            return;
        }

        for (int index = 0; index < visuals.arraySize; index++)
        {
            if (visuals.GetArrayElementAtIndex(index).objectReferenceValue != expectedVisual)
            {
                failures.Add(
                    $"Controller guide step '{stepId}' visual {index} must reference '{expectedVisual.name}'.");
            }
        }

        UnityEngine.Object companion = step.FindPropertyRelative("controllerGuideCompanionVisual")
            ?.objectReferenceValue;
        if (companion != expectedCompanion)
        {
            failures.Add(
                $"Controller guide step '{stepId}' companion must reference '{expectedCompanion.name}'.");
        }
    }

    static void ValidateReference(
        SerializedObject serialized,
        string propertyName,
        UnityEngine.Object expected,
        List<string> failures)
    {
        UnityEngine.Object actual = serialized.FindProperty(propertyName)?.objectReferenceValue;
        if (actual != expected)
        {
            failures.Add(
                $"PPEVoiceFlowDirector.{propertyName} must reference " +
                $"'{(expected != null ? expected.name : "null")}'.");
        }
    }

    static void ValidateGuideChild(
        Transform parent,
        string childName,
        bool expectedActive,
        string expectedSpritePath,
        List<string> failures)
    {
        Transform child = parent.Find(childName);
        if (child == null)
        {
            failures.Add($"Controller guide group '{parent.name}' is missing child '{childName}'.");
            return;
        }

        ValidateAuthoredActive(child, expectedActive, failures);
        ValidateGuideImage(child, expectedSpritePath, failures);
    }

    static void ValidateControllerGuideArtworkUniformity(List<string> failures)
    {
        string[] paths =
        {
            "Assets/UIs/Guide/Controller_tri.png",
            "Assets/UIs/Guide/Controller_gri.png",
            "Assets/UIs/Guide/Controller_joy.png",
        };

        RectInt? referenceBounds = null;
        string referencePath = null;
        foreach (string path in paths)
        {
            Texture2D source = new(2, 2, TextureFormat.RGBA32, false, true);
            try
            {
                if (!source.LoadImage(File.ReadAllBytes(path), false))
                {
                    failures.Add($"Controller guide artwork could not be decoded: '{path}'.");
                    continue;
                }

                Color32[] pixels = source.GetPixels32();
                int minX = source.width;
                int minY = source.height;
                int maxX = -1;
                int maxY = -1;
                for (int y = 0; y < source.height; y++)
                {
                    int row = y * source.width;
                    for (int x = 0; x < source.width; x++)
                    {
                        Color32 pixel = pixels[row + x];
                        if (pixel.r >= 220 && pixel.g >= 220 && pixel.b >= 220)
                            continue;

                        minX = Mathf.Min(minX, x);
                        minY = Mathf.Min(minY, y);
                        maxX = Mathf.Max(maxX, x);
                        maxY = Mathf.Max(maxY, y);
                    }
                }

                if (maxX < minX || maxY < minY)
                {
                    failures.Add($"Controller guide artwork is empty: '{path}'.");
                    continue;
                }

                RectInt bounds = new(minX, minY, maxX - minX + 1, maxY - minY + 1);
                if (referenceBounds == null)
                {
                    referenceBounds = bounds;
                    referencePath = path;
                    continue;
                }

                RectInt reference = referenceBounds.Value;
                float centerX = bounds.xMin + (bounds.width - 1) * 0.5f;
                float centerY = bounds.yMin + (bounds.height - 1) * 0.5f;
                float referenceCenterX = reference.xMin + (reference.width - 1) * 0.5f;
                float referenceCenterY = reference.yMin + (reference.height - 1) * 0.5f;
                if (Mathf.Abs(bounds.width - reference.width) > 3f ||
                    Mathf.Abs(bounds.height - reference.height) > 3f ||
                    Mathf.Abs(centerX - referenceCenterX) > 3f ||
                    Mathf.Abs(centerY - referenceCenterY) > 3f)
                {
                    failures.Add(
                        $"Controller guide artwork bounds differ between '{referencePath}' and '{path}'. " +
                        "All trigger/grip/joystick variants must retain the same authored visual scale and center.");
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(source);
            }
        }
    }

    static void ValidateGuideImage(
        Transform transform,
        string expectedSpritePath,
        List<string> failures)
    {
        UnityEngine.UI.Image image = transform.GetComponent<UnityEngine.UI.Image>();
        string actualPath = image != null && image.sprite != null
            ? AssetDatabase.GetAssetPath(image.sprite)
            : null;
        if (actualPath != expectedSpritePath)
        {
            failures.Add(
                $"Controller guide '{transform.name}' sprite is '{actualPath ?? "null"}', " +
                $"expected '{expectedSpritePath ?? "null"}'.");
        }

        if (image != null && !image.preserveAspect)
            failures.Add($"Controller guide '{transform.name}' must preserve the baked PNG aspect ratio.");

        bool isControllerDiagram = expectedSpritePath == "Assets/UIs/Guide/Controller_tri.png" ||
            expectedSpritePath == "Assets/UIs/Guide/Controller_gri.png" ||
            expectedSpritePath == "Assets/UIs/Guide/Controller_joy.png";
        if (isControllerDiagram && transform is RectTransform rectTransform && image?.sprite != null)
        {
            Texture2D texture = image.sprite.texture;
            if (texture == null || texture.width != texture.height)
            {
                failures.Add(
                    $"Controller guide '{transform.name}' source must retain its square white canvas.");
            }

            if (Mathf.Abs(rectTransform.sizeDelta.x - 100f) > 0.01f ||
                Mathf.Abs(rectTransform.sizeDelta.y - 100f) > 0.01f)
            {
                failures.Add(
                    $"Controller guide '{transform.name}' authored RectTransform must remain 100x100.");
            }

            if (Mathf.Abs(rectTransform.anchoredPosition.x - -166f) > 0.01f)
            {
                failures.Add(
                    $"Controller guide '{transform.name}' authored X position must remain -166.");
            }

            float effectiveAuthoredWidth =
                rectTransform.sizeDelta.x * Mathf.Abs(rectTransform.localScale.x);
            if (Mathf.Abs(effectiveAuthoredWidth - 500f) > 0.5f)
            {
                failures.Add(
                    $"Controller guide '{transform.name}' effective authored width " +
                    $"({effectiveAuthoredWidth:F1}) must remain 500 panel units.");
            }
        }
    }

    static void ValidateAuthoredActive(
        Transform transform,
        bool expected,
        List<string> failures)
    {
        if (transform.gameObject.activeSelf != expected)
        {
            failures.Add(
                $"Controller guide '{transform.name}' authored activeSelf must be {expected}.");
        }
    }

    static void ValidateBodyColliderProximity(List<string> failures)
    {
        MethodInfo pointCheck = typeof(PPEActionPanelController).GetMethod(
            "IsPointWithinColliderRange",
            BindingFlags.Static | BindingFlags.NonPublic);
        MethodInfo segmentCheck = typeof(PPEActionPanelController).GetMethod(
            "IsSegmentWithinColliderRange",
            BindingFlags.Static | BindingFlags.NonPublic);
        if (pointCheck == null || segmentCheck == null)
        {
            failures.Add("Body-collider proximity helpers could not be inspected.");
            return;
        }

        GameObject probe = new("PPE Body Proximity Validation Probe");
        try
        {
            BoxCollider collider = probe.AddComponent<BoxCollider>();
            collider.size = Vector3.one;
            Physics.SyncTransforms();

            bool pointInside = (bool)pointCheck.Invoke(
                null,
                new object[] { collider, Vector3.zero, 0.01f });
            bool segmentInside = (bool)segmentCheck.Invoke(
                null,
                new object[]
                {
                    collider,
                    new Vector3(0f, -1f, 0f),
                    new Vector3(0f, 1f, 0f),
                    0.01f,
                });
            bool pointOutside = (bool)pointCheck.Invoke(
                null,
                new object[] { collider, new Vector3(1f, 0f, 0f), 0.1f });
            if (!pointInside || !segmentInside || pointOutside)
            {
                failures.Add(
                    "A grabbed PPE collider touching or containing the body target must count as in range.");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(probe);
        }
    }

    static void ValidateChecklistWorkPlanReset(
        PPEVoiceFlowDirector director,
        PPEEducationWearChecklist checklist,
        List<string> failures)
    {
        Type checklistType = typeof(PPEEducationWearChecklist);
        FieldInfo approvedField = checklistType.GetField("approvedItemTypes", InstancePrivate);
        FieldInfo observedField = checklistType.GetField("observedWorkPlan", InstancePrivate);
        MethodInfo reset = checklistType.GetMethod(
            "ResetChecksWhenWorkPlanChanges",
            InstancePrivate);
        PropertyInfo activeWorkPlan = typeof(PPEVoiceFlowDirector).GetProperty(
            nameof(PPEVoiceFlowDirector.ActiveWorkPlan));

        if (approvedField == null || observedField == null || reset == null ||
            activeWorkPlan == null)
        {
            failures.Add("Checklist work-plan reset members could not be inspected.");
            return;
        }

        HashSet<PPEItemType> approved =
            (HashSet<PPEItemType>)approvedField.GetValue(checklist);
        approved.Add(PPEItemType.HazmatSuit);
        approved.Add(PPEItemType.RubberBootLeft);
        observedField.SetValue(checklist, ScenarioDetailModal.PpeWorkPlan.ConfinedSpace);
        activeWorkPlan.SetValue(director, ScenarioDetailModal.PpeWorkPlan.LeakResponse);
        reset.Invoke(checklist, null);
        if (approved.Count != 0)
            failures.Add("Changing work plan must clear prior checklist approvals.");
    }

    static void ValidateHazmatAlreadyEquippedPriority(List<string> failures)
    {
        string panelSource = File.ReadAllText("Assets/Scripts/PPEActionPanelController.cs");
        int method = panelSource.IndexOf("void ResolveUseChoice()", StringComparison.Ordinal);
        int preConditionRejection = panelSource.IndexOf(
            "voiceFlowDirector.RejectUseBeforeConditionCheck(this)",
            method,
            StringComparison.Ordinal);
        int condition = panelSource.IndexOf(
            "inspectionState.CurrentCondition != PPEItemCondition.Clean",
            method,
            StringComparison.Ordinal);
        if (method < 0 || preConditionRejection < method || condition < preConditionRejection)
        {
            failures.Add(
                "Already-equipped and scenario-mismatch rejection must run before the candidate condition check.");
        }

        string directorSource = File.ReadAllText("Assets/Scripts/PPEVoiceFlowDirector.cs");
        int rejectionMethod = directorSource.IndexOf(
            "public bool RejectUseBeforeConditionCheck",
            StringComparison.Ordinal);
        int approvalMethod = directorSource.IndexOf(
            "public bool CanApprovePpeUse",
            rejectionMethod,
            StringComparison.Ordinal);
        int workPlanCheck = directorSource.IndexOf(
            "!IsPpeTypeAllowedForActiveWorkPlan(itemType.Value)",
            rejectionMethod,
            StringComparison.Ordinal);
        int workPlanVoice = directorSource.IndexOf(
            "RejectUseWithWrongSfx(panel, m_WorkPlanMismatchVoice)",
            workPlanCheck,
            StringComparison.Ordinal);
        if (rejectionMethod < 0 || approvalMethod < 0 ||
            workPlanCheck < rejectionMethod || workPlanCheck >= approvalMethod ||
            workPlanVoice < workPlanCheck || workPlanVoice >= approvalMethod)
        {
            failures.Add(
                "Scenario mismatch must select EDU 205 in the pre-condition rejection path.");
        }
    }

    static void ValidateFootstepInputOwnership(Scene scene, List<string> failures)
    {
        PPEConfigurableDynamicMoveProvider moveProvider =
            FindSingle<PPEConfigurableDynamicMoveProvider>(scene, failures);
        AudioManager audioManager = FindSingle<AudioManager>(scene, failures);
        if (moveProvider == null || audioManager == null)
            return;

        SerializedObject moveSerialized = new(moveProvider);
        string sfxId = moveSerialized.FindProperty("m_FootstepSfxId")?.stringValue;
        AudioSource footstepSource =
            moveSerialized.FindProperty("m_FootstepSource")?.objectReferenceValue as AudioSource;
        if (string.IsNullOrWhiteSpace(sfxId))
            failures.Add("The locomotion input owner is missing its footstep SFX id.");

        SerializedObject audioSerialized = new(audioManager);
        AudioSource sharedSfxSource =
            audioSerialized.FindProperty("m_SfxSource")?.objectReferenceValue as AudioSource;
        if (footstepSource == null)
            failures.Add("The move provider requires a dedicated Footstep AudioSource.");
        else
        {
            if (footstepSource == sharedSfxSource)
                failures.Add("Footsteps must not use the shared SFX AudioSource.");
            if (footstepSource.playOnAwake || !Mathf.Approximately(footstepSource.pitch, 1f))
                failures.Add("The dedicated Footstep source must not play on awake and must keep pitch 1.");
        }

        SerializedProperty sfx = audioSerialized.FindProperty("m_Sfx");
        bool hasEnabledFootstep = false;
        for (int index = 0; sfx != null && index < sfx.arraySize; index++)
        {
            SerializedProperty sound = sfx.GetArrayElementAtIndex(index);
            if (sound.FindPropertyRelative("id")?.stringValue != sfxId)
                continue;

            hasEnabledFootstep = sound.FindPropertyRelative("clip")?.objectReferenceValue != null
                && sound.FindPropertyRelative("enabled")?.boolValue == true
                && sound.FindPropertyRelative("volume")?.floatValue > 0f;
            break;
        }

        if (!hasEnabledFootstep)
            failures.Add($"AudioManager SFX '{sfxId}' must have an enabled clip and positive volume.");

        string moveSource = File.ReadAllText(
            "Assets/Scripts/PPEConfigurableDynamicMoveProvider.cs");
        string idleSource = File.ReadAllText("Assets/Scripts/PPEIdleLocomotionAnimator.cs");
        if (!moveSource.Contains("UpdateFootsteps(hasLocomotionInput)", StringComparison.Ordinal))
            failures.Add("The move provider must drive footsteps from locomotion input.");
        if (!moveSource.Contains(
                "leftInput != Vector2.zero || rightInput != Vector2.zero",
                StringComparison.Ordinal))
        {
            failures.Add("Any non-zero left or right locomotion input must drive footsteps.");
        }
        if (!moveSource.Contains(
                "PlayLoopingSfx(m_FootstepSfxId, m_FootstepSource)",
                StringComparison.Ordinal))
            failures.Add("Footsteps must use one dedicated constant-speed loop while input is held.");
        if (!moveSource.Contains("StopFootsteps();", StringComparison.Ordinal))
            failures.Add("Releasing locomotion input must stop the Footstep loop immediately.");

        string audioManagerSource = File.ReadAllText("Assets/Scripts/AudioManager.cs");
        if (!audioManagerSource.Contains(
                "public bool PlayLoopingSfx(string id, AudioSource targetSource)",
                StringComparison.Ordinal)
            || !audioManagerSource.Contains(
                "public void StopLoopingSfx(AudioSource targetSource)",
                StringComparison.Ordinal))
        {
            failures.Add("AudioManager must own start/stop of the dedicated looping SFX source.");
        }
        if (idleSource.Contains("PlaySfx(", StringComparison.Ordinal))
            failures.Add("The optional player model animator must not own footstep playback.");
    }

    static void ValidateHeadRelativeLocomotion(Scene scene, List<string> failures)
    {
        PPEConfigurableDynamicMoveProvider moveProvider =
            FindSingle<PPEConfigurableDynamicMoveProvider>(scene, failures);
        GameObject activeOrigin = scene.GetRootGameObjects()
            .SingleOrDefault(root => root.name == "XR Origin (VR)");
        Camera activeCamera = activeOrigin != null
            ? activeOrigin.GetComponentsInChildren<Camera>(true)
                .SingleOrDefault(camera => camera.gameObject.name == "Main Camera")
            : null;
        if (moveProvider == null || activeCamera == null)
        {
            if (activeCamera == null)
                failures.Add("XR Origin (VR) requires one authored Main Camera for head-relative movement.");
            return;
        }

        SerializedObject move = new(moveProvider);
        if (move.FindProperty("m_HeadTransform")?.objectReferenceValue != activeCamera.transform)
            failures.Add("Move Provider head transform must reference XR Origin (VR)/Camera Offset/Main Camera.");
        if (move.FindProperty("m_ForwardSource")?.objectReferenceValue != activeCamera.transform)
            failures.Add("Move Provider authored forward source must reference the active XR camera.");
        if (move.FindProperty("m_LeftHandMovementDirection")?.enumValueIndex != 0 ||
            move.FindProperty("m_RightHandMovementDirection")?.enumValueIndex != 0)
        {
            failures.Add("Both joystick movement directions must be head-relative.");
        }
    }

    static T FindSingle<T>(Scene scene, List<string> failures) where T : Component
    {
        T[] matches = FindAll<T>(scene);
        if (matches.Length == 1)
            return matches[0];

        failures.Add($"Expected one {typeof(T).Name}, found {matches.Length}.");
        return null;
    }

    static T[] FindAll<T>(Scene scene) where T : Component
    {
        return scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true))
            .ToArray();
    }
}
