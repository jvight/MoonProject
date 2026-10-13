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
    /// Scripted sessions for scanning (docs/design/0.5.5-owner-feedback.md §1) on the real components behind the fake
    /// rover: the garage's two Cargo Cradle fragments scanned one by one (brackets, the hold, the beats, the dissolve)
    /// complete the blueprint and the plotter in the awake bay draws it; letting go keeps the progress, also across a
    /// load; another E verb in reach comes first; a known blueprint's fragment pays Metal; the Focus Lens scans faster
    /// and shows brackets farther; and a save from before scanning keeps its learned blueprints, their fragments gone.
    /// </summary>
    public sealed class ScanSessions : InputTestFixture
    {
        private const string CargoCradle = "rover.cargo_cradle";
        private const string CellPack = "rover.cell_pack";
        private const string Garage = "site.garage";

        /// <summary>Seconds a held press may take to be seen (a slow first frame must not eat it).</summary>
        private const float PressTimeout = 1f;

        /// <summary>Metres 07 parks from a fragment, looking at it.</summary>
        private const float Park = 4f;

        private readonly List<FeelBeat> _beats = new List<FeelBeat>();
        private InputActionAsset _controls;
        private GameplayFixture _fixture;
        private Keyboard _keyboard;
        private System.IDisposable _listening;

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _controls = BootstrapHarness.LoadControlsCopy();
        }

        public override void TearDown()
        {
            _listening?.Dispose();
            _fixture?.Dispose();
            Object.Destroy(_controls);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator TheGaragesTwoCradleFragments_CompleteTheBlueprint_ThePlotterDrawsIt_AndALoadKeepsIt()
        {
            string slot = BootstrapHarness.NewTestSlot();
            Boot(slot);
            yield return null;
            ScanField scans = _fixture.Gameplay.Scans;
            ScanFragment first = scans.Find(CargoCradle + ".0");
            ScanFragment second = scans.Find(CargoCradle + ".1");
            Assert.IsNotNull(first, "the garage's fragments lie on its sockets");
            Assert.AreEqual(Garage, first.HostId);
            Assert.AreEqual(2, first.Count, "a rack: two fragments (§1.3)");
            Assert.AreEqual(ScanSize.Medium, first.Size);
            Assert.AreEqual(CargoCradle, scans.KitAt(Garage));
            Assert.AreEqual(2, scans.Left(Garage));
            Assert.IsFalse(Brackets(first), "far from 07: no brackets");
            Assert.AreEqual(18, scans.Fragments.Count, "every set lies in the world (§1.3)");

            yield return Face(first);
            Assert.IsTrue(Brackets(first), "within reach of 07's eye: its corner brackets");
            Assert.AreSame(first, scans.Candidate);
            Assert.AreEqual(InteractionKind.Scan, _fixture.Gameplay.Hints.Primary.Kind, "the Scan chip");
            Assert.AreEqual(0, _fixture.Rover.HoldStillCount);

            float seconds = _fixture.ScanTuning.Seconds(ScanSize.Medium);
            Press(_keyboard.eKey);
            yield return Waits.Until(() => scans.Scanning == first, PressTimeout);
            Assert.AreSame(first, scans.Scanning, "holding E scans it");
            Assert.AreEqual(1, _fixture.Rover.HoldStillCount, "07 eases to a stop");
            Assert.AreEqual(1, _fixture.Events.ScanStarted.Count);
            Assert.AreEqual(FeelPhase.Anticipate, _beats[0].Phase, "the lens opens");
            Assert.AreEqual(seconds, _beats[0].Seconds, 0.05f, "the whole scan lies ahead");
            yield return Waits.Until(() => first.Scanned, seconds + PressTimeout);
            yield return null;
            Assert.IsNull(scans.Scanning, "E must be let go before the next scan, though the next one is in view");
            Release(_keyboard.eKey);
            Assert.IsTrue(first.Scanned);
            Assert.AreEqual(0, _fixture.Rover.HoldStillCount, "done: 07 is free");
            CollectionAssert.AreEqual(new[] { 0.25f, 0.5f, 0.75f, 1f }, Strengths(FeelPhase.Act),
                "an Act at each quarter");
            Assert.AreEqual(1, Strengths(FeelPhase.Reward).Count, "the reward at its scan point");
            Assert.AreEqual(1, _fixture.Events.FragmentScanned.Count);
            FragmentScanned scanned = _fixture.Events.FragmentScanned[0].Value;
            Assert.AreEqual(CargoCradle + ".0", scanned.FragmentId);
            Assert.AreEqual(0, scanned.Index, "fragment 1 of 2");
            Assert.AreEqual(2, scanned.Count);
            Assert.IsFalse(scanned.Known);
            Assert.AreEqual(0, scanned.Metal, "new knowledge pays no Metal");
            Assert.IsEmpty(_fixture.Events.BlueprintCompleted, "one of two: not yet");
            Assert.IsFalse(_fixture.Gameplay.Blueprints.IsPinned(CargoCradle));
            Assert.AreEqual(1, scans.Left(Garage));
            Assert.AreNotSame(first, scans.Candidate, "a scanned fragment is no candidate");
            yield return Waits.Until(() => !first.Root.gameObject.activeSelf,
                _fixture.ScanTuning.DissolveSeconds + _fixture.ScanTuning.OutlineSeconds + 1f);
            Assert.IsFalse(first.Root.gameObject.activeSelf, "dissolved, its dust outline faded: nothing is left");

            yield return Face(second);
            Press(_keyboard.eKey);
            yield return Waits.Until(() => second.Scanned, seconds + PressTimeout);
            Release(_keyboard.eKey);
            Assert.AreEqual(1, _fixture.Events.FragmentScanned[1].Value.Index, "fragment 2 of 2");
            Assert.AreEqual(1, _fixture.Events.BlueprintCompleted.Count, "the set's last fragment completes it");
            Assert.AreEqual(CargoCradle, _fixture.Events.BlueprintCompleted[0].Value.BlueprintId);
            Assert.IsTrue(_fixture.Gameplay.Blueprints.IsPinned(CargoCradle), "the bay can build it");
            Assert.AreEqual(0, scans.Left(Garage));

            BlueprintPlotter plotter = _fixture.Gameplay.Plotter;
            Assert.IsFalse(plotter.Drawing, "the bay sleeps: its sheet waits");
            _fixture.PowerTo(2);
            yield return Waits.Until(() => plotter.Drawing, 3f);
            Assert.AreEqual(CargoCradle, plotter.DrawingKit, "the woken bay draws the learned sheet");
            yield return Waits.Until(() => !plotter.Drawing, _fixture.ProgressionTuning.PlotterDrawSeconds + 2f);
            Assert.IsTrue(plotter.ShowsSheet(CargoCradle), "drawn, it hangs on Kenji's pegboard");

            _fixture.Dispose(true);
            yield return null;
            Boot(slot);
            yield return null;
            yield return null;
            scans = _fixture.Gameplay.Scans;
            Assert.IsTrue(_fixture.Gameplay.Blueprints.IsPinned(CargoCradle), "learned across a load");
            Assert.IsTrue(_fixture.Gameplay.Plotter.ShowsSheet(CargoCradle), "and its sheet simply shown");
            Assert.IsTrue(scans.Find(CargoCradle + ".1").Scanned);
            Assert.IsFalse(scans.Find(CargoCradle + ".0").Root.gameObject.activeSelf, "a scanned fragment stays gone");
            Assert.AreEqual(0, scans.Left(Garage));
            Assert.IsEmpty(_fixture.Events.FragmentScanned, "a load is silent");
        }

        [UnityTest]
        public IEnumerator LettingGo_KeepsTheProgress_TheNextHoldResumesIt_AndALoadKeepsIt()
        {
            string slot = BootstrapHarness.NewTestSlot();
            Boot(slot);
            yield return null;
            ScanField scans = _fixture.Gameplay.Scans;
            ScanFragment fragment = scans.Find(CargoCradle + ".0");
            yield return Face(fragment);
            Press(_keyboard.eKey);
            yield return Waits.Until(() => fragment.Progress >= 0.3f, _fixture.ScanTuning.Seconds(fragment.Size));
            Release(_keyboard.eKey);
            yield return null;
            yield return null;
            Assert.IsNull(scans.Scanning, "let go: the scan stops");
            Assert.AreEqual(1, _fixture.Events.ScanStopped.Count);
            float kept = fragment.Progress;
            Assert.Greater(kept, 0.3f);
            Assert.Less(kept, 1f);
            Assert.AreEqual(kept, _fixture.Events.ScanStopped[0].Value.Progress, 1e-4f);
            Assert.AreEqual(1, Strengths(FeelPhase.Cancel).Count, "the beat eases out");
            Assert.AreEqual(0, _fixture.Rover.HoldStillCount, "07 is free to drive");
            yield return new WaitForSeconds(0.3f);
            Assert.AreEqual(kept, fragment.Progress, "the drawn part stays drawn (ruling 4)");

            _beats.Clear();
            Press(_keyboard.eKey);
            yield return Waits.Until(() => scans.Scanning == fragment, PressTimeout);
            Assert.AreEqual(kept, _beats[0].Strength, 0.02f, "the next hold resumes where it stopped");
            yield return null;
            Release(_keyboard.eKey);
            yield return null;
            kept = fragment.Progress;
            Assert.IsFalse(fragment.Scanned);
            Assert.IsTrue(_fixture.Bootstrap.Context.Get<ISaveService>().SaveNow());

            _fixture.Dispose(true);
            yield return null;
            Boot(slot);
            yield return null;
            fragment = _fixture.Gameplay.Scans.Find(CargoCradle + ".0");
            Assert.AreEqual(kept, fragment.Progress, 1e-4f, "a load keeps how far it was scanned");
            Assert.IsFalse(fragment.Scanned);
        }

        [UnityTest]
        public IEnumerator ASealedContainerInReach_ComesFirst_ThenPastItsReachTheFragmentScans()
        {
            Boot(null);
            yield return null;
            ScanField scans = _fixture.Gameplay.Scans;
            ScanFragment fragment = scans.Find(CellPack + ".1");
            Assert.AreEqual(WorldAnchorIds.Micro06, fragment.HostId, "06's battery bay (§1.3)");
            Transform elder = _fixture.Gameplay.Salvage.FindDerelict(WorldAnchorIds.Micro06).Root;
            yield return Face(fragment, fragment.Point * 2f - elder.position, 6f);
            Assert.IsNotNull(_fixture.Gameplay.Containers.Candidate, "06's tape case is in the beam's reach");
            Assert.IsNull(scans.Candidate, "scanning comes last (§1.2)");
            Assert.AreEqual(InteractionKind.Salvage, _fixture.Gameplay.Hints.Primary.Kind, "the cut is offered");
            Press(_keyboard.eKey);
            yield return null;
            yield return null;
            Assert.IsNull(scans.Scanning, "the hold is the cut's");
            Release(_keyboard.eKey);

            yield return Face(fragment, elder.position, Park);
            Assert.IsNull(_fixture.Gameplay.Containers.Candidate, "past the case's reach");
            Assert.AreSame(fragment, scans.Candidate);
            Assert.AreEqual(InteractionKind.Scan, _fixture.Gameplay.Hints.Primary.Kind);
        }

        [UnityTest]
        public IEnumerator AFragmentOfAKnownBlueprint_PaysThreeMetal_AfterItsDissolve()
        {
            Boot(null);
            yield return null;
            _fixture.Learn(CargoCradle);
            MaterialStock stock = _fixture.Gameplay.Materials;
            int metal = stock.Hold.Units(SalvageMaterial.Metal) + stock.Home.OverflowUnits(SalvageMaterial.Metal);
            ScanFragment fragment = _fixture.Gameplay.Scans.Find(CargoCradle + ".0");
            yield return Face(fragment);
            Press(_keyboard.eKey);
            yield return Waits.Until(() => fragment.Scanned, _fixture.ScanTuning.Seconds(fragment.Size) + PressTimeout);
            Release(_keyboard.eKey);
            FragmentScanned scanned = _fixture.Events.FragmentScanned[0].Value;
            Assert.IsTrue(scanned.Known, "a known blueprint");
            Assert.AreEqual(_fixture.ScanTuning.KnownMetal, scanned.Metal, "the note shows its Metal");
            Assert.IsEmpty(_fixture.Events.BlueprintCompleted, "nothing new completes");
            Assert.AreEqual(metal, stock.Hold.Units(SalvageMaterial.Metal) + stock.Home.OverflowUnits(
                SalvageMaterial.Metal), "the Metal follows the dissolve");
            yield return new WaitForSeconds(_fixture.ScanTuning.DissolveSeconds + 0.2f);
            Assert.AreEqual(metal + _fixture.ScanTuning.KnownMetal, stock.Hold.Units(SalvageMaterial.Metal) +
                stock.Home.OverflowUnits(SalvageMaterial.Metal), "into the hold, else home's overflow crates");
        }

        [UnityTest]
        public IEnumerator TheFocusLens_ShowsTheBracketsFarther_AndScansFaster()
        {
            Boot(null);
            yield return null;
            ScanTuning tuning = _fixture.ScanTuning;
            ScanFragment fragment = _fixture.Gameplay.Scans.Find(CargoCradle + ".0");
            Vector3 away = new Vector3(fragment.Point.x, 0f, fragment.Point.z) +
                           Vector3.back * ((tuning.BracketRange + tuning.LensBracketRange) * 0.5f);
            _fixture.Rover.Place(away, 0f);
            yield return null;
            Assert.IsFalse(Brackets(fragment), "beyond the plain eye's range");
            _fixture.Rover.Grant(RoverAbility.FocusLens);
            yield return null;
            Assert.IsTrue(Brackets(fragment), "the Lens sees them from farther (§1.6)");

            yield return Face(fragment);
            float seconds = tuning.Seconds(fragment.Size) / tuning.LensFactor;
            Press(_keyboard.eKey);
            yield return Waits.Until(() => _fixture.Gameplay.Scans.Scanning == fragment, PressTimeout);
            float start = Time.time;
            yield return Waits.Until(() => fragment.Scanned, seconds + PressTimeout);
            Release(_keyboard.eKey);
            Assert.IsTrue(fragment.Scanned);
            Assert.Less(Time.time - start, tuning.Seconds(fragment.Size) - 0.3f, "faster than the plain eye");
        }

        [UnityTest]
        public IEnumerator ASaveFromBeforeScanning_KeepsItsLearnedBlueprints_TheirFragmentsGone()
        {
            string slot = BootstrapHarness.NewTestSlot();
            Boot(slot);
            yield return null;
            _fixture.Gameplay.Blueprints.Restore(new BlueprintsSaveData
            {
                carried = new[] { CargoCradle }, reading = new[] { CellPack },
            });
            Assert.IsTrue(_fixture.Bootstrap.Context.Get<ISaveService>().SaveNow());
            _fixture.Dispose(true);
            yield return null;

            Boot(slot);
            yield return null;
            ScanField scans = _fixture.Gameplay.Scans;
            Assert.IsTrue(_fixture.Gameplay.Blueprints.IsLearned(CargoCradle), "its tube rode on 07: learned");
            Assert.IsTrue(_fixture.Gameplay.Blueprints.IsLearned(CellPack), "its tube was in the reader: learned");
            Assert.AreEqual(0, scans.Left(Garage), "the garage's Cradle fragments lie scanned");
            Assert.AreEqual(0, scans.Left(WorldAnchorIds.Micro06), "and 06's Cell Pack fragments");
            Assert.IsFalse(scans.Find(CargoCradle + ".0").Root.gameObject.activeSelf, "gone, with no dissolve");
            Assert.AreEqual(3, scans.Left(WorldAnchorIds.CanyonLip), "Hover-Jump's set still waits at the lip");
            Assert.IsEmpty(_fixture.Events.FragmentScanned, "silently");
            Assert.IsEmpty(_fixture.Events.BlueprintCompleted);
        }

        private void Boot(string slot)
        {
            _listening?.Dispose();
            _fixture = GameplayFixture.Boot(_controls, slot);
            _beats.Clear();
            _listening = _fixture.Bootstrap.Context.Events.Subscribe<FeelBeat>(beat =>
            {
                if (beat.Kind == FeelKind.Scan)
                {
                    _beats.Add(beat);
                }
            });
        }

        private List<float> Strengths(FeelPhase phase)
        {
            var strengths = new List<float>();
            foreach (FeelBeat beat in _beats)
            {
                if (beat.Phase == phase)
                {
                    strengths.Add(beat.Strength);
                }
            }

            return strengths;
        }

        private static bool Brackets(ScanFragment fragment)
        {
            return fragment.Root.Find("Brackets").gameObject.activeSelf;
        }

        /// <summary>07 parks <see cref="Park"/> m from the fragment, its view on it, away from its host.</summary>
        private IEnumerator Face(ScanFragment fragment)
        {
            Vector3 host = fragment.Root.parent.parent.position;
            yield return Face(fragment, host, Park);
        }

        /// <summary>
        /// 07 parks <paramref name="distance"/> m from the fragment on the side away from <paramref name="from"/>, its
        /// view on it.
        /// </summary>
        private IEnumerator Face(ScanFragment fragment, Vector3 from, float distance)
        {
            Vector3 point = fragment.Point;
            Vector3 away = point - from;
            away.y = 0f;
            away = away.sqrMagnitude > 1e-4f ? away.normalized : Vector3.back;
            Vector3 park = point + away * distance;
            float yaw = Mathf.Atan2(-away.x, -away.z) * Mathf.Rad2Deg;
            _fixture.Rover.Place(new Vector3(park.x, 0f, park.z), yaw);
            _fixture.Rover.Aim(point + away * (distance + 4f) + Vector3.up * 3f, point);
            yield return null;
            yield return null;
        }
    }
}
