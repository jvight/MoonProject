using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio
{
    /// <summary>
    /// 07's radio (docs/design/opening-v2.md B0): a set that hears only static, never music. Silent until
    /// <see cref="RoverAwoke"/>: then it crackles on with a tuning swish into a faint signal hiss, the only thing it
    /// hears while the base has no power; when the base powers on (<see cref="OpeningMilestoneReached"/>
    /// <see cref="OpeningMilestone.PowerOn"/>) the hiss clears. The static follows 07's distance from the base through
    /// <see cref="RadioSignal"/>: faint near home, thicker towards the signal's edge; it thickens a little while 07
    /// crawls on its reserve, steps back under the pause menu, and pulls back when 07 is still (the
    /// <see cref="Soundscape"/>'s mix). Initialised by <see cref="AudioDirector"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RadioStation : MonoBehaviour
    {
        private const int SubscriptionCount = 5;

        [Tooltip("Assets/_Project/Data/Audio/RadioTuning.asset.")]
        [SerializeField] private RadioTuning _tuning;

        private readonly RadioWakeUp _wake = new RadioWakeUp();
        private readonly EasedValue _hissFloor = new EasedValue(0f);
        private readonly IDisposable[] _subscriptions = new IDisposable[SubscriptionCount];
        private bool _openingHoldsRadio;
        private bool _powered;
        private IRoverState _listener;
        private IWorldLayout _layout;
        private AudioDirector _director;
        private Soundscape _soundscape;
        private RadioSignal _signal;
        private bool _crawling;
        private AudioSource _static;
        private AudioSource _swish;
        private float _staticCueVolume;
        private float _swishCueVolume;

        /// <summary>True once 07 has started waking and the radio has come on.</summary>
        public bool IsOn => _wake.IsAwake;

        /// <summary>True once the base has power: the signal hiss has cleared.</summary>
        public bool Powered => _powered;

        /// <summary>Smoothed signal clarity the player hears, 0 (lost) .. 1 (clear).</summary>
        public float Clarity => _signal != null ? _signal.Clarity : 0f;

        /// <summary>Where the signal is lost entirely: the clear radius plus the falloff (metres from the base).</summary>
        public float SignalEdge => _signal != null ? _signal.Radius + _tuning.FalloffWidth : 0f;

        /// <summary>The hiss level the unpowered set holds above the clarity static (0 once the hiss has cleared).
        /// </summary>
        public float HissFloor => _hissFloor.Value;

        /// <summary>07's horizontal distance from the base at <paramref name="position"/> (metres).</summary>
        public float ReachDistance(Vector3 position)
        {
            return SignalField.HorizontalDistance(position, _layout.BasePosition);
        }

        internal void Initialize(GameContext context, AudioDirector director)
        {
            if (_tuning == null)
            {
                Debug.LogError($"{nameof(RadioStation)}: {nameof(_tuning)} is not assigned.", this);
                enabled = false;
                return;
            }

            CueHandle staticCue = director.Resolve(AudioCueIds.RadioStatic);
            CueHandle swishCue = director.Resolve(AudioCueIds.RadioTune);
            if (!staticCue.IsValid || !swishCue.IsValid)
            {
                enabled = false;
                return;
            }

            _director = director;
            _soundscape = director.Soundscape;
            _listener = context.Get<IRoverState>();
            _layout = context.Get<IWorldLayout>();
            _signal = new RadioSignal(_tuning);
            _signal.Snap(ReachDistance(_listener.Position));

            _static = director.CreateLoopSource(transform, "Static", staticCue, 0f);
            _staticCueVolume = director.Library.GetCue(staticCue).VolumeMax;
            _swish = director.CreateLoopSource(transform, "TuningSwish", swishCue, 0f);
            _swish.loop = false;
            _swishCueVolume = director.Library.GetCue(swishCue).VolumeMax;
            _hissFloor.Snap(_tuning.UnpoweredStaticFloor);

            _subscriptions[0] = context.Events.Subscribe<RoverAwoke>(OnRoverAwoke);
            _subscriptions[1] = context.Events.Subscribe<RoverWakeBeatReached>(OnWakeBeat);
            _subscriptions[2] = context.Events.Subscribe<RoverPlaced>(OnRoverPlaced);
            _subscriptions[3] = context.Events.Subscribe<LowPowerChanged>(OnLowPowerChanged);
            _subscriptions[4] = context.Events.Subscribe<OpeningMilestoneReached>(OnMilestone);
        }

        internal void Wire(RadioTuning tuning)
        {
            _tuning = tuning;
        }

        private void Update()
        {
            if (_signal == null || !_wake.IsAwake)
            {
                return;
            }

            float dt = Time.unscaledDeltaTime;
            _signal.Step(ReachDistance(_listener.Position) + (_crawling ? _tuning.CrawlDistance : 0f), dt);
            _wake.Step(dt);

            float level = _director.Buses.Effective(AudioBus.Music) * _wake.Power;
            float floor = _hissFloor.Step(_powered ? 0f : _tuning.UnpoweredStaticFloor, dt, _tuning.PowerOnClearTime);
            float staticVolume = Mathf.Max(_signal.StaticVolume, floor) + _tuning.WakeStaticBoost * _wake.CrackleBoost;
            _static.volume = Mathf.Clamp01(staticVolume) * _staticCueVolume * level * _soundscape.RadioGain *
                             Mathf.Lerp(1f, _tuning.CabinStaticGain, _director.CabinBlend);
            _swish.volume = _tuning.TuneSwishVolume * _swishCueVolume * level * _soundscape.RadioGain;
        }

        /// <summary>The base has power: the hiss clears (at once for a load).</summary>
        private void OnMilestone(OpeningMilestoneReached reached)
        {
            if (reached.Milestone != OpeningMilestone.PowerOn || _powered)
            {
                return;
            }

            _powered = true;
            if (reached.Restored)
            {
                _hissFloor.Snap(0f);
            }
        }

        /// <summary>07 crawls home on its reserve: the static thickens as if a little farther from the base, eased.
        /// </summary>
        private void OnLowPowerChanged(LowPowerChanged lowPower)
        {
            _crawling = lowPower.LowPower;
        }

        private void OnRoverPlaced(RoverPlaced placed)
        {
            // Set down unseen (a recovery): the signal is the new place's at once.
            _signal.Snap(ReachDistance(placed.Position));
        }

        private void OnRoverAwoke(RoverAwoke awoke)
        {
            if (!_openingHoldsRadio)
            {
                WakeUp(awoke.WokenByPlayer);
            }
        }

        /// <summary>
        /// 07's opening keeps the set asleep: the Asleep static is heard from inside 07 instead, and the radio wakes
        /// at the chase frame.
        /// </summary>
        private void OnWakeBeat(RoverWakeBeatReached reached)
        {
            switch (reached.Beat)
            {
                case WakeBeat.FadedOut:
                case WakeBeat.Crackle:
                    _openingHoldsRadio = true;
                    break;
                case WakeBeat.ChaseFrame:
                    if (_openingHoldsRadio)
                    {
                        _openingHoldsRadio = false;
                        WakeUp(false);
                    }

                    break;
            }
        }

        private void WakeUp(bool byPlayer)
        {
            if (_wake.IsAwake)
            {
                return;
            }

            _wake.Begin(byPlayer ? _tuning.PlayerWakeSettleDelay : _tuning.WakeSettleDelay,
                byPlayer ? _tuning.PlayerWakeSettleTime : _tuning.WakeSettleTime, _tuning.WakePowerTime);
            _static.volume = 0f;
            _static.Play();
            _swish.Play();
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _subscriptions.Length; i++)
            {
                _subscriptions[i]?.Dispose();
                _subscriptions[i] = null;
            }
        }
    }
}
