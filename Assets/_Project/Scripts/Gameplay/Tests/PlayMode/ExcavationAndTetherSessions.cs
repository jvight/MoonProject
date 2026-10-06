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
    /// <summary>Scripted sessions for the tractor beam and the tether on the real components behind fakes.</summary>
    public sealed class ExcavationAndTetherSessions : InputTestFixture
    {
        private const string Duck = "rubber_duck";

        private readonly List<Object> _props = new List<Object>();
        private InputActionAsset _controls;
        private GameplayFixture _fixture;
        private Keyboard _keyboard;
        private Mouse _mouse;

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _mouse = InputSystem.AddDevice<Mouse>();
            _controls = BootstrapHarness.LoadControlsCopy();
        }

        public override void TearDown()
        {
            _fixture?.Dispose();
            foreach (Object prop in _props)
            {
                Object.Destroy(prop);
            }

            _props.Clear();
            Object.Destroy(_controls);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator Excavation_KeepsProgressWhenReleased_ThenSurfacesTheRelic()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            Relic duck = _fixture.FindRelic(Duck);
            ExcavationSystem excavation = _fixture.Gameplay.Excavation;
            Vector3 site = duck.Site.Position;
            _fixture.Rover.Place(site + new Vector3(0f, 0f, -4f), 0f);
            yield return null;
            Assert.AreSame(duck, excavation.Candidate, "within reach of the site");

            Press(_keyboard.eKey);
            yield return null;
            yield return null;
            Assert.AreEqual(1, _fixture.Events.ExcavationStarted.Count);
            Assert.AreEqual(site, _fixture.Events.ExcavationStarted[0].Value.Position);
            Assert.AreEqual(1, _fixture.Rover.HoldStillCount, "holding the beam asks 07 to hold still");
            Assert.IsTrue(_fixture.Rover.TryGetGaze(excavation, out _, out int priority));
            Assert.AreEqual(GazePriorities.Focus, priority);

            yield return new WaitForSeconds(2f);
            Assert.Greater(excavation.BeamLevel, 0.8f * _fixture.ExcavationTuning.BeamIntensity);
            Assert.Greater(excavation.Dust.ParticleCount, 5, "dust swirls over the site");
            _fixture.Rover.Aim(site + new Vector3(6f, 3f, -6f), site + Vector3.up);
            _fixture.Capture("04-excavation-beam");

            Release(_keyboard.eKey);
            yield return null;
            yield return null;
            Assert.AreEqual(1, _fixture.Events.ExcavationStopped.Count);
            Assert.IsFalse(_fixture.Events.ExcavationStopped[0].Value.Completed);
            Assert.AreEqual(0, _fixture.Rover.HoldStillCount, "07 is free again");
            Assert.AreEqual(RelicState.Surfacing, duck.State);
            float kept = duck.Progress;
            Assert.That(kept, Is.InRange(0.3f, 0.55f));

            yield return new WaitForSeconds(1f);
            Assert.AreEqual(kept, duck.Progress, "progress is kept, nothing resets");
            Assert.Greater(duck.transform.position.y, duck.BuriedPosition.y + 0.3f, "it waits where it was");

            Press(_keyboard.eKey);
            float deadline = Time.time + 6f;
            while (_fixture.Events.RelicSurfaced.Count == 0 && Time.time < deadline)
            {
                yield return null;
            }

            Release(_keyboard.eKey);
            Assert.AreEqual(1, _fixture.Events.RelicSurfaced.Count, "the relic surfaces");
            float secondHold = _fixture.Events.RelicSurfaced[0].Time - _fixture.Events.ExcavationStarted[1].Time;
            float full = _fixture.ExcavationTuning.DurationFor(duck.Definition.Mass);
            Assert.AreEqual(full * (1f - kept), secondHold, 0.25f, "the second hold only finishes the rest");
            CollectionAssert.AreEqual(new[]
            {
                nameof(ExcavationStarted), nameof(ExcavationStopped), nameof(ExcavationStarted),
                nameof(ExcavationStopped), nameof(RelicSurfaced),
            }, _fixture.Events.Order);
            Assert.IsTrue(_fixture.Events.ExcavationStopped[1].Value.Completed);
            Assert.AreEqual(Duck, _fixture.Events.RelicSurfaced[0].Value.RelicId);
            Assert.AreEqual(RelicState.Loose, duck.State);
            Assert.IsFalse(duck.Body.isKinematic);
            Assert.IsTrue(duck.Collider.enabled);
            Assert.AreEqual(Layers.Relic, duck.gameObject.layer);
            Assert.AreEqual(0, _fixture.Rover.HoldStillCount);

            yield return new WaitForSeconds(1.5f);
            Assert.Greater(Vector3.Distance(duck.transform.position, _fixture.Rover.Position), 2.5f,
                "it pops out ahead of 07, clear of the rover");
            _fixture.Capture("05-relic-surfaced");
        }

        [UnityTest]
        public IEnumerator Tether_HighlightsBeforePress_AndTowsBehind07()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            Relic duck = LooseRelicAt(new Vector3(1f, 0.6f, 9f));
            TetherSystem tether = _fixture.Gameplay.Tether;
            yield return new WaitForSeconds(1f);

            Vector3 camera = _fixture.Rover.Camera.transform.position;
            Vector3 off = Quaternion.Euler(0f, 5f, 0f) * (duck.transform.position - camera);
            _fixture.Rover.Aim(camera, camera + off);
            yield return null;
            yield return null;
            Assert.AreEqual(TetherAimState.Hovering, tether.State, "5 degrees off still picks it");
            Assert.AreSame(duck, tether.Hovered);
            yield return new WaitForSeconds(0.3f);
            Assert.Greater(duck.HaloLevel, 0.5f, "a visible highlight before pressing");
            _fixture.Capture("06-tether-hover");

            Press(_mouse.rightButton, queueEventOnly: true);
            yield return null;
            yield return null;
            Assert.AreEqual(1, _fixture.Events.TetherAttached.Count);
            Assert.AreEqual(duck.Definition.Mass, _fixture.Events.TetherAttached[0].Value.Mass, 1e-4f);
            Assert.AreEqual(TetherAimState.Towing, tether.State);
            Assert.IsTrue(_fixture.Rover.TryGetGaze(tether, out _, out int priority));
            Assert.AreEqual(GazePriorities.Focus, priority);
            float length = tether.Length;

            float speed = 5f;
            Vector3 start = _fixture.Rover.Position;
            float began = Time.time;
            while (Time.time - began < 4f)
            {
                _fixture.Rover.MoveTo(start + Vector3.back * (speed * (Time.time - began)), 180f);
                yield return null;
            }

            Vector3 eye = _fixture.Rover.TetherOrigin.position;
            float distance = Vector3.Distance(duck.transform.position, eye);
            Assert.That(distance, Is.InRange(length - 2f, length + 3f), "towed at about the tether length");
            Assert.Greater(duck.transform.position.z, eye.z, "it trails behind 07");
            Assert.That(duck.transform.position.y, Is.InRange(0.2f, 3f), "floating just above the dust");
            Assert.AreEqual(0, _fixture.Events.TetherReleased.Count);
            Vector3 rover = _fixture.Rover.Position;
            _fixture.Rover.Aim(rover + new Vector3(9f, 4f, 3f), Vector3.Lerp(rover, duck.transform.position, 0.5f));
            _fixture.Capture("07-tether-tow");

            Release(_mouse.rightButton);
            yield return null;
            Assert.AreEqual(1, _fixture.Events.TetherReleased.Count);
            Assert.IsFalse(_fixture.Events.TetherReleased[0].Value.Snapped);
            Assert.AreNotEqual(TetherAimState.Towing, tether.State);
            Assert.IsFalse(duck.IsTethered);
        }

        [UnityTest]
        public IEnumerator Tether_CaughtBehindAWall_LetsGoSoftly_AndTheRelicStaysPut()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            Relic duck = LooseRelicAt(new Vector3(0f, 0.6f, 7f));
            yield return new WaitForSeconds(1f);
            Vector3 camera = _fixture.Rover.Camera.transform.position;
            _fixture.Rover.Aim(camera, duck.transform.position);
            yield return null;
            Press(_mouse.rightButton, queueEventOnly: true);
            yield return null;
            yield return null;
            Assert.AreEqual(1, _fixture.Events.TetherAttached.Count);
            Wall(new Vector3(0f, 1.5f, 4.5f), new Vector3(20f, 3f, 0.6f));

            Vector3 start = _fixture.Rover.Position;
            float began = Time.time;
            while (_fixture.Events.TetherReleased.Count == 0 && Time.time - began < 10f)
            {
                _fixture.Rover.MoveTo(start + Vector3.back * Mathf.Min(30f, 4f * (Time.time - began)), 180f);
                yield return null;
            }

            Assert.AreEqual(1, _fixture.Events.TetherReleased.Count, "the tether lets go by itself");
            Assert.IsTrue(_fixture.Events.TetherReleased[0].Value.Snapped);
            yield return new WaitForSeconds(0.5f);
            Assert.Less(duck.Body.linearVelocity.magnitude, 1f, "no fling: it settles where it was");
            Assert.Greater(duck.transform.position.z, 4f, "still behind the wall that caught it");
            Assert.AreEqual(RelicState.Loose, duck.State);

            yield return null;
            Assert.AreNotEqual(TetherAimState.Towing, _fixture.Gameplay.Tether.State);
            Assert.AreEqual(1, _fixture.Events.TetherAttached.Count, "no re-grab until the button is pressed again");
            Release(_mouse.rightButton);
        }

        private Relic LooseRelicAt(Vector3 position)
        {
            Relic duck = _fixture.FindRelic(Duck);
            duck.BeginLift();
            duck.SetLiftPose(position, Quaternion.identity, 1f);
            duck.Surface(Vector3.zero, Vector3.zero);
            return duck;
        }

        private void Wall(Vector3 centre, Vector3 size)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "Wall";
            wall.layer = Layers.Prop;
            wall.transform.position = centre;
            wall.transform.localScale = size;
            _props.Add(wall);
        }
    }
}
