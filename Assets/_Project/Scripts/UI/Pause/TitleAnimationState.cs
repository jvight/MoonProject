using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>Evaluated world-space pose and visibility of one rim landmark at time <c>t</c>.</summary>
    public struct TitleLandmarkPose
    {
        public TitleLandmarkKind Kind;
        public float PassTimeSeconds;
        public float Scale;
        public float ThetaRadians;
        public float DeltaFromCrownRadians;
        public Vector2 Position;
        public Vector2 Up;
        public Vector2 Right;
        public float Alpha;
        public float BeaconBlink;
        public bool Visible;
    }

    /// <summary>
    /// Pure, deterministic C# evaluator for the TITLE-SCREEN v1 2D line-art animation at time <c>t</c>:
    /// the 48 s Moon revolution, 07's ground-locked wheel rotation, surface micro-bump suspension bob and pitch,
    /// scoop nod, solar wing tilt, whip antenna sway, Morse <c>· · · —</c> beacon, CRT eye blink, Earth drift,
    /// twinkling stars, and rim landmark positions. Pre-allocates its output buffers once so per-frame evaluation
    /// allocates zero GC.
    /// </summary>
    public sealed class TitleAnimationState
    {
        private readonly TitleSettings _settings;
        private readonly TitleLandmarkPose[] _landmarks;
        private readonly float[] _starTwinkles;

        public TitleAnimationState(TitleSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            int landmarkCount = settings.Landmarks != null ? settings.Landmarks.Length : 0;
            int starCount = settings.Stars != null ? settings.Stars.Length : 0;
            _landmarks = new TitleLandmarkPose[landmarkCount];
            _starTwinkles = new float[starCount];
        }

        public TitleSettings Settings => _settings;

        public float TimeSeconds { get; private set; }

        public bool StandaloneFraming { get; private set; }

        public Vector2 ReferenceSize { get; private set; }

        public Vector2 MoonCenter { get; private set; }

        public float MoonRadius { get; private set; }

        public float RoverScale { get; private set; }

        public float MoonOmega { get; private set; }

        public float MoonRotationRadians { get; private set; }

        public float WheelRadius => _settings.WheelRadius;

        public float GroundSpeed { get; private set; }

        public float WheelOmega { get; private set; }

        public float WheelLinearSpeed => WheelOmega * _settings.WheelRadius;

        public float WheelAngleRadians { get; private set; }

        public float RearSurfaceAngleRadians { get; private set; }

        public float FrontSurfaceAngleRadians { get; private set; }

        public float BumpRear { get; private set; }

        public float BumpFront { get; private set; }

        public float BodyBob { get; private set; }

        public float BodyPitch { get; private set; }

        public float ScoopNod { get; private set; }

        public float SolarTilt { get; private set; }

        public float AntennaSway { get; private set; }

        public float HeadNod { get; private set; }

        public float MorsePhase { get; private set; }

        public bool MorseLit { get; private set; }

        public float MorseIntensity { get; private set; }

        public bool EyeBlinking { get; private set; }

        public bool CursorVisible { get; private set; }

        public Vector2 EarthCenter { get; private set; }

        public float EarthRadius { get; private set; }

        public Vector2 ArkPosition { get; private set; }

        public float ArkPulse { get; private set; }

        public TitleLandmarkPose[] Landmarks => _landmarks;

        public float[] StarTwinkles => _starTwinkles;

        /// <summary>Evaluates all animation poses deterministically for <paramref name="timeSeconds"/>.</summary>
        public void Evaluate(float timeSeconds, bool standaloneFraming = false)
        {
            TimeSeconds = timeSeconds;
            StandaloneFraming = standaloneFraming;

            ReferenceSize = standaloneFraming ? _settings.StandaloneReferenceSize : _settings.MenuReferenceSize;
            MoonCenter = standaloneFraming ? _settings.StandaloneMoonCenter : _settings.MenuMoonCenter;
            MoonRadius = standaloneFraming ? _settings.StandaloneMoonRadius : _settings.MenuMoonRadius;
            RoverScale = standaloneFraming ? _settings.StandaloneRoverScale : _settings.MenuRoverScale;

            float period = _settings.MoonTurnPeriodSeconds;
            MoonOmega = (2f * Mathf.PI) / period;
            float cycleTime = PositiveMod(timeSeconds, period);
            MoonRotationRadians = -MoonOmega * cycleTime;

            // Ground speed is locked to the reference 320 px Moon and 0.35 m wheel so the tire tread never slips.
            GroundSpeed = MoonOmega * _settings.StandaloneMoonRadius;
            WheelOmega = GroundSpeed / _settings.WheelRadius;
            WheelAngleRadians = WheelOmega * timeSeconds;

            float halfSpanAngle = (_settings.WheelbaseHalf * _settings.StandaloneRoverScale) /
                                  _settings.StandaloneMoonRadius;
            RearSurfaceAngleRadians = -Mathf.PI * 0.5f - halfSpanAngle - MoonRotationRadians;
            FrontSurfaceAngleRadians = -Mathf.PI * 0.5f + halfSpanAngle - MoonRotationRadians;
            BumpRear = SampleSurfaceBumpMeters(RearSurfaceAngleRadians, _settings.StandaloneRoverScale);
            BumpFront = SampleSurfaceBumpMeters(FrontSurfaceAngleRadians, _settings.StandaloneRoverScale);

            BodyBob = (BumpRear + BumpFront) * 0.5f + Mathf.Sin(timeSeconds * 7.5f) * 0.008f;
            BodyPitch = (BumpRear - BumpFront) * _settings.BodyPitchFactor +
                        Mathf.Sin(timeSeconds * 5.2f + 0.6f) * 0.006f;
            ScoopNod = Mathf.Sin(timeSeconds * 4.1f) * _settings.ScoopNodAmplitude;
            SolarTilt = Mathf.Sin(timeSeconds * 3.3f + 1.1f) * _settings.SolarTiltAmplitude;
            AntennaSway = Mathf.Sin(timeSeconds * 6.0f + 0.4f) * _settings.AntennaSwayAmplitude +
                          Mathf.Sin(timeSeconds * 13.5f) * (_settings.AntennaSwayAmplitude / 3f);
            HeadNod = _settings.HeadNodBase + Mathf.Sin(timeSeconds * 2.8f) * _settings.HeadNodAmplitude;

            MorsePhase = PositiveMod(timeSeconds, _settings.MorsePeriodSeconds);
            MorseLit = (MorsePhase > 0.20f && MorsePhase < 0.36f) ||
                       (MorsePhase > 0.56f && MorsePhase < 0.72f) ||
                       (MorsePhase > 0.92f && MorsePhase < 1.08f) ||
                       (MorsePhase > 1.32f && MorsePhase < 1.88f);
            MorseIntensity = MorseLit ? 1f : 0.18f;

            float eyePhase = PositiveMod(timeSeconds, _settings.EyeBlinkPeriodSeconds);
            EyeBlinking = eyePhase > _settings.EyeBlinkCloseThreshold;

            float cursorPhase = PositiveMod(timeSeconds, _settings.CursorBlinkPeriodSeconds) /
                                _settings.CursorBlinkPeriodSeconds;
            CursorVisible = cursorPhase < _settings.CursorVisibleDuty;

            Vector2 baseEarth = standaloneFraming ? _settings.StandaloneEarthCenter : _settings.MenuEarthCenter;
            if (standaloneFraming)
            {
                EarthCenter = new Vector2(
                    baseEarth.x + PositiveMod(timeSeconds * 1.6f, 140f),
                    baseEarth.y - Mathf.Sin(timeSeconds * 0.04f) * 6f);
            }
            else
            {
                EarthCenter = new Vector2(
                    baseEarth.x + Mathf.Sin((timeSeconds - 1.35f) * 0.05f) * 6f,
                    baseEarth.y - Mathf.Sin((timeSeconds - 1.35f) * 0.04f) * 4f);
            }

            EarthRadius = standaloneFraming ? _settings.StandaloneEarthRadius : _settings.MenuEarthRadius;
            Vector2 arkOffset = standaloneFraming ? _settings.StandaloneArkOffset : _settings.MenuArkOffset;
            ArkPosition = EarthCenter + arkOffset;
            ArkPulse = 0.72f + 0.28f * Mathf.Sin(timeSeconds * 1.6f);

            EvaluateStars(timeSeconds);
            EvaluateLandmarks(timeSeconds);
        }

        /// <summary>Samples the procedural lunar horizon bump in pixels (at 320px Moon reference) at <paramref name="worldAngle"/>.</summary>
        public static float SampleSurfaceBumpPixels(float worldAngle)
        {
            return Mathf.Sin(worldAngle * 9f) * 1.1f +
                   Mathf.Cos(worldAngle * 17f + 0.8f) * 0.65f +
                   Mathf.Sin(worldAngle * 29f) * 0.3f;
        }

        /// <summary>Samples the procedural lunar horizon bump in rover metres at <paramref name="worldAngle"/>.</summary>
        public static float SampleSurfaceBumpMeters(float worldAngle, float referenceRoverScale)
        {
            return SampleSurfaceBumpPixels(worldAngle) / referenceRoverScale;
        }

        /// <summary>Wraps <paramref name="angleRadians"/> into <c>(-pi, pi]</c>.</summary>
        public static float WrapSignedAngle(float angleRadians)
        {
            return Mathf.Atan2(Mathf.Sin(angleRadians), Mathf.Cos(angleRadians));
        }

        public static float PositiveMod(float value, float period)
        {
            if (period <= 0f)
            {
                return 0f;
            }

            float r = value % period;
            return r < 0f ? r + period : r;
        }

        private void EvaluateStars(float timeSeconds)
        {
            TitleStarEntry[] stars = _settings.Stars;
            if (stars == null)
            {
                return;
            }

            int count = Mathf.Min(stars.Length, _starTwinkles.Length);
            for (int i = 0; i < count; i++)
            {
                TitleStarEntry s = stars[i];
                _starTwinkles[i] = 0.45f + 0.55f * (0.5f + 0.5f * Mathf.Sin(timeSeconds * (2f * Mathf.PI) * s.Frequency + s.Phase));
            }
        }

        private void EvaluateLandmarks(float timeSeconds)
        {
            TitleLandmarkEntry[] entries = _settings.Landmarks;
            if (entries == null)
            {
                return;
            }

            int count = Mathf.Min(entries.Length, _landmarks.Length);
            for (int i = 0; i < count; i++)
            {
                TitleLandmarkEntry entry = entries[i];
                float theta0 = -Mathf.PI * 0.5f + entry.PassTimeSeconds * MoonOmega;
                float theta = theta0 + MoonRotationRadians;
                float dTheta = WrapSignedAngle(theta + Mathf.PI * 0.5f);
                float absD = Mathf.Abs(dTheta);
                bool visible = absD < _settings.LandmarkMaxAngle && absD >= _settings.LandmarkRoverClearAngle;
                float edgeFade = Mathf.Clamp01((1.35f - absD) / _settings.LandmarkEdgeFadeSpan);
                float roverFade = _settings.LandmarkRoverClearAngle > 0f
                    ? Mathf.Clamp01((absD - _settings.LandmarkRoverClearAngle) / _settings.LandmarkRoverFadeSpan)
                    : 1f;
                float cos = Mathf.Cos(theta);
                float sin = Mathf.Sin(theta);
                _landmarks[i] = new TitleLandmarkPose
                {
                    Kind = entry.Kind,
                    PassTimeSeconds = entry.PassTimeSeconds,
                    Scale = entry.Scale,
                    ThetaRadians = theta,
                    DeltaFromCrownRadians = dTheta,
                    Position = new Vector2(MoonCenter.x + cos * (MoonRadius - 1f), MoonCenter.y + sin * (MoonRadius - 1f)),
                    Up = new Vector2(cos, sin),
                    Right = new Vector2(-sin, cos),
                    Alpha = visible ? 0.95f * edgeFade * roverFade : 0f,
                    BeaconBlink = Mathf.Sin(timeSeconds * 5.2f) > 0.55f ? 1f : 0f,
                    Visible = visible,
                };
            }
        }
    }
}
