namespace MoonProject.UI
{
    /// <summary>
    /// Holds the boot's dark until the first frames have passed: the engine's first frames (scene load, the first
    /// render) are long, so the dark lifts only once frames have come steadily for a moment, never in the middle of a
    /// hitch. Plain logic, ticked once per frame.
    /// </summary>
    internal sealed class BootGate
    {
        private readonly float _steadyMilliseconds;
        private readonly int _steadyFrames;
        private readonly float _minSeconds;
        private readonly float _maxSeconds;
        private float _elapsed;
        private int _steady;

        public BootGate(float steadyMilliseconds, int steadyFrames, float minSeconds, float maxSeconds)
        {
            _steadyMilliseconds = steadyMilliseconds;
            _steadyFrames = steadyFrames;
            _minSeconds = minSeconds;
            _maxSeconds = maxSeconds;
        }

        /// <summary>Adds a frame; true on the frame the dark may lift (and on every one after).</summary>
        public bool Tick(float unscaledDeltaTime)
        {
            _elapsed += unscaledDeltaTime;
            _steady = unscaledDeltaTime * 1000f <= _steadyMilliseconds ? _steady + 1 : 0;
            return _elapsed >= _maxSeconds || (_elapsed >= _minSeconds && _steady >= _steadyFrames);
        }
    }
}
