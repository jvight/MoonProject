using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// 07's face booting as a machine draws it: the tube's dot, line and bloom; the maker's mark plotted by a beam at
    /// one constant speed with lifts between strokes, its wordmark typed at a steady rate behind a block cursor, the
    /// held mark, then the phosphor's afterglow decaying to a ghost before it clears; the load bar's uneven steps; every
    /// cue once, in order, even when a part is cut short or fast-forwarded.
    /// </summary>
    public sealed class BootFaceTests
    {
        private const float Frame = 1f / 60f;
        private const int WordmarkLength = 26;
        private const int LoadLength = 21;
        private const float PowerOnSeconds = 1.2f;
        private const float LogoSeconds = 5f;
        private const float LoadSeconds = 2.4f;

        // Eleven strokes, one three times as long as the rest (the beam's speed is what is constant).
        private static readonly float[] Lengths = { 1f, 1f, 3f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f };

        private BootFaceSettings _settings;
        private BootFace _face;
        private readonly List<BootFaceCue> _cues = new List<BootFaceCue>();

        [SetUp]
        public void SetUp()
        {
            _settings = new BootFaceSettings();
            _face = new BootFace(_settings, Lengths);
            _face.SetTexts(WordmarkLength, LoadLength);
            _cues.Clear();
        }

        [Test]
        public void TheTube_GrowsADotIntoALine_OpensToTheRaster_BloomsThenSettlesDark()
        {
            _face.PowerOn(PowerOnSeconds);
            Assert.AreEqual(BootFaceStage.PowerOn, _face.Part);
            Assert.AreEqual(0f, _face.RasterWidth, 1e-4f, "a dot at first");
            Run(PowerOnSeconds * _settings.LineShare);
            Assert.AreEqual(1f, _face.RasterWidth, 0.05f, "the dot has become a line");
            Assert.Less(_face.RasterHeight, 0.05f, "still a line");
            Run(PowerOnSeconds * _settings.OpenShare);
            Assert.AreEqual(1f, _face.RasterHeight, 0.05f, "the full raster");
            Assert.AreEqual(_settings.BloomPeak, _face.RasterGlow, 0.1f, "at its bloom");
            Run(PowerOnSeconds);
            Assert.AreEqual(0f, _face.RasterGlow, 1e-3f, "then dark for the mark");
        }

        [Test]
        public void TheBeam_PlotsAtOneSpeed_ALongStrokeTakingLonger_WithALiftBetweenStrokes_AndCuesEachAsItBegins()
        {
            BeginLogo();
            var began = new float[Lengths.Length];
            var ended = new float[Lengths.Length];
            for (int i = 0; i < Lengths.Length; i++)
            {
                began[i] = -1f;
                ended[i] = -1f;
            }

            float t = 0f;
            while (t < LogoSeconds * _settings.StrokesShare + 0.2f)
            {
                Step();
                t += Frame;
                for (int i = 0; i < Lengths.Length; i++)
                {
                    float progress = _face.StrokeProgress(i);
                    if (began[i] < 0f && progress > 0f)
                    {
                        began[i] = t;
                    }

                    if (ended[i] < 0f && progress >= 1f)
                    {
                        ended[i] = t;
                    }
                }
            }

            float plain = ended[1] - began[1];
            float longer = ended[2] - began[2];
            Assert.AreEqual(3f, longer / plain, 0.35f, "a stroke three times as long takes three times as long");
            Assert.Greater(began[1] - ended[0], Frame, "the beam lifts between strokes");
            Assert.LessOrEqual(ended[Lengths.Length - 1], LogoSeconds * _settings.StrokesShare + 2f * Frame,
                "the strokes keep to their share");
            CollectionAssert.AreEqual(Sequence(Lengths.Length), Of(BootFaceCueKind.StrokeDrawn), "each once, in order");
        }

        [Test]
        public void AStrokesCue_ComesAsTheBeamBeginsIt_NotAsItEnds()
        {
            BeginLogo();
            CollectionAssert.AreEqual(new[] { 0 }, Of(BootFaceCueKind.StrokeDrawn), "the first, the moment it begins");
            while (_face.StrokeProgress(1) <= 0f)
            {
                Step();
            }

            Assert.Less(_face.StrokeProgress(2), 1f);
            CollectionAssert.AreEqual(new[] { 0, 1 }, Of(BootFaceCueKind.StrokeDrawn), "the second as it starts");
        }

        [Test]
        public void TheWordmark_TypesAtItsRate_BehindABlockCursor_ThenTheMarkHoldsAndDecaysToAGhost()
        {
            BeginLogo();
            float typeFrom = LogoSeconds * _settings.StrokesShare + _settings.TypeGapSeconds;
            Run(typeFrom - Frame);
            Assert.AreEqual(0, _face.WordmarkVisible, "nothing types while the beam plots");
            Assert.IsFalse(_face.CursorVisible);

            Run(1f + Frame);
            Assert.AreEqual(_settings.TypeCharsPerSecond, _face.WordmarkVisible, 1.5f, "a steady rate, char by char");
            Assert.IsTrue(_face.CursorVisible, "the block cursor rides the typing");
            Assert.AreEqual(1f, _face.LogoAlpha, 1e-4f, "full brightness while it types");

            float typeEnd = typeFrom + WordmarkLength / _settings.TypeCharsPerSecond;
            float dimFrom = LogoSeconds * (1f - _settings.DimShare);
            Assert.GreaterOrEqual(dimFrom - typeEnd, 1.2f, "the finished mark holds long enough to read");
            Run(typeEnd - typeFrom - 1f + 0.1f);
            Assert.AreEqual(WordmarkLength, _face.WordmarkVisible);
            float lowest = 1f;
            bool blinkedOff = false;
            while (_face.Clock < dimFrom - Frame)
            {
                Step();
                lowest = Mathf.Min(lowest, _face.LogoAlpha);
                blinkedOff |= !_face.CursorVisible;
            }

            Assert.GreaterOrEqual(lowest, 1f - _settings.ShimmerDepth - 1e-4f, "only a faint shimmer in the hold");
            Assert.Less(lowest, 1f, "but a shimmer");
            Assert.IsTrue(blinkedOff, "the cursor blinks through the hold");

            float last = 1f;
            bool ghosted = false;
            while (_face.Clock < LogoSeconds - Frame)
            {
                Step();
                Assert.LessOrEqual(_face.LogoAlpha, last + 1e-4f, "the afterglow only fades");
                Assert.Less(last - _face.LogoAlpha, 0.25f, "never a hard cut");
                ghosted |= Mathf.Abs(_face.LogoAlpha - _settings.GhostLevel) < 0.03f;
                last = _face.LogoAlpha;
                Assert.IsFalse(_face.CursorVisible, "the cursor leaves with the dim");
            }

            Assert.IsTrue(ghosted, "it lingers as a ghost before the raster clears");
            Step();
            Assert.AreEqual(0f, _face.LogoAlpha, 1e-3f, "then clears");

            Assert.AreEqual(WordmarkLength / _settings.TypeCueEvery, Of(BootFaceCueKind.Typed, BootFace.WordmarkLine).Count,
                "a typing cue per character");
            CollectionAssert.AreEqual(new[] { BootFace.WordmarkLine }, Of(BootFaceCueKind.LineCompleted));
        }

        [Test]
        public void TheLoad_TypesItsLine_ThenJumpsItsStepsWithUnevenDwells()
        {
            _face.PowerOn(PowerOnSeconds);
            _face.LogoBegan(LogoSeconds);
            _face.LoadBegan(LoadSeconds);
            Drain();
            _cues.Clear();
            var filledAt = new List<float>();
            int filled = 0;
            float t = 0f;
            while (t < LoadSeconds + Frame)
            {
                Step();
                t += Frame;
                if (_face.StepsFilled > filled)
                {
                    Assert.AreEqual(filled + 1, _face.StepsFilled, "one whole step at a time");
                    filled = _face.StepsFilled;
                    filledAt.Add(t);
                }
            }

            Assert.AreEqual(LoadLength, _face.LoadVisible);
            CollectionAssert.AreEqual(Sequence(_face.LoadSteps), Of(BootFaceCueKind.LoadStep), "a tick each jump");
            CollectionAssert.AreEqual(new[] { BootFace.LoadLine }, Of(BootFaceCueKind.LineCompleted));
            float quick = filledAt[1] - filledAt[0];
            float seek = filledAt[2] - filledAt[1];
            Assert.Greater(seek, quick * 3f, "an old drive seeks long on one step and skips quickly over another");
        }

        [Test]
        public void APartCutShort_PlaysOutItsCuesFirst_SoEachFiresExactlyOnce()
        {
            _face.PowerOn(PowerOnSeconds);
            _face.LogoBegan(LogoSeconds);
            Run(0.1f);
            _face.LoadBegan(LoadSeconds);
            Run(0.1f);
            _face.Clear();
            Drain();
            CollectionAssert.AreEqual(Sequence(Lengths.Length), Of(BootFaceCueKind.StrokeDrawn));
            CollectionAssert.AreEqual(Sequence(_face.LoadSteps), Of(BootFaceCueKind.LoadStep));
            CollectionAssert.AreEqual(new[] { BootFace.WordmarkLine, BootFace.LoadLine },
                Of(BootFaceCueKind.LineCompleted));
            Assert.IsFalse(_face.IsShowing, "the lines have the face");
        }

        [Test]
        public void AWholeBoot_CuesEachStrokeAndStepOnce_AndAFastForwardRunsItThatMuchFaster()
        {
            _face.PowerOn(PowerOnSeconds);
            Run(PowerOnSeconds + Frame);
            _face.LogoBegan(LogoSeconds);
            Run(LogoSeconds + Frame);
            _face.LoadBegan(LoadSeconds);
            Run(LoadSeconds + Frame);
            _face.Clear();
            Drain();
            Assert.AreEqual(Lengths.Length, Of(BootFaceCueKind.StrokeDrawn).Count);
            Assert.AreEqual(_face.LoadSteps, Of(BootFaceCueKind.LoadStep).Count);

            _cues.Clear();
            const float Scale = 6f;
            _face.PowerOn(PowerOnSeconds);
            _face.LogoBegan(LogoSeconds);
            _face.Rush(Scale);
            Run(LogoSeconds / Scale + 2f * Frame);
            Assert.AreEqual(Lengths.Length, Of(BootFaceCueKind.StrokeDrawn).Count, "the whole mark, six times faster");
            Assert.AreEqual(0f, _face.LogoAlpha, 1e-3f);
        }

        private void BeginLogo()
        {
            _face.PowerOn(PowerOnSeconds);
            Run(PowerOnSeconds);
            _cues.Clear();
            _face.LogoBegan(LogoSeconds);
            Drain();
        }

        private void Run(float seconds)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                Step();
            }
        }

        private void Step()
        {
            _face.Tick(Frame);
            Drain();
        }

        private void Drain()
        {
            while (_face.TryTakeCue(out BootFaceCue cue))
            {
                _cues.Add(cue);
            }
        }

        private List<int> Of(BootFaceCueKind kind, int line = -1)
        {
            var found = new List<int>();
            foreach (BootFaceCue cue in _cues)
            {
                if (cue.Kind == kind && (line < 0 || cue.A == line))
                {
                    found.Add(kind == BootFaceCueKind.Typed ? cue.B : cue.A);
                }
            }

            return found;
        }

        private static List<int> Sequence(int count)
        {
            var list = new List<int>();
            for (int i = 0; i < count; i++)
            {
                list.Add(i);
            }

            return list;
        }
    }
}
