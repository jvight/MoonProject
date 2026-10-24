using NUnit.Framework;
using MoonProject.Core;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// The power pulse on a base cable (base-home-and-crater-exits §3): it leaves as soon as power flows, eases along
    /// the cable, glows softly at both ends, arrives once, leaves again every interval; a stop lets the pulse in
    /// flight finish and starts no other. The array feeds while it is not dusk, the bank from dusk.
    /// </summary>
    public sealed class PowerPulseTrainTests
    {
        private const float Interval = 3.6f;
        private const float Travel = 2.4f;
        private const float Step = 0.1f;

        [Test]
        public void WhilePowerFlows_APulseLeavesAtOnce_EasesAlong_AndArrivesOncePerInterval()
        {
            var train = new PowerPulseTrain();
            Assert.IsFalse(train.Step(Step, true, Interval, Travel));
            Assert.IsTrue(train.InFlight, "power flows: a pulse leaves at once");

            float last = train.Along;
            float brightest = 0f;
            int arrivals = 0;
            float firstArrival = -1f;
            float secondLeave = -1f;
            float now = Step;
            while (now < Interval * 2f)
            {
                bool wasInFlight = train.InFlight;
                bool arrived = train.Step(Step, true, Interval, Travel);
                now += Step;
                if (arrived)
                {
                    arrivals++;
                    firstArrival = firstArrival < 0f ? now : firstArrival;
                }

                if (!wasInFlight && train.InFlight && secondLeave < 0f)
                {
                    secondLeave = now;
                }

                if (train.InFlight && arrivals == 0)
                {
                    Assert.GreaterOrEqual(train.Along, last, "the pulse only moves on toward the far end");
                    last = train.Along;
                    brightest = UnityEngine.Mathf.Max(brightest, train.Glow);
                }
            }

            Assert.AreEqual(Travel, firstArrival, Step * 2.5f, "it reaches the far end after its travel time");
            Assert.AreEqual(Interval, secondLeave, Step * 2.5f, "the next leaves an interval after the first");
            Assert.AreEqual(2, arrivals, "one arrival per pulse, never two");
            Assert.Greater(brightest, 0.95f, "full glow mid-way");
        }

        [Test]
        public void ThePulse_GlowsSoftAtBothEnds_AndEasesOutOfThePortAndIntoTheFarEnd()
        {
            var train = new PowerPulseTrain();
            train.Step(0.01f, true, Interval, Travel);
            Assert.Less(train.Glow, 0.05f, "soft as it leaves the port");
            Assert.Less(train.Along, 0.001f, "and slow out of it");

            train.Step(Travel * 0.5f - 0.01f, true, Interval, Travel);
            Assert.AreEqual(0.5f, train.Along, 0.01f);
            Assert.AreEqual(1f, train.Glow, 0.01f);

            train.Step(Travel * 0.5f - 0.02f, true, Interval, Travel);
            Assert.Greater(train.Along, 0.999f, "slow into the far end");
            Assert.Less(train.Glow, 0.05f, "and soft again");
        }

        [Test]
        public void WhenThePowerStops_ThePulseInFlightFinishes_AndNoOtherLeaves()
        {
            var train = new PowerPulseTrain();
            train.Step(Step, true, Interval, Travel);
            int arrivals = 0;
            for (float t = 0f; t < Interval * 3f; t += Step)
            {
                arrivals += train.Step(Step, false, Interval, Travel) ? 1 : 0;
            }

            Assert.AreEqual(1, arrivals, "the pulse already on its way still arrives");
            Assert.IsFalse(train.InFlight, "and no other leaves");

            train.Step(Step, true, Interval, Travel);
            Assert.IsTrue(train.InFlight, "power back: a pulse leaves at once");
            train.Reset();
            Assert.IsFalse(train.InFlight, "the cable laid again: no pulse on it");
            Assert.AreEqual(0f, train.Glow);
        }

        [Test]
        public void TheArrayFeedsWhileItIsNotDusk_TheBankFromDusk_NothingElsePulses()
        {
            Assert.IsTrue(PowerPulse.Flows(StructureIds.SolarArray, false), "sun up: the array feeds");
            Assert.IsFalse(PowerPulse.Flows(StructureIds.SolarArray, true), "dusk: the array stops");
            Assert.IsFalse(PowerPulse.Flows(StructureIds.BatteryBank, false), "by day the bank holds");
            Assert.IsTrue(PowerPulse.Flows(StructureIds.BatteryBank, true), "dusk: the bank feeds");
            Assert.IsFalse(PowerPulse.Flows(StructureIds.ChargingPad, false), "the pad's own cable stays still");
            Assert.IsFalse(PowerPulse.Flows(StructureIds.AntennaMast, true));
        }
    }
}
