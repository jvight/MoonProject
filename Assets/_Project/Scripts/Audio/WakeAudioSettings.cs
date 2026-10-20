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
        [Tooltip("Static level once the crackle swells in the black, until the self-test's checks begin.")]
        [Range(0f, 1f)]
        [SerializeField] private float _openingStatic = 0.6f;

        [Tooltip("Seconds of the crackle's swell in the black.")]
        [Range(0.1f, 4f)]
        [SerializeField] private float _swellSeconds = 1.2f;

        [Tooltip("Seconds the static takes to give way as the checks begin (or the chase frame, or the short wake).")]
        [Range(0.2f, 6f)]
        [SerializeField] private float _handOverSeconds = 2f;

        [Tooltip("Low-pass (Hz) on the static: heard muffled, from inside 07.")]
        [Range(300f, 6000f)]
        [SerializeField] private float _insideCutoff = 1100f;

        [Header("The ghost chord (D-A-E)")]
        [Tooltip("Seconds into the black before the ghost chord surfaces.")]
        [Range(0f, 3f)]
        [SerializeField] private float _crackleGhostDelay = 0.9f;

        [Tooltip("The ghost chord's level (share of the cue's volume): faint, inside the static.")]
        [Range(0f, 1f)]
        [SerializeField] private float _ghostFaint = 0.4f;

        [Header("07's small wake cues")]
        [Tooltip("The felted power-on thump inside the black.")]
        [Range(0f, 1f)]
        [SerializeField] private float _thump = 0.8f;

        [Tooltip("The lid's servo ticks as the picture opens and as the eyes open.")]
        [Range(0f, 1f)]
        [SerializeField] private float _tick = 0.8f;

        [Tooltip("The short wake's lid tick (Continue).")]
        [Range(0f, 1f)]
        [SerializeField] private float _stir = 0.5f;

        [Header("The self-test (New Game)")]
        [Tooltip("Each check's motion sound (the iris, the lamps, the head, the wheels...), from 07.")]
        [Range(0f, 1f)]
        [SerializeField] private float _check = 0.8f;

        [Tooltip("A check's fault result (the cutter): 07's short strain-and-choke.")]
        [Range(0f, 1f)]
        [SerializeField] private float _fault = 0.8f;

        [Tooltip("The rush when the player fast-forwards the self-test: one soft tape wind and relay chatter, in place " +
                 "of the remaining motions.")]
        [Range(0f, 1f)]
        [SerializeField] private float _rush = 0.7f;

        [Header("07's face booting")]
        [Tooltip("The tube powering on: the degauss thunk and the high-voltage whine.")]
        [Range(0f, 1f)]
        [SerializeField] private float _facePowerOn = 0.8f;

        [Tooltip("Each stroke of the maker's mark: the beam's faint hum on its note.")]
        [Range(0f, 1f)]
        [SerializeField] private float _faceStroke = 0.7f;

        [Tooltip("How many strokes share one note as the mark climbs the pentatonic (11 strokes over 6 notes).")]
        [Range(1, 4)]
        [SerializeField] private int _strokesPerNote = 2;

        [Tooltip("The mark whole (its wordmark typed out): the resolve on D.")]
        [Range(0f, 1f)]
        [SerializeField] private float _faceResolve = 0.7f;

        [Tooltip("Each typed character's counter tick (per character since the plotter face: kept under the mix).")]
        [Range(0f, 1f)]
        [SerializeField] private float _faceText = 0.45f;

        [Tooltip("A typed line settling: the heavier tick.")]
        [Range(0f, 1f)]
        [SerializeField] private float _faceLine = 0.7f;

        [Tooltip("Each step of the load bar; the fifth resolves.")]
        [Range(0f, 1f)]
        [SerializeField] private float _faceLoad = 0.75f;

        [Tooltip("Text ticks closer together than this (seconds) are skipped.")]
        [Range(0f, 0.2f)]
        [SerializeField] private float _textMinInterval = 0.045f;

        [Tooltip("Text ticks within this many seconds of the last count as a run and soften.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _textStreakWindow = 0.2f;

        [Tooltip("Volume multiplier per text tick within a run.")]
        [Range(0.5f, 1f)]
        [SerializeField] private float _textStreakDecay = 0.95f;

        [Tooltip("Softest a run of text ticks gets (volume scale), so a 7 s readout stays quiet but present.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _textStreakFloor = 0.7f;

        [Tooltip("Stroke hums within this many seconds of the last count as a run and soften (none is ever skipped).")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _strokeStreakWindow = 0.3f;

        [Tooltip("Volume multiplier per stroke hum within a run: the plotter's quick strokes overlap their hums.")]
        [Range(0.5f, 1f)]
        [SerializeField] private float _strokeStreakDecay = 0.85f;

        [Tooltip("Softest a run of stroke hums gets (volume scale), so the climb stays audible without stacking up.")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _strokeStreakFloor = 0.45f;

        public float OpeningStatic => _openingStatic;
        public float SwellSeconds => _swellSeconds;
        public float HandOverSeconds => _handOverSeconds;
        public float InsideCutoff => _insideCutoff;
        public float CrackleGhostDelay => _crackleGhostDelay;
        public float GhostFaint => _ghostFaint;
        public float Thump => _thump;
        public float Tick => _tick;
        public float Stir => _stir;
        public float Check => _check;
        public float Fault => _fault;
        public float Rush => _rush;
        public float FacePowerOn => _facePowerOn;
        public float FaceStroke => _faceStroke;
        public int StrokesPerNote => _strokesPerNote;
        public float FaceResolve => _faceResolve;
        public float FaceText => _faceText;
        public float FaceLine => _faceLine;
        public float FaceLoad => _faceLoad;
        public float TextMinInterval => _textMinInterval;
        public float TextStreakWindow => _textStreakWindow;
        public float TextStreakDecay => _textStreakDecay;
        public float TextStreakFloor => _textStreakFloor;
        public float StrokeStreakWindow => _strokeStreakWindow;
        public float StrokeStreakDecay => _strokeStreakDecay;
        public float StrokeStreakFloor => _strokeStreakFloor;
    }
}
