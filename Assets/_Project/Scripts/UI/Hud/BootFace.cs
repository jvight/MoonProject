using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// 07's face booting before its self-test lines (pure, EditMode-tested). Rover says when each part begins and how
    /// long it lasts (<see cref="PowerOn"/>, <see cref="LogoBegan"/>, <see cref="LoadBegan"/>); this works out what the
    /// face shows at each moment: the tube's dot, line and blooming raster; the maker's mark traced stroke by stroke
    /// with each stroke's afterglow, its wordmark typing under it, the hold and the dim; the load line typing and its
    /// bar filling step by step, lingering on one. It queues a cue for each stroke traced, each load step, every few
    /// characters typed and each line done (<see cref="TryTakeCue"/>). A part that ends before its time (the next part
    /// began, or the lines took over) first plays out its cues, so each fires exactly once.
    /// </summary>
    internal sealed class BootFace
    {
        public const int WordmarkLine = 0;
        public const int LoadLine = 1;

        /// <summary>The self-test's lines count on from here in the cues' line numbers.</summary>
        public const int FirstPostLine = 2;

        private const int CueCapacity = 64;

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

        public BootFace(BootFaceSettings settings, int strokeCount)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            if (strokeCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(strokeCount), "The maker's mark needs a stroke.");
            }

            StrokeCount = strokeCount;
        }

        public int StrokeCount { get; }

        /// <summary>True from the tube waking until the self-test's lines take the face (<see cref="Clear"/>).</summary>
        public bool IsShowing => _part != BootFaceStage.None;

        public BootFaceStage Part => _part;

        /// <summary>The raster's width and height (0..1 of the face) and its glow (0..1) while the tube wakes.</summary>
        public float RasterWidth { get; private set; }

        public float RasterHeight { get; private set; }

        public float RasterGlow { get; private set; }

        /// <summary>How much of the mark shows (1, dimming to 0 at the end of its part).</summary>
        public float LogoAlpha { get; private set; }

        public int WordmarkVisible => _wordmarkTyped;

        public int LoadVisible => _loadTyped;

        /// <summary>Load-bar steps filled.</summary>
        public int StepsFilled => _stepsFilled;

        public int LoadSteps => _settings.LoadSteps;

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

        /// <summary>How far (0..1) the beam has traced stroke <paramref name="stroke"/>.</summary>
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

            float each = StrokeSeconds;
            return each > 1e-5f ? Mathf.Clamp01((_time - stroke * each) / each) : 1f;
        }

        /// <summary>How much brighter (0..1) stroke <paramref name="stroke"/> glows just after it was traced.</summary>
        public float StrokeGlow(int stroke)
        {
            if (_part != BootFaceStage.Logo || StrokeProgress(stroke) < 1f)
            {
                return 0f;
            }

            float since = (_time - (stroke + 1) * StrokeSeconds) / _rush;
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

        private float StrokeSeconds => _length * _settings.StrokesShare / StrokeCount;

        private void Begin(BootFaceStage part, float seconds)
        {
            PlayOut();
            _part = part;
            _length = Math.Max(seconds, 1e-4f);
            _time = 0f;
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
            int drawn = Math.Min(StrokeCount, (int)(u * StrokeCount / _settings.StrokesShare + 1e-4f));
            while (_strokesDrawn < drawn)
            {
                Queue(BootFaceCueKind.StrokeDrawn, _strokesDrawn, 0);
                _strokesDrawn++;
            }

            float typeFrom = _settings.StrokesShare;
            float typeTo = typeFrom + _settings.WordmarkShare;
            float dimFrom = typeTo + _settings.HoldShare;
            Type(WordmarkLine, ref _wordmarkTyped, _wordmarkLength, Share(u, typeFrom, typeTo));
            LogoAlpha = 1f - Ease(dimFrom, 1f, u);
        }

        private void EvaluateLoad(float u)
        {
            float textShare = _settings.LoadTextShare;
            Type(LoadLine, ref _loadTyped, _loadLength, Share(u, 0f, textShare));
            float bar = Share(u, textShare, 1f);
            float total = _settings.LoadSteps - 1 + _settings.LoadPauseWeight;
            float end = 0f;
            int filled = 0;
            for (int step = 0; step < _settings.LoadSteps; step++)
            {
                end += step == _settings.LoadPauseStep ? _settings.LoadPauseWeight : 1f;
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
