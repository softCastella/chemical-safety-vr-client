using UnityEngine;

public enum PPEItemCondition
{
    Clean,
    Contaminated
}

[DisallowMultipleComponent]
[RequireComponent(typeof(PPEItemIdentity))]
public sealed class PPEItemPresentationBinding : MonoBehaviour
{
    [SerializeField]
    [Tooltip("이 PPE의 종류를 식별하는 씬 작성 컴포넌트입니다.")]
    PPEItemIdentity itemIdentity;

    [SerializeField]
    [Tooltip("사용자가 잡고 관찰하는 입기 전 월드 오브젝트입니다.")]
    GameObject inspectionVisual;

    [SerializeField]
    [Tooltip("착용 연출 완료 후 신체와 거울에 표시할 장착 오브젝트입니다.")]
    GameObject equippedVisual;

    [SerializeField]
    [Tooltip("씬에서 작성한 이 PPE의 초기 상태입니다.")]
    PPEItemCondition initialCondition = PPEItemCondition.Clean;

    public PPEItemIdentity ItemIdentity => itemIdentity;
    public GameObject InspectionVisual => inspectionVisual;
    public GameObject EquippedVisual => equippedVisual;
    public PPEItemCondition InitialCondition => initialCondition;

    public bool HasCompleteReferences =>
        itemIdentity != null &&
        inspectionVisual != null &&
        equippedVisual != null &&
        inspectionVisual != equippedVisual;

#if UNITY_EDITOR
    public void ConfigureForEditor(
        PPEItemIdentity configuredIdentity,
        GameObject configuredInspectionVisual,
        GameObject configuredEquippedVisual,
        PPEItemCondition configuredInitialCondition)
    {
        itemIdentity = configuredIdentity;
        inspectionVisual = configuredInspectionVisual;
        equippedVisual = configuredEquippedVisual;
        initialCondition = configuredInitialCondition;
    }
#endif
}
