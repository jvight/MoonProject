using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Testing;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// Scripted sessions for the relay network (docs/features/M3-06) with stand-in masts on the flat world's relay
    /// pads: gathering a mast's part, restoring it with the materials it takes, the link pulse and the reach it adds; a
    /// mast beyond the lit frontier listening until a neighbour or a stronger tower links it; the radio-hop home and
    /// back; and all of it kept through a save and a reboot.
    /// </summary>
    public sealed class RelaySessions : InputTestFixture
    {
        private const string Tower = "radio_tower";

        /// <summary>07 stands this far (m) in front of a mast's junction box to restore it.</summary>
        private const float FootStandOff = 3f;

        private InputActionAsset _controls;
        private GameplayFixture _fixture;
        private Keyboard _keyboard;

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _controls = BootstrapHarness.LoadControlsCopy();
        }

        public override void TearDown()
        {
            _fixture?.Dispose();
            Object.Destroy(_controls);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator Relay_IsRestoredWithItsPartAndMaterials_ComesOnline_AndHomeReachesFarther()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            RelayField relays = _fixture.Gameplay.Relays;
            var reach = _fixture.Bootstrap.Context.Get<IStationReach>();
            Assert.AreSame(relays.Reach, reach);
            Assert.AreSame(relays, _fixture.Bootstrap.Context.Get<IRelayStatus>());
            Assert.AreEqual(5, reach.NodeCount, "home and four masts");
            Assert.AreEqual(1, reach.LitCount);
            RelayMast mast = relays.Masts[0];
            Assert.AreEqual("relay.0", mast.Id);
            Assert.LessOrEqual(SurfaceRules.HorizontalDistance(mast.PartRest, mast.Anchor.Position), 40f,
                "its part glints nearby");
            Vector3 beyond = mast.Anchor.Position - mast.Anchor.Forward * 80f;
            Assert.IsFalse(reach.IsInReach(beyond), "past the mast, beyond the dark tower's reach");

            yield return Gather(mast);
            Assert.AreEqual(RelayPartState.Held, mast.PartState);
            Assert.AreEqual(RelayCue.PartCollected, _fixture.Events.RelayCued[0].Value.Cue);

            AtTheFoot(mast);
            yield return null;
            yield return null;
            Assert.IsTrue(_fixture.Gameplay.Hints.TryGet(InteractionKind.Restore, out InteractionHint hint));
            Assert.IsFalse(hint.Ready, "materials it does not have yet");
            Assert.AreEqual(3, relays.NextCost.Total);
            yield return HoldInteract(_fixture.RelayTuning.RestoreHold + 0.4f);
            Assert.IsFalse(mast.Restoring, "no restoration on credit");

            _fixture.GiveMaterials(3, 1, 1);
            yield return null;
            Assert.IsTrue(_fixture.Gameplay.Hints.TryGet(InteractionKind.Restore, out hint));
            Assert.IsTrue(hint.Ready);
            Assert.AreEqual(InteractionKind.Restore, _fixture.Gameplay.Hints.Primary.Kind);
            yield return HoldInteract(_fixture.RelayTuning.RestoreHold + 0.2f);
            Assert.IsTrue(mast.Restoring);
            Assert.AreEqual(3, mast.Paid, "its recipe paid: 2 metal and 1 wiring");
            Assert.AreEqual(1, _fixture.Gameplay.Materials.Metal);
            Assert.AreEqual(4, relays.NextCost.Total, "the next one costs more");
            Assert.AreEqual(1, _fixture.Rover.HoldStillCount, "07 holds still under its beam");
            _fixture.Rover.Aim(mast.Anchor.Position + mast.Anchor.Forward * 9f + Vector3.up * 3f,
                mast.Anchor.Position + Vector3.up * 3f);
            RelayBeat beat = RelayBeat.For(_fixture.RelayTuning);
            yield return new WaitForSeconds(beat.StitchEnd + 0.5f);
            Assert.AreEqual(0, _fixture.Rover.HoldStillCount, "the beam lets go");
            Assert.IsTrue(mast.Restored.Visible, "the mast straightens");
            _fixture.Capture("relay-straightening");
            yield return new WaitForSeconds(beat.Duration - beat.StitchEnd);

            Assert.AreEqual(1, _fixture.Events.RelayRestored.Count);
            RelayRestored restored = _fixture.Events.RelayRestored[0].Value;
            Assert.AreEqual("relay.0", restored.RelayId);
            Assert.AreEqual(mast.Restored.Lamp.position, restored.Position, "where 07 and the camera look up to");
            Assert.AreEqual(1, restored.LitCount);
            Assert.AreEqual(4, restored.Total);
            Assert.AreEqual("home", restored.LinkedNodeId, "its pulse runs home");
            float way = SurfaceRules.HorizontalDistance(mast.Anchor.Position, Vector3.zero);
            Assert.AreEqual(way / _fixture.RelayTuning.PulseSpeed, restored.PulseSeconds, 1e-3f,
                "when the pulse reaches home");
            RelayNode node = reach.GetNode(mast.Node);
            Assert.AreEqual(_fixture.RelayTuning.MastReach, node.Radius);
            Assert.Less(Vector3.Distance(mast.Restored.Lamp.position, node.LampPosition), 1e-3f, "its lamp, upright");
            Assert.Less(Vector3.Distance(_fixture.Gameplay.Tower.BeaconPosition, reach.GetNode(0).LampPosition),
                1e-3f, "home's lamp is the tower's beacon");
            Assert.AreEqual(_fixture.RadioTowerUpgrade.SignalRadiusAt(0), reach.GetNode(0).Radius,
                "home's reach is the dark tower's");
            TickerLine line = _fixture.Events.TickerLine[_fixture.Events.TickerLine.Count - 1].Value;
            Assert.AreEqual("ticker.relay.online", line.Key);
            Assert.AreEqual("1", line.Argument);
            CollectionAssert.AreEqual(
                new[]
                {
                    RelayCue.PartCollected, RelayCue.Stitched, RelayCue.PartSlotted, RelayCue.Straightened,
                    RelayCue.LampWarmed,
                },
                Cues());
            Assert.AreEqual(RelayPartState.Installed, mast.PartState);
            Assert.AreEqual(_fixture.RelayTuning.LampGlow, mast.Restored.LampLevel, 0.05f, "its lamp is warm");
            Assert.IsTrue(mast.Pulse.Running, "a pulse runs home along the ground");
            Assert.AreEqual(2, reach.LitCount);
            Assert.IsTrue(reach.IsInReach(beyond), "home reaches past the mast now");
            Assert.AreEqual(1, relays.LitMasts);
            _fixture.Capture("relay-online");
            yield return new WaitForSeconds(5f);
            Assert.IsFalse(mast.Pulse.Running, "it pulses once");
        }

        [UnityTest]
        public IEnumerator Relay_BeyondTheFrontier_Listens_UntilANeighbourOrTheTowerLinksIt()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            RelayField relays = _fixture.Gameplay.Relays;
            RelayMast mound = relays.Masts[0];
            RelayMast shoulder = relays.Masts[1];
            RelayMast mouth = relays.Masts[2];
            _fixture.GiveMaterials(8, 4, 0);

            yield return Restore(mouth);
            Assert.IsEmpty(_fixture.Events.RelayRestored, "nothing lit reaches the canyon mouth yet");
            Assert.AreEqual("ticker.relay.waiting", LastTicker().Key);
            Assert.AreEqual(_fixture.RelayTuning.WaitingGlow, mouth.Restored.LampLevel, 0.02f, "a listening glow");
            Assert.IsFalse(relays.Reach.IsLit(mouth.Node));

            yield return Restore(mound);
            Assert.AreEqual(1, _fixture.Events.RelayRestored.Count, "the mound comes online first");
            yield return new WaitForSeconds(_fixture.RelayTuning.ChainDelay + 0.3f);
            Assert.AreEqual(2, _fixture.Events.RelayRestored.Count, "...then the mouth, in turn");
            Assert.AreEqual("relay.2", _fixture.Events.RelayRestored[1].Value.RelayId);
            Assert.AreEqual("relay.0", _fixture.Events.RelayRestored[1].Value.LinkedNodeId,
                "the mouth's pulse runs to the mound, the node it links through");
            Assert.AreEqual(2, _fixture.Events.RelayRestored[1].Value.LitCount);
            Assert.Greater(_fixture.Events.RelayRestored[1].Time, _fixture.Events.RelayRestored[0].Time);
            yield return new WaitForSeconds(_fixture.RelayTuning.LampWarm + 0.2f);
            Assert.AreEqual(_fixture.RelayTuning.LampGlow, mouth.Restored.LampLevel, 0.05f);

            yield return Restore(shoulder);
            Assert.AreEqual(2, _fixture.Events.RelayRestored.Count, "the dark tower does not reach the shoulder");
            Assert.AreEqual(3, mouth.Paid, "the first restoration takes 3 units");
            Assert.AreEqual(4, mound.Paid);
            Assert.AreEqual(5, shoulder.Paid);
            _fixture.GiveMaterials(0, 1, 1);
            Assert.AreEqual(PurchaseResult.Purchased, _fixture.Gameplay.Upgrades.Purchase(Tower));
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(3, _fixture.Events.RelayRestored.Count, "the tower's first level links it");
            Assert.AreEqual("relay.1", _fixture.Events.RelayRestored[2].Value.RelayId);
            Assert.AreEqual(4, relays.Reach.LitCount);
        }

        [UnityTest]
        public IEnumerator Hop_FromALitMast_Home_AndBack_ThroughASoftDark()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            RelayField relays = _fixture.Gameplay.Relays;
            IRadioHop hop = _fixture.Bootstrap.Context.Get<IRadioHop>();
            Assert.AreSame(relays.Hop, hop);
            RelayMast mast = relays.Masts[0];
            _fixture.GiveMaterials(2, 1, 0);
            Assert.IsFalse(_fixture.Gameplay.Hints.TryGet(InteractionKind.Hop, out _), "nowhere to hop yet");
            yield return Restore(mast);

            _fixture.Rover.Place(Flat(mast.Anchor.Position), Yaw(mast.Anchor.Forward));
            yield return null;
            yield return null;
            Assert.IsTrue(_fixture.Gameplay.Hints.TryGet(InteractionKind.Hop, out InteractionHint hint));
            Assert.AreEqual(mast.Anchor.Position, hint.Position);
            Press(_keyboard.eKey);
            yield return null;
            yield return null;
            Assert.AreEqual(RadioHopPhase.Choosing, hop.Phase);
            Assert.IsTrue(_fixture.Events.RadioHopListChanged[0].Value.Open, "the camera keeps its wide shot closed");
            Assert.AreEqual(1, hop.ChoiceCount);
            Assert.AreEqual("hop.node.home", hop.ChoiceLabelKey(0));
            Release(_keyboard.eKey);
            yield return null;
            Press(_keyboard.eKey);
            yield return new WaitForSeconds(_fixture.RelayTuning.HopConfirmHold + 0.15f);
            Release(_keyboard.eKey);
            Assert.AreEqual("relay.0", _fixture.Events.RadioHopStarted[0].Value.FromId);
            Assert.AreEqual("home", _fixture.Events.RadioHopStarted[0].Value.ToId);
            Assert.IsFalse(_fixture.Events.RadioHopListChanged[1].Value.Open);
            HopSequence sequence = HopSequence.For(_fixture.RelayTuning);
            yield return new WaitForSeconds(sequence.PlaceAt - 0.3f);
            Assert.Greater(hop.Fade, 0.3f, "the view eases out");
            Assert.AreEqual(0, _fixture.Rover.Placements, "07 stays put until the view is dark");
            yield return new WaitForSeconds(sequence.Duration - sequence.PlaceAt + 0.5f);
            Assert.AreEqual(1, _fixture.Rover.Placements);
            Assert.AreEqual("home", _fixture.Events.RadioHopFinished[0].Value.ToId);
            Assert.Less(SurfaceRules.HorizontalDistance(_fixture.Rover.Position, Vector3.zero), 0.01f,
                "on home's pad");
            Assert.AreEqual(RadioHopPhase.Closed, hop.Phase);
            Assert.AreEqual(0f, hop.Fade);
            Assert.AreEqual(0, _fixture.Rover.HoldStillCount);

            Press(_keyboard.eKey);
            yield return null;
            Release(_keyboard.eKey);
            yield return null;
            Assert.AreEqual(RadioHopPhase.Choosing, hop.Phase, "home is a pad too");
            Assert.AreEqual("hop.node.relay.0", hop.ChoiceLabelKey(0));
            Assert.IsTrue(hop.Confirm(), "the UI may confirm too");
            yield return new WaitForSeconds(sequence.Duration + 0.3f);
            Assert.AreEqual(2, _fixture.Rover.Placements);
            Assert.Less(SurfaceRules.HorizontalDistance(_fixture.Rover.Position, mast.Anchor.Position), 0.01f,
                "back on relay.0's pad");
            Assert.AreEqual(Yaw(-mast.Anchor.Forward), _fixture.Rover.Rotation.eulerAngles.y, 0.5f, "facing out");
        }

        [UnityTest]
        public IEnumerator Restored_PartsAndPayments_ComeBackAfterAReboot_Silently()
        {
            string slot = BootstrapHarness.NewTestSlot();
            _fixture = GameplayFixture.Boot(_controls, slot);
            yield return null;
            RelayField relays = _fixture.Gameplay.Relays;
            _fixture.GiveMaterials(2, 1, 0);
            yield return Restore(relays.Masts[0]);
            yield return Gather(relays.Masts[1]);
            _fixture.Dispose(true);

            _fixture = GameplayFixture.Boot(_controls, slot);
            yield return null;
            yield return null;
            relays = _fixture.Gameplay.Relays;
            RelayMast mound = relays.Masts[0];
            Assert.IsTrue(mound.IsRestored);
            Assert.AreEqual(3, mound.Paid);
            Assert.IsTrue(mound.Restored.Visible, "upright");
            Assert.IsFalse(mound.Broken.Visible);
            Assert.AreEqual(RelayPartState.Installed, mound.PartState);
            Assert.AreEqual(_fixture.RelayTuning.LampGlow, mound.Restored.LampLevel, 1e-3f, "lit at once");
            Assert.IsTrue(relays.Reach.IsLit(mound.Node));
            Assert.AreEqual(RelayPartState.Held, relays.Masts[1].PartState, "the shoulder's part is still held");
            Assert.AreEqual(4, relays.NextCost.Total);
            Assert.IsEmpty(_fixture.Events.RelayRestored, "a load replays no moment");
            Assert.IsFalse(mound.Pulse.Running);
        }

        private IEnumerator Gather(RelayMast mast)
        {
            _fixture.Rover.Place(Flat(mast.PartRest), 0f);
            yield return new WaitForSeconds(_fixture.FriendTuning.PartFlightDuration + 1f);
            Assert.AreEqual(RelayPartState.Held, mast.PartState, $"{mast.Id}'s part is drawn in");
        }

        /// <summary>Gathers the mast's part and restores it with the materials already in the stock.</summary>
        private IEnumerator Restore(RelayMast mast)
        {
            yield return Gather(mast);
            AtTheFoot(mast);
            yield return null;
            yield return HoldInteract(_fixture.RelayTuning.RestoreHold + 0.2f);
            Assert.IsTrue(mast.Restoring || mast.IsRestored, $"{mast.Id}'s restoration begins");
            yield return new WaitForSeconds(RelayBeat.For(_fixture.RelayTuning).Duration + 0.3f);
            Assert.IsFalse(mast.Restoring);
        }

        private void AtTheFoot(RelayMast mast)
        {
            Vector3 foot = mast.Broken.PartSocket.position + mast.Anchor.Forward * FootStandOff;
            _fixture.Rover.Place(Flat(foot), Yaw(-mast.Anchor.Forward));
        }

        private IEnumerator HoldInteract(float seconds)
        {
            Press(_keyboard.eKey);
            yield return new WaitForSeconds(seconds);
            Release(_keyboard.eKey);
            yield return null;
        }

        private RelayCue[] Cues()
        {
            var cues = new RelayCue[_fixture.Events.RelayCued.Count];
            for (int i = 0; i < cues.Length; i++)
            {
                cues[i] = _fixture.Events.RelayCued[i].Value.Cue;
            }

            return cues;
        }

        private TickerLine LastTicker()
        {
            return _fixture.Events.TickerLine[_fixture.Events.TickerLine.Count - 1].Value;
        }

        private static Vector3 Flat(Vector3 point)
        {
            return new Vector3(point.x, 0f, point.z);
        }

        private static float Yaw(Vector3 direction)
        {
            return Mathf.Repeat(Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg, 360f);
        }
    }
}
