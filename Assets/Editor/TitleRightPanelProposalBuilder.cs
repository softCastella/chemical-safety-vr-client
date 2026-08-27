using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Creates a visual-only right-panel proposal above the existing title buttons.
/// The original RightPanelArea and its click targets remain untouched.
/// </summary>
public static class TitleRightPanelProposalBuilder
{
    private const string TargetScenePath = "Assets/Scenes/1_Title_Test.unity";
    private const string RightPanelAreaName = "RightPanelArea";
    private const string ProposalName = "RightPanelProposal";
    private const string FontPath = "Assets/Font/Pretendard-Medium SDF.asset";

    private static readonly Color SurfaceColor = new(0.925f, 0.976f, 0.985f, 0.98f);
    private static readonly Color OutlineColor = new(0.11f, 0.69f, 0.79f, 0.34f);
    private static readonly Color AccentColor = new(0.08f, 0.74f, 0.88f, 1f);
    private static readonly Color LabelColor = new(0.055f, 0.22f, 0.29f, 1f);

    [MenuItem("Tools/Title/Create Right Panel Design Proposal")]
    private static void CreateProposal()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != TargetScenePath)
        {
            Debug.LogError($"Open '{TargetScenePath}' before creating the title-panel proposal.");
            return;
        }

        RectTransform panelArea = FindInScene(scene, RightPanelAreaName);
        if (panelArea == null)
        {
            Debug.LogError($"'{RightPanelAreaName}' was not found in '{TargetScenePath}'.");
            return;
        }

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null)
        {
            Debug.LogError($"Title-panel proposal requires '{FontPath}'.");
            return;
        }

        Transform existing = panelArea.Find(ProposalName);
        if (existing != null)
            Undo.DestroyObjectImmediate(existing.gameObject);

        GameObject proposal = UiObject(ProposalName, panelArea);
        RectTransform proposalRect = proposal.GetComponent<RectTransform>();
        Stretch(proposalRect, 0f, 0f, 0f, 0f);
        proposal.transform.SetAsLastSibling();

        CreateSurface(proposal.transform);
        CreateAccentRail(proposal.transform);
        CreateOptionVisuals(panelArea, proposal.transform, font);

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = proposal;
        Debug.Log(
            "Created a visual-only right-panel proposal. Existing RightPanelArea buttons remain unchanged and receive input through the proposal overlay.",
            proposal);
    }

    [MenuItem("Tools/Title/Remove Right Panel Design Proposal")]
    private static void RemoveProposal()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != TargetScenePath)
        {
            Debug.LogError($"Open '{TargetScenePath}' before removing the title-panel proposal.");
            return;
        }

        RectTransform panelArea = FindInScene(scene, RightPanelAreaName);
        Transform proposal = panelArea != null ? panelArea.Find(ProposalName) : null;
        if (proposal == null)
        {
            Debug.LogWarning("No right-panel proposal exists in the active test scene.");
            return;
        }

        Undo.DestroyObjectImmediate(proposal.gameObject);
        EditorSceneManager.MarkSceneDirty(scene);
    }

    [MenuItem("Tools/Title/Validate Right Panel Design Proposal")]
    private static void ValidateProposal()
    {
        Scene scene = SceneManager.GetActiveScene();
        RectTransform panelArea = scene.path == TargetScenePath ? FindInScene(scene, RightPanelAreaName) : null;
        Transform proposal = panelArea != null ? panelArea.Find(ProposalName) : null;
        if (proposal == null)
        {
            Debug.LogError("Right-panel proposal is missing. Run Tools > Title > Create Right Panel Design Proposal.");
            return;
        }

        bool raycastTargetFound = proposal.GetComponentsInChildren<Graphic>(true)
            .Any(graphic => graphic.raycastTarget);
        if (raycastTargetFound)
        {
            Debug.LogError("Right-panel proposal must not block the existing title-button raycast path.", proposal);
            return;
        }

        Debug.Log("Right-panel proposal is present and does not intercept existing button input.", proposal);
    }

    private static void CreateSurface(Transform parent)
    {
        GameObject surface = UiObject("Surface", parent);
        Stretch(surface.GetComponent<RectTransform>(), 5f, 5f, 7f, 7f);
        RoundedRectangleGraphic graphic = surface.AddComponent<RoundedRectangleGraphic>();
        graphic.color = SurfaceColor;
        graphic.CornerRadius = 7f;
        graphic.raycastTarget = false;

        GameObject divider = UiObject("Divider", parent);
        SetRect(divider.GetComponent<RectTransform>(), new Vector2(0f, 39f), new Vector2(44f, 0.7f));
        Image dividerGraphic = divider.AddComponent<Image>();
        dividerGraphic.color = new Color(0.11f, 0.69f, 0.79f, 0.28f);
        dividerGraphic.raycastTarget = false;
    }

    private static void CreateAccentRail(Transform parent)
    {
        GameObject rail = UiObject("Accent Rail", parent);
        RectTransform rect = rail.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(10f, 0f);
        rect.sizeDelta = new Vector2(1.5f, 72f);
        Image graphic = rail.AddComponent<Image>();
        graphic.color = AccentColor;
        graphic.raycastTarget = false;
    }

    private static void CreateOptionVisuals(RectTransform panelArea, Transform parent, TMP_FontAsset font)
    {
        Button[] buttons = panelArea.GetComponentsInChildren<Button>(false)
            .OrderByDescending(button => ((RectTransform)button.transform).anchoredPosition.y)
            .ToArray();

        for (int index = 0; index < buttons.Length; index++)
            CreateOptionVisual(parent, (RectTransform)buttons[index].transform, buttons[index], font, index + 1);
    }

    private static void CreateOptionVisual(
        Transform parent,
        RectTransform source,
        Button sourceButton,
        TMP_FontAsset font,
        int number)
    {
        GameObject option = UiObject($"Option {number:00}", parent);
        CopyRect(source, option.GetComponent<RectTransform>());

        RoundedRectangleGraphic graphic = option.AddComponent<RoundedRectangleGraphic>();
        graphic.color = Color.white;
        graphic.CornerRadius = 6f;
        graphic.raycastTarget = false;
        Outline outline = option.AddComponent<Outline>();
        outline.effectColor = OutlineColor;
        outline.effectDistance = new Vector2(0.8f, -0.8f);

        GameObject marker = UiObject("Marker", option.transform);
        SetRect(marker.GetComponent<RectTransform>(), new Vector2(-22.5f, 0f), new Vector2(2f, 8f));
        Image markerGraphic = marker.AddComponent<Image>();
        markerGraphic.color = AccentColor;
        markerGraphic.raycastTarget = false;

        TextMeshProUGUI numberText = Text(
            "Number",
            option.transform,
            font,
            number.ToString("00"),
            3.2f,
            AccentColor,
            TextAlignmentOptions.MidlineLeft,
            new Vector2(-17f, 0f),
            new Vector2(10f, 12f));
        numberText.raycastTarget = false;

        TMP_Text sourceLabel = sourceButton.GetComponentInChildren<TMP_Text>(true);
        string label = sourceLabel != null ? sourceLabel.text.Replace(" 버튼", string.Empty) : $"훈련 {number}";
        TextMeshProUGUI labelText = Text(
            "Label",
            option.transform,
            font,
            label,
            4f,
            LabelColor,
            TextAlignmentOptions.MidlineLeft,
            new Vector2(-5f, 0f),
            new Vector2(34f, 13f));
        labelText.raycastTarget = false;

        TextMeshProUGUI arrow = Text(
            "Arrow",
            option.transform,
            font,
            ">",
            5f,
            AccentColor,
            TextAlignmentOptions.MidlineRight,
            new Vector2(22f, 0f),
            new Vector2(7f, 13f));
        arrow.raycastTarget = false;
    }

    private static TextMeshProUGUI Text(
        string name,
        Transform parent,
        TMP_FontAsset font,
        string value,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment,
        Vector2 position,
        Vector2 size)
    {
        GameObject textObject = UiObject(name, parent);
        SetRect(textObject.GetComponent<RectTransform>(), position, size);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    private static GameObject UiObject(string name, Transform parent)
    {
        GameObject result = new(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(result, "Create Right Panel Design Proposal");
        result.layer = parent.gameObject.layer;
        result.transform.SetParent(parent, false);
        return result;
    }

    private static RectTransform FindInScene(Scene scene, string name)
    {
        return scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<RectTransform>(true))
            .FirstOrDefault(rect => rect.name == name);
    }

    private static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void CopyRect(RectTransform source, RectTransform destination)
    {
        destination.anchorMin = source.anchorMin;
        destination.anchorMax = source.anchorMax;
        destination.pivot = source.pivot;
        destination.anchoredPosition = source.anchoredPosition;
        destination.sizeDelta = source.sizeDelta;
    }
}