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
    /// The databank in the running game (docs/design/databank.md §3): scanning the fallen solar array's three targets
    /// by hand brings its entry in on the first and counts it 1/3, 2/3, 3/3 as the real scan field publishes each
    /// <see cref="FragmentScanned"/>, and a save written then restores it.
    /// </summary>
    public sealed class DatabankSessions : InputTestFixture
    {
        private const string ArrayEntry = "db.array";
        private const string ArraySet = "FallenSolarArray";
        private const float ScanSeconds = 30f;
        private const float StandOff = 4f;
        private const float EyeBack = 8f;
        private const float EyeHeight = 3f;

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
        public IEnumerator ScanningTheFallenArray_BringsItsEntryIn_AndCountsItUpToThreeOfThree()
        {
            _fixture = GameplayFixture.Boot(_controls, startKit: true);
            yield return null;
            GameplaySystem gameplay = _fixture.Gameplay;
            IDatabank databank = _fixture.Bootstrap.Context.Get<IDatabank>();
            var added = new List<string>();
            var counts = new List<int>();
            _fixture.Bootstrap.Context.Events.Subscribe<DatabankEntryAdded>(entry => added.Add(entry.EntryId));
            _fixture.Bootstrap.Context.Events.Subscribe<DatabankEntryProgressed>(progress =>
            {
                if (progress.EntryId == ArrayEntry)
                {
                    counts.Add(progress.Found);
                }
            });

            CollectionAssert.DoesNotContain(databank.UnlockedIds, ArrayEntry, "07 has not scanned the array yet");
            Assert.AreEqual(0, databank.Progress(ArrayEntry).Count);

            for (int i = 0; i < 3; i++)
            {
                ScanFragment fragment = gameplay.Scans.Find(ScanField.FragmentId("ruin." + ArraySet, i));
                Assert.IsNotNull(fragment, $"array target {i} is in the world");
                yield return ScanByHand(gameplay, fragment);

                DatabankProgress progress = databank.Progress(ArrayEntry);
                Assert.AreEqual(i + 1, progress.Found, $"{i + 1} of 3 targets scanned");
                Assert.AreEqual(3, progress.Count);
                Assert.AreEqual(i == 2, progress.IsComplete);
                Assert.IsTrue(databank.IsUnread(ArrayEntry), "each new piece is news");
                databank.MarkRead(ArrayEntry);
            }

            CollectionAssert.AreEqual(new[] { ArrayEntry, "db.lumen" }, added.FindAll(id => id != "db.me"),
                "the array's entry comes in once, on the first target, with the Lumen place");
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, counts);

            DatabankSaveData saved = gameplay.Databank.Capture();
            gameplay.Databank.Restore(saved);
            Assert.AreEqual(3, databank.Progress(ArrayEntry).Found, "a save keeps the count");
            Assert.IsFalse(databank.IsUnread(ArrayEntry), "and that it was read");
        }

        private IEnumerator ScanByHand(GameplaySystem gameplay, ScanFragment fragment)
        {
            Vector3 park = fragment.Point + Vector3.back * StandOff;
            _fixture.Rover.Place(new Vector3(park.x, 0f, park.z), 0f);
            _fixture.Rover.Aim(fragment.Point + Vector3.back * EyeBack + Vector3.up * EyeHeight, fragment.Point);
            yield return null;
            yield return null;
            yield return Waits.Until(() => gameplay.Scans.Candidate == fragment, 5f);
            Press(_keyboard.eKey);
            yield return Waits.Until(() => fragment.Scanned, ScanSeconds);
            Release(_keyboard.eKey);
            yield return null;
        }
    }
}
