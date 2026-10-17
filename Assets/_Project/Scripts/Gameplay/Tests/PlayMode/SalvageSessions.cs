using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;
using MoonProject.Testing;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// Scripted sessions for salvage (docs/features/M3-13) on the Kestrel's real components behind fakes: cutting a
    /// piece with the beam (progress kept when the hold lets go, ruling 4), the piece folding into the stock with the
    /// site's melody, taken pieces that stay taken after a reboot, the hatch opening for a bundle with room (and a full
    /// hold holding the piece on its wreck), and a warm emitter for the next cut at the same site.
    /// </summary>
    public sealed class SalvageSessions : InputTestFixture
    {
        /// <summary>Seconds a held press may take to be seen (a slow first frame must not eat it).</summary>
        private const float PressTimeout = 1f;

        /// <summary>Seconds from a press to the beam firing on any piece: the slowest fresh charge (three stages)
        /// plus the press's own allowance.</summary>
        private float IgnitionTimeout => _fixture.Gameplay.Cutter.Plan(3, false).TotalSeconds + PressTimeout;

        /// <summary>
        /// Seconds from a press until a nearly cut piece is in the stock: the slowest charge, the last of its cut, and
        /// its flight into 07 (the arrivals are spaced, so allow a second one's interval too).
        /// </summary>
        private float PieceTakenTimeout => IgnitionTimeout + _fixture.SalvageTuning.FlightDuration +
                                           2f * _fixture.SalvageTuning.MinArrivalInterval + PressTimeout;

        private InputActionAsset _controls;
        private GameplayFixture _fixture;
        private Keyboard _keyboard;

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _controls = BootstrapHarness.LoadControlsCopy();
        }

        public override void TearDown()
        {
            _fixture?.Dispose();
            Object.Destroy(_controls);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator Cut_KeepsProgressWhenReleased_ThenThePieceFoldsIntoTheStock()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            SalvageField salvage = _fixture.Gameplay.Salvage;
            SalvagePiece metal = _fixture.FindSite("kestrel").Find(0);
            Assert.AreEqual(SalvageMaterial.Metal, metal.Material);
            Assert.AreEqual(_fixture.SalvageTuning.Size(PieceSize.Small).Units, metal.Units, "a small piece");
            Assert.AreEqual(Layers.Prop, metal.Body.gameObject.layer, "the wreck is solid");
            yield return FaceCut(metal);
            Assert.AreSame(metal, salvage.Candidate, "aimed at and within reach");
            Assert.AreEqual(InteractionKind.Salvage, _fixture.Gameplay.Hints.Primary.Kind, "the hold-to-cut prompt");
            Assert.IsTrue(_fixture.Rover.TryGetGaze(salvage, out _, out int glance));
            Assert.AreEqual(GazePriorities.Interest, glance, "07 looks at the piece it could cut");
            var status = _fixture.Bootstrap.Context.Get<ISalvageStatus>();
            Assert.AreSame(salvage, status, "registered for the UI's hold ring and the audio's beam");
            Assert.IsTrue(status.HasTarget);
            Assert.Less(Vector3.Distance(metal.CutPosition, status.CutPoint), 1e-4f);
            Assert.IsFalse(status.IsCutting);
            Assert.AreEqual(0f, status.Progress);
            Assert.AreEqual(SalvageMaterial.Metal, status.Material);

            Press(_keyboard.eKey);
            yield return Waits.Until(() => _fixture.Events.SalvageCutStarted.Count > 0, IgnitionTimeout);
            Assert.AreEqual(1, _fixture.Events.SalvageCutStarted.Count);
            Assert.AreEqual(SalvageMaterial.Metal, _fixture.Events.SalvageCutStarted[0].Value.Material);
            Assert.Less(Vector3.Distance(metal.CutPosition, _fixture.Events.SalvageCutStarted[0].Value.Position),
                1e-3f, "sparks fly where the beam meets the cut");
            Assert.AreEqual(1, _fixture.Rover.HoldStillCount, "holding the beam asks 07 to hold still");
            Assert.IsTrue(_fixture.Rover.TryGetGaze(salvage, out _, out int focus));
            Assert.AreEqual(GazePriorities.Focus, focus);
            Assert.IsTrue(status.IsCutting);

            yield return new WaitForSeconds(0.4f * metal.CutSeconds);
            Assert.Greater(salvage.BeamLevel, 0.8f, "the beam is on");
            Assert.Greater(salvage.Sparks.ParticleCount, 3, "and the cut sparks");
            _fixture.Capture("30-salvage-cut");
            Release(_keyboard.eKey);
            yield return null;
            yield return null;
            Assert.AreEqual(1, _fixture.Events.SalvageCutStopped.Count);
            Assert.IsFalse(_fixture.Events.SalvageCutStopped[0].Value.Completed);
            Assert.AreEqual(0, _fixture.Rover.HoldStillCount, "07 is free again");
            float kept = metal.Progress;
            Assert.That(kept, Is.InRange(0.2f, 0.6f));
            Assert.IsFalse(status.IsCutting, "the hold let go");
            Assert.AreEqual(kept, status.Progress, "the ring still shows how far the cut came");
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(kept, metal.Progress, "progress is kept, nothing resets");
            Assert.AreEqual(SalvagePieceState.Attached, metal.State);

            Press(_keyboard.eKey);
            yield return Waits.Until(() => _fixture.Events.SalvageCutStopped.Count > 1, metal.CutSeconds + 2f);
            Release(_keyboard.eKey);
            Assert.AreEqual(2, _fixture.Events.SalvageCutStarted.Count);
            Assert.IsTrue(_fixture.Events.SalvageCutStopped[1].Value.Completed);
            Assert.AreEqual(SalvagePieceState.Breaking, metal.State, "it comes away from the wreck");
            yield return Waits.Until(() => _fixture.Events.MaterialSalvaged.Count > 0, 5f);
            float secondCut = _fixture.Events.SalvageCutStopped[1].Time - _fixture.Events.SalvageCutStarted[1].Time;
            Assert.AreEqual(metal.CutSeconds * (1f - kept), secondCut, 0.25f, "the second hold only finishes the rest");
            Assert.AreEqual(1, _fixture.Events.MaterialSalvaged.Count, "it folds into 07");
            MaterialSalvaged salvaged = _fixture.Events.MaterialSalvaged[0].Value;
            Assert.AreEqual(SalvageMaterial.Metal, salvaged.Material);
            Assert.AreEqual(metal.Units, salvaged.Amount);
            Assert.AreEqual("site.kestrel", salvaged.SiteId);
            Assert.AreEqual(0, salvaged.ComboStep, "the first piece of a chain");
            Assert.AreEqual(metal.Units, _fixture.Gameplay.Materials.Metal);
            int salvagedAt = _fixture.Events.Order.IndexOf(nameof(MaterialSalvaged));
            Assert.AreEqual(nameof(MaterialsChanged), _fixture.Events.Order[salvagedAt + 1], "then the stock grows");
            Assert.AreEqual(SalvagePieceState.Taken, metal.State);
            Assert.IsFalse(metal.Body.gameObject.activeSelf, "gone from the wreck");

            SalvagePiece optics = _fixture.FindSite("kestrel").Find(2);
            optics.Progress = 0.95f;
            yield return FaceCut(optics);
            Press(_keyboard.eKey);
            yield return Waits.Until(() => _fixture.Events.MaterialSalvaged.Count > 1, 5f);
            Release(_keyboard.eKey);
            Assert.AreEqual(2, _fixture.Events.MaterialSalvaged.Count);
            Assert.AreEqual(1, _fixture.Events.MaterialSalvaged[1].Value.ComboStep, "the next piece climbs a step");
            Assert.AreEqual(SalvageMaterial.Optics, _fixture.Events.MaterialSalvaged[1].Value.Material);
        }

        [UnityTest]
        public IEnumerator TakenPieces_AreSaved_AndNeverRespawn_APartCutKeepsItsCut()
        {
            string slot = BootstrapHarness.NewTestSlot();
            _fixture = GameplayFixture.Boot(_controls, slot);
            yield return null;
            SalvageSite kestrel = _fixture.FindSite("kestrel");
            int last = kestrel.Pieces.Count - 1;
            for (int i = 0; i < last; i++)
            {
                SalvagePiece piece = kestrel.Pieces[i];
                piece.Progress = 0.97f;
                yield return FaceCut(piece);
                Press(_keyboard.eKey);
                yield return Waits.Until(() => piece.State == SalvagePieceState.Taken, PieceTakenTimeout);
                Release(_keyboard.eKey);
                Assert.AreEqual(SalvagePieceState.Taken, piece.State,
                    $"the Kestrel's piece {piece.Number} is cut and folds into 07 within its charge, cut and flight");
                yield return null;
            }

            Assert.AreEqual(last, _fixture.Events.MaterialSalvaged.Count);
            Assert.IsTrue(kestrel.Root.Find("Skeleton").gameObject.activeSelf, "the skeleton stays");
            Assert.IsFalse(kestrel.IsPickedClean, "one piece is still on the wreck");
            kestrel.Pieces[last].Progress = 0.5f;
            int total = _fixture.Gameplay.Materials.Total;
            Assert.IsTrue(_fixture.Bootstrap.Context.Get<ISaveService>().SaveNow());
            _fixture.Dispose(true);
            yield return null;

            _fixture = GameplayFixture.Boot(_controls, slot);
            yield return null;
            yield return new WaitForSeconds(0.5f);
            kestrel = _fixture.FindSite("kestrel");
            for (int i = 0; i < last; i++)
            {
                Assert.AreEqual(SalvagePieceState.Taken, kestrel.Pieces[i].State);
                Assert.IsFalse(kestrel.Pieces[i].Body.gameObject.activeSelf, "nothing respawns");
            }

            Assert.AreEqual(0.5f, kestrel.Pieces[last].Progress, 1e-4f, "a part-cut piece keeps its cut");
            Assert.AreEqual(total, _fixture.Gameplay.Materials.Total);
            Assert.AreEqual(0, _fixture.Events.MaterialSalvaged.Count, "a load is not salvage");
        }

        [UnityTest]
        public IEnumerator ABundle_OpensTheHatchWhenThereIsRoom_AndBumpsItShutWhenFull()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            var beats = new List<FeelBeat>();
            using var listening = _fixture.Bootstrap.Context.Events.Subscribe<FeelBeat>(beat =>
            {
                if (beat.Kind == FeelKind.Collect || beat.Kind == FeelKind.Cargo)
                {
                    beats.Add(beat);
                }
            });
            SalvagePiece metal = _fixture.FindSite("kestrel").Find(0);
            metal.Progress = 0.95f;
            yield return FaceCut(metal);
            Press(_keyboard.eKey);
            yield return Waits.Until(() => _fixture.Events.MaterialSalvaged.Count > 0, 6f);
            Release(_keyboard.eKey);
            FeelBeat open = beats.Find(beat => beat.Phase == FeelPhase.Anticipate);
            Assert.AreEqual(FeelKind.Collect, open.Kind, "M3-25 T1: the hatch opens for the flight");
            Assert.Greater(open.Seconds, 0f, "Seconds = the time to arrival");
            Assert.Less(beats.FindIndex(beat => beat.Phase == FeelPhase.Anticipate),
                beats.FindIndex(beat => beat.Kind == FeelKind.Collect && beat.Phase == FeelPhase.Reward),
                "open, then the Reward closes it");

            beats.Clear();
            MaterialStock stock = _fixture.Gameplay.Materials;
            stock.Collect(SalvageMaterial.Optics, stock.Hold.FreeUnits(SalvageMaterial.Optics));
            SalvagePiece optics = _fixture.FindSite("kestrel").Find(2);
            optics.Progress = 0.95f;
            yield return FaceCut(optics);
            Press(_keyboard.eKey);
            yield return Waits.Until(() => beats.Exists(beat => beat.Kind == FeelKind.Cargo), 6f);
            Release(_keyboard.eKey);
            Assert.IsFalse(beats.Exists(beat => beat.Phase == FeelPhase.Anticipate), "a full hold keeps it shut");
            Assert.AreEqual(FeelPhase.Impact, beats.Find(beat => beat.Kind == FeelKind.Cargo).Phase, "the bump");
        }

        [UnityTest]
        public IEnumerator ACut_BuildsItsStages_AndTheNextCutHereFindsTheEmitterWarm()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            var staged = new List<CutChargeStaged>();
            using var listening = _fixture.Bootstrap.Context.Events.Subscribe<CutChargeStaged>(staged.Add);
            SalvagePiece first = _fixture.FindSite("kestrel").Find(0);
            first.Progress = 0.95f;
            yield return FaceCut(first);
            Press(_keyboard.eKey);
            yield return Waits.Until(() => _fixture.Events.MaterialSalvaged.Count > 0, 6f);
            Release(_keyboard.eKey);
            Assert.AreEqual(0, staged[0].Stage, "stage 0 as the charge starts");
            Assert.IsFalse(staged[0].Warm, "the first cut braces");
            Assert.IsTrue(staged[staged.Count - 1].IsFullLock, "a small piece: one stage, the full lock");

            staged.Clear();
            SalvagePiece next = _fixture.FindSite("kestrel").Find(1);
            next.Progress = 0.95f;
            yield return FaceCut(next);
            Press(_keyboard.eKey);
            yield return Waits.Until(() => staged.Count > 0, 2f);
            Release(_keyboard.eKey);
            Assert.IsTrue(staged[0].Warm, "0.5.5 §5.1: a re-press at the same site within 6 s skips the brace");
        }

        /// <summary>Parks 07 in front of the piece's cut, looking at it, and lets the aim settle.</summary>
        private IEnumerator FaceCut(SalvagePiece piece)
        {
            Vector3 cut = piece.CutPosition;
            Vector3 away = piece.CutNormal;
            away.y = 0f;
            away = away.sqrMagnitude > 1e-4f ? away.normalized : Vector3.back;
            Vector3 park = cut + away * 4.5f;
            float yaw = Mathf.Atan2(-away.x, -away.z) * Mathf.Rad2Deg;
            _fixture.Rover.Place(new Vector3(park.x, 0f, park.z), yaw);
            _fixture.Rover.Aim(cut + away * 9f + Vector3.up * 3.5f, cut);
            yield return null;
            yield return null;
        }
    }
}
