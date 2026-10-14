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
    /// ring fitted and the tether wakes; Kenji's spare crate cut open gives Ro's tape deck, which glides onto the tray
    /// and is fitted on 07 once it parks, and saved; an older save's keepsake deck stays fitted; a
    /// loaded game has both back at once.
    /// </summary>
    public sealed class GaragePortSessions : InputTestFixture
    {
        private const string Duck = "rubber_duck";

        /// <summary>07 stands this far (m) in front of a seal to cut it.</summary>
        private const float SealStandOff = 1.4f;

        private InputActionAsset _controls;
        private GameplayFixture _fixture;
        private Mouse _mouse;

        public override void Setup()
        {
            base.Setup();
            _mouse = InputSystem.AddDevice<Mouse>();
            InputSystem.AddDevice<Keyboard>();
            _controls = BootstrapHarness.LoadControlsCopy();
        }

        public override void TearDown()
        {
            _fixture?.Dispose();
            Object.Destroy(_controls);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator ThePort_FitsTheRing_ThenRosDeckFromKenjisSpareCrate()
        {
            _fixture = GameplayFixture.Boot(_controls, tether: false);
            yield return null;
            GameplaySystem gameplay = _fixture.Gameplay;
            GaragePort port = gameplay.Port;
            EventBus events = _fixture.Bootstrap.Context.Events;
            var fittings = new List<PartFitted>();
            var lines = new List<string>();
            using var fitted = events.Subscribe<PartFitted>(fittings.Add);
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
            Assert.IsTrue(port.AtGarage);
            StringAssert.StartsWith(ProgressionKeys.NextGarageScrap + ":", port.StepLine(),
                "step (a): clear the scrap around the garage, with remaining count");
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
            yield return null;
            Assert.AreEqual(ProgressionKeys.NextPort, port.StepLine(), "step (b): port open, park on the pad");
            yield return Waits.Until(() => port.RingFitted, 4f);
            Assert.IsTrue(port.IsOpen, "the last cut opened it");
            Assert.AreEqual(ProgressionKeys.NextGarageCrate, port.StepLine(), "step (c): cut the 07 · SPARE crate");
            Assert.AreEqual(RoverKitPiece.TetherRing, fittings[0].Part);
            Assert.AreEqual(RoverSocket.Tether, fittings[0].Socket, "fitted through 07's equipment (0.5.5 §3.5)");
            Assert.IsFalse(fittings[0].Gift, "the port's arm fits it");
            Assert.IsTrue(_fixture.Rover.Has(RoverAbility.Tether), "the tether wakes with the ring");

            SealedContainer crate = port.SpareCrate;
            Assert.AreEqual(ContainerKind.Crate, crate.Kind, "Kenji's \"07 · SPARE\" crate (0.5.5 §2)");
            Assert.IsTrue(crate.Available, "with the ring on, its seal glows");
            Assert.IsNull(gameplay.Relics.Find(TapeDeckMigration.LegacyDeckRelic), "the deck is no keepsake");
            Vector3 front = crate.CutPoint.position + crate.Root.forward * SealStandOff;
            front.y = 0f;
            Vector3 back = -crate.Root.forward;
            _fixture.Rover.Place(front, Mathf.Atan2(back.x, back.z) * Mathf.Rad2Deg);
            yield return null;
            yield return SealCut.Hold(this, gameplay, crate, "Kenji's spare crate");
            yield return Waits.Until(() => port.DeckInTheCrate,
                _fixture.ProgressionTuning.PartAppearSeconds + 1f);
            Assert.IsTrue(port.DeckInTheCrate, "Ro's tape deck rises to the crate's mouth");
            Assert.AreEqual(ProgressionKeys.NextGarageDeck, port.StepLine(),
                "step (d): put the tape deck on the tray, then park on the pad again");
            yield return PickupPress.Take(this, gameplay, 2f, "Ro's tape deck");
            yield return Waits.Until(() => port.DeckOnTheTray, _fixture.ProgressionTuning.PortTrayGlideSeconds + 1f);
            Assert.IsTrue(port.DeckOnTheTray, "it glides onto the port's tray");
            Assert.AreEqual(ProgressionKeys.NextGarageDeck, port.StepLine(), "still step (d) until parked on the pad");
            Assert.AreEqual(1, fittings.Count, "on the tray it waits for 07 to park");
            _fixture.Rover.Place(port.PadCentre, 0f);
            yield return Waits.Until(() => port.DeckFitted, 4f);
            Assert.AreEqual(string.Empty, port.StepLine(), "clears when the garage beat is done");
            Assert.AreEqual(2, fittings.Count);
            Assert.AreEqual(RoverKitPiece.TapeDeck, fittings[1].Part, "the arm fits the deck on 07");
            Assert.AreEqual(RoverSocket.Deck, fittings[1].Socket);

            GaragePortSaveData saved = port.Capture();
            Assert.IsTrue(saved.ringFitted && saved.deckFitted && saved.placeSeen && saved.tetherHinted);
            fittings.Clear();
            port.Restore(saved);
            port.Announce();
            Assert.IsEmpty(fittings, "a load publishes nothing: the rover reads the equipment");
            Assert.IsTrue(gameplay.Equipment.IsFitted(RoverKitPiece.TetherRing), "the ring is back at once");
            Assert.IsTrue(gameplay.Equipment.IsFitted(RoverKitPiece.TapeDeck), "and the deck");
        }

        [UnityTest]
        public IEnumerator AnOldSavesKeepsakeDeck_StaysFitted_AndKenjisCrateStandsOpen()
        {
            _fixture = GameplayFixture.Boot(_controls, tether: false);
            yield return null;
            GaragePort port = _fixture.Gameplay.Port;
            var relics = new RelicsSaveData
            {
                relics = new[] { new RelicSaveData { id = TapeDeckMigration.LegacyDeckRelic, state = 9 } },
            };
            Assert.IsTrue(TapeDeckMigration.WasFitted(relics), "the old keepsake deck was fitted on 07");
            Assert.IsFalse(TapeDeckMigration.WasFitted(new RelicsSaveData
            {
                relics = new[] { new RelicSaveData { id = TapeDeckMigration.LegacyDeckRelic, state = 2 } },
            }), "one only dug up goes back into the crate");
            port.AdoptLegacyDeck();
            port.Announce();
            Assert.IsTrue(port.DeckFitted);
            Assert.IsTrue(_fixture.Gameplay.Equipment.IsFitted(RoverKitPiece.TapeDeck), "on 07 at once");
            Assert.IsTrue(port.SpareCrate.StandsOpen, "and the crate it came from stands open");
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
