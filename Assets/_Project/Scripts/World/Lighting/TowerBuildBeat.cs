using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// World's answer to the tower coming online (feel-pass §8.2, FeelKind.Build at the tower): a new power stage is
    /// held until the beat's Follow (strength 0), so the light races along the cable and the windows step on after the
    /// flare, never before it; on Impact the top light flares and starts its slow turn. A stage that arrives with no
    /// beat is released after a timeout; a restored stage never waits and never flares. Plain logic.
    /// </summary>
    public sealed class TowerBuildBeat
    {
        // A Follow at or under this strength is the cable race's cue (strength 1 is the reach ring's).
        private const float CableFollowStrength = 0.5f;

        // The flare reaches its peak in this share of its length.
        private const float FlareRiseShare = 0.1f;

        private readonly HomeLightingSettings _settings;
        private float _anticipatedAt = float.NegativeInfinity;
        private float _followedAt = float.NegativeInfinity;
        private float _impactAt = float.NegativeInfinity;
        private float _heldAt = float.NegativeInfinity;
        private BasePowerStage _held;

        public TowerBuildBeat(HomeLightingSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>True once the top light turns: from the first Impact, or a restored stage already lit.</summary>
        public bool Turning { get; private set; }

        /// <summary>True while a new stage waits for the beat's Follow.</summary>
        public bool Holding { get; private set; }

        /// <summary>A beat of the tower's build arrived.</summary>
        public void Beat(FeelPhase phase, float strength, float time)
        {
            switch (phase)
            {
                case FeelPhase.Anticipate:
                    _anticipatedAt = time;
                    break;
                case FeelPhase.Impact:
                    _impactAt = time;
                    Turning = true;
                    break;
                case FeelPhase.Follow when strength <= CableFollowStrength:
                case FeelPhase.Cancel:
                    _followedAt = time;
                    break;
            }
        }

        /// <summary>
        /// A power stage arrived. Returns true when it applies now; false when it is held for the beat's Follow
        /// (take it from <see cref="TryRelease"/>).
        /// </summary>
        public bool Arrive(BasePowerStage stage, bool restored, float time)
        {
            if (restored)
            {
                Holding = false;
                Turning |= stage >= BasePowerStage.Home;
                return true;
            }

            bool followed = _followedAt >= _anticipatedAt && time - _followedAt <= _settings.BuildHoldTimeout;
            if (followed)
            {
                return true;
            }

            Holding = true;
            _held = stage;
            _heldAt = time;
            return false;
        }

        /// <summary>The held stage once the beat's Follow came (or the hold timed out).</summary>
        public bool TryRelease(float time, out BasePowerStage stage)
        {
            stage = _held;
            if (!Holding)
            {
                return false;
            }

            bool followed = _followedAt >= _heldAt;
            float waited = time - Mathf.Max(_heldAt, _anticipatedAt);
            if (!followed && waited < _settings.BuildHoldTimeout)
            {
                return false;
            }

            Holding = false;
            return true;
        }

        /// <summary>The top light's flare at <paramref name="time"/> (0..1): a quick rise, a slow settle.</summary>
        public float Flare(float time)
        {
            float since = time - _impactAt;
            if (since < 0f || since >= _settings.BeaconFlareSeconds)
            {
                return 0f;
            }

            float rise = Mathf.Clamp01(since / (_settings.BeaconFlareSeconds * FlareRiseShare));
            float settle = 1f - Mathf.SmoothStep(0f, 1f, since / _settings.BeaconFlareSeconds);
            return Mathf.Min(rise, settle);
        }

        /// <summary>
        /// How much of the top light faces <paramref name="toViewer"/> (horizontal) at <paramref name="time"/>: the
        /// slow turn's sweep, 1 when not turning.
        /// </summary>
        public float Sweep(Vector3 toViewer, float time)
        {
            if (!Turning)
            {
                return 1f;
            }

            float angle = 2f * Mathf.PI * time / _settings.BeaconTurnPeriod;
            var beam = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
            var view = new Vector2(toViewer.x, toViewer.z);
            float facing = view.sqrMagnitude > 1e-6f ? Mathf.Max(0f, Vector2.Dot(beam, view.normalized)) : 1f;
            float sweep = _settings.BeaconSweep;
            return 1f - sweep + sweep * Mathf.Pow(facing, _settings.BeaconSweepSharpness);
        }
    }
}
