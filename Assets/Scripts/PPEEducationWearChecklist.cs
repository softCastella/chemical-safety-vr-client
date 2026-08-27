using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class PPEEducationWearChecklistEntry
{
    [SerializeField]
    GameObject checkMark;

    [SerializeField]
    PPEItemType[] itemTypes;

    public GameObject CheckMark => checkMark;
    public PPEItemType[] ItemTypes => itemTypes;

#if UNITY_EDITOR
    public void ConfigureForEditor(GameObject configuredCheckMark, PPEItemType[] configuredItemTypes)
    {
        checkMark = configuredCheckMark;
        itemTypes = configuredItemTypes ?? Array.Empty<PPEItemType>();
    }
#endif
}

[DisallowMultipleComponent]
public sealed class PPEEducationWearChecklist : MonoBehaviour
{
    [SerializeField]
    PPEVoiceFlowDirector director;

    [SerializeField]
    PPETabletChecklistController tablet;

    [SerializeField]
    PPEEquipmentVisualController equipmentVisual;

    [SerializeField]
    PPEHazmatEquipController hazmatEquip;

    [SerializeField]
    CanvasGroup panelGroup;

    [SerializeField]
    GameObject confinedRoot;

    [SerializeField]
    GameObject leakRoot;

    [SerializeField]
    PPEEducationWearChecklistEntry[] confinedEntries;

    [SerializeField]
    PPEEducationWearChecklistEntry[] leakEntries;

    readonly HashSet<PPEItemType> approvedItemTypes = new();
    readonly List<PPEActionPanelController> subscribedPanels = new();
    ScenarioDetailModal.PpeWorkPlan observedWorkPlan = ScenarioDetailModal.PpeWorkPlan.None;
    bool runtimeInitialized;
    bool isShown;

    void Awake()
    {
        if (!Application.isPlaying)
            return;

        HideChecks(confinedEntries);
        HideChecks(leakEntries);
        ApplyVisibility(false);
    }

    void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        if (!HasCompleteReferences())
        {
            Debug.LogError(
                "PPEEducationWearChecklist requires authored director, tablet, equipment visual, hazmat, CanvasGroup, confined/leak roots, and confined list entries.",
                this);
            enabled = false;
            return;
        }

