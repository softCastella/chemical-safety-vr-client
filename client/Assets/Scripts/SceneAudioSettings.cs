using UnityEngine;

[CreateAssetMenu(fileName = "SceneAudioSettings", menuName = "3D UI/Scene Audio Settings")]
public sealed class SceneAudioSettings : ScriptableObject
{
    [Header("Playback Timing")]
    [Min(0f)] public float startDelay = 0.5f;

    [Header("BGM")]
    public AudioClip bgm;
    [Range(0f, 1f)] public float bgmVolume = 1f;
    [Min(0f)] public float bgmFadeInDuration;
    public bool stopPreviousBgmWhenEmpty = true;

    [Header("Ambience")]
    public AudioClip ambience;
    [Range(0f, 1f)] public float ambienceVolume = 1f;
    public bool stopPreviousAmbienceWhenEmpty = true;
}
