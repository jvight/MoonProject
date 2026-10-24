using NUnit.Framework;

namespace MoonProject.Autoplay.Tests
{
    /// <summary>The hitch table's logic: the tags of a frame, the Tab windows, and the worst frames kept per tag.</summary>
    public sealed class FrameTagTests
    {
        [Test]
        public void TheFaceScreensFlips_OpenTransitionWindowsNamedInOrder()
        {
            var tagger = new FrameTagger();
            Assert.AreEqual(FrameTag.None, tagger.Step(0f, false, false, false, false));
            Assert.AreEqual(string.Empty, tagger.Window);

            FrameTag opening = tagger.Step(1f, true, false, false, false);
            Assert.AreEqual(FrameTag.Face | FrameTag.Transition, opening);
            Assert.AreEqual("Tab open #1", tagger.Window);

            FrameTag open = tagger.Step(1f + FrameTagger.TransitionSeconds + 0.1f, true, false, false, false);
            Assert.AreEqual(FrameTag.Face, open);
            Assert.AreEqual(string.Empty, tagger.Window);

            tagger.Step(5f, false, false, false, false);
            Assert.AreEqual("Tab close #1", tagger.Window);
            tagger.Step(9f, true, false, false, false);
            Assert.AreEqual("Tab open #2", tagger.Window);
        }

        [Test]
        public void BuildScanAndDock_TagTheFrame()
        {
            var tagger = new FrameTagger();
            FrameTag tags = tagger.Step(0f, false, true, true, true);
            Assert.AreEqual(FrameTag.Build | FrameTag.Scan | FrameTag.Dock, tags);
        }

        [Test]
        public void TheLogKeepsTheWorstFramesPerTag_AndOneRowPerTabWindow()
        {
            var log = new FrameTagLog();
            log.Record(new FrameSample(1f, 12f, FrameTag.None, 0, 8f, 8f, 4f, 2f), string.Empty);
            for (int i = 0; i < 5; i++)
            {
                log.Record(new FrameSample(2f + i, 20f + i * 10f, FrameTag.Face | FrameTag.Transition, 100 * i, 15f, 15f, 9f, 6f),
                    "Tab open #1");
            }

            log.Record(new FrameSample(9f, 15f, FrameTag.Face | FrameTag.Transition, 0, 5f, 5f, 3f, 2f), "Tab close #1");
            Assert.AreEqual(60f, log.WorstMilliseconds(FrameTag.Face), 1e-3f);
            Assert.AreEqual(12f, log.WorstMilliseconds(FrameTag.None), 1e-3f);
            Assert.AreEqual(0f, log.WorstMilliseconds(FrameTag.Dock), 1e-3f);
            Assert.AreEqual(60f, log.WindowWorstMilliseconds("Tab open #1"), 1e-3f);
            Assert.AreEqual(15f, log.WindowWorstMilliseconds("Tab close #1"), 1e-3f);
            Assert.AreEqual(2, log.WindowCount);
            Assert.IsTrue(log.HasCounters);

            string report = log.Render();
            StringAssert.Contains("| Face | 6 | 1 | 6.0 | 60.0 | 400 | 15.0 | 15.0 | 9.0 | 6.0 |", report);
            StringAssert.Contains("| Tab open #1 | 5 | 60.0 |", report);
            StringAssert.Contains("| Tab close #1 | 1 | 15.0 |", report);
        }

        [Test]
        public void WithoutCounters_TheReportSaysSo()
        {
            var log = new FrameTagLog();
            log.Record(new FrameSample(1f, 12f, FrameTag.None, -1, -1f, -1f, -1f, -1f), string.Empty);
            Assert.IsFalse(log.HasCounters);
            StringAssert.Contains("not available in this player", log.Render());
        }

        [Test]
        public void ALongFrameWithLittleMainThreadWork_IsWaiting_AndTheWorstFiveAreListed()
        {
            var log = new FrameTagLog();
            Assert.IsFalse(FrameTagLog.IsWaiting(new FrameSample(1f, 200f, FrameTag.None, 0, 190f, 190f, 5f, 3f)),
                "the main thread did the work");
            Assert.IsFalse(FrameTagLog.IsWaiting(new FrameSample(1f, 20f, FrameTag.None, 0, 1f, 1f, 1f, 1f)),
                "a short frame");
            Assert.IsFalse(FrameTagLog.IsWaiting(new FrameSample(1f, 200f, FrameTag.None, 0, -1f, -1f, -1f, -1f)),
                "no main thread reading");
            for (int i = 0; i < 7; i++)
            {
                var waiting = new FrameSample(2f + i, 100f + i * 10f, FrameTag.Face, 0, 5f, 5f, 2f, 40f);
                Assert.IsTrue(FrameTagLog.IsWaiting(waiting));
                log.Record(waiting, string.Empty);
            }

            Assert.AreEqual(7, log.WaitingCount);
            string report = log.Render();
            StringAssert.Contains("### Waiting frames", report);
            StringAssert.Contains("| Face | 8.0 | 160.0 | 0 | 5.0 | 5.0 | 2.0 | 40.0 |", report);
            StringAssert.DoesNotContain("| Face | 2.0 | 100.0 |", report);
        }
    }
}
