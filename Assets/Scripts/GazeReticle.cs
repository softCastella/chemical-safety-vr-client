using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Gaze;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// World-space crosshair for the <see cref="XRGazeInteractor"/> with a radial gauge that fills as
/// the dwell timer counts toward selection. The gaze interactor ships with no line renderer or
/// reticle, so without this there is no way to tell where the ray points or how much longer to hold.
///
/// Everything (mesh, sprites, canvas) is generated at runtime, so there is nothing to wire up beyond
/// dropping the component into a gaze scene.
/// </summary>
[DefaultExecutionOrder(100)]
public class GazeReticle : MonoBehaviour
{
    /// <summary>Angular size of the reticle, kept constant regardless of hit distance.</summary>
    const float k_AngularScale = 0.035f;
    const float k_FallbackDistance = 3f;
    const float k_SurfaceOffset = 0.02f;
    const int k_TextureSize = 128;

    static readonly Color k_IdleColor = new Color(1f, 1f, 1f, 0.35f);
    static readonly Color k_HoverColor = new Color(0.35f, 0.78f, 1f, 0.9f);
    static readonly Color k_ProgressColor = new Color(0.35f, 0.78f, 1f, 1f);
    static readonly Color k_SelectedColor = new Color(0.4f, 1f, 0.5f, 1f);

    XRGazeInteractor m_Interactor;
    Camera m_Camera;

    Transform m_Root;
    Image m_Ring;
    Image m_Progress;
    Image m_Dot;

    IXRInteractable m_Hovered;
    float m_HoverStartTime;
    bool m_Selected;

    void Start()
    {
        m_Interactor = FindAnyObjectByType<XRGazeInteractor>(FindObjectsInactive.Include);
        if (m_Interactor == null)
        {
            Debug.LogWarning("[GAZE] GazeReticle found no XRGazeInteractor. Disabling.");
            enabled = false;
            return;
        }

        BuildVisuals();

        m_Interactor.hoverEntered.AddListener(OnHoverEntered);
        m_Interactor.hoverExited.AddListener(OnHoverExited);
        m_Interactor.selectEntered.AddListener(OnSelectEntered);
        m_Interactor.selectExited.AddListener(OnSelectExited);
    }

    void OnDestroy()
    {
        if (m_Interactor == null)
            return;

        m_Interactor.hoverEntered.RemoveListener(OnHoverEntered);
        m_Interactor.hoverExited.RemoveListener(OnHoverExited);
        m_Interactor.selectEntered.RemoveListener(OnSelectEntered);
        m_Interactor.selectExited.RemoveListener(OnSelectExited);
    }

    void LateUpdate()
    {
        if (m_Root == null)
            return;

        // The DemoScene toggles the interactor on and off via its trigger zone, so follow that state.
        if (!m_Interactor.isActiveAndEnabled)
        {
            m_Root.gameObject.SetActive(false);
            return;
        }

        m_Root.gameObject.SetActive(true);

        if (m_Camera == null)
            m_Camera = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();

        var origin = m_Interactor.rayOriginTransform != null
            ? m_Interactor.rayOriginTransform
            : m_Interactor.transform;

        Vector3 point;
        float distance;
        if (m_Interactor.TryGetCurrent3DRaycastHit(out var hit))
        {
            point = hit.point;
            distance = hit.distance;
        }
        else
        {
            point = origin.position + origin.forward * k_FallbackDistance;
            distance = k_FallbackDistance;
        }

        // Lift off the surface so the reticle is not swallowed by the geometry it sits on.
        m_Root.position = point + (origin.position - point).normalized * k_SurfaceOffset;

        var eye = m_Camera != null ? m_Camera.transform.position : origin.position;
        m_Root.rotation = Quaternion.LookRotation(m_Root.position - eye);
        m_Root.localScale = Vector3.one * (k_AngularScale * Mathf.Max(distance, 0.5f));

        UpdateGauge();
    }

