using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class PPEWallPosterSetup
{
    const string TargetScenePath = "Assets/Scenes/3_PPE_Room_Train_Test_mask.unity";
    const string CopyTargetScenePath = "Assets/Scenes/3_PPE_Room_Train_Test_1.unity";
    const string TargetWallPath = "PPE Room/Room/Rear Wall";
    const string PosterRootName = "PPE Wall Posters";
    const string MaterialFolder = "Assets/Materials/PPE/Posters";
    const string UnlitShaderName = "Universal Render Pipeline/Unlit";

    const float PosterHeight = 1.65f;
    const float PosterGap = 0.28f;
    const float PosterCenterOffsetWorldY = -1.5f;
    const float WallSurfaceOffset = 0.012f;

    static readonly string[] TexturePaths =
    {
        "Assets/UIs/Poster/PPE_Poster_0.png",
        "Assets/UIs/Poster/PPE_Poster_1.png",
        "Assets/UIs/Poster/PPE_Poster_2.png"
    };

    [MenuItem("Tools/PPE/Fix Poster 2 Aspect Preserving Height &%#2")]
    public static void FixPoster2AspectPreservingHeight()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!ValidateActiveScene(scene))
            return;

        if (scene.isDirty)
        {
            Debug.LogError(
                "Poster 2 aspect correction stopped because the active scene has unrelated unsaved changes. " +
                "Save or revert those changes first.");
            return;
        }

        GameObject wall = GameObject.Find(TargetWallPath);
        Transform root = wall != null ? wall.transform.Find(PosterRootName) : null;
        Transform poster = root != null ? root.Find("PPE_Poster_2") : null;
        if (poster == null)
        {
            Debug.LogError($"Poster 2 aspect correction stopped because '{TargetWallPath}/{PosterRootName}/PPE_Poster_2' is missing.");
            return;
        }

        TextureImporter importer = AssetImporter.GetAtPath(TexturePaths[2]) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"Poster 2 aspect correction stopped because '{TexturePaths[2]}' has no TextureImporter.");
            return;
        }

        importer.GetSourceTextureWidthAndHeight(out int sourceWidth, out int sourceHeight);
        if (sourceWidth <= 0 || sourceHeight <= 0)
        {
            Debug.LogError($"Poster 2 aspect correction stopped because source dimensions are invalid: {sourceWidth}x{sourceHeight}.");
            return;
        }

        Vector3 parentScale = Abs(poster.parent.lossyScale);
        if (parentScale.x <= Mathf.Epsilon || parentScale.y <= Mathf.Epsilon)
        {
            Debug.LogError("Poster 2 aspect correction stopped because its parent has a zero world scale.");
            return;
        }

        float sourceAspect = (float)sourceWidth / sourceHeight;
        Vector3 localScale = poster.localScale;
        float xSign = localScale.x < 0f ? -1f : 1f;
        float correctedX = Mathf.Abs(localScale.y) * sourceAspect * parentScale.y / parentScale.x;

        Undo.RecordObject(poster, "Fix PPE Poster 2 aspect");
        localScale.x = correctedX * xSign;
        poster.localScale = localScale;
        EditorUtility.SetDirty(poster);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log(
            $"Corrected PPE_Poster_2 aspect from source {sourceWidth}x{sourceHeight} ({sourceAspect:F6}). " +
            $"Preserved position={poster.localPosition}, rotation={poster.localEulerAngles}, " +
            $"localScaleY={localScale.y}, localScaleZ={localScale.z}; set localScaleX={localScale.x:F8}.");
    }

    [MenuItem("Tools/PPE/Create Wall Posters on Rear Wall &%#p")]
    public static void Create()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!ValidateActiveScene(scene))
            return;

        if (scene.isDirty)
        {
            Debug.LogError(
                "Wall poster creation stopped because the active scene has unsaved changes. " +
                "Save or revert those changes first so this command does not persist unrelated work.");
            return;
        }

        GameObject wall = GameObject.Find(TargetWallPath);
        if (wall == null)
        {
            Debug.LogError($"Wall poster creation stopped because '{TargetWallPath}' was not found.");
            return;
        }

        if (wall.transform.Find(PosterRootName) != null)
        {
            Debug.LogError(
                $"Wall poster creation stopped because '{TargetWallPath}/{PosterRootName}' already exists. " +
                "The existing scene-authored poster transforms were not overwritten.");
            return;
        }

        Texture2D[] textures = LoadTextures();
        if (textures == null)
            return;

        Shader shader = Shader.Find(UnlitShaderName);
        if (shader == null)
        {
            Debug.LogError($"Wall poster creation stopped because shader '{UnlitShaderName}' was not found.");
            return;
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Create PPE wall posters");

        try
        {
            EnsureAssetFolder(MaterialFolder);
            Material[] materials = CreateOrLoadMaterials(shader, textures);

            GameObject root = new(PosterRootName);
            Undo.RegisterCreatedObjectUndo(root, "Create PPE wall poster root");
            root.layer = wall.layer;
            root.transform.SetParent(wall.transform, false);

            Vector3 wallScale = Abs(wall.transform.lossyScale);
            if (wallScale.x <= Mathf.Epsilon || wallScale.y <= Mathf.Epsilon || wallScale.z <= Mathf.Epsilon)
                throw new InvalidOperationException($"'{TargetWallPath}' has a zero world scale.");

            root.transform.localPosition = new Vector3(
                0f,
                PosterCenterOffsetWorldY / wallScale.y,
                WallSurfaceOffset / wallScale.z);
            root.transform.localRotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;

            float[] widths = new float[textures.Length];
            float totalWidth = PosterGap * (textures.Length - 1);
            for (int i = 0; i < textures.Length; i++)
            {
                widths[i] = PosterHeight * textures[i].width / textures[i].height;
                totalWidth += widths[i];
            }

            float cursor = -totalWidth * 0.5f;
            for (int i = 0; i < textures.Length; i++)
            {
                float centerX = -(cursor + widths[i] * 0.5f);
                CreatePoster(root.transform, wall.layer, i, centerX, widths[i], wallScale, materials[i]);
                cursor += widths[i] + PosterGap;
            }

            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene);
            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log(
                $"Created 3 PPE wall posters in '{scene.path}' under '{TargetWallPath}/{PosterRootName}'. " +
                "The source image aspect ratios were preserved and no colliders or runtime components were added.");
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(undoGroup);
            Debug.LogException(exception);
        }
    }

    [MenuItem("Tools/PPE/Copy Wall Posters To Train Test 1 &%#c")]
    public static void CopyToTrainTest1()
    {
        Scene sourceScene = SceneManager.GetActiveScene();
        if (sourceScene.path != TargetScenePath)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene loaded = SceneManager.GetSceneAt(i);
                if (loaded.path == TargetScenePath)
                {
                    sourceScene = loaded;
                    SceneManager.SetActiveScene(sourceScene);
                    break;
                }
            }

            if (sourceScene.path != TargetScenePath)
                sourceScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);

            if (!sourceScene.IsValid() || sourceScene.path != TargetScenePath)
            {
                Debug.LogError($"Poster copy stopped because source scene '{TargetScenePath}' could not be opened.");
                return;
            }
        }

        Transform sourceRoot = FindScenePath(sourceScene, TargetWallPath + "/" + PosterRootName);
        if (sourceRoot == null || sourceRoot.childCount != 6)
        {
            Debug.LogError(
                $"Poster copy stopped. Expected six children at '{TargetWallPath}/{PosterRootName}', " +
                $"found {(sourceRoot == null ? "no root" : sourceRoot.childCount.ToString())}.");
            return;
        }

        Scene targetScene = EditorSceneManager.OpenScene(CopyTargetScenePath, OpenSceneMode.Additive);
        Transform targetWall = FindScenePath(targetScene, TargetWallPath);
        if (targetWall == null)
        {
            Debug.LogError($"Poster copy stopped because target wall '{TargetWallPath}' was not found in '{CopyTargetScenePath}'.");
            return;
        }

        if (targetWall.Find(PosterRootName) != null)
        {
            Debug.LogError(
                $"Poster copy stopped because '{TargetWallPath}/{PosterRootName}' already exists in '{CopyTargetScenePath}'. " +
                "The target scene was not overwritten.");
            return;
        }

        GameObject clone = null;
        try
        {
            clone = UnityEngine.Object.Instantiate(sourceRoot.gameObject);
            clone.name = PosterRootName;
            clone.transform.SetParent(targetWall, false);
            clone.transform.localPosition = sourceRoot.localPosition;
            clone.transform.localRotation = sourceRoot.localRotation;
            clone.transform.localScale = sourceRoot.localScale;
            SceneManager.MoveGameObjectToScene(clone, targetScene);

            EditorSceneManager.MarkSceneDirty(targetScene);
            EditorSceneManager.SaveScene(targetScene);
            EditorSceneManager.SetActiveScene(targetScene);
            Selection.activeGameObject = clone;
            EditorGUIUtility.PingObject(clone);
            Debug.Log(
                $"Copied six poster objects from '{TargetScenePath}' to '{CopyTargetScenePath}' under '{TargetWallPath}/{PosterRootName}'. " +
                "The source scene was not saved or modified.");
        }
        catch (Exception exception)
        {
            if (clone != null)
                UnityEngine.Object.DestroyImmediate(clone);
            Debug.LogException(exception);
        }
    }

    [MenuItem("Tools/PPE/Validate Wall Posters &%#v")]
    public static void Validate()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!ValidateActiveScene(scene))
            return;

        List<string> failures = new();
        GameObject wall = GameObject.Find(TargetWallPath);
        if (wall == null)
        {
            failures.Add($"Missing wall: {TargetWallPath}");
        }
        else
        {
            Transform root = wall.transform.Find(PosterRootName);
            if (root == null)
            {
                failures.Add($"Missing poster root: {TargetWallPath}/{PosterRootName}");
            }
            else
            {
                ValidatePosters(root, failures);
            }
        }

        if (failures.Count > 0)
        {
            Debug.LogError("PPE wall poster validation failed:\n- " + string.Join("\n- ", failures));
            return;
        }

        Debug.Log(
            "PPE wall poster validation passed: scene path, hierarchy, three textures, URP Unlit materials, " +
            "room-facing geometry, and collider-free geometry are correct. Authored transforms were not validated or changed.");
    }

    [MenuItem("Tools/PPE/Frame Wall Posters From Room Side &%#f")]
    public static void FrameFromRoomSide()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!ValidateActiveScene(scene))
            return;

        GameObject wall = GameObject.Find(TargetWallPath);
        Transform root = wall != null ? wall.transform.Find(PosterRootName) : null;
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (wall == null || root == null || sceneView == null)
        {
            Debug.LogError("Cannot frame the wall posters because the wall, poster root, or Scene view is missing.");
            return;
        }

        Quaternion viewRotation = Quaternion.LookRotation(-wall.transform.forward, wall.transform.up);
        sceneView.LookAt(root.position, viewRotation, 3.5f, false, true);
        sceneView.Focus();
        SceneView.RepaintAll();
    }

    static bool ValidateActiveScene(Scene scene)
    {
        if (scene.path == TargetScenePath)
            return true;

        Debug.LogError(
            $"Wall poster command is restricted to '{TargetScenePath}'. Active scene: '{scene.path}'.");
        return false;
    }

    static Transform FindScenePath(Scene scene, string path)
    {
        string[] segments = path.Split('/');
        if (segments.Length == 0)
            return null;

        Transform current = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == segments[0])
            {
                current = root.transform;
                break;
            }
        }

        if (current == null)
            return null;

        for (int i = 1; i < segments.Length; i++)
        {
            current = current.Find(segments[i]);
            if (current == null)
                return null;
        }

        return current;
    }

    static Texture2D[] LoadTextures()
    {
        Texture2D[] textures = new Texture2D[TexturePaths.Length];
        for (int i = 0; i < TexturePaths.Length; i++)
        {
            textures[i] = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePaths[i]);
            if (textures[i] != null)
                continue;

            Debug.LogError($"Wall poster creation stopped because texture '{TexturePaths[i]}' was not found.");
            return null;
        }

        return textures;
    }

    static Material[] CreateOrLoadMaterials(Shader shader, IReadOnlyList<Texture2D> textures)
    {
        Material[] materials = new Material[textures.Count];
        for (int i = 0; i < textures.Count; i++)
        {
            string materialPath = GetMaterialPath(i);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material != null)
            {
                Texture assignedTexture = material.HasProperty("_BaseMap")
                    ? material.GetTexture("_BaseMap")
                    : material.mainTexture;
                if (material.shader == null || material.shader.name != UnlitShaderName || assignedTexture != textures[i])
                {
                    throw new InvalidOperationException(
                        $"Existing authored material '{materialPath}' does not match poster {i}. " +
                        "It was not overwritten.");
                }

                materials[i] = material;
                continue;
            }

            material = new Material(shader)
            {
                name = $"PPE_Poster_{i}_Unlit"
            };
            material.SetTexture("_BaseMap", textures[i]);
            material.SetColor("_BaseColor", Color.white);
            if (material.HasProperty("_Cull"))
                material.SetFloat("_Cull", (float)CullMode.Back);

            AssetDatabase.CreateAsset(material, materialPath);
            materials[i] = material;
        }

        return materials;
    }

    static void CreatePoster(
        Transform parent,
        int layer,
        int index,
        float centerWorldX,
        float width,
        Vector3 wallScale,
        Material material)
    {
        GameObject poster = GameObject.CreatePrimitive(PrimitiveType.Quad);
        poster.name = $"PPE_Poster_{index}";
        Undo.RegisterCreatedObjectUndo(poster, $"Create PPE poster {index}");
        poster.layer = layer;
        poster.transform.SetParent(parent, false);
        poster.transform.localPosition = new Vector3(centerWorldX / wallScale.x, 0f, 0f);
        poster.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        poster.transform.localScale = new Vector3(width / wallScale.x, PosterHeight / wallScale.y, 1f);

        MeshRenderer renderer = poster.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

        Collider collider = poster.GetComponent<Collider>();
        if (collider != null)
            Undo.DestroyObjectImmediate(collider);
    }

    static void ValidatePosters(Transform root, ICollection<string> failures)
    {
        if (root.childCount != TexturePaths.Length)
            failures.Add($"Expected {TexturePaths.Length} poster children, found {root.childCount}.");

        for (int i = 0; i < TexturePaths.Length; i++)
        {
            Transform poster = root.Find($"PPE_Poster_{i}");
            if (poster == null)
            {
                failures.Add($"Missing poster child: PPE_Poster_{i}");
                continue;
            }

            if (poster.GetComponent<Collider>() != null)
                failures.Add($"PPE_Poster_{i} must not have a Collider.");

            Vector3 posterFront = -poster.forward;
            Vector3 roomDirection = root.parent.forward;
            if (Vector3.Dot(posterFront, roomDirection) < 0.99f)
                failures.Add($"PPE_Poster_{i} front does not face into the room.");

            MeshRenderer renderer = poster.GetComponent<MeshRenderer>();
            MeshFilter filter = poster.GetComponent<MeshFilter>();
            if (renderer == null || filter == null || filter.sharedMesh == null)
            {
                failures.Add($"PPE_Poster_{i} is missing its MeshRenderer or Quad mesh.");
                continue;
            }

            Material material = renderer.sharedMaterial;
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePaths[i]);
            if (material == null || material.shader == null || material.shader.name != UnlitShaderName)
            {
                failures.Add($"PPE_Poster_{i} does not use '{UnlitShaderName}'.");
                continue;
            }

            Texture assignedTexture = material.HasProperty("_BaseMap")
                ? material.GetTexture("_BaseMap")
                : material.mainTexture;
            if (texture == null || assignedTexture != texture)
                failures.Add($"PPE_Poster_{i} does not reference '{TexturePaths[i]}'.");

        }
    }

    static string GetMaterialPath(int index)
    {
        return $"{MaterialFolder}/PPE_Poster_{index}_Unlit.mat";
    }

    static Vector3 Abs(Vector3 value)
    {
        return new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
    }

    static void EnsureAssetFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
