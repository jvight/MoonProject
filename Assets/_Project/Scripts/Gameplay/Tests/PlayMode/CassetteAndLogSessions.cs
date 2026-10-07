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
    /// Scripted sessions for M3-05's pickups: Ro's cassettes joining the radio program, kept through a save and a
    /// reboot.
    /// </summary>
    public sealed class CassetteAndLogSessions : InputTestFixture
    {
        private const string DustAndHoney = "dust_and_honey";

        private InputActionAsset _controls;
        private GameplayFixture _fixture;

        public override void Setup()
        {
            base.Setup();
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
        public IEnumerator BasinCassette_PopsOutInto07_JoinsTheRadio_AndStaysCollected()
        {
            string slot = BootstrapHarness.NewTestSlot();
            _fixture = GameplayFixture.Boot(_controls, slot);
            yield return null;
            IRadioProgram radio = _fixture.Bootstrap.Context.Get<IRadioProgram>();
            Assert.AreSame(_fixture.Gameplay.Radio, radio);
            Assert.AreEqual(3, radio.TotalTapeCount);
            Assert.AreEqual(0, radio.OwnedTapeCount);
            Assert.IsFalse(radio.DialUnlocked);
            Assert.AreEqual(1, _fixture.Events.RadioProgramChanged.Count, "announced once the save is loaded");

            CassetteField field = _fixture.Gameplay.Cassettes;
            int basin = IndexOf(field, DustAndHoney);
            CassetteSite site = field.Site(basin);
            Vector2 band = _fixture.CassetteTuning.Distance;
            Assert.That(SurfaceRules.HorizontalDistance(site.Position, Vector3.zero), Is.InRange(band.x, band.y));
            foreach (RelicSite relic in _fixture.Gameplay.Relics.Sites)
            {
                Assert.GreaterOrEqual(SurfaceRules.HorizontalDistance(site.Position, relic.Position),
                    _fixture.CassetteTuning.Clearance, "clear of every relic");
            }

            Friend tilly = _fixture.Gameplay.Friends.Find("tilly");
            Assert.GreaterOrEqual(SurfaceRules.HorizontalDistance(site.Position, tilly.Site.Position),
                _fixture.CassetteTuning.Clearance, "clear of Tilly");
            Assert.IsTrue(field.IsWaiting(basin));

            Vector3 approach = site.Position + site.Facing * 8f;
            _fixture.Rover.Place(new Vector3(approach.x, 0f, approach.z), Yaw(approach, site.Position));
            yield return null;
            _fixture.Rover.Aim(site.Position + site.Facing * 2.5f + Vector3.up * 1.2f, site.Position);
            _fixture.Capture("20-cassette-waiting");
            Assert.AreEqual(0, _fixture.Events.CassetteCollected.Count, "8 m away it waits");

            _fixture.Rover.Place(new Vector3(site.Position.x, 0f, site.Position.z) + site.Facing * 2f,
                Yaw(approach, site.Position));
            yield return new WaitForSeconds(_fixture.CassetteTuning.FlightDuration + 1f);
            Assert.AreEqual(1, _fixture.Events.CassetteCollected.Count, "driving through pops it out into 07");
            CassetteCollected collected = _fixture.Events.CassetteCollected[0].Value;
            Assert.AreEqual(DustAndHoney, collected.CassetteId);
            Assert.AreEqual(1, collected.Collected);
            Assert.AreEqual(3, collected.Total);
            Assert.AreEqual(1, radio.OwnedTapeCount);
            Assert.AreEqual(DustAndHoney, radio.GetOwnedTape(0));
            Assert.AreEqual(2, _fixture.Events.RadioProgramChanged.Count, "the program changed");
            Assert.AreEqual(RadioChannel.LumenAfterDark, radio.Channel, "no dial before Bell");
            Assert.IsFalse(field.IsWaiting(basin));
            _fixture.Dispose(true);
            yield return null;

            _fixture = GameplayFixture.Boot(_controls, slot);
            yield return null;
            radio = _fixture.Bootstrap.Context.Get<IRadioProgram>();
            Assert.AreEqual(1, radio.OwnedTapeCount, "the tape was saved when it arrived");
            Assert.AreEqual(DustAndHoney, radio.GetOwnedTape(0));
            Assert.IsFalse(_fixture.Gameplay.Cassettes.IsWaiting(basin), "a collected tape never comes back");
            Assert.AreEqual(0, _fixture.Events.CassetteCollected.Count, "a load is not a pickup");
            Assert.AreEqual(1, _fixture.Events.RadioProgramChanged.Count, "announced once after the load");
        }

        private static int IndexOf(CassetteField field, string id)
        {
            for (int i = 0; i < field.Count; i++)
            {
                if (field.Definition(i).Id == id)
                {
                    return i;
                }
            }

            Assert.Fail($"No cassette '{id}' in the field.");
            return -1;
        }

        private static float Yaw(Vector3 from, Vector3 to)
        {
            Vector3 direction = to - from;
            return Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        }
    }
}
