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
    /// once on pause, focus loss and without a gamepad. Zero allocations per frame.
    /// </summary>
    public sealed class RoverFeel : IDisposable
    {
        private const int NoVoice = -1;

        private readonly CutFeelSettings _cut;
        private readonly IFeelSettings _preferences;
        private readonly IRumbleMotors _motors;
        private readonly RoverVisualRig _body;
        private readonly RumbleMixer _rumble = new RumbleMixer();
        private readonly ImpactNudge _nudge = new ImpactNudge();
        private readonly IDisposable _beats;
        private readonly IDisposable _pauses;
        private int _held = NoVoice;
        private bool _sustaining;
        private bool _paused;
        private bool _focused = true;

        public RoverFeel(EventBus events, CutFeelSettings cut, IFeelSettings preferences, IRumbleMotors motors,
            RoverVisualRig body)
        {
            _cut = cut ?? throw new ArgumentNullException(nameof(cut));
            _preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));
            _motors = motors ?? throw new ArgumentNullException(nameof(motors));
            _body = body;
            _beats = events.Subscribe<FeelBeat>(OnBeat);
            _pauses = events.Subscribe<PauseChanged>(OnPauseChanged);
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

        public void Tick(float deltaTime, float time)
        {
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
            _motors.Set(0f, 0f);
        }

        public void Dispose()
        {
            _beats.Dispose();
            _pauses.Dispose();
            Silence();
        }

        private void OnBeat(FeelBeat beat)
        {
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

        /// <summary>Ignition (strength 0, the first Act) kicks and settles to the sustain; each cut step retunes it.</summary>
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
