using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// How the databank's model turns (pure, EditMode-tested): slowly by itself while nobody touches it, and by hand
    /// with a drag or the right stick, yaw free and pitch held within its limit (docs/design/databank.md §4). The shown
    /// angles ease toward the hand's, so a turn never snaps.
    /// </summary>
    internal sealed class Turntable
    {
        private const float FullTurn = 360f;

        private readonly DatabankSettings _settings;
        private float _targetYaw;
        private float _targetPitch;
        private float _sinceHand;

        public Turntable(DatabankSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _sinceHand = float.MaxValue;
        }

        /// <summary>Degrees the model is turned about its up axis now.</summary>
        public float Yaw { get; private set; }

        /// <summary>Degrees the model is tipped toward the viewer now (within ± the pitch limit).</summary>
        public float Pitch { get; private set; }

        /// <summary>A new model: it stands at <paramref name="yaw"/>, level, and starts turning by itself.</summary>
        public void Reset(float yaw)
        {
            _targetYaw = yaw;
            _targetPitch = 0f;
            Yaw = yaw;
            Pitch = 0f;
            _sinceHand = float.MaxValue;
        }

        /// <param name="deltaTime">Unscaled seconds; 0 while paused.</param>
        /// <param name="dragPixels">The mouse drag over the model this step (panel pixels; y down).</param>
        /// <param name="stick">The right stick (-1..1; y up).</param>
        public void Tick(float deltaTime, Vector2 dragPixels, Vector2 stick)
        {
            Vector2 hand = dragPixels * _settings.DragDegreesPerPixel +
                           new Vector2(stick.x, -stick.y) * (_settings.StickDegreesPerSecond * deltaTime);
            if (hand.sqrMagnitude > 0f)
            {
                _sinceHand = 0f;
                _targetYaw -= hand.x;
                float limit = _settings.PitchLimitDegrees;
                _targetPitch = Mathf.Clamp(_targetPitch + hand.y, -limit, limit);
            }
            else
            {
                _sinceHand += deltaTime;
                if (_sinceHand >= _settings.IdleResumeSeconds)
                {
                    _targetYaw += _settings.IdleTurnDegreesPerSecond * deltaTime;
                }
            }

            if (Mathf.Abs(_targetYaw) > FullTurn)
            {
                float wrap = Mathf.Sign(_targetYaw) * FullTurn;
                _targetYaw -= wrap;
                Yaw -= wrap;
            }

            float follow = _settings.TurnFollowSeconds <= 0f
                ? 1f
                : 1f - Mathf.Exp(-deltaTime / _settings.TurnFollowSeconds);
            Yaw += (_targetYaw - Yaw) * follow;
            Pitch += (_targetPitch - Pitch) * follow;
        }
    }
}
