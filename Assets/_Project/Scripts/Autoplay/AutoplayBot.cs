using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;
using MoonProject.Core;
using MoonProject.Routing;

namespace MoonProject.Autoplay
{
    /// <summary>
    /// What the bot can do with its hands and eyes, as coroutine steps a scenario strings together: wait for
    /// something on screen, tap or hold a key, steer 07 to a spot with W/A/D, turn the view with the mouse. Every wait
    /// has a timeout; a timed-out wait fails the run with the reason, and every later step then does nothing.
    /// </summary>
    public sealed class AutoplayBot
    {
        private const float KeyTapSeconds = 0.12f;
        private const float StuckWindowSeconds = 2f;
        private const float StuckProgressMetres = 0.4f;
        private const float UnstickReverseSeconds = 1.3f;
        private const float AlignToleranceDegrees = 4f;
        private const float AlignTimeoutSeconds = 12f;
        private const float LookTimeoutSeconds = 8f;
        private const float LookAroundSweepPixels = 6f;
        private const float RouteCellMetres = 2f;
        private const float RouteMarginMetres = 40f;
        private const float RouteClearance = 1.0f;
        private const float RouteRelaxMetres = 4f;

        /// <summary>A leg that needed this many unsticks is abandoned: the route is planned again, round the place it stuck.</summary>
        private const int ReplanAfterUnsticks = 2;

        /// <summary>Plans after the first: each avoids the places 07 stuck and keeps this much more clear of props.</summary>
        private const int MaxReplans = 4;

        private const float RouteGrowMetres = 0.5f;

        /// <summary>Seconds 07 may take to settle before a new route is driven.</summary>
        private const float ReplanSettleSeconds = 2f;

        /// <summary>Metres round a place 07 stuck that the next plan treats as blocked.</summary>
        private const float StuckAvoidMetres = 3f;
        private const float RouteArriveRadius = 2.5f;

        private readonly AutoplayWorld _world;
        private readonly VirtualInput _input;
        private readonly BeatLog _beats;
        private readonly Action<string> _screenshot;
        private readonly ThrottleGate _gate = new ThrottleGate();
        private readonly System.Collections.Generic.List<string> _warnings = new System.Collections.Generic.List<string>();
        private readonly System.Collections.Generic.List<Vector2> _points = new System.Collections.Generic.List<Vector2>();
        private readonly System.Collections.Generic.List<Vector3> _route = new System.Collections.Generic.List<Vector3>();
        private readonly System.Collections.Generic.List<Vector2> _stuckAt = new System.Collections.Generic.List<Vector2>();
        private bool _replan;
        private bool _mayReplan;
        private string _beatId;
        private string _beatTitle;
        private float _beatStart;

        public AutoplayBot(AutoplayWorld world, VirtualInput input, BeatLog beats, Action<string> screenshot)
        {
            _world = world;
            _input = input;
            _beats = beats;
            _screenshot = screenshot;
        }

        public AutoplayWorld World => _world;

        /// <summary>The reason the run failed, or null while it has not.</summary>
        public string Failure { get; private set; }

        public bool Failed => Failure != null;

        /// <summary>One line of what the bot is doing now, for the slow-frame log.</summary>
        public string Activity { get; private set; } = "starting";

        public void Fail(string reason)
        {
            Failure = Failure ?? reason;
        }

        /// <summary>Notes something wrong that did not stop the run: it shows in the beat's reason.</summary>
        public void Warn(string warning)
        {
            _warnings.Add(warning);
        }

        public void BeginBeat(string id, string title)
        {
            _beatId = id;
            _beatTitle = title;
            _beatStart = Time.unscaledTime;
            _warnings.Clear();
            Activity = id + " " + title;
        }

        /// <summary>Closes the current beat as passed, or as failed with the run's failure; takes its screenshot.</summary>
        public void EndBeat(string passNote)
        {
            string objective = _world.Refresh() && _world.Ui != null ? _world.Ui.ObjectiveLine() : string.Empty;
            string chip = _world.Refresh() && _world.Ui != null ? _world.Ui.Chip() : string.Empty;
            bool passed = !Failed;
            string sky = _world.Refresh() ? _world.Context.Get<ISunlight>().Phase.ToString() : string.Empty;
            string note = passed ? passNote : Failure;
            if (_warnings.Count > 0)
            {
                note += " WARNING: " + string.Join(" ", _warnings);
            }

            _beats.Add(new BeatResult(_beatId, _beatTitle, passed, note,
                Time.unscaledTime - _beatStart, objective, chip, sky));
            _screenshot(_beatId + (passed ? "-done" : "-failed"));
        }

