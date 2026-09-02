using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class SceneDependencyValidationHarness
{
    readonly struct ExpectedScene
    {
        public ExpectedScene(string path, string guid, bool enabled)
        {
            Path = path;
            Guid = guid;
            Enabled = enabled;
        }

        public string Path { get; }
        public string Guid { get; }
        public bool Enabled { get; }
    }

    static readonly ExpectedScene[] ExpectedBuildScenes =
    {
        new("Assets/Scenes/0_App.unity", "f82a172c14e36cd4d8f076b4dbfb2612", true),
        new("Assets/Scenes/1_Title.unity", "4688f76b6baf5ec4d9e09fda55fad950", true),
        new("Assets/Scenes/2_Intro.unity", "7def8ae4c77d1fd43affdc27146a489f", true),
        new("Assets/Scenes/3_Loading.unity", "33987000c09f408ab5a5ca7a4e73cd12", true),
        new("Assets/Scenes/4_PPE_Room.unity", "e4c91a8b7d254f3aa6e0c1d5928476bf", true),
        new("Assets/Scenes/5_MixerRoom.unity", "d1925986e2c5e3b46819e2fd8e31ee7e", false),
        new("Assets/Scenes/6_InsideMixer.unity", "de100982fa3753449a7976e76e56c436", false),
    };

    static readonly string[] LegacySceneTokens =
    {
        "6_LoadingScene_0",
        "3_PPE_Room_3mode_loco",
        "5_MixerRoom_Unlit_scale",
        "5_MixerRoom_Unlit",
        "4_InsideMixer",
        "6_LoadingScene",
    };

    static readonly IReadOnlyDictionary<string, string> CriticalMixerAssets =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Assets/Prefabs/Floor.prefab"] = "94d73b4974d94284a9259a144c0a0b80",
            ["Assets/Prefabs/Road_set_v1.FBX"] = "c7f7a9ec09a97d144963b6dd16448dc3",
            ["Assets/RPG_FPS_game_assets_industrial/Buildings/Industrial/Hangars/Hangar_v2/Source/Hangar_v2.mat"] =
                "8d81f9b16a9135446b5451caa37570ee",
            ["Assets/RPG_FPS_game_assets_industrial/Buildings/Industrial/Hangars/Hangar_v2/Source/Hangar_v2.tga"] =
                "54db2c86883388c4eb087ed2a47c9938",
            ["Assets/RPG_FPS_game_assets_industrial/Buildings/Industrial/Hangars/Hangar_v2/Source/Hangar_v2_6.FBX"] =
                "d38c6eb5eac83874ca4e1361ff5ff63c",
            ["Assets/RPG_FPS_game_assets_industrial/Buildings/Industrial/Hangars/Hangars_interior/Source/Hangars_interior_v1.mat"] =
                "3c221020d878d374c985ed24b385fedb",
            ["Assets/RPG_FPS_game_assets_industrial/Buildings/Industrial/Hangars/Hangars_interior/Source/Hangars_interior_v1.tga"] =
                "7e8aaa8126dc6274b962d65ff7529e5c",
            ["Assets/RPG_FPS_game_assets_industrial/Other_props/Support_set/Support_set_v1/Source/Support_set_v1.mat"] =
                "4d4b2f88cea09284f84181568075554b",
            ["Assets/RPG_FPS_game_assets_industrial/Other_props/Support_set/Support_set_v1/Source/Support_set_v1.tga"] =
                "86b604fae468ab24fa69d961eac2d3c8",
            ["Assets/RPG_FPS_game_assets_industrial/Textures/Asphalt/Seamless_asphalt_v1/Seamless_asphalt_v1.mat"] =
                "999517f045d65924db469ec2a4a5c833",
            ["Assets/RPG_FPS_game_assets_industrial/Textures/Asphalt/Seamless_asphalt_v1/Seamless_asphalt_v1.tga"] =
                "b0fe82528f70e8947aa0d123fa3194fe",
        };

    [MenuItem("Tools/Build/Validate Scene Dependencies")]
    public static void Validate()
    {
        List<string> failures = new();
        ValidateBuildSettings(failures);
        ValidateSceneAssets(failures);
        ValidateSerializedTargets(failures);
        ValidateCriticalMixerAssets(failures);
        ValidateProjectOwnedReferences(failures);

        if (failures.Count > 0)
        {
            string message = "[Scene Dependency Validation] FAIL:\n- " +
                string.Join("\n- ", failures);
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        Debug.Log(
            "[Scene Dependency Validation] PASS: build order, enabled states, GUIDs, " +
            "serialized transitions, and project-owned scene-name references are consistent.");
    }

    static void ValidateBuildSettings(List<string> failures)
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        if (scenes.Length < ExpectedBuildScenes.Length)
        {
            failures.Add(
                $"Build Settings contains {scenes.Length} scenes; expected at least " +
                $"{ExpectedBuildScenes.Length}.");
            return;
        }

        for (int index = 0; index < ExpectedBuildScenes.Length; index++)
        {
            ExpectedScene expected = ExpectedBuildScenes[index];
            EditorBuildSettingsScene actual = scenes[index];
            if (!string.Equals(actual.path, expected.Path, StringComparison.Ordinal))
            {
                failures.Add(
                    $"Build scene {index} path is '{actual.path}', expected '{expected.Path}'.");
            }
            if (actual.enabled != expected.Enabled)
            {
                failures.Add(
                    $"Build scene {index} enabled={actual.enabled}, expected {expected.Enabled}.");
            }
            if (!string.Equals(actual.guid.ToString(), expected.Guid, StringComparison.Ordinal))
            {
                failures.Add(
                    $"Build scene {index} GUID is '{actual.guid}', expected '{expected.Guid}'.");
            }
        }

        foreach (EditorBuildSettingsScene scene in scenes)
        {
            if (LegacySceneTokens.Any(token =>
                    scene.path.IndexOf(token, StringComparison.Ordinal) >= 0))
            {
                failures.Add($"Build Settings still contains legacy path '{scene.path}'.");
            }
        }
    }

    static void ValidateSceneAssets(List<string> failures)
    {
        foreach (ExpectedScene expected in ExpectedBuildScenes)
        {
            if (!File.Exists(expected.Path))
            {
                failures.Add($"Scene asset is missing: {expected.Path}");
                continue;
            }

            string actualGuid = AssetDatabase.AssetPathToGUID(expected.Path);
            if (!string.Equals(actualGuid, expected.Guid, StringComparison.Ordinal))
            {
                failures.Add(
                    $"Scene GUID mismatch for '{expected.Path}': " +
                    $"actual={actualGuid}, expected={expected.Guid}.");
            }
        }
    }

    static void ValidateSerializedTargets(List<string> failures)
    {
        RequireText("Assets/Scenes/2_Intro.unity", "nextSceneName: 4_PPE_Room", failures);
        RequireText("Assets/Scenes/2_Intro_Multi.unity", "nextSceneName: 4_PPE_Room", failures);
        RequireText("Assets/Scenes/3_Loading.unity", "nextSceneName: 4_PPE_Room", failures);
        RequireText("Assets/Scripts/LoadingSceneController.cs", "LoadingSceneName = \"3_Loading\"", failures);
        RequireText("Assets/Scripts/LoadingSceneController.cs", "nextSceneName = \"4_PPE_Room\"", failures);
        RequireText("Assets/Scripts/IntroSceneTransition.cs", "nextSceneName = \"4_PPE_Room\"", failures);
    }

    static void ValidateCriticalMixerAssets(List<string> failures)
    {
        foreach (KeyValuePair<string, string> asset in CriticalMixerAssets)
        {
            if (!File.Exists(asset.Key))
            {
                failures.Add($"Critical MixerRoom asset is missing: {asset.Key}");
                continue;
            }

            string actualGuid = AssetDatabase.AssetPathToGUID(asset.Key);
            if (!string.Equals(actualGuid, asset.Value, StringComparison.Ordinal))
            {
                failures.Add(
                    $"Critical MixerRoom asset GUID mismatch for '{asset.Key}': " +
                    $"actual={actualGuid}, expected={asset.Value}.");
            }
        }

        RequireText("Assets/Scenes/5_MixerRoom.unity", "guid: 94d73b4974d94284a9259a144c0a0b80", failures);
        RequireText("Assets/Scenes/5_MixerRoom.unity", "guid: d38c6eb5eac83874ca4e1361ff5ff63c", failures);
        RequireText("Assets/Prefabs/Floor.prefab", "guid: 999517f045d65924db469ec2a4a5c833", failures);
    }

    static void ValidateProjectOwnedReferences(List<string> failures)
    {
        string[] roots =
        {
            "Assets/Scripts",
            "Assets/Editor",
            "Assets/Scenes",
        };

        IEnumerable<string> files = roots
            .Where(Directory.Exists)
            .SelectMany(root => Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase));

        foreach (string path in files)
        {
            if (path.Replace('\\', '/').EndsWith(
                    "Assets/Editor/SceneDependencyValidationHarness.cs",
                    StringComparison.Ordinal))
            {
                continue;
            }

            string contents = File.ReadAllText(path);
            foreach (string token in LegacySceneTokens)
            {
                if (contents.IndexOf(token, StringComparison.Ordinal) >= 0)
                    failures.Add($"Legacy scene token '{token}' remains in '{path}'.");
            }
        }
    }

    static void RequireText(string path, string requiredText, List<string> failures)
    {
        if (!File.Exists(path))
        {
            failures.Add($"Required file is missing: {path}");
            return;
        }

        string contents = File.ReadAllText(path);
        if (contents.IndexOf(requiredText, StringComparison.Ordinal) < 0)
            failures.Add($"'{path}' does not contain required text '{requiredText}'.");
    }
}
