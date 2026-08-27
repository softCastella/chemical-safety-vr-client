using UnityEngine;

public enum PPEItemType
{
    ConstructionHelmet,
    PackingTape,
    ScubaGear,
    TacticalHarness,
    RubberGloveRight,
    RubberGloveLeft,
    RubberBootRight,
    RubberBootLeft,
    GasMask,
    HazmatSuit,
    FaceShield,
    SafetyGoggles,
    NitrileInnerGloveRight,
    NitrileInnerGloveLeft
}

[DisallowMultipleComponent]
public sealed class PPEItemIdentity : MonoBehaviour
{
    [SerializeField] private PPEItemType itemType;
    [SerializeField, HideInInspector] private bool platformGrabSetupApplied;

    public PPEItemType ItemType => itemType;
    public bool PlatformGrabSetupApplied => platformGrabSetupApplied;

#if UNITY_EDITOR
    public void ConfigureForEditor(PPEItemType configuredType)
    {
        ConfigureItemTypeForEditor(configuredType);
        platformGrabSetupApplied = true;
    }

    public void ConfigureItemTypeForEditor(PPEItemType configuredType)
    {
        itemType = configuredType;
    }
#endif
}
