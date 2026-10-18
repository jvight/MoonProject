using System;
using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// How 07's wake sounds (docs/design/cutscenes.md §1.2): the radio's Asleep profile (quiet out-of-tune static with
    /// a ghost Rhodes chord surfacing on cue) on the fresh title and through the opening, 07's own small wake cues,
    /// each keyed to a <see cref="MoonProject.Core.WakeBeat"/>, and the New Game self-test's checks. Every level is a
    /// share of the cue's own volume.
    /// </summary>
    [Serializable]
    public sealed class WakeAudioSettings
    {
        [Header("The Asleep radio (static)")]
        [Tooltip("Static level once the crackle swells in O1 and through the look inside the lens.")]
        [Range(0f, 1f)]
        [SerializeField] private float _openingStatic = 0.6f;

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

        [Tooltip("The ghost chord's level (share of the cue's volume): faint, inside the static.")]
        [Range(0f, 1f)]
        [SerializeField] private float _ghostFaint = 0.4f;

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

        [Tooltip("The short wake's lid tick (Continue).")]
        [Range(0f, 1f)]
        [SerializeField] private float _stir = 0.5f;

        [Tooltip("Hurried by drive input, every wake cue plays this much softer, and the ghost chord is left out.")]
        [Range(0f, 1f)]
        [SerializeField] private float _hurried = 0.7f;

        [Header("The self-test (New Game)")]
        [Tooltip("Each check's motion sound (the iris, the lamps, the head, the wheels...), from 07.")]
        [Range(0f, 1f)]
        [SerializeField] private float _check = 0.8f;

        [Tooltip("A check's fault result (the cutter): 07's short strain-and-choke.")]
        [Range(0f, 1f)]
        [SerializeField] private float _fault = 0.8f;

        public float OpeningStatic => _openingStatic;
        public float SwellSeconds => _swellSeconds;
        public float HandOverSeconds => _handOverSeconds;
        public float InsideCutoff => _insideCutoff;
        public float BesideCutoff => _besideCutoff;
        public float BesidePan => _besidePan;
        public float NarrowSeconds => _narrowSeconds;
        public float CrackleGhostDelay => _crackleGhostDelay;
        public float GhostFaint => _ghostFaint;
        public float Thump => _thump;
        public float Tick => _tick;
        public float Whine => _whine;
        public float Stir => _stir;
        public float Hurried => _hurried;
        public float Check => _check;
        public float Fault => _fault;
    }
}
