using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio
{
    /// <summary>
    /// 07's own small sounds, the company it keeps far from home (feel pillar 6): the warm hum of its lamp (from
    /// <see cref="RoverAwoke"/>), the whirr of its steering servo as the wheels turn and of its neck servo as the head
    /// looks around (<see cref="ServoWhir"/>), and its metal ticking as it cools after a drive
    /// (<see cref="MotorCooling"/>). All very soft and scaled by the <see cref="Soundscape"/>'s small-sounds gain,
    /// so they are heard mostly in the quiet. Game-time loops duck while paused. Initialised by
    /// <see cref="AudioDirector"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoverSmallSounds : MonoBehaviour
    {
        [Tooltip("Assets/_Project/Data/Audio/SoundscapeTuning.asset.")]
        [SerializeField] private SoundscapeTuning _tuning;

        private readonly ServoWhir _steering = new ServoWhir();
        private readonly ServoWhir _neck = new ServoWhir();
        private readonly LoopFader _lamp = new LoopFader();
        private AudioDirector _director;
        private Soundscape _soundscape;
        private IRoverState _rover;
        private Transform _eye;
        private MotorCooling _cooling;
        private AudioSource _lampHum;
        private AudioSource _steeringServo;
        private AudioSource _neckServo;
        private CueHandle _tick;
        private IDisposable _awokeSubscription;
        private Vector3 _lastGaze = Vector3.forward;
        private float _lastSteer;
        private float _lampCueVolume;
        private float _servoCueVolume;
        private bool _hasLast;

        internal AudioSource LampSource => _lampHum;

        internal AudioSource SteeringServoSource => _steeringServo;

        internal AudioSource NeckServoSource => _neckServo;

        /// <summary>07's heat from driving, 0 cold .. 1 hot (its metal ticks as it cools).</summary>
        public float Heat => _cooling != null ? _cooling.Heat : 0f;

        internal void Initialize(GameContext context, AudioDirector director, Soundscape soundscape)
        {
            if (_tuning == null)
            {
                Debug.LogError($"{nameof(RoverSmallSounds)}: {nameof(_tuning)} is not assigned.", this);
                enabled = false;
                return;
            }

            _eye = context.Get<IRoverRig>().TetherOrigin;
            if (_eye == null)
            {
                Debug.LogError($"{nameof(RoverSmallSounds)}: {nameof(IRoverRig)}.TetherOrigin is null.", this);
                enabled = false;
                return;
            }

            CueHandle lamp = director.Resolve(AudioCueIds.RoverLampHum);
            CueHandle servo = director.Resolve(AudioCueIds.RoverServo);
            _tick = director.Resolve(AudioCueIds.RoverMetalTick);
            if (!lamp.IsValid || !servo.IsValid || !_tick.IsValid)
            {
                enabled = false;
                return;
            }

            _director = director;
            _soundscape = soundscape;
            _rover = context.Get<IRoverState>();
            _cooling = new MotorCooling(_tuning, new AudioRandom(unchecked((uint)Environment.TickCount) ^ 0x2545F491u));
            _lampCueVolume = director.Library.GetCue(lamp).VolumeMax;
            _servoCueVolume = director.Library.GetCue(servo).VolumeMax;
            _lampHum = director.CreateLoopSource(transform, "LampHum", lamp, 1f);
            _steeringServo = director.CreateLoopSource(transform, "SteeringServo", servo, 1f);
            _neckServo = director.CreateLoopSource(transform, "NeckServo", servo, 1f);
            _awokeSubscription = context.Events.Subscribe<RoverAwoke>(OnRoverAwoke);
        }

        internal void Wire(SoundscapeTuning tuning)
        {
            _tuning = tuning;
        }

        private void Update()
        {
            if (_director == null)
            {
                return;
            }

            float dt = Time.deltaTime;
            float sfx = _director.Buses.Effective(AudioBus.Sfx) * _director.WorldGain * _soundscape.SmallSoundsGain;
            Vector3 body = _rover.Position;
            Vector3 eye = _eye.position;
            Vector3 gaze = Quaternion.Inverse(_rover.Rotation) * _eye.forward;
            float steer = _rover.DriveInput.x;
            float steerRate = _hasLast && dt > 0f ? Mathf.Abs(steer - _lastSteer) / dt : 0f;
            float neckRate = _hasLast ? ServoWhir.AngularRate(_lastGaze, gaze, dt) : 0f;
            _lastSteer = steer;
            _lastGaze = gaze;
            _hasLast = true;

            _steering.Step(steerRate, dt, _tuning.SteerDeadRate, _tuning.SteerFullRate, _tuning.ServoAttack,
                _tuning.ServoRelease);
            _neck.Step(neckRate, dt, _tuning.NeckDeadRate, _tuning.NeckFullRate, _tuning.ServoAttack,
                _tuning.ServoRelease);
            _steeringServo.transform.position = body;
            _neckServo.transform.position = eye;
            _lampHum.transform.position = eye;
            Drive(_steeringServo, _steering.Amount * _tuning.ServoVolume * _servoCueVolume * sfx,
                ServoPitch(_steering.Amount, 1f));
            Drive(_neckServo, _neck.Amount * _tuning.ServoVolume * _servoCueVolume * sfx,
                ServoPitch(_neck.Amount, _tuning.NeckPitch));

            _lamp.Step(dt, _tuning.LampFadeIn, _tuning.LampFadeIn);
            Drive(_lampHum, _lamp.Gain * _tuning.LampVolume * _lampCueVolume * sfx, 1f);

            bool moving = _rover.Speed >= _tuning.StillSpeed || !_rover.IsGrounded;
            float effort = _rover.IsGrounded ? _rover.NormalizedSpeed : 0f;
            if (_cooling.Step(dt, effort, moving, out float tickVolume))
            {
                _director.PlayAt(_tick, body, tickVolume * _soundscape.SmallSoundsGain);
            }
        }

        private float ServoPitch(float amount, float scale)
        {
            return Mathf.Lerp(_tuning.ServoMinPitch, _tuning.ServoMaxPitch, amount) * scale;
        }

        private static void Drive(AudioSource loop, float volume, float pitch)
        {
            loop.volume = volume;
            loop.pitch = pitch;
            if (volume > 0f && !loop.isPlaying)
            {
                loop.Play();
            }
            else if (volume <= 0f && loop.isPlaying)
            {
                loop.Stop();
            }
        }

        private void OnRoverAwoke(RoverAwoke awoke)
        {
            _lamp.FadeIn();
        }

        private void OnDestroy()
        {
            _awokeSubscription?.Dispose();
            _awokeSubscription = null;
        }
    }
}