        /// <summary>Waits until <paramref name="condition"/> holds; fails the run when it does not within the timeout.</summary>
        public IEnumerator Until(Func<bool> condition, float timeoutSeconds, string what, Func<string> detail = null)
        {
            float deadline = Time.unscaledTime + timeoutSeconds;
            while (!Failed && !condition())
            {
                if (Time.unscaledTime > deadline)
                {
                    Fail($"timed out after {timeoutSeconds.ToString("0", CultureInfo.InvariantCulture)} s waiting for " +
                         $"{what}. {(detail != null ? detail() + " " : string.Empty)}{Where()}");
                    yield break;
                }

                yield return null;
            }
        }

        public IEnumerator Wait(float seconds)
        {
            float until = Time.unscaledTime + seconds;
            while (Time.unscaledTime < until)
            {
                yield return null;
            }
        }

        public IEnumerator Tap(Key key)
        {
            _input.Set(key, true);
            yield return Wait(KeyTapSeconds);
            _input.Set(key, false);
            yield return null;
        }

        /// <summary>Holds <paramref name="key"/> until <paramref name="done"/> holds, then lets go.</summary>
        public IEnumerator Hold(Key key, Func<bool> done, float timeoutSeconds, string what,
            Func<string> detail = null)
        {
            _input.Set(key, true);
            yield return Until(done, timeoutSeconds, what, detail);
            _input.Set(key, false);
            yield return null;
        }

        /// <summary>Sweeps the mouse right and back, the way a player looks around when the eye opens.</summary>
        public IEnumerator LookAround(float seconds)
        {
            Activity = "looking around";
            float half = seconds * 0.5f;
            for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
            {
                _input.Look(new Vector2(t < half ? LookAroundSweepPixels : -LookAroundSweepPixels, 0f));
                yield return null;
            }
        }

        /// <summary>
        /// Drives 07 to within <paramref name="arriveRadius"/> metres of <paramref name="target"/>, round the props
        /// that stand between (a coarse route over their colliders, so it never drives into a lander or a crate).
        /// </summary>
        public IEnumerator DriveTo(Vector3 target, float arriveRadius, string what, float timeoutSeconds)
        {
            _stuckAt.Clear();
            for (int attempt = 0; attempt <= MaxReplans && !Failed; attempt++)
            {
                // The last plan is driven to the end, stuck or not: it times out as a plain drive does.
                _mayReplan = attempt < MaxReplans;
                _replan = false;
                PlanRoute(target, attempt);
                for (int i = 0; i < _route.Count - 1 && !Failed && !_replan; i++)
                {
                    yield return DriveDirect(_route[i], RouteArriveRadius, what + " (round the props)", timeoutSeconds,
                        false);
                }

                if (!Failed && !_replan)
                {
                    yield return DriveDirect(target, arriveRadius, what, timeoutSeconds, true);
                }

                if (Failed || !_replan)
                {
                    yield break;
                }

                Vector3 here = _world.Rover.Position;
                _stuckAt.Add(new Vector2(here.x, here.z));
                Debug.Log($"Autoplay: {Activity}: stuck near {here} on the way to {what}; planning again (plan {attempt + 2}).");
                // 07 may still be rolling on a slope: give it a moment to settle, and go on either way.
                float settleUntil = Time.unscaledTime + ReplanSettleSeconds;
                while (Rover().Speed >= 0.3f && Time.unscaledTime < settleUntil)
                {
                    yield return null;
                }
            }
        }

