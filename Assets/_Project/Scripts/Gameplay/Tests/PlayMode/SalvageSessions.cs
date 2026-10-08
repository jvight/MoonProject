using System.Collections;
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
    /// Scripted sessions for salvage (docs/features/M3-13) on the real components behind fakes: cutting a piece with
    /// the beam (progress kept when the hold lets go, ruling 4), the piece folding into the stock with the site's
    /// melody, a drag piece tethered clear and then cut, a lost drag piece floating back beside its site (ruling 1),
    /// the hold shared with the dig at a site's heart, Kestrel-3's trail bits drawn in by driving through, and a
    /// picked-clean site that stays empty after a reboot.
    /// </summary>
    public sealed class SalvageSessions : InputTestFixture
    {
        /// <summary>Seconds a held press may take to be seen (a slow first frame must not eat it).</summary>
        private const float PressTimeout = 1f;

        private InputActionAsset _controls;
        private GameplayFixture _fixture;
        private Keyboard _keyboard;
        private Mouse _mouse;

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _mouse = InputSystem.AddDevice<Mouse>();
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
            SalvagePiece metal = _fixture.FindSite("depot").Find(0);
            Assert.AreEqual(SalvageMaterial.Metal, metal.Material);
            Assert.AreEqual(_fixture.SalvageTuning.SmallYield, metal.Units, "a small piece");
            Assert.AreEqual(Layers.Prop, metal.Body.gameObject.layer, "the wreck is solid");
            yield return FaceCut(metal);
            Assert.AreSame(metal, salvage.Candidate, "aimed at and within reach");
            Assert.IsNull(_fixture.Gameplay.Excavation.Candidate);
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
            yield return Waits.Until(() => _fixture.Events.SalvageCutStarted.Count > 0, PressTimeout);
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
            Assert.AreEqual("site.depot", salvaged.SiteId);
            Assert.AreEqual(0, salvaged.ComboStep, "the first piece of a chain");
            Assert.AreEqual(metal.Units, _fixture.Gameplay.Materials.Metal);
            int salvagedAt = _fixture.Events.Order.IndexOf(nameof(MaterialSalvaged));
            Assert.AreEqual(nameof(MaterialsChanged), _fixture.Events.Order[salvagedAt + 1], "then the stock grows");
            Assert.AreEqual(SalvagePieceState.Taken, metal.State);
            Assert.IsFalse(metal.Body.gameObject.activeSelf, "gone from the wreck");

            SalvagePiece optics = _fixture.FindSite("depot").Find(2);
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
        public IEnumerator DragPiece_IsTetheredClear_ThenCut()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            SalvageField salvage = _fixture.Gameplay.Salvage;
            TetherSystem tether = _fixture.Gameplay.Tether;
            SalvagePiece hatch = _fixture.FindSite("depot").Find(3);
            Assert.IsTrue(hatch.IsDrag);
            Assert.IsFalse(hatch.IsCuttable, "it must come clear of the wreck first");
            Vector3 hang = hatch.Drag.Position;

            _fixture.Rover.Place(new Vector3(hang.x, 0f, hang.z - 6f), 0f);
            Vector3 camera = _fixture.Rover.Camera.transform.position;
            _fixture.Rover.Aim(camera, hang);
            yield return null;
            yield return null;
            Assert.AreNotSame(hatch, salvage.Candidate, "the beam cannot cut it where it hangs");
            Assert.AreEqual(TetherAimState.Hovering, tether.State, "but the tether can latch on");
            Assert.IsTrue(_fixture.Gameplay.Hints.TryGet(InteractionKind.Tether, out InteractionHint hint));
            Assert.Less(Vector3.Distance(hint.Position, hang), 1e-3f);

            Press(_mouse.rightButton, queueEventOnly: true);
            yield return Waits.Until(() => _fixture.Events.TetherAttached.Count > 0, PressTimeout);
            Assert.AreEqual(1, _fixture.Events.TetherAttached.Count);
            Assert.AreEqual(_fixture.SalvageTuning.DragMass, _fixture.Events.TetherAttached[0].Value.Mass, 1e-4f);
            Assert.IsTrue(hatch.Drag.IsTethered);
            Assert.IsFalse(hatch.Drag.Body.isKinematic, "free to be pulled");

            Vector3 start = _fixture.Rover.Position;
            float began = Time.time;
            while (_fixture.Events.TetherReleased.Count == 0 && Time.time - began < 10f)
            {
                _fixture.Rover.MoveTo(start + Vector3.back * Mathf.Min(12f, 2f * (Time.time - began)), 180f);
                yield return null;
            }

            Assert.AreEqual(1, _fixture.Events.TetherReleased.Count, "pulled clear, the tether lets go by itself");
            Assert.IsFalse(_fixture.Events.TetherReleased[0].Value.Snapped, "softly: the tow did its job");
            Assert.AreEqual(SalvagePieceState.Loose, hatch.State);
            Assert.IsTrue(hatch.IsCuttable);
            Assert.GreaterOrEqual(SurfaceRules.HorizontalDistance(hatch.Drag.Position, hang),
                _fixture.SalvageTuning.DragClearance - 0.01f);
            yield return null;
            Assert.AreEqual(1, _fixture.Events.TetherAttached.Count, "no re-grab while the button is still held");
            Release(_mouse.rightButton);
            yield return new WaitForSeconds(1.5f);

            hatch.Progress = 0.9f;
            yield return FaceCut(hatch);
            Assert.AreSame(hatch, salvage.Candidate, "now the beam can cut it");
            Press(_keyboard.eKey);
            yield return Waits.Until(() => _fixture.Events.MaterialSalvaged.Count > 0, 5f);
            Release(_keyboard.eKey);
            Assert.AreEqual(1, _fixture.Events.MaterialSalvaged.Count);
            Assert.AreEqual(_fixture.SalvageTuning.DragYield, _fixture.Events.MaterialSalvaged[0].Value.Amount);
            Assert.AreEqual(SalvagePieceState.Taken, hatch.State);
        }

        [UnityTest]
        public IEnumerator DragPiece_LostOffTheFloor_FloatsBackBesideItsSite()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            SalvageTuning tuning = _fixture.SalvageTuning;
            SalvageSite depot = _fixture.FindSite("depot");
            SalvagePiece hatch = depot.Find(3);

            // Past the edge of the drivable floor (the flat world's floor runs on beyond it).
            var lost = new Vector3(250f, 1f, 250f);
            Assert.IsFalse(_fixture.World.IsDrivable(lost.x, lost.z));
            hatch.Drag.RestoreLoose(lost, Quaternion.Euler(20f, 40f, 0f));
            hatch.State = SalvagePieceState.Loose;
            yield return Waits.Until(() => hatch.State == SalvagePieceState.Returning, tuning.ReturnDelay + 3f);
            Assert.AreEqual(SalvagePieceState.Returning, hatch.State, "resting off the floor, it starts back");
            Assert.IsFalse(hatch.IsCuttable);
            Assert.IsFalse(hatch.Drag.IsTetherable, "nothing grabs it while it floats");
            SalvagePieceSaveData saved = SavedPiece(depot, hatch);
            Assert.IsTrue(saved.loose, "a save mid-float keeps it");
            Assert.Less(Vector3.Distance(hatch.Drag.ReturnEnd, saved.position), 1e-3f, "where it will rest");

            float began = Time.time;
            yield return Waits.Until(() => hatch.State != SalvagePieceState.Returning, 30f);
            Assert.AreEqual(SalvagePieceState.Loose, hatch.State, "set down loose again, clear of its wreck");
            Assert.GreaterOrEqual(Time.time - began, tuning.ReturnDuration * 0.9f, "a gentle float, not a snap");
            yield return new WaitForSeconds(2f);
            AssertBesideItsSite(depot, hatch, tuning);
            Assert.IsTrue(hatch.IsCuttable, "and ready for the beam");

            // Sunk under the surface (it fell through): it starts back at once.
            hatch.Drag.RestoreLoose(new Vector3(6f, -3f, 18f), Quaternion.identity);
            yield return null;
            yield return null;
            Assert.AreEqual(SalvagePieceState.Returning, hatch.State, "under the floor, it starts back straight away");
            yield return Waits.Until(() => hatch.State != SalvagePieceState.Returning, 30f);
            yield return new WaitForSeconds(2f);
            AssertBesideItsSite(depot, hatch, tuning);
            Assert.Less(SurfaceRules.HorizontalDistance(hatch.Drag.Position, new Vector3(6f, 0f, 18f)),
                SurfaceRules.HorizontalDistance(hatch.Drag.Position, lost), "on the side it was lost toward");
        }

        [UnityTest]
        public IEnumerator Hold_GoesToTheHeart_OrThePiece_Whichever07LooksAt()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            SalvageSite depot = _fixture.FindSite("depot");
            Relic walkman = depot.Relics[0];
            Assert.AreEqual("cassette_player", walkman.Definition.Id, "the walkman rests in the depot's heart");
            Assert.Less(SurfaceRules.HorizontalDistance(walkman.Site.Position, depot.Heart), 1e-3f);
            Assert.Greater(walkman.BuriedPosition.y + walkman.RestHeight, depot.Heart.y,
                "its top peeks out of the dust (the stand-in is as tall above its pivot as below)");

            _fixture.Rover.Place(depot.Heart + new Vector3(0f, 0f, -4f), 0f);
            Vector3 camera = depot.Heart + new Vector3(0f, 3.5f, -10f);
            _fixture.Rover.Aim(camera, depot.Heart);
            yield return null;
            yield return null;
            Assert.AreSame(walkman, _fixture.Gameplay.Excavation.Candidate, "looking at the heart: dig");
            Assert.IsNull(_fixture.Gameplay.Salvage.Candidate);

            SalvagePiece wiring = depot.Find(1);
            _fixture.Rover.Aim(camera, wiring.CutPosition);
            yield return null;
            yield return null;
            Assert.AreSame(wiring, _fixture.Gameplay.Salvage.Candidate, "looking at a piece: cut");
            Assert.IsNull(_fixture.Gameplay.Excavation.Candidate);

            Press(_keyboard.eKey);
            yield return Waits.Until(() => _fixture.Events.SalvageCutStarted.Count > 0, PressTimeout);
            Assert.AreEqual(0, _fixture.Events.ExcavationStarted.Count, "one hold, one job");
            Release(_keyboard.eKey);
        }

        [UnityTest]
        public IEnumerator TrailBits_FoldInto07_AsItDrivesThrough()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            SalvageField salvage = _fixture.Gameplay.Salvage;
            Assert.AreEqual(5, salvage.TrailCount);
            _fixture.Rover.Place(new Vector3(-40f, 0f, 40f), 0f);
            yield return null;
            yield return null;
            Assert.AreEqual(5, salvage.Glints.Drawn, "the trail glints from afar");
            _fixture.Capture("31-salvage-trail");

            Vector3 from = salvage.TrailBitPosition(0);
            Vector3 to = salvage.TrailBitPosition(salvage.TrailCount - 1);
            float yaw = Mathf.Atan2(to.x - from.x, to.z - from.z) * Mathf.Rad2Deg;
            float began = Time.time;
            const float driveSeconds = 10f;
            while (Time.time - began < driveSeconds)
            {
                float t = (Time.time - began) / driveSeconds;
                Vector3 point = Vector3.Lerp(from, to, t);
                _fixture.Rover.MoveTo(new Vector3(point.x, 0f, point.z), yaw);
                yield return null;
            }

            yield return new WaitForSeconds(2f);
            Assert.AreEqual(5, _fixture.Events.MaterialSalvaged.Count, "every bit folds in");
            int metal = 0;
            int wiring = 0;
            int optics = 0;
            for (int i = 0; i < 5; i++)
            {
                MaterialSalvaged bit = _fixture.Events.MaterialSalvaged[i].Value;
                Assert.AreEqual(WorldAnchorIds.KestrelTrail, bit.SiteId);
                Assert.AreEqual(_fixture.SalvageTuning.TrailYield, bit.Amount);
                Assert.AreEqual(i, bit.ComboStep, "a run of bits climbs the melody");
                metal += bit.Material == SalvageMaterial.Metal ? bit.Amount : 0;
                wiring += bit.Material == SalvageMaterial.Wiring ? bit.Amount : 0;
                optics += bit.Material == SalvageMaterial.Optics ? bit.Amount : 0;
                Assert.IsFalse(salvage.IsTrailBitWaiting(i));
            }

            Assert.AreEqual(metal, _fixture.Gameplay.Materials.Metal);
            Assert.AreEqual(wiring, _fixture.Gameplay.Materials.Wiring);
            Assert.AreEqual(optics, _fixture.Gameplay.Materials.Optics);
            Assert.AreEqual(0, salvage.Glints.Drawn, "no glints left");
        }

        [UnityTest]
        public IEnumerator PickedClean_IsSaved_AndNeverRespawns()
        {
            string slot = BootstrapHarness.NewTestSlot();
            _fixture = GameplayFixture.Boot(_controls, slot);
            yield return null;
            SalvageSite garage = _fixture.FindSite("garage");
            for (int i = 0; i < garage.Pieces.Count; i++)
            {
                garage.Pieces[i].Progress = 0.97f;
            }

            for (int i = 0; i < garage.Pieces.Count; i++)
            {
                int salvaged = _fixture.Events.MaterialSalvaged.Count;
                yield return FaceCut(garage.Pieces[i]);
                Press(_keyboard.eKey);
                yield return Waits.Until(() => _fixture.Events.MaterialSalvaged.Count > salvaged, 5f);
                Release(_keyboard.eKey);
                yield return null;
            }

            Assert.IsTrue(garage.IsPickedClean);
            Assert.AreEqual(garage.Pieces.Count, _fixture.Events.MaterialSalvaged.Count);
            Assert.IsTrue(garage.Root.Find("Skeleton").gameObject.activeSelf, "the skeleton stays");
            Assert.IsTrue(garage.AnswersSonar, "its heart still holds the duck and the gnome");
            SalvagePiece partCut = _fixture.FindSite("drill").Find(1);
            partCut.Progress = 0.5f;
            int total = _fixture.Gameplay.Materials.Total;
            Assert.IsTrue(_fixture.Bootstrap.Context.Get<ISaveService>().SaveNow());
            _fixture.Dispose(true);
            yield return null;

            _fixture = GameplayFixture.Boot(_controls, slot);
            yield return null;
            yield return new WaitForSeconds(0.5f);
            garage = _fixture.FindSite("garage");
            Assert.IsTrue(garage.IsPickedClean, "picked clean for good");
            foreach (SalvagePiece piece in garage.Pieces)
            {
                Assert.AreEqual(SalvagePieceState.Taken, piece.State);
                Assert.IsFalse(piece.Body.gameObject.activeSelf, "nothing respawns");
            }

            Assert.AreEqual(0.5f, _fixture.FindSite("drill").Find(1).Progress, 1e-4f, "a part-cut piece keeps its cut");
            Assert.AreEqual(total, _fixture.Gameplay.Materials.Total);
            Assert.AreEqual(0, _fixture.Events.MaterialSalvaged.Count, "a load is not salvage");
        }

        private SalvagePieceSaveData SavedPiece(SalvageSite site, SalvagePiece piece)
        {
            foreach (SalvageSiteSaveData saved in _fixture.Gameplay.Salvage.Capture().sites)
            {
                if (saved.id != site.Id)
                {
                    continue;
                }

                foreach (SalvagePieceSaveData pieceData in saved.pieces)
                {
                    if (pieceData.number == piece.Number)
                    {
                        return pieceData;
                    }
                }
            }

            Assert.Fail($"{site.Id} piece {piece.Number} is not in the save.");
            return null;
        }

        private void AssertBesideItsSite(SalvageSite site, SalvagePiece piece, SalvageTuning tuning)
        {
            Vector3 rest = piece.Drag.Position;
            Assert.IsTrue(_fixture.World.IsDrivable(rest.x, rest.z), "back on the drivable floor");
            Assert.That(rest.y, Is.InRange(0f, 1.5f), "resting on the ground");
            Assert.That(SurfaceRules.HorizontalDistance(rest, site.Position),
                Is.InRange(site.Radius, site.Radius + tuning.ReturnMargin + 1.5f),
                "beside its site, just outside the footprint");
        }

        /// <summary>Parks 07 in front of the piece's cut, looking at it, and lets the aim settle.</summary>
        private IEnumerator FaceCut(SalvagePiece piece)
        {
            Vector3 cut = piece.CutPosition;
            Vector3 away = piece.State == SalvagePieceState.Loose ? cut - piece.Site.Position : piece.CutNormal;
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
