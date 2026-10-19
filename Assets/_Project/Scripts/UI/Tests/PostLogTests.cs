using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.UI.Editor;

namespace MoonProject.UI.Tests
{
    /// <summary>07's self-test log: how its lines type, settle and fit inside Rover's step durations.</summary>
    public sealed class PostLogTests
    {
        private const float Frame = 1f / 60f;

        // Rover's line seconds (PostSettings' opening lines), in BootCheckStep order; the checks the opening leaves
        // out for pace keep the seconds they had, so they still fit if they are put back.
        private static readonly float[] StepSeconds =
        {
            0.7f, 0.8f, 1.4f, 1.0f, 1.2f, 1.8f, 1.8f, 1.6f, 1.4f, 2.0f, 1.8f, 0.9f, 0.7f, 0.6f,
        };

        private PostLineSettings _settings;
        private PostLog _log;
        private LocalizationService _text;

        [SetUp]
        public void SetUp()
        {
            _settings = new PostLineSettings();
            _log = new PostLog(_settings);
            _text = TestTables.Localization(new EventBus(),
                "post.cutter", "CUTTER", "MỎ CẮT",
                "post.cutter.fault", "FAULT · NO IGNITER", "LỖI · MẤT BỘ ĐÁNH LỬA",
                "post.solar_wing", "SOLAR WING", "CÁNH PIN",
                "post.battery", "CELLS", "PIN",
                "post.battery.ok", "{0}%", "{0}%",
                "post.resume", "CONTINUE · {0} DAYS", "TIẾP TỤC · {0} NGÀY",
                "post.result.ok", "OK", "OK",
                "post.result.fault", "FAULT", "LỖI",
                "post.result.jammed", "JAMMED", "KẸT");
        }

        [Test]
        public void ACheck_TypesItsName_RunsDots_ThenTypesItsResultOverThem()
        {
            _log.Started(BootCheckStep.Cutter, PostText.Started(BootCheckStep.Cutter, _text, 0), false);
            Run(1f);
            Assert.AreEqual("CUTTER · ", Shown(0), "the name types in");
            Assert.Greater(MaxDots(1f), 0, "and the check runs its dots");

            _log.Completed(BootCheckStep.Cutter, BootCheckResult.Fault,
                PostText.Settled(BootCheckStep.Cutter, BootCheckResult.Fault, _text, 0, 0f));
            Assert.AreEqual(0, _log.DotsOf(0), "the dots go when the result arrives");
            Assert.AreEqual("CUTTER · ".Length, _log.Visible(0), "the name stays, only the result types");
            Run(1f);
            Assert.AreEqual("CUTTER · FAULT · NO IGNITER", Shown(0), "the step's own words for its fault");
            Assert.IsTrue(_log.IsAlertLine(0), "a fault is an alert");
        }

        [Test]
        public void AStepWithoutItsOwnWords_UsesTheSharedResult_AndTheCellsSayTheirCharge()
        {
            Assert.AreEqual("SOLAR WING · JAMMED",
                PostText.Settled(BootCheckStep.SolarWing, BootCheckResult.Jammed, _text, 0, 0f));
            Assert.AreEqual("CELLS · 50%", PostText.Settled(BootCheckStep.Battery, BootCheckResult.Ok, _text, 0, 0.5f));
            Assert.IsTrue(PostLog.IsAlert(BootCheckResult.Jammed));
            Assert.IsTrue(PostLog.IsAlert(BootCheckResult.ReceiveOnly));
            Assert.IsFalse(PostLog.IsAlert(BootCheckResult.Ok));
        }

        [Test]
        public void AnInformationLine_TypesWhole_WithoutDots_AndTheDaysAreGrouped()
        {
            string line = PostText.Started(BootCheckStep.Resume, _text, 13148);
            Assert.AreEqual("CONTINUE · 13 148 DAYS", line);
            _log.Started(BootCheckStep.Resume, line, true);
            Run(1.5f);
            Assert.AreEqual(line, Shown(0));
            Assert.AreEqual(0, _log.DotsOf(0));
            _log.Completed(BootCheckStep.Resume, BootCheckResult.Info, line);
            Assert.AreEqual(line.Length, _log.Visible(0), "settling an information line keeps it");
        }

