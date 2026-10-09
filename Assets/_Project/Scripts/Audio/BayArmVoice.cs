using System;

namespace MoonProject.Audio
{
    /// <summary>
    /// One of the Rover Bay's old gantry arms (M3-14): its servo whirs with how fast its tip moves (silent when still,
    /// a placement jump ignored), and an arm that has really moved gives a soft hydraulic sigh as it comes to rest - a
    /// twitch does not. Allocation-free.
    /// </summary>
    public sealed class BayArmVoice
    {
        private readonly StationAudioTuning _tuning;
        private readonly ServoWhir _servo = new ServoWhir();
        private float _moved;
        private bool _sighDue;

        public BayArmVoice(StationAudioTuning tuning)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
        }

        /// <summary>0 still .. 1 at full speed.</summary>
        public float Amount => _servo.Amount;

        /// <summary>Feeds the tip's speed (m/s) for one frame; returns <see cref="Amount"/>.</summary>
        public float Step(float speed, float deltaTime)
        {
            float rate = speed > _tuning.ArmPlacedSpeed ? 0f : speed;
            float amount = _servo.Step(rate, deltaTime, _tuning.ArmDeadSpeed, _tuning.ArmFullSpeed, _tuning.ArmAttack,
                _tuning.ArmRelease);
            if (amount >= _tuning.SighMoving)
            {
                _moved += deltaTime;
            }
            else if (amount <= _tuning.SighResting)
            {
                _sighDue |= _moved >= _tuning.SighAfter;
                _moved = 0f;
            }

            return amount;
        }

        /// <summary>True once after the arm came to rest from a real move; clears it.</summary>
        public bool TakeSigh()
        {
            bool due = _sighDue;
            _sighDue = false;
            return due;
        }
    }
}
