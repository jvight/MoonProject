using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Testing;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// Sealed containers (docs/design/0.5.5-owner-feedback.md §2): a cut seal keeps its container open through a reboot
    /// with no event, and an older save whose find was already taken loads its container open.
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
        public IEnumerator ACutSeal_KeepsItsContainerOpenThroughAReboot_Silently()
        {
            string slot = BootstrapHarness.NewTestSlot();
            _fixture = GameplayFixture.Boot(_controls, slot);
            yield return null;
            GameplaySystem gameplay = _fixture.Gameplay;
            SealedContainer crate = Coil(gameplay);
            gameplay.Crates.Restore(new CratesSaveData { opened = new[] { WorldAnchorIds.SitePrefix + "depot" } });
            _fixture.Rover.Place(crate.CutPoint.position + crate.Root.forward * 1.5f, 0f);
            yield return SealCut.Hold(this, gameplay, crate, "the coil crate");
            Quaternion open = crate.Lid.localRotation;

            _fixture.Dispose(true);
            yield return null;
            _fixture = GameplayFixture.Boot(_controls, slot);
            var opens = new List<ContainerOpened>();
            _fixture.Bootstrap.Context.Events.Subscribe<ContainerOpened>(opens.Add);
            yield return null;
            yield return null;
            crate = Coil(_fixture.Gameplay);
            Assert.IsTrue(crate.StandsOpen, "the cut was a checkpoint: it stands open");
            Assert.Less(Quaternion.Angle(open, crate.Lid.localRotation), 1f, "its lid back where it swung to");
            Assert.IsEmpty(opens, "a load is not an opening");
            Assert.IsNull(_fixture.Gameplay.Containers.Candidate, "nothing left to cut");
        }

        [UnityTest]
        public IEnumerator AnOlderSaveWhoseCoilWasTaken_LoadsItsCrateOpen()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            GameplaySystem gameplay = _fixture.Gameplay;
            SealedContainer crate = Coil(gameplay);
            Assert.IsFalse(crate.Opened, "a new game's crate is sealed");
            gameplay.TowerParts.Restore(new TowerPartsSaveData { carried = new[] { "Part_TowerCoil" } });
            gameplay.TowerPartField.Sync();
            Assert.IsTrue(crate.StandsOpen, "the coil already rides in 07's hold: its crate stands open");
            Assert.IsFalse(gameplay.TowerPartField.IsResting(0), "and holds nothing");
            Assert.IsTrue(gameplay.Materials.Hold.HasPart("Part_TowerCoil"), "held in 07's hold");
        }

        private static SealedContainer Coil(GameplaySystem gameplay)
        {
            SealedContainer crate = gameplay.Containers.Find(TowerPartField.ContainerId(gameplay.TowerParts.PartAt(0)));
            Assert.IsNotNull(crate);
            return crate;
        }
    }
}