        SubscribePanels(true);
        if (tablet != null)
            tablet.DocumentCompleted += OnTabletDocumentCompleted;
        runtimeInitialized = true;
        ApplyVisibility(ShouldShow());
    }

    void OnDisable()
    {
        if (!runtimeInitialized)
            return;

        if (tablet != null)
            tablet.DocumentCompleted -= OnTabletDocumentCompleted;
        SubscribePanels(false);
        subscribedPanels.Clear();
        approvedItemTypes.Clear();
        runtimeInitialized = false;
    }

    void Update()
    {
        ResetChecksWhenWorkPlanChanges();

        bool show = ShouldShow();
        if (show != isShown)
            ApplyVisibility(show);
        else if (show)
            RefreshCheckMarks();
    }

    void OnTabletDocumentCompleted()
    {
        ApplyVisibility(ShouldShow());
    }

    void ResetChecksWhenWorkPlanChanges()
    {
        ScenarioDetailModal.PpeWorkPlan activeWorkPlan = director != null
            ? director.ActiveWorkPlan
            : ScenarioDetailModal.PpeWorkPlan.None;
        if (activeWorkPlan == observedWorkPlan)
            return;

        observedWorkPlan = activeWorkPlan;
        approvedItemTypes.Clear();
        HideChecks(confinedEntries);
        HideChecks(leakEntries);
    }

    bool ShouldShow()
    {
        return director != null &&
            tablet != null &&
            director.ActiveLearningMode == ScenarioDetailModal.PpeLearningMode.Education &&
            director.ActiveWorkPlan != ScenarioDetailModal.PpeWorkPlan.None &&
            tablet.IsDocumentCompleted;
    }

    void ApplyVisibility(bool show)
    {
        isShown = show;

        if (panelGroup != null)
        {
            panelGroup.alpha = show ? 1f : 0f;
            panelGroup.interactable = show;
            panelGroup.blocksRaycasts = show;
        }

        bool showLeak = show && director != null &&
            director.ActiveWorkPlan == ScenarioDetailModal.PpeWorkPlan.LeakResponse;
        if (confinedRoot != null)
            confinedRoot.SetActive(show && !showLeak);
        if (leakRoot != null)
            leakRoot.SetActive(showLeak);

        if (!show)
        {
            HideChecks(confinedEntries);
            HideChecks(leakEntries);
            return;
        }

        RefreshCheckMarks();
    }

    void SubscribePanels(bool subscribe)
    {
        if (!subscribe)
        {
            for (int index = 0; index < subscribedPanels.Count; index++)
            {
                PPEActionPanelController panel = subscribedPanels[index];
                if (panel != null)
                    panel.ChoiceResolvedWithSource -= OnChoiceResolved;
            }

            return;
        }

        subscribedPanels.Clear();
        PPEActionPanelController[] panels = FindObjectsByType<PPEActionPanelController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int index = 0; index < panels.Length; index++)
        {
            PPEActionPanelController panel = panels[index];
            if (panel == null)
                continue;

            panel.ChoiceResolvedWithSource += OnChoiceResolved;
            subscribedPanels.Add(panel);
        }
    }

    void OnChoiceResolved(
        PPEActionPanelController panel,
        PPEActionChoice choice,
        PPEActionResult result)
    {
        if (choice != PPEActionChoice.Use || result != PPEActionResult.UseApproved)
            return;

        PPEItemType? itemType = panel?.InspectionState?.PresentationBinding?.ItemIdentity?.ItemType;
        if (!itemType.HasValue)
            return;

        approvedItemTypes.Add(itemType.Value);
        RefreshCheckMarks();
    }

    void RefreshCheckMarks()
    {
        PPEEducationWearChecklistEntry[] entries =
            director != null && director.ActiveWorkPlan == ScenarioDetailModal.PpeWorkPlan.LeakResponse
                ? leakEntries
                : confinedEntries;
        ApplyChecks(entries);
    }

    void ApplyChecks(PPEEducationWearChecklistEntry[] entries)
    {
        if (entries == null)
            return;

        for (int index = 0; index < entries.Length; index++)
        {
            PPEEducationWearChecklistEntry entry = entries[index];
            if (entry?.CheckMark == null)
                continue;

            bool checkedOn = AreAllEntryTypesWorn(entry);
            if (entry.CheckMark.activeSelf != checkedOn)
                entry.CheckMark.SetActive(checkedOn);
        }
    }

    static void HideChecks(PPEEducationWearChecklistEntry[] entries)
    {
        if (entries == null)
            return;

        for (int index = 0; index < entries.Length; index++)
        {
            GameObject checkMark = entries[index]?.CheckMark;
            if (checkMark != null && checkMark.activeSelf)
                checkMark.SetActive(false);
        }
    }

    bool AreAllEntryTypesWorn(PPEEducationWearChecklistEntry entry)
    {
        PPEItemType[] types = entry.ItemTypes;
        if (types == null || types.Length == 0)
            return false;

        for (int index = 0; index < types.Length; index++)
        {
            if (!IsWorn(types[index]))
                return false;
        }

        return true;
    }

    bool IsWorn(PPEItemType itemType)
    {
        if (approvedItemTypes.Contains(itemType))
            return true;

        if (itemType == PPEItemType.HazmatSuit && hazmatEquip != null && hazmatEquip.IsEquipped)
            return true;

        return equipmentVisual != null && equipmentVisual.IsItemUsed(itemType);
    }

    bool HasCompleteReferences()
    {
        return director != null &&
            tablet != null &&
            equipmentVisual != null &&
            hazmatEquip != null &&
            panelGroup != null &&
            confinedRoot != null &&
            leakRoot != null &&
            confinedEntries != null &&
            confinedEntries.Length > 0;
    }

#if UNITY_EDITOR
    public void ConfigureForEditor(
        PPEVoiceFlowDirector configuredDirector,
        PPETabletChecklistController configuredTablet,
        PPEEquipmentVisualController configuredVisual,
        PPEHazmatEquipController configuredHazmat,
        CanvasGroup configuredPanelGroup,
        GameObject configuredConfinedRoot,
        GameObject configuredLeakRoot,
        PPEEducationWearChecklistEntry[] configuredConfinedEntries,
        PPEEducationWearChecklistEntry[] configuredLeakEntries)
    {
        director = configuredDirector;
        tablet = configuredTablet;
        equipmentVisual = configuredVisual;
        hazmatEquip = configuredHazmat;
        panelGroup = configuredPanelGroup;
        confinedRoot = configuredConfinedRoot;
        leakRoot = configuredLeakRoot;
        confinedEntries = configuredConfinedEntries ?? Array.Empty<PPEEducationWearChecklistEntry>();
        leakEntries = configuredLeakEntries ?? Array.Empty<PPEEducationWearChecklistEntry>();
    }
#endif
}
