using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The cutter turret's short refusal while the cutter is offline (docs/design/opening-v2.md §10): when a press is
    /// refused it shakes its head left-right a couple of times with a small dip, fading out, then stays parked.
    /// Pure and allocation-free; the body language applies <see cref="Yaw"/> and <see cref="Pitch"/> to the turret.
    /// </summary>
    public sealed class CutterRefusal
    {
        private const float TwoPi = Mathf.PI * 2f;

        private readonly CutFocusSettings _settings;
        private float _clock = -1f;

        public CutterRefusal(CutFocusSettings settings)
        {
            _settings = settings ?? throw new System.ArgumentNullException(nameof(settings));
        }

        /// <summary>True while a refusal is under way.</summary>
        public bool IsActive => _clock >= 0f;

        /// <summary>Turret yaw offset (deg).</summary>
        public float Yaw { get; private set; }

        /// <summary>Turret pitch offset (deg, + = down).</summary>
        public float Pitch { get; private set; }

        /// <summary>A refused press: starts the motion over.</summary>
        public void Trigger()
        {
            _clock = 0f;
        }

        public void Step(float deltaTime)
        {
            if (_clock < 0f)
            {
                Yaw = 0f;
                Pitch = 0f;
                return;
            }

            _clock += Mathf.Max(deltaTime, 0f);
            float duration = _settings.RefusalSeconds;
            if (_clock >= duration)
            {
                _clock = -1f;
                Yaw = 0f;
                Pitch = 0f;
                return;
            }

            float t = _clock / duration;
            float fade = (1f - t) * (1f - t);
            Yaw = _settings.RefusalYawDegrees * fade * Mathf.Sin(t * _settings.RefusalShakes * TwoPi);
            Pitch = _settings.RefusalDipDegrees * fade * Mathf.Sin(t * Mathf.PI);
        }
    }
}
