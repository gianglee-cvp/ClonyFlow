using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColonyFlow.Gameplay
{
    public enum AudioCue
    {
        MenuMusic,
        GameplayMusic,
        ButtonClick,
        BoxSelected,
        AntPickup,
        Booster,
        Win,
        Lose
    }

    [Serializable]
    public sealed class AudioCueEntry
    {
        [SerializeField] private AudioCue cue;
        [SerializeField] private AudioClip clip;
        [SerializeField, Range(0, 1)] private float volume = 1;
        [SerializeField, Min(0)] private float cooldown;

        public AudioCueEntry(AudioCue cue, float volume = 1, float cooldown = 0)
        {
            this.cue = cue;
            this.volume = volume;
            this.cooldown = cooldown;
        }

        public AudioCue Cue => cue;
        public AudioClip Clip => clip;
        public float Volume => volume;
        public float Cooldown => cooldown;
    }

    [CreateAssetMenu(fileName = "DefaultAudioConfig", menuName = "ColonyFlow/Audio Config")]
    public sealed class AudioConfig : ScriptableObject
    {
        [SerializeField] private List<AudioCueEntry> entries = new List<AudioCueEntry>
        {
            new AudioCueEntry(AudioCue.MenuMusic, .65f),
            new AudioCueEntry(AudioCue.GameplayMusic, .65f),
            new AudioCueEntry(AudioCue.ButtonClick, .8f),
            new AudioCueEntry(AudioCue.BoxSelected, .9f, .05f),
            new AudioCueEntry(AudioCue.AntPickup, .75f, .08f),
            new AudioCueEntry(AudioCue.Booster, .9f, .1f),
            new AudioCueEntry(AudioCue.Win, 1, .25f),
            new AudioCueEntry(AudioCue.Lose, 1, .25f)
        };
        public IReadOnlyList<AudioCueEntry> Entries => entries;
    }
}