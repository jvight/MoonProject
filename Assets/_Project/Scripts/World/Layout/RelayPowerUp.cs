using UnityEngine;
using MoonProject.Core.Events;

namespace MoonProject.World
{
    /// <summary>
    /// A relay's power-up once its feed unit is fitted (<see cref="RelayPowerStage"/>), as its props see it: how long
    /// ago each stage began, and how far a motion of a given length started at a stage has run. A stage replayed
    /// from a loaded game counts that stage and every one before it as long done, so everything stands at its end.
    /// Plain logic.
    /// </summary>
    public sealed class RelayPowerUp
    {
        private const float NotYet = -1f;

        private readonly float[] _since =
        {
            NotYet, NotYet, NotYet, NotYet, NotYet,
        };

        /// <summary>The stage has begun (this session, or long ago in a loaded one).</summary>
        public bool Reached(RelayStage stage)
        {
            return _since[(int)stage] >= 0f;
        }

        public void OnStage(RelayStage stage, bool restored)
        {
            if (!restored)
            {
                _since[(int)stage] = 0f;
                return;
            }

            for (int i = 0; i <= (int)stage; i++)
            {
                _since[i] = float.MaxValue;
            }
        }

        public void Step(float deltaTime)
        {
            for (int i = 0; i < _since.Length; i++)
            {
                if (_since[i] >= 0f && _since[i] < float.MaxValue)
                {
                    _since[i] += deltaTime;
                }
            }
        }

        /// <summary>How far, 0..1, a motion of <paramref name="seconds"/> begun at <paramref name="stage"/> has run.</summary>
        public float Progress(RelayStage stage, float seconds)
        {
            float since = _since[(int)stage];
            return since < 0f ? 0f : Mathf.Clamp01(since / Mathf.Max(seconds, 1e-3f));
        }

        /// <summary>
        /// A motion eased to rest with a small settle: 0 at <paramref name="t"/> 0, past 1 by
        /// <paramref name="overshoot"/> at <paramref name="settleFrom"/> of the way, then back to exactly 1. C1.
        /// </summary>
        public static float Settle(float t, float overshoot, float settleFrom)
        {
            return SmoothMath.Smootherstep(0f, settleFrom, t) * (1f + overshoot)
                - overshoot * SmoothMath.Smootherstep(settleFrom, 1f, t);
        }
    }
}
