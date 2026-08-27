using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Reversible editor-only test for removing normal-map and occlusion-map usage
/// from project materials associated with imported FBX content.
/// </summary>
public static class FbxNormalOcclusionTest
{
    private const string SnapshotPath = "Assets/Editor/FbxNormalOcclusionTestSnapshot.json";
    private static readonly string[] Roots = { "Assets/FBX", "Assets/Materials" };
    private static readonly string[] NormalProperties = { "_BumpMap", "_NormalMap" };
    private static readonly string[] NormalKeywords = { "_NORMALMAP" };
    private static readonly string[] OcclusionKeywords = { "_OCCLUSIONMAP" };

    [Serializable]
    private class Snapshot
    {
        public List<MaterialRecord> materials = new List<MaterialRecord>();
        public string createdUtc;
    }

    [Serializable]
    private class MaterialRecord
    {
        public string materialPath;
        public string bumpMapGuid;
        public string normalMapGuid;
        public string occlusionMapGuid;
        public bool hasOcclusionStrength;
        public float occlusionStrength;
        public bool normalKeyword;
        public bool occlusionKeyword;
    }

    [MenuItem("Tools/FBX Texture Test/Disable Normal Maps + Occlusion (Create Snapshot)", priority = 200)]
    public static void DisableNormalMapsAndOcclusion()
    {
        if (File.Exists(SnapshotPath))
        {
            EditorUtility.DisplayDialog("FBX Texture Test", "이미 스냅샷이 있습니다. 먼저 Restore Snapshot을 실행하거나 Clear Snapshot을 선택하세요.", "확인");
            return;
        }

        if (!EditorUtility.DisplayDialog(
                "FBX Texture Test",
                "Assets/FBX와 Assets/Materials 머티리얼에서 노멀맵과 글로벌 오클루전 사용을 끕니다.\n\n원복을 위해 현재 상태를 스냅샷으로 저장합니다. 계속할까요?",
                "스냅샷 저장 후 실행",
                "취소"))
            return;

        var snapshot = new Snapshot { createdUtc = DateTime.UtcNow.ToString("O") };
        var materials = FindTargetMaterials();
        int changed = 0;

        foreach (var material in materials)
        {
            var record = Capture(material);
            if (record == null)
                continue;

            snapshot.materials.Add(record);
            foreach (var property in NormalProperties)
                if (material.HasProperty(property)) material.SetTexture(property, null);
            if (material.HasProperty("_OcclusionMap")) material.SetTexture("_OcclusionMap", null);
            if (material.HasProperty("_OcclusionStrength")) material.SetFloat("_OcclusionStrength", 0f);
            foreach (var keyword in NormalKeywords) material.DisableKeyword(keyword);
            foreach (var keyword in OcclusionKeywords) material.DisableKeyword(keyword);
            EditorUtility.SetDirty(material);
            changed++;
        }

        if (snapshot.materials.Count == 0)
        {
            EditorUtility.DisplayDialog("FBX Texture Test", "대상 폴더에서 노멀맵 또는 AO를 사용하는 머티리얼을 찾지 못했습니다.", "확인");
            return;
        }

        File.WriteAllText(SnapshotPath, JsonUtility.ToJson(snapshot, true));
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[FBX Texture Test] 노멀맵/AO를 끈 머티리얼 {changed}개. 원복 스냅샷: {SnapshotPath}");
    }

    [MenuItem("Tools/FBX Texture Test/Restore Original Normal Maps + Occlusion", priority = 201)]
    public static void RestoreOriginal()
    {
        if (!File.Exists(SnapshotPath))
        {
            EditorUtility.DisplayDialog("FBX Texture Test", "복원할 스냅샷이 없습니다.", "확인");
            return;
        }

        var snapshot = JsonUtility.FromJson<Snapshot>(File.ReadAllText(SnapshotPath));
        int restored = 0;
        foreach (var record in snapshot.materials)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(record.materialPath);
            if (material == null)
                continue;

            if (material.HasProperty("_BumpMap")) material.SetTexture("_BumpMap", LoadTexture(record.bumpMapGuid));
            if (material.HasProperty("_NormalMap")) material.SetTexture("_NormalMap", LoadTexture(record.normalMapGuid));
            if (material.HasProperty("_OcclusionMap")) material.SetTexture("_OcclusionMap", LoadTexture(record.occlusionMapGuid));
            if (record.hasOcclusionStrength && material.HasProperty("_OcclusionStrength")) material.SetFloat("_OcclusionStrength", record.occlusionStrength);
            SetKeyword(material, "_NORMALMAP", record.normalKeyword);
            SetKeyword(material, "_OCCLUSIONMAP", record.occlusionKeyword);
            EditorUtility.SetDirty(material);
            restored++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[FBX Texture Test] 원래 노멀맵/AO를 복원한 머티리얼 {restored}개.");
    }

    [MenuItem("Tools/FBX Texture Test/Clear Snapshot", priority = 202)]
    public static void ClearSnapshot()
    {
        if (!File.Exists(SnapshotPath)) return;
        if (!EditorUtility.DisplayDialog("FBX Texture Test", "스냅샷 파일을 삭제할까요? 먼저 Restore를 실행했는지 확인하세요.", "삭제", "취소")) return;
        AssetDatabase.DeleteAsset(SnapshotPath);
        AssetDatabase.Refresh();
    }

    private static List<Material> FindTargetMaterials()
    {
        var result = new List<Material>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var guid in AssetDatabase.FindAssets("t:Material", Roots))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!seen.Add(path)) continue;
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) result.Add(material);
        }
        return result;
    }

    private static MaterialRecord Capture(Material material)
    {
        bool hasNormal = false;
        foreach (var property in NormalProperties)
            hasNormal |= material.HasProperty(property) && material.GetTexture(property) != null;
        bool hasOcclusion = material.HasProperty("_OcclusionMap") && material.GetTexture("_OcclusionMap") != null;
        bool hasKeywords = material.IsKeywordEnabled("_NORMALMAP") || material.IsKeywordEnabled("_OCCLUSIONMAP");
        if (!hasNormal && !hasOcclusion && !hasKeywords) return null;

        return new MaterialRecord
        {
            materialPath = AssetDatabase.GetAssetPath(material),
            bumpMapGuid = TextureGuid(material, "_BumpMap"),
            normalMapGuid = TextureGuid(material, "_NormalMap"),
            occlusionMapGuid = TextureGuid(material, "_OcclusionMap"),
            hasOcclusionStrength = material.HasProperty("_OcclusionStrength"),
            occlusionStrength = material.HasProperty("_OcclusionStrength") ? material.GetFloat("_OcclusionStrength") : 0f,
            normalKeyword = material.IsKeywordEnabled("_NORMALMAP"),
            occlusionKeyword = material.IsKeywordEnabled("_OCCLUSIONMAP")
        };
    }

    private static string TextureGuid(Material material, string property)
    {
        if (!material.HasProperty(property)) return string.Empty;
        var texture = material.GetTexture(property);
        return texture == null ? string.Empty : AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(texture));
    }

    private static Texture LoadTexture(string guid)
    {
        if (string.IsNullOrEmpty(guid)) return null;
        return AssetDatabase.LoadAssetAtPath<Texture>(AssetDatabase.GUIDToAssetPath(guid));
    }

    private static void SetKeyword(Material material, string keyword, bool enabled)
    {
        if (enabled) material.EnableKeyword(keyword);
        else material.DisableKeyword(keyword);
    }
}
