using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover
{
    /// <summary>
    /// 07's chassis and the gamepad answering the feel beats (docs/design/feel-pass.md §3.1, §4): the cut's (and the
    /// dig's) lean, recoil, steadying micro-motions, crack dip and received rear dip on <see cref="ImpactNudge"/>, and
    /// its rumble through <see cref="RumbleMixer"/>, under the player's <see cref="IFeelSettings"/>. Rumble stops at
    /// once on pause, focus loss and without a gamepad. The dock's clunk dips 07 and rumbles (§7.1). Zero allocations
    /// per frame.
    /// </summary>
    public sealed class RoverFeel : IDisposable
    {
        private const int NoVoice = -1;

        private readonly CutFeelSettings _cut;
        private readonly DockFeelSettings _dock;
        private readonly MotionFeelSettings _motion;
        private readonly EventBus _events;
        private readonly IDisposable[] _verbs;
        private int _chargeVoice = NoVoice;
        private int _boostVoice = NoVoice;
        private bool _moving;
        private readonly IFeelSettings _preferences;
        private readonly IRumbleMotors _motors;
        private readonly RoverVisualRig _body;
        private readonly RumbleMixer _rumble = new RumbleMixer();
        private readonly ImpactNudge _nudge = new ImpactNudge();
        private readonly IDisposable _beats;
        private readonly IDisposable _pauses;
        private int _held = NoVoice;
        private Vector3 _position;
        private bool _sustaining;
        private bool _paused;
        private bool _focused = true;

        public RoverFeel(EventBus events, CutFeelSettings cut, DockFeelSettings dock, MotionFeelSettings motion,
            IFeelSettings preferences, IRumbleMotors motors, RoverVisualRig body)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _cut = cut ?? throw new ArgumentNullException(nameof(cut));
            _dock = dock ?? throw new ArgumentNullException(nameof(dock));
            _motion = motion ?? throw new ArgumentNullException(nameof(motion));
            _preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
            _motors = motors ?? throw new ArgumentNullException(nameof(motors));
            _body = body;
            _beats = events.Subscribe<FeelBeat>(OnBeat);
            _pauses = events.Subscribe<PauseChanged>(OnPauseChanged);
            _verbs = new[]
            {
                events.Subscribe<RoverJumpCharged>(charged => Publish(FeelKind.Leap, FeelPhase.Anticipate,
                    charged.Strength)),
                events.Subscribe<RoverJumped>(jumped => Publish(FeelKind.Leap, FeelPhase.Act, jumped.Strength)),
                events.Subscribe<RoverLanded>(OnLanded),
                events.Subscribe<RoverKitFitted>(_ => Publish(FeelKind.Install, FeelPhase.Impact, 1f)),
                events.Subscribe<LowPowerChanged>(OnLowPowerChanged),
                events.Subscribe<RoverBoostChanged>(OnBoostChanged),
            };
        }

        /// <summary>The chassis response the visual rig applies.</summary>
        public ImpactNudge Nudge => _nudge;

        /// <summary>The rumble mix (tests and tooling).</summary>
        public RumbleMixer Rumble => _rumble;

        /// <summary>The window lost or regained focus: rumble stops while it is away.</summary>
        public void SetFocused(bool focused)
        {
            _focused = focused;
            if (!focused)
            {
                Silence();
            }
        }

        /// <param name="speed">07's ground speed (m/s): drive start and stop are told from it (Drive beats).</param>
        /// <param name="position">Where 07 is, for the beats it publishes.</param>
        public void Tick(float deltaTime, float time, float speed, Vector3 position)
        {
            _position = position;
            if (!_moving && speed > _motion.StartSpeed)
            {
                _moving = true;
                Publish(FeelKind.Drive, FeelPhase.Anticipate, Mathf.Clamp01(speed));
            }
            else if (_moving && speed < _motion.StopSpeed)
            {
                _moving = false;
                Publish(FeelKind.Drive, FeelPhase.Impact, 1f);
            }

            float jitter = _sustaining
                ? (2f * Mathf.PerlinNoise(time * _cut.SustainJitterRate, 0f) - 1f) * _cut.SustainJitter
                : 0f;
            _nudge.Step(deltaTime, jitter);

            _rumble.Enabled = _preferences.RumbleEnabled && !_paused && _focused && _motors.IsConnected;
            _rumble.Strength = _preferences.RumbleStrength;
            if (!_rumble.Enabled)
            {
                Silence();
                return;
            }

            _rumble.Step(deltaTime);
            _motors.Set(_rumble.Low, _rumble.High);
        }

        /// <summary>Stops the motors at once and drops every playing profile.</summary>
        public void Silence()
        {
            _rumble.StopAll();
            _held = NoVoice;
            _chargeVoice = NoVoice;
            _boostVoice = NoVoice;
            _motors.Set(0f, 0f);
        }

        public void Dispose()
        {
            _beats.Dispose();
            _pauses.Dispose();
            foreach (IDisposable verb in _verbs)
            {
                verb.Dispose();
            }

            Silence();
        }

        private void OnBeat(FeelBeat beat)
        {
            if (AnswerMotion(beat))
            {
                return;
            }

            if (beat.Kind == FeelKind.Collect && beat.Phase == FeelPhase.Reward)
            {
                _rumble.Play(_cut.ReceivedTick);
                _nudge.KickPitch(_cut.RearDip, -1f);
                if (_body != null)
                {
                    _body.KickAntenna(_cut.AntennaWiggle);
                }

                return;
            }

            if (beat.Kind == FeelKind.Dock && beat.Phase == FeelPhase.Impact)
            {
                _rumble.Play(_dock.LockRumble);
                _nudge.KickHeave(_dock.LockDip);
                return;
            }

            if (beat.Kind != FeelKind.Cut && beat.Kind != FeelKind.Dig)
            {
                return;
            }

            switch (beat.Phase)
            {
                case FeelPhase.Anticipate:
                    _held = _rumble.Play(_cut.Charge, 1f, beat.Seconds);
                    _nudge.Hold(_cut.Lean);
                    break;
                case FeelPhase.Act:
                    Act(beat.Strength);
                    break;
                case FeelPhase.Cancel:
                    _rumble.FadeOut(_cut.ReleaseFade);
                    _held = NoVoice;
                    _sustaining = false;
                    _nudge.Release();
                    break;
                case FeelPhase.Impact:
                    _rumble.Stop(_held);
                    _held = NoVoice;
                    _rumble.Play(_cut.Crack);
                    _sustaining = false;
                    _nudge.Release();
                    _nudge.KickHeave(_cut.CrackDip);
                    break;
                case FeelPhase.Follow:
                    if (beat.Strength >= 1f)
                    {
                        _rumble.Play(_cut.Fold);
                    }

                    break;
            }
        }

        /// <summary>Ignition (strength 0, the first Act) kicks and settles to the sustain; each cut step retunes
        /// it.</summary>
        private void Act(float progress)
        {
            if (!_sustaining)
            {
                _sustaining = true;
                _rumble.Stop(_held);
                _rumble.Play(_cut.Ignition);
                _held = _rumble.Play(_cut.Sustain, _cut.SustainIgnited);
                _nudge.KickSurge(_cut.Recoil);
                return;
            }

            if (_held == NoVoice)
            {
                _held = _rumble.Play(_cut.Sustain, _cut.SustainAt(progress));
                return;
            }

            _rumble.SetGain(_held, _cut.SustainAt(progress));
        }

        /// <summary>07's own verbs (the beats it publishes): drive, leap, land, install, the battery emptying.</summary>
        private bool AnswerMotion(FeelBeat beat)
        {
            switch (beat.Kind)
            {
                case FeelKind.Drive when beat.Phase == FeelPhase.Anticipate:
                    _nudge.KickPitch(_motion.StartSquat, -1f);
                    _rumble.Play(_motion.StartRumble);
                    return true;
                case FeelKind.Drive when beat.Phase == FeelPhase.Impact:
                    _nudge.KickPitch(_motion.StopDip);
                    _rumble.Play(_motion.StopRumble);
                    return true;
                case FeelKind.Leap when beat.Phase == FeelPhase.Anticipate:
                    _nudge.HoldHeave(_motion.CrouchSpring, _motion.Crouch * beat.Strength);
                    float level = _motion.ChargeLevel(beat.Strength);
                    if (_chargeVoice == NoVoice)
                    {
                        _chargeVoice = _rumble.Play(_motion.ChargeRumble, level);
                    }
                    else
                    {
                        _rumble.SetGain(_chargeVoice, level);
                    }

                    return true;
                case FeelKind.Leap when beat.Phase == FeelPhase.Act:
                    _nudge.Release();
                    _nudge.KickHeave(_motion.LaunchStretch, 1f);
                    _rumble.Stop(_chargeVoice);
                    _chargeVoice = NoVoice;
                    _rumble.Play(_motion.LaunchRumble);
                    return true;
                case FeelKind.Land when beat.Phase == FeelPhase.Impact:
                    _rumble.Play(_motion.LandRumble, _motion.LandLevel(beat.Strength));
                    return true;
                case FeelKind.Install when beat.Phase == FeelPhase.Impact:
                    _nudge.KickHeave(_motion.SeatDip);
                    _rumble.Play(_motion.SeatRumble);
                    return true;
                case FeelKind.LowPower when beat.Phase == FeelPhase.Impact:
                    _rumble.Play(_motion.PowerDown);
                    return true;
                default:
                    return false;
            }
        }

        private void OnLanded(RoverLanded landed)
        {
            float impact = landed.ImpactSpeed / _motion.HardLanding;
            _events.Publish(new FeelBeat(FeelKind.Land, FeelPhase.Impact, landed.Position, Vector3.up, impact, 0f));
        }

        private void OnLowPowerChanged(LowPowerChanged power)
        {
            if (power.LowPower && !power.Restored)
            {
                Publish(FeelKind.LowPower, FeelPhase.Impact, 1f);
            }
        }

        private void OnBoostChanged(RoverBoostChanged boost)
        {
            if (boost.Boosting && _boostVoice == NoVoice)
            {
                _boostVoice = _rumble.Play(_motion.Boost);
            }
            else if (!boost.Boosting)
            {
                _rumble.Stop(_boostVoice);
                _boostVoice = NoVoice;
            }
        }

        private void Publish(FeelKind kind, FeelPhase phase, float strength)
        {
            _events.Publish(new FeelBeat(kind, phase, _position, Vector3.up, strength, 0f));
        }

        private void OnPauseChanged(PauseChanged pause)
        {
            _paused = pause.Paused;
            if (_paused)
            {
                Silence();
            }
        }
    }
}
