using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The four devices of 07's socket display and the words for 07's systems (docs/design/opening-v2.md §10): which
    /// system each shows, what it says in each state, and where the selection goes between them.
    /// </summary>
    public sealed class FaceDevicesTests
    {
        [Test]
        public void TheDevices_AreTheLens_TheCutter_AndTheTwoEmptySockets()
        {
            Assert.AreEqual(RoverSystem.Scan, FaceDevices.SystemOf(FaceDevices.Scan));
            Assert.AreEqual(RoverSystem.Cutter, FaceDevices.SystemOf(FaceDevices.Cutter));
            Assert.AreEqual(RoverSystem.Sockets, FaceDevices.SystemOf(FaceDevices.SocketOne));
            Assert.AreEqual(RoverSystem.Sockets, FaceDevices.SystemOf(FaceDevices.SocketTwo));
            Assert.AreEqual(4, FaceDevices.Count);
        }

        [Test]
        public void OnlyTheCutter_OffersTheIgniter_AndOnlyWhenTheRoverSaysItCanBeFitted()
        {
            for (int device = 0; device < FaceDevices.Count; device++)
            {
                Assert.IsFalse(FaceDevices.OffersIgniter(device, false), $"device {device} without the pad");
                Assert.AreEqual(device == FaceDevices.Cutter, FaceDevices.OffersIgniter(device, true));
            }
        }

        [Test]
        public void TheCutterDescription_FollowsItsFault_ThenTheOffer_ThenTheMend()
        {
            Assert.AreEqual("ui.device.cutter.fault",
                FaceDevices.DescriptionKey(FaceDevices.Cutter, RoverSystemState.Fault, false));
            Assert.AreEqual("ui.device.cutter.fit",
                FaceDevices.DescriptionKey(FaceDevices.Cutter, RoverSystemState.Fault, true));
            Assert.AreEqual("ui.device.cutter.ok",
                FaceDevices.DescriptionKey(FaceDevices.Cutter, RoverSystemState.Ok, false));
            Assert.AreEqual("ui.device.scan.ok",
                FaceDevices.DescriptionKey(FaceDevices.Scan, RoverSystemState.Ok, true));
            Assert.AreEqual("ui.device.socket.empty",
                FaceDevices.DescriptionKey(FaceDevices.SocketTwo, RoverSystemState.Empty, false));
        }

        [Test]
        public void TheKeys_FollowTheScreen_AcrossAndDownBetweenTheFourFrames()
        {
            Assert.AreEqual(FaceDevices.Scan, FaceDevices.Step(FaceDevices.Cutter, NavigationMoveEvent.Direction.Right));
            Assert.AreEqual(FaceDevices.Cutter, FaceDevices.Step(FaceDevices.Scan, NavigationMoveEvent.Direction.Left));
            Assert.AreEqual(FaceDevices.SocketOne, FaceDevices.Step(FaceDevices.Cutter, NavigationMoveEvent.Direction.Down));
            Assert.AreEqual(FaceDevices.SocketTwo, FaceDevices.Step(FaceDevices.Scan, NavigationMoveEvent.Direction.Down));
            Assert.AreEqual(FaceDevices.Cutter, FaceDevices.Step(FaceDevices.SocketOne, NavigationMoveEvent.Direction.Up));
            Assert.AreEqual(FaceDevices.Scan, FaceDevices.Step(FaceDevices.SocketTwo, NavigationMoveEvent.Direction.Up));
            Assert.AreEqual(FaceDevices.SocketOne, FaceDevices.Step(FaceDevices.SocketTwo, NavigationMoveEvent.Direction.Left));
            Assert.AreEqual(FaceDevices.SocketTwo, FaceDevices.Step(FaceDevices.SocketOne, NavigationMoveEvent.Direction.Right));
            Assert.AreEqual(-1, FaceDevices.Step(FaceDevices.Cutter, NavigationMoveEvent.Direction.Left),
                "left from a left frame leads to the hold, which the view enters");
            Assert.AreEqual(-1, FaceDevices.Step(FaceDevices.SocketOne, NavigationMoveEvent.Direction.Down));
            Assert.AreEqual(-1, FaceDevices.Step(FaceDevices.Scan, NavigationMoveEvent.Direction.Up));
            Assert.AreEqual(-1, FaceDevices.Step(FaceDevices.Scan, NavigationMoveEvent.Direction.Right));
        }

        [Test]
        public void TheSteps_MatchWhereTheFramesStand()
        {
            foreach (NavigationMoveEvent.Direction direction in new[]
                     {
                         NavigationMoveEvent.Direction.Left, NavigationMoveEvent.Direction.Right,
                         NavigationMoveEvent.Direction.Up, NavigationMoveEvent.Direction.Down,
                     })
            {
                for (int device = 0; device < FaceDevices.Count; device++)
                {
                    int next = FaceDevices.Step(device, direction);
                    if (next < 0)
                    {
                        continue;
                    }

                    Vector2 delta = FaceLayout.DeviceFrame(next).center - FaceLayout.DeviceFrame(device).center;
                    bool across = direction == NavigationMoveEvent.Direction.Left ||
                                  direction == NavigationMoveEvent.Direction.Right;
                    float along = across ? delta.x : delta.y;
                    bool forward = direction == NavigationMoveEvent.Direction.Right ||
                                   direction == NavigationMoveEvent.Direction.Down;
                    Assert.AreEqual(forward, along > 0f, $"{direction} from device {device}");
                    Assert.AreEqual(0f, across ? delta.y : delta.x, 1e-3f, $"{direction} from device {device}");
                }
            }
        }

        [Test]
        public void EachDevice_HasAFaceIcon_AndOnlyBrokenStatesReadAsFaults()
        {
            for (int device = 0; device < FaceDevices.Count; device++)
            {
                CollectionAssert.Contains(RoverDiagramData.FaceIconIds, FaceDevices.IconId(device));
            }

            Assert.IsFalse(FaceDevices.IsFaulty(RoverSystemState.Ok));
            Assert.IsFalse(FaceDevices.IsFaulty(RoverSystemState.Empty));
            Assert.IsTrue(FaceDevices.IsFaulty(RoverSystemState.Fault));
            Assert.IsTrue(FaceDevices.IsFaulty(RoverSystemState.Jammed));
            Assert.IsTrue(FaceDevices.IsFaulty(RoverSystemState.ReceiveOnly));
        }

        [Test]
        public void EveryDevice_HasANameKey_AndAFrameInsideTheScreenMargins()
        {
            for (int device = 0; device < FaceDevices.Count; device++)
            {
                StringAssert.StartsWith("ui.device.", FaceDevices.NameKey(device));
                Rect frame = FaceLayout.DeviceFrame(device);
                Assert.GreaterOrEqual(frame.xMin, FaceLayout.Margin, $"device {device}");
                Assert.LessOrEqual(frame.xMax, FaceLayout.RightEdge, $"device {device}");
                Assert.AreEqual(frame.center, FaceDevices.CenterOf(device));
            }
        }

        [Test]
        public void EverySystemAndState_HasWordsToSay_AndTheCutterFaultNamesItsCause()
        {
            foreach (RoverSystem system in Enum.GetValues(typeof(RoverSystem)))
            {
                StringAssert.StartsWith("ui.system.", SystemText.NameKey(system));
                foreach (RoverSystemState state in Enum.GetValues(typeof(RoverSystemState)))
                {
                    StringAssert.StartsWith("ui.system.state.", SystemText.StateKey(system, state));
                }
            }

            Assert.AreEqual("ui.system.state.fault_igniter",
                SystemText.StateKey(RoverSystem.Cutter, RoverSystemState.Fault));
            Assert.AreEqual("ui.system.state.fault", SystemText.StateKey(RoverSystem.Antenna, RoverSystemState.Fault));
        }
    }
}