        /// <summary>The way from 07 to <paramref name="target"/> into <see cref="_route"/>: one point when it is clear.</summary>
        private void PlanRoute(Vector3 target, int attempt)
        {
            _route.Clear();
            var terrain = _world.Context.Get<ITerrainQuery>();
            Vector3 position = _world.Rover.Position;
            float clearance = RouteClearance + attempt * RouteGrowMetres;
            bool Blocked(Vector2 point)
            {
                for (int i = 0; i < _stuckAt.Count; i++)
                {
                    if ((_stuckAt[i] - point).sqrMagnitude < StuckAvoidMetres * StuckAvoidMetres)
                    {
                        return true;
                    }
                }

                float ground = terrain.SampleHeight(point.x, point.y);
                return Physics.CheckSphere(new Vector3(point.x, ground + clearance, point.y), clearance,
                    Layers.PropMask, QueryTriggerInteraction.Ignore);
            }

            bool planned = RoutePlanner.Plan(new Vector2(position.x, position.z), new Vector2(target.x, target.z),
                Blocked, RouteCellMetres, RouteMarginMetres, RouteRelaxMetres, _points);
            if (!planned)
            {
                Warn("no way round the props to " + target + " was found; the bot drives straight.");
            }

            foreach (Vector2 point in _points)
            {
                _route.Add(new Vector3(point.x, target.y, point.y));
            }

            if (_route.Count > 1)
            {
                Debug.Log($"Autoplay: {Activity}: {_route.Count} waypoints round the props from {position} to {target}.");
            }
        }

        private IEnumerator DriveDirect(Vector3 target, float arriveRadius, string what, float timeoutSeconds,
            bool settle)
        {
            Activity = "driving to " + what;
            float deadline = Time.unscaledTime + timeoutSeconds;
            float windowEnd = Time.unscaledTime + StuckWindowSeconds;
            float windowStart = DriveSteering.FlatDistance(_world.Rover.Position, target);
            int unsticks = 0;
            while (!Failed)
            {
                RoverPose rover = Rover();
                DriveSteering.Command command = DriveSteering.Decide(rover.Position, rover.Forward, target, arriveRadius);
                if (command.Arrived)
                {
                    break;
                }

                ApplyDrive(command);
                if (Time.unscaledTime > deadline)
                {
                    Fail($"timed out after {timeoutSeconds.ToString("0", CultureInfo.InvariantCulture)} s driving to " +
                         $"{what} ({unsticks} unstick(s)). {Where()}");
                    break;
                }

                yield return null;
                if (Time.unscaledTime < windowEnd)
                {
                    continue;
                }

                float distance = DriveSteering.FlatDistance(Rover().Position, target);
                if (windowStart - distance < StuckProgressMetres && command.Throttle > 0f)
                {
                    unsticks++;
                    if (_mayReplan && unsticks >= ReplanAfterUnsticks)
                    {
                        ReleaseDrive();
                        _replan = true;
                        yield break;
                    }

                    yield return Unstick(unsticks);
                    distance = DriveSteering.FlatDistance(Rover().Position, target);
                }

                windowStart = distance;
                windowEnd = Time.unscaledTime + StuckWindowSeconds;
            }

            if (!settle)
            {
                yield break;
            }

            ReleaseDrive();
            yield return Until(() => Rover().Speed < 0.3f, 6f, "07 to come to rest at " + what);
        }

        /// <summary>Turns 07 on the spot until its nose is on <paramref name="point"/>.</summary>
        public IEnumerator TurnToward(Vector3 point, string what)
        {
            Activity = "turning to " + what;
            float deadline = Time.unscaledTime + AlignTimeoutSeconds;
            while (!Failed)
            {
                RoverPose rover = Rover();
                float error = DriveSteering.HeadingError(rover.Position, rover.Forward, point);
                if (Mathf.Abs(error) <= AlignToleranceDegrees)
                {
                    break;
                }

                if (Time.unscaledTime > deadline)
                {
                    Fail($"07 could not turn to {what} within {AlignTimeoutSeconds.ToString("0", CultureInfo.InvariantCulture)} s " +
                         $"(still {error.ToString("0", CultureInfo.InvariantCulture)} deg off). {Where()}");
                    break;
                }

                _input.Set(Key.A, error < 0f);
                _input.Set(Key.D, error > 0f);
                yield return null;
            }

            ReleaseDrive();
            yield return Until(() => Rover().Speed < 0.3f, 4f, "07 to stop turning");
        }

        /// <summary>Backs 07 straight up (S held) until <paramref name="done"/> holds, as a player reverses off.</summary>
        public IEnumerator BackOff(Func<bool> done, float timeoutSeconds, string what)
        {
            Activity = "backing off " + what;
            ReleaseDrive();
            _input.Set(Key.S, true);
            yield return Until(done, timeoutSeconds, "07 to back off " + what);
            _input.Set(Key.S, false);
            yield return Until(() => Rover().Speed < 0.3f, 4f, "07 to stop backing off");
        }

