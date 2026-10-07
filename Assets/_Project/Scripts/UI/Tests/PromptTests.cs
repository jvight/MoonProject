using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MoonProject.Core.Input;
using MoonProject.Gameplay;
using MoonProject.UI.Editor;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// Design ruling 6 as rules: a prompt shows only near a usable thing, after a short dwell, one at a time, only the
    /// first few times (counted and saved), then never again.
    /// </summary>
    public sealed class PromptTests
    {
        private const float Frame = 1f / 60f;

        private static readonly Vector3 Site = new Vector3(10f, 2f, -4f);

        private PromptSettings _settings;
        private PromptLedger _ledger;
        private PromptDirector _director;
        private Fade _chip;

        [SetUp]
        public void SetUp()
        {
            _settings = new PromptSettings();
            _ledger = new PromptLedger(_settings);
            _director = new PromptDirector(_settings, _ledger);
            _chip = new Fade(_settings.Reveal);
        }

        [Test]
        public void APrompt_WaitsForItsDwell_ThenShowsOnceCounted()
        {
            float dwell = _settings.Find(InteractionKind.Excavate).DwellSeconds;
            Run(dwell * 0.5f, Ready(InteractionKind.Excavate));
            Assert.AreEqual(InteractionKind.None, _director.Displayed, "driving past does not flash a prompt");

            Run(dwell, Ready(InteractionKind.Excavate));
            Assert.AreEqual(InteractionKind.Excavate, _director.Displayed);
            Assert.IsTrue(_director.WantsShown);
            Assert.AreEqual(1, _ledger.Shown(InteractionKind.Excavate), "a showing counts once");
            Run(1f, Ready(InteractionKind.Excavate));
            Assert.AreEqual(1, _ledger.Shown(InteractionKind.Excavate));
        }

        [Test]
        public void ThePrompt_FloatsAboveTheHint_ByItsLift()
        {
            Run(2f, Ready(InteractionKind.Excavate));
            Vector3 expected = Site + Vector3.up * _settings.Find(InteractionKind.Excavate).LiftMetres;
            Assert.AreEqual(expected, _director.WorldPoint);
        }

        [Test]
        public void NothingShows_WhenNotReady_OrTheGateIsClosed_OrTheKindHasNoPrompt()
        {
            Run(20f, new InteractionHint(InteractionKind.Excavate, Site, false));
            Run(20f, Ready(InteractionKind.Excavate), gateOpen: false);
            Run(20f, Ready(InteractionKind.Upgrade));
            Run(20f, InteractionHint.None);
            Assert.AreEqual(InteractionKind.None, _director.Displayed);
            Assert.AreEqual(0, _ledger.Shown(InteractionKind.Excavate));
        }

        [Test]
        public void DoingTheAction_EndsTheShowing_ThenItRestsForItsCooldown()
        {
            Run(1f, Ready(InteractionKind.Excavate));
            _director.NotifyUsed(InteractionKind.Excavate);
            Assert.IsFalse(_director.WantsShown, "the prompt bows out as soon as the player does it");
            Assert.AreEqual(1, _ledger.Used(InteractionKind.Excavate));

            Run(_settings.RepeatCooldown * 0.5f, Ready(InteractionKind.Excavate));
            Assert.AreEqual(InteractionKind.None, _director.Displayed, "rests during the cooldown");
            float dwell = _settings.Find(InteractionKind.Excavate).DwellSeconds;
            Run(_settings.RepeatCooldown * 0.5f + dwell + 1f, Ready(InteractionKind.Excavate));
            Assert.AreEqual(InteractionKind.Excavate, _director.Displayed, "and may return once");
            Assert.AreEqual(2, _ledger.Shown(InteractionKind.Excavate));
        }

        [Test]
        public void AShowing_BowsOutOnItsOwn_AfterTheMaximumTime()
        {
            Run(1f, Ready(InteractionKind.Excavate));
            Run(_settings.MaxShowSeconds, Ready(InteractionKind.Excavate));
            Assert.IsFalse(_director.WantsShown);
        }

        [Test]
        public void APrompt_IsRetired_AfterItsShowings()
        {
            for (int i = 0; i < _settings.ShowingsToRetire; i++)
            {
                Run(_settings.RepeatCooldown + _settings.MaxShowSeconds + 2f, Ready(InteractionKind.Tether));
            }

            Assert.AreEqual(_settings.ShowingsToRetire, _ledger.Shown(InteractionKind.Tether));
            Assert.IsFalse(_ledger.ShouldTeach(InteractionKind.Tether));
            Run(120f, Ready(InteractionKind.Tether));
            Assert.AreEqual(_settings.ShowingsToRetire, _ledger.Shown(InteractionKind.Tether), "never again");
            Assert.AreEqual(InteractionKind.None, _director.Displayed);
        }

        [Test]
        public void APrompt_IsRetired_OnceTheActionIsLearned()
        {
            for (int i = 0; i < _settings.Find(InteractionKind.Excavate).UsesToRetire; i++)
            {
                _director.NotifyUsed(InteractionKind.Excavate);
            }

            Run(60f, Ready(InteractionKind.Excavate));
            Assert.AreEqual(0, _ledger.Shown(InteractionKind.Excavate),
                "someone who already digs is not taught to dig");
        }

        [Test]
        public void TheTunePrompt_NamesInteract_AndRetiresAfterTheFirstTurnOfTheDial()
        {
            PromptEntry tune = _settings.Find(InteractionKind.Tune);
            Assert.IsNotNull(tune, "Bell's dial is taught");
            Assert.AreEqual(RoverAction.Excavate, tune.Action, "the dial turns with Interact, like the repair");
            Assert.AreEqual(1, tune.UsesToRetire, "one turn and the player knows the dial");

            Run(2f, Ready(InteractionKind.Tune));
            Assert.AreEqual(InteractionKind.Tune, _director.Displayed);
            _director.NotifyUsed(InteractionKind.Tune);
            Assert.IsFalse(_director.WantsShown, "the turn ends the showing");
            Assert.IsFalse(_ledger.ShouldTeach(InteractionKind.Tune), "and retires the prompt");
            Run(_settings.RepeatCooldown + 60f, Ready(InteractionKind.Tune));
            Assert.AreEqual(1, _ledger.Shown(InteractionKind.Tune), "never again");
        }

        [Test]
        public void EveryOtherPrompt_IsLearnedAfterTheDefaultNumberOfUses()
        {
            foreach (PromptEntry entry in _settings.Entries)
            {
                if (entry.Kind != InteractionKind.Tune)
                {
                    Assert.AreEqual(PromptEntry.DefaultUsesToRetire, entry.UsesToRetire, entry.Kind.ToString());
                }
            }
        }

        [Test]
        public void TheShippedTuning_LetsEveryPromptBeLearned()
        {
            var tuning = AssetDatabase.LoadAssetAtPath<UiTuning>(UiAssetPaths.Tuning);
            Assert.IsNotNull(tuning, UiAssetPaths.Tuning);
            foreach (PromptEntry entry in tuning.Prompts.Entries)
            {
                Assert.That(entry.UsesToRetire, Is.InRange(1, 10),
                    $"{entry.Kind}: an entry saved before the per-prompt limit existed reads its default");
            }
        }

        [Test]
        public void OnlyOnePrompt_AtATime_TheNextWaitsForTheFadeOut()
        {
            Run(1f, Ready(InteractionKind.Excavate));
            Assert.AreEqual(InteractionKind.Excavate, _director.Displayed);

            InteractionHint tether = Ready(InteractionKind.Tether);
            _director.Step(Frame, true, tether, _chip.IsHidden);
            _chip.Set(_director.WantsShown);
            _chip.Step(Frame);
            Assert.AreEqual(InteractionKind.Excavate, _director.Displayed, "the old prompt fades before the new one");
            Assert.IsFalse(_director.WantsShown);

            Run(_settings.Reveal.FadeOut + _settings.Find(InteractionKind.Tether).DwellSeconds + 0.1f, tether);
            Assert.AreEqual(InteractionKind.Tether, _director.Displayed);
            Assert.IsTrue(_director.WantsShown);
        }

        [Test]
        public void Ledger_RoundTripsThroughItsSaveData_ByKindName()
        {
            _ledger.RecordShown(InteractionKind.Ping);
            _ledger.RecordShown(InteractionKind.Ping);
            _ledger.RecordUsed(InteractionKind.Deposit);
            string json = JsonUtility.ToJson(_ledger.Capture());
            StringAssert.Contains("\"Ping\"", json, "saved by name, not by enum number");

            var restored = new PromptLedger(_settings);
            restored.Restore(JsonUtility.FromJson<PromptsSaveData>(json));
            Assert.AreEqual(2, restored.Shown(InteractionKind.Ping));
            Assert.AreEqual(1, restored.Used(InteractionKind.Deposit));
            Assert.AreEqual(0, restored.Shown(InteractionKind.Excavate));
        }

        [Test]
        public void Ledger_SkipsKindsThisBuildDoesNotKnow()
        {
            var data = new PromptsSaveData
            {
                kinds = new[]
                {
                    new PromptCountData { kind = "Hoverjump", shown = 3, used = 1 },
                    new PromptCountData { kind = "Reel", shown = 1, used = 0 },
                },
            };

            _ledger.Restore(data);
            Assert.AreEqual(1, _ledger.Shown(InteractionKind.Reel));
        }

        [Test]
        public void Settings_DefaultTable_IsValid_AndTeachesEveryKindButTheUpgrade()
        {
            Assert.IsNull(_settings.Validate());
            Assert.IsNull(_settings.Find(InteractionKind.Upgrade), "the tower panel teaches the upgrade itself");
            InteractionKind[] taught =
            {
                InteractionKind.Ping, InteractionKind.Excavate, InteractionKind.Tether, InteractionKind.Deposit,
                InteractionKind.Reel,
            };
            foreach (InteractionKind kind in taught)
            {
                Assert.IsNotNull(_settings.Find(kind), kind.ToString());
            }
        }

        [Test]
        public void ScreenAnchor_MapsViewportToPanel_KeepsAMargin_AndRejectsBehindTheCamera()
        {
            var panel = new Vector2(1920f, 1080f);
            Assert.IsTrue(ScreenAnchor.TryToPanel(new Vector3(0.5f, 0.5f, 10f), panel, 50f, out Vector2 centre));
            AssertNear(new Vector2(960f, 540f), centre, "the centre");
            Assert.IsTrue(ScreenAnchor.TryToPanel(new Vector3(0.25f, 0.9f, 10f), panel, 50f, out Vector2 upperLeft));
            AssertNear(new Vector2(480f, 108f), upperLeft, "viewport y is up, panel y is down");
            Assert.IsTrue(ScreenAnchor.TryToPanel(new Vector3(-1f, 2f, 5f), panel, 50f, out Vector2 clamped));
            AssertNear(new Vector2(50f, 50f), clamped, "kept a margin from the edges");
            Assert.IsFalse(ScreenAnchor.TryToPanel(new Vector3(0.5f, 0.5f, -1f), panel, 50f, out _));
        }

        [Test]
        public void ScreenAnchor_Follow_ClosesHalfTheGapPerHalfLife()
        {
            Vector2 next = ScreenAnchor.Follow(Vector2.zero, new Vector2(100f, 0f), 0.1f, 0.1f);
            Assert.AreEqual(50f, next.x, 1e-3f);
            Assert.AreEqual(new Vector2(100f, 0f), ScreenAnchor.Follow(Vector2.zero, new Vector2(100f, 0f), 0f, 0.1f));
        }

        private static void AssertNear(Vector2 expected, Vector2 actual, string message)
        {
            Assert.Less(Vector2.Distance(expected, actual), 1e-3f, $"{message}: expected {expected}, got {actual}");
        }

        private static InteractionHint Ready(InteractionKind kind)
        {
            return new InteractionHint(kind, Site, true);
        }

        private void Run(float seconds, InteractionHint hint, bool gateOpen = true)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                _director.Step(Frame, gateOpen, hint, _chip.IsHidden);
                _chip.Set(_director.WantsShown);
                _chip.Step(Frame);
            }
        }
    }
}
