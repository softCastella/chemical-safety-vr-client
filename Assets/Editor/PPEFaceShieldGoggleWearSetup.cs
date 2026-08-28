using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Wires already-placed FaceShield and Goggle display items to the same grab →
/// body+trigger → full-suit child path as the other PPE. Display and equipped
/// Transforms are not moved.
/// </summary>
public static class PPEFaceShieldGoggleWearSetup
{
    const string ScenePath = "Assets/Scenes/3_PPE_Room_Train_Test_mask.unity";
    const string TemplateName = "PPE_A_Boots_L_Clean";
    const string MarkerName = "XR Item Marker_small";
    const string PoseName = "Action Panel Pose";
    const string PlaceholderName = "equipped_placeholder";
    const string EquippedFaceShieldName = "PPE_A_FaceShield";
    const string EquippedGoggleName = "PPE_A_Goggle";

    static readonly HashSet<string> IdentityProperties = new()
    {
        "m_ObjectHideFlags",
        "m_CorrespondingSourceObject",
        "m_PrefabInstance",
        "m_PrefabAsset",
        "m_GameObject",
        "m_Enabled",
        "m_EditorHideFlags",
        "m_Script",
        "m_Name",
        "m_EditorClassIdentifier"
    };

    sealed class ItemSpec
    {
        public string DisplayName;
        public PPEItemType ItemType;
        public PPEItemCondition Condition;
        public string DisplayLabel;
        public string EquippedChildName;
        public bool AddVisualSlot;
    }

    static readonly ItemSpec[] Specs =
    {
        new ItemSpec
        {
            DisplayName = "PPE_A_FaceShield_Clean",
            ItemType = PPEItemType.FaceShield,
            Condition = PPEItemCondition.Clean,
            DisplayLabel = "안면보호대",
            EquippedChildName = EquippedFaceShieldName,
            AddVisualSlot = true
        },
        new ItemSpec
        {
            DisplayName = "PPE_A_FaceShield_Contam",
            ItemType = PPEItemType.FaceShield,
            Condition = PPEItemCondition.Contaminated,
            DisplayLabel = "안면보호대",
            EquippedChildName = EquippedFaceShieldName,
            AddVisualSlot = false
        },
        new ItemSpec
        {
            DisplayName = "PPE_A_Goggle_Clean",
            ItemType = PPEItemType.SafetyGoggles,
            Condition = PPEItemCondition.Clean,
            DisplayLabel = "화학보안경",
            EquippedChildName = EquippedGoggleName,
            AddVisualSlot = true
        },
        new ItemSpec
        {
            DisplayName = "PPE_A_Goggle_Contam",
            ItemType = PPEItemType.SafetyGoggles,
            Condition = PPEItemCondition.Contaminated,
            DisplayLabel = "화학보안경",
            EquippedChildName = EquippedGoggleName,
            AddVisualSlot = false
        }
    };

