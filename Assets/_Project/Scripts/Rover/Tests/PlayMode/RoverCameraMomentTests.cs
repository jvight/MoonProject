using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core.Events;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>Camera moments (real wiring): eased in and out, framing the subject, then back to normal.</summary>
    public sealed class RoverCameraMomentTests : InputTestFixture
    {
        /// <summary>Height (m) of the middle of 07's body above its ground contact.</summary>
        private const float BodyHeight = 0.8f;

        private LunarTestPhysics _physics;
        private TestWorld _world;
        private TestRover _rover;

        public override void Setup()
        {
            base.Setup();
            _physics = new LunarTestPhysics();
            _world = new TestWorld();
            _rover = TestRover.Spawn(_world, TestWorld.Point(0f, -150f), 0f);
        }

        public override void TearDown()
        {
            _rover.Dispose();
            _world.Dispose();
            _physics.Dispose();
            base.TearDown();
        }

        private static IEnumerator Wait(float seconds)
        {
            float until = Time.time + seconds;
            while (Time.time < until)
            {
                yield return null;
            }
        }

        private float AngleToCameraCentre(Vector3 point)
        {
            Transform camera = _rover.Camera.transform;
            return Vector3.Angle(camera.forward, point - camera.position);
        }

        [UnityTest]
        public IEnumerator RelicSurfacing_FramesTheRelic_ThenReturnsSmoothly()
        {
            yield return Wait(2f);
            RoverController controller = _rover.Controller;
            Vector3 relic = controller.Position + controller.Rotation * new Vector3(5f, 1.5f, 1f);
            float before = AngleToCameraCentre(relic);
            Vector3 restingCamera = _rover.Camera.transform.position;

            _rover.Context.Events.Publish(new RelicSurfaced(relic, "test"));
            float closest = before;
            float largestJump = 0f;
            Vector3 previous = _rover.Camera.transform.position;
            float until = Time.time + 2f;
            while (Time.time < until)
            {
                yield return null;
                Vector3 position = _rover.Camera.transform.position;
                largestJump = Mathf.Max(largestJump, Vector3.Distance(previous, position));
                previous = position;
                closest = Mathf.Min(closest, AngleToCameraCentre(relic));
                Vector3 onScreen = _rover.Camera.WorldToViewportPoint(controller.Position);
                Assert.That(onScreen.x, Is.InRange(0f, 1f), "07 stays in frame.");
            }

            Debug.Log($"[rover-moment] relic {before:0.0} deg off centre before, {closest:0.0} deg at the moment");
            Assert.Less(closest, before - 10f, "The camera turns to frame the relic.");
            Assert.Less(largestJump, 0.5f, "Eased: no cut.");

            yield return Wait(4f);
            Assert.Less(Vector3.Distance(restingCamera, _rover.Camera.transform.position), 0.5f,
                "Back to the normal chase framing.");
        }

        [UnityTest]
        public IEnumerator UpgradePurchase_LiftsAndPullsBack_ThenReturns()
        {
            yield return Wait(2f);
            float restingHeight = _rover.Camera.transform.position.y;
            float restingDistance = Vector3.Distance(_rover.Camera.transform.position, _rover.Controller.Position);

            _rover.Context.Events.Publish(new UpgradePurchased("radio_tower", 2));
            float highest = restingHeight;
            float farthest = restingDistance;
            float until = Time.time + 3.5f;
            while (Time.time < until)
            {
                yield return null;
                highest = Mathf.Max(highest, _rover.Camera.transform.position.y);
                farthest = Mathf.Max(farthest,
                    Vector3.Distance(_rover.Camera.transform.position, _rover.Controller.Position));
            }

            Debug.Log($"[rover-moment] upgrade lift {highest - restingHeight:0.00} m, "
                + $"pull back {farthest - restingDistance:0.00} m");
            Assert.Greater(highest - restingHeight, 1f, "A gentle lift.");
            Assert.Greater(farthest - restingDistance, 1f, "A gentle pull-back.");

            yield return Wait(4f);
            Assert.AreEqual(restingHeight, _rover.Camera.transform.position.y, 0.3f, "Returns to the chase framing.");
        }

        /// <summary>Angle (deg) between 07 and a point as the camera sees them: near 0 means 07 hides it.</summary>
        private float SeparationFrom07(Vector3 point)
        {
            Transform camera = _rover.Camera.transform;
            Vector3 body = _rover.Controller.Position + Vector3.up * BodyHeight;
            return Vector3.Angle(body - camera.position, point - camera.position);
        }

        [UnityTest]
        public IEnumerator Digging_ShowsTheSiteBeside07_HoldsWhileTheBeamIsOn_AndEasesBackWhenStopped()
        {
            yield return Wait(2f);
            RoverController controller = _rover.Controller;
            Vector3 site = controller.Position + controller.Rotation * new Vector3(0f, 0f, 3f);
            Vector3 rising = site + Vector3.up * 0.8f;
            float hidden = SeparationFrom07(rising);
            Vector3 resting = _rover.Camera.transform.position;

            _rover.Context.Events.Publish(new ExcavationStarted(site));
            yield return Wait(6f);
            float shown = SeparationFrom07(rising);
            Debug.Log($"[rover-moment] dig: site {hidden:0.0} deg from 07's body before, {shown:0.0} deg digging");
            Assert.Greater(shown, hidden + 8f, "The rising relic is seen beside 07, not behind it.");
            Assert.Less(_rover.Camera.WorldToViewportPoint(rising).y, 0.95f, "The relic stays in frame.");

            _rover.Context.Events.Publish(new ExcavationStopped(site, false));
            yield return Wait(3f);
            Assert.Less(Vector3.Distance(resting, _rover.Camera.transform.position), 0.5f, "Eased back afterwards.");
        }

        [UnityTest]
        public IEnumerator CompletingTheDig_FlowsStraightIntoTheRelicMoment()
        {
            yield return Wait(2f);
            RoverController controller = _rover.Controller;
            Vector3 site = controller.Position + controller.Rotation * new Vector3(1f, 0f, 3f);
            Vector3 resting = _rover.Camera.transform.position;
            _rover.Context.Events.Publish(new ExcavationStarted(site));
            yield return Wait(4f);

            _rover.Context.Events.Publish(new ExcavationStopped(site, true));
            _rover.Context.Events.Publish(new RelicSurfaced(site + Vector3.up * 1.6f, "test"));
            Vector3 previous = _rover.Camera.transform.position;
            float largestJump = 0f;
            float until = Time.time + 1.5f;
            while (Time.time < until)
            {
                yield return null;
                Vector3 position = _rover.Camera.transform.position;
                largestJump = Mathf.Max(largestJump, Vector3.Distance(previous, position));
                previous = position;
            }

            Assert.Less(largestJump, 0.3f, "The hand-over from dig to relic framing never jumps.");
            yield return Wait(4f);
            Assert.Less(Vector3.Distance(resting, _rover.Camera.transform.position), 0.5f, "Back to the chase view.");
        }
    }
}
