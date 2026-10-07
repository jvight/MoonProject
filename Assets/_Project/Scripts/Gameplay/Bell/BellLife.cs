using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Bell once she is awake (docs/features/M3-05 beats 4-6), as a pure state machine: senses in, a pose out. Right
    /// after her repair she does a little two-step, then waddles off home on her own. At home she listens: she sways
    /// and now and then taps a foot while music plays, turns her dial to watch 07 park, and her needle rests on the
    /// station's detent. While 07 is away, or after 07 has sat still near home for a long while, she dozes: her dial
    /// dims to a low ember, the needle rests and her knees give a little. Any movement of 07 near home, a new relic or
    /// a turn of her dial wakes her, slowly: the dial warms first, then the needle lifts, then a little stretch of the
    /// legs. When 07 comes home she wakes and greets it (the jingle); her very first homecoming greeting comes as soon
    /// as both are home. A new relic on the shelf makes her needle jiggle happily. Everything eases; nothing snaps.
    /// Allocation-free.
    /// </summary>
    public sealed class BellLife
    {
        private const float FullCircle = 2f * Mathf.PI;

        /// <summary>Share of the dance spent easing in and out of it.</summary>
        private const float DanceEdge = 0.15f;

        private readonly BellTuning _bell;
        private readonly FriendTuning _friends;
        private readonly RoverHomeWatch _watch = new RoverHomeWatch();
        private DeterministicRandom _random;
        private Mode _mode;
        private float _modeStart;
        private float _stillSince;
        private float _nextTap;
        private float _tapStart = float.NegativeInfinity;
        private float _greetStart = float.NegativeInfinity;
        private float _crackleStart = float.NegativeInfinity;
        private float _clickStart = float.NegativeInfinity;
        private bool _greetPending;
        private float _dial;
        private float _needle;
        private float _turn;
        private float _doze;
        private float _sway;
        private float _gait;
        private float _waddle;
        private float _wakeDial;
        private float _wakeNeedle;
        private float _wakeDoze;

        public BellLife(BellTuning bell, FriendTuning friends, int seed)
        {
            _bell = bell != null ? bell : throw new ArgumentNullException(nameof(bell));
            _friends = friends != null ? friends : throw new ArgumentNullException(nameof(friends));
            _random = new DeterministicRandom(seed);
        }

        public enum Mode
        {
            /// <summary>The two-step right after her repair.</summary>
            Dancing = 0,

            /// <summary>Waddling home on her own.</summary>
            Walking = 1,

            /// <summary>Home and awake: swaying, tapping, watching 07.</summary>
            Listening = 2,

            /// <summary>Home, dial dimmed to an ember.</summary>
            Dozing = 3,

            /// <summary>Waking up: dial, then needle, then a stretch.</summary>
            Waking = 4,
        }

        public Mode Current => _mode;

        /// <summary>She is home (listening, dozing or waking).</summary>
        public bool IsHome => _mode == Mode.Listening || _mode == Mode.Dozing || _mode == Mode.Waking;

        /// <summary>Current dial glow.</summary>
        public float Dial => _dial;

        /// <summary>Current needle angle (degrees).</summary>
        public float Needle => _needle;

        /// <summary>Just repaired at her site: her dial glows and the needle rests where the repair left it.</summary>
        public void Begin(float now, float needle)
        {
            _dial = _bell.DialGlow;
            _needle = needle;
            _doze = 0f;
            Enter(Mode.Dancing, now);
        }

        /// <summary>Loaded from a save: she is at her corner and 07 has just woken at home.</summary>
        public void Settle(float now, RadioChannel channel)
        {
            _dial = _bell.DialGlow;
            _needle = _bell.Detent(channel);
            _doze = 0f;
            _watch.Reset(true);
            _stillSince = now;
            ScheduleTap(now);
            Enter(Mode.Listening, now);
        }

        /// <summary>A new relic reached the shelf: false when she is not home to see it.</summary>
        public bool Crackle(float now)
        {
            if (!IsHome)
            {
                return false;
            }

            _crackleStart = now;
            WakeIfDozing(now);
            return true;
        }

        /// <summary>07 turned her dial one detent.</summary>
        public void DialClicked(float now)
        {
            _clickStart = now;
            WakeIfDozing(now);
        }

        public BellBeat Step(BellSenses senses)
        {
            var pose = new BellPose();
            bool greeted = false;
            bool tapped = false;
            float now = senses.Now;
            float deltaTime = senses.DeltaTime;
            switch (_mode)
            {
                case Mode.Dancing:
                    Dance(senses, ref pose);
                    if (now - _modeStart >= _bell.DanceDuration)
                    {
                        Enter(Mode.Walking, now);
                    }

                    break;
                case Mode.Walking:
                    if (senses.AtHome)
                    {
                        Arrive(senses);
                    }

                    break;
                default:
                    greeted = LiveAtHome(senses);
                    tapped = Tap(senses);
                    break;
            }

            bool walking = _mode == Mode.Walking;
            _gait = Damp.Toward(_gait, walking ? 1f : 0f, _bell.TurnEase, deltaTime);
            if (walking)
            {
                _waddle += senses.Walked * _bell.WaddlePerMetre;
            }

            Waddle(ref pose);
            if (IsHome)
            {
                PoseAtHome(senses, ref pose);
            }
            else
            {
                pose.Needle = _needle;
                pose.DialLamp = _dial;
            }

            FriendActivity activity = _mode == Mode.Dozing ? FriendActivity.Napping : FriendActivity.Home;
            float motor = _mode == Mode.Dancing ? _bell.DanceMotor : _bell.WalkMotor * _gait;
            return new BellBeat(pose, activity, motor, greeted, tapped);
        }

        private void Arrive(BellSenses senses)
        {
            float distance = SurfaceRules.HorizontalDistance(senses.Rover, senses.Home);
            _watch.Reset(distance < _friends.HomeRadius);
            _stillSince = senses.Now;
            ScheduleTap(senses.Now);
            Enter(Mode.Listening, senses.Now);
        }

        /// <summary>Home life's choices; true when she begins a greeting this frame.</summary>
        private bool LiveAtHome(BellSenses senses)
        {
            float now = senses.Now;
            RoverHomeWatch.Change change = _watch.Step(senses.Rover, senses.Home, _friends);
            bool moving = senses.RoverSpeed >= _bell.StillSpeed;
            if (moving || !_watch.Home)
            {
                _stillSince = now;
            }

            if (change == RoverHomeWatch.Change.CameHome)
            {
                _greetPending = true;
            }

            switch (_mode)
            {
                case Mode.Listening:
                    if (!_watch.Home || now - _stillSince >= _bell.DozeAfter)
                    {
                        Enter(Mode.Dozing, now);
                    }

                    break;
                case Mode.Dozing:
                    if (_watch.Home && (moving || _greetPending))
                    {
                        BeginWaking(now);
                    }

                    break;
                default:
                    if (now - _modeStart >= _bell.WakeDuration)
                    {
                        _stillSince = now;
                        Enter(Mode.Listening, now);
                    }

                    break;
            }

            bool greeting = now - _greetStart < _bell.GreetDuration;
            bool firstHomecoming = !senses.Welcomed && _watch.Home;
            if (_mode != Mode.Listening || greeting || !(_greetPending || firstHomecoming))
            {
                return false;
            }

            _greetPending = false;
            _greetStart = now;
            return true;
        }

        private bool Tap(BellSenses senses)
        {
            bool music = senses.Channel != RadioChannel.QuietHours;
            if (_mode != Mode.Listening || !music || senses.Now < _nextTap)
            {
                return false;
            }

            _tapStart = senses.Now;
            ScheduleTap(senses.Now);
            return true;
        }

        private void PoseAtHome(BellSenses senses, ref BellPose pose)
        {
            float now = senses.Now;
            float deltaTime = senses.DeltaTime;
            float detent = _bell.Detent(senses.Channel);
            bool music = senses.Channel != RadioChannel.QuietHours;
            switch (_mode)
            {
                case Mode.Dozing:
                    _dial = Damp.Toward(_dial, _bell.EmberGlow, _bell.DozeEase, deltaTime);
                    _needle = Damp.Toward(_needle, _bell.Detent(RadioChannel.LumenAfterDark), _bell.DozeEase,
                        deltaTime);
                    _doze = Damp.Toward(_doze, 1f, _bell.DozeEase, deltaTime);
                    break;
                case Mode.Waking:
                    Wake(now - _modeStart, detent, ref pose);
                    break;
                default:
                    _dial = Damp.Toward(_dial, _bell.DialGlow, _bell.DialEase, deltaTime);
                    _needle = Damp.Toward(_needle, detent, _bell.NeedleEase, deltaTime);
                    _doze = Damp.Toward(_doze, 0f, _bell.DialEase, deltaTime);
                    break;
            }

            _sway = Damp.Toward(_sway, _mode == Mode.Listening && music ? 1f : 0f, _bell.DialEase, deltaTime);
            float sway = FullCircle * _bell.SwayRate * now;
            float breath = 0.5f - 0.5f * Mathf.Cos(FullCircle * now / _bell.BreathPeriod);
            pose.Roll += Mathf.Sin(sway) * _bell.SwayRoll * _sway;
            pose.Bob += (0.5f - 0.5f * Mathf.Cos(2f * sway)) * _bell.SwayBob * _sway + breath * _bell.BreathBob -
                        _doze * _bell.DozeSink;
            pose.Speaker = Mathf.Sin(FullCircle * _bell.SpeakerRate * now) * _bell.SpeakerPulse * _sway;
            for (int leg = 0; leg < BellPose.Legs; leg++)
            {
                pose.AddLeg(leg, 0f, _doze * _bell.DozeKnee);
            }

            float tap = Ease.Hump((now - _tapStart) / _bell.FootTapDuration);
            pose.AddLeg(BellPose.FrontRight, _bell.FootTapLeg * tap, _bell.FootTapKnee * tap);

            float greet = (now - _greetStart) / _bell.GreetDuration;
            if (greet < 1f)
            {
                pose.Lid += _bell.GreetLid * Ease.Hump(greet);
                pose.Bob += _bell.GreetHop * Ease.Hump(Mathf.Repeat(greet * _bell.GreetBounces, 1f));
            }

            float needle = _needle;
            float dial = _dial;
            float crackle = (now - _crackleStart) / _bell.CrackleDuration;
            if (crackle < 1f)
            {
                needle += Mathf.Sin(FullCircle * _bell.CrackleRate * (now - _crackleStart)) * _bell.CrackleNeedle *
                          (1f - crackle);
                dial += _bell.CrackleFlash * Ease.Hump(crackle);
            }

            float click = (now - _clickStart) / _bell.ClickDuration;
            if (click < 1f)
            {
                needle += _bell.ClickKick * Mathf.Sin(Mathf.PI * click) * (1f - click);
            }

            pose.Needle = needle;
            pose.DialLamp = dial;
            Watch(senses, ref pose);
        }

        /// <summary>Waking, in order: the dial warms, then the needle lifts to its station, then a stretch.</summary>
        private void Wake(float t, float detent, ref BellPose pose)
        {
            float dial = Ease.InOutSine(t / _bell.WakeDial);
            float lift = Ease.InOutSine((t - _bell.WakeDial) / _bell.WakeNeedle);
            float stretchStart = _bell.WakeDial + _bell.WakeNeedle;
            float stretch = (t - stretchStart) / _bell.WakeStretch;
            _dial = Mathf.Lerp(_wakeDial, _bell.DialGlow, dial);
            _needle = Mathf.Lerp(_wakeNeedle, detent, lift);
            _doze = _wakeDoze * (1f - Ease.InOutSine(stretch));
            float reach = Ease.Hump(stretch);
            pose.AddLeg(BellPose.FrontLeft, -_bell.StretchLeg * reach, 0f);
            pose.AddLeg(BellPose.FrontRight, -_bell.StretchLeg * reach, 0f);
            pose.AddLeg(BellPose.RearLeft, _bell.StretchLeg * reach, 0f);
            pose.AddLeg(BellPose.RearRight, _bell.StretchLeg * reach, 0f);
            pose.Bob += _bell.StretchLift * reach;
        }

        /// <summary>Turns her dial toward 07 while it is close and she is awake.</summary>
        private void Watch(BellSenses senses, ref BellPose pose)
        {
            float target = 0f;
            Vector3 toRover = senses.Rover - senses.Position;
            toRover.y = 0f;
            bool near = toRover.magnitude <= _bell.WatchRadius && toRover.sqrMagnitude > 1e-4f;
            if (_mode != Mode.Dozing && near)
            {
                float offset = Mathf.DeltaAngle(senses.HomeYaw, SurfaceRules.Bearing(toRover));
                target = Mathf.Clamp(offset, -_bell.WatchMaxTurn, _bell.WatchMaxTurn);
            }

            _turn = Damp.Toward(_turn, target, _bell.TurnEase, senses.DeltaTime);
            pose.Turn = _turn;
        }

        /// <summary>The two-step: one side's legs, then the other's, with a hop and a sway into each step.</summary>
        private void Dance(BellSenses senses, ref BellPose pose)
        {
            float elapsed = senses.Now - _modeStart;
            float duration = _bell.DanceDuration;
            float edge = duration * DanceEdge;
            float level = Ease.Step(0f, edge, elapsed) * (1f - Ease.Step(duration - edge, duration, elapsed));
            float steps = elapsed * _bell.DanceRate;
            float step = Ease.Hump(Mathf.Repeat(steps, 1f));
            bool left = Mathf.Repeat(steps, 2f) < 1f;
            float lift = -_bell.DanceStep * step * level;
            float knee = _bell.DanceStep * step * level;
            pose.AddLeg(left ? BellPose.FrontLeft : BellPose.FrontRight, lift, knee);
            pose.AddLeg(left ? BellPose.RearLeft : BellPose.RearRight, -lift, knee);
            pose.Roll += Mathf.Sin(Mathf.PI * steps) * _bell.DanceRoll * level;
            pose.Bob += _bell.DanceHop * step * level;
        }

        /// <summary>
        /// The waddle (art: diagonal pairs FL+RR and FR+RL swing against each other, the pair swinging forward bends
        /// its knees, with a little body roll and bob), scaled by how much she is walking.
        /// </summary>
        private void Waddle(ref BellPose pose)
        {
            if (_gait <= 0f)
            {
                return;
            }

            float phase = FullCircle * _waddle;
            float swing = Mathf.Sin(phase) * _bell.WaddleSwing * _gait;
            float kneeA = Mathf.Max(0f, Mathf.Cos(phase)) * _bell.WaddleKnee * _gait;
            float kneeB = Mathf.Max(0f, -Mathf.Cos(phase)) * _bell.WaddleKnee * _gait;
            pose.AddLeg(BellPose.FrontLeft, -swing, kneeA);
            pose.AddLeg(BellPose.RearRight, -swing, kneeA);
            pose.AddLeg(BellPose.FrontRight, swing, kneeB);
            pose.AddLeg(BellPose.RearLeft, swing, kneeB);
            pose.Roll += Mathf.Sin(phase) * _bell.WaddleRoll * _gait;
            pose.Bob += (0.5f - 0.5f * Mathf.Cos(2f * phase)) * _bell.WaddleBob * _gait;
        }

        private void BeginWaking(float now)
        {
            _wakeDial = _dial;
            _wakeNeedle = _needle;
            _wakeDoze = _doze;
            Enter(Mode.Waking, now);
        }

        private void WakeIfDozing(float now)
        {
            if (_mode == Mode.Dozing)
            {
                BeginWaking(now);
            }
        }

        private void ScheduleTap(float now)
        {
            Vector2 interval = _bell.FootTapInterval;
            _nextTap = now + _random.Range(interval.x, interval.y);
        }

        private void Enter(Mode mode, float now)
        {
            _mode = mode;
            _modeStart = now;
        }
    }
}
