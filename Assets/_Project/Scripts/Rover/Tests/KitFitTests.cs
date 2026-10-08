using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover.Tests
{
    /// <summary>How kit and gifts come onto 07: at once on load, carried in by the bay, gifts grown in place.</summary>
    public sealed class KitFitTests
    {
        private const float Frame = 1f / 60f;
        private const float PickScale = 0.6f;

        private KitSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _settings = new KitSettings();
        }

        [Test]
        public void Show_IsThereAtOnce_LightsOn()
        {
            var fit = new KitFit(_settings, false);
            Assert.IsTrue(fit.IsHidden);
            Assert.IsFalse(fit.Visible);
            fit.Show();
            Assert.IsTrue(fit.Visible);
            Assert.AreEqual(1f, fit.Scale);
            Assert.AreEqual(1f, fit.Lights);
            Assert.IsFalse(fit.Step(Frame), "A loaded piece never 'lands'.");
        }

        [Test]
        public void Carried_GrowsSmoothlyFromItsRackSize_LightsStayOffUntilItLands()
        {
            var fit = new KitFit(_settings, false);
            fit.Carry(PickScale);
            Assert.IsTrue(fit.Visible);
            Assert.IsTrue(fit.IsCarried);
            Assert.IsFalse(fit.IsHidden, "Taken by the bay: nothing else may start it.");
            Assert.AreEqual(PickScale, fit.Scale, 1e-4f);

            float previous = fit.Scale;
            float largest = 0f;
            for (float t = 0f; t < 2f; t += Frame)
            {
                Assert.IsFalse(fit.Step(Frame), "Crafted kit lands when the arm sets it, not on its own.");
                Assert.IsTrue(fit.Moving);
                Assert.Less(Mathf.Abs(fit.Scale - previous), 0.08f, "No pop in scale.");
                previous = fit.Scale;
                largest = Mathf.Max(largest, fit.Scale);
                Assert.AreEqual(0f, fit.Lights);
            }

            Assert.AreEqual(1f, fit.Scale, 0.01f, "Full size well before the arm lowers it.");
            Assert.LessOrEqual(largest, 1f + 1e-3f, "Grows without overshoot: it is held, not popped.");
        }

        [Test]
        public void Landed_IsFittedAtFullSize_WrittenOnce_ThenItsLightsEaseOn()
        {
            var fit = new KitFit(_settings, false);
            fit.Carry(PickScale);
            fit.Step(Frame);
            fit.Land();
            Assert.IsFalse(fit.IsCarried);
            Assert.AreEqual(1f, fit.Scale);

            fit.Step(Frame);
            Assert.IsTrue(fit.Moving, "The landing frame writes its final pose.");
            Assert.Less(fit.Lights, 0.2f, "Its lights come on after it lands...");
            fit.Step(Frame);
            Assert.IsFalse(fit.Moving, "...and then nothing is left to write.");
            for (float t = 0f; t < 3f; t += Frame)
            {
                fit.Step(Frame);
            }

            Assert.Greater(fit.Lights, 0.99f);
        }

        [Test]
        public void CraftedKit_IsNeverGrownInPlace()
        {
            var fit = new KitFit(_settings, false);
            Assert.Throws<System.InvalidOperationException>(fit.Install);
        }

        [Test]
        public void Gift_WaitsThenGrowsInPlaceFromNothing_AndLandsOnce()
        {
            var fit = new KitFit(_settings, true);
            fit.Install();
            Assert.IsFalse(fit.Visible, "A short wait first.");
            float largest = 0f;
            int landings = 0;
            for (float t = 0f; t < 6f; t += Frame)
            {
                landings += fit.Step(Frame) ? 1 : 0;
                largest = Mathf.Max(largest, fit.Scale);
            }

            Assert.AreEqual(1, landings);
            Assert.Greater(largest, 1f, "Grows a hair past full size...");
            Assert.Less(largest, 1.15f, "...a hair.");
            Assert.AreEqual(1f, fit.Scale);
            Assert.Greater(fit.Lights, 0.99f);
        }

        [Test]
        public void Pieces_MapToTheirAbilities_GiftsToNone()
        {
            Assert.IsTrue(RoverKitPieces.TryGetAbility(RoverKitPiece.LampBar, out RoverAbility lamp));
            Assert.AreEqual(RoverAbility.WarmHeadlamp, lamp);
            Assert.IsTrue(RoverKitPieces.TryGetAbility(RoverKitPiece.CapacitorDrums, out RoverAbility drums));
            Assert.AreEqual(RoverAbility.BoostCoils, drums);
            Assert.IsTrue(RoverKitPieces.TryGetAbility(RoverKitPiece.CargoRack, out RoverAbility rack));
            Assert.AreEqual(RoverAbility.CargoCradle, rack);
            Assert.IsTrue(RoverKitPieces.TryGetAbility(RoverKitPiece.HoverCoils, out RoverAbility coils));
            Assert.AreEqual(RoverAbility.HoverJump, coils);
            Assert.IsFalse(RoverKitPieces.TryGetAbility(RoverKitPiece.SolarCell, out _));
            Assert.IsFalse(RoverKitPieces.TryGetAbility(RoverKitPiece.FreshPaint, out _));
        }

        [Test]
        public void RoverUpgrades_AreTheRoverPrefixedIds()
        {
            Assert.IsTrue(RoverKitPieces.IsRoverUpgrade("rover.boost_coils"));
            Assert.IsFalse(RoverKitPieces.IsRoverUpgrade("radio_tower"));
            Assert.IsFalse(RoverKitPieces.IsRoverUpgrade(null));
        }

        [Test]
        public void EveryPiece_HasAView()
        {
            var views = new KitViewSettings();
            foreach (RoverKitPiece piece in System.Enum.GetValues(typeof(RoverKitPiece)))
            {
                Assert.That(views.Bearing(piece), Is.InRange(-180f, 180f), piece.ToString());
            }
        }
    }
}
