using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Testing;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// The feel checklist's last line: the opening's busy moments allocate nothing (opening-v2): 07 charging on the pad
    /// it built with the dock lock raised and the lander's reserve drawn, a scan of a ruin target under way, the
    /// salvage field with a piece in reach, and build mode placing a ghost.
    /// </summary>
    public sealed class GameplayAllocationSessions : InputTestFixture
    {
        private const int WarmFrames = 30;
        private const int MeasuredFrames = 300;

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
        public IEnumerator ChargingOnThePad_AndAScanUnderWay_AllocateNothing()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            GameplaySystem gameplay = _fixture.Gameplay;
            Vector3 apron = _fixture.World.Anchor("place.apron").Position;
            gameplay.PlacedBase.PlaceUnchecked(gameplay.Building.Catalog.Find(StructureIds.ChargingPad),
                new StructurePose(apron, 0f), 1f);
            for (int i = 0; i < gameplay.Battery.Model.Cells * 4; i++)
            {
                gameplay.Battery.Model.PayLeap(1f);
            }

            _fixture.Rover.Place(apron, 0f);
            yield return Waits.Until(() => gameplay.PadLock.Raised, 5f);
            Assert.IsTrue(gameplay.Battery.Model.IsCharging, "07 charges on the pad from the lander's reserve");
            Assert.IsTrue(gameplay.PadLock.Raised, "the dock lock stands round it");
            AssertAllocatesNothing(Method(gameplay, "Update"), "the opening with 07 charging on its pad");
            AssertAllocatesNothing(Method(gameplay.Battery, "Update"), "the battery charging from the reserve");

            ScanFragment target = gameplay.Scans.Find("ruin.FallenSolarArray.0");
            Vector3 park = target.Point + Vector3.back * 4f;
            _fixture.Rover.Place(new Vector3(park.x, 0f, park.z), 0f);
            _fixture.Rover.Aim(target.Point + Vector3.back * 8f + Vector3.up * 3f, target.Point);
            yield return null;
            yield return null;
            Press(_keyboard.eKey);
            yield return Waits.Until(() => gameplay.Scans.Scanning == target, 2f);
            Assert.AreSame(target, gameplay.Scans.Scanning, "07's lens is open on the array");
            target.Progress = 0f;
            AssertAllocatesNothing(() => gameplay.Scans.Step(Time.time, 1f / 600f), "a scan under way");
            Release(_keyboard.eKey);
        }

        [UnityTest]
        public IEnumerator SalvageInReach_AndBuildModePlacing_AllocateNothing()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            GameplaySystem gameplay = _fixture.Gameplay;
            SalvagePiece piece = _fixture.FindSite("kestrel").Find(0);
            Vector3 park = piece.CutPosition + piece.CutNormal * 4.5f;
            _fixture.Rover.Place(new Vector3(park.x, 0f, park.z),
                Quaternion.LookRotation(-piece.CutNormal).eulerAngles.y);
            _fixture.Rover.Aim(piece.CutPosition + piece.CutNormal * 9f + Vector3.up * 3.5f, piece.CutPosition);
            yield return null;
            yield return null;
            Assert.AreSame(piece, gameplay.Salvage.Candidate, "a piece in reach");
            AssertAllocatesNothing(Method(gameplay.Salvage, "Update"), "the salvage field with a piece in reach");

            _fixture.Rover.Place(new Vector3(0f, 0f, -20f), 0f);
            _fixture.Rover.Aim(new Vector3(0f, 1.5f, -20f), new Vector3(0f, 0f, -15f));
            gameplay.KnownStructures.Learn(StructureIds.ChargingPad);
            Assert.IsTrue(gameplay.Building.Controller.TryOpenMenu());
            Assert.IsTrue(gameplay.Building.Controller.PickStructure(StructureIds.ChargingPad));
            yield return null;
            Assert.AreEqual(BuildModeState.Placing, gameplay.Building.State);
            AssertAllocatesNothing(Method(gameplay.Building, "Update"), "build mode ghost placement");
            gameplay.Building.Controller.Close();
        }

        /// <summary>Warms <paramref name="update"/> up, then asserts 300 more frames of it allocate nothing.</summary>
        private static void AssertAllocatesNothing(Action update, string what)
        {
            for (int frame = 0; frame < WarmFrames; frame++)
            {
                update();
            }

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int frame = 0; frame < MeasuredFrames; frame++)
            {
                update();
            }

            Assert.AreEqual(0L, GC.GetAllocatedBytesForCurrentThread() - before,
                "bytes allocated by 300 frames of " + what);
        }

        private static Action Method(MonoBehaviour component, string name)
        {
            MethodInfo method = component.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"{component.GetType().Name}.{name}");
            return (Action)Delegate.CreateDelegate(typeof(Action), component, method);
        }
    }
}
