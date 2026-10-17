using System.Collections.Generic;
using NUnit.Framework;

namespace MoonProject.UI.Tests
{
    /// <summary>The objective line's timing (docs/design/opening-v2.md §3).</summary>
    public sealed class ObjectiveLineTests
    {
        private const float Step = 0.1f;
        private const float Still = 0f;
        private const float Driving = 3f;

        private GuidanceSettings _settings;
        private Memory _memory;
        private ObjectiveLine _line;

        [SetUp]
        public void SetUp()
        {
            _settings = new GuidanceSettings();
            _memory = new Memory();
            _line = new ObjectiveLine(_settings, _memory);
        }

        [Test]
        public void ANewStep_ShowsAfterTheDelay_OnceSevenHasBeenStillAWhile()
        {
            _line.SetKey("next.v2.drive_lander");
            Run(_settings.ObjectiveDelaySeconds - 0.3f, Still);
            Assert.IsFalse(_line.WantsShown, "not before the delay");

            Run(_settings.ObjectiveDelaySeconds + _settings.ObjectiveStillSeconds, Still);
            Assert.IsTrue(_line.WantsShown);
            Assert.IsTrue(_line.Bright, "the first time is bright");
        }

        [Test]
        public void WhileDriving_NothingShows_EvenAfterTheDelay()
        {
            _line.SetKey("next.v2.drive_lander");
            Run(_settings.ObjectiveDelaySeconds + 5f, Driving);
            Assert.IsFalse(_line.WantsShown);

            Run(_settings.ObjectiveStillSeconds + Step, Still);
            Assert.IsTrue(_line.WantsShown, "it comes once 07 has been still for a second");
        }

        [Test]
        public void TheLine_RestsBright_ThenDimsToASmallLine()
        {
            _line.SetKey("next.v2.build_pad");
            Run(_settings.ObjectiveDelaySeconds + _settings.ObjectiveStillSeconds + 0.3f, Still);
            Assert.IsTrue(_line.Bright);

            Run(_settings.ObjectiveRestSeconds * 0.5f, Still, true);
            Assert.IsTrue(_line.Bright, "still resting");
            Run(_settings.ObjectiveRestSeconds, Still, true);
            Assert.IsFalse(_line.Bright, "dimmed");
            Assert.IsTrue(_line.WantsShown, "a dim line stays until its step is done");
        }

        [Test]
        public void TheRest_CountsOnlyWhileTheLineIsOnScreen()
        {
            _line.SetKey("next.v2.build_pad");
            Run(_settings.ObjectiveDelaySeconds + _settings.ObjectiveStillSeconds + 0.3f, Still);

            Run(_settings.ObjectiveRestSeconds * 3f, Still, false);

            Assert.IsTrue(_line.Bright, "hidden behind a chip, it has not rested yet");
        }

        [Test]
        public void CompletingTheStep_ClearsTheLineAtOnce_AndTheNextOneStartsItsDelay()
        {
            _line.SetKey("next.v2.build_pad");
            Run(_settings.ObjectiveDelaySeconds + _settings.ObjectiveStillSeconds + 0.3f, Still);
            Assert.IsTrue(_line.WantsShown);

            _line.SetKey("next.v2.charge");
            Assert.IsFalse(_line.WantsShown, "cleared the moment the step changes");
            Run(_settings.ObjectiveDelaySeconds - 0.3f, Still);
            Assert.IsFalse(_line.WantsShown);
            Run(_settings.ObjectiveDelaySeconds + _settings.ObjectiveStillSeconds, Still);
            Assert.IsTrue(_line.WantsShown);
        }

        [Test]
        public void AnEmptyKey_MeansNoLine()
        {
            _line.SetKey("next.v2.build_pad");
            Run(_settings.ObjectiveDelaySeconds + _settings.ObjectiveStillSeconds + 0.3f, Still);

            _line.SetKey(string.Empty);
            Run(10f, Still);

            Assert.IsFalse(_line.WantsShown);
        }

        [Test]
        public void ADimLine_BrightensAgain_OnlyAfterStillnessAndALongWithoutProgress()
        {
            _line.SetKey("next.v2.build_pad");
            DimIt();

            Run(_settings.ObjectiveIdleSeconds * 0.5f, Still, true);
            Assert.IsFalse(_line.Bright, "progress was recent enough");

            _line.NoteProgress();
            Run(_settings.ObjectiveIdleSeconds - 5f, Still, true);
            Assert.IsFalse(_line.Bright, "progress restarts the wait");

            Run(10f, Driving, true);
            Run(_settings.ObjectiveRebrightStillSeconds * 0.5f, Still, true);
            Assert.IsFalse(_line.Bright, "07 must be still for a few seconds as well");

            Run(_settings.ObjectiveRebrightStillSeconds, Still, true);
            Assert.IsTrue(_line.Bright, "still and idle: it brightens once more");
        }

        [Test]
        public void AStepShownInThisSave_ShowsDimmed_WhenTheGameLoads()
        {
            _memory.Add("next.v2.build_pad");

            _line.SetKey("next.v2.build_pad");
            Run(_settings.ObjectiveDelaySeconds + _settings.ObjectiveStillSeconds + 0.3f, Still);

            Assert.IsTrue(_line.WantsShown);
            Assert.IsFalse(_line.Bright, "once, dimmed");
        }

        [Test]
        public void EachStepsLine_IsRemembered_WhenItFirstShows()
        {
            _line.SetKey("next.v2.drive_lander");
            Assert.IsFalse(_memory.Has("next.v2.drive_lander"));

            Run(_settings.ObjectiveDelaySeconds + _settings.ObjectiveStillSeconds + 0.3f, Still);

            Assert.IsTrue(_memory.Has("next.v2.drive_lander"));
        }

        [Test]
        public void GuidanceSwitchedOff_ShowsNothing()
        {
            _line.SetKey("next.v2.build_pad");
            for (float t = 0f; t < 10f; t += Step)
            {
                _line.Tick(Step, Still, false, false);
            }

            Assert.IsFalse(_line.WantsShown);
        }

        [Test]
        public void ThePromptsLedger_RemembersTheLinesAcrossASave()
        {
            var ledger = new PromptLedger(new PromptSettings());
            ledger.Add("next.v2.build_pad");

            var restored = new PromptLedger(new PromptSettings());
            restored.Restore(ledger.Capture());

            Assert.IsTrue(restored.Has("next.v2.build_pad"));
            Assert.IsFalse(restored.Has("next.v2.charge"));
        }

        private void DimIt()
        {
            Run(_settings.ObjectiveDelaySeconds + _settings.ObjectiveStillSeconds + 0.3f, Still);
            Run(_settings.ObjectiveRestSeconds + 1f, Still, true);
            Assert.IsFalse(_line.Bright);
        }

        private void Run(float seconds, float speed, bool showing = false)
        {
            for (float t = 0f; t < seconds; t += Step)
            {
                _line.Tick(Step, speed, true, showing);
            }
        }

        private sealed class Memory : IObjectiveMemory
        {
            private readonly HashSet<string> _keys = new HashSet<string>();

            public bool Has(string key)
            {
                return _keys.Contains(key);
            }

            public void Add(string key)
            {
                _keys.Add(key);
            }
        }
    }
}