    [MenuItem("Tools/PPE/Wire FaceShield And Goggle Wear")]
    public static void ConfigureFromMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("Play Mode를 종료한 뒤 안면보호대·화학보안경 착용 연결을 실행해야 합니다.");
            return;
        }

        if (!Application.isBatchMode &&
            !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            Debug.LogWarning("FaceShield/Goggle wear wiring was cancelled.");
            return;
        }

        ConfigureInternal(true);
    }

    public static void ConfigureBatch()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Play Mode를 종료한 뒤 실행해야 합니다.");

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
            throw new InvalidOperationException($"Failed to open '{ScenePath}'.");

        ConfigureInternal(true);
    }

    [MenuItem("Tools/PPE/Validate FaceShield And Goggle Wear")]
    public static void ValidateFromMenu()
    {
        Scene scene = RequireTargetScene();
        ValidateScene(scene, true);
    }

    public static void ValidateBatch()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ValidateScene(scene, true);
    }

    static void ConfigureInternal(bool save)
    {
        Scene scene = RequireTargetScene();
        GameObject template = RequireUnique(scene, TemplateName);
        PPEActionPanelController templatePanel = template.GetComponent<PPEActionPanelController>();
        XRGrabInteractable templateGrab = template.GetComponent<XRGrabInteractable>();
        Rigidbody templateBody = template.GetComponent<Rigidbody>();
        if (templatePanel == null || templateGrab == null || templateBody == null)
        {
            throw new InvalidOperationException(
                $"'{TemplateName}' must already have grab, rigidbody, and action panel.");
        }

        PPEEquipmentVisualController visual = FindVisualController(scene);
        if (visual == null || visual.EquipmentRoot == null)
            throw new InvalidOperationException("PPEEquipmentVisualController.equipmentRoot is missing.");

        Undo.SetCurrentGroupName("Wire FaceShield and Goggle wear");
        int undoGroup = Undo.GetCurrentGroup();
        var capturedPoses = new List<(Transform target, Vector3 pos, Quaternion rot, Vector3 scale)>();

        try
        {
            foreach (ItemSpec spec in Specs)
            {
                GameObject display = RequireUnique(scene, spec.DisplayName);
                Transform equipped = FindDirectChild(visual.EquipmentRoot, spec.EquippedChildName);
                if (equipped == null)
                {
                    throw new InvalidOperationException(
                        $"Full-suit child '{spec.EquippedChildName}' was not found under '{visual.EquipmentRoot.name}'.");
                }

                CapturePose(display.transform, capturedPoses);
                CapturePose(equipped, capturedPoses);
                WireDisplayItem(display, spec, templatePanel, templateGrab, templateBody);
                if (spec.AddVisualSlot)
                    AssignCleanSlot(visual, display.GetComponent<PPEItemPresentationBinding>(), equipped.gameObject);
            }

            AssertPosesUnchanged(capturedPoses);
            ValidateScene(scene, false);
            EditorSceneManager.MarkSceneDirty(scene);
            if (save && !EditorSceneManager.SaveScene(scene))
                throw new InvalidOperationException($"'{ScenePath}' 저장에 실패했습니다.");

            Debug.Log(
                "FaceShield and Goggle wear wiring complete: grab, body+trigger, and full-suit slots. " +
                "Display and equipped Transforms were not moved.");
        }
        finally
        {
            Undo.CollapseUndoOperations(undoGroup);
        }
    }

    static void WireDisplayItem(
        GameObject display,
        ItemSpec spec,
        PPEActionPanelController templatePanel,
        XRGrabInteractable templateGrab,
        Rigidbody templateBody)
    {
        Transform marker = RequireMarker(display.transform);
        if (marker.name != MarkerName)
        {
            Undo.RecordObject(marker.gameObject, "Rename PPE marker");
            marker.gameObject.name = MarkerName;
            EditorUtility.SetDirty(marker.gameObject);
        }

        GameObject placeholder = GetOrCreateDirectChild(display.transform, PlaceholderName, false);
        Transform pose = GetOrCreateDirectChild(display.transform, PoseName, false).transform;

        PPEItemIdentity identity = display.GetComponent<PPEItemIdentity>();
        if (identity == null)
        {
            identity = Undo.AddComponent<PPEItemIdentity>(display);
            identity.ConfigureItemTypeForEditor(spec.ItemType);
        }
        else if (identity.ItemType != spec.ItemType)
        {
            throw new InvalidOperationException(
                $"'{display.name}' already has PPEItemType {identity.ItemType}; expected {spec.ItemType}.");
        }

        EditorUtility.SetDirty(identity);

        PPEItemPresentationBinding binding = display.GetComponent<PPEItemPresentationBinding>();
        if (binding == null)
            binding = Undo.AddComponent<PPEItemPresentationBinding>(display);

        Undo.RecordObject(binding, "Configure FaceShield/Goggle binding");
        binding.ConfigureForEditor(identity, display, placeholder, spec.Condition);
        EditorUtility.SetDirty(binding);

        Rigidbody body = display.GetComponent<Rigidbody>();
        if (body == null)
        {
            body = Undo.AddComponent<Rigidbody>(display);
            CopySerializedValues(templateBody, body);
        }

        XRGrabInteractable grab = display.GetComponent<XRGrabInteractable>();
        if (grab == null)
        {
            grab = Undo.AddComponent<XRGrabInteractable>(display);
            CopySerializedValues(templateGrab, grab);
        }

        Collider markerCollider = marker.GetComponent<Collider>();
        if (markerCollider == null)
            throw new InvalidOperationException($"'{display.name}' marker has no Collider.");

        grab.colliders.Clear();
        grab.colliders.Add(markerCollider);
        grab.throwOnDetach = false;
        grab.retainTransformParent = true;
        EditorUtility.SetDirty(grab);

        PPEMarkerToggleGrab toggle = display.GetComponent<PPEMarkerToggleGrab>();
        if (toggle == null)
            toggle = Undo.AddComponent<PPEMarkerToggleGrab>(display);

        SerializedObject toggleSo = new SerializedObject(toggle);
        toggleSo.Update();
        toggleSo.FindProperty("interactionMarker").objectReferenceValue = marker;
        toggleSo.FindProperty("useToggleGrip").boolValue = true;
        toggleSo.FindProperty("returnToAuthoredPoseOnRelease").boolValue = true;
        toggleSo.FindProperty("holdHandGripWhileSelected").boolValue = true;
        toggleSo.ApplyModifiedProperties();
        EditorUtility.SetDirty(toggle);

        PPEInspectionState state = display.GetComponent<PPEInspectionState>();
        if (state == null)
            state = Undo.AddComponent<PPEInspectionState>(display);

        Undo.RecordObject(state, "Configure FaceShield/Goggle inspection");
        state.ConfigureForEditor(binding, grab);
        SerializedObject stateSo = new SerializedObject(state);
        stateSo.Update();
        SerializedProperty rightHandOnly = stateSo.FindProperty("rightHandGrabOnly");
        if (rightHandOnly != null)
            rightHandOnly.boolValue = false;
        stateSo.ApplyModifiedProperties();
        EditorUtility.SetDirty(state);

        PPEActionPanelController panel = display.GetComponent<PPEActionPanelController>();
        if (panel == null)
        {
            panel = Undo.AddComponent<PPEActionPanelController>(display);
            CopySerializedValues(templatePanel, panel);
        }

        SerializedObject panelSo = new SerializedObject(panel);
        panelSo.Update();
        panelSo.FindProperty("inspectionState").objectReferenceValue = state;
        panelSo.FindProperty("panelRoot").objectReferenceValue = pose.gameObject;
        panelSo.FindProperty("panelPose").objectReferenceValue = pose;
        panelSo.FindProperty("itemDisplayName").stringValue = spec.DisplayLabel;
        panelSo.FindProperty("approveUseByBodyProximity").boolValue = true;
        SerializedProperty sfx = panelSo.FindProperty("useSfxId");
        if (sfx != null)
            sfx.stringValue = string.Empty;
        SerializedProperty inspectChoice = panelSo.FindProperty("enableInspectChoice");
        if (inspectChoice != null)
            inspectChoice.boolValue = false;
        SerializedProperty requireInspect = panelSo.FindProperty("requireInspectBeforeUseOrDiscard");
        if (requireInspect != null)
            requireInspect.boolValue = false;
        SerializedProperty inspectButton = panelSo.FindProperty("inspectButton");
        if (inspectButton != null)
            inspectButton.objectReferenceValue = null;
        SerializedProperty inspectAnimation = panelSo.FindProperty("inspectAnimation");
        if (inspectAnimation != null)
            inspectAnimation.objectReferenceValue = null;
        panelSo.ApplyModifiedProperties();
        EditorUtility.SetDirty(panel);
        EditorUtility.SetDirty(display);
    }

    static void AssignCleanSlot(
        PPEEquipmentVisualController visual,
        PPEItemPresentationBinding binding,
        GameObject equippedChild)
    {
        if (binding == null)
            throw new InvalidOperationException("Clean FaceShield/Goggle binding is missing.");

        SerializedObject so = new SerializedObject(visual);
        SerializedProperty slots = so.FindProperty("slots");
        if (slots == null)
            throw new InvalidOperationException("PPEEquipmentVisualController.slots was not found.");

        int slotIndex = FindSlotIndex(slots, binding, equippedChild);
        if (slotIndex < 0)
        {
            slotIndex = slots.arraySize;
            slots.arraySize++;
        }

        SerializedProperty slot = slots.GetArrayElementAtIndex(slotIndex);
        slot.FindPropertyRelative("itemBinding").objectReferenceValue = binding;
        slot.FindPropertyRelative("equippedChild").objectReferenceValue = equippedChild;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(visual);
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
        if (visual == null || visual.EquipmentRoot == null)
            failures.Add("PPEEquipmentVisualController.equipmentRoot is missing.");

        foreach (ItemSpec spec in Specs)
        {
            GameObject display;
            try
            {
                display = RequireUnique(scene, spec.DisplayName);
            }
            catch (InvalidOperationException ex)
            {
                failures.Add(ex.Message);
                continue;
            }

            Transform marker = FindMarker(display.transform);
            PPEItemIdentity identity = display.GetComponent<PPEItemIdentity>();
            PPEItemPresentationBinding binding = display.GetComponent<PPEItemPresentationBinding>();
            PPEInspectionState state = display.GetComponent<PPEInspectionState>();
            PPEActionPanelController panel = display.GetComponent<PPEActionPanelController>();
            XRGrabInteractable grab = display.GetComponent<XRGrabInteractable>();
            PPEMarkerToggleGrab toggle = display.GetComponent<PPEMarkerToggleGrab>();
            Transform placeholder = FindDirectChild(display.transform, PlaceholderName);
            Transform pose = FindDirectChild(display.transform, PoseName);

            if (identity == null || identity.ItemType != spec.ItemType)
                failures.Add($"'{spec.DisplayName}' PPEItemIdentity/{spec.ItemType} is missing.");
            if (binding == null ||
                binding.ItemIdentity != identity ||
                binding.InspectionVisual != display ||
                binding.InitialCondition != spec.Condition ||
                placeholder == null ||
                binding.EquippedVisual != placeholder.gameObject)
            {
                failures.Add($"'{spec.DisplayName}' presentation binding is incomplete.");
            }

            if (grab == null || display.GetComponent<Rigidbody>() == null)
                failures.Add($"'{spec.DisplayName}' grab/rigidbody is missing.");
            if (marker == null || marker.GetComponent<Collider>() == null)
                failures.Add($"'{spec.DisplayName}' is missing '{MarkerName}' with a Collider.");
            if (toggle == null || toggle.InteractionMarker != marker)
                failures.Add($"'{spec.DisplayName}' marker Grab owner is not assigned.");
            if (state == null || state.PresentationBinding != binding || state.GrabInteractable != grab)
                failures.Add($"'{spec.DisplayName}' inspection/Grab chain is incomplete.");
            if (panel == null || panel.InspectionState != state || pose == null)
            {
                failures.Add($"'{spec.DisplayName}' action panel is incomplete.");
            }
            else
            {
                SerializedObject panelSo = new SerializedObject(panel);
                if (!panel.ApproveUseByBodyProximity ||
                    panelSo.FindProperty("panelRoot").objectReferenceValue != pose.gameObject ||
                    panelSo.FindProperty("panelPose").objectReferenceValue != pose)
                {
                    failures.Add($"'{spec.DisplayName}' action panel pose/body-proximity references are incomplete.");
                }
            }

            if (visual == null)
                continue;

            Transform equipped = FindDirectChild(visual.EquipmentRoot, spec.EquippedChildName);
            if (equipped == null)
            {
                failures.Add($"Equipped '{spec.EquippedChildName}' is missing under the full-suit root.");
                continue;
            }

            if (equipped.GetComponent<XRGrabInteractable>() != null ||
                equipped.GetComponent<PPEItemIdentity>() != null)
            {
                failures.Add($"Equipped '{spec.EquippedChildName}' must remain visual-only.");
            }

            if (!spec.AddVisualSlot)
                continue;

            bool slotFound = false;
            foreach (PPEEquipmentVisualSlot slot in visual.Slots ?? Array.Empty<PPEEquipmentVisualSlot>())
            {
                if (slot != null &&
                    slot.ItemBinding == binding &&
                    slot.EquippedChild == equipped.gameObject)
                {
                    slotFound = true;
                    break;
                }
            }

            if (!slotFound)
                failures.Add($"Clean slot for '{spec.DisplayName}' → '{spec.EquippedChildName}' is missing.");
        }

        if (failures.Count > 0)
        {
            string message = "FaceShield/Goggle wear validation failed:\n- " +
                string.Join("\n- ", failures);
            Debug.LogError(message);
            throw new InvalidOperationException(message);
        }

        if (logSuccess)
        {
            Debug.Log(
                "FaceShield/Goggle wear validation passed: Clean/Contam grab paths and Clean full-suit slots are connected.");
        }
    }

    static GameObject GetOrCreateDirectChild(Transform parent, string childName, bool active)
    {
        Transform existing = FindDirectChild(parent, childName);
        if (existing != null)
        {
            if (existing.gameObject.activeSelf != active)
            {
                Undo.RecordObject(existing.gameObject, "Set PPE helper active state");
                existing.gameObject.SetActive(active);
            }

            return existing.gameObject;
        }

        GameObject child = new GameObject(childName);
        Undo.RegisterCreatedObjectUndo(child, "Create PPE helper child");
        child.transform.SetParent(parent, false);
        child.transform.localPosition = Vector3.zero;
        child.transform.localRotation = Quaternion.identity;
        child.transform.localScale = Vector3.one;
        child.SetActive(active);
        return child;
    }

    static Transform RequireMarker(Transform root)
    {
        Transform marker = FindMarker(root);
        if (marker == null)
        {
            throw new InvalidOperationException(
                $"'{root.name}' needs a child named '{MarkerName}' with a Collider. The mesh is not a grab target.");
        }

        return marker;
    }

    static Transform FindMarker(Transform root)
    {
        Transform exact = null;
        Transform prefixed = null;
        foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
        {
            if (candidate == root)
                continue;
            if (candidate.name == MarkerName)
            {
                exact = candidate;
                break;
            }

            if (prefixed == null && candidate.name.StartsWith(MarkerName, StringComparison.Ordinal))
                prefixed = candidate;
        }

        return exact != null ? exact : prefixed;
    }

    static Transform FindDirectChild(Transform parent, string childName)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
                return child;
        }

        return null;
    }

    static GameObject RequireUnique(Scene scene, string objectName)
    {
        GameObject match = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name != objectName)
                    continue;
                if (match != null)
                    throw new InvalidOperationException($"'{objectName}' must be unique in '{scene.path}'.");
                match = candidate.gameObject;
            }
        }

        if (match == null)
            throw new InvalidOperationException($"'{objectName}' was not found in '{scene.path}'.");
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
                $"Open '{ScenePath}' before wiring FaceShield/Goggle wear. Current scene: '{scene.path}'.");
        }

        return scene;
    }

    static void CopySerializedValues(Component source, Component destination)
    {
        SerializedObject from = new SerializedObject(source);
        SerializedObject to = new SerializedObject(destination);
        SerializedProperty iterator = from.GetIterator();
        if (!iterator.NextVisible(true))
            return;

        do
        {
            if (IdentityProperties.Contains(iterator.propertyPath))
                continue;
            to.CopyFromSerializedProperty(iterator);
        }
        while (iterator.NextVisible(false));

        to.ApplyModifiedProperties();
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
                    $"'{GetPath(target)}' Transform changed during FaceShield/Goggle wear wiring.");
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
