using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// The lab's lamps (docs/features/M3-19): lamps warm up in play and restore at once, the porthole counts known
    /// nodes, the recovery rows place their lamps (the Link's last in the Peak socket), the chart pen plots round home,
    /// and the halo rule holds for home's pilot.
    /// </summary>
    public sealed class LabLightingTests
    {
        private const float Tolerance = 1e-4f;

        [Test]
        public void Lamps_WarmUpInPlay_AndRestoreAtOnce()
        {
            var bank = new LampBank(2);
            bank.Set(0, 1f, false);
            bank.Set(1, 1f, true);
            Assert.AreEqual(0f, bank.Level(0), "no frame-one pop");
            Assert.AreEqual(1f, bank.Level(1), "a restored lamp is lit at once");
            bank.Tick(0.5f, 2f);
            Assert.AreEqual(0.25f, bank.Level(0), Tolerance, "a quarter of the way after a quarter of the warm-up");
            bank.Tick(10f, 2f);
            Assert.AreEqual(1f, bank.Level(0), "and lit, never past it");
        }

        [Test]
        public void Tally_CountsDecodedAndBuiltNodes_Once()
        {
            var tally = new TechNodeTally();
            tally.Apply(TechNodeId.TowerL1, TechNodeState.Built);
            tally.Apply(TechNodeId.HoverJump, TechNodeState.Decoded);
            tally.Apply(TechNodeId.HoverJump, TechNodeState.Built);
            Assert.AreEqual(2, tally.KnownCount, "a decoded node that gets built still counts once");
            Assert.AreEqual(TechNodeState.Built, tally.StateOf(TechNodeId.HoverJump));
            tally.Apply(TechNodeId.HoverJump, TechNodeState.Unknown);
            Assert.AreEqual(1, tally.KnownCount);
        }

        [Test]
        public void RecoveryRow_PlacesItsLamps_TheLinksLastInThePeakSocket()
        {
            const int Sockets = 8;
            Assert.AreEqual(Sockets - 1, RecoveryRowLamps.Socket(4, 5, Sockets, true), "the Peak's foot");
            Assert.AreEqual(2, RecoveryRowLamps.Socket(2, 5, Sockets, true));
            Assert.IsTrue(RecoveryRowLamps.Shows(3, 5, Sockets, true));
            Assert.IsFalse(RecoveryRowLamps.Shows(5, 5, Sockets, true), "the sockets between stay hidden");
            Assert.IsTrue(RecoveryRowLamps.Shows(Sockets - 1, 5, Sockets, true));
            Assert.IsFalse(RecoveryRowLamps.Lit(Sockets - 1, 4, 5, Sockets, true), "the Peak is lit last");
            Assert.IsTrue(RecoveryRowLamps.Lit(Sockets - 1, 5, 5, Sockets, true));
            Assert.IsTrue(RecoveryRowLamps.Shows(2, 3, 3, false));
            Assert.IsFalse(RecoveryRowLamps.Shows(3, 3, 4, false), "a plain row shows only its lamps");
            Assert.IsFalse(RecoveryRowLamps.Shows(0, 0, Sockets, true), "an empty row shows nothing");
        }

        [Test]
        public void ChartPen_PlotsRoundHome_AndStopsAtTheEdge()
        {
            var home = new Vector3(-10f, 0f, 8f);
            Assert.AreEqual(new Vector2(0.5f, 0.5f), ChartPenPlot.OnPaper(home, home, 300f), "home in the middle");
            Vector2 east = ChartPenPlot.OnPaper(home + new Vector3(150f, 4f, 0f), home, 300f);
            Assert.AreEqual(0.75f, east.x, Tolerance, "half way to the edge");
            Assert.AreEqual(0.5f, east.y, Tolerance);
            Assert.AreEqual(Vector2.one, ChartPenPlot.OnPaper(home + new Vector3(900f, 0f, 900f), home, 300f),
                "beyond the paper, drawn at its edge");
        }

        [Test]
        public void HaloRule_HomesPilotKeepsToItsLampWithin60m()
        {
            var settings = new HomeLightingSettings();
            Assert.GreaterOrEqual(settings.PilotHaloFrom, 60f,
                "the beacon's minimum size only applies beyond 60 m (M3-19 §8)");
            Assert.LessOrEqual(settings.PilotRadius, 0.15f, "up close, a tight glow on the lamp's own housing");
        }
    }
}