    void UpdateGauge()
    {
        if (m_Selected)
        {
            m_Ring.color = k_SelectedColor;
            m_Progress.fillAmount = 1f;
            m_Progress.color = k_SelectedColor;
            m_Dot.color = k_SelectedColor;
            return;
        }

        if (m_Hovered == null)
        {
            m_Ring.color = k_IdleColor;
            m_Progress.fillAmount = 0f;
            m_Dot.color = k_IdleColor;
            return;
        }

        m_Ring.color = k_HoverColor;
        m_Dot.color = k_HoverColor;

        // Only fill the gauge when dwelling can actually select. Otherwise a hover-only target
        // would show a gauge that never completes, which reads as a bug.
        var selectTime = DwellTimeFor(m_Hovered);
        if (selectTime <= 0f)
        {
            m_Progress.fillAmount = 0f;
            return;
        }

        m_Progress.color = k_ProgressColor;
        m_Progress.fillAmount = Mathf.Clamp01((Time.time - m_HoverStartTime) / selectTime);
    }

    /// <summary>
    /// Seconds of dwell needed to select <paramref name="interactable"/>, or 0 if it cannot be
    /// gaze-selected. Mirrors <c>XRGazeInteractor.GetHoverTimeToSelect</c>, which is not public.
    /// </summary>
    float DwellTimeFor(IXRInteractable interactable)
    {
        if (!m_Interactor.hoverToSelect)
            return 0f;

        if (interactable is XRBaseInteractable baseInteractable && !baseInteractable.allowGazeSelect)
            return 0f;

        if (interactable is IXROverridesGazeAutoSelect { overrideGazeTimeToSelect: true } overrideProvider)
            return overrideProvider.gazeTimeToSelect;

        return m_Interactor.hoverTimeToSelect;
    }

    void OnHoverEntered(HoverEnterEventArgs args)
    {
        m_Hovered = args.interactableObject;
        m_HoverStartTime = Time.time;
    }

    void OnHoverExited(HoverExitEventArgs args)
    {
        if (ReferenceEquals(m_Hovered, args.interactableObject))
            m_Hovered = null;
    }

    void OnSelectEntered(SelectEnterEventArgs args) => m_Selected = true;

    void OnSelectExited(SelectExitEventArgs args) => m_Selected = false;

    void BuildVisuals()
    {
        var canvasGo = new GameObject("Gaze Reticle Canvas");
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        var canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(1f, 1f);
        m_Root = canvasGo.transform;

        // Dim ring that marks the aim point even with nothing under the cursor.
        m_Ring = CreateImage("Ring", canvasRect, CreateRingSprite(0.62f, 0.86f), 1f);

        // The gauge itself: same ring geometry, drawn as a clockwise radial fill from the top.
        m_Progress = CreateImage("Progress", canvasRect, CreateRingSprite(0.58f, 0.9f), 1f);
        m_Progress.type = Image.Type.Filled;
        m_Progress.fillMethod = Image.FillMethod.Radial360;
        m_Progress.fillOrigin = (int)Image.Origin360.Top;
        m_Progress.fillClockwise = true;
        m_Progress.fillAmount = 0f;

        m_Dot = CreateImage("Dot", canvasRect, CreateRingSprite(0f, 0.16f), 1f);
    }

    static Image CreateImage(string name, RectTransform parent, Sprite sprite, float size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(size, size);

        return image;
    }

    /// <summary>
    /// Builds a white annulus (or a disc when <paramref name="innerRatio"/> is 0) with a one-pixel
    /// feathered edge, so the reticle does not need any imported texture asset.
    /// </summary>
    static Sprite CreateRingSprite(float innerRatio, float outerRatio)
    {
        var texture = new Texture2D(k_TextureSize, k_TextureSize, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
        };

        var pixels = new Color32[k_TextureSize * k_TextureSize];
        var center = (k_TextureSize - 1) * 0.5f;
        var inner = innerRatio * center;
        var outer = outerRatio * center;

        for (var y = 0; y < k_TextureSize; y++)
        {
            for (var x = 0; x < k_TextureSize; x++)
            {
                var dx = x - center;
                var dy = y - center;
                var distance = Mathf.Sqrt(dx * dx + dy * dy);

                var alpha = Mathf.Clamp01(outer - distance);
                if (inner > 0f)
                    alpha *= Mathf.Clamp01(distance - inner);

                pixels[y * k_TextureSize + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0f, 0f, k_TextureSize, k_TextureSize), new Vector2(0.5f, 0.5f), 100f);
    }
}
