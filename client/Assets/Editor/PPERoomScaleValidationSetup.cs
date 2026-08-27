using System;
using System.Collections.Generic;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PPERoomScaleValidationSetup
{
    private const string ScenePath = "Assets/Scenes/3_PPE_Room_HandTest_scale.unity";
    private const string RootName = "Real Scale Test - 1.65m Eye Height";
    private const string ObsoleteRootName = "Scale Validation Lab";
    private const string EyeReferenceName = "Eye Height Reference - 1.65m";
    private const string SecondaryEyeReferenceName = "Eye Height Reference - 1.56m";
    private const float TargetEyeHeight = 1.65f;
    private const float SecondaryEyeHeight = 1.56f;
    private const float SecondaryReferenceHorizontalOffset = 1.5f;
    private const float SizeTolerance = 0.006f;

    internal sealed class ScaleSpec
    {
        public readonly string KoreanName;
        public readonly string AssetPath;
        public readonly float ImportedMaxDimension;
        public readonly float Correction;
        public readonly string RealDimensionLabel;
        public readonly float DisplayCenterHeight;
        public readonly bool UsePipeProxy;

        public float TargetMaxDimension => ImportedMaxDimension * Correction;

        public ScaleSpec(
            string koreanName,
            string assetPath,
            float importedMaxDimension,
            float correction,
            string realDimensionLabel = null,
            float displayCenterHeight = -1f,
            bool usePipeProxy = false)
        {
            KoreanName = koreanName;
            AssetPath = assetPath;
            ImportedMaxDimension = importedMaxDimension;
            Correction = correction;
            RealDimensionLabel = realDimensionLabel ?? string.Empty;
            DisplayCenterHeight = displayCenterHeight;
            UsePipeProxy = usePipeProxy;
        }
    }

    internal static readonly ScaleSpec[] Specs =
    {
        new(
            "방독면",
            "Assets/UIs/Things/PPE_room/gas_mask_3d_model_Clone1_Clone1_Clone1_Clone1_Clone1.prefab",
            1.02f,
            0.275f,
            realDimensionLabel: "높이 0.28m"),
        new(
            "내화학성 장갑",
            "Assets/UIs/Things/PPE_room/blue_rubber_gloves_3d_model_Clone1.prefab",
            0.94f,
            0.404f,
            realDimensionLabel: "길이 0.38m"),
        new(
            "차단 밸브",
            "Assets/FBX/valve_wheel/red+valve_wheel.fbx",
            1.00f,
            0.300f,
            realDimensionLabel: "핸들 지름 0.30m"),
        new(
            "균열 배관",
            string.Empty,
            2.48f,
            0.806f,
            realDimensionLabel: "관경 0.25m · 길이 2.0m",
            usePipeProxy: true),
        new(
            "흡착포 롤",
            "Assets/UIs/Things/PPE_room/PPE_A_Tape.prefab",
            1.24f,
            0.363f,
            realDimensionLabel: "지름 0.20m · 폭 0.45m"),
        new(
            "경고 표지판",
            "Assets/TripoModels/yellow_sign_board_3d_model/yellow_sign_board_3d_model.fbx",
            1.74f,
            0.346f,
            realDimensionLabel: "판면 0.45×0.60m · 설치높이 1.50m",
            displayCenterHeight: 1.50f)
    };

    [MenuItem("Tools/PPE/Build Real Scale Test %#&l")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Play Mode를 종료한 뒤 실물 스케일 시험을 구성해야 합니다.");

        Scene scene = RequireScene();
        Camera xrCamera = FindSceneComponent<Camera>(
            scene, component => component.CompareTag("MainCamera"));
        XROrigin xrOrigin = FindSceneComponent<XROrigin>(scene);
        if (xrCamera == null || xrOrigin == null)
            throw new InvalidOperationException("대상 씬에서 XR Origin 또는 Main Camera를 찾지 못했습니다.");

        RemoveMistakenComparisonLab(scene, xrOrigin);

        GameObject existing = FindRoot(scene, RootName);
        if (existing != null)
        {
            ValidateScene(scene, true);
            Selection.activeGameObject = existing;
            Debug.Log("기존 실물 스케일 시험 오브젝트를 보존하고 검증만 실행했습니다.", existing);
            return;
        }

        float floorY = FindFloorTop(scene, xrCamera.transform.position);
        Vector3 labOrigin = new(
            xrCamera.transform.position.x,
            floorY,
            xrCamera.transform.position.z + 4f);

        GameObject root = new(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Build PPE real scale test");

        BuildEyeHeightReference(root.transform, labOrigin, floorY);
        BuildSecondaryEyeHeightReference(root.transform);

        for (int index = 0; index < Specs.Length; index++)
        {
            int column = index % 3;
            int row = index / 3;
            Vector3 position = labOrigin + new Vector3(
                (column - 1) * 2.4f,
                0f,
                1.6f + row * 3.0f);
            BuildRealScaleObject(root.transform, Specs[index], index, position, floorY);
        }

        ConfigureEyeHeightAligner(xrOrigin, floorY);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException($"'{ScenePath}' 저장에 실패했습니다.");

        ValidateScene(scene, true);
        Selection.activeGameObject = root;
        Debug.Log(
            $"실물 스케일 시험 구성 완료: 바닥 Y={floorY:F3}m, 눈높이 1.65m, "
            + "실물 크기 보정 모델 6개",
            root);
    }

    [MenuItem("Tools/PPE/Validate Real Scale Test")]
    public static void Validate()
    {
        ValidateScene(RequireScene(), true);
    }

    [MenuItem("Tools/PPE/Create 1.65m Capsule Reference (Active Scene)")]
    public static void CreateCapsuleReferenceInActiveScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException(
                "Play Mode를 종료한 뒤 1.65m 캡슐 기준물을 생성해야 합니다.");

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
            throw new InvalidOperationException(
                "현재 활성 씬이 유효하지 않습니다.");

        const string capsuleName = "1.65m Capsule Reference";
        GameObject existing = FindRoot(scene, capsuleName);
        if (existing != null)
        {
            Selection.activeGameObject = existing;
            Debug.Log("기존 1.65m 캡슐 기준물을 선택했습니다.", existing);
            return;
        }

        Camera camera = FindSceneComponent<Camera>(
            scene,
            candidate => candidate.CompareTag("MainCamera"));
        if (camera == null)
            throw new InvalidOperationException(
                "활성 씬에서 Main Camera를 찾지 못했습니다.");

        float floorY = FindFloorTop(scene, camera.transform.position);
        Vector3 forward = camera.transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;
        forward.Normalize();

        GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Undo.RegisterCreatedObjectUndo(capsule, "Create 1.65m capsule reference");
        capsule.name = capsuleName;
        capsule.transform.position =
            camera.transform.position + forward * 1.5f;
        capsule.transform.position = new Vector3(
            capsule.transform.position.x,
            floorY + 0.825f,
            capsule.transform.position.z);
        capsule.transform.localScale = new Vector3(0.45f, 0.825f, 0.45f);

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException(
                $"'{scene.path}' 씬 저장에 실패했습니다.");

        Selection.activeGameObject = capsule;
        Debug.Log(
            $"1.65m 캡슐 기준물을 활성 씬에 생성했습니다. 바닥 Y={floorY:F3}m.",
            capsule);
    }

    [MenuItem("Tools/PPE/Toggle 1.65m Capsule Reference (Active Scene)")]
    public static void ToggleCapsuleReferenceInActiveScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException(
                "Play Mode를 종료한 뒤 1.65m 캡슐 기준물을 전환해야 합니다.");

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
            throw new InvalidOperationException(
                "현재 활성 씬이 유효하지 않습니다.");

        const string capsuleName = "1.65m Capsule Reference";
        GameObject capsule = FindRoot(scene, capsuleName);
        if (capsule == null)
        {
            Debug.LogWarning(
                "활성 씬에서 1.65m 캡슐 기준물을 찾지 못했습니다.");
            return;
        }

        capsule.SetActive(!capsule.activeSelf);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException(
                $"'{scene.path}' 씬 저장에 실패했습니다.");

        Selection.activeGameObject = capsule;
        Debug.Log(
            $"1.65m 캡슐 기준물 표시 상태: {(capsule.activeSelf ? "켜짐" : "꺼짐")}",
            capsule);
    }

    [MenuItem("Tools/PPE/Add 1.56m Eye Height Reference %#&h")]
    public static void AddSecondaryEyeHeightReference()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Play Mode를 종료한 뒤 1.56m 눈높이 기준물을 추가해야 합니다.");

        Scene scene = RequireScene();
        GameObject root = FindRoot(scene, RootName);
        if (root == null)
            throw new InvalidOperationException($"'{RootName}' 오브젝트가 없습니다.");

        Transform existing = root.transform.Find(SecondaryEyeReferenceName);
        if (existing == null)
        {
            BuildSecondaryEyeHeightReference(root.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException($"'{ScenePath}' 저장에 실패했습니다.");
            existing = root.transform.Find(SecondaryEyeReferenceName);
        }

        string validationFailure = GetSecondaryEyeHeightValidationFailure(root.transform);
        if (validationFailure != null)
            throw new InvalidOperationException(validationFailure);

        Selection.activeTransform = existing;
        Debug.Log("현재 씬에 1.56m 눈높이 기준물을 추가했습니다.", existing);
    }

    [MenuItem("Tools/PPE/Add 1.56m Eye Height Reference %#&h", true)]
    private static bool ValidateAddSecondaryEyeHeightReferenceMenu()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    private static Scene RequireScene()
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        if (scene.IsValid() && scene.isLoaded)
            return scene;

        if (!Application.isBatchMode
            && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            throw new OperationCanceledException("실물 스케일 시험 씬 열기가 취소되었습니다.");
        }

        return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
    }

    private static void RemoveMistakenComparisonLab(Scene scene, XROrigin xrOrigin)
    {
        GameObject obsolete = FindRoot(scene, ObsoleteRootName);
        if (obsolete != null)
            Undo.DestroyObjectImmediate(obsolete);

        XRScaleTestEyeHeightAligner oldAligner =
            xrOrigin.GetComponent<XRScaleTestEyeHeightAligner>();
        if (oldAligner != null)
            Undo.DestroyObjectImmediate(oldAligner);
    }

    private static void BuildEyeHeightReference(
        Transform parent, Vector3 labOrigin, float floorY)
    {
        GameObject reference = new(EyeReferenceName);
        Undo.RegisterCreatedObjectUndo(reference, "Create 1.65m eye reference");
        reference.transform.SetParent(parent, false);

        Vector3 basePosition = new(labOrigin.x - 3.6f, floorY, labOrigin.z);
        CreatePrimitive(
            PrimitiveType.Cube,
            "1.65m Vertical Ruler",
            reference.transform,
            basePosition + Vector3.up * (TargetEyeHeight * 0.5f),
            new Vector3(0.04f, TargetEyeHeight, 0.04f));
        CreatePrimitive(
            PrimitiveType.Cube,
            "1.65m Eye Line",
            reference.transform,
            basePosition + Vector3.up * TargetEyeHeight,
            new Vector3(1.2f, 0.025f, 0.025f));
        CreatePrimitive(
            PrimitiveType.Sphere,
            "Average Eye Point",
            reference.transform,
            basePosition + Vector3.up * TargetEyeHeight,
            Vector3.one * 0.09f);
    }

    private static void BuildSecondaryEyeHeightReference(Transform parent)
    {
        Transform primaryReference = parent.Find(EyeReferenceName);
        Transform primaryRuler = primaryReference != null
            ? primaryReference.Find("1.65m Vertical Ruler")
            : null;
        if (primaryRuler == null)
            throw new InvalidOperationException("1.56m 기준물을 배치할 1.65m 원본 기준자가 없습니다.");

        Bounds primaryBounds = CalculateBounds(primaryRuler.gameObject);
        float floorY = primaryBounds.min.y;
        Vector3 basePosition = new(
            primaryBounds.center.x + SecondaryReferenceHorizontalOffset,
            floorY,
            primaryBounds.center.z);
        Material referenceMaterial = primaryRuler.GetComponentInChildren<Renderer>(true)?.sharedMaterial;

        GameObject reference = new(SecondaryEyeReferenceName);
        Undo.RegisterCreatedObjectUndo(reference, "Create 1.56m eye reference");
        reference.transform.SetParent(parent, false);

        GameObject ruler = CreatePrimitive(
            PrimitiveType.Cube,
            "1.56m Vertical Ruler",
            reference.transform,
            basePosition + Vector3.up * (SecondaryEyeHeight * 0.5f),
            new Vector3(0.04f, SecondaryEyeHeight, 0.04f));
        GameObject eyeLine = CreatePrimitive(
            PrimitiveType.Cube,
            "1.56m Eye Line",
            reference.transform,
            basePosition + Vector3.up * SecondaryEyeHeight,
            new Vector3(1.2f, 0.025f, 0.025f));
        GameObject eyePoint = CreatePrimitive(
            PrimitiveType.Sphere,
            "1.56m Eye Point",
            reference.transform,
            basePosition + Vector3.up * SecondaryEyeHeight,
            Vector3.one * 0.09f);

        ApplyReferenceMaterial(ruler, referenceMaterial);
        ApplyReferenceMaterial(eyeLine, referenceMaterial);
        ApplyReferenceMaterial(eyePoint, referenceMaterial);
    }

    private static void ApplyReferenceMaterial(GameObject target, Material material)
    {
        if (material == null)
            return;

        foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(true))
        {
            Undo.RecordObject(renderer, "Match eye reference material");
            renderer.sharedMaterial = material;
            EditorUtility.SetDirty(renderer);
        }
    }

    private static void BuildRealScaleObject(
        Transform parent,
        ScaleSpec spec,
        int index,
        Vector3 position,
        float floorY)
    {
        GameObject target = spec.UsePipeProxy
            ? CreatePipeProxy(parent)
            : InstantiateAsset(spec.AssetPath, parent);
        target.name = $"{index + 1:00} {spec.KoreanName} - Real Scale";

        if (!spec.UsePipeProxy)
            NormalizeMaxDimension(target, spec.ImportedMaxDimension);

        target.transform.localScale *= spec.Correction;
        target.transform.position = position;
        PlaceForDisplay(target, floorY, spec.DisplayCenterHeight);
    }

    private static GameObject InstantiateAsset(string assetPath, Transform parent)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (asset == null)
            throw new MissingReferenceException($"실물 크기 시험 모델을 찾지 못했습니다: {assetPath}");

        GameObject instance = PrefabUtility.InstantiatePrefab(asset, parent) as GameObject;
        if (instance == null)
            instance = UnityEngine.Object.Instantiate(asset, parent);
        Undo.RegisterCreatedObjectUndo(instance, "Instantiate real scale test model");
        return instance;
    }

    private static GameObject CreatePipeProxy(Transform parent)
    {
        GameObject pipe = CreatePrimitive(
            PrimitiveType.Cylinder,
            "Pipe Scale Proxy",
            parent,
            Vector3.zero,
            new Vector3(0.31f, 1.24f, 0.31f));
        pipe.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        return pipe;
    }

    private static GameObject CreatePrimitive(
        PrimitiveType type,
        string name,
        Transform parent,
        Vector3 position,
        Vector3 localScale)
    {
        GameObject primitive = GameObject.CreatePrimitive(type);
        Undo.RegisterCreatedObjectUndo(primitive, $"Create {name}");
        primitive.name = name;
        primitive.transform.SetParent(parent, true);
        primitive.transform.position = position;
        primitive.transform.localScale = localScale;
        Collider collider = primitive.GetComponent<Collider>();
        if (collider != null)
            UnityEngine.Object.DestroyImmediate(collider);
        return primitive;
    }

    private static void NormalizeMaxDimension(GameObject target, float expectedMaxDimension)
    {
        Bounds bounds = CalculateBounds(target);
        float currentMax = MaxComponent(bounds.size);
        if (currentMax <= 0.0001f || !float.IsFinite(currentMax))
            throw new InvalidOperationException($"'{target.name}' Renderer bounds가 유효하지 않습니다.");

        target.transform.localScale *= expectedMaxDimension / currentMax;
    }

    private static void PlaceForDisplay(
        GameObject target, float floorY, float displayCenterHeight)
    {
        Bounds bounds = CalculateBounds(target);
        float targetY = displayCenterHeight >= 0f
            ? floorY + displayCenterHeight
            : floorY + 0.02f;
        float currentY = displayCenterHeight >= 0f ? bounds.center.y : bounds.min.y;
        target.transform.position += Vector3.up * (targetY - currentY);
    }

    private static Bounds CalculateBounds(GameObject target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            throw new InvalidOperationException($"'{target.name}' 아래 Renderer가 없습니다.");

        Bounds bounds = renderers[0].bounds;
        for (int index = 1; index < renderers.Length; index++)
            bounds.Encapsulate(renderers[index].bounds);
        return bounds;
    }

    private static float FindFloorTop(Scene scene, Vector3 referencePosition)
    {
        List<Renderer> candidates = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Renderer>(true))
            .Where(renderer =>
                renderer.gameObject.name == "Floor"
                || renderer.gameObject.name.StartsWith("Floor (", StringComparison.Ordinal))
            .Where(renderer =>
                renderer.bounds.min.x <= referencePosition.x
                && renderer.bounds.max.x >= referencePosition.x
                && renderer.bounds.min.z <= referencePosition.z
                && renderer.bounds.max.z >= referencePosition.z)
            .ToList();

        if (candidates.Count == 0)
            throw new InvalidOperationException("XR 시작점 아래의 PPE Room 바닥 Renderer를 찾지 못했습니다.");

        return candidates.Max(renderer => renderer.bounds.max.y);
    }

    private static void ConfigureEyeHeightAligner(XROrigin origin, float floorY)
    {
        XRScaleTestEyeHeightAligner aligner =
            Undo.AddComponent<XRScaleTestEyeHeightAligner>(origin.gameObject);

        SerializedObject serialized = new(aligner);
        serialized.FindProperty("alignOnStart").boolValue = true;
        serialized.FindProperty("floorWorldY").floatValue = floorY;
        serialized.FindProperty("targetEyeHeight").floatValue = TargetEyeHeight;
        serialized.FindProperty("trackingWarmupFrames").intValue = 3;
        serialized.FindProperty("deviceWaitSeconds").floatValue = 10f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(aligner);
    }

    private static void ValidateScene(Scene scene, bool logSuccess)
    {
        List<string> failures = new();
        GameObject root = FindRoot(scene, RootName);
        if (root == null)
        {
            failures.Add($"'{RootName}'이 없습니다.");
        }
        else
        {
            Transform eyeReference = root.transform.Find(EyeReferenceName);
            Transform ruler = eyeReference != null
                ? eyeReference.Find("1.65m Vertical Ruler")
                : null;
            if (ruler == null
                || Mathf.Abs(CalculateBounds(ruler.gameObject).size.y - TargetEyeHeight)
                > SizeTolerance)
            {
                failures.Add("1.65m 눈높이 기준자가 정확하지 않습니다.");
            }

            string secondaryFailure = GetSecondaryEyeHeightValidationFailure(root.transform);
            if (secondaryFailure != null)
                failures.Add(secondaryFailure);

            for (int index = 0; index < Specs.Length; index++)
            {
                string name = $"{index + 1:00} {Specs[index].KoreanName} - Real Scale";
                Transform target = root.transform.Find(name);
                if (target == null)
                {
                    failures.Add($"{name} 오브젝트가 없습니다.");
                    continue;
                }

                float actual = MaxComponent(CalculateBounds(target.gameObject).size);
                if (Mathf.Abs(actual - Specs[index].TargetMaxDimension) > SizeTolerance)
                {
                    failures.Add(
                        $"{Specs[index].KoreanName} 최대 치수 {actual:F3}m "
                        + $"!= 목표 {Specs[index].TargetMaxDimension:F3}m.");
                }
            }
        }

        XROrigin origin = FindSceneComponent<XROrigin>(scene);
        XRScaleTestEyeHeightAligner aligner = origin != null
            ? origin.GetComponent<XRScaleTestEyeHeightAligner>()
            : null;
        if (aligner == null)
            failures.Add("XR Origin에 1.65m 눈높이 정렬기가 없습니다.");

        if (FindRoot(scene, ObsoleteRootName) != null)
            failures.Add("잘못 생성된 원본/보정 비교 랩이 남아 있습니다.");

        if (failures.Count > 0)
        {
            string message = "PPE 실물 스케일 검증 실패:\n- "
                + string.Join("\n- ", failures);
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        if (logSuccess)
        {
            string result = string.Join(
                "\n",
                Specs.Select((spec, index) =>
                    $"{index + 1:00} {spec.KoreanName}: "
                    + $"{spec.ImportedMaxDimension:F3}m × {spec.Correction:F3} "
                    + $"= {spec.TargetMaxDimension:F3}m"));
            Debug.Log("PPE 실물 스케일 검증 통과 — 눈높이 1.65m\n" + result, root);
        }
    }

    private static float MaxComponent(Vector3 value)
    {
        return Mathf.Max(value.x, Mathf.Max(value.y, value.z));
    }

    private static string GetSecondaryEyeHeightValidationFailure(Transform root)
    {
        Transform secondaryReference = root.Find(SecondaryEyeReferenceName);
        Transform secondaryRuler = secondaryReference != null
            ? secondaryReference.Find("1.56m Vertical Ruler")
            : null;
        Transform secondaryEyeLine = secondaryReference != null
            ? secondaryReference.Find("1.56m Eye Line")
            : null;
        Transform secondaryEyePoint = secondaryReference != null
            ? secondaryReference.Find("1.56m Eye Point")
            : null;
        if (secondaryRuler == null
            || secondaryEyeLine == null
            || secondaryEyePoint == null)
        {
            return "1.56m 눈높이 기준물 계층이 완전하지 않습니다.";
        }

        Bounds secondaryRulerBounds = CalculateBounds(secondaryRuler.gameObject);
        float secondaryFloorY = secondaryRulerBounds.min.y;
        float eyeLineY = CalculateBounds(secondaryEyeLine.gameObject).center.y;
        float eyePointY = CalculateBounds(secondaryEyePoint.gameObject).center.y;
        if (Mathf.Abs(secondaryRulerBounds.size.y - SecondaryEyeHeight) > SizeTolerance
            || Mathf.Abs(eyeLineY - secondaryFloorY - SecondaryEyeHeight) > SizeTolerance
            || Mathf.Abs(eyePointY - secondaryFloorY - SecondaryEyeHeight) > SizeTolerance)
        {
            return "1.56m 눈높이 기준자의 높이 또는 눈높이 표시 위치가 정확하지 않습니다.";
        }

        return null;
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        return scene.GetRootGameObjects().FirstOrDefault(root => root.name == name);
    }

    private static T FindSceneComponent<T>(
        Scene scene, Func<T, bool> predicate = null) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (T component in root.GetComponentsInChildren<T>(true))
            {
                if (predicate == null || predicate(component))
                    return component;
            }
        }

        return null;
    }
}
