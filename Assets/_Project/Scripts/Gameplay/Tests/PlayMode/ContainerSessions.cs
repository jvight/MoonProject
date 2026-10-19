using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core.Events;
using MoonProject.Testing;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// Sealed containers (docs/design/0.5.5-owner-feedback.md §2, opening-v2 §4): a cut seal keeps its crate open
    /// through a reboot with no event, and the lander's open crate stands open from the start with nothing to cut.
    /// </summary>
    public sealed class ContainerSessions : InputTestFixture
    {
        private const float BesideSeal = 1.5f;
        private const float EyeHeight = 1.5f;
        private const float ScanAside = 2.5f;
        private const float ScanLift = 0.5f;
        private const float ColdCutTimeout = 12f;

        private InputActionAsset _controls;
        private GameplayFixture _fixture;

        public override void Setup()
        {
            base.Setup();
            InputSystem.AddDevice<Keyboard>();
            _controls = BootstrapHarness.LoadControlsCopy();
        }

        public override void TearDown()
        {
            _fixture?.Dispose();
            Object.Destroy(_controls);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator ACutSeal_KeepsItsCrateOpenThroughAReboot_Silently()
        {
            string slot = BootstrapHarness.NewTestSlot();
            _fixture = GameplayFixture.Boot(_controls, slot);
            yield return null;
            GameplaySystem gameplay = _fixture.Gameplay;
            SealedContainer crate = gameplay.Containers.Find("supply.array.junction");
            _fixture.Rover.Place(crate.CutPoint.position + crate.Root.forward * 1.5f,
                Quaternion.LookRotation(-crate.Root.forward).eulerAngles.y);
            yield return SealCut.Hold(this, gameplay, crate, "the junction box's crate");
            Quaternion open = crate.Lid.localRotation;

            _fixture.Dispose(true);
            yield return null;
            _fixture = GameplayFixture.Boot(_controls, slot);
            var opens = new List<ContainerOpened>();
            _fixture.Bootstrap.Context.Events.Subscribe<ContainerOpened>(opens.Add);
            yield return null;
            yield return null;
            crate = _fixture.Gameplay.Containers.Find("supply.array.junction");
            Assert.IsTrue(crate.StandsOpen, "the cut was a checkpoint: it stands open");
            Assert.Less(Quaternion.Angle(open, crate.Lid.localRotation), 1f, "its lid back where it swung to");
            Assert.IsEmpty(opens, "a load is not an opening");
        }

        [UnityTest]
        public IEnumerator TheLandersOpenCrate_StandsOpenFromTheStart_WithNothingToCut()
        {
            _fixture = GameplayFixture.Boot(_controls, startKit: true);
            yield return null;
            SealedContainer crate = _fixture.Gameplay.Containers.Find("supply.lander.open");
            Assert.IsTrue(crate.StandsOpen, "opening-v2 §10: no cutting before B6");
            _fixture.Rover.Place(crate.CutPoint.position + crate.Root.forward * 5f,
                Quaternion.LookRotation(-crate.Root.forward).eulerAngles.y);
            yield return null;
            yield return null;
            Assert.AreNotEqual(crate, _fixture.Gameplay.Containers.Candidate, "an open crate is never a cut");
        }

        [UnityTest]
        public IEnumerator CuttingASealedContainer_AimsTheBeamAtItsCutPoint_AndHolds07Still()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            GameplaySystem gameplay = _fixture.Gameplay;
            SealedContainer crate = gameplay.Containers.Find("supply.array.junction");
            _fixture.Rover.Place(crate.CutPoint.position + crate.Root.forward * 1.5f,
                Quaternion.LookRotation(-crate.Root.forward).eulerAngles.y);
            yield return null;
            yield return null;

            Assert.AreSame(crate, gameplay.Containers.Candidate);
            Assert.IsTrue(gameplay.Salvage.HasTarget, "salvage reports the sealed container as the cutter's target");
            Assert.Less(Vector3.Distance(gameplay.Salvage.CutPoint, crate.CutPoint.position), 1e-3f,
                "the cut point is the container's, not the world origin");

            Press(Keyboard.current.eKey);
            yield return Waits.Until(() => gameplay.Containers.IsCutting && gameplay.Salvage.BeamLevel > 0.1f, 3f);
            Assert.Greater(_fixture.Rover.HoldStillCount, 0, "07 holds still while it cuts a container");
            Assert.Less(Vector3.Distance(gameplay.Salvage.CutPoint, crate.CutPoint.position), 1e-3f,
                "the beam stays on the container's cut point through the cut");

            yield return Waits.Until(() => crate.Opened, 4f);
            Release(Keyboard.current.eKey);
            yield return null;
            Assert.AreEqual(0, _fixture.Rover.HoldStillCount, "the hold lets go once the container is open");
        }

        [UnityTest]
        public IEnumerator AHoldOnASeal_StaysOnTheSeal_WhileTheViewSwingsToAScanTarget_AndTheCutCompletes()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            GameplaySystem gameplay = _fixture.Gameplay;
            SealedContainer crate = gameplay.Containers.Find("supply.array.junction");
            Vector3 stand = crate.CutPoint.position + crate.Root.forward * BesideSeal;
            _fixture.Rover.Place(stand, Quaternion.LookRotation(-crate.Root.forward).eulerAngles.y);
            ScanFragment fragment = gameplay.Scans.Find("ruin." + ScanTuning.LanderCargoId + ".0");
            Vector3 eye = stand + Vector3.up * EyeHeight;
            Vector3 aside = Vector3.Cross(Vector3.up, crate.Root.forward).normalized * ScanAside;
            fragment.Root.position = crate.CutPoint.position + aside + Vector3.up * ScanLift;
            _fixture.Rover.Aim(eye, crate.CutPoint.position);
            yield return null;
            yield return null;
            Assert.AreSame(crate, gameplay.Containers.Candidate, "looking at the seal offers the seal");

            Press(Keyboard.current.eKey);
            yield return null;
            _fixture.Rover.Aim(eye, fragment.Point);
            yield return null;
            Assert.AreSame(fragment, gameplay.Scans.LookedAt(out float scanScore),
                "the swung view rests on the scan target");
            Assert.IsTrue(!gameplay.Scans.InView(crate.CutPoint.position, out float sealScore) || scanScore < sealScore,
                "and more squarely than on the seal: the scan would take E (the trap's set)");

            yield return Waits.Until(() => crate.Opened, ColdCutTimeout);
            Assert.AreSame(crate, gameplay.Containers.Cutting ?? gameplay.Containers.Candidate,
                "the hold stayed on the seal while the view swung");
            Release(Keyboard.current.eKey);
            Assert.IsTrue(crate.Opened, "the cut completed");
            Assert.IsFalse(fragment.Scanned, "the scan target did not take the hold");
        }
    }
}