        [Test]
        public void TheLogRunsFromTheFirstStepUntilFinished_ThenLingers()
        {
            Assert.IsFalse(_log.IsRunning);
            _log.Started(BootCheckStep.Carrier, "CARRIER", true);
            Assert.IsTrue(_log.IsRunning, "guidance waits while it runs");
            _log.Finish();
            Assert.IsFalse(_log.IsRunning);
            Assert.IsTrue(_log.IsFinished);
            Run(0.5f);
            Assert.AreEqual(0.5f, _log.SinceFinished, 0.05f);
            _log.Reset();
            Assert.AreEqual(0, _log.Count);
            Assert.IsFalse(_log.IsFinished);
        }

        [Test]
        public void EveryLine_TypesInsideItsStep_InEveryLanguage()
        {
            foreach (TextAsset asset in StringTableAssets.Load())
            {
                StringTable table = StringTable.Parse(asset.text, asset.name);
                var localization = new LocalizationService(new EventBus(), new[] { table });
                for (int step = 0; step < PostLog.Capacity; step++)
                {
                    var s = (BootCheckStep)step;
                    int longest = PostText.Started(s, localization, _settings.ResumeDays).Length;
                    if (!PostText.IsInformation(s))
                    {
                        foreach (BootCheckResult result in new[]
                                 {
                                     BootCheckResult.Ok, BootCheckResult.Fault, BootCheckResult.Jammed,
                                     BootCheckResult.ReceiveOnly,
                                 })
                        {
                            longest = Mathf.Max(longest,
                                PostText.Settled(s, result, localization, _settings.ResumeDays, 1f).Length);
                        }
                    }

                    float seconds = longest / Mathf.Max(_settings.CharsPerSecond, longest / _settings.MaxLineSeconds);
                    Assert.Less(seconds, StepSeconds[step] * 0.8f,
                        $"{table.Language} {s}: {longest} characters must type well inside its {StepSeconds[step]} s");
                }
            }
        }

        [Test]
        public void TheCellsLine_ReadsItsFillLive_ThenSettles()
        {
            _log.Started(BootCheckStep.Battery, PostText.Started(BootCheckStep.Battery, _text, 0), false);
            _log.Live(BootCheckStep.Battery, PostText.Filling(_text, 0.2f));
            Assert.AreEqual(0, _log.Visible(0), "no reading before the name has typed");
            Run(0.5f);
            _log.Live(BootCheckStep.Battery, PostText.Filling(_text, 0.2f));
            Assert.AreEqual("CELLS · 20%", Shown(0), "the fill shows at once");
            Assert.AreEqual(0, _log.DotsOf(0));
            _log.Completed(BootCheckStep.Battery, BootCheckResult.Ok,
                PostText.Settled(BootCheckStep.Battery, BootCheckResult.Ok, _text, 0, 0.5f));
            Run(0.5f);
            Assert.AreEqual("CELLS · 50%", Shown(0));
        }

        [Test]
        public void ALongBlock_TypesFaster_SoItStillFitsItsStep()
        {
            string block = new string('x', 120);
            _log.Started(BootCheckStep.Agenda, block, true);
            Run(_settings.MaxLineSeconds + 0.05f);
            Assert.AreEqual(block.Length, _log.Visible(0));
        }

        [Test]
        public void AFastForward_TypesTheLinesThatMuchFaster_UntilTheNextWake()
        {
            const float Scale = 4f;
            const string Line = "CONTINUE · 13 148 DAYS · RESTORING TASK QUEUE";
            float seconds = Line.Length / Mathf.Max(_settings.CharsPerSecond, Line.Length / _settings.MaxLineSeconds);
            _log.Started(BootCheckStep.Resume, Line, true);
            _log.Rush(Scale);
            Run(seconds / Scale + 2f * Frame);
            Assert.AreEqual(Line, Shown(0), "the line types four times faster");

            _log.Reset();
            _log.Started(BootCheckStep.Resume, Line, true);
            Run(seconds / Scale + 2f * Frame);
            Assert.AreNotEqual(Line, Shown(0), "a new wake types at its own pace");
        }

        private void Run(float seconds)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                _log.Tick(Frame);
            }
        }

        private int MaxDots(float seconds)
        {
            int most = 0;
            for (float t = 0f; t < seconds; t += Frame)
            {
                _log.Tick(Frame);
                most = Mathf.Max(most, _log.DotsOf(0));
            }

            return most;
        }

        private string Shown(int line)
        {
            return _log.Target(line).Substring(0, _log.Visible(line));
        }
    }
}
