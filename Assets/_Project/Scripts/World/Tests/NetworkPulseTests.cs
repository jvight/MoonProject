using NUnit.Framework;
using UnityEngine;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// The network's handshake (north-star §4a beat 7): from a newly lit mast, a bead runs the network's links one hop
    /// at a time, out to every lit mast and back to home, and each lamp flashes as it arrives.
    /// </summary>
    public sealed class NetworkPulseTests
    {
        private const float Tolerance = 1e-4f;

        // Home, then four masts: a chain out east (1, 2), one north of home (3) and one far east not yet lit (4).
        private static readonly Vector3[] Lamps =
        {
            new Vector3(0f, 10f, 0f),
            new Vector3(100f, 12f, 0f),
            new Vector3(200f, 14f, 10f),
            new Vector3(0f, 12f, 120f),
            new Vector3(300f, 12f, 0f),
        };

        private static readonly bool[] Lit = { true, true, true, true, false };

        private static NetworkPulse StartFrom(int origin, out NetworkPulseSettings settings)
        {
            settings = new NetworkPulseSettings();
            var pulse = new NetworkPulse(settings);
            pulse.Start(Lamps, Lit, Lamps.Length, origin, 0f);
            return pulse;
        }

        [Test]
        public void Links_JoinEachLitNodeToTheNearestOneAlreadyJoined()
        {
            NetworkPulse pulse = StartFrom(2, out _);
            Assert.AreEqual(-1, pulse.Parent(0), "home is the root");
            Assert.AreEqual(0, pulse.Parent(1));
            Assert.AreEqual(1, pulse.Parent(2), "the far east mast links through the near one");
            Assert.AreEqual(0, pulse.Parent(3));
            Assert.AreEqual(-1, pulse.Parent(4), "a dark mast takes no part");
        }

        [Test]
        public void Hops_RunFromTheNewMastBackHomeAndOutToTheOtherMasts()
        {
            NetworkPulse pulse = StartFrom(2, out _);
            Assert.AreEqual(0, pulse.Hops(2));
            Assert.AreEqual(1, pulse.Hops(1));
            Assert.AreEqual(2, pulse.Hops(0), "back to home");
            Assert.AreEqual(3, pulse.Hops(3), "and on out past it");
            Assert.AreEqual(-1, pulse.Hops(4));
            Assert.AreEqual(3, pulse.FarthestHop);
        }

        [Test]
        public void Flash_EachLampFlashesInTurn_TheNewMastBrightest()
        {
            NetworkPulse pulse = StartFrom(2, out NetworkPulseSettings settings);
            float peak = settings.FlashRise;
            float hop = settings.HopSeconds;
            Assert.AreEqual(settings.OriginBoost, pulse.Flash(2, peak), Tolerance, "the new mast first, boosted");
            Assert.AreEqual(0f, pulse.Flash(1, peak), Tolerance, "the next lamp still waits");
            Assert.AreEqual(1f, pulse.Flash(1, hop + peak), Tolerance, "one hop later, the next lamp");
            Assert.AreEqual(1f, pulse.Flash(0, 2f * hop + peak), Tolerance, "home answers");
            Assert.AreEqual(0f, pulse.Flash(4, 4f * hop), Tolerance, "a dark mast never flashes");
            Assert.AreEqual(0f, pulse.Flash(2, peak + settings.FlashFall), Tolerance, "each flash eases away");
        }

        [Test]
        public void Bead_TravelsEachLinkAwayFromTheNewMast_ArcingAboveTheLamps()
        {
            NetworkPulse pulse = StartFrom(2, out NetworkPulseSettings settings);
            float hop = settings.HopSeconds;
            Assert.IsTrue(pulse.Bead(2, 0.5f * hop, out Vector3 middle, out float level), "first link, mid-hop");
            Assert.AreEqual(150f, middle.x, 1f, "half way between the two lamps");
            Assert.Greater(middle.y, 13f, "above the straight line: a radio hop over the ridges");
            Assert.AreEqual(1f, level, Tolerance);
            Assert.IsFalse(pulse.Bead(1, 0.5f * hop, out _, out _), "the link home waits for the next hop");
            Assert.IsTrue(pulse.Bead(1, 1.1f * hop, out Vector3 early, out _));
            Assert.Greater(early.x, 90f, "the bead leaves the lamp the handshake reached first");
            Assert.IsTrue(pulse.Bead(3, 2.5f * hop, out Vector3 outward, out _), "out past home to the north mast");
            Assert.Greater(outward.z, 0f);
        }

        [Test]
        public void IsActive_EndsOnceTheFarthestFlashHasFaded()
        {
            NetworkPulse pulse = StartFrom(2, out NetworkPulseSettings settings);
            float end = pulse.FarthestHop * settings.HopSeconds + settings.FlashRise + settings.FlashFall;
            Assert.IsTrue(pulse.IsActive(end - 0.01f));
            Assert.IsFalse(pulse.IsActive(end + 0.01f));
            Assert.IsFalse(new NetworkPulse(settings).IsActive(0f), "nothing runs before a mast lights");
        }
    }
}