        /// <summary>Moves the mouse until the view's centre is on <paramref name="point"/>.</summary>
        public IEnumerator LookAt(Vector3 point, string what)
        {
            Activity = "looking at " + what;
            float deadline = Time.unscaledTime + LookTimeoutSeconds;
            while (!Failed)
            {
                Transform view = _world.View.Camera.transform;
                float yaw = LookSteering.YawError(view.position, view.forward, point);
                float pitch = LookSteering.PitchError(view.position, view.forward, point);
                if (Mathf.Abs(yaw) <= LookSteering.ToleranceDegrees && Mathf.Abs(pitch) <= LookSteering.ToleranceDegrees)
                {
                    break;
                }

                if (Time.unscaledTime > deadline)
                {
                    Fail($"the view could not settle on {what} within {LookTimeoutSeconds.ToString("0", CultureInfo.InvariantCulture)} s " +
                         $"(yaw {yaw.ToString("0", CultureInfo.InvariantCulture)}, pitch {pitch.ToString("0", CultureInfo.InvariantCulture)} deg off). {Where()}");
                    break;
                }

                _input.Look(new Vector2(LookSteering.Pixels(yaw), LookSteering.Pixels(pitch)));
                yield return null;
            }
        }

        /// <summary>
        /// One frame's mouse movement toward <paramref name="point"/>, true when the view is on it: called every frame
        /// of a wait that needs the view to stay there, as a player's hand keeps correcting.
        /// </summary>
        public bool KeepLookingAt(Vector3 point)
        {
            Transform view = _world.View.Camera.transform;
            float yaw = LookSteering.YawError(view.position, view.forward, point);
            float pitch = LookSteering.PitchError(view.position, view.forward, point);
            _input.Look(new Vector2(LookSteering.Pixels(yaw), LookSteering.Pixels(pitch)));
            return Mathf.Abs(yaw) <= LookSteering.ToleranceDegrees && Mathf.Abs(pitch) <= LookSteering.ToleranceDegrees;
        }

        /// <summary>Where 07 is and how it moves, for a failure message and the slow-frame log.</summary>
        public string Where()
        {
            if (!_world.Refresh())
            {
                return "No booted game yet.";
            }

            RoverPose rover = Rover();
            return string.Format(CultureInfo.InvariantCulture, "07 at ({0:0.0}, {1:0.0}, {2:0.0}), heading {3:0}, {4:0.0} m/s; {5}.",
                rover.Position.x, rover.Position.y, rover.Position.z, rover.Heading, rover.Speed, Activity);
        }

        public void Shutdown()
        {
            ReleaseDrive();
            _input.ReleaseAll();
        }

        private IEnumerator Unstick(int attempt)
        {
            Activity = "backing off an obstacle";
            ReleaseDrive();
            _input.Set(Key.S, true);
            _input.Set(attempt % 2 == 0 ? Key.A : Key.D, true);
            yield return Wait(UnstickReverseSeconds);
            ReleaseDrive();
            _input.Set(Key.S, false);
        }

        private void ApplyDrive(DriveSteering.Command command)
        {
            _input.Set(Key.A, command.Turn < 0f);
            _input.Set(Key.D, command.Turn > 0f);
            _input.Set(Key.W, _gate.Next(command.Throttle));
        }

        private void ReleaseDrive()
        {
            _input.Set(Key.W, false);
            _input.Set(Key.A, false);
            _input.Set(Key.D, false);
            _gate.Next(0f);
        }

        private RoverPose Rover()
        {
            var state = _world.Rover;
            return new RoverPose(state.Position, state.Rotation * Vector3.forward, state.Rotation.eulerAngles.y,
                state.Speed);
        }

        private readonly struct RoverPose
        {
            public RoverPose(Vector3 position, Vector3 forward, float heading, float speed)
            {
                Position = position;
                Forward = forward;
                Heading = heading;
                Speed = speed;
            }

            public Vector3 Position { get; }

            public Vector3 Forward { get; }

            public float Heading { get; }

            public float Speed { get; }
        }
    }
}
