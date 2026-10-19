using System.Collections.Generic;
using NUnit.Framework;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// 07's face booting: the tube's dot, line and bloom; the maker's mark traced stroke by stroke with its wordmark,
    /// hold and dim; the load bar; every cue once, in order, even when a part is cut short or fast-forwarded.
    /// </summary>
    public sealed class BootFaceTests
    {
        private const float Frame = 1f / 60f;
        private const int Strokes = 11;
        private const int WordmarkLength = 26;
        private const int LoadLength = 21;
        private const float PowerOnSeconds = 0.6f;
        private const float LogoSeconds = 2f;
        private const float LoadSeconds = 0.8f;

        private BootFaceSettings _settings;
        private BootFace _face;
        private readonly List<BootFaceCue> _cues = new List<BootFaceCue>();

        [SetUp]
        public void SetUp()
        {
            _settings = new BootFaceSettings();
            _face = new BootFace(_settings, Strokes);
            _face.SetTexts(WordmarkLength, LoadLength);
            _cues.Clear();
        }

        private void Run(float seconds)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                _face.Tick(Frame);
                Drain();
            }
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

        private void RunWholeBoot()
        {
            _face.PowerOn(PowerOnSeconds);
            Run(PowerOnSeconds + Frame);
            _face.LogoBegan(LogoSeconds);
            Run(LogoSeconds + Frame);
            _face.LoadBegan(LoadSeconds);
            Run(LoadSeconds + Frame);
            _face.Clear();
            Drain();
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
        public void TheMark_TracesEveryStrokeOnceInOrder_TypesItsWordmark_HoldsThenDims()
        {
            _face.PowerOn(PowerOnSeconds);
            Run(PowerOnSeconds);
            _face.LogoBegan(LogoSeconds);
            Assert.AreEqual(0f, _face.StrokeProgress(0), 1e-4f, "the beam starts at the first stroke");
            Run(LogoSeconds * _settings.StrokesShare * 0.5f);
            Assert.Greater(_face.StrokeProgress(Strokes / 2 - 1), 0f, "halfway through the strokes");
            Assert.AreEqual(0f, _face.StrokeProgress(Strokes - 1), 1e-4f, "the last is still to come");
            Assert.Greater(_face.StrokeGlow(Strokes / 2 - 1), 0f, "a stroke just traced still glows");
            Run(LogoSeconds * (_settings.StrokesShare * 0.5f + _settings.WordmarkShare + 0.05f));
            Assert.AreEqual(1f, _face.LogoAlpha, 1e-4f, "the finished mark holds");
            Assert.AreEqual(WordmarkLength, _face.WordmarkVisible, "the wordmark has typed");
            Run(LogoSeconds);
            Assert.AreEqual(0f, _face.LogoAlpha, 1e-3f, "then dims away");

            CollectionAssert.AreEqual(Sequence(Strokes), Of(BootFaceCueKind.StrokeDrawn), "each stroke once, in order");
            Assert.AreEqual(WordmarkLength / _settings.TypeCueEvery, Of(BootFaceCueKind.Typed, BootFace.WordmarkLine).Count,
                "a typing cue every two characters");
            CollectionAssert.AreEqual(new[] { BootFace.WordmarkLine }, Of(BootFaceCueKind.LineCompleted));
        }

        [Test]
        public void TheLoad_TypesItsLine_ThenFillsItsStepsInOrder_LingeringOnOne()
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
                _face.Tick(Frame);
                t += Frame;
                Drain();
                if (_face.StepsFilled > filled)
                {
                    filled = _face.StepsFilled;
                    filledAt.Add(t);
                }
            }

            Assert.AreEqual(LoadLength, _face.LoadVisible);
            CollectionAssert.AreEqual(Sequence(_settings.LoadSteps), Of(BootFaceCueKind.LoadStep));
            CollectionAssert.AreEqual(new[] { BootFace.LoadLine }, Of(BootFaceCueKind.LineCompleted));
            float plain = filledAt[1] - filledAt[0];
            float linger = filledAt[_settings.LoadPauseStep] - filledAt[_settings.LoadPauseStep - 1];
            Assert.Greater(linger, plain * 1.5f, "an old drive lingers on one step");
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
            CollectionAssert.AreEqual(Sequence(Strokes), Of(BootFaceCueKind.StrokeDrawn));
            CollectionAssert.AreEqual(Sequence(_settings.LoadSteps), Of(BootFaceCueKind.LoadStep));
            CollectionAssert.AreEqual(new[] { BootFace.WordmarkLine, BootFace.LoadLine },
                Of(BootFaceCueKind.LineCompleted));
            Assert.IsFalse(_face.IsShowing, "the lines have the face");
        }

        [Test]
        public void AWholeBoot_CuesEachStrokeAndStepOnce_AndAFastForwardRunsItThatMuchFaster()
        {
            RunWholeBoot();
            Assert.AreEqual(Strokes, Of(BootFaceCueKind.StrokeDrawn).Count);
            Assert.AreEqual(_settings.LoadSteps, Of(BootFaceCueKind.LoadStep).Count);

            _cues.Clear();
            const float Scale = 6f;
            _face.PowerOn(PowerOnSeconds);
            _face.LogoBegan(LogoSeconds);
            _face.Rush(Scale);
            Run(LogoSeconds / Scale + 2f * Frame);
            Assert.AreEqual(Strokes, Of(BootFaceCueKind.StrokeDrawn).Count, "the whole mark, six times faster");
            Assert.AreEqual(0f, _face.LogoAlpha, 1e-3f);
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
