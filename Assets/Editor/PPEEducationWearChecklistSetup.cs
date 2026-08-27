using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Wires the authored Window Canvas / CheckList rows to education-mode wear
/// checks. Unity generates component FileIDs; this menu does not invent them.
/// </summary>
public static class PPEEducationWearChecklistSetup
{
    const string ScenePath = "Assets/Scenes/3_PPE_Room_Train_Test_mask.unity";
    const string CanvasName = "Window Canvas";
    const string CheckListName = "CheckList";
    const string ConfinedName = "Confined";
    const string LeakName = "Leak";
    const int ExpectedConfinedCount = 7;
    const int ExpectedLeakCount = 7;

    [MenuItem("Tools/PPE/Wire Education Wear Checklist")]
    public static void ConfigureFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("Play Mode를 종료한 뒤 교육 체크리스트 연결을 실행해야 합니다.");
            return;
        }

        if (!Application.isBatchMode &&
            !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("Education wear checklist wiring was cancelled.");
            return;
        }

        ConfigureInternal(true);
    }

    [MenuItem("Tools/PPE/Validate Education Wear Checklist")]
    public static void ValidateFromMenu()
    {
        ValidateScene(RequireTargetScene(), true);
    }

    static void ConfigureInternal(bool save)
    {
        Scene scene = RequireTargetScene();
        Undo.SetCurrentGroupName("Wire education wear checklist");
        int undoGroup = Undo.GetCurrentGroup();

        try
        {
            GameObject checkList = RequireChildOfNamedParent(scene, CanvasName, CheckListName);
            CanvasGroup panelGroup = checkList.GetComponent<CanvasGroup>();
            if (panelGroup == null)
                throw new InvalidOperationException("CheckList requires an authored CanvasGroup.");

            GameObject confinedRoot = RequireChildOfNamedParent(scene, CheckListName, ConfinedName);
            GameObject leakRoot = RequireChildOfNamedParent(scene, CheckListName, LeakName);

            PPEEducationWearChecklist checklist = checkList.GetComponent<PPEEducationWearChecklist>();
            if (checklist == null)
                checklist = Undo.AddComponent<PPEEducationWearChecklist>(checkList);

            List<(GameObject checkMark, PPEItemType[] types)> confinedEntries =
                BuildEntries(confinedRoot.transform);
            if (confinedEntries.Count != ExpectedConfinedCount)
            {
                throw new InvalidOperationException(
                    $"Confined checklist must have {ExpectedConfinedCount} List rows with Check marks. Found {confinedEntries.Count}.");
            }

            List<(GameObject checkMark, PPEItemType[] types)> leakEntries =
                BuildEntries(leakRoot.transform);
            if (leakEntries.Count != ExpectedLeakCount)
            {
                throw new InvalidOperationException(
                    $"Leak checklist must have {ExpectedLeakCount} List rows with Check marks. Found {leakEntries.Count}.");
            }

            SerializedObject serialized = new(checklist);
            serialized.Update();
            serialized.FindProperty("director").objectReferenceValue =
                RequireSingle<PPEVoiceFlowDirector>(scene);
            serialized.FindProperty("tablet").objectReferenceValue =
                RequireSingle<PPETabletChecklistController>(scene);
            serialized.FindProperty("equipmentVisual").objectReferenceValue =
                RequireSingle<PPEEquipmentVisualController>(scene);
            serialized.FindProperty("hazmatEquip").objectReferenceValue =
                RequireSingle<PPEHazmatEquipController>(scene);
            serialized.FindProperty("panelGroup").objectReferenceValue = panelGroup;
            serialized.FindProperty("confinedRoot").objectReferenceValue = confinedRoot;
            serialized.FindProperty("leakRoot").objectReferenceValue = leakRoot;
            WriteEntries(serialized, "confinedEntries", confinedEntries);
            WriteEntries(serialized, "leakEntries", leakEntries);
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(checklist);

            ValidateScene(scene, false);
            EditorSceneManager.MarkSceneDirty(scene);
            if (save && !EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException($"'{ScenePath}' 저장에 실패했습니다.");

            Debug.Log(
                "Education wear checklist wired: Confined and Leak List_* names map to wear types. 안전화 is not used.");
        }
        finally
        {
            Undo.CollapseUndoOperations(undoGroup);
        }
    }

    static void ValidateScene(Scene scene, bool logSuccess)
    {
        List<string> failures = new();
        GameObject checkList = FindChildOfNamedParent(scene, CanvasName, CheckListName);
        if (checkList == null)
        {
            failures.Add($"{CanvasName}/{CheckListName} is missing.");
        }
        else
        {
            PPEEducationWearChecklist checklist = checkList.GetComponent<PPEEducationWearChecklist>();
            if (checklist == null)
            {
                failures.Add("CheckList is missing PPEEducationWearChecklist. Run Tools > PPE > Wire Education Wear Checklist.");
            }
            else
            {
                SerializedObject serialized = new(checklist);
                serialized.Update();
                ValidateObject(serialized, "director", failures);
                ValidateObject(serialized, "tablet", failures);
                ValidateObject(serialized, "equipmentVisual", failures);
                ValidateObject(serialized, "hazmatEquip", failures);
                ValidateObject(serialized, "panelGroup", failures);
                ValidateObject(serialized, "confinedRoot", failures);
                ValidateObject(serialized, "leakRoot", failures);

                SerializedProperty confined = serialized.FindProperty("confinedEntries");
                if (confined == null || confined.arraySize != ExpectedConfinedCount)
                {
                    failures.Add($"confinedEntries must have {ExpectedConfinedCount} rows.");
                }
                else
                {
                    for (int index = 0; index < confined.arraySize; index++)
                        ValidateEntry(confined.GetArrayElementAtIndex(index), $"confinedEntries[{index}]", failures);
                }

                SerializedProperty leak = serialized.FindProperty("leakEntries");
                if (leak == null || leak.arraySize != ExpectedLeakCount)
                {
                    failures.Add($"leakEntries must have {ExpectedLeakCount} rows.");
                }
                else
                {
                    for (int index = 0; index < leak.arraySize; index++)
                        ValidateEntry(leak.GetArrayElementAtIndex(index), $"leakEntries[{index}]", failures);
                }
            }
        }

        if (failures.Count > 0)
            throw new InvalidOperationException(string.Join("\n", failures));

        if (logSuccess)
        {
            Debug.Log(
                "Education wear checklist validation passed: Confined and Leak rows are wired by List_* names.");
        }
    }

    static void ValidateObject(SerializedObject serialized, string propertyName, List<string> failures)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null || property.objectReferenceValue == null)
            failures.Add($"{propertyName} is not assigned.");
    }

    static void ValidateEntry(SerializedProperty entry, string label, List<string> failures)
    {
        SerializedProperty checkMark = entry.FindPropertyRelative("checkMark");
        SerializedProperty itemTypes = entry.FindPropertyRelative("itemTypes");
        if (checkMark == null || checkMark.objectReferenceValue == null)
            failures.Add($"{label}.checkMark is not assigned.");
        if (itemTypes == null || itemTypes.arraySize == 0)
            failures.Add($"{label}.itemTypes is empty.");
    }

    static void WriteEntries(
        SerializedObject serialized,
        string propertyName,
        List<(GameObject checkMark, PPEItemType[] types)> entries)
    {
        SerializedProperty array = serialized.FindProperty(propertyName);
        array.arraySize = entries.Count;
        for (int index = 0; index < entries.Count; index++)
        {
            SerializedProperty element = array.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("checkMark").objectReferenceValue = entries[index].checkMark;
            SerializedProperty types = element.FindPropertyRelative("itemTypes");
            types.arraySize = entries[index].types.Length;
            for (int typeIndex = 0; typeIndex < entries[index].types.Length; typeIndex++)
                types.GetArrayElementAtIndex(typeIndex).intValue = (int)entries[index].types[typeIndex];
        }
    }

    static List<(GameObject checkMark, PPEItemType[] types)> BuildEntries(Transform groupRoot)
    {
        List<(GameObject checkMark, PPEItemType[] types)> entries = new();
        for (int index = 0; index < groupRoot.childCount; index++)
        {
            Transform child = groupRoot.GetChild(index);
            if (child == null || !child.name.StartsWith("List_", StringComparison.Ordinal))
                continue;

            GameObject checkMark = RequireCheckMark(child);
            string label = ReadListLabel(child);
            PPEItemType[] types = MapRowToItemTypes(child.name, label);
            if (types.Length == 0)
            {
                throw new InvalidOperationException(
                    $"'{groupRoot.name}/{child.name}' has no PPEItemType mapping. Use a List_* suffix such as Suit, Boots, Backplate, Mask, Helmet, InnerGlove, Glove, Goggle, Face, or Scuba.");
            }

            entries.Add((checkMark, types));
        }

        return entries;
    }

    static GameObject RequireCheckMark(Transform listRoot)
    {
        Transform checkBox = listRoot.Find("CheckBox");
        Transform check = checkBox != null ? checkBox.Find("Check") : null;
        if (check == null)
        {
            throw new InvalidOperationException(
                $"'{listRoot.name}' requires CheckBox/Check. Do not create a new Check object from this menu.");
        }

        return check.gameObject;
    }

    static string ReadListLabel(Transform listRoot)
    {
        TMP_Text[] labels = listRoot.GetComponentsInChildren<TMP_Text>(true);
        for (int index = 0; index < labels.Length; index++)
        {
            TMP_Text label = labels[index];
            if (label != null && label.transform.parent == listRoot)
                return label.text != null ? label.text.Trim() : string.Empty;
        }

        return string.Empty;
    }

    static PPEItemType[] MapRowToItemTypes(string objectName, string label)
    {
        string suffix = ReadListSuffix(objectName);
        PPEItemType[] fromName = MapSuffixToItemTypes(suffix);
        if (fromName.Length > 0)
            return fromName;

        return MapLabelToItemTypes(label);
    }

    static string ReadListSuffix(string objectName)
    {
        if (string.IsNullOrEmpty(objectName) || !objectName.StartsWith("List_", StringComparison.Ordinal))
            return string.Empty;

        int separator = objectName.LastIndexOf('_');
        if (separator <= 4 || separator >= objectName.Length - 1)
            return string.Empty;

        return objectName.Substring(separator + 1);
    }

    static PPEItemType[] MapSuffixToItemTypes(string suffix)
    {
        if (string.IsNullOrEmpty(suffix))
            return Array.Empty<PPEItemType>();

        if (suffix.Equals("Suit", StringComparison.OrdinalIgnoreCase))
            return new[] { PPEItemType.HazmatSuit };
        if (suffix.Equals("Boots", StringComparison.OrdinalIgnoreCase))
            return new[] { PPEItemType.RubberBootLeft, PPEItemType.RubberBootRight };
        if (suffix.Equals("Backplate", StringComparison.OrdinalIgnoreCase) ||
            suffix.Equals("Harness", StringComparison.OrdinalIgnoreCase))
            return new[] { PPEItemType.TacticalHarness };
        if (suffix.Equals("Mask", StringComparison.OrdinalIgnoreCase))
            return new[] { PPEItemType.GasMask };
        if (suffix.Equals("Helmet", StringComparison.OrdinalIgnoreCase))
            return new[] { PPEItemType.ConstructionHelmet };
        if (suffix.Equals("InnerGlove", StringComparison.OrdinalIgnoreCase))
            return new[] { PPEItemType.NitrileInnerGloveLeft, PPEItemType.NitrileInnerGloveRight };
        if (suffix.Equals("Glove", StringComparison.OrdinalIgnoreCase))
            return new[] { PPEItemType.RubberGloveLeft, PPEItemType.RubberGloveRight };
        if (suffix.Equals("Goggle", StringComparison.OrdinalIgnoreCase) ||
            suffix.Equals("Goggles", StringComparison.OrdinalIgnoreCase))
            return new[] { PPEItemType.SafetyGoggles };
        if (suffix.Equals("Face", StringComparison.OrdinalIgnoreCase) ||
            suffix.Equals("FaceShield", StringComparison.OrdinalIgnoreCase))
            return new[] { PPEItemType.FaceShield };
        if (suffix.Equals("Scuba", StringComparison.OrdinalIgnoreCase) ||
            suffix.Equals("SCBA", StringComparison.OrdinalIgnoreCase))
            return new[] { PPEItemType.ScubaGear };

        return Array.Empty<PPEItemType>();
    }

    static PPEItemType[] MapLabelToItemTypes(string label)
    {
        if (string.IsNullOrEmpty(label))
            return Array.Empty<PPEItemType>();

        if (label.Contains("방호복"))
            return new[] { PPEItemType.HazmatSuit };
        if (label.Contains("장화"))
            return new[] { PPEItemType.RubberBootLeft, PPEItemType.RubberBootRight };
        if (label.Contains("니트릴") || label.Contains("내부장갑") || label.Contains("내부 장갑"))
            return new[] { PPEItemType.NitrileInnerGloveLeft, PPEItemType.NitrileInnerGloveRight };
        if (label.Contains("외부") || label.Contains("네오프렌"))
            return new[] { PPEItemType.RubberGloveLeft, PPEItemType.RubberGloveRight };
        if (label.Contains("송기"))
            return new[] { PPEItemType.GasMask };
        if (label.Contains("안전모"))
            return new[] { PPEItemType.ConstructionHelmet };
        if (label.Contains("안전대") || label.Contains("백플레이트") || label.Contains("Backplate"))
            return new[] { PPEItemType.TacticalHarness };
        if (label.Contains("보안경") || label.Contains("고글"))
            return new[] { PPEItemType.SafetyGoggles };
        if (label.Contains("안면") || label.Contains("페이스"))
            return new[] { PPEItemType.FaceShield };
        if (label.Contains("호흡"))
            return new[] { PPEItemType.ScubaGear };
        if (label.Contains("장갑"))
            return new[] { PPEItemType.RubberGloveLeft, PPEItemType.RubberGloveRight };

        return Array.Empty<PPEItemType>();
    }

    static T RequireSingle<T>(Scene scene) where T : Component
    {
        T found = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (T candidate in root.GetComponentsInChildren<T>(true))
            {
                if (found != null)
                    throw new InvalidOperationException($"Expected exactly one {typeof(T).Name}.");
                found = candidate;
            }
        }

        if (found == null)
            throw new InvalidOperationException($"{typeof(T).Name} was not found.");
        return found;
    }

    static GameObject RequireChildOfNamedParent(Scene scene, string parentName, string childName)
    {
        GameObject match = FindChildOfNamedParent(scene, parentName, childName);
        if (match == null)
            throw new InvalidOperationException($"'{parentName}/{childName}' was not found.");
        return match;
    }

    static GameObject FindChildOfNamedParent(Scene scene, string parentName, string childName)
    {
        GameObject match = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name != childName ||
                    candidate.parent == null ||
                    candidate.parent.name != parentName)
                {
                    continue;
                }

                if (match != null)
                    throw new InvalidOperationException($"'{parentName}/{childName}' must be unique.");
                match = candidate.gameObject;
            }
        }

        return match;
    }

    static Scene RequireTargetScene()
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
            scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
        {
            throw new InvalidOperationException(
                $"Open '{ScenePath}' before wiring the education wear checklist. Current scene: '{scene.path}'.");
        }

        return scene;
    }
}
