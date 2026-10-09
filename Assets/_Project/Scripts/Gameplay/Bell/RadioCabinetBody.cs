using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Bell's body (docs/features/M3-05): her broken and repaired rigs (<see cref="BellRig"/>) and her beats. Broken,
    /// she leans against the canyon wall with her dial lamp flickering (the warm light 07 saw from home). Repaired:
    /// she shivers under 07's stitching beam, 07 slides her tape in along the beam, her dial warms, the needle sweeps,
    /// she stands up on her four legs (<see cref="BellRepairSequence"/>). Awake (<see cref="BellLife"/>): a two-step,
    /// then she waddles home on her own and is set down at her corner the moment nobody could see it
    /// (<see cref="WalkHome"/>); at home she sways, taps, watches 07 park, dozes, wakes and greets. Her sounds go out
    /// as <see cref="BellCued"/>. Allocation-free per frame.
    /// </summary>
    internal sealed class RadioCabinetBody : IFriendBody
    {
        // Shiver frequencies (radians per second) on two axes: unrelated, so it reads as a shudder, not a wobble.
        private const float ShiverPitchRate = 37f;
        private const float ShiverRollRate = 29f;
        private const float WobbleRate = 7f;

        /// <summary>Her broken lamp flickers between these noise levels (dark below, full above).</summary>
        private const float FlickerLow = 0.35f;
        private const float FlickerHigh = 0.7f;

        /// <summary>The noise row her flicker reads (any fixed row: the flicker is deterministic).</summary>
        private const float FlickerRow = 0.37f;

        private readonly FriendTuning _friends;
        private readonly BellTuning _bell;
        private readonly ITerrainQuery _terrain;
        private readonly IRoverState _rover;
        private readonly IRoverRig _roverRig;
        private readonly IViewCamera _view;
        private readonly IRadioProgram _radio;
        private readonly EventBus _events;
        private readonly FriendProgress _progress;
        private readonly Transform _home;
        private readonly BellRig _broken;
        private readonly BellRig _repaired;
        private readonly Transform _tape;
        private readonly Vector3 _tapeScale;
        private readonly BellRepairSequence _sequence;
        private readonly BellLife _life;
        private readonly Vector3[] _wayHome;
        private readonly Vector3 _site;
        private readonly Quaternion _siteRotation;
        private WalkHome _walk;
        private Vector3 _position;
        private float _yaw;
        private bool _swapped;
        private bool _awake;
        private bool _tapeIn;
        private bool _swept;

        public RadioCabinetBody(FriendTuning friends, BellTuning bell, ITerrainQuery terrain, IRoverState rover,
            IRoverRig roverRig, IViewCamera view, IRadioProgram radio, EventBus events, FriendProgress progress,
            Transform home, BellRig broken, BellRig repaired, Transform tape, BellRepairSequence sequence,
            BellLife life, FriendSite site, Vector3[] wayHome)
        {
            _friends = friends != null ? friends : throw new ArgumentNullException(nameof(friends));
            _bell = bell != null ? bell : throw new ArgumentNullException(nameof(bell));
            _terrain = terrain ?? throw new ArgumentNullException(nameof(terrain));
            _rover = rover ?? throw new ArgumentNullException(nameof(rover));
            _roverRig = roverRig ?? throw new ArgumentNullException(nameof(roverRig));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _radio = radio ?? throw new ArgumentNullException(nameof(radio));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
            _home = home != null ? home : throw new ArgumentNullException(nameof(home));
            _broken = broken ?? throw new ArgumentNullException(nameof(broken));
            _repaired = repaired ?? throw new ArgumentNullException(nameof(repaired));
            _tape = tape != null ? tape : throw new ArgumentNullException(nameof(tape));
            _sequence = sequence ?? throw new ArgumentNullException(nameof(sequence));
            _life = life ?? throw new ArgumentNullException(nameof(life));
            _wayHome = wayHome ?? throw new ArgumentNullException(nameof(wayHome));
            if (site == null)
            {
                throw new ArgumentNullException(nameof(site));
            }

            _site = site.Position;
            _siteRotation = Quaternion.LookRotation(site.Facing);
            _position = _site;
            _yaw = _siteRotation.eulerAngles.y;
            _tapeScale = tape.localScale;
            _broken.Root.SetPositionAndRotation(_site, _siteRotation);
            _repaired.Root.SetPositionAndRotation(_site, _siteRotation);
            _repaired.Visible = false;
            _tape.gameObject.SetActive(false);
        }

        public Vector3 Position => _swapped ? _position : _site;

        public Vector3 BeamTarget => (_swapped ? _repaired : _broken).TapeSlot.position;

        public FriendActivity Activity { get; private set; } = FriendActivity.Dormant;

        public float Motor { get; private set; }

        public bool IsHome => _awake && _life.IsHome;

        public float RepairDuration => _sequence.Duration;

        /// <summary>Her tuning knob, where 07's beam taps to tune her (as repaired).</summary>
        public Vector3 Knob => _repaired.Knob.position;

        /// <summary>Where 07 parks to turn her dial: in front of her corner.</summary>
        public Vector3 DialFront => _home.position + _home.forward * _bell.TuneFront;

        internal BellLife Life => _life;

        public bool Beaming(float t)
        {
            return _sequence.Beaming(t);
        }

        public void SetLamp(int lamp, float intensity)
        {
            (_swapped ? _repaired : _broken).SetLamp(lamp, intensity);
        }

        public void StepBroken(float now, float deltaTime)
        {
            Activity = FriendActivity.Dormant;
            Motor = 0f;
            _broken.SetDialLamp(BrokenFlicker(now));
        }

        public void StepRepair(float t, float now, float deltaTime)
        {
            Activity = FriendActivity.Repairing;
            Motor = 0f;
            if (_sequence.Stitching(t))
            {
                float shiver = _friends.Shiver * Ease.InOutSine(_sequence.StitchProgress(t));
                _broken.Root.rotation = _siteRotation * Quaternion.Euler(
                    Mathf.Sin(now * ShiverPitchRate) * shiver, 0f, Mathf.Sin(now * ShiverRollRate) * shiver);
                _broken.SetDialLamp(BrokenFlicker(now));
                return;
            }

            if (!_swapped)
            {
                Swap();
            }

            StepTape(t);
            if (!_swept && t >= _sequence.SweepStart)
            {
                _swept = true;
                _events.Publish(new BellCued(BellCue.NeedleSwept, _site));
            }

            float stand = _sequence.Stand(t);
            var pose = new BellPose
            {
                Needle = _bell.Detent(RadioChannel.QuietHours) * _sequence.Needle(t),
                DialLamp = _bell.DialGlow * _sequence.DialLamp(t),
                Roll = _bell.StandWobble * Ease.Hump(stand) * Mathf.Sin(now * WobbleRate),
            };
            _repaired.Apply(stand, pose);
            _repaired.SetDialLamp(pose.DialLamp);
        }

        public void Wake(float now)
        {
            _awake = true;
            _tape.gameObject.SetActive(false);
            _life.Begin(now, 0f);
            _walk = new WalkHome(_site, _wayHome);
            _position = _site;
        }

        public void SettleAtHome(float now)
        {
            _awake = true;
            _swapped = true;
            _broken.Visible = false;
            _repaired.Visible = true;
            _tape.gameObject.SetActive(false);
            _walk = null;
            _position = _home.position;
            _yaw = _home.eulerAngles.y;
            _life.Settle(now, _radio.Channel);
            Activity = FriendActivity.Home;
            Motor = 0f;
        }

        public FriendBeat StepAwake(float now, float deltaTime)
        {
            float walked = 0f;
            float headingTarget = _yaw;
            if (_life.Current == BellLife.Mode.Walking && _walk != null && !_walk.IsHome)
            {
                Vector3 before = _walk.Position;
                _walk.Step(deltaTime, _terrain, ViewFrustum.Of(_view.Camera), _rover.Position, _friends);
                _position = _walk.Position;
                headingTarget = _walk.Heading;
                if (_walk.PlacedUnseen)
                {
                    _yaw = _home.eulerAngles.y;
                }
                else
                {
                    walked = SurfaceRules.HorizontalDistance(before, _walk.Position);
                }
            }

            bool home = _walk == null || _walk.IsHome;
            if (home && _life.IsHome)
            {
                _position = _home.position;
                headingTarget = _home.eulerAngles.y;
            }

            var senses = new BellSenses
            {
                Now = now,
                DeltaTime = deltaTime,
                Position = _position,
                Home = _home.position,
                HomeYaw = _home.eulerAngles.y,
                AtHome = home,
                Walked = walked,
                Hop = _walk != null ? _walk.Hop : 0f,
                Rover = _rover.Position,
                RoverSpeed = _rover.Speed,
                Channel = _radio.Channel,
                Welcomed = _progress.Welcomed,
            };
            BellBeat beat = _life.Step(senses);
            _yaw = Mathf.LerpAngle(_yaw, headingTarget, Damp.Factor(_bell.TurnEase, deltaTime));
            _repaired.Root.SetPositionAndRotation(_position, Quaternion.Euler(0f, _yaw + beat.Pose.Turn, 0f));
            _repaired.Apply(1f, beat.Pose);
            _repaired.SetDialLamp(beat.Pose.DialLamp);
            if (beat.FootTapped)
            {
                _events.Publish(new BellCued(BellCue.FootTapped, _position));
            }

            Activity = beat.Activity;
            Motor = beat.Motor;
            return new FriendBeat(beat.Greeted, false, default);
        }

        /// <summary>A new relic reached the shelf: a happy crackle if she is home to see it.</summary>
        public void NoticeNewRelic(float now)
        {
            if (_awake && _life.Crackle(now))
            {
                _events.Publish(new BellCued(BellCue.Crackled, _position));
            }
        }

        /// <summary>07's beam taps her tuning knob (a tiny tick): she turns the dial a beat later.</summary>
        public void KnobTapped()
        {
            _events.Publish(new BellCued(BellCue.DialTapped, Knob));
        }

        /// <summary>07 turned her dial one detent.</summary>
        public void DialTurned(float now)
        {
            _life.DialClicked(now);
            _events.Publish(new BellCued(BellCue.DialTurned, _position));
        }

        private void Swap()
        {
            _swapped = true;
            _repaired.Root.SetPositionAndRotation(_site, _siteRotation);
            _repaired.CapturePoseFrom(_broken);
            _repaired.Apply(0f, new BellPose());
            _repaired.Visible = true;
            _broken.Visible = false;
        }

        /// <summary>The tape glides from 07 along the beam into her slot, then clicks in.</summary>
        private void StepTape(float t)
        {
            if (_tapeIn)
            {
                return;
            }

            if (t >= _sequence.TapeIn)
            {
                _tapeIn = true;
                _tape.gameObject.SetActive(false);
                _events.Publish(new BellCued(BellCue.TapeSlotted, _repaired.TapeSlot.position));
                return;
            }

            float glide = _sequence.Tape(t);
            Transform slot = _repaired.TapeSlot;
            Vector3 from = _roverRig.CargoSocket.position;
            Vector3 position = Vector3.Lerp(from, slot.position, glide) +
                               Vector3.up * (_bell.TapeArc * Ease.Hump(glide));
            Vector3 travel = slot.position - from;
            Quaternion along = travel.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(travel) : slot.rotation;
            _tape.SetPositionAndRotation(position, Quaternion.Slerp(along, slot.rotation, glide));
            _tape.localScale = _tapeScale * Mathf.Lerp(1f, _bell.TapeArrivalScale, glide);
            _tape.gameObject.SetActive(true);
        }

        private float BrokenFlicker(float now)
        {
            float noise = Mathf.PerlinNoise(now * _bell.BrokenFlickerRate, FlickerRow);
            return _bell.BrokenFlicker * Ease.Step(FlickerLow, FlickerHigh, noise);
        }
    }
}
