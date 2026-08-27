using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Marks already-placed PPE_A_InnerGlove_L/R as nitrile inner gloves and wires
/// them to InnerGlove (before hazmat) and InnerGloveSuit (after). Display and
/// hand Transforms are not moved.
/// </summary>
public static class PPENitrileInnerGloveWearSetup
{
    const string ScenePath = "Assets/Scenes/3_PPE_Room_Train_Test_mask.unity";
    const string DisplayParentName = "PPE";

    sealed class ItemSpec
    {
        public string DisplayName;
        public PPEItemType ItemType;
        public string DisplayLabel;
        public string SuitHandModelName;
        public string BareHandModelName;
        public PPEItemType OuterGloveType;
    }

    static readonly ItemSpec[] Specs =
    {
        new ItemSpec
        {
            DisplayName = "PPE_A_InnerGlove_L",
            ItemType = PPEItemType.NitrileInnerGloveLeft,
            DisplayLabel = "니트릴 내부 장갑(좌)",
            SuitHandModelName = "PPE_A_Hand_InnerGloveSuit_L",
            BareHandModelName = "PPE_A_Hand_InnerGlove_L",
            OuterGloveType = PPEItemType.RubberGloveLeft
        },
        new ItemSpec
        {
            DisplayName = "PPE_A_InnerGlove_R",
            ItemType = PPEItemType.NitrileInnerGloveRight,
            DisplayLabel = "니트릴 내부 장갑(우)",
            SuitHandModelName = "PPE_A_Hand_InnerGloveSuit_R",
            BareHandModelName = "PPE_A_Hand_InnerGlove_R",
            OuterGloveType = PPEItemType.RubberGloveRight
        }
    };

