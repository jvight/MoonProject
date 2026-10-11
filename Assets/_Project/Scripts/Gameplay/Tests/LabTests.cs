using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Gameplay.Lift;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// The crew's lab (docs/features/M3-19 §5): the turntable's eased turns, the stick's one command per push, the
    /// lab's announcements and the reader's feed and decode.
    /// </summary>
    public sealed class LabTests
    {
        private const float Frame = 1f / 60f;

        [Test]
        public void TheTurntable_TurnsEased_TakesLongerTheFurtherItGoes_AndRestsAtTheStop()
        {
            var table = new Turntable(1f, 0.5f);
            table.Snap(350f);
            Assert.IsTrue(table.TurnTo(80f, 1), "a quarter turn clockwise, across north");
            Assert.IsTrue(table.Turning);
            Assert.IsFalse(table.Step(0.1f));
            Assert.That(Mathf.DeltaAngle(350f, table.Yaw), Is.InRange(0f, 9f), "eased: slow away from the stop");
            Assert.IsFalse(table.Step(0.85f));
            Assert.IsTrue(table.Step(0.1f), "a quarter turn takes its second");
            Assert.AreEqual(80f, table.Yaw, 1e-3f);
            Assert.IsFalse(table.Turning);

            Assert.IsTrue(table.TurnTo(260f, -1), "a half turn counter-clockwise");
            Assert.IsFalse(table.Step(1.9f));
            Assert.IsTrue(table.Step(0.2f), "a half turn takes two");
            Assert.AreEqual(260f, table.Yaw, 1e-3f);

            Assert.IsFalse(table.TurnTo(260f, 0), "already there: nothing to turn");
            Assert.AreEqual(-90f, Turntable.Delta(0f, 270f, 0), 1e-3f, "the short way round");
            Assert.AreEqual(270f, Turntable.Delta(0f, 270f, 1), 1e-3f, "clockwise when asked");
            Assert.AreEqual(-270f, Turntable.Delta(0f, 90f, -1), 1e-3f, "counter-clockwise when asked");
        }

        [Test]
        public void TheStick_GivesOneCommandPerPush_OnItsMainAxis()
        {
            var latch = new LabCommandLatch();
            Assert.AreEqual(LabCommand.None, latch.Step(new Vector2(1f, 0f), 0.6f, 0.3f),
                "a fresh latch waits for the stick to come back first");
            Assert.AreEqual(LabCommand.None, latch.Step(Vector2.zero, 0.6f, 0.3f));
            Assert.AreEqual(LabCommand.TurnRight, latch.Step(new Vector2(0.9f, 0.2f), 0.6f, 0.3f));
            Assert.AreEqual(LabCommand.None, latch.Step(new Vector2(0.9f, 0.2f), 0.6f, 0.3f), "held: once");
            Assert.AreEqual(LabCommand.None, latch.Step(new Vector2(0.2f, 0f), 0.6f, 0.3f));
            Assert.AreEqual(LabCommand.TurnLeft, latch.Step(new Vector2(-0.7f, 0f), 0.6f, 0.3f));
            latch.Step(Vector2.zero, 0.6f, 0.3f);
            Assert.AreEqual(LabCommand.Look, latch.Step(new Vector2(0.1f, 0.8f), 0.6f, 0.3f));
            latch.Step(Vector2.zero, 0.6f, 0.3f);
            Assert.AreEqual(LabCommand.Down, latch.Step(new Vector2(0f, -1f), 0.6f, 0.3f));
            latch.Step(Vector2.zero, 0.6f, 0.3f);
            latch.Disarm();
            Assert.AreEqual(LabCommand.None, latch.Step(new Vector2(0f, -1f), 0.6f, 0.3f), "disarmed: wait");
        }

        [Test]
        public void TheLab_AnnouncesEnteringFacingLookingAndLeaving_Once()
        {
            var events = new EventBus();
            var log = new List<string>();
            events.Subscribe<LabEntered>(e => log.Add("entered " + e.Facing));
            events.Subscribe<LabStationFaced>(e => log.Add("faced " + e.Station));
            events.Subscribe<LabStationLooked>(e => log.Add((e.Looking ? "look " : "away ") + e.Station));
            events.Subscribe<LabLeft>(_ => log.Add("left"));
            var lab = new LabInterior(events);
            lab.Look(true);
            Assert.IsEmpty(log, "nothing to look at from outside");

            lab.Enter(LabStation.Reader);
            lab.Faced();
            lab.Look(true);
            lab.Look(true);
            lab.TurnTo(LabInterior.Next(LabStation.Reader, 1));
            Assert.AreEqual(LabStation.Board, lab.Facing);
            lab.Faced();
            lab.Look(true);
            lab.Leave();
            lab.Leave();
            CollectionAssert.AreEqual(new[]
            {
                "entered Reader", "faced Reader", "look Reader", "away Reader", "faced Board", "look Board",
                "away Board", "left",
            }, log);
            Assert.IsFalse(lab.IsInside);
            Assert.AreEqual(LabStation.Keepsakes, LabInterior.Next(LabStation.Chart, -1), "round the ring");
        }

        [Test]
        public void TheReader_FeedsTheTubeIn_ThenDecodesIt_OneAtATime()
        {
            var reader = new ReaderDecode(1f, 4f);
            Assert.AreEqual(ReaderStep.None, reader.Step(1f), "idle: nothing to read");
            reader.Feed("rover.cargo_cradle");
            Assert.AreEqual(ReaderPhase.Feeding, reader.Phase);
            Assert.Throws<System.InvalidOperationException>(() => reader.Feed("rover.boost_coils"), "one at a time");
            Assert.AreEqual(ReaderStep.None, Run(reader, 0.9f));
            Assert.AreEqual(ReaderStep.Taken, Run(reader, 0.2f), "the beam has fed the tube in");
            Assert.AreEqual(ReaderPhase.Decoding, reader.Phase);
            Run(reader, 2f);
            Assert.AreEqual(0.5f, reader.DecodeProgress, 0.02f);
            Assert.AreEqual(ReaderStep.Decoded, Run(reader, 2.1f));
            Assert.AreEqual(ReaderPhase.Idle, reader.Phase);
            Assert.AreEqual("rover.cargo_cradle", reader.Kit);

            reader.Resume("rover.boost_coils");
            Assert.AreEqual(ReaderPhase.Decoding, reader.Phase, "a loaded tube decodes again, no feeding");
            Assert.AreEqual(ReaderStep.Decoded, Run(reader, 4.1f));
        }

        private static ReaderStep Run(ReaderDecode reader, float seconds)
        {
            ReaderStep first = ReaderStep.None;
            for (float t = 0f; t < seconds; t += Frame)
            {
                ReaderStep step = reader.Step(Frame);
                first = first == ReaderStep.None ? step : first;
            }

            return first;
        }
    }
}
