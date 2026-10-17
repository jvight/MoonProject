using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// The title CRT switching off like an old tube (docs/design/opening-cutscene-research.md Q1; pure,
    /// EditMode-tested): the picture squeezes to a bright line, then the line shrinks to a dot, in one short run.
    /// </summary>
    internal sealed class CrtSwitchOff
    {
        private readonly float _seconds;
        private readonly float _lineShare;
        private readonly float _lineThickness;
        private float _elapsed;

        /// <param name="seconds">The whole run.</param>
        /// <param name="lineShare">The share of the run spent squeezing to the line (the rest shrinks it to a dot).</param>
        /// <param name="lineThickness">The line's height as a share of the screen's.</param>
        public CrtSwitchOff(float seconds, float lineShare, float lineThickness)
        {
            if (seconds <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(seconds), seconds, "The switch-off needs a duration.");
            }

            _seconds = seconds;
            _lineShare = Mathf.Clamp(lineShare, 0.05f, 0.95f);
            _lineThickness = Mathf.Clamp01(lineThickness);
            ScaleX = 1f;
            ScaleY = 1f;
        }

        public bool IsRunning { get; private set; }

        /// <summary>The picture's horizontal scale now (1 until the line stage ends).</summary>
        public float ScaleX { get; private set; }

        /// <summary>The picture's vertical scale now.</summary>
        public float ScaleY { get; private set; }

        public void Start()
        {
            _elapsed = 0f;
            IsRunning = true;
            Evaluate();
        }

        /// <summary>Advances the run; true on the tick it finishes.</summary>
        public bool Tick(float deltaTime)
        {
            if (!IsRunning)
            {
                return false;
            }

            _elapsed += deltaTime;
            if (_elapsed >= _seconds)
            {
                IsRunning = false;
                ScaleX = 0f;
                ScaleY = _lineThickness;
                return true;
            }

            Evaluate();
            return false;
        }

        private void Evaluate()
        {
            float t = Mathf.Clamp01(_elapsed / _seconds);
            if (t < _lineShare)
            {
                float u = t / _lineShare;
                ScaleX = 1f;
                ScaleY = Mathf.Lerp(1f, _lineThickness, u * u);
            }
            else
            {
                float v = (t - _lineShare) / (1f - _lineShare);
                ScaleX = Mathf.Lerp(1f, 0f, v * v);
                ScaleY = _lineThickness;
            }
        }
    }
}
