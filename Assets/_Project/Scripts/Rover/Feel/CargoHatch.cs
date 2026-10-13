using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover
{
    /// <summary>
    /// Drives 07's rear cargo hatch and its crate window (M3-25 T1) from gameplay's beats:
    /// <list type="bullet">
    /// <item>a bundle flying in (Collect Anticipate, Seconds = its flight) opens the hatch just before it lands, and
    /// its arrival (Collect Reward) closes it with a soft thunk;</item>
    /// <item>a bundle bumping the shut hatch of a full hold (Cargo Impact) shakes it;</item>
    /// <item>unloading at the dock (Cargo Anticipate) opens it; its Reward or Cancel, or an empty hold, closes it;</item>
    /// <item>the window shows one crate per used hold slot (<see cref="CargoChanged"/>).</item>
    /// </list>
    /// Art's contract: RoverModel's <c>CargoHatch</c> (top hinge, opening about local +X) and its
    /// <c>HatchCrate_0..7</c> (top row left to right, then the bottom row, as seen from behind). Zero allocations.
    /// </summary>
    public sealed class CargoHatch : IDisposable
    {
        private readonly Transform _hatch;
        private readonly GameObject[] _crates;
        private readonly CargoHatchSettings _settings;
        private readonly RumbleMixer _rumble;
        private readonly CargoHatchMotion _motion;
        private readonly Quaternion _rest;
        private readonly IDisposable[] _subscriptions;

        public CargoHatch(EventBus events, CargoHatchSettings settings, Transform hatch, GameObject[] crates,
            RumbleMixer rumble)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _hatch = hatch != null ? hatch : throw new ArgumentNullException(nameof(hatch));
            _crates = crates ?? throw new ArgumentNullException(nameof(crates));
            _rumble = rumble ?? throw new ArgumentNullException(nameof(rumble));
            _motion = new CargoHatchMotion(settings);
            _rest = hatch.localRotation;
            ShowCrates(0);
            _subscriptions = new[]
            {
                events.Subscribe<FeelBeat>(OnBeat),
                events.Subscribe<CargoChanged>(OnCargoChanged),
            };
        }

        public CargoHatchMotion Motion => _motion;

        public void Tick(float deltaTime)
        {
            if (_motion.Step(deltaTime))
            {
                _rumble.Play(_settings.ThunkRumble);
            }

            _hatch.localRotation = _rest * Quaternion.AngleAxis(_motion.Angle, Vector3.right);
        }

        /// <summary>07 was placed: the hatch is shut at once.</summary>
        public void Reset()
        {
            _motion.Reset();
        }

        public void Dispose()
        {
            foreach (IDisposable subscription in _subscriptions)
            {
                subscription.Dispose();
            }
        }

        private void OnBeat(FeelBeat beat)
        {
            if (beat.Kind == FeelKind.Collect)
            {
                if (beat.Phase == FeelPhase.Anticipate)
                {
                    _motion.OpenBefore(beat.Seconds);
                }
                else if (beat.Phase == FeelPhase.Reward)
                {
                    _motion.Close();
                }

                return;
            }

            if (beat.Kind != FeelKind.Cargo)
            {
                return;
            }

            switch (beat.Phase)
            {
                case FeelPhase.Anticipate:
                    _motion.Open();
                    break;
                case FeelPhase.Impact:
                    _motion.Bump();
                    _rumble.Play(_settings.BumpRumble);
                    break;
                case FeelPhase.Reward:
                case FeelPhase.Cancel:
                    _motion.Close();
                    break;
            }
        }

        private void OnCargoChanged(CargoChanged cargo)
        {
            ShowCrates(cargo.UsedSlots);
            if (cargo.UsedSlots == 0 && _motion.IsOpening)
            {
                _motion.Close();
            }
        }

        private void ShowCrates(int used)
        {
            for (int i = 0; i < _crates.Length; i++)
            {
                bool shown = i < used;
                if (_crates[i].activeSelf != shown)
                {
                    _crates[i].SetActive(shown);
                }
            }
        }
    }
}