    [MenuItem("Tools/PPE/Wire Nitrile Inner Glove Wear")]
    public static void ConfigureFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("Play Mode를 종료한 뒤 니트릴 내부 장갑 착용 연결을 실행해야 합니다.");
            return;
        }

        if (!Application.isBatchMode &&
            !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("Nitrile inner glove wear wiring was cancelled.");
            return;
        }

        ConfigureInternal(true);
    }

    [MenuItem("Tools/PPE/Validate Nitrile Inner Glove Wear")]
    public static void ValidateFromMenu()
    {
        ValidateScene(RequireTargetScene(), true);
    }

    static void ConfigureInternal(bool save)
    {
        Scene scene = RequireTargetScene();
        PPEEquipmentVisualController visual = FindVisualController(scene);
        if (visual == null)
            throw new InvalidOperationException("PPEEquipmentVisualController is missing.");

        Undo.SetCurrentGroupName("Wire nitrile inner glove wear");
        int undoGroup = Undo.GetCurrentGroup();
        var capturedPoses = new List<(Transform target, Vector3 pos, Quaternion rot, Vector3 scale)>();

        try
        {
            foreach (ItemSpec spec in Specs)
            {
                GameObject display = RequireChildOfNamedParent(scene, DisplayParentName, spec.DisplayName);
                GameObject suitHand = RequireNamed(scene, spec.SuitHandModelName);
                GameObject bareHand = RequireNamed(scene, spec.BareHandModelName);
                CapturePose(display.transform, capturedPoses);
                CapturePose(suitHand.transform, capturedPoses);
                CapturePose(bareHand.transform, capturedPoses);

                if (suitHand.name != spec.SuitHandModelName)
                {
                    Undo.RecordObject(suitHand, "Trim InnerGloveSuit name");
                    suitHand.name = spec.SuitHandModelName;
                    EditorUtility.SetDirty(suitHand);
                }

                WireDisplayItem(display, spec);
                AssignHandSlot(visual, display.GetComponent<PPEItemPresentationBinding>(), suitHand);
                AssignHandSwaps(visual, spec, suitHand, bareHand);
                RepairHandColorTargets(suitHand);
                RepairHandColorTargets(bareHand);
            }

            AssertPosesUnchanged(capturedPoses);
            ValidateScene(scene, false);
            EditorSceneManager.MarkSceneDirty(scene);
            if (save && !EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException($"'{ScenePath}' 저장에 실패했습니다.");

            Debug.Log(
                "Nitrile inner glove wear wiring complete: PPE_A_InnerGlove_L/R → InnerGlove before hazmat, InnerGloveSuit after. " +
                "Display and hand Transforms were not moved.");
        }
        finally
        {
            Undo.CollapseUndoOperations(undoGroup);
        }
    }

    static void WireDisplayItem(GameObject display, ItemSpec spec)
    {
        PPEItemIdentity identity = display.GetComponent<PPEItemIdentity>();
        if (identity == null)
            throw new InvalidOperationException($"'{display.name}' is missing PPEItemIdentity.");

        if (identity.ItemType != spec.ItemType)
        {
            Undo.RecordObject(identity, "Set nitrile inner glove type");
            identity.ConfigureItemTypeForEditor(spec.ItemType);
            EditorUtility.SetDirty(identity);
        }

        PPEActionPanelController panel = display.GetComponent<PPEActionPanelController>();
        if (panel == null)
            throw new InvalidOperationException($"'{display.name}' is missing PPEActionPanelController.");

        SerializedObject panelSo = new SerializedObject(panel);
        panelSo.Update();
        SerializedProperty nameProperty = panelSo.FindProperty("itemDisplayName");
        if (nameProperty != null && nameProperty.stringValue != spec.DisplayLabel)
            nameProperty.stringValue = spec.DisplayLabel;
        panelSo.ApplyModifiedProperties();
        EditorUtility.SetDirty(panel);
    }

    static void RepairHandColorTargets(GameObject hand)
    {
        PPEGloveVisualAppearance appearance = hand.GetComponent<PPEGloveVisualAppearance>();
        if (appearance == null)
            return;

        Renderer[] current = appearance.TargetRenderers;
        if (current != null && current.Length > 0 && current[0] != null)
            return;

        Renderer gloveRenderer = null;
        foreach (Renderer renderer in hand.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer.gameObject.name != "Chemical_Glove")
                continue;
            if (gloveRenderer != null)
                return;
            gloveRenderer = renderer;
        }

        if (gloveRenderer == null)
            return;

        Undo.RecordObject(appearance, "Assign inner glove color renderer");
        appearance.ConfigureForEditor(
            new[] { gloveRenderer },
            appearance.Color,
            appearance.Brightness);
        EditorUtility.SetDirty(appearance);
    }

    static void AssignHandSlot(
        PPEEquipmentVisualController visual,
        PPEItemPresentationBinding binding,
        GameObject hand)
    {
        if (binding == null)
            throw new InvalidOperationException("Inner glove presentation binding is missing.");

        SerializedObject so = new SerializedObject(visual);
        SerializedProperty slots = so.FindProperty("slots");
        int slotIndex = FindSlotIndex(slots, binding, hand);
        if (slotIndex < 0)
        {
            slotIndex = slots.arraySize;
            slots.arraySize++;
        }

        SerializedProperty slot = slots.GetArrayElementAtIndex(slotIndex);
        slot.FindPropertyRelative("itemBinding").objectReferenceValue = binding;
        slot.FindPropertyRelative("equippedChild").objectReferenceValue = hand;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(visual);
    }

    static void AssignHandSwaps(
        PPEEquipmentVisualController visual,
        ItemSpec spec,
        GameObject suitHand,
        GameObject bareHand)
    {
        SerializedObject so = new SerializedObject(visual);
        SerializedProperty swaps = so.FindProperty("handModelSwaps");
        if (swaps == null)
            throw new InvalidOperationException("handModelSwaps was not found.");

        int outerIndex = FindSwapIndex(swaps, spec.OuterGloveType);
        if (outerIndex < 0)
            throw new InvalidOperationException($"No hand swap exists for {spec.OuterGloveType}.");

        SerializedProperty outer = swaps.GetArrayElementAtIndex(outerIndex);
        SetObjectReference(outer, "innerGloveHand", suitHand);
        SetObjectReference(outer, "bareInnerGloveHand", bareHand);

        int tapeIndex = FindTapeSwapIndex(swaps, spec.OuterGloveType);
        if (tapeIndex >= 0)
        {
            SetObjectReference(swaps.GetArrayElementAtIndex(tapeIndex), "innerGloveHand", suitHand);
            SetObjectReference(swaps.GetArrayElementAtIndex(tapeIndex), "bareInnerGloveHand", bareHand);
        }

        int nitrileIndex = FindSwapIndex(swaps, spec.ItemType);
        if (nitrileIndex < 0)
        {
            nitrileIndex = swaps.arraySize;
            swaps.arraySize++;
            SerializedProperty created = swaps.GetArrayElementAtIndex(nitrileIndex);
            CopySwapTemplate(outer, created);
            created.FindPropertyRelative("itemType").enumValueIndex = (int)spec.ItemType;
            created.FindPropertyRelative("tapeRequiredItemType").enumValueIndex = 0;
            created.FindPropertyRelative("tapedHand").objectReferenceValue = null;
        }

        SetObjectReference(swaps.GetArrayElementAtIndex(nitrileIndex), "innerGloveHand", suitHand);
        SetObjectReference(swaps.GetArrayElementAtIndex(nitrileIndex), "bareInnerGloveHand", bareHand);
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(visual);
    }

    static void CopySwapTemplate(SerializedProperty source, SerializedProperty destination)
    {
        SetObjectReference(destination, "bareHand", GetObjectReference(source, "bareHand"));
        SetObjectReference(destination, "bareSuitHand", GetObjectReference(source, "bareSuitHand"));
        SetObjectReference(destination, "gloveHand", GetObjectReference(source, "gloveHand"));
        SetObjectReference(destination, "tapedHand", GetObjectReference(source, "tapedHand"));
        SetObjectReference(destination, "innerGloveHand", GetObjectReference(source, "innerGloveHand"));
        SetObjectReference(destination, "bareInnerGloveHand", GetObjectReference(source, "bareInnerGloveHand"));
    }

    static int FindSwapIndex(SerializedProperty swaps, PPEItemType itemType)
    {
        for (int i = 0; i < swaps.arraySize; i++)
        {
            SerializedProperty itemTypeProperty =
                swaps.GetArrayElementAtIndex(i).FindPropertyRelative("itemType");
            if (itemTypeProperty != null && itemTypeProperty.enumValueIndex == (int)itemType)
                return i;
        }

        return -1;
    }

    static int FindTapeSwapIndex(SerializedProperty swaps, PPEItemType requiredItemType)
    {
        for (int i = 0; i < swaps.arraySize; i++)
        {
            SerializedProperty element = swaps.GetArrayElementAtIndex(i);
            SerializedProperty itemType = element.FindPropertyRelative("itemType");
            SerializedProperty required = element.FindPropertyRelative("tapeRequiredItemType");
            if (itemType != null &&
                itemType.enumValueIndex == (int)PPEItemType.PackingTape &&
                required != null &&
                required.enumValueIndex == (int)requiredItemType)
            {
                return i;
            }
        }

        return -1;
    }

    static int FindSlotIndex(
        SerializedProperty slots,
        PPEItemPresentationBinding binding,
        GameObject equippedChild)
    {
        int equippedMatch = -1;
        for (int i = 0; i < slots.arraySize; i++)
        {
            SerializedProperty element = slots.GetArrayElementAtIndex(i);
            SerializedProperty itemBinding = element.FindPropertyRelative("itemBinding");
            SerializedProperty child = element.FindPropertyRelative("equippedChild");
            if (itemBinding != null && itemBinding.objectReferenceValue == binding)
                return i;
            if (child != null && child.objectReferenceValue == equippedChild)
                equippedMatch = i;
        }

        return equippedMatch;
    }

    static void ValidateScene(Scene scene, bool logSuccess)
    {
        var failures = new List<string>();
        PPEEquipmentVisualController visual = FindVisualController(scene);
        if (visual == null)
            failures.Add("PPEEquipmentVisualController is missing.");

        foreach (ItemSpec spec in Specs)
        {
            GameObject display;
            GameObject suitHand;
            GameObject bareHand;
            try
            {
                display = RequireChildOfNamedParent(scene, DisplayParentName, spec.DisplayName);
                suitHand = RequireNamed(scene, spec.SuitHandModelName);
                bareHand = RequireNamed(scene, spec.BareHandModelName);
            }
            catch (InvalidOperationException ex)
            {
                failures.Add(ex.Message);
                continue;
            }

            PPEItemIdentity identity = display.GetComponent<PPEItemIdentity>();
            PPEActionPanelController panel = display.GetComponent<PPEActionPanelController>();
            if (identity == null || identity.ItemType != spec.ItemType)
                failures.Add($"'{spec.DisplayName}' is not {spec.ItemType}.");
            if (panel == null || panel.ItemDisplayName != spec.DisplayLabel)
                failures.Add($"'{spec.DisplayName}' display name must be '{spec.DisplayLabel}'.");

            if (visual == null)
                continue;

            bool slotFound = false;
            foreach (PPEEquipmentVisualSlot slot in visual.Slots ?? Array.Empty<PPEEquipmentVisualSlot>())
            {
                if (slot != null &&
                    slot.ItemBinding == display.GetComponent<PPEItemPresentationBinding>() &&
                    slot.EquippedChild == suitHand)
                {
                    slotFound = true;
                    break;
                }
            }

            if (!slotFound)
                failures.Add($"Slot for '{spec.DisplayName}' → '{spec.SuitHandModelName}' is missing.");

            SerializedObject so = new SerializedObject(visual);
            SerializedProperty swaps = so.FindProperty("handModelSwaps");
            int nitrileIndex = FindSwapIndex(swaps, spec.ItemType);
            if (nitrileIndex < 0)
            {
                failures.Add($"Hand swap for {spec.ItemType} is missing.");
            }
            else
            {
                SerializedProperty nitrileSwap = swaps.GetArrayElementAtIndex(nitrileIndex);
                if (GetObjectReference(nitrileSwap, "innerGloveHand") != suitHand)
                    failures.Add($"'{spec.SuitHandModelName}' is not the innerGloveHand for {spec.ItemType}.");
                if (GetObjectReference(nitrileSwap, "bareInnerGloveHand") != bareHand)
                    failures.Add($"'{spec.BareHandModelName}' is not the bareInnerGloveHand for {spec.ItemType}.");

                SerializedProperty tapeRequired = nitrileSwap.FindPropertyRelative("tapeRequiredItemType");
                if (tapeRequired != null && tapeRequired.enumValueIndex != 0)
                    failures.Add($"{spec.ItemType} must not be a tape target.");
            }

            int outerIndex = FindSwapIndex(swaps, spec.OuterGloveType);
            if (outerIndex >= 0)
            {
                SerializedProperty outerSwap = swaps.GetArrayElementAtIndex(outerIndex);
                if (GetObjectReference(outerSwap, "innerGloveHand") != suitHand)
                    failures.Add($"Outer {spec.OuterGloveType} must hide '{spec.SuitHandModelName}' when worn.");
                if (GetObjectReference(outerSwap, "bareInnerGloveHand") != bareHand)
                    failures.Add($"Outer {spec.OuterGloveType} must hide '{spec.BareHandModelName}' when worn.");
            }
        }

        if (failures.Count > 0)
        {
            string message = "Nitrile inner glove wear validation failed:\n- " +
                string.Join("\n- ", failures);
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        if (logSuccess)
        {
            Debug.Log(
                "Nitrile inner glove wear validation passed: InnerGlove is used before hazmat, InnerGloveSuit after.");
        }
    }

    static GameObject RequireChildOfNamedParent(Scene scene, string parentName, string childName)
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

        if (match == null)
            throw new InvalidOperationException($"'{parentName}/{childName}' was not found.");
        return match;
    }

    static GameObject RequireNamed(Scene scene, string objectName)
    {
        GameObject match = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name != objectName && candidate.name.Trim() != objectName)
                    continue;
                if (match != null)
                    throw new InvalidOperationException($"'{objectName}' must be unique.");
                match = candidate.gameObject;
            }
        }

        if (match == null)
            throw new InvalidOperationException($"'{objectName}' was not found.");
        return match;
    }

    static PPEEquipmentVisualController FindVisualController(Scene scene)
    {
        PPEEquipmentVisualController found = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (PPEEquipmentVisualController candidate in
                     root.GetComponentsInChildren<PPEEquipmentVisualController>(true))
            {
                if (found != null)
                    throw new InvalidOperationException("Expected exactly one PPEEquipmentVisualController.");
                found = candidate;
            }
        }

        return found;
    }

    static Scene RequireTargetScene()
    {
        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
            scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
        {
            throw new InvalidOperationException(
                $"Open '{ScenePath}' before wiring nitrile inner gloves. Current scene: '{scene.path}'.");
        }

        return scene;
    }

    static void SetObjectReference(SerializedProperty owner, string fieldName, UnityEngine.Object value)
    {
        SerializedProperty field = owner.FindPropertyRelative(fieldName);
        if (field == null)
            throw new InvalidOperationException($"Missing serialized field '{fieldName}'.");
        field.objectReferenceValue = value;
    }

    static UnityEngine.Object GetObjectReference(SerializedProperty owner, string fieldName)
    {
        SerializedProperty field = owner.FindPropertyRelative(fieldName);
        return field != null ? field.objectReferenceValue : null;
    }

    static void CapturePose(
        Transform target,
        List<(Transform target, Vector3 pos, Quaternion rot, Vector3 scale)> poses)
    {
        poses.Add((target, target.localPosition, target.localRotation, target.localScale));
    }

    static void AssertPosesUnchanged(
        List<(Transform target, Vector3 pos, Quaternion rot, Vector3 scale)> poses)
    {
        foreach ((Transform target, Vector3 pos, Quaternion rot, Vector3 scale) in poses)
        {
            if (target.localPosition != pos || target.localRotation != rot || target.localScale != scale)
            {
                throw new InvalidOperationException(
                    $"'{GetPath(target)}' Transform changed during nitrile inner glove wear wiring.");
            }
        }
    }

    static string GetPath(Transform transform)
    {
        string path = transform.name;
        while (transform.parent != null)
        {
            transform = transform.parent;
            path = transform.name + "/" + path;
        }

        return path;
    }
}
