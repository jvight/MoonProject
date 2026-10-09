using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// Kenji's Rover Bay fits 07's kit (M3-14, VISION ruling 14: 07 has no hands), through the real wiring on a
    /// stand-in bay built to the art contract (<see cref="TestRoverBay"/>): bought kit waits for the bay; when the bay
    /// fits it, 07 is centred on the turntable, an arm carries the piece from under the roof down onto its socket (the
    /// piece rides the arm's tip the whole way and is set exactly on its socket, never dropped), the arm folds away,
    /// the turntable turns 07 to show it and 07 strikes its proud pose once it is let go. The drums take two arms,
    /// the Hover-Jump coils ride the floor arm up through the turntable. A socket out of reach is a contract bug,
    /// logged loudly. The camera holds a view in through the bay's open front, under the crane rail, while 07 turns,
    /// and eases in close to the socket while a part is set on (low under 07 for the floor arm) and back out after.
    /// </summary>
    public sealed class RoverBayTests : InputTestFixture
    {
        private const float ParkedYaw = 0f;

        /// <summary>A carried part sits on its arm's tip, a fitted one on its socket, within this (m).</summary>
        private const float GripTolerance = 2e-3f;

        /// <summary>Held by the bay, 07 and the arms move eased, never faster than these (m/s, deg/s).</summary>
        private const float MaxBodySpeed = 3f;
        private const float MaxBodyTurn = 260f;
        private const float MaxTipSpeed = 6f;

        /// <summary>Viewport margin a socket and the tool setting a part on it must sit inside.</summary>
        private const float FrameMargin = 0.05f;

        /// <summary>The camera in the bay never whips (m/s, deg/s).</summary>
        private const float MaxCameraSpeed = 12f;
        private const float MaxCameraTurn = 120f;

        /// <summary>The whole fitting is over by this (s); it takes at least a few eased seconds.</summary>
        private const float LongestFitting = 7f;
        private const float ShortestFitting = 2.5f;

        private const string WarmHeadlamp = "rover.warm_headlamp";
        private const string BoostCoils = "rover.boost_coils";
        private const string HoverJump = "rover.hover_jump";

        private readonly List<RoverKitInstalling> _installing = new List<RoverKitInstalling>();
        private readonly List<RoverKitFitted> _fitted = new List<RoverKitFitted>();
        private LunarTestPhysics _physics;
        private TestWorld _world;
        private TestRover _rover;
        private TestRoverBay _bay;

        private static Vector3 Centre => TestWorld.Point(0f, -150f);

        public override void Setup()
        {
            base.Setup();
            _physics = new LunarTestPhysics();
            _world = new TestWorld();
            _installing.Clear();
            _fitted.Clear();
        }

        public override void TearDown()
        {
            _rover?.Dispose();
            _rover = null;
            _bay?.Dispose();
            _bay = null;
            _world.Dispose();
            _physics.Dispose();
            base.TearDown();
        }

        /// <summary>07 drove into the bay and stopped a little off-centre and askew, as a player parks.</summary>
        private IEnumerator Park(float upperLength = TestRoverBay.UpperLength,
            float lowerLength = TestRoverBay.LowerLength)
        {
            _bay = TestRoverBay.Build(Centre, ParkedYaw, _world.Material, upperLength, lowerLength);
            _rover = TestRover.Spawn(_world, Centre + new Vector3(0.25f, 0f, 0.3f), ParkedYaw + 8f);
            _rover.Context.Register<IRoverBay>(_bay);
            EventBus events = _rover.Context.Events;
            events.Subscribe<RoverKitInstalling>(_installing.Add);
            events.Subscribe<RoverKitFitted>(_fitted.Add);
            yield return Wait(1f);
        }

        private static IEnumerator Wait(float seconds)
        {
            float until = Time.time + seconds;
            while (Time.time < until)
            {
                yield return null;
            }
        }

        /// <summary>A purchase at the bay: the ability is granted just before the purchase is announced.</summary>
        private void Buy(RoverAbility ability, string upgradeId)
        {
            _rover.Context.Get<IRoverAbilities>().Grant(ability);
            _rover.Context.Events.Publish(new UpgradePurchased(upgradeId, 1));
        }

        /// <summary>The hopper is fed: the bay starts fitting.</summary>
        private void Fit(string upgradeId)
        {
            _rover.Context.Events.Publish(new RoverBayFitting(upgradeId));
        }

        private float NearestTip(Vector3 point)
        {
            float nearest = float.MaxValue;
            for (int i = 0; i < _bay.ArmCount; i++)
            {
                nearest = Mathf.Min(nearest, Vector3.Distance(_bay.TipEnd(i), point));
            }

            return nearest;
        }

        private int NearestArm(Vector3 point)
        {
            int nearest = 0;
            for (int i = 1; i < _bay.ArmCount; i++)
            {
                if (Vector3.Distance(_bay.TipEnd(i), point) < Vector3.Distance(_bay.TipEnd(nearest), point))
                {
                    nearest = i;
                }
            }

            return nearest;
        }

        /// <summary>Each arm folded back in its rest pose along the rail.</summary>
        private void AssertArmsAtRest()
        {
            for (int i = 0; i < _bay.ArmCount; i++)
            {
                Assert.Less(_bay.OffRest(i), 0.5f, $"arm {i} folded back to rest");
            }
        }

        /// <summary>
        /// As a part is set on <paramref name="socket"/> by <paramref name="tool"/> (an arm's tip or the floor arm's),
        /// the game camera is in its close shot: a few metres off, inside the bay's walls, under the crane rail, with
        /// the socket and the tool in frame. Returns the camera's height above 07's pivot.
        /// </summary>
        private float AssertSetOnFramed(Vector3 socket, Vector3 tool, string piece)
        {
            Camera view = _rover.Camera;
            Vector3 eye = view.transform.position;
            BayShotSettings shot = _rover.CameraTuning.BayFraming.Shot;
            float distance = Vector3.Distance(eye, socket);
            Vector3 onScreen = view.WorldToViewportPoint(socket);
            Vector3 toolOnScreen = view.WorldToViewportPoint(tool);
            Debug.Log($"[rover-bay] {piece} set on: camera {distance:0.00} m off, {eye.y - Centre.y:0.00} m up, "
                + $"socket at ({onScreen.x:0.00}, {onScreen.y:0.00}), tool at ({toolOnScreen.x:0.00}, "
                + $"{toolOnScreen.y:0.00}), shot weight {_rover.CameraRig.FittingShotWeight:0.00}");
            Assert.Greater(_rover.CameraRig.FittingShotWeight, 0.95f, $"{piece}: the close shot is in.");
            Assert.That(distance, Is.InRange(shot.MinDistance - 0.5f, shot.ArmDistance + 1f),
                $"{piece}: a few metres off the socket.");
            Assert.IsTrue(_bay.WithinWalls(eye), $"{piece}: inside the bay's walls.");
            Assert.Less(eye.y - Centre.y, TestRoverBay.RailHeight, $"{piece}: under the crane rail.");
            Assert.IsTrue(InFrame(onScreen), $"{piece}: the socket is in frame.");
            Assert.IsTrue(InFrame(toolOnScreen), $"{piece}: the arm setting it on is in frame.");
            return eye.y - Centre.y;
        }

        private static bool InFrame(Vector3 viewport)
        {
            return viewport.z > 0f && viewport.x > FrameMargin && viewport.x < 1f - FrameMargin
                && viewport.y > FrameMargin && viewport.y < 1f - FrameMargin;
        }

        [UnityTest]
        public IEnumerator FittingTheLampBar_AnArmCarriesItOntoItsSocket_TheTurntableShowsIt_07IsProud()
        {
            yield return Park();
            float plainAngle = _rover.Headlamp.spotAngle;
            Transform bar = _rover.LampBar;
            Vector3 rest = bar.localPosition;
            Buy(RoverAbility.WarmHeadlamp, WarmHeadlamp);
            yield return Wait(0.5f);
            Assert.IsFalse(bar.gameObject.activeSelf, "Bought, the bar waits for the bay (07 feeds the hopper first).");
            Assert.IsEmpty(_installing);

            Fit(WarmHeadlamp);
            yield return null;
            Assert.AreEqual(1, _installing.Count, "The install moment begins as the bay starts fitting.");
            Assert.AreEqual(RoverKitPiece.LampBar, _installing[0].Piece);
            Assert.IsFalse(_installing[0].Gift);
            Assert.IsTrue(_rover.Kit.IsFitting);
            Assert.IsTrue(_rover.Controller.IsCradled, "The bay holds 07.");
            Assert.IsTrue(bar.gameObject.activeSelf, "The bar hangs from an arm...");
            Assert.Greater(bar.position.y - Centre.y, 2f, "...taken from the rack under the roof.");

            var body = new CameraTrace(_rover.Rig.transform);
            var camera = new CameraTrace(_rover.Camera.transform);
            var tips = new CameraTrace[_bay.ArmCount];
            for (int i = 0; i < tips.Length; i++)
            {
                tips[i] = new CameraTrace(_bay.GetArmJoint(i, RoverBayJoint.SparkSocket));
            }

            Vector3 socket = bar.parent.TransformPoint(rest);
            float started = Time.time;
            float landedAt = -1f;
            float finishedAt = -1f;
            float grip = 0f;
            float above = 0f;
            float landingGap = -1f;
            float seat = 0f;
            float proudHeld = 0f;
            float proud = 0f;
            float bearing = 0f;
            float cameraHigh = float.MinValue;
            bool watched = false;
            float watchAfter = 1.2f;
            while (Time.time - started < LongestFitting
                && (finishedAt < 0f || Time.time - started - finishedAt < watchAfter))
            {
                yield return null;
                float now = Time.time - started;
                body.Step(0f);
                camera.Step(_rover.CameraRig.MomentWeight);
                for (int i = 0; i < tips.Length; i++)
                {
                    tips[i].Step(0f);
                }

                if (_rover.Kit.Fit(RoverKitPiece.LampBar).IsCarried)
                {
                    socket = bar.parent.TransformPoint(rest);
                    grip = Mathf.Max(grip, NearestTip(bar.position));
                    above = Mathf.Max(above, Vector3.Dot(bar.position - socket, Vector3.up));
                    watched |= _rover.Controller.Gaze.TryGetTop(out Vector3 gaze)
                        && Vector3.Distance(gaze, bar.position) < 0.05f;
                }

                if (landedAt < 0f && _fitted.Count > 0)
                {
                    landedAt = now;
                    landingGap = NearestTip(bar.position);
                    AssertSetOnFramed(bar.position, _bay.TipEnd(NearestArm(bar.position)), "lamp bar");
                }

                if (landedAt >= 0f)
                {
                    seat = Mathf.Max(seat, Vector3.Distance(bar.localPosition, rest));
                }

                if (finishedAt < 0f && !_rover.Kit.IsFitting)
                {
                    finishedAt = now;
                }

                float perk = _rover.BodyLanguage.Mood.Perk;
                if (finishedAt < 0f)
                {
                    proudHeld = Mathf.Max(proudHeld, perk);
                }
                else
                {
                    proud = Mathf.Max(proud, perk);
                }

                if (_rover.CameraRig.MomentWeight > 0.95f && _rover.CameraRig.FittingShotWeight < 0.05f)
                {
                    Vector3 fromBay = Vector3.ProjectOnPlane(_rover.Camera.transform.position - Centre, Vector3.up);
                    bearing = Mathf.Max(bearing, Vector3.Angle(fromBay, _bay.OpenFront));
                    cameraHigh = Mathf.Max(cameraHigh, _rover.Camera.transform.position.y - Centre.y);
                }
            }

            Debug.Log($"[rover-bay] lamp bar: landed {landedAt:0.00} s, finished {finishedAt:0.00} s; "
                + $"grip {grip:0.0000} m, came down from {above:0.00} m above, landing gap {landingGap:0.0000} m, "
                + $"seat {seat:0.0000} m; "
                + $"07 {body}; camera {camera}, bearing off the front {bearing:0.0} deg, top {cameraHigh:0.00} m; "
                + $"perk held {proudHeld:0.00} then {proud:0.00}");
            Assert.AreEqual(1, _fitted.Count, "Fitted once.");
            Assert.AreEqual(RoverKitPiece.LampBar, _fitted[0].Piece);
            Assert.AreEqual(WarmHeadlamp, _fitted[0].UpgradeId, "The kit's identity rides with it.");
            Assert.That(finishedAt, Is.InRange(ShortestFitting, LongestFitting), "A few eased seconds.");
            Assert.Less(grip, GripTolerance, "The bar rides the arm's tip all the way.");
            Assert.Greater(above, 0.3f, "It comes down onto its socket from above.");
            Assert.Less(landingGap, GripTolerance, "Set on its socket by the tip: no drop the last stretch.");
            Assert.Less(seat, 1e-4f, "And it stays exactly on its socket.");
            Assert.IsTrue(watched, "07 watches the bar come in.");
            Assert.Less(body.Fastest, MaxBodySpeed, "07 is centred and turned eased.");
            Assert.Less(body.FastestTurn, MaxBodyTurn);
            for (int i = 0; i < tips.Length; i++)
            {
                Assert.Less(tips[i].Fastest, MaxTipSpeed, $"Arm {i} moves eased.");
            }

            Assert.IsFalse(_rover.Controller.IsCradled, "07 is let go.");
            Assert.AreEqual(180f, Quaternion.Angle(_bay.Turntable.rotation, _bay.TurntableRotation), 1f,
                "The turntable turned 07 to show the bar to the open front.");
            Assert.AreEqual(0f, Mathf.DeltaAngle(_rover.Controller.Heading, ParkedYaw + 180f), 1f);
            AssertArmsAtRest();
            Assert.Greater(proud, 0.6f, "07's proud pose...");
            Assert.Less(proudHeld, proud - 0.2f, "...once the bay lets it go, showing the bar.");
            Assert.Greater(_rover.Headlamp.spotAngle, plainAngle + 20f, "A wider, warmer road light.");
            Assert.Less(bearing, 20f, "Out of the close shot, the camera looks in through the open front...");
            Assert.Less(_rover.CameraRig.FittingShotWeight, 0.05f, "...back in the bay's view once 07 is let go...");
            Assert.Less(cameraHigh, TestRoverBay.RailHeight, "...from under the crane rail...");
            Assert.Greater(cameraHigh, 1.8f, "...and above 07's back.");
            Assert.Less(camera.Fastest, MaxCameraSpeed, "Eased, never a cut.");
            Assert.Less(camera.FastestTurn, MaxCameraTurn, "Eased, never a whip.");
        }

        [UnityTest]
        public IEnumerator FittingTheDrums_TwoArmsSetOneDrumEach()
        {
            yield return Park();
            Buy(RoverAbility.BoostCoils, BoostCoils);
            yield return null;
            Fit(BoostCoils);
            yield return null;
            Transform[] drums = _rover.Drums;
            Assert.AreNotEqual(NearestArm(drums[0].position), NearestArm(drums[1].position), "One arm per drum.");

            float grip = 0f;
            float started = Time.time;
            bool framed = false;
            while (_rover.Kit.IsFitting && Time.time - started < LongestFitting)
            {
                if (_rover.Kit.Fit(RoverKitPiece.CapacitorDrums).IsCarried)
                {
                    grip = Mathf.Max(grip, Mathf.Max(NearestTip(drums[0].position), NearestTip(drums[1].position)));
                }

                if (!framed && _fitted.Count > 0)
                {
                    framed = true;
                    AssertSetOnFramed(drums[0].position, _bay.TipEnd(NearestArm(drums[0].position)), "drum");
                }

                yield return null;
            }

            Debug.Log($"[rover-bay] drums: fitted in {Time.time - started:0.00} s, grip {grip:0.0000} m");
            Assert.IsFalse(_rover.Kit.IsFitting, "Done in time.");
            Assert.Less(grip, GripTolerance, "Each drum rides its arm's tip.");
            Assert.AreEqual(1, _fitted.Count);
            Assert.AreEqual(BoostCoils, _fitted[0].UpgradeId);
            Assert.AreEqual(Vector3.zero, drums[0].localPosition, "On its socket.");
            Assert.AreEqual(Vector3.zero, drums[1].localPosition, "On its socket.");
            AssertArmsAtRest();
        }

        [UnityTest]
        public IEnumerator FittingTheHoverCoils_TheFloorArmCarriesThemUpToTheBelly()
        {
            yield return Park();
            Buy(RoverAbility.HoverJump, HoverJump);
            yield return null;
            Assert.IsFalse(_rover.CoilMount.gameObject.activeSelf, "Bought, the coils wait for the bay.");
            Fit(HoverJump);
            yield return null;
            Transform mount = _rover.CoilMount;
            Vector3 liftRest = _bay.FloorLift.localPosition;
            Assert.IsTrue(mount.gameObject.activeSelf);
            Assert.Less(mount.position.y, Centre.y, "They start in the pit under the turntable...");

            float lowest = mount.position.y;
            float aside = 0f;
            float started = Time.time;
            float cameraUp = -1f;
            while (_rover.Kit.IsFitting && Time.time - started < LongestFitting)
            {
                if (cameraUp < 0f && _fitted.Count > 0)
                {
                    cameraUp = AssertSetOnFramed(mount.position, _bay.FloorTip.position, "coils");
                }

                lowest = Mathf.Min(lowest, mount.position.y);
                if (_rover.Kit.Fit(RoverKitPiece.HoverCoils).IsCarried)
                {
                    Vector3 offset = mount.position - _bay.FloorTip.position;
                    aside = Mathf.Max(aside, Vector3.ProjectOnPlane(offset, Vector3.up).magnitude);
                    Assert.Less(Mathf.Abs(offset.y), GripTolerance, "...riding the floor arm's tip...");
                }

                yield return null;
            }

            Debug.Log($"[rover-bay] coils: fitted in {Time.time - started:0.00} s from {lowest - Centre.y:0.00} m, "
                + $"aside the floor tip {aside:0.000} m");
            Assert.IsFalse(_rover.Kit.IsFitting, "Done in time.");
            Assert.AreEqual(1, _fitted.Count);
            Assert.AreEqual(RoverKitPiece.HoverCoils, _fitted[0].Piece);
            Assert.AreEqual(Vector3.zero, mount.localPosition, "...up onto the CoilSocket.");
            Assert.AreEqual(1f, mount.localScale.x, 1e-3f, "Full size.");
            Assert.AreEqual(liftRest, _bay.FloorLift.localPosition, "The floor arm sinks back into its pit.");
            Assert.Less(aside, 0.25f, "Straight over the floor arm.");
            Assert.Less(cameraUp, 1f, "The camera watched from low down, under 07's belly.");
            AssertArmsAtRest();
        }

        [UnityTest]
        public IEnumerator ASocketOutOfReach_IsAContractBug_LoggedLoudly_NeverDropped()
        {
            yield return Park(TestRoverBay.ShortUpperLength, TestRoverBay.ShortLowerLength);
            Buy(RoverAbility.WarmHeadlamp, WarmHeadlamp);
            yield return null;
            LogAssert.Expect(LogType.Error, new Regex("cannot reach the LampBar socket"));
            Fit(WarmHeadlamp);
            yield return null;
            yield return null;
            Assert.IsFalse(_rover.Kit.IsFitting, "No moment with arms that cannot reach.");
            Assert.IsFalse(_rover.Controller.IsCradled);
            Assert.IsTrue(_rover.LampBar.gameObject.activeSelf, "The bar is simply on 07...");
            Assert.AreEqual(Vector3.zero, _rover.LampBar.localPosition, "...on its socket, never dropped onto it.");
            Assert.AreEqual(1, _fitted.Count);
            AssertArmsAtRest();
        }

        [UnityTest]
        public IEnumerator WithoutABay_TheKitIsAWiringBug_LoggedLoudly()
        {
            _rover = TestRover.Spawn(_world, Centre, ParkedYaw);
            _rover.Context.Events.Subscribe<RoverKitFitted>(_fitted.Add);
            yield return Wait(0.5f);
            Buy(RoverAbility.CargoCradle, "rover.cargo_cradle");
            yield return null;
            LogAssert.Expect(LogType.Error, new Regex("no IRoverBay is registered"));
            Fit("rover.cargo_cradle");
            yield return null;
            yield return null;
            Assert.IsTrue(_rover.CargoRack.gameObject.activeSelf, "The rack is simply on 07.");
            Assert.AreEqual(1, _fitted.Count);
        }

        [UnityTest]
        public IEnumerator InTheBay_LookingAround_DismissesTheView_UntilTheBayFitsSomething()
        {
            yield return Park();
            yield return Wait(1.5f);
            Assert.Greater(_rover.CameraRig.MomentWeight, 0.95f, "Parked in the bay: the view in through the front.");
            Assert.IsFalse(_rover.CameraRig.WideShot.IsOpen);

            var gamepad = InputSystem.AddDevice<Gamepad>();
            Set(gamepad.rightStick, new Vector2(0.7f, 0f));
            yield return Wait(1f);
            Set(gamepad.rightStick, Vector2.zero);
            yield return Wait(1f);
            Assert.Less(_rover.CameraRig.MomentWeight, 0.05f, "Looking around hands the view to the player...");
            yield return Wait(1f);
            Assert.Less(_rover.CameraRig.MomentWeight, 0.05f, "...and it stays theirs.");

            Buy(RoverAbility.CargoCradle, "rover.cargo_cradle");
            Fit("rover.cargo_cradle");
            yield return Wait(1.6f);
            Assert.Greater(_rover.CameraRig.MomentWeight, 0.9f, "The bay fitting a piece brings the view back.");
        }
    }
}
