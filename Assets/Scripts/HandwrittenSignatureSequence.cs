using System.Collections;
using System;
using UnityEngine;

public sealed class HandwrittenSignatureSequence : MonoBehaviour
{
    [System.Serializable]
    public sealed class SignatureStep
    {
        [SerializeField] Renderer targetRenderer;
        [SerializeField] AudioClip sound;
        [SerializeField, Range(0f, 1f)] float soundVolume = 1f;
        [SerializeField] bool animateReveal = true;
        [SerializeField, Min(0f)] float delayBefore;
        [SerializeField, Min(0.05f)] float duration = 1.4f;
        [SerializeField] AnimationCurve revealCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public Renderer TargetRenderer => targetRenderer;
        public AudioClip Sound => sound;
        public float SoundVolume => soundVolume;
        public bool AnimateReveal => animateReveal;
        public float DelayBefore => delayBefore;
        public float Duration => duration;
        public AnimationCurve RevealCurve => revealCurve;
    }

    [SerializeField] SignatureStep[] signatures;
    [SerializeField] AudioSource audioSource;
    [SerializeField] bool playOnEnable = true;
    [SerializeField] bool useUnscaledTime;
    [SerializeField, Min(0f)] float initialDelay = 0.8f;
    [SerializeField] bool duckBgmDuringPlayback = true;
    [SerializeField, Range(0f, 1f)] float duckedBgmVolume = 0.25f;

    static readonly int RevealProperty = Shader.PropertyToID("_Reveal");
    Material[] runtimeMaterials;
    Coroutine playback;
    float previousBgmVolume;
    bool bgmDucked;

    public int StepCount => signatures?.Length ?? 0;
    public bool IsPlaying => playback != null;
    public bool PlayOnEnable => playOnEnable;
    public bool HasCompleteTargetReferences
    {
        get
        {
            if (signatures == null || signatures.Length == 0)
                return false;

            for (int index = 0; index < signatures.Length; index++)
            {
                if (signatures[index]?.TargetRenderer == null)
                    return false;
            }

            return true;
        }
    }

    public event Action<int, int> PlaybackCompleted;

    void Awake()
    {
        EnsureRuntimeMaterials();
        ResetSignatures();
    }

    void OnEnable()
    {
        if (playOnEnable)
            Replay();
    }

    void OnDisable()
    {
        if (playback != null)
        {
            StopCoroutine(playback);
            playback = null;
        }

        if (audioSource != null)
            audioSource.Stop();

        RestoreBgmVolume();
    }

    void OnDestroy()
    {
        if (runtimeMaterials == null)
            return;

        for (int index = 0; index < runtimeMaterials.Length; index++)
        {
            if (runtimeMaterials[index] != null)
                Destroy(runtimeMaterials[index]);
        }

        runtimeMaterials = null;
    }

    [ContextMenu("Replay Signatures")]
    public void Replay()
    {
        StartPlayback(0, StepCount, true, true, true);
    }

    public bool PlayRange(int startIndex, int count, bool resetFirst = false)
    {
        return StartPlayback(startIndex, count, resetFirst, false, false, 0);
    }

    public bool PlayRangeWithInstantPrefix(
        int startIndex,
        int count,
        int instantPrefixCount,
        bool resetFirst = false)
    {
        return StartPlayback(
            startIndex,
            count,
            resetFirst,
            false,
            false,
            Mathf.Max(0, instantPrefixCount));
    }

    [ContextMenu("Reset Signatures")]
    public void ResetSignatures()
    {
        if (signatures == null)
            return;

        for (int i = 0; i < signatures.Length; i++)
            SetStepReveal(i, signatures[i] != null && !signatures[i].AnimateReveal ? 1f : 0f);
    }

    public void SetStepReveal(int index, float reveal)
    {
        if (signatures == null || index < 0 || index >= signatures.Length)
            return;

        EnsureRuntimeMaterials();
        Material material = runtimeMaterials != null && index < runtimeMaterials.Length
            ? runtimeMaterials[index]
            : null;
        if (material == null)
            return;

        material.SetFloat(RevealProperty, Mathf.Clamp01(reveal));
    }

