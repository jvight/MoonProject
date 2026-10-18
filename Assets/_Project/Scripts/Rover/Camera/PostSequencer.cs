using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover
{
    /// <summary>One thing the self-test has to announce.</summary>
    public readonly struct PostEvent
    {
        public PostEvent(PostEventKind kind, BootCheckStep step, BootCheckResult result)
        {
            Kind = kind;
            Step = step;
            Result = result;
        }

        public PostEventKind Kind { get; }

        public BootCheckStep Step { get; }

        public BootCheckResult Result { get; }
    }

    public enum PostEventKind
    {
        Started = 0,
        Completed = 1,
        Finished = 2,
    }

    /// <summary>
    /// The checks of 07's power-on self-test after the lens has opened and the camera has pulled out, as a pure clock
    /// (EditMode-tested): each step in its order, for its tuned seconds, announced when it starts and when it is done
    /// with its result. The opening publishes the events as Core's <c>BootCheckStarted</c>/<c>Completed</c>/
    /// <c>Finished</c>, and drives each step's motion from <see cref="Current"/> and <see cref="StepProgress01"/>.
    /// Not skippable: only time moves it.
    /// </summary>
    public sealed class PostSequencer
    {
        private const int EventCapacity = 32;

        /// <summary>The checks in order, with what each comes out as (the story, not tuning).</summary>
        private static readonly (BootCheckStep Step, BootCheckResult Result)[] Checks =
        {
            (BootCheckStep.Lens, BootCheckResult.Ok),
            (BootCheckStep.Lamps, BootCheckResult.Ok),
            (BootCheckStep.Head, BootCheckResult.Ok),
            (BootCheckStep.Wheels, BootCheckResult.Ok),
            (BootCheckStep.Motor, BootCheckResult.Ok),
            (BootCheckStep.Cutter, BootCheckResult.Fault),
            (BootCheckStep.SolarWing, BootCheckResult.Jammed),
            (BootCheckStep.Antenna, BootCheckResult.ReceiveOnly),
            (BootCheckStep.Battery, BootCheckResult.Ok),
            (BootCheckStep.Assembly, BootCheckResult.Info),
            (BootCheckStep.Begin, BootCheckResult.Info),
        };

        private readonly PostSettings _settings;
        private readonly PostEvent[] _events = new PostEvent[EventCapacity];
        private int _head;
        private int _count;
        private int _index = -1;
        private float _stepTime;
        private float _stepLength;

        public PostSequencer(PostSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>The number of checks.</summary>
        public static int StepCount => Checks.Length;

        /// <summary>True from <see cref="Begin"/> until the last step is done.</summary>
        public bool IsRunning { get; private set; }

        /// <summary>True once the last step has completed (and <see cref="PostEventKind.Finished"/> was queued).</summary>
        public bool IsFinished { get; private set; }

        /// <summary>The step under way (meaningful while <see cref="IsRunning"/>).</summary>
        public BootCheckStep Current => _index >= 0 && _index < Checks.Length ? Checks[_index].Step : BootCheckStep.Lens;

        /// <summary>How far through the current step, 0..1.</summary>
        public float StepProgress01 => _stepLength > 1e-4f ? Mathf.Clamp01(_stepTime / _stepLength) : 1f;

        /// <summary>Seconds the whole run takes.</summary>
        public float TotalSeconds
        {
            get
            {
                float total = 0f;
                for (int i = 0; i < Checks.Length; i++)
                {
                    total += _settings.SecondsOf(Checks[i].Step);
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
                Publish(new PostEvent(PostEventKind.Completed, Checks[_index].Step, Checks[_index].Result));
                Advance();
                _stepTime = carry;
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
            if (_index >= Checks.Length)
            {
                IsRunning = false;
                IsFinished = true;
                _stepLength = 0f;
                Publish(new PostEvent(PostEventKind.Finished, BootCheckStep.Begin, BootCheckResult.Info));
                return;
            }

            _stepLength = _settings.SecondsOf(Checks[_index].Step);
            Publish(new PostEvent(PostEventKind.Started, Checks[_index].Step, BootCheckResult.Info));
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
