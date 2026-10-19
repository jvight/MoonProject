using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover
{
    /// <summary>One thing the self-test has to announce.</summary>
    public readonly struct PostEvent
    {
        public PostEvent(PostEventKind kind, BootCheckStep step, BootCheckResult result, int index = 0,
            float progress01 = 0f)
        {
            Kind = kind;
            Step = step;
            Result = result;
            Index = index;
            Progress01 = progress01;
        }

        /// <summary>For <see cref="PostEventKind.Pulse"/>: which tick of the check.</summary>
        public int Index { get; }

        /// <summary>For <see cref="PostEventKind.Pulse"/>: progress through a continuous tick, else 0.</summary>
        public float Progress01 { get; }

        public PostEventKind Kind { get; }

        public BootCheckStep Step { get; }

        public BootCheckResult Result { get; }
    }

    public enum PostEventKind
    {
        Started = 0,
        Completed = 1,
        Finished = 2,
        Pulse = 3,

        /// <summary>07's face blinks (the ready performance's double blink; <see cref="PostEvent.Index"/> counts them).</summary>
        Blink = 4,
    }

    /// <summary>
    /// The lines of 07's power-on self-test once its eyes are awake, as a pure clock (EditMode-tested): the tuned
    /// lines (<see cref="PostSettings.LineStep"/>) in their order, each for its tuned seconds, announced when it starts
    /// and when it is done with its result, with the check's pulses (each wheel twitch, spark, flicker; the continuous
    /// ramps ten times a second) in between. The opening publishes the events as Core's <c>BootCheckStarted</c>/
    /// <c>Completed</c>/<c>Finished</c>, and drives each step's motion from <see cref="Current"/> and
    /// <see cref="StepProgress01"/>. Only time moves it; the opening fast-forwards it by stepping it faster.
    /// </summary>
    public sealed class PostSequencer
    {
        private const int EventCapacity = 64;
        private const float ContinuousInterval = 0.1f;
        private const int LampRampIndex = 3;

        private readonly PostSettings _settings;
        private readonly LampPowerUp _lamps;
        private readonly PostEvent[] _events = new PostEvent[EventCapacity];
        private int _head;
        private int _count;
        private int _index = -1;
        private float _stepTime;
        private float _stepLength;
        private int _pulsesSent;
        private float _nextTick;

        public PostSequencer(PostSettings settings, LampPowerUp lamps)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _lamps = lamps ?? throw new ArgumentNullException(nameof(lamps));
        }

        /// <summary>The number of lines, the information lines included.</summary>
        public int StepCount => _settings.LineCount;

        /// <summary>True from <see cref="Begin"/> until the last step is done.</summary>
        public bool IsRunning { get; private set; }

        /// <summary>True once the last step has completed (and <see cref="PostEventKind.Finished"/> was queued).</summary>
        public bool IsFinished { get; private set; }

        /// <summary>The step under way (meaningful while <see cref="IsRunning"/>).</summary>
        public BootCheckStep Current => _index >= 0 && _index < StepCount ? _settings.LineStep(_index) : BootCheckStep.Carrier;

        /// <summary>How far through the current step, 0..1.</summary>
        public float StepProgress01 => _stepLength > 1e-4f ? Mathf.Clamp01(_stepTime / _stepLength) : 1f;

        /// <summary>Seconds the whole run takes.</summary>
        public float TotalSeconds
        {
            get
            {
                float total = 0f;
                for (int i = 0; i < StepCount; i++)
                {
                    total += _settings.LineSeconds(i);
                }

                return total;
            }
        }

        public void Begin()
        {
            _head = 0;
            _count = 0;
            IsFinished = false;
            IsRunning = true;
            _index = -1;
            Advance();
        }

        public void Step(float deltaTime)
        {
            if (!IsRunning)
            {
                return;
            }

            _stepTime += Mathf.Max(deltaTime, 0f);
            while (IsRunning && _stepTime >= _stepLength)
            {
                float carry = _stepTime - _stepLength;
                EmitPulses(_stepLength);
                BootCheckStep step = _settings.LineStep(_index);
                Publish(new PostEvent(PostEventKind.Completed, step, ResultOf(step)));
                Advance();
                _stepTime = carry;
            }

            if (IsRunning)
            {
                EmitPulses(_stepTime);
            }
        }

        /// <summary>Takes the oldest event not yet taken.</summary>
        public bool TryTake(out PostEvent postEvent)
        {
            if (_count == 0)
            {
                postEvent = default;
                return false;
            }

            postEvent = _events[_head];
            _head = (_head + 1) % EventCapacity;
            _count--;
            return true;
        }

        private void Advance()
        {
            _index++;
            _stepTime = 0f;
            if (_index >= StepCount)
            {
                IsRunning = false;
                IsFinished = true;
                _stepLength = 0f;
                Publish(new PostEvent(PostEventKind.Finished, BootCheckStep.Begin, BootCheckResult.Info));
                return;
            }

            _stepLength = _settings.LineSeconds(_index);
            _pulsesSent = 0;
            _nextTick = 0f;
            Publish(new PostEvent(PostEventKind.Started, _settings.LineStep(_index), BootCheckResult.Info));
        }

        /// <summary>Publishes the current check's pulses that fall at or before <paramref name="time"/> into the check.</summary>
        private void EmitPulses(float time)
        {
            BootCheckStep step = _settings.LineStep(_index);
            if (step == BootCheckStep.Lamps)
            {
                EmitLampPulses(time);
                return;
            }

            int count = PostMotions.PulseCount(step);
            while (_pulsesSent < count)
            {
                PostMotions.PulseMark mark = PostMotions.Pulse(step, _pulsesSent);
                if (mark.Share * _stepLength > time)
                {
                    break;
                }

                Publish(new PostEvent(PostEventKind.Pulse, step, BootCheckResult.Info, mark.Index));
                _pulsesSent++;
            }

            if (PostMotions.TryGetContinuous(step, out int index, out float startShare))
            {
                EmitContinuous(step, index, startShare * _stepLength, _stepLength, time);
            }
        }

        private void EmitLampPulses(float time)
        {
            int flickers = _lamps.FlickerCount;
            while (_pulsesSent < flickers && _lamps.FlickerCenter(_pulsesSent) <= time)
            {
                Publish(new PostEvent(PostEventKind.Pulse, BootCheckStep.Lamps, BootCheckResult.Info, _pulsesSent));
                _pulsesSent++;
            }

            float rampStart = _lamps.RampStart;
            EmitContinuous(BootCheckStep.Lamps, LampRampIndex, rampStart, rampStart + _lamps.RampSeconds, time);
        }

        /// <summary>A pulse with progress, ten times a second from <paramref name="start"/> to <paramref name="end"/>.</summary>
        private void EmitContinuous(BootCheckStep step, int index, float start, float end, float time)
        {
            if (_nextTick < start)
            {
                _nextTick = start;
            }

            while (_nextTick <= time && _nextTick <= end)
            {
                float progress = end > start ? Mathf.Clamp01((_nextTick - start) / (end - start)) : 1f;
                Publish(new PostEvent(PostEventKind.Pulse, step, BootCheckResult.Info, index, progress));
                _nextTick += ContinuousInterval;
            }
        }

        /// <summary>What <paramref name="step"/> comes out as: the story, not tuning.</summary>
        public static BootCheckResult ResultOf(BootCheckStep step)
        {
            switch (step)
            {
                case BootCheckStep.Lens:
                case BootCheckStep.Lamps:
                case BootCheckStep.Head:
                case BootCheckStep.Wheels:
                case BootCheckStep.Motor:
                case BootCheckStep.Battery:
                    return BootCheckResult.Ok;
                case BootCheckStep.Cutter:
                    return BootCheckResult.Fault;
                case BootCheckStep.SolarWing:
                    return BootCheckResult.Jammed;
                case BootCheckStep.Antenna:
                    return BootCheckResult.ReceiveOnly;
                default:
                    return BootCheckResult.Info;
            }
        }

        private void Publish(PostEvent postEvent)
        {
            if (_count == EventCapacity)
            {
                _head = (_head + 1) % EventCapacity;
                _count--;
            }

            _events[(_head + _count) % EventCapacity] = postEvent;
            _count++;
        }
    }
}
