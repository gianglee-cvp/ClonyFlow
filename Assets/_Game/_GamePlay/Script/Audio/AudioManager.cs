using System.Collections.Generic;
using UnityEngine;

namespace ColonyFlow.Gameplay
{
    public sealed class AudioManager : Singleton<AudioManager>
    {
        private const string DefaultConfigPath = "Audio/DefaultAudioConfig";

        [SerializeField] private AudioConfig config;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField, Range(0, 1)] private float duckedVolume = .4f;

        private readonly Dictionary<AudioCue, AudioCueEntry> entries = new Dictionary<AudioCue, AudioCueEntry>();
        private readonly Dictionary<AudioCue, float> lastPlayedAt = new Dictionary<AudioCue, float>();
        private readonly HashSet<AudioCue> missingWarnings = new HashSet<AudioCue>();
        private AudioCue? currentMusic;
        private float currentMusicVolume = 1;
        private bool initialized;
        private bool musicDucked;

        public bool MusicEnabled { get; private set; } = true;
        public bool SfxEnabled { get; private set; } = true;

        public void Init()
        {
            if (initialized) return;
            initialized = true;
            if (config == null) config = Resources.Load<AudioConfig>(DefaultConfigPath);
            EnsureSources();
            BuildLookup();
            MusicEnabled = PlayerDataManager.Instance.MusicEnabled;
            SfxEnabled = PlayerDataManager.Instance.SfxEnabled;
            ApplyMusicOutput();
        }

        public void SetMusicEnabled(bool enabled)
        {
            Init();
            MusicEnabled = enabled;
            PlayerDataManager.Instance.SetMusicEnabled(enabled);
            ApplyMusicOutput();
        }

        public void SetSfxEnabled(bool enabled)
        {
            Init();
            SfxEnabled = enabled;
            PlayerDataManager.Instance.SetSfxEnabled(enabled);
        }

        public void PlayMusic(AudioCue cue, bool restart = false)
        {
            Init();
            if (!TryGetClip(cue, out var entry))
            {
                if (currentMusic != cue) musicSource.Stop();
                currentMusic = cue;
                return;
            }
            if (!restart && currentMusic == cue && musicSource.clip == entry.Clip)
            {
                if (!musicSource.isPlaying) musicSource.Play();
                return;
            }
            currentMusic = cue;
            currentMusicVolume = Mathf.Clamp01(entry.Volume);
            musicSource.clip = entry.Clip;
            musicSource.loop = true;
            ApplyMusicOutput();
            musicSource.Play();
        }

        public void PlaySfx(AudioCue cue)
        {
            Init();
            if (!SfxEnabled || !TryGetClip(cue, out var entry)) return;
            float now = Time.unscaledTime;
            if (lastPlayedAt.TryGetValue(cue, out float last) && now - last < entry.Cooldown) return;
            lastPlayedAt[cue] = now;
            sfxSource.PlayOneShot(entry.Clip, Mathf.Clamp01(entry.Volume));
        }

        public void SetMusicDucked(bool ducked)
        {
            Init();
            musicDucked = ducked;
            ApplyMusicOutput();
        }

        private void EnsureSources()
        {
            if (musicSource == null) musicSource = CreateSource("Music Audio");
            if (sfxSource == null) sfxSource = CreateSource("SFX Audio");
            ConfigureSource(musicSource);
            ConfigureSource(sfxSource);
            musicSource.loop = true;
        }

        private AudioSource CreateSource(string sourceName)
        {
            var child = transform.Find(sourceName);
            if (child == null)
            {
                child = new GameObject(sourceName).transform;
                child.SetParent(transform, false);
            }
            var source = child.GetComponent<AudioSource>();
            return source != null ? source : child.gameObject.AddComponent<AudioSource>();
        }

        private static void ConfigureSource(AudioSource source)
        {
            source.playOnAwake = false;
            source.spatialBlend = 0;
        }

        private void BuildLookup()
        {
            entries.Clear();
            if (config == null)
            {
                Debug.LogWarning("AudioManager: no AudioConfig assigned.", this);
                return;
            }
            foreach (var entry in config.Entries)
            {
                if (entry == null) continue;
                if (!entries.TryAdd(entry.Cue, entry))
                    Debug.LogWarning("AudioManager: duplicate cue " + entry.Cue + ". The first entry is used.", config);
            }
        }

        private bool TryGetClip(AudioCue cue, out AudioCueEntry entry)
        {
            if (!entries.TryGetValue(cue, out entry))
            {
                if (missingWarnings.Add(cue))
                    Debug.LogWarning("AudioManager: cue " + cue + " is missing from the config.", config);
                return false;
            }
            return entry.Clip != null;
        }

        private void ApplyMusicOutput()
        {
            if (musicSource == null) return;
            musicSource.mute = !MusicEnabled;
            musicSource.volume = currentMusicVolume * (musicDucked ? duckedVolume : 1);
        }
    }
}