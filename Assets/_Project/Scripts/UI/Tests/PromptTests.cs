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
            float dwell = _settings.Find(InteractionKind.Salvage).DwellSeconds;
            Run(dwell * 0.5f, Ready(InteractionKind.Salvage));
            Assert.AreEqual(InteractionKind.None, _director.Displayed, "driving past does not flash a prompt");

            Run(dwell, Ready(InteractionKind.Salvage));
            Assert.AreEqual(InteractionKind.Salvage, _director.Displayed);
            Assert.IsTrue(_director.WantsShown);
            Assert.AreEqual(1, _ledger.Shown(InteractionKind.Salvage), "a showing counts once");
            Run(1f, Ready(InteractionKind.Salvage));
            Assert.AreEqual(1, _ledger.Shown(InteractionKind.Salvage));
        }

        [Test]
        public void ThePrompt_FloatsAboveTheHint_ByItsLift()
        {
            Run(2f, Ready(InteractionKind.Salvage));
            Vector3 expected = Site + Vector3.up * _settings.Find(InteractionKind.Salvage).LiftMetres;
            Assert.AreEqual(expected, _director.WorldPoint);
        }

        [Test]
        public void NothingShows_WhenNotReady_OrTheGateIsClosed()
        {
            Run(20f, new InteractionHint(InteractionKind.Salvage, Site, false));
            Run(20f, Ready(InteractionKind.Salvage), gateOpen: false);
            Run(20f, Ready(InteractionKind.Deconstruct), gateOpen: false);
            Run(20f, InteractionHint.None);
            Assert.AreEqual(InteractionKind.None, _director.Displayed);
            Assert.AreEqual(0, _ledger.Shown(InteractionKind.Salvage));
        }

        [Test]
        public void DoingTheAction_EndsTheShowing_AndRetiresThePrompt()
        {
            Run(1f, Ready(InteractionKind.Salvage));
            _director.NotifyUsed(InteractionKind.Salvage);
            Assert.IsFalse(_director.WantsShown, "the prompt bows out as soon as the player does it");
            Assert.AreEqual(1, _ledger.Used(InteractionKind.Salvage));

            float dwell = _settings.Find(InteractionKind.Salvage).DwellSeconds;
            Run(_settings.RepeatCooldown + dwell + 1f, Ready(InteractionKind.Salvage));
            Assert.AreEqual(InteractionKind.None, _director.Displayed, "done once, the verb is known and never returns");
            Assert.AreEqual(1, _ledger.Shown(InteractionKind.Salvage));
        }

        [Test]
        public void AShowing_BowsOutOnItsOwn_AfterTheMaximumTime()
        {
            Run(1f, Ready(InteractionKind.Salvage));
            Run(_settings.MaxShowSeconds, Ready(InteractionKind.Salvage));
            Assert.IsFalse(_director.WantsShown);
        }

        [Test]
        public void AnIgnoredPrompt_RestsAtItsSpot_ButANewTargetTeachesAtOnce()
        {
            Run(1f, Ready(InteractionKind.Salvage));
            Run(_settings.MaxShowSeconds + 1f, Ready(InteractionKind.Salvage));
            Assert.IsFalse(_director.WantsShown, "ignored at this crate: it rests");
            Run(2f, Ready(InteractionKind.Salvage));
            Assert.IsFalse(_director.WantsShown, "and does not nag at the same spot");

            var fresh = new InteractionHint(InteractionKind.Salvage,
                Site + Vector3.forward * (_settings.RepeatRadius + 2f), true);
            Assert.IsTrue(_director.Wants(fresh), "a fresh crate is worth teaching");
            Run(1f, fresh);
            Assert.IsTrue(_director.WantsShown, "the player who drives straight to it sees the key");
        }

        [Test]
        public void ATargetThatWavers_KeepsItsDwell_AndKeepsTheSlot()
        {
            InteractionHint cut = Ready(InteractionKind.Salvage);
            for (int i = 0; i < 12 && !_director.WantsShown; i++)
            {
                Run(0.15f, cut);
                Assert.IsTrue(_director.Wants(InteractionHint.None), "a wavering aim still holds the slot");
                Run(0.1f, InteractionHint.None);
            }

            Assert.IsTrue(_director.WantsShown, "nosing in does not restart the dwell for ever");
            Run(_settings.DwellGraceSeconds + 0.2f, InteractionHint.None);
            Assert.IsFalse(_director.Wants(InteractionHint.None) && !_director.WantsShown);
        }

        [Test]
        public void ThePrompt_DwellsWithoutTheSlot_ClaimsItOnlyWhenReady_AndStartsTheMomentItIsGranted()
        {
            InteractionHint cut = Ready(InteractionKind.Salvage);
            Run(Frame, cut, slotGranted: false);
            Assert.IsFalse(_director.ReadyToShow, "mid-dwell the prompt does not claim the slot: the line stays");

            Run(_settings.Find(InteractionKind.Salvage).DwellSeconds + 0.1f, cut, slotGranted: false);
            Assert.AreEqual(InteractionKind.None, _director.Displayed, "no start without the slot");
            Assert.IsTrue(_director.ReadyToShow, "the dwell is done: it claims the slot");
            Assert.AreEqual(0, _ledger.Shown(InteractionKind.Salvage));

            _director.Step(Frame, true, cut, _chip.IsHidden, true);
            Assert.IsTrue(_director.Started, "granted, it starts on that very step");
            Assert.AreEqual(InteractionKind.Salvage, _director.Displayed);
        }

        [Test]
        public void APrompt_IsRetired_OnceTheActionIsLearned()
        {
            for (int i = 0; i < _settings.Find(InteractionKind.Salvage).UsesToRetire; i++)
            {
                _director.NotifyUsed(InteractionKind.Salvage);
            }

            Run(60f, Ready(InteractionKind.Salvage));
            Assert.AreEqual(0, _ledger.Shown(InteractionKind.Salvage),
                "someone who already digs is not taught to dig");
        }

        [Test]
        public void TheScanPrompt_NamesInteract_ShowsBeforeTheFirstScan_AndNeverAfterEvenAcrossASave()
        {
            PromptEntry scan = _settings.Find(InteractionKind.Scan);
            Assert.IsNotNull(scan, "the first fragment scan is taught");
            Assert.AreEqual(RoverAction.Excavate, scan.Action, "a scan is held with Interact");
            Assert.AreEqual(1, scan.UsesToRetire, "one completed scan and the player knows how to scan");

            InteractionHint hint = Ready(InteractionKind.Scan);
            Assert.IsTrue(_director.Wants(hint), "offered before the first scan");
            Run(scan.DwellSeconds + 0.1f, hint);
            Assert.AreEqual(InteractionKind.Scan, _director.Displayed);
            Assert.IsTrue(_director.WantsShown);

            _director.Yield(InteractionKind.Scan);
            Assert.IsFalse(_director.WantsShown, "starting the sweep hides the chip while the beam draws");
            Assert.IsTrue(_ledger.ShouldTeach(InteractionKind.Scan), "letting go early has not yet completed a scan");

            _director.NotifyUsed(InteractionKind.Scan);
            Assert.AreEqual(1, _ledger.Used(InteractionKind.Scan));
            Assert.IsFalse(_ledger.ShouldTeach(InteractionKind.Scan), "the first completed scan retires the prompt");
            Assert.IsFalse(_director.Wants(hint), "never offered again");
            Run(_settings.RepeatCooldown + 60f, hint);
            Assert.AreEqual(InteractionKind.None, _director.Displayed);
            Assert.AreEqual(1, _ledger.Shown(InteractionKind.Scan));

            string json = JsonUtility.ToJson(_ledger.Capture());
            StringAssert.Contains("\"Scan\"", json, "saved by kind name in the prompt ledger");
            var restoredLedger = new PromptLedger(_settings);
            restoredLedger.Restore(JsonUtility.FromJson<PromptsSaveData>(json));
            Assert.AreEqual(1, restoredLedger.Used(InteractionKind.Scan));
            Assert.IsFalse(restoredLedger.ShouldTeach(InteractionKind.Scan), "still retired after loading the save");

            var restoredDirector = new PromptDirector(_settings, restoredLedger);
            var restoredChip = new Fade(_settings.Reveal);
            Assert.IsFalse(restoredDirector.Wants(hint));
            for (float t = 0f; t < 60f; t += Frame)
            {
                restoredDirector.Step(Frame, true, hint, restoredChip.IsHidden, true);
                restoredChip.Set(restoredDirector.WantsShown);
                restoredChip.Step(Frame);
            }

            Assert.AreEqual(InteractionKind.None, restoredDirector.Displayed, "never offered after loading the save");
        }

        [Test]
        public void EveryPrompt_IsLearnedAfterTheDefaultNumberOfUses()
        {
            foreach (PromptEntry entry in _settings.Entries)
            {
                Assert.AreEqual(PromptEntry.DefaultUsesToRetire, entry.UsesToRetire, entry.Kind.ToString());
            }
        }

        [Test]
        public void TheShippedTuning_LetsEveryPromptBeLearned()
        {
            var tuning = AssetDatabase.LoadAssetAtPath<UiTuning>(UiAssetPaths.Tuning);
            Assert.IsNotNull(tuning, UiAssetPaths.Tuning);
            Assert.IsNotNull(tuning.Prompts.Find(InteractionKind.Scan), "the shipped tuning includes the scan prompt");
            foreach (PromptEntry entry in tuning.Prompts.Entries)
            {
                Assert.That(entry.UsesToRetire, Is.InRange(1, 10),
                    $"{entry.Kind}: an entry saved before the per-prompt limit existed reads its default");
            }
        }

        [Test]
        public void OnlyOnePrompt_AtATime_TheNextWaitsForTheFadeOut()
        {
            Run(1f, Ready(InteractionKind.Salvage));
            Assert.AreEqual(InteractionKind.Salvage, _director.Displayed);

            InteractionHint build = Ready(InteractionKind.Build);
            _director.Step(Frame, true, build, _chip.IsHidden, true);
            _chip.Set(_director.WantsShown);
            _chip.Step(Frame);
            Assert.AreEqual(InteractionKind.Salvage, _director.Displayed, "the old prompt fades before the new one");
            Assert.IsFalse(_director.WantsShown);

            Run(_settings.Reveal.FadeOut + _settings.Find(InteractionKind.Build).DwellSeconds + 0.1f, build);
            Assert.AreEqual(InteractionKind.Build, _director.Displayed);
            Assert.IsTrue(_director.WantsShown);
        }

        [Test]
        public void Ledger_RoundTripsThroughItsSaveData_ByKindName()
        {
            _ledger.RecordShown(InteractionKind.Build);
            _ledger.RecordShown(InteractionKind.Build);
            _ledger.RecordUsed(InteractionKind.Deconstruct);
            string json = JsonUtility.ToJson(_ledger.Capture());
            StringAssert.Contains("\"Build\"", json, "saved by name, not by enum number");

            var restored = new PromptLedger(_settings);
            restored.Restore(JsonUtility.FromJson<PromptsSaveData>(json));
            Assert.AreEqual(2, restored.Shown(InteractionKind.Build));
            Assert.AreEqual(1, restored.Used(InteractionKind.Deconstruct));
            Assert.AreEqual(0, restored.Shown(InteractionKind.Salvage));
        }

        [Test]
        public void Ledger_SkipsKindsThisBuildDoesNotKnow()
        {
            var data = new PromptsSaveData
            {
                kinds = new[]
                {
                    new PromptCountData { kind = "Ping", shown = 3, used = 1 },
                    new PromptCountData { kind = "Scan", shown = 1, used = 0 },
                },
            };

            _ledger.Restore(data);
            Assert.AreEqual(1, _ledger.Shown(InteractionKind.Scan));
        }

        [Test]
        public void Settings_DefaultTable_IsValid_AndTeachesTheOpeningsVerbs()
        {
            Assert.IsNull(_settings.Validate());
            InteractionKind[] taught =
            {
                InteractionKind.Salvage, InteractionKind.PickUp, InteractionKind.Build, InteractionKind.Scan,
                InteractionKind.Deconstruct,
            };
            Assert.AreEqual(taught.Length, _settings.Entries.Length, "no prompt teaches a verb the opening lacks");
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

        private void Run(float seconds, InteractionHint hint, bool gateOpen = true, bool slotGranted = true)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                _director.Step(Frame, gateOpen, hint, _chip.IsHidden, slotGranted);
                _chip.Set(_director.WantsShown);
                _chip.Step(Frame);
            }
        }
    }
}
