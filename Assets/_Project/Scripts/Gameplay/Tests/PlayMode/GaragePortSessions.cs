using System.Collections;
using System.Collections.Generic;
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
    /// Kenji's garage port (docs/design/first-hour-v2.md §3) on the real components behind the fake rover: the tether
    /// is asleep and a loose keepsake says so once; the garage's last cut opens the port; parked on its pad 07 has its
    /// ring fitted and the tether wakes; Ro's tape deck set on the tray with the tether is fitted on 07 and saved; a
    /// loaded game has both back at once.
    /// </summary>
    public sealed class GaragePortSessions : InputTestFixture
    {
        private const string Deck = "cassette_player";
        private const string Duck = "rubber_duck";

        private InputActionAsset _controls;
        private GameplayFixture _fixture;
        private Mouse _mouse;

        public override void Setup()
        {
            base.Setup();
            _mouse = InputSystem.AddDevice<Mouse>();
            _controls = BootstrapHarness.LoadControlsCopy();
        }

        public override void TearDown()
        {
            _fixture?.Dispose();
            Object.Destroy(_controls);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator ThePort_FitsTheRing_ThenRosDeckSetOnItsTray()
        {
            _fixture = GameplayFixture.Boot(_controls, tether: false);
            yield return null;
            GameplaySystem gameplay = _fixture.Gameplay;
            GaragePort port = gameplay.Port;
            EventBus events = _fixture.Bootstrap.Context.Events;
            var fittings = new List<PortFitting>();
            var lines = new List<string>();
            using var fitted = events.Subscribe<PortFitting>(fittings.Add);
            using var ticker = events.Subscribe<TickerLine>(line => lines.Add(line.Key));

            Relic duck = LooseAt(Duck, _fixture.Rover.Position + new Vector3(1f, 0.6f, 5f));
            yield return Aim(duck);
            Press(_mouse.rightButton, queueEventOnly: true);
            yield return new WaitForSeconds(0.5f);
            Release(_mouse.rightButton);
            Assert.IsFalse(duck.IsTethered, "no tether before Kenji's port");
            Assert.AreEqual(TetherAimState.Asleep, gameplay.Tether.State, "nothing to hold with: no reticle, no icon");
            Assert.AreEqual(1, lines.FindAll(key => key == ProgressionKeys.NotYetTether).Count,
                "the keepsake says once where the tether can be found again");

            _fixture.Rover.Place(port.PadCentre, 0f);
            yield return new WaitForSeconds(2f);
            Assert.IsFalse(port.IsOpen, "the garage still stands whole: the port waits");
            Assert.IsEmpty(fittings);

            gameplay.Salvage.Restore(new SalvageSaveData
            {
                sites = new[]
                {
                    new SalvageSiteSaveData
                    {
                        id = WorldAnchorIds.SitePrefix + "garage", discovered = true,
                        taken = new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 },
                    },
                },
            });
            yield return Waits.Until(() => port.RingFitted, 4f);
            Assert.IsTrue(port.IsOpen, "the last cut opened it");
            Assert.AreEqual(RoverKitPiece.TetherRing, fittings[0].Piece);
            Assert.IsFalse(fittings[0].Restored);
            Assert.IsTrue(_fixture.Rover.Has(RoverAbility.Tether), "the tether wakes with the ring");

            Relic deck = LooseAt(Deck, port.TrayPosition + Vector3.up * 0.3f);
            yield return Aim(deck);
            Press(_mouse.rightButton, queueEventOnly: true);
            yield return Waits.Until(() => port.DeckFitted, 6f);
            Release(_mouse.rightButton);
            Assert.AreEqual(2, fittings.Count);
            Assert.AreEqual(RoverKitPiece.TapeDeck, fittings[1].Piece, "the arm fits the deck on 07's flank");
            Assert.AreEqual(RelicState.Fitted, deck.State);
            Assert.IsFalse(deck.gameObject.activeSelf, "it rides on 07 now, nowhere on the ground");

            GaragePortSaveData saved = port.Capture();
            Assert.IsTrue(saved.ringFitted && saved.placeSeen && saved.tetherHinted);
            fittings.Clear();
            port.Restore(saved);
            port.Announce();
            Assert.AreEqual(2, fittings.Count, "a loaded game has the ring and the deck back at once");
            Assert.IsTrue(fittings[0].Restored && fittings[1].Restored);
        }

        private Relic LooseAt(string id, Vector3 position)
        {
            Relic relic = _fixture.FindRelic(id);
            relic.BeginLift();
            relic.SetLiftPose(position, Quaternion.identity, 1f);
            relic.Surface(Vector3.zero, Vector3.zero);
            return relic;
        }

        private IEnumerator Aim(Relic relic)
        {
            Vector3 camera = _fixture.Rover.Camera.transform.position;
            _fixture.Rover.Aim(camera, relic.transform.position);
            yield return null;
            yield return null;
        }
    }
}
