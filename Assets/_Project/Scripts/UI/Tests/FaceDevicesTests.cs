using System;
using NUnit.Framework;
using UnityEngine;
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
        public void FromTheRight_OfAHoldRow_TheNearestDeviceIsSelected()
        {
            Assert.AreEqual(FaceDevices.Scan, FaceDevices.NearestTo(FaceDevices.CenterOf(FaceDevices.Scan).y));
            Assert.AreEqual(FaceDevices.SocketTwo, FaceDevices.NearestTo(700f));
            Assert.AreEqual(FaceDevices.Scan, FaceDevices.NearestTo(0f));
        }

        [Test]
        public void EveryDevice_HasANameKey_AndASlotInsideTheRightPane()
        {
            for (int device = 0; device < FaceDevices.Count; device++)
            {
                StringAssert.StartsWith("ui.device.", FaceDevices.NameKey(device));
                Vector2 centre = FaceDevices.CenterOf(device);
                Assert.Greater(centre.x, FaceInventoryView.DividerX, $"device {device} is right of the divider");
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
