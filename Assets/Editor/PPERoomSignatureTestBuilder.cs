using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class PPERoomSignatureTestBuilder
{
    const string ScenePath = "Assets/Scenes/3_PPE_Room.unity";
    const string RootName = "Signature_Test_Sequence";
    const string ShaderName = "Project/Handwritten Signature Reveal";
    const string MaterialFolder = "Assets/Materials/PPE/Tablet/Signatures";
    const string PlayerTexturePath = "Assets/UIs/sign_stamp/sign_player_rm.png";
    const string ConductorTexturePath = "Assets/UIs/sign_stamp/sign_conductor_ppe.png";
    const string CheckTexturePath = "Assets/UIs/sign_stamp/sign_check.png";
    const string CheckAudioPath = "Assets/Audio/SFX/check.ogg";
    const string SignAudioPath = "Assets/Audio/SFX/sign.ogg";
    const string PlayerMaterialPath = MaterialFolder + "/PlayerSignature_Handwrite.mat";
    const string ConductorMaterialPath = MaterialFolder + "/ConductorSignature_Handwrite.mat";
    const string CheckMaterialPath = MaterialFolder + "/ChecklistCheck_Handwrite.mat";

    [MenuItem("Tools/PPE/Build Tablet Signature Test")]
    public static void Build()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject tablet = GameObject.Find("Tablet (1)");
        Transform documentPlane = tablet != null ? tablet.transform.Find("GeneratedPlane") : null;
        if (documentPlane == null)
        {
            Debug.LogError("Cannot build signature test because Tablet (1)/GeneratedPlane was not found.");
            return;
        }

        Shader signatureShader = Shader.Find(ShaderName);
        if (signatureShader == null)
        {
            Debug.LogError($"Cannot build signature test because shader '{ShaderName}' was not found.");
            return;
        }

        Texture2D playerTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(PlayerTexturePath);
        Texture2D conductorTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(ConductorTexturePath);
        Texture2D checkTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(CheckTexturePath);
        AudioClip checkAudio = AssetDatabase.LoadAssetAtPath<AudioClip>(CheckAudioPath);
        AudioClip signAudio = AssetDatabase.LoadAssetAtPath<AudioClip>(SignAudioPath);
        if (playerTexture == null || conductorTexture == null || checkTexture == null ||
            checkAudio == null || signAudio == null)
        {
            Debug.LogError(
                $"Cannot build signature test. player={playerTexture != null} " +
                $"conductor={conductorTexture != null} check={checkTexture != null} " +
                $"checkAudio={checkAudio != null} signAudio={signAudio != null}");
            return;
        }

        EnsureFolder(MaterialFolder);
        Material playerMaterial = CreateOrUpdateMaterial(
            PlayerMaterialPath,
            signatureShader,
            playerTexture,
            new Vector4(0.15f, 0.24f, 0.91f, 0.76f),
            true,
            0.62f,
            0.2f);
        Material checkMaterial = CreateOrUpdateMaterial(
            CheckMaterialPath,
            signatureShader,
            checkTexture,
            new Vector4(0f, 0f, 1f, 1f),
            true,
            0.62f,
            0.12f);
        checkMaterial.SetColor("_InkColor", new Color(0.01f, 0.025f, 0.03f, 1f));
        checkMaterial.SetFloat("_InkExpansion", 0.025f);
        checkMaterial.SetFloat("_AlphaBoost", 3f);
        EditorUtility.SetDirty(checkMaterial);
        Material conductorMaterial = CreateOrUpdateMaterial(
            ConductorMaterialPath,
            signatureShader,
            conductorTexture,
            new Vector4(0.25f, 0.25f, 0.84f, 0.76f),
            true,
            0.62f,
            0.2f);

        Transform existing = documentPlane.Find(RootName);
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        GameObject root = new(RootName);
        root.layer = documentPlane.gameObject.layer;
        root.transform.SetParent(documentPlane, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        MeshRenderer playerRenderer = CreateSignatureQuad(
            root.transform,
            "Player_Signature",
            new Vector3(-2.64f, -1.65f, -0.25f),
            new Vector3(2f, 0.26f, 1f),
            playerMaterial);
        MeshRenderer conductorRenderer = CreateSignatureQuad(
            root.transform,
            "Conductor_Signature",
            new Vector3(-0.03f, -1.65f, -0.25f),
            new Vector3(2f, 0.26f, 1f),
            conductorMaterial);
        MeshRenderer checkerRenderer = CreateSignatureQuad(
            root.transform,
            "Checker_Signature",
            new Vector3(2.58f, -1.65f, -0.25f),
            new Vector3(2f, 0.26f, 1f),
            conductorMaterial);

        float[] checkYPositions = { -0.07f, -0.22f, -0.37f, -0.52f, -0.67f };
        MeshRenderer[] checkRenderers = new MeshRenderer[checkYPositions.Length];
        for (int i = 0; i < checkYPositions.Length; i++)
        {
            checkRenderers[i] = CreateSignatureQuad(
                root.transform,
                $"Checklist_Check_{i + 1:00}",
                new Vector3(-3.74f, checkYPositions[i], -0.25f),
                new Vector3(0.34f, 0.103f, 1f),
                checkMaterial);
        }

        HandwrittenSignatureSequence sequence = root.AddComponent<HandwrittenSignatureSequence>();
        AudioSource audioSource = root.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 1f;
        audioSource.dopplerLevel = 0f;
        audioSource.minDistance = 3f;
        audioSource.maxDistance = 20f;

        SerializedObject serializedSequence = new(sequence);
        serializedSequence.FindProperty("playOnEnable").boolValue = true;
        serializedSequence.FindProperty("useUnscaledTime").boolValue = false;
        serializedSequence.FindProperty("initialDelay").floatValue = 0.8f;
        serializedSequence.FindProperty("audioSource").objectReferenceValue = audioSource;
        serializedSequence.FindProperty("duckBgmDuringPlayback").boolValue = true;
        serializedSequence.FindProperty("duckedBgmVolume").floatValue = 0.25f;

        SerializedProperty steps = serializedSequence.FindProperty("signatures");
        steps.arraySize = checkRenderers.Length + 3;
        for (int i = 0; i < checkRenderers.Length; i++)
        {
            ConfigureStep(
                steps.GetArrayElementAtIndex(i),
                checkRenderers[i],
                checkAudio,
                i == 0 ? 0f : 0.12f,
                checkAudio.length,
                true);
        }

        int signatureIndex = checkRenderers.Length;
        ConfigureStep(
            steps.GetArrayElementAtIndex(signatureIndex),
            playerRenderer,
            signAudio,
            0.35f,
            signAudio.length,
            true);
        ConfigureStep(
            steps.GetArrayElementAtIndex(signatureIndex + 1),
            conductorRenderer,
            null,
            0f,
            signAudio.length,
            false);
        ConfigureStep(
            steps.GetArrayElementAtIndex(signatureIndex + 2),
            checkerRenderer,
            null,
            0f,
            signAudio.length,
            false);
        serializedSequence.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(root);
        EditorUtility.SetDirty(documentPlane.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = root;
        Debug.Log("Built PPE tablet signature test on work_confirm document.", root);
    }

    static void ConfigureStep(
        SerializedProperty step,
        Renderer renderer,
        AudioClip sound,
        float delayBefore,
        float duration,
        bool animateReveal)
    {
        step.FindPropertyRelative("targetRenderer").objectReferenceValue = renderer;
        step.FindPropertyRelative("sound").objectReferenceValue = sound;
        step.FindPropertyRelative("soundVolume").floatValue = 1f;
        step.FindPropertyRelative("animateReveal").boolValue = animateReveal;
        step.FindPropertyRelative("delayBefore").floatValue = delayBefore;
        step.FindPropertyRelative("duration").floatValue = duration;
        step.FindPropertyRelative("revealCurve").animationCurveValue =
            AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    }

    static MeshRenderer CreateSignatureQuad(
        Transform parent,
        string name,
        Vector3 localPosition,
        Vector3 localScale,
        Material material)
    {
        GameObject signature = GameObject.CreatePrimitive(PrimitiveType.Quad);
        signature.name = name;
        signature.layer = parent.gameObject.layer;
        signature.transform.SetParent(parent, false);
        signature.transform.localPosition = localPosition;
        signature.transform.localRotation = Quaternion.identity;
        signature.transform.localScale = localScale;

        Collider collider = signature.GetComponent<Collider>();
        if (collider != null)
            Object.DestroyImmediate(collider);

        MeshRenderer renderer = signature.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        renderer.sortingOrder = 20;
        return renderer;
    }

    static Material CreateOrUpdateMaterial(
        string path,
        Shader shader,
        Texture texture,
        Vector4 cropRect,
        bool useTextureAlpha,
        float threshold,
        float softness)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.shader = shader;
        }

        material.SetTexture("_MainTex", texture);
        material.SetColor("_InkColor", new Color(0.025f, 0.055f, 0.075f, 0.96f));
        material.SetFloat("_Reveal", 0f);
        material.SetFloat("_RevealSoftness", 0.025f);
        material.SetFloat("_InkThreshold", threshold);
        material.SetFloat("_InkSoftness", softness);
        material.SetFloat("_UseTextureAlpha", useTextureAlpha ? 1f : 0f);
        material.SetFloat("_InkExpansion", 0f);
        material.SetFloat("_AlphaBoost", 1f);
        material.SetVector("_CropRect", cropRect);
        material.enableInstancing = true;
        material.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(material);
        return material;
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string folderName = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }
}
