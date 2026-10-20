using System;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core.Events;

namespace MoonProject.UI
{
    /// <summary>
    /// The call's waveform on 07's face: Lumen's call sign, three short and one long (docs/design/opening-v3-purpose.md
    /// §2), scrolling in from the right on a quiet baseline. A broken call drops out and crackles, a locked one keeps a
    /// little static, and a clear or answered one is steady. USS: --wave-color, --wave-width, --baseline-color.
    /// </summary>
    internal sealed class CallWavePainter : StylePainter
    {
        // The call sign in units: pulses at [0,1), [2,3), [4,5) and the long one at [6,9), then a rest to 13.
        private const float SignUnits = 13f;
        private const float LongStart = 6f;
        private const float LongEnd = 9f;
        private const int ShortPulses = 3;

        // Half the element's height a full pulse reaches, leaving room for static above it.
        private const float PulseHeight = 0.36f;

        private static readonly CustomStyleProperty<Color> WaveColorProperty =
            new CustomStyleProperty<Color>("--wave-color");

        private static readonly CustomStyleProperty<Color> BaselineColorProperty =
            new CustomStyleProperty<Color>("--baseline-color");

        private static readonly CustomStyleProperty<float> WaveWidthProperty =
            new CustomStyleProperty<float>("--wave-width");

        private readonly CallDisplaySettings _settings;
        private Color _waveColor = Color.clear;
        private Color _baselineColor = Color.clear;
        private float _waveWidth;
        private float _time;
        private CallState _state;

        public CallWavePainter(VisualElement element, CallDisplaySettings settings) : base(element)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>The wave moves on: <paramref name="time"/> is the display's own clock (seconds).</summary>
        public void Set(float time, CallState state)
        {
            _time = time;
            _state = state;
            Repaint();
        }

        /// <summary>True while the call sign keys at <paramref name="unit"/> (any unit, wrapping every call).</summary>
        public static bool IsKeyed(float unit)
        {
            float at = unit - Mathf.Floor(unit / SignUnits) * SignUnits;
            if (at >= LongStart)
            {
                return at < LongEnd;
            }

            int whole = (int)at;
            return whole % 2 == 0 && whole / 2 < ShortPulses;
        }

        protected override void ReadStyle(ICustomStyle style)
        {
            style.TryGetValue(WaveColorProperty, out _waveColor);
            style.TryGetValue(BaselineColorProperty, out _baselineColor);
            style.TryGetValue(WaveWidthProperty, out _waveWidth);
        }

        protected override void Paint(MeshGenerationContext context)
        {
            Rect rect = Element.contentRect;
            if (rect.width <= 0f || rect.height <= 0f || _waveWidth <= 0f)
            {
                return;
            }

            Painter2D painter = context.painter2D;
            painter.lineCap = LineCap.Round;
            painter.lineJoin = LineJoin.Round;
            float middle = rect.center.y;
            painter.lineWidth = _waveWidth * 0.5f;
            painter.strokeColor = _baselineColor;
            painter.BeginPath();
            painter.MoveTo(new Vector2(rect.xMin, middle));
            painter.LineTo(new Vector2(rect.xMax, middle));
            painter.Stroke();

            float staticShare = _state == CallState.Broken ? _settings.BrokenStatic
                : _state == CallState.Locked ? _settings.LockedStatic : 0f;
            float dropout = _state == CallState.Broken ? _settings.BrokenDropout : 0f;
            float tick = Mathf.Floor(_time * _settings.StaticRate);
            float now = _time / _settings.UnitSeconds;
            float height = rect.height * PulseHeight;
            int samples = _settings.Samples;
            painter.lineWidth = _waveWidth;
            painter.strokeColor = _waveColor;
            painter.BeginPath();
            for (int i = 0; i < samples; i++)
            {
                float along = i / (samples - 1f);
                float unit = now - (1f - along) * _settings.WindowUnits;
                bool keyed = IsKeyed(unit) && Noise(Mathf.Floor(unit), 0f) >= dropout;
                float carrier = keyed ? Mathf.Sin(unit * _settings.CarrierCycles * 2f * Mathf.PI) : 0f;
                float crackle = staticShare * (Noise(i, tick) * 2f - 1f);
                var point = new Vector2(rect.xMin + along * rect.width, middle - height * (carrier + crackle));
                if (i == 0)
                {
                    painter.MoveTo(point);
                }
                else
                {
                    painter.LineTo(point);
                }
            }

            painter.Stroke();
        }

        /// <summary>A steady pseudo-random value in [0, 1) for a sample and a static tick (no allocation, no state).</summary>
        private static float Noise(float sample, float tick)
        {
            // The common sine hash: large odd factors scatter neighbouring inputs across [0, 1).
            float n = Mathf.Sin(sample * 12.9898f + tick * 78.233f) * 43758.547f;
            return n - Mathf.Floor(n);
        }
    }
}
