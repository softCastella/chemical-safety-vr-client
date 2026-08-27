#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class PPEAssetEvaluationAnalyzerWindow : EditorWindow
{
    [Serializable]
    public class TargetDefinition
    {
        public int no;
        public string objectName;
        public string category;
        public string role;

        public TargetDefinition(int no, string objectName, string category, string role)
        {
            this.no = no;
            this.objectName = objectName;
            this.category = category;
            this.role = role;
        }
    }

    [Serializable]
    public class AssetResult
    {
        public int no;
        public string objectName;
        public string category;
        public string role;

        public string foundFrom;     // Scene / Project / Missing
        public string sourcePath;
        public string format;
        public string status;

        public float sizeX_m;
        public float sizeY_m;
        public float sizeZ_m;

        public float originalSizeX_m;
        public float originalSizeY_m;
        public float originalSizeZ_m;

        public long triCount;
        public int textureCount;
        public string textureState;
        public int maxTextureWidth;
        public int maxTextureHeight;
        public string textureMaps;

        public int duplicateSceneObjects;
    }

    [Serializable]
    public class ScaleCheckResult
    {
        public int no;
        public string assetName;
        public string realDimension;
        public string sourcePath;
        public float importedImmediately_m;
        public float correctionMultiplier;
        public float correctedSize_m;
        public float expectedCorrectedSize_m;
        public string result;
    }

    [Serializable]
    private class ReportWrapper
    {
        public string generatedAt;
        public string unityVersion;
        public string projectName;
        public string purpose;
        public int targetCount;
        public int foundCount;
        public int missingCount;
        public long totalTri;
        public AssetResult[] assets;
        public ScaleCheckResult[] scaleChecks;
    }

    // =========================================================
    // 프로젝트 실제 34종
    // A = 착용/장비, B = 설비/환경, C = 상호작용 도구
    // =========================================================
    private static readonly TargetDefinition[] Targets =
    {
        new TargetDefinition( 1, "PPE_B_Bench",              "B 설비/환경",     "PPE룸 환경"),
        new TargetDefinition( 2, "PPE_B_MetalLocker",        "B 설비/환경",     "PPE룸 환경"),
        new TargetDefinition( 3, "PPE_B_MaskLocker",         "B 설비/환경",     "마스크 보관·배치 환경"),
        new TargetDefinition( 4, "PPE_B_SafetyCabinet",      "B 설비/환경",     "PPE룸 환경"),
        new TargetDefinition( 5, "PPE_B_WoodenCrate",        "B 설비/환경",     "PPE룸 배경 환경"),
        new TargetDefinition( 6, "PPE_B_WallHanger",         "B 설비/환경",     "PPE 보관·배치 환경"),
        new TargetDefinition( 7, "PPE_B_HandSanitizer",      "B 설비/환경",     "PPE룸 환경"),
        new TargetDefinition( 8, "PPE_B_FireExtinguisher",   "B 설비/환경",     "PPE룸 배경 환경"),
        new TargetDefinition( 9, "PPE_B_YellowTrashBin",     "B 설비/환경",     "PPE룸 배경 환경"),
        new TargetDefinition(10, "PPE_B_StorageWallRack",    "B 설비/환경",     "PPE 보관·배치 환경"),
        new TargetDefinition(11, "PPE_B_SuitHanger",         "B 설비/환경",     "방호복 보관·배치 환경"),
        new TargetDefinition(12, "PPE_B_CleaningCart",       "B 설비/환경",     "PPE룸 배경 환경"),
        new TargetDefinition(13, "PPE_B_Duct",               "B 설비/환경",     "PPE룸 배경 환경"),
        new TargetDefinition(14, "PPE_B_CCTV",               "B 설비/환경",     "PPE룸 배경 환경"),

        new TargetDefinition(15, "PPE_C_Tablet",             "C 상호작용 도구", "PPE 착용 전 작업 확인"),

        new TargetDefinition(16, "PPE_A_SuitHang",           "A 착용/장비",     "방호복 진열 상태"),
        new TargetDefinition(17, "PPE_A_SuitWear",           "A 착용/장비",     "방호복 착용 상태"),
        new TargetDefinition(18, "PPE_A_Boots_L",            "A 착용/장비",     "왼쪽 장화"),
        new TargetDefinition(19, "PPE_A_Boots_R",            "A 착용/장비",     "오른쪽 장화"),
        new TargetDefinition(20, "PPE_A_Backplate",          "A 착용/장비",     "등지게"),
        new TargetDefinition(21, "PPE_A_Mask",               "A 착용/장비",     "송기마스크"),
        new TargetDefinition(22, "PPE_A_Helmet_Strap",       "A 착용/장비",     "안전모"),
        new TargetDefinition(23, "PPE_A_Glove_L",            "A 착용/장비",     "왼쪽 장갑"),
        new TargetDefinition(24, "PPE_A_Glove_R",            "A 착용/장비",     "오른쪽 장갑"),
        new TargetDefinition(25, "PPE_A_Tape",               "A 착용/장비",     "테이핑"),
        new TargetDefinition(26, "PPE_A_Taped",              "A 착용/장비",     "테이핑 적용 상태"),
        new TargetDefinition(27, "PPE_A_Hand_Bare_L",        "A 착용/장비",     "왼손 맨손 상태"),
        new TargetDefinition(28, "PPE_A_Hand_Bare_R",        "A 착용/장비",     "오른손 맨손 상태"),
        new TargetDefinition(29, "PPE_A_Hand_Suit_L",        "A 착용/장비",     "왼손 방호복 착용 상태"),
        new TargetDefinition(30, "PPE_A_Hand_Suit_R",        "A 착용/장비",     "오른손 방호복 착용 상태"),
        new TargetDefinition(31, "PPE_A_Hand_GloveSuit_L",   "A 착용/장비",     "왼손 방호복+장갑 상태"),
        new TargetDefinition(32, "PPE_A_Hand_GloveSuit_R",   "A 착용/장비",     "오른손 방호복+장갑 상태"),
        new TargetDefinition(33, "PPE_A_Hand_GloveTape_L",   "A 착용/장비",     "왼손 방호복+장갑+테이핑 상태"),
        new TargetDefinition(34, "PPE_A_Hand_GloveTape_R",   "A 착용/장비",     "오른손 방호복+장갑+테이핑 상태")
    };

    private readonly List<AssetResult> results = new List<AssetResult>();
    private readonly List<ScaleCheckResult> scaleChecks =
        new List<ScaleCheckResult>();

    private Vector2 scroll;
    private string exportFolder = "Assets/PPE_Evaluation_Report";
    private bool searchSceneFirst = true;
    private bool projectFolderFallback = true;

    [MenuItem("Tools/PPE Asset Evaluation Analyzer")]
    public static void Open()
    {
        PPEAssetEvaluationAnalyzerWindow window =
            GetWindow<PPEAssetEvaluationAnalyzerWindow>();

        window.titleContent = new GUIContent("PPE Asset Analyzer");
        window.minSize = new Vector2(980, 620);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField(
            "PPE Asset Evaluation Analyzer",
            EditorStyles.boldLabel
        );

        EditorGUILayout.HelpBox(
            "현재 프로젝트의 34개 에셋명을 코드에 고정해 두었습니다.\n" +
            "Hierarchy에 있으면 실제 씬 오브젝트를 우선 분석하고, 없으면 Project 전체에서 같은 이름의 모델 에셋을 분석합니다.\n" +
            "한 에셋 밑에 자식 Mesh가 100개 이상 있어도 모두 합산하며, 최상위 에셋 1개를 결과 1줄로 출력합니다.",
            MessageType.Info
        );

        EditorGUILayout.Space(6);

        searchSceneFirst = EditorGUILayout.ToggleLeft(
            "Hierarchy/Scene의 실제 배치 오브젝트를 우선 분석",
            searchSceneFirst
        );

        projectFolderFallback = EditorGUILayout.ToggleLeft(
            "Scene에 없으면 Project 전체에서 동일 이름 모델 에셋 찾기",
            projectFolderFallback
        );

        exportFolder = EditorGUILayout.TextField(
            "결과 폴더",
            exportFolder
        );

        EditorGUILayout.Space(8);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button(
                "34종 자동 분석",
                GUILayout.Height(38)))
        {
            AnalyzeAll();
        }

        GUI.enabled = results.Count > 0;

        if (GUILayout.Button(
                "CSV + JSON + MD 저장",
                GUILayout.Height(38)))
        {
            ExportAll();
        }

        if (GUILayout.Button(
                "결과 폴더 열기",
                GUILayout.Height(38)))
        {
            RevealExportFolder();
        }

        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        DrawSummary();
        DrawTable();
        DrawScaleValidationTable();
    }

    // =========================================================
    // 분석
    // =========================================================

    private void AnalyzeAll()
    {
        results.Clear();

        try
        {
            for (int i = 0; i < Targets.Length; i++)
            {
                TargetDefinition target = Targets[i];

                EditorUtility.DisplayProgressBar(
                    "PPE 34종 분석",
                    target.objectName,
                    (float)i / Targets.Length
                );

                AssetResult result = AnalyzeTarget(target);
                results.Add(result);
            }

            AnalyzeScaleCriteria();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        Repaint();
    }

    private void AnalyzeScaleCriteria()
    {
        scaleChecks.Clear();

        for (int index = 0;
             index < PPERoomScaleValidationSetup.Specs.Length;
             index++)
        {
            PPERoomScaleValidationSetup.ScaleSpec spec =
                PPERoomScaleValidationSetup.Specs[index];

            ScaleCheckResult check = new ScaleCheckResult();
            check.no = index + 1;
            check.assetName = spec.KoreanName;
            check.realDimension = spec.RealDimensionLabel;
            check.sourcePath = spec.AssetPath;
            check.correctionMultiplier = spec.Correction;
            check.expectedCorrectedSize_m = spec.TargetMaxDimension;

            float importedMax = spec.ImportedMaxDimension;
            bool measured = spec.UsePipeProxy ||
                            string.IsNullOrEmpty(spec.AssetPath);

            if (!measured)
            {
                GameObject asset =
                    AssetDatabase.LoadAssetAtPath<GameObject>(spec.AssetPath);

                if (asset == null)
                {
                    check.result = "기준 에셋 없음";
                    scaleChecks.Add(check);
                    continue;
                }

                GameObject temp = null;

                try
                {
                    temp =
                        PrefabUtility.InstantiatePrefab(asset)
                        as GameObject;

                    if (temp == null)
                        temp = Instantiate(asset);

                    temp.hideFlags = HideFlags.HideAndDontSave;
                    Vector3 size = CalculateRootAlignedWorldSize(temp);
                    importedMax = Mathf.Max(size.x, size.y, size.z);
                }
                finally
                {
                    if (temp != null)
                        DestroyImmediate(temp);
                }
            }

            check.importedImmediately_m = importedMax;
            check.correctedSize_m = importedMax * check.correctionMultiplier;
            check.result = Mathf.Abs(
                check.correctedSize_m - check.expectedCorrectedSize_m) <= 0.006f
                ? "적합"
                : "Import 치수 확인 필요";

            scaleChecks.Add(check);
        }
    }

    private AssetResult AnalyzeTarget(TargetDefinition target)
    {
        AssetResult result = new AssetResult();

        result.no = target.no;
        result.objectName = target.objectName;
        result.category = target.category;
        result.role = target.role;
        result.foundFrom = "Missing";
        result.status = "찾지 못함";
        result.textureState = "-";

        // 1) 실제 Scene/Hierarchy 오브젝트 우선
        if (searchSceneFirst)
        {
            List<GameObject> sceneMatches =
                FindSceneObjectsExact(target.objectName);

            if (sceneMatches.Count > 0)
            {
                GameObject root = sceneMatches[0];

                result.duplicateSceneObjects = sceneMatches.Count;
                result.foundFrom = "Scene";
                result.status = sceneMatches.Count == 1
                    ? "정상"
                    : "동일 이름 " + sceneMatches.Count + "개 발견 - 첫 번째 분석";

                result.sourcePath = ResolveSourcePath(root);
                result.format = GetFormat(result.sourcePath);

                FillGeometryResult(root, result);
                ApplyTextureProfile(root, result);

                return result;
            }
        }

        // 2) Scene에 없으면 Project 전체에서 같은 이름의 모델 에셋 찾기
        if (projectFolderFallback)
        {
            string assetPath =
                FindExactProjectAsset(target.objectName);

            if (!string.IsNullOrEmpty(assetPath))
            {
                string modelPath =
                    FindBestGameObjectAsset(target.objectName);

                if (!string.IsNullOrEmpty(modelPath))
                {
                    GameObject asset =
                        AssetDatabase.LoadAssetAtPath<GameObject>(
                            modelPath
                        );

                    if (asset != null)
                    {
                        GameObject temp = null;

                        try
                        {
                            temp =
                                PrefabUtility.InstantiatePrefab(asset)
                                as GameObject;

                            if (temp == null)
                                temp = Instantiate(asset);

                            temp.name = target.objectName;
                            temp.hideFlags = HideFlags.HideAndDontSave;

                            result.foundFrom = "Project";
                            result.sourcePath = modelPath;
                            result.format = GetFormat(modelPath);
                            result.status = "Project 모델 분석";

                            FillGeometryResult(temp, result);
                            ApplyTextureProfile(temp, result);

                            return result;
                        }
                        finally
                        {
                            if (temp != null)
                                DestroyImmediate(temp);
                        }
                    }
                }

                result.status =
                    "폴더는 찾았으나 분석 가능한 GameObject 모델 없음";
            }
        }

        return result;
    }

    // =========================================================
    // Scene 검색
    // =========================================================

    private List<GameObject> FindSceneObjectsExact(string targetName)
    {
        List<GameObject> matches = new List<GameObject>();

        for (int s = 0; s < SceneManager.sceneCount; s++)
        {
            Scene scene = SceneManager.GetSceneAt(s);

            if (!scene.isLoaded)
                continue;

            GameObject[] roots = scene.GetRootGameObjects();

            foreach (GameObject root in roots)
            {
                Transform[] transforms =
                    root.GetComponentsInChildren<Transform>(true);

                foreach (Transform t in transforms)
                {
                    if (NormalizeName(t.name) == targetName)
                        matches.Add(t.gameObject);
                }
            }
        }

        return matches;
    }

    private static string NormalizeName(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        string name = value.Trim();

        if (name.EndsWith("(Clone)", StringComparison.Ordinal))
            name = name.Substring(0, name.Length - 7).Trim();

        return name;
    }

    // =========================================================
    // Project 검색
    // =========================================================

    private string FindExactProjectAsset(string assetName)
    {
        string[] guids = AssetDatabase.FindAssets(assetName);

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (string.IsNullOrEmpty(path) ||
                Path.GetFileNameWithoutExtension(path) != assetName)
                continue;

            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
                return path;
        }

        return "";
    }

    private string FindBestGameObjectAsset(string targetName)
    {
        string[] guids =
            AssetDatabase.FindAssets(
                targetName
            );

        List<string> candidates = new List<string>();

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);

            if (string.IsNullOrEmpty(path))
                continue;

            GameObject obj =
                AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (obj != null)
                candidates.Add(path);
        }

        if (candidates.Count == 0)
            return "";

        // 1순위: 파일명이 폴더/대상명과 정확히 같은 것
        string exact = candidates.FirstOrDefault(
            p => Path.GetFileNameWithoutExtension(p) == targetName
        );

        if (!string.IsNullOrEmpty(exact))
            return exact;

        // 2순위: Prefab
        string prefab = candidates.FirstOrDefault(
            p => string.Equals(
                Path.GetExtension(p),
                ".prefab",
                StringComparison.OrdinalIgnoreCase
            )
        );

        if (!string.IsNullOrEmpty(prefab))
            return prefab;

        // 3순위: FBX
        string fbx = candidates.FirstOrDefault(
            p => string.Equals(
                Path.GetExtension(p),
                ".fbx",
                StringComparison.OrdinalIgnoreCase
            )
        );

        if (!string.IsNullOrEmpty(fbx))
            return fbx;

        // 나머지 GameObject asset
        return candidates[0];
    }

    // =========================================================
    // Geometry / Size / Tri
    // =========================================================

    private void FillGeometryResult(
        GameObject root,
        AssetResult result)
    {
        Vector3 size = CalculateRootAlignedWorldSize(root);

        result.sizeX_m = size.x;
        result.sizeY_m = size.y;
        result.sizeZ_m = size.z;

        Vector3 originalSize =
            CalculateOriginalAssetSize(root, result.sourcePath);

        result.originalSizeX_m = originalSize.x;
        result.originalSizeY_m = originalSize.y;
        result.originalSizeZ_m = originalSize.z;

        result.triCount = CalculateTotalTriangles(root);

        int textureCount = CalculateTextureCount(root);

        result.textureCount = textureCount;
        result.textureState =
            textureCount > 0
                ? "있음 (" + textureCount + ")"
                : "없음";
    }

    private Vector3 CalculateOriginalAssetSize(
        GameObject sceneObject,
        string sourcePath)
    {
        if (!string.IsNullOrEmpty(sourcePath))
        {
            GameObject source =
                AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);

            if (source != null)
            {
                GameObject temp = null;

                try
                {
                    temp =
                        PrefabUtility.InstantiatePrefab(source)
                        as GameObject;

                    if (temp == null)
                        temp = Instantiate(source);

                    temp.hideFlags = HideFlags.HideAndDontSave;
                    return CalculateRootAlignedWorldSize(temp);
                }
                finally
                {
                    if (temp != null)
                        DestroyImmediate(temp);
                }
            }
        }

        return sceneObject == null
            ? Vector3.zero
            : CalculateRootAlignedWorldSize(sceneObject);
    }

    /// <summary>
    /// root의 회전 때문에 Renderer.worldBounds가 부풀어 보이는 문제를 피하기 위해
    /// 각 Mesh Bounds를 root 로컬축으로 모은 뒤 root의 실제 world scale을 적용한다.
    /// 자식 오브젝트의 위치/회전/스케일은 모두 반영된다.
    /// </summary>
    private Vector3 CalculateRootAlignedWorldSize(GameObject root)
    {
        bool initialized = false;
        Bounds combined = new Bounds();

        MeshFilter[] meshFilters =
            root.GetComponentsInChildren<MeshFilter>(true);

        foreach (MeshFilter mf in meshFilters)
        {
            if (mf == null || mf.sharedMesh == null)
                continue;

            Matrix4x4 toRoot =
                root.transform.worldToLocalMatrix *
                mf.transform.localToWorldMatrix;

            EncapsulateBoundsCorners(
                mf.sharedMesh.bounds,
                toRoot,
                ref combined,
                ref initialized
            );
        }

        SkinnedMeshRenderer[] skinned =
            root.GetComponentsInChildren<SkinnedMeshRenderer>(true);

        foreach (SkinnedMeshRenderer smr in skinned)
        {
            if (smr == null || smr.sharedMesh == null)
                continue;

            Matrix4x4 toRoot =
                root.transform.worldToLocalMatrix *
                smr.transform.localToWorldMatrix;

            EncapsulateBoundsCorners(
                smr.localBounds,
                toRoot,
                ref combined,
                ref initialized
            );
        }

        if (!initialized)
            return Vector3.zero;

        Vector3 rootScale = root.transform.lossyScale;

        rootScale = new Vector3(
            Mathf.Abs(rootScale.x),
            Mathf.Abs(rootScale.y),
            Mathf.Abs(rootScale.z)
        );

        return Vector3.Scale(combined.size, rootScale);
    }

    private void EncapsulateBoundsCorners(
        Bounds bounds,
        Matrix4x4 matrix,
        ref Bounds combined,
        ref bool initialized)
    {
        Vector3 c = bounds.center;
        Vector3 e = bounds.extents;

        for (int x = -1; x <= 1; x += 2)
        {
            for (int y = -1; y <= 1; y += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 localCorner =
                        c + Vector3.Scale(
                            e,
                            new Vector3(x, y, z)
                        );

                    Vector3 point =
                        matrix.MultiplyPoint3x4(localCorner);

                    if (!initialized)
                    {
                        combined =
                            new Bounds(point, Vector3.zero);

                        initialized = true;
                    }
                    else
                    {
                        combined.Encapsulate(point);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 자식 Mesh 조각이 100개 이상이어도 전부 합산.
    /// 같은 sharedMesh를 여러 자식이 사용하면 "각 배치 인스턴스"를 각각 계산.
    /// </summary>
    private long CalculateTotalTriangles(GameObject root)
    {
        long total = 0;

        MeshFilter[] meshFilters =
            root.GetComponentsInChildren<MeshFilter>(true);

        foreach (MeshFilter mf in meshFilters)
        {
            if (mf == null || mf.sharedMesh == null)
                continue;

            total += CountTriangles(mf.sharedMesh);
        }

        SkinnedMeshRenderer[] skinned =
            root.GetComponentsInChildren<SkinnedMeshRenderer>(true);

        foreach (SkinnedMeshRenderer smr in skinned)
        {
            if (smr == null || smr.sharedMesh == null)
                continue;

            total += CountTriangles(smr.sharedMesh);
        }

        return total;
    }

    private long CountTriangles(Mesh mesh)
    {
        long total = 0;

        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            if (mesh.GetTopology(i) == MeshTopology.Triangles)
            {
                total +=
                    (long)mesh.GetIndexCount(i) / 3L;
            }
        }

        return total;
    }

    // =========================================================
    // Texture
    // =========================================================

    private int CalculateTextureCount(GameObject root)
    {
        HashSet<Texture> textures =
            new HashSet<Texture>();

        Renderer[] renderers =
            root.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            Material[] materials =
                renderer.sharedMaterials;

            foreach (Material material in materials)
            {
                if (material == null)
                    continue;

                string[] properties =
                    material.GetTexturePropertyNames();

                foreach (string property in properties)
                {
                    Texture texture =
                        material.GetTexture(property);

                    if (texture != null)
                        textures.Add(texture);
                }
            }
        }

        return textures.Count;
    }

    private void ApplyTextureProfile(GameObject root, AssetResult result)
    {
        HashSet<Texture> textures = new HashSet<Texture>();
        HashSet<string> mapNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        bool hasPbrMap = false;
        bool hasCustomMap = false;
        int maxWidth = 0;
        int maxHeight = 0;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            foreach (Material material in renderer.sharedMaterials)
            {
                if (material == null)
                    continue;

                foreach (string property in material.GetTexturePropertyNames())
                {
                    Texture texture = material.GetTexture(property);
                    if (texture == null)
                        continue;

                    textures.Add(texture);
                    mapNames.Add(property);
                    maxWidth = Mathf.Max(maxWidth, texture.width);
                    maxHeight = Mathf.Max(maxHeight, texture.height);

                    string name = property.ToLowerInvariant();
                    bool standard =
                        name.Contains("base") ||
                        name.Contains("maintex") ||
                        name.Contains("albedo") ||
                        name.Contains("normal") ||
                        name.Contains("bump") ||
                        name.Contains("metal") ||
                        name.Contains("rough") ||
                        name.Contains("occlusion") ||
                        name.Contains("emission") ||
                        name.Contains("specgloss") ||
                        name.Contains("parallax") ||
                        name.Contains("mask");

                    if (standard)
                        hasPbrMap = true;
                    else
                        hasCustomMap = true;
                }
            }
        }

        result.textureCount = textures.Count;
        result.maxTextureWidth = maxWidth;
        result.maxTextureHeight = maxHeight;
        result.textureMaps = string.Join(", ", mapNames.OrderBy(name => name));

        if (textures.Count == 0)
        {
            result.textureState = "없음";
            return;
        }

        string resolution = maxWidth >= 2048 || maxHeight >= 2048
            ? "2K"
            : maxWidth >= 1024 || maxHeight >= 1024
                ? "1K"
                : "<1K";

        result.textureState = resolution + (hasPbrMap ? " PBR" : " Texture");
        if (hasCustomMap)
            result.textureState += " + Custom";
    }

    // =========================================================
    // Source / Format
    // =========================================================

    private string ResolveSourcePath(GameObject root)
    {
        UnityEngine.Object source =
            PrefabUtility.GetCorrespondingObjectFromSource(root);

        if (source != null)
        {
            string prefabPath =
                AssetDatabase.GetAssetPath(source);

            if (!string.IsNullOrEmpty(prefabPath))
                return prefabPath;
        }

        MeshFilter mf =
            root.GetComponentInChildren<MeshFilter>(true);

        if (mf != null && mf.sharedMesh != null)
        {
            string meshPath =
                AssetDatabase.GetAssetPath(mf.sharedMesh);

            if (!string.IsNullOrEmpty(meshPath))
                return meshPath;
        }

        SkinnedMeshRenderer smr =
            root.GetComponentInChildren<SkinnedMeshRenderer>(true);

        if (smr != null && smr.sharedMesh != null)
        {
            string meshPath =
                AssetDatabase.GetAssetPath(smr.sharedMesh);

            if (!string.IsNullOrEmpty(meshPath))
                return meshPath;
        }

        return "";
    }

    private static string GetFormat(string path)
    {
        if (string.IsNullOrEmpty(path))
            return "";

        string ext = Path.GetExtension(path);

        if (string.IsNullOrEmpty(ext))
            return "";

        return ext.TrimStart('.').ToUpperInvariant();
    }

    // =========================================================
    // GUI 결과
    // =========================================================

    private void DrawSummary()
    {
        if (results.Count == 0)
            return;

        int found =
            results.Count(r => r.foundFrom != "Missing");

        int missing =
            results.Count - found;

        long totalTri =
            results.Sum(r => r.triCount);

        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.LabelField(
            "분석 요약",
            EditorStyles.boldLabel
        );

        EditorGUILayout.LabelField(
            "대상",
            Targets.Length + "종"
        );

        EditorGUILayout.LabelField(
            "찾음",
            found + "종"
        );

        EditorGUILayout.LabelField(
            "누락",
            missing + "종"
        );

        EditorGUILayout.LabelField(
            "전체 Tri 합계",
            totalTri.ToString("N0")
        );

        EditorGUILayout.EndVertical();
    }

    private void DrawTable()
    {
        if (results.Count == 0)
            return;

        EditorGUILayout.Space(6);

        scroll =
            EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("No", GUILayout.Width(32));
        GUILayout.Label("에셋명", GUILayout.Width(245));
        GUILayout.Label("분류", GUILayout.Width(100));
        GUILayout.Label("Original(m)", GUILayout.Width(115));
        GUILayout.Label("X(m)", GUILayout.Width(65));
        GUILayout.Label("Y(m)", GUILayout.Width(65));
        GUILayout.Label("Z(m)", GUILayout.Width(65));
        GUILayout.Label("Tri", GUILayout.Width(95));
        GUILayout.Label("Texture", GUILayout.Width(80));
        GUILayout.Label("Source", GUILayout.Width(70));
        GUILayout.Label("상태");
        EditorGUILayout.EndHorizontal();

        foreach (AssetResult r in results)
        {
            EditorGUILayout.BeginHorizontal("box");

            GUILayout.Label(
                r.no.ToString("00"),
                GUILayout.Width(32)
            );

            GUILayout.Label(
                r.objectName,
                GUILayout.Width(245)
            );

            GUILayout.Label(
                r.category,
                GUILayout.Width(100)
            );

            GUILayout.Label(
                r.originalSizeX_m.ToString("F3") + " x " +
                r.originalSizeY_m.ToString("F3") + " x " +
                r.originalSizeZ_m.ToString("F3"),
                GUILayout.Width(115)
            );

            GUILayout.Label(
                r.sizeX_m.ToString("F3"),
                GUILayout.Width(65)
            );

            GUILayout.Label(
                r.sizeY_m.ToString("F3"),
                GUILayout.Width(65)
            );

            GUILayout.Label(
                r.sizeZ_m.ToString("F3"),
                GUILayout.Width(65)
            );

            GUILayout.Label(
                r.triCount.ToString("N0"),
                GUILayout.Width(95)
            );

            GUILayout.Label(
                r.textureState,
                GUILayout.Width(80)
            );

            GUILayout.Label(
                r.foundFrom,
                GUILayout.Width(70)
            );

            GUILayout.Label(r.status);

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawScaleValidationTable()
    {
        if (scaleChecks.Count == 0)
            return;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField(
            "3-2. 스케일 검증 — 기준 스케일: 플레이어 시점 높이 1.65m",
            EditorStyles.boldLabel
        );

        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUILayout.Label("No", GUILayout.Width(32));
        GUILayout.Label("에셋명", GUILayout.Width(150));
        GUILayout.Label("실물 기준 치수", GUILayout.Width(220));
        GUILayout.Label("Import 직후", GUILayout.Width(100));
        GUILayout.Label("보정 배율", GUILayout.Width(90));
        GUILayout.Label("결과");
        EditorGUILayout.EndHorizontal();

        foreach (ScaleCheckResult check in scaleChecks)
        {
            EditorGUILayout.BeginHorizontal("box");
            GUILayout.Label(check.no.ToString("00"), GUILayout.Width(32));
            GUILayout.Label(check.assetName, GUILayout.Width(150));
            GUILayout.Label(check.realDimension, GUILayout.Width(220));
            GUILayout.Label(
                check.importedImmediately_m.ToString("F2") + "m",
                GUILayout.Width(100)
            );
            GUILayout.Label(
                "x" + check.correctionMultiplier.ToString("F3"),
                GUILayout.Width(90)
            );
            GUILayout.Label(check.result);
            EditorGUILayout.EndHorizontal();
        }
    }

    // =========================================================
    // Export
    // =========================================================

    private void ExportAll()
    {
        if (results.Count == 0)
            return;

        string absoluteFolder =
            GetAbsoluteExportFolder();

        Directory.CreateDirectory(absoluteFolder);

        string csv =
            Path.Combine(
                absoluteFolder,
                "PPE_Asset_Evaluation_34.csv"
            );

        string json =
            Path.Combine(
                absoluteFolder,
                "PPE_Asset_Evaluation_34.json"
            );

        string md =
            Path.Combine(
                absoluteFolder,
                "PPE_Asset_Evaluation_34.md"
            );

        string scaleCsv =
            Path.Combine(
                absoluteFolder,
                "PPE_Scale_Validation_3_2.csv"
            );

        File.WriteAllText(
            csv,
            BuildCsv(),
            new UTF8Encoding(true)
        );

        File.WriteAllText(
            json,
            BuildJson(),
            new UTF8Encoding(false)
        );

        File.WriteAllText(
            md,
            BuildMarkdown(),
            new UTF8Encoding(true)
        );

        File.WriteAllText(
            scaleCsv,
            BuildScaleCsv(),
            new UTF8Encoding(true)
        );

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "완료",
            "CSV / JSON / MD 생성 완료\n\n" + exportFolder,
            "확인"
        );
    }

    private string BuildCsv()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine(
            "No,AssetName,Category,ProjectRole,FoundFrom," +
            "OriginalSizeX_m,OriginalSizeY_m,OriginalSizeZ_m," +
            "SizeX_m,SizeY_m,SizeZ_m,Tri,Texture,TextureCount," +
            "MaxTextureWidth,MaxTextureHeight,TextureMaps,Format," +
            "SourcePath,Status"
        );

        foreach (AssetResult r in results)
        {
            sb.AppendLine(
                r.no + "," +
                Csv(r.objectName) + "," +
                Csv(r.category) + "," +
                Csv(r.role) + "," +
                Csv(r.foundFrom) + "," +
                r.originalSizeX_m.ToString("F3") + "," +
                r.originalSizeY_m.ToString("F3") + "," +
                r.originalSizeZ_m.ToString("F3") + "," +
                r.sizeX_m.ToString("F3") + "," +
                r.sizeY_m.ToString("F3") + "," +
                r.sizeZ_m.ToString("F3") + "," +
                r.triCount + "," +
                Csv(r.textureState) + "," +
                r.textureCount + "," +
                r.maxTextureWidth + "," +
                r.maxTextureHeight + "," +
                Csv(r.textureMaps) + "," +
                Csv(r.format) + "," +
                Csv(r.sourcePath) + "," +
                Csv(r.status)
            );
        }

        return sb.ToString();
    }

    private string BuildScaleCsv()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(
            "No,AssetName,RealDimension,ImportedImmediately_m," +
            "CorrectionMultiplier,CorrectedSize_m,ExpectedCorrectedSize_m," +
            "Result,SourcePath"
        );

        foreach (ScaleCheckResult check in scaleChecks)
        {
            sb.AppendLine(
                check.no + "," +
                Csv(check.assetName) + "," +
                Csv(check.realDimension) + "," +
                check.importedImmediately_m.ToString("F3") + "," +
                check.correctionMultiplier.ToString("F3") + "," +
                check.correctedSize_m.ToString("F3") + "," +
                check.expectedCorrectedSize_m.ToString("F3") + "," +
                Csv(check.result) + "," +
                Csv(check.sourcePath)
            );
        }

        return sb.ToString();
    }

    private string BuildJson()
    {
        ReportWrapper wrapper =
            new ReportWrapper();

        wrapper.generatedAt =
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        wrapper.unityVersion =
            Application.unityVersion;

        wrapper.projectName =
            Application.productName;

        wrapper.purpose =
            "화학물질 안전훈련 VR - PPE 착용 교육 에셋 평가자료 자동분석";

        wrapper.targetCount =
            Targets.Length;

        wrapper.foundCount =
            results.Count(r => r.foundFrom != "Missing");

        wrapper.missingCount =
            Targets.Length - wrapper.foundCount;

        wrapper.totalTri =
            results.Sum(r => r.triCount);

        wrapper.assets =
            results.ToArray();

        wrapper.scaleChecks =
            scaleChecks.ToArray();

        return JsonUtility.ToJson(wrapper, true);
    }

    private string BuildMarkdown()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine("# PPE 에셋 34종 Unity 자동분석 결과");
        sb.AppendLine();
        sb.AppendLine("- 프로젝트: 화학물질 안전훈련 VR");
        sb.AppendLine("- 범위: PPE 착용 교육");
        sb.AppendLine("- 기준: 1 Unity Unit = 1m");
        sb.AppendLine("- 입력 정책: 양손 Grip = Grab / Trigger = 선택·실행");
        sb.AppendLine("- 생성일: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine("- Unity: " + Application.unityVersion);
        sb.AppendLine("- 총 대상: " + Targets.Length + "종");
        sb.AppendLine("- 찾음: " + results.Count(r => r.foundFrom != "Missing") + "종");
        sb.AppendLine("- 전체 Tri: " + results.Sum(r => r.triCount).ToString("N0"));
        sb.AppendLine();

        sb.AppendLine(
            "|No|에셋명|분류|프로젝트 역할|Size X×Y×Z(m)|Tri|Texture|Format|Source|상태|"
        );

        sb.AppendLine(
            "|---:|---|---|---|---:|---:|---|---|---|---|"
        );

        foreach (AssetResult r in results)
        {
            sb.AppendLine(
                "|" + r.no.ToString("00") +
                "|" + Md(r.objectName) +
                "|" + Md(r.category) +
                "|" + Md(r.role) +
                "|" + r.sizeX_m.ToString("F3") +
                " × " + r.sizeY_m.ToString("F3") +
                " × " + r.sizeZ_m.ToString("F3") +
                "|" + r.triCount.ToString("N0") +
                "|" + Md(r.textureState) +
                "|" + Md(r.format) +
                "|" + Md(r.foundFrom) +
                "|" + Md(r.status) +
                "|"
            );
        }

        sb.AppendLine();
        sb.AppendLine("## 에이전트 사용 메모");
        sb.AppendLine();
        sb.AppendLine("- A: 착용/장비 오브젝트");
        sb.AppendLine("- B: PPE룸 설비/환경 오브젝트");
        sb.AppendLine("- C: 상호작용 도구 오브젝트");
        sb.AppendLine("- 15_PPE_C_Tablet은 PPE 착용 전 작업 확인 단계에 사용.");
        sb.AppendLine("- B 환경 에셋은 PPE 위치·배치 공간을 성립시키거나 PPE룸의 몰입·집중도를 조성하는 요소.");
        sb.AppendLine("- Size와 Tri는 Unity 자동측정값이며 임의 수치가 아님.");
        sb.AppendLine("- Missing 항목은 자동으로 찾지 못한 것이므로 경로/이름 확인 필요.");

        return sb.ToString();
    }

    private static string Csv(string value)
    {
        if (value == null)
            value = "";

        return "\"" +
               value.Replace("\"", "\"\"") +
               "\"";
    }

    private static string Md(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        return value
            .Replace("|", "\\|")
            .Replace("\r", " ")
            .Replace("\n", " ");
    }

    private string GetAbsoluteExportFolder()
    {
        string normalized =
            exportFolder.Replace("\\", "/").Trim('/');

        return Path.Combine(
            Directory.GetCurrentDirectory(),
            normalized
        );
    }

    private void RevealExportFolder()
    {
        string folder =
            GetAbsoluteExportFolder();

        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        EditorUtility.RevealInFinder(folder);
    }
}
#endif
