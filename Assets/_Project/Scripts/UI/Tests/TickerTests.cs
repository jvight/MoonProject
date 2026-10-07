using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The radio ticker's rules: one line at a time in arrival order, each resting for its length, waiting while the
    /// gate is closed and coming back whole after yielding, frozen by the pause, formatted with its argument and
    /// re-read in a new language.
    /// </summary>
    public sealed class TickerTests
    {
        private const float Frame = 1f / 60f;
        private const string Home = "test.ticker.home";
        private const string Signal = "test.ticker.signal";
        private const string Playing = "test.ticker.playing";
        private const string Track = "test.track.title";
        private const string Numbered = "test.ticker.n";
        private const int NumberedLines = 8;

        private readonly List<string> _started = new List<string>();
        private TickerSettings _settings;
        private LocalizationService _localization;
        private TickerText _text;
        private TickerQueue _queue;
        private Fade _line;
        private int _revision;

        [SetUp]
        public void SetUp()
        {
            var entries = new List<string>
            {
                Home, "Bell got home before you.", "Bell về nhà trước bạn rồi.",
                Signal, "Bell's picking something up… bearing {0}.", "Bell đang bắt được tín hiệu… hướng {0}.",
                Playing, "Now playing — {0}", "Đang phát — {0}",
                Track, "Slow Orbit", "Quỹ đạo chậm",
            };
            for (int i = 0; i < NumberedLines; i++)
            {
                entries.Add(Numbered + i);
                entries.Add("Line " + i);
                entries.Add("Dòng " + i);
            }

            _settings = new TickerSettings();
            _localization = TestTables.Localization(new EventBus(), entries.ToArray());
            _text = new TickerText(_localization);
            _queue = new TickerQueue(_settings, _text);
            _line = new Fade(_settings.Reveal);
            _started.Clear();
            _revision = 0;
        }

        [Test]
        public void Lines_PlayOneAtATime_InTheOrderTheyArrived()
        {
            _queue.Enqueue(new TickerLine(Home));
            _queue.Enqueue(new TickerLine(Signal, "140"));
            _queue.Enqueue(new TickerLine(Playing, Track, true));
            Run(_settings.GapSeconds * 0.5f);
            Assert.IsEmpty(_started, "a breath of quiet first");

            Run(_settings.GapSeconds);
            CollectionAssert.AreEqual(new[] { "Bell got home before you." }, _started);
            Assert.AreEqual(2, _queue.Waiting);

            Run(Lifetime(_settings.MaxHoldSeconds) * 2f);
            CollectionAssert.AreEqual(new[]
            {
                "Bell got home before you.",
                "Bell's picking something up… bearing 140.",
                "Now playing — Slow Orbit",
            }, _started);
        }

        [Test]
        public void TheNextLine_StartsOnlyOnceThePreviousHasEasedAway()
        {
            _queue.Enqueue(new TickerLine(Home));
            _queue.Enqueue(new TickerLine(Signal, "140"));
            bool overlapped = false;
            for (float t = 0f; t < 40f; t += Frame)
            {
                int before = _started.Count;
                bool wasHidden = _line.IsHidden;
                Step(Frame, true);
                overlapped |= _started.Count > 1 && _started.Count != before && !wasHidden;
            }

            Assert.AreEqual(2, _started.Count);
            Assert.IsFalse(overlapped, "a new line never replaces one still on screen");
        }

        [Test]
        public void TheArgument_IsFormattedIntoThePlaceholder()
        {
            Assert.AreEqual("Bell's picking something up… bearing 140.",
                _text.Format(new TickerLine(Signal, "140")));
        }

        [Test]
        public void AKeyArgument_IsLocalized_ALiteralOneIsShownAsItIs()
        {
            Assert.AreEqual("Now playing — Slow Orbit", _text.Format(new TickerLine(Playing, Track, true)));
            Assert.AreEqual("Now playing — test.track.title", _text.Format(new TickerLine(Playing, Track)),
                "no guessing: only a flagged argument is looked up");

            _localization.SetLanguage(TestTables.Vietnamese);
            Assert.AreEqual("Đang phát — Quỹ đạo chậm", _text.Format(new TickerLine(Playing, Track, true)));
        }

        [Test]
        public void AMismatchedLine_IsLogged_AndStillShowsItsText()
        {
            LogAssert.Expect(LogType.Error, new Regex("'test.ticker.signal' has a \\{0\\} but its line carries no"));
            Assert.AreEqual("Bell's picking something up… bearing {0}.", _text.Format(new TickerLine(Signal)));

            LogAssert.Expect(LogType.Error, new Regex("'test.ticker.home' has no \\{0\\} for the argument '140'"));
            Assert.AreEqual("Bell got home before you.", _text.Format(new TickerLine(Home, "140")));
        }

        [Test]
        public void ALineWithoutAKey_IsLogged_AndNotQueued()
        {
            LogAssert.Expect(LogType.Error, new Regex("arrived without a key"));
            _queue.Enqueue(default);
            Assert.AreEqual(0, _queue.Waiting);
        }

        [Test]
        public void ALine_RestsLongerTheLongerItIs_WithinItsLimits()
        {
            Assert.AreEqual(_settings.MinHoldSeconds, _settings.HoldSeconds(0));
            Assert.AreEqual(_settings.MaxHoldSeconds, _settings.HoldSeconds(10000));
            Assert.Greater(_settings.HoldSeconds(80), _settings.HoldSeconds(40));

            _queue.Enqueue(new TickerLine(Signal, "140"));
            RunUntilShown();
            Assert.AreEqual(_settings.HoldSeconds(_queue.Text.Length), _queue.HoldSeconds);
            Run(_queue.HoldSeconds - 0.1f);
            Assert.IsTrue(_queue.WantsShown, "still resting");
            Run(0.2f);
            Assert.IsFalse(_queue.WantsShown, "then it eases away");
            Run(_settings.Reveal.FadeOut + 0.1f);
            Assert.IsTrue(_line.IsHidden);
            Assert.IsFalse(_queue.HasLine);
        }

        [Test]
        public void AClosedGate_KeepsEveryLineWaiting()
        {
            _queue.Enqueue(new TickerLine(Home));
            _queue.Enqueue(new TickerLine(Signal, "140"));
            Run(30f, false);
            Assert.IsEmpty(_started, "nothing speaks over a card, a dig or a prompt");
            Assert.AreEqual(2, _queue.Waiting);

            Run(_settings.GapSeconds + Frame * 2f);
            CollectionAssert.AreEqual(new[] { "Bell got home before you." }, _started);
        }

        [Test]
        public void ALineUp_EasesAwayWhenTheGateCloses_AndComesBackWhole()
        {
            _queue.Enqueue(new TickerLine(Home));
            RunUntilShown();
            Run(_queue.HoldSeconds * 0.8f);

            Run(Frame, false);
            Assert.IsFalse(_queue.WantsShown, "it yields at once");
            Run(_settings.Reveal.FadeOut + 0.1f, false);
            Assert.IsTrue(_line.IsHidden);
            Run(20f, false);
            Assert.IsTrue(_line.IsHidden, "and stays away while the gate is closed");
            Assert.IsTrue(_queue.HasLine, "keeping its place");

            Run(_settings.GapSeconds - 0.1f);
            Assert.IsTrue(_line.IsHidden, "a breath of quiet after the gate opens");
            RunUntilShown();
            Run(_queue.HoldSeconds - 0.2f);
            Assert.IsTrue(_queue.WantsShown, "it rests its whole time again");
            Assert.AreEqual(1, _started.Count, "the same line, not a new one");
        }

        [Test]
        public void AGateClosingOnlyBriefly_TurnsTheLineBackWithoutLosingItsRest()
        {
            _queue.Enqueue(new TickerLine(Home));
            RunUntilShown();
            Run(1f);
            Run(_settings.Reveal.FadeOut * 0.3f, false);
            Assert.IsFalse(_line.IsHidden);

            Run(_settings.Reveal.FadeIn);
            Assert.IsTrue(_queue.WantsShown, "it eases straight back in");
            Run(_queue.HoldSeconds - 1f);
            Assert.IsFalse(_queue.WantsShown, "the rest it already had still counts");
        }

        [Test]
        public void Pausing_FreezesTheLineAndTheQueue()
        {
            _queue.Enqueue(new TickerLine(Home));
            _queue.Enqueue(new TickerLine(Signal, "140"));
            RunUntilShown();
            for (int i = 0; i < 10000; i++)
            {
                Step(0f, true);
            }

            Assert.IsTrue(_queue.WantsShown && _line.IsShown, "the line waits under the pause menu");
            Assert.AreEqual(1, _queue.Waiting);
            Assert.AreEqual(1, _started.Count);
        }

        [Test]
        public void AWaitingLine_TakesTheNewestArgumentOfItsKey()
        {
            _queue.Enqueue(new TickerLine(Signal, "140"));
            _queue.Enqueue(new TickerLine(Home));
            _queue.Enqueue(new TickerLine(Signal, "200"));
            Assert.AreEqual(2, _queue.Waiting);

            Run(60f);
            CollectionAssert.AreEqual(new[]
            {
                "Bell's picking something up… bearing 200.",
                "Bell got home before you.",
            }, _started);
        }

        [Test]
        public void ALineWaitingToComeBack_TakesTheNewestArgumentOfItsKey()
        {
            _queue.Enqueue(new TickerLine(Signal, "140"));
            RunUntilShown();
            Run(_settings.Reveal.FadeOut + 0.1f, false);
            Assert.IsTrue(_line.IsHidden, "it yielded");

            _queue.Enqueue(new TickerLine(Signal, "205"));
            Assert.AreEqual(0, _queue.Waiting, "not a second line");
            RunUntilShown();
            Assert.AreEqual("Bell's picking something up… bearing 205.", _queue.Text, "it comes back with the news");
            Assert.AreEqual(2, _started.Count);
        }

        [Test]
        public void TheLineOnScreen_IsNotQueuedAgain()
        {
            _queue.Enqueue(new TickerLine(Signal, "140"));
            RunUntilShown();
            _queue.Enqueue(new TickerLine(Signal, "140"));
            Assert.AreEqual(0, _queue.Waiting);
            _queue.Enqueue(new TickerLine(Signal, "200"));
            Assert.AreEqual(1, _queue.Waiting, "a new bearing is news");
        }

        [Test]
        public void PastItsCapacity_TheOldestWaitingLineIsDropped()
        {
            Assert.Less(_settings.Capacity, NumberedLines);
            for (int i = 0; i <= _settings.Capacity; i++)
            {
                _queue.Enqueue(new TickerLine(Numbered + i));
            }

            Assert.AreEqual(_settings.Capacity, _queue.Waiting);
            Run(_settings.GapSeconds + Frame * 2f);
            CollectionAssert.AreEqual(new[] { "Line 1" }, _started);
        }

        [Test]
        public void ANewLanguage_RereadsTheLineOnScreen_AndWaitingLinesStartInIt()
        {
            _queue.Enqueue(new TickerLine(Signal, "140"));
            _queue.Enqueue(new TickerLine(Playing, Track, true));
            RunUntilShown();
            Assert.AreEqual("Bell's picking something up… bearing 140.", _queue.Text);

            _localization.SetLanguage(TestTables.Vietnamese);
            int revision = _queue.Revision;
            _queue.Relocalize();
            Assert.AreEqual("Bell đang bắt được tín hiệu… hướng 140.", _queue.Text);
            Assert.Greater(_queue.Revision, revision, "the view rewrites its label");
            _revision = _queue.Revision;

            Run(60f);
            Assert.AreEqual("Đang phát — Quỹ đạo chậm", _started[_started.Count - 1]);
        }

        private float Lifetime(float hold)
        {
            return _settings.GapSeconds + _settings.Reveal.FadeIn + hold + _settings.Reveal.FadeOut + 0.5f;
        }

        private void RunUntilShown()
        {
            for (float t = 0f; t < 30f && !(_queue.WantsShown && _line.IsShown); t += Frame)
            {
                Step(Frame, true);
            }

            Assert.IsTrue(_line.IsShown, "the line eased in");
        }

        private void Run(float seconds, bool gateOpen = true)
        {
            int frames = Mathf.CeilToInt(seconds / Frame);
            for (int i = 0; i < frames; i++)
            {
                Step(Frame, gateOpen);
            }
        }

        private void Step(float deltaTime, bool gateOpen)
        {
            _queue.Step(deltaTime, gateOpen, _line.IsShown, _line.IsHidden);
            if (_queue.Revision != _revision)
            {
                _revision = _queue.Revision;
                _started.Add(_queue.Text);
            }

            _line.Set(_queue.WantsShown);
            _line.Step(deltaTime);
        }
    }
}
