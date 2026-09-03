using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class AudioManager : MonoBehaviour
{
    [Serializable]
    public sealed class Sound
    {
        public string id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("이 클립을 재생할지 여부입니다.")]
        public bool enabled = true;
    }

    [Serializable]
    public sealed class BgmSound
    {
        public string id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("이 곡을 재생 후보로 사용할지 여부입니다.")]
        public bool enabled = true;
    }

    [Header("Scene-owned Sources")]
    [SerializeField] private AudioSource m_BgmSource;
    [SerializeField] private AudioSource m_VoiceSource;
    [SerializeField] private AudioSource m_SfxSource;
    [SerializeField] private AudioSource m_AmbienceSource;

    [Header("Scene Transition")]
    [Tooltip("Keep this scene-owned manager alive only until a flow controller explicitly releases it.")]
    [SerializeField] private bool m_PersistAcrossSceneLoads;

    [Header("This Scene: Start BGM")]
    [Tooltip("이 씬이 시작될 때 재생할 BGM입니다. 전체 앱 또는 이후 Flow의 시작 설정이 아닙니다.")]
    [SerializeField] private bool m_PlayBgmOnStart;
    [Tooltip("이 씬 시작 BGM으로 사용할 BGM Library 항목 ID입니다.")]
    [SerializeField] private string m_StartupBgmId;
    [Tooltip("이 씬 시작 후 BGM을 요청하기까지의 지연 시간입니다.")]
    [SerializeField, Min(0f)] private float m_StartupBgmDelay = 0.2f;

    [Header("BGM Library")]
    [SerializeField] private List<BgmSound> m_Bgm = new();

    [Header("Voice Library")]
    [SerializeField] private List<Sound> m_Voice = new();

    [Header("Ambience Library")]
    [SerializeField] private List<Sound> m_Ambience = new();

    [Header("SFX Library")]
    [SerializeField] private List<Sound> m_Sfx = new();

    public static AudioManager Instance { get; private set; }
    public float BgmVolume => m_BgmSource != null ? m_BgmSource.volume : 0f;
    public bool IsVoiceLoading => m_VoiceSource != null
        && m_VoiceSource.clip != null
        && m_VoiceSource.clip.loadState == AudioDataLoadState.Loading;
    public bool IsVoicePlaying => m_VoiceSource != null && m_VoiceSource.isPlaying;
    public AudioClip CurrentVoiceClip => m_VoiceSource != null ? m_VoiceSource.clip : null;

    private Coroutine m_BgmFadeRoutine;
    private Coroutine m_StartupBgmRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError(
                "More than one scene AudioManager is active. Keep one AudioManager in the loaded test scene.",
                this);
            enabled = false;
            return;
        }

        if (!ValidateSources())
        {
            enabled = false;
            return;
        }

        Instance = this;

        if (m_PersistAcrossSceneLoads)
            DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (m_PlayBgmOnStart && !string.IsNullOrWhiteSpace(m_StartupBgmId))
            m_StartupBgmRoutine = StartCoroutine(PlayStartupBgmRoutine());
    }

    private void OnDestroy()
    {
        if (m_StartupBgmRoutine != null)
            StopCoroutine(m_StartupBgmRoutine);

        if (m_BgmFadeRoutine != null)
            StopCoroutine(m_BgmFadeRoutine);

        if (Instance == this)
            Instance = null;
    }

    public void PlayBgm(string id)
    {
        PlayBgm(id, 0f);
    }

    public void PlayBgm(string id, float fadeInDuration)
    {
        BgmSound sound = FindBgm(id);
        if (sound == null)
            return;

        if (!sound.enabled)
        {
            Debug.Log($"AudioManager: BGM '{id}' is disabled in this scene.", this);
            return;
        }

        if (sound.clip == null)
        {
            Debug.LogError($"AudioManager: BGM '{id}' has no serialized AudioClip.", this);
            return;
        }

        StopBgmFade();
        if (fadeInDuration <= 0f)
        {
            PlayLooping(sound.clip, sound.volume, m_BgmSource);
            return;
        }

        m_BgmFadeRoutine = StartCoroutine(
            FadeInBgmRoutine(sound.clip, sound.volume, fadeInDuration));
    }

    public void StopBgm()
    {
        StopBgmFade();
        m_BgmSource.Stop();
    }

    public void StopAndReleasePersistentPlayback()
    {
        if (!m_PersistAcrossSceneLoads)
            return;

        m_PersistAcrossSceneLoads = false;
        StopBgm();
        Destroy(gameObject);
    }

    public void FadeOutBgm(float duration)
    {
        StopBgmFade();
        m_BgmFadeRoutine = StartCoroutine(FadeOutBgmRoutine(duration));
    }

    public void PlayAmbience(string id)
    {
        Sound sound = Find(m_Ambience, id, "Ambience");
        if (sound?.clip == null || !sound.enabled)
            return;

        PlayLooping(sound.clip, sound.volume, m_AmbienceSource);
    }

    public void StopAmbience()
    {
        m_AmbienceSource.Stop();
    }

    public void PlaySfx(string id)
    {
        Sound sound = Find(m_Sfx, id, "SFX");
        if (sound?.clip != null && sound.enabled)
            m_SfxSource.PlayOneShot(sound.clip, sound.volume);
    }

    public bool PlayLoopingSfx(string id, AudioSource targetSource)
    {
        Sound sound = Find(m_Sfx, id, "SFX");
        if (sound?.clip == null || !sound.enabled || targetSource == null)
            return false;

        if (targetSource.isPlaying && targetSource.clip == sound.clip)
        {
            targetSource.volume = sound.volume;
            return true;
        }

        targetSource.Stop();
        targetSource.clip = sound.clip;
        targetSource.volume = sound.volume;
        targetSource.loop = true;
        targetSource.Play();
        return true;
    }

    public void StopLoopingSfx(AudioSource targetSource)
    {
        if (targetSource == null)
            return;

        targetSource.Stop();
        targetSource.loop = false;
        targetSource.clip = null;
    }

    public void StopSfx()
    {
        m_SfxSource.Stop();
    }

    public bool PlayVoice(AudioClip clip, float volume = 1f)
    {
        if (clip == null)
            return false;

        Sound sound = FindByClip(m_Voice, clip);
        if (sound != null && !sound.enabled)
            return true;

        StopVoice();
        if (clip.loadState == AudioDataLoadState.Unloaded)
            clip.LoadAudioData();

        m_VoiceSource.clip = clip;
        float configuredVolume = sound == null ? 1f : sound.volume;
        m_VoiceSource.volume = Mathf.Clamp01(volume * configuredVolume);
        m_VoiceSource.loop = false;
        m_VoiceSource.Play();
        return true;
    }

    public void StopVoice()
    {
        m_VoiceSource.Stop();
        m_VoiceSource.clip = null;
    }

    public void SetBgmVolume(float volume)
    {
        StopBgmFade();
        m_BgmSource.volume = Mathf.Clamp01(volume);
    }

    public void SetAmbienceVolume(float volume)
    {
        m_AmbienceSource.volume = Mathf.Clamp01(volume);
    }

    public void SetSfxVolume(float volume)
    {
        m_SfxSource.volume = Mathf.Clamp01(volume);
    }

    private IEnumerator PlayStartupBgmRoutine()
    {
        if (m_StartupBgmDelay > 0f)
            yield return new WaitForSecondsRealtime(m_StartupBgmDelay);

        PlayBgm(m_StartupBgmId);
        m_StartupBgmRoutine = null;
    }

    private IEnumerator FadeOutBgmRoutine(float duration)
    {
        if (!m_BgmSource.isPlaying || duration <= 0f)
        {
            m_BgmSource.Stop();
            m_BgmFadeRoutine = null;
            yield break;
        }

        float startVolume = Mathf.Max(m_BgmSource.volume, 0.0001f);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            m_BgmSource.volume = Mathf.Lerp(startVolume, 0f, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        m_BgmSource.Stop();
        m_BgmSource.volume = startVolume;
        m_BgmFadeRoutine = null;
    }

    private IEnumerator FadeInBgmRoutine(AudioClip clip, float targetVolume, float duration)
    {
        targetVolume = Mathf.Clamp01(targetVolume);
        m_BgmSource.Stop();
        m_BgmSource.loop = true;
        m_BgmSource.clip = clip;
        m_BgmSource.volume = 0f;
        m_BgmSource.Play();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            m_BgmSource.volume = Mathf.Lerp(
                0f,
                targetVolume,
                Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        m_BgmSource.volume = targetVolume;
        m_BgmFadeRoutine = null;
    }

    private void StopBgmFade()
    {
        if (m_BgmFadeRoutine == null)
            return;

        StopCoroutine(m_BgmFadeRoutine);
        m_BgmFadeRoutine = null;
    }

    private bool ValidateSources()
    {
        if (m_BgmSource != null && m_VoiceSource != null
            && m_SfxSource != null && m_AmbienceSource != null)
            return true;

        Debug.LogError(
            "AudioManager requires serialized BGM, Voice, SFX, and Ambience AudioSource references. " +
            "Use Tools > Audio > Setup HandTest Scale 0 Scene Audio Manager to repair the target test scene.",
            this);
        return false;
    }

    private static void PlayLooping(AudioClip clip, float volume, AudioSource source)
    {
        if (clip == null || source == null)
            return;

        if (source.isPlaying && source.clip == clip)
        {
            source.volume = Mathf.Clamp01(volume);
            return;
        }

        source.loop = true;
        source.clip = clip;
        source.volume = Mathf.Clamp01(volume);
        source.Play();
    }

    private BgmSound FindBgm(string id)
    {
        if (m_Bgm == null || string.IsNullOrWhiteSpace(id))
            return null;

        foreach (BgmSound sound in m_Bgm)
        {
            if (sound != null && sound.id == id)
                return sound;
        }

        Debug.LogWarning($"BGM id '{id}' is not registered on this scene AudioManager.", this);
        return null;
    }

    private static Sound Find(List<Sound> sounds, string id, string category)
    {
        if (sounds == null || string.IsNullOrWhiteSpace(id))
            return null;

        foreach (Sound sound in sounds)
        {
            if (sound != null && sound.id == id)
                return sound;
        }

        Debug.LogWarning($"{category} id '{id}' is not registered on this scene AudioManager.");
        return null;
    }

    private static Sound FindByClip(List<Sound> sounds, AudioClip clip)
    {
        if (sounds == null || clip == null)
            return null;

        foreach (Sound sound in sounds)
        {
            if (sound != null && sound.clip == clip)
                return sound;
        }

        return null;
    }
}
