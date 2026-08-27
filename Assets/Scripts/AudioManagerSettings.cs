using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "AudioManagerSettings", menuName = "3D UI/Audio Manager Settings")]
public sealed class AudioManagerSettings : ScriptableObject
{
    [Serializable]
    public sealed class Sound
    {
        public string id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    [Header("Mixer Routing (Optional)")]
    public AudioMixerGroup bgmOutput;
    public AudioMixerGroup ambienceOutput;
    public AudioMixerGroup sfxOutput;
    public AudioMixerGroup voiceOutput;

    [Header("Startup")]
    public bool playBgmOnStartup;
    public string startupBgmId;

    [Header("Sound Library")]
    public List<Sound> bgm = new();
    public List<Sound> ambience = new();
    public List<Sound> sfx = new();
}