    void EnsureRuntimeMaterials()
    {
        if (signatures == null)
            return;

        if (runtimeMaterials != null && runtimeMaterials.Length == signatures.Length)
            return;

        runtimeMaterials = new Material[signatures.Length];
        for (int index = 0; index < signatures.Length; index++)
        {
            Renderer target = signatures[index]?.TargetRenderer;
            if (target == null)
                continue;

            Material shared = target.sharedMaterial;
            if (shared == null)
                continue;

            // Per-renderer instances keep authored assets intact and guarantee
            // _Reveal updates are visible even when MaterialPropertyBlock/GPU
            // instancing would leave the shared material stuck at 0.
            Material instance = new Material(shared)
            {
                name = shared.name + " (Reveal Instance)"
            };
            runtimeMaterials[index] = instance;
            target.sharedMaterial = instance;
        }
    }

    bool StartPlayback(
        int startIndex,
        int count,
        bool resetFirst,
        bool includeInitialDelay,
        bool replaceCurrentPlayback,
        int instantPrefixCount = 0)
    {
        if (!isActiveAndEnabled || signatures == null || count <= 0 ||
            startIndex < 0 || startIndex >= signatures.Length ||
            startIndex + count > signatures.Length)
        {
            return false;
        }

        if (playback != null)
        {
            if (!replaceCurrentPlayback)
                return false;

            StopCoroutine(playback);
            playback = null;
            RestoreBgmVolume();
        }

        if (resetFirst)
            ResetSignatures();

        playback = StartCoroutine(PlaySequence(
            startIndex,
            count,
            includeInitialDelay,
            Mathf.Min(instantPrefixCount, count)));
        return true;
    }

    IEnumerator PlaySequence(
        int startIndex,
        int count,
        bool includeInitialDelay,
        int instantPrefixCount)
    {
        if (includeInitialDelay && initialDelay > 0f)
            yield return Wait(initialDelay);

        DuckBgm();

        int endIndex = startIndex + count;
        int instantEndIndex = Mathf.Min(startIndex + instantPrefixCount, endIndex);
        for (int i = startIndex; i < instantEndIndex; i++)
            SetStepReveal(i, 1f);

        if (instantPrefixCount > 0 && audioSource != null)
        {
            SignatureStep cueStep = signatures[startIndex];
            if (cueStep?.Sound != null)
                audioSource.PlayOneShot(cueStep.Sound, cueStep.SoundVolume);
        }

        for (int i = instantEndIndex; i < endIndex; i++)
        {
            SignatureStep step = signatures[i];
            if (step == null || step.TargetRenderer == null)
                continue;

            if (!step.AnimateReveal)
            {
                SetStepReveal(i, 1f);
                continue;
            }

            if (step.DelayBefore > 0f)
                yield return Wait(step.DelayBefore);

            if (audioSource != null && step.Sound != null)
            {
                float previousPitch = audioSource.pitch;
                // Match writing SFX length to the authored reveal duration.
                if (step.Sound.length > 0.001f && step.Duration > 0.001f)
                    audioSource.pitch = step.Sound.length / step.Duration;
                audioSource.PlayOneShot(step.Sound, step.SoundVolume);
                audioSource.pitch = previousPitch;
            }

            float elapsed = 0f;
            while (elapsed < step.Duration)
            {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / step.Duration);
                SetStepReveal(i, step.RevealCurve.Evaluate(normalizedTime));
                yield return null;
            }

            SetStepReveal(i, 1f);
        }

        RestoreBgmVolume();
        playback = null;
        PlaybackCompleted?.Invoke(startIndex, count);
    }

#if UNITY_EDITOR
    public void SetPlayOnEnableForEditor(bool value)
    {
        playOnEnable = value;
    }
#endif

    void DuckBgm()
    {
        if (!duckBgmDuringPlayback || bgmDucked || AudioManager.Instance == null)
            return;

        previousBgmVolume = AudioManager.Instance.BgmVolume;
        AudioManager.Instance.SetBgmVolume(Mathf.Min(previousBgmVolume, duckedBgmVolume));
        bgmDucked = true;
    }

    void RestoreBgmVolume()
    {
        if (!bgmDucked)
            return;

        if (AudioManager.Instance != null)
            AudioManager.Instance.SetBgmVolume(previousBgmVolume);
        bgmDucked = false;
    }

    IEnumerator Wait(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            yield return null;
        }
    }
}
