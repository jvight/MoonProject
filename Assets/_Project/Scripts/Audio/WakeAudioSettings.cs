using System;
using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// How 07's wake sounds (docs/design/cutscenes.md §1.2): the radio's Asleep profile (quiet out-of-tune static with
    /// a ghost Rhodes chord surfacing on cue) on the fresh title and through the opening, and 07's own small wake
    /// cues, each keyed to a <see cref="MoonProject.Core.WakeBeat"/>. Every level is a share of the cue's own volume.
    /// </summary>
    [Serializable]
    public sealed class WakeAudioSettings
    {
        [Header("The Asleep radio (static)")]
        [Tooltip("Static level while the fresh title is up: quiet, out of tune.")]
        [Range(0f, 1f)]
        [SerializeField] private float _titleStatic = 0.35f;

        [Tooltip("Static level once the crackle swells in O1 and through the look inside the lens.")]
        [Range(0f, 1f)]
        [SerializeField] private float _openingStatic = 0.6f;

        [Tooltip("Seconds the static takes to come up on the title.")]
        [Range(0.1f, 6f)]
        [SerializeField] private float _titleFadeSeconds = 2.5f;

        [Tooltip("Seconds of the O1 crackle swell.")]
        [Range(0.1f, 4f)]
        [SerializeField] private float _swellSeconds = 1.2f;

        [Tooltip("Seconds the static takes to give way to the radio's own wake (O12, or the chase frame).")]
        [Range(0.2f, 6f)]
        [SerializeField] private float _handOverSeconds = 2f;

        [Tooltip("Low-pass (Hz) on the static while it is heard from inside 07.")]
        [Range(300f, 6000f)]
        [SerializeField] private float _insideCutoff = 1100f;

        [Tooltip("Low-pass (Hz) once the camera has left the lens: the static heard beside 07.")]
        [Range(1000f, 22000f)]
        [SerializeField] private float _besideCutoff = 5000f;

        [Tooltip("Stereo position of the static beside 07 (the radio's speaker side).")]
        [Range(-1f, 1f)]
        [SerializeField] private float _besidePan = 0.3f;

        [Tooltip("Seconds the static takes to move from inside 07 to beside it (O10).")]
        [Range(0.5f, 8f)]
        [SerializeField] private float _narrowSeconds = 4f;

        [Header("The ghost chord (D-A-E)")]
        [Tooltip("O1: seconds into the black before the ghost chord surfaces.")]
        [Range(0f, 3f)]
        [SerializeField] private float _crackleGhostDelay = 0.9f;

        [Tooltip("O7: seconds into the look at the pilot before it surfaces again, with the pilot's light.")]
        [Range(0f, 3f)]
        [SerializeField] private float _pilotGhostDelay = 0.8f;

        [Tooltip("O1's faint ghost (share of the cue's volume).")]
        [Range(0f, 1f)]
        [SerializeField] private float _ghostFaint = 0.4f;

        [Tooltip("O7's ghost, a hair clearer.")]
        [Range(0f, 1f)]
        [SerializeField] private float _ghostClearer = 0.6f;

        [Tooltip("O9's two bars, the longest and clearest.")]
        [Range(0f, 1f)]
        [SerializeField] private float _ghostLong = 0.85f;

        [Header("07's small wake cues")]
        [Tooltip("The felted power-on thump inside the black (O1).")]
        [Range(0f, 1f)]
        [SerializeField] private float _thump = 0.8f;

        [Tooltip("The lid's servo ticks and the focus landing.")]
        [Range(0f, 1f)]
        [SerializeField] private float _tick = 0.8f;

        [Tooltip("The lens motor's whine while it hunts.")]
        [Range(0f, 1f)]
        [SerializeField] private float _whine = 0.7f;

        [Tooltip("The lamp hum's dropout as 07 comes on.")]
        [Range(0f, 1f)]
        [SerializeField] private float _hum = 0.8f;

        [Tooltip("The old neck's servo as the gaze turns.")]
        [Range(0f, 1f)]
        [SerializeField] private float _servo = 0.6f;

        [Tooltip("The two-note 'hm?' with the first blink.")]
        [Range(0f, 1f)]
        [SerializeField] private float _chirp = 0.8f;

        [Tooltip("Seconds after the blink's lid click before the 'hm?'.")]
        [Range(0f, 1f)]
        [SerializeField] private float _chirpDelay = 0.2f;

        [Tooltip("The short wake's lid tick (Continue).")]
        [Range(0f, 1f)]
        [SerializeField] private float _stir = 0.5f;

        [Tooltip("Hurried by drive input, every wake cue plays this much softer, and the ghost chords are left out.")]
        [Range(0f, 1f)]
        [SerializeField] private float _hurried = 0.7f;

        public float TitleStatic => _titleStatic;
        public float OpeningStatic => _openingStatic;
        public float TitleFadeSeconds => _titleFadeSeconds;
        public float SwellSeconds => _swellSeconds;
        public float HandOverSeconds => _handOverSeconds;
        public float InsideCutoff => _insideCutoff;
        public float BesideCutoff => _besideCutoff;
        public float BesidePan => _besidePan;
        public float NarrowSeconds => _narrowSeconds;
        public float CrackleGhostDelay => _crackleGhostDelay;
        public float PilotGhostDelay => _pilotGhostDelay;
        public float GhostFaint => _ghostFaint;
        public float GhostClearer => _ghostClearer;
        public float GhostLong => _ghostLong;
        public float Thump => _thump;
        public float Tick => _tick;
        public float Whine => _whine;
        public float Hum => _hum;
        public float Servo => _servo;
        public float Chirp => _chirp;
        public float ChirpDelay => _chirpDelay;
        public float Stir => _stir;
        public float Hurried => _hurried;
    }
}
