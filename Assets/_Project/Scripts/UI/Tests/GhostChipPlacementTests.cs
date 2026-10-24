using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The build ghost's chip never covers the ghost (owner playtest, build f3d1ff265) nor the HUD's corner cards:
    /// below it when there is room, then above, right, left, and a fixed low slot when nothing fits.
    /// </summary>
    public sealed class GhostChipPlacementTests
    {
        private const float Gap = 12f;
        private static readonly Rect Safe = Rect.MinMaxRect(24f, 24f, 1896f, 1056f);
        private static readonly Vector2 Chip = new Vector2(300f, 110f);
        private static readonly IReadOnlyList<Rect> NoCards = Array.Empty<Rect>();

        [Test]
        public void WithRoomBelow_TheChipHangsUnderTheGhost_Centred()
        {
            var ghost = Rect.MinMaxRect(800f, 300f, 1100f, 600f);
            Vector2 at = GhostChipPlacement.Place(ghost, Chip, Safe, Gap, NoCards);
            Assert.AreEqual(ghost.yMax + Gap, at.y, 1e-3f);
            Assert.AreEqual(ghost.center.x, at.x + Chip.x * 0.5f, 1e-3f);
            AssertApart(at, ghost);
        }

        [Test]
        public void AGhostLowOnTheScreen_PutsTheChipAboveIt()
        {
            var ghost = Rect.MinMaxRect(800f, 800f, 1100f, 1040f);
            Vector2 at = GhostChipPlacement.Place(ghost, Chip, Safe, Gap, NoCards);
            Assert.AreEqual(ghost.yMin - Gap - Chip.y, at.y, 1e-3f);
            AssertApart(at, ghost);
        }

        [Test]
        public void ATallGhost_PutsTheChipBesideIt()
        {
            var ghost = Rect.MinMaxRect(600f, 60f, 900f, 1030f);
            Vector2 at = GhostChipPlacement.Place(ghost, Chip, Safe, Gap, NoCards);
            Assert.AreEqual(ghost.xMax + Gap, at.x, 1e-3f, "to its right first");
            AssertApart(at, ghost);

            var atTheRightEdge = Rect.MinMaxRect(1500f, 60f, 1890f, 1030f);
            Vector2 left = GhostChipPlacement.Place(atTheRightEdge, Chip, Safe, Gap, NoCards);
            Assert.AreEqual(atTheRightEdge.xMin - Gap - Chip.x, left.x, 1e-3f, "then to its left");
            AssertApart(left, atTheRightEdge);
        }

        [Test]
        public void AnApronInTheBottomLeftCorner_KeepsTheChipOffTheBatteryDial()
        {
            var dial = Rect.MinMaxRect(24f, 900f, 180f, 1056f);
            var ghost = Rect.MinMaxRect(30f, 700f, 400f, 1050f);
            Vector2 at = GhostChipPlacement.Place(ghost, Chip, Safe, Gap, new[] { dial });
            AssertApart(at, ghost);
            AssertApart(at, dial);
        }

        [Test]
        public void AGhostThatFillsTheView_DocksTheChipLowInTheMiddle_InsideTheSafeArea()
        {
            var ghost = Rect.MinMaxRect(0f, 0f, 1920f, 1080f);
            Vector2 at = GhostChipPlacement.Place(ghost, Chip, Safe, Gap, NoCards);
            Assert.AreEqual(Safe.center.x, at.x + Chip.x * 0.5f, 1e-3f);
            Assert.AreEqual(Safe.yMax - Chip.y, at.y, 1e-3f);
        }

        [Test]
        public void TheGhostsScreenBounds_AreTheSmallestRectAroundItsCorners()
        {
            var corners = new[] { new Vector2(5f, 9f), new Vector2(-2f, 4f), new Vector2(7f, 1f) };
            Rect rect = GhostChipPlacement.Around(corners, corners.Length);
            Assert.AreEqual(Rect.MinMaxRect(-2f, 1f, 7f, 9f), rect);
        }

        private static void AssertApart(Vector2 at, Rect other)
        {
            var chip = new Rect(at, Chip);
            Assert.IsFalse(chip.Overlaps(other), $"chip {chip} overlaps {other}");
            Assert.IsTrue(Safe.Contains(chip.min) && Safe.Contains(chip.max - Vector2.one * 1e-3f),
                $"chip {chip} leaves the safe area");
        }
    }
}
