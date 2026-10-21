using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// 07's face booting before its self-test lines (pure, EditMode-tested). Rover says when each part begins and how
    /// long it lasts (<see cref="PowerOn"/>, <see cref="LogoBegan"/>, <see cref="LoadBegan"/>); this works out what the
    /// face shows at each moment, as a machine draws it:
    /// - the tube's dot, line and blooming raster;
    /// - the maker's mark plotted by a vector beam at one constant speed, stroke after stroke with a short lift between
    ///   them (no easing), each stroke keeping a moment's afterglow;
    /// - the wordmark typed character by character at a steady rate behind a block cursor;
    /// - the finished mark held at full brightness with only a faint shimmer;
    /// - then the phosphor's afterglow: the image decays to a ghost before the raster clears;
    /// - the load line typed, and its bar jumping step by step with uneven dwells, like an old drive.
    /// It queues a cue as the beam begins each stroke, for each load step, for typed characters and for each line done
    /// (<see cref="TryTakeCue"/>). A part that ends before its time (the next part began, or the lines took over) first
    /// plays out its cues, so each fires exactly once.
    /// </summary>
    internal sealed class BootFace
    {
        public const int WordmarkLine = 0;
        public const int LoadLine = 1;

        /// <summary>The self-test's lines count on from here in the cues' line numbers.</summary>
        public const int FirstPostLine = 2;

        private const int CueCapacity = 64;

        // The beam keeps at least this share of the strokes' time moving, however long the lifts between strokes.
        private const float MinTravelShare = 0.5f;

        // The cursor's share of each blink lit.
        private const float CursorOnShare = 0.5f;

        private readonly BootFaceSettings _settings;
        private readonly BootFaceCue[] _cues = new BootFaceCue[CueCapacity];
        private int _cueHead;
        private int _cueCount;
        private BootFaceStage _part;
        private float _time;
        private float _length;
        private float _rush = 1f;
        private int _wordmarkLength;
        private int _loadLength;
        private int _strokesDrawn;
        private int _wordmarkTyped;
        private int _loadTyped;
        private int _stepsFilled;
        private readonly float[] _strokeStart;
        private readonly float[] _strokeLength;
        private readonly float _totalLength;

        /// <param name="strokeLengths">The mark's strokes' lengths, in drawing order: the beam's speed is constant.</param>
        public BootFace(BootFaceSettings settings, IReadOnlyList<float> strokeLengths)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (strokeLengths == null || strokeLengths.Count < 1)
            {
                throw new ArgumentException("The maker's mark needs a stroke.", nameof(strokeLengths));
            }

            _strokeLength = new float[strokeLengths.Count];
            _strokeStart = new float[strokeLengths.Count];
            for (int i = 0; i < strokeLengths.Count; i++)
            {
                if (strokeLengths[i] <= 0f)
                {
                    throw new ArgumentException($"Stroke {i} of the maker's mark has no length.", nameof(strokeLengths));
                }

                _strokeLength[i] = strokeLengths[i];
                _totalLength += strokeLengths[i];
            }
        }

        public int StrokeCount => _strokeLength.Length;

        /// <summary>True from the tube waking until the self-test's lines take the face (<see cref="Clear"/>).</summary>
        public bool IsShowing => _part != BootFaceStage.None;

        public BootFaceStage Part => _part;

        /// <summary>Seconds into the current part, on the boot's own clock (it runs faster when rushed).</summary>
        public float Clock => _time;

        /// <summary>The raster's width and height (0..1 of the face) and its glow (0..1) while the tube wakes.</summary>
        public float RasterWidth { get; private set; }

        public float RasterHeight { get; private set; }

        public float RasterGlow { get; private set; }

        /// <summary>How much of the mark shows (1, dimming to 0 at the end of its part).</summary>
        public float LogoAlpha { get; private set; }

        public int WordmarkVisible => _wordmarkTyped;

        /// <summary>The block cursor shows after the wordmark: steady while it types, blinking through the hold.</summary>
        public bool CursorVisible { get; private set; }

        public int LoadVisible => _loadTyped;

        /// <summary>Load-bar steps filled.</summary>
        public int StepsFilled => _stepsFilled;

        public int LoadSteps => _settings.LoadDwells.Count;

        /// <summary>The face's own texts, whose lengths the typing counts against (the current language's).</summary>
        public void SetTexts(int wordmarkLength, int loadLength)
        {
            _wordmarkLength = Math.Max(0, wordmarkLength);
            _loadLength = Math.Max(0, loadLength);
        }

        /// <summary>The tube wakes, for <paramref name="seconds"/>: a new boot from the start.</summary>
        public void PowerOn(float seconds)
        {
            _cueHead = 0;
            _cueCount = 0;
            _rush = 1f;
            _strokesDrawn = 0;
            _wordmarkTyped = 0;
            _loadTyped = 0;
            _stepsFilled = 0;
            LogoAlpha = 0f;
            CursorVisible = false;
            Begin(BootFaceStage.PowerOn, seconds);
        }

        public void LogoBegan(float seconds)
        {
            Begin(BootFaceStage.Logo, seconds);
        }

        public void LoadBegan(float seconds)
        {
            Begin(BootFaceStage.Load, seconds);
        }

        /// <summary>The boot was fast-forwarded: it runs <paramref name="scale"/> times faster until the next power-on.</summary>
        public void Rush(float scale)
        {
            _rush = Math.Max(1f, scale);
        }

        /// <summary>The self-test's lines take the face: the boot plays out its last cues and leaves.</summary>
        public void Clear()
        {
            PlayOut();
            _part = BootFaceStage.None;
            RasterWidth = 0f;
            RasterHeight = 0f;
            RasterGlow = 0f;
            LogoAlpha = 0f;
            CursorVisible = false;
        }

        public void Tick(float deltaTime)
        {
            if (_part == BootFaceStage.None)
            {
                return;
            }

            _time = Math.Min(_length, _time + Math.Max(deltaTime, 0f) * _rush);
            Evaluate();
        }

        /// <summary>
        /// How far (0..1) the beam has traced stroke <paramref name="stroke"/>: it moves at one constant speed, so a long
        /// stroke takes longer than a short one.
        /// </summary>
        public float StrokeProgress(int stroke)
        {
            if (_part == BootFaceStage.Load || _part == BootFaceStage.None)
            {
                return 1f;
            }

            if (_part != BootFaceStage.Logo)
            {
                return 0f;
            }

            return Mathf.Clamp01((_time - _strokeStart[stroke]) * BeamSpeed / _strokeLength[stroke]);
        }

        /// <summary>How much brighter (0..1) stroke <paramref name="stroke"/> glows just after it was traced.</summary>
        public float StrokeGlow(int stroke)
        {
            if (_part != BootFaceStage.Logo || StrokeProgress(stroke) < 1f)
            {
                return 0f;
            }

            float end = _strokeStart[stroke] + _strokeLength[stroke] / BeamSpeed;
            float since = (_time - end) / _rush;
            return _settings.AfterglowBoost * Mathf.Clamp01(1f - since / _settings.AfterglowSeconds);
        }

        /// <summary>Takes the oldest cue not yet taken.</summary>
        public bool TryTakeCue(out BootFaceCue cue)
        {
            if (_cueCount == 0)
            {
                cue = default;
                return false;
            }

            cue = _cues[_cueHead];
            _cueHead = (_cueHead + 1) % CueCapacity;
            _cueCount--;
            return true;
        }

        private float StrokesSeconds => _length * _settings.StrokesShare;

        /// <summary>
        /// The lift between strokes: the tuned seconds, unless the lifts would take more than their share of the
        /// strokes' time (a short logo), when they shrink so the beam still moves for most of it.
        /// </summary>
        private float Lift => StrokeCount > 1
            ? Mathf.Min(_settings.StrokeLiftSeconds, StrokesSeconds * (1f - MinTravelShare) / (StrokeCount - 1))
            : 0f;

        /// <summary>The beam's speed (stroke length a second): the strokes and the lifts between them fill their share.</summary>
        private float BeamSpeed => _totalLength / (StrokesSeconds - (StrokeCount - 1) * Lift);

        /// <summary>Where each stroke begins on the logo's clock, at the beam's speed and with a lift before each.</summary>
        private void PlanStrokes()
        {
            float lift = Lift;
            float speed = BeamSpeed;
            float at = 0f;
            for (int i = 0; i < StrokeCount; i++)
            {
                _strokeStart[i] = at;
                at += _strokeLength[i] / speed + lift;
            }
        }

        private void Begin(BootFaceStage part, float seconds)
        {
            PlayOut();
            _part = part;
            _length = Math.Max(seconds, 1e-4f);
            _time = 0f;
            if (part == BootFaceStage.Logo)
            {
                PlanStrokes();
            }

            Evaluate();
        }

        /// <summary>Plays the current part to its end, so every cue it owes fires before the next part.</summary>
        private void PlayOut()
        {
            if (_part == BootFaceStage.None)
            {
                return;
            }

            _time = _length;
            Evaluate();
        }

        private void Evaluate()
        {
            float u = Mathf.Clamp01(_time / _length);
            switch (_part)
            {
                case BootFaceStage.PowerOn:
                    EvaluatePowerOn(u);
                    break;
                case BootFaceStage.Logo:
                    RasterGlow = 0f;
                    EvaluateLogo(u);
                    break;
                case BootFaceStage.Load:
                    RasterGlow = 0f;
                    LogoAlpha = 0f;
                    EvaluateLoad(u);
                    break;
            }
        }

        private void EvaluatePowerOn(float u)
        {
            float line = _settings.LineShare;
            float open = line + _settings.OpenShare;
            RasterWidth = Ease(0f, line, u);
            RasterHeight = Ease(line, open, u);
            RasterGlow = u < line ? 1f
                : u < open ? Mathf.Lerp(1f, _settings.BloomPeak, Mathf.Sqrt((u - line) / (open - line)))
                : _settings.BloomPeak * (1f - Ease(open, 1f, u));
        }

        private void EvaluateLogo(float u)
        {
            // Each stroke's cue as the beam begins it, so its hum starts with the line.
            bool done = u >= 1f;
            while (_strokesDrawn < StrokeCount && (done || _time + 1e-5f >= _strokeStart[_strokesDrawn]))
            {
                Queue(BootFaceCueKind.StrokeDrawn, _strokesDrawn, 0);
                _strokesDrawn++;
            }

            float typeFrom = StrokesSeconds + _settings.TypeGapSeconds;
            float dimFrom = _length * (1f - _settings.DimShare);
            float typed = _wordmarkLength > 0
                ? (_time - typeFrom) * _settings.TypeCharsPerSecond / _wordmarkLength
                : 1f;
            Type(WordmarkLine, ref _wordmarkTyped, _wordmarkLength, done ? 1f : Mathf.Clamp01(typed));
            bool typing = _time >= typeFrom && _wordmarkTyped < _wordmarkLength;
            float blink = _time * _settings.CursorBlinkHertz;
            CursorVisible = !done && _time >= typeFrom && _time < dimFrom &&
                            (typing || blink - Mathf.Floor(blink) < CursorOnShare);
            if (_time < dimFrom)
            {
                bool holding = _wordmarkTyped >= _wordmarkLength;
                float shimmer = Mathf.Sin(_time * _settings.ShimmerHertz * 2f * Mathf.PI) * 0.5f + 0.5f;
                LogoAlpha = holding ? 1f - _settings.ShimmerDepth * shimmer : 1f;
                return;
            }

            LogoAlpha = Afterglow(Share(_time, dimFrom, _length));
        }

        /// <summary>
        /// The phosphor's afterglow through the dim (<paramref name="v"/> 0..1): the image decays fast, then lingers as a
        /// ghost, and only then does the raster clear, with no hard cut.
        /// </summary>
        private float Afterglow(float v)
        {
            float clear = _settings.GhostClearShare;
            float ghost = _settings.GhostLevel;
            if (v < 1f - clear)
            {
                float x = v / (1f - clear);
                float sharp = _settings.DecaySharpness;
                float floor = Mathf.Exp(-sharp);
                return ghost + (1f - ghost) * (Mathf.Exp(-sharp * x) - floor) / (1f - floor);
            }

            return ghost * (1f - Ease(1f - clear, 1f, v));
        }

        private void EvaluateLoad(float u)
        {
            float textShare = _settings.LoadTextShare;
            Type(LoadLine, ref _loadTyped, _loadLength, Share(u, 0f, textShare));
            float bar = Share(u, textShare, 1f);
            IReadOnlyList<float> dwells = _settings.LoadDwells;
            float total = 0f;
            for (int step = 0; step < dwells.Count; step++)
            {
                total += dwells[step];
            }

            // The bar jumps a whole step at the end of each dwell, the dwells uneven like an old drive's seeks.
            float end = 0f;
            int filled = 0;
            for (int step = 0; step < dwells.Count; step++)
            {
                end += dwells[step];
                if (bar * total + 1e-4f >= end)
                {
                    filled = step + 1;
                }
            }

            while (_stepsFilled < filled)
            {
                Queue(BootFaceCueKind.LoadStep, _stepsFilled, 0);
                _stepsFilled++;
            }
        }

        /// <summary>Types <paramref name="line"/> up to <paramref name="share"/> of its length, cueing as it goes.</summary>
        private void Type(int line, ref int typed, int length, float share)
        {
            int target = Math.Min(length, (int)(share * length + 1e-4f));
            int every = _settings.TypeCueEvery;
            while (typed < target)
            {
                typed++;
                if (typed % every == 0)
                {
                    Queue(BootFaceCueKind.Typed, line, typed - 1);
                }

                if (typed == length)
                {
                    Queue(BootFaceCueKind.LineCompleted, line, 0);
                }
            }
        }

        private static float Ease(float from, float to, float u)
        {
            return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(from, to, u));
        }

        private static float Share(float u, float from, float to)
        {
            return to > from ? Mathf.Clamp01((u - from) / (to - from)) : 1f;
        }

        private void Queue(BootFaceCueKind kind, int a, int b)
        {
            if (_cueCount == CueCapacity)
            {
                _cueHead = (_cueHead + 1) % CueCapacity;
                _cueCount--;
            }

            _cues[(_cueHead + _cueCount) % CueCapacity] = new BootFaceCue(kind, a, b);
            _cueCount++;
        }
    }
}
