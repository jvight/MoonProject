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
            SealedContainer crate = gameplay.Containers.Find("supply.wake");
            _fixture.Rover.Place(crate.CutPoint.position + crate.Root.forward * 1.5f,
                Quaternion.LookRotation(-crate.Root.forward).eulerAngles.y);
            yield return SealCut.Hold(this, gameplay, crate, "the wake crate");
            Quaternion open = crate.Lid.localRotation;

            _fixture.Dispose(true);
            yield return null;
            _fixture = GameplayFixture.Boot(_controls, slot);
            var opens = new List<ContainerOpened>();
            _fixture.Bootstrap.Context.Events.Subscribe<ContainerOpened>(opens.Add);
            yield return null;
            yield return null;
            crate = _fixture.Gameplay.Containers.Find("supply.wake");
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
            SealedContainer crate = gameplay.Containers.Find("supply.wake");
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
    }
}
