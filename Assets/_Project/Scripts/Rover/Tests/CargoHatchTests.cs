using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Rover;

namespace MoonProject.Rover.Tests
{
    /// <summary>07's cargo hatch (M3-25 T1): opens just before a bundle lands, shuts with one soft thunk, shakes when
    /// bumped, and its window shows a crate per used slot.</summary>
    public sealed class CargoHatchTests
    {
        private const float Frame = 1f / 120f;

        private readonly CargoHatchSettings _settings = new CargoHatchSettings();

        private static int Run(CargoHatchMotion hatch, float seconds)
        {
            int thunks = 0;
            for (float t = 0f; t < seconds; t += Frame)
            {
                thunks += hatch.Step(Frame) ? 1 : 0;
            }

            return thunks;
        }

        [Test]
        public void ItOpensJustBeforeTheBundleLands_Eased()
        {
            var hatch = new CargoHatchMotion(_settings);
            hatch.OpenBefore(0.7f);
            Run(hatch, 0.7f - _settings.OpenLead - 0.02f);
            Assert.AreEqual(0f, hatch.Angle, "Still shut while the bundle is in the air.");
            Run(hatch, 0.1f);
            Assert.That(hatch.Angle, Is.GreaterThan(0f).And.LessThan(_settings.OpenAngle), "Easing open.");
            Run(hatch, 2f);
            Assert.AreEqual(_settings.OpenAngle, hatch.Angle, 1f);
        }

        [Test]
        public void Shutting_ThunksOnce_AndNeverSwingsInward()
        {
            var hatch = new CargoHatchMotion(_settings);
            hatch.Open();
            Run(hatch, 2f);
            hatch.Close();
            int thunks = 0;
            for (float t = 0f; t < 3f; t += Frame)
            {
                thunks += hatch.Step(Frame) ? 1 : 0;
                Assert.GreaterOrEqual(hatch.Angle, 0f, "The frame stops it.");
            }

            Assert.AreEqual(1, thunks, "One soft thunk; the bounce's settle is silent.");
            Assert.AreEqual(0f, hatch.Angle, 0.5f);
        }

        [Test]
        public void AShowOfTheHold_OpensStaysOpen_ThenShutsWithOneThunk()
        {
            var hatch = new CargoHatchMotion(_settings);
            hatch.OpenFor(0.8f);
            Assert.AreEqual(0, Run(hatch, 0.75f), "Open the whole while.");
            Assert.Greater(hatch.Angle, 0.5f * _settings.OpenAngle, "Eased well open.");
            Assert.AreEqual(1, Run(hatch, 3f), "Then it shuts by itself, one soft thunk.");
            Assert.AreEqual(0f, hatch.Angle, 0.5f);
            Assert.IsFalse(hatch.IsOpening);
        }

        [Test]
        public void AShowOfTheHold_NeverShutsAHatchABundleOrAnUnloadOpened()
        {
            var hatch = new CargoHatchMotion(_settings);
            hatch.Open();
            hatch.OpenFor(0.2f);
            Run(hatch, 2f);
            Assert.AreEqual(_settings.OpenAngle, hatch.Angle, 1f, "The unload still owns it.");

            var shown = new CargoHatchMotion(_settings);
            shown.OpenFor(0.2f);
            shown.OpenBefore(0.5f);
            Run(shown, 2f);
            Assert.IsTrue(shown.IsOpening, "A bundle on its way keeps it open until it lands.");
        }

        [Test]
        public void ABump_ShakesTheShutHatch_ThenItSettles()
        {
            var hatch = new CargoHatchMotion(_settings);
            hatch.Bump();
            Run(hatch, 0.03f);
            Assert.Greater(Mathf.Abs(hatch.Angle), 0.5f);
            Run(hatch, 2f);
            Assert.AreEqual(0f, hatch.Angle, 0.05f);
        }

        [Test]
        public void TheWindow_ShowsACratePerUsedSlot_AndAnEmptyHoldShuts()
        {
            var events = new EventBus();
            var root = new GameObject("CargoHatch");
            var canopy = new GameObject(RoverModelNodes.UpperCanopy);
            canopy.transform.SetParent(root.transform, false);
            var chute = new GameObject(RoverModelNodes.LowerChute);
            chute.transform.SetParent(root.transform, false);
            var crates = new GameObject[8];
            for (int i = 0; i < crates.Length; i++)
            {
                crates[i] = new GameObject(RoverModelNodes.HatchCrate(i));
                crates[i].transform.SetParent(root.transform);
            }

            var driver = new CargoHatch(events, _settings, root.transform, crates, new RumbleMixer());
            Assert.IsFalse(crates[0].activeSelf, "An empty window to start.");
            events.Publish(new CargoChanged(3, 8));
            Assert.IsTrue(crates[0].activeSelf && crates[2].activeSelf && !crates[3].activeSelf);
            events.Publish(new FeelBeat(FeelKind.Cargo, FeelPhase.Anticipate, Vector3.zero, Vector3.up, 1f, 0.5f));
            Assert.IsTrue(driver.Motion.IsOpening, "The dock unload opens it.");
            for (int i = 0; i < 240; i++)
            {
                driver.Tick(Frame);
            }

            Assert.AreEqual(_settings.OpenAngle, Mathf.DeltaAngle(0f, canopy.transform.localEulerAngles.x), 1f,
                "UpperCanopy swings open around +X.");
            Assert.AreEqual(-22f, Mathf.DeltaAngle(0f, chute.transform.localEulerAngles.x), 1f,
                "LowerChute swings down around -X.");
            events.Publish(new CargoChanged(0, 8));
            Assert.IsFalse(driver.Motion.IsOpening, "An empty hold shuts it.");
            Assert.IsFalse(crates[0].activeSelf);
            events.Publish(new RoverHatchCycleRequested(0.5f));
            Assert.IsTrue(driver.Motion.IsOpening, "The first Tab's boot shows the hold.");
            driver.Dispose();
            Object.DestroyImmediate(root);
        }
    }
}
