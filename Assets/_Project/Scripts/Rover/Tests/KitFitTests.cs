using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover.Tests
{
    /// <summary>How kit and gifts come onto 07: at once on load, eased in after a purchase.</summary>
    public sealed class KitFitTests
    {
        private const float Frame = 1f / 60f;

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
            Assert.AreEqual(0f, fit.Offset);
            Assert.AreEqual(1f, fit.Scale);
            Assert.AreEqual(1f, fit.Lights);
            Assert.IsFalse(fit.Step(Frame), "A loaded piece never 'lands'.");
        }

        [Test]
        public void Install_WaitsForTheCamera_ThenDropsFromAboveAndSettlesWithOneSmallDip()
        {
            var fit = new KitFit(_settings, false);
            fit.Install();
            Assert.IsFalse(fit.IsHidden);
            for (float t = 0f; t < _settings.FitDelay - Frame; t += Frame)
            {
                Assert.IsFalse(fit.Step(Frame));
                Assert.IsFalse(fit.Visible, "Hidden while the camera eases round.");
            }

            fit.Step(2f * Frame);
            Assert.IsTrue(fit.Visible, "Then it appears...");
            Assert.Greater(fit.Offset, 0.8f * _settings.FitDrop, "...just above its socket...");
            Assert.Less(fit.Scale, 0.6f, "...small, never a hard pop-in.");

            int landings = 0;
            float lowest = 0f;
            float lightsAtLanding = -1f;
            for (float t = 0f; t < 5f; t += Frame)
            {
                if (fit.Step(Frame))
                {
                    landings++;
                    lightsAtLanding = fit.Lights;
                }

                lowest = Mathf.Min(lowest, fit.Offset);
            }

            Assert.AreEqual(1, landings, "It lands once.");
            Assert.Less(lightsAtLanding, 0.2f, "Its lights come on after it lands.");
            Assert.Less(lowest, -0.002f, "A small overshoot past the seat...");
            Assert.Greater(lowest, -0.15f * _settings.FitDrop, "...only a small one.");
            Assert.AreEqual(0f, fit.Offset, "Settled exactly on its socket.");
            Assert.AreEqual(1f, fit.Scale);
            Assert.IsFalse(fit.Moving, "At rest: nothing left to write.");
            Assert.Greater(fit.Lights, 0.99f);
        }

        [Test]
        public void Gift_GrowsInPlaceFromNothing_AndLandsOnce()
        {
            var fit = new KitFit(_settings, true);
            fit.Install();
            float largest = 0f;
            int landings = 0;
            for (float t = 0f; t < 6f; t += Frame)
            {
                landings += fit.Step(Frame) ? 1 : 0;
                Assert.AreEqual(0f, fit.Offset, "A gift does not drop.");
                largest = Mathf.Max(largest, fit.Scale);
            }

            Assert.AreEqual(1, landings);
            Assert.Greater(largest, 1f, "Grows a hair past full size...");
            Assert.Less(largest, 1.15f, "...a hair.");
            Assert.AreEqual(1f, fit.Scale);
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
