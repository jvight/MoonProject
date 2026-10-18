using System;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// The log of 07's power-on self-test (pure, EditMode-tested): one line per <see cref="BootCheckStep"/>, typed in
    /// as Rover announces the step. A check prints its name and a running row of dots, then types its result over the
    /// dots when it completes; an information line types whole. Lines whose result is a fault, a jam or a one-way
    /// antenna are alerts (amber on the CRT).
    /// </summary>
    internal sealed class PostLog
    {
        public const int Capacity = (int)BootCheckStep.Begin + 1;
        public const int MaxDots = 3;

        private readonly PostLineSettings _settings;
        private readonly string[] _targets = new string[Capacity];
        private readonly float[] _typed = new float[Capacity];
        private readonly bool[] _checking = new bool[Capacity];
        private readonly bool[] _alert = new bool[Capacity];
        private readonly BootCheckStep[] _steps = new BootCheckStep[Capacity];
        private float _dotClock;

        public PostLog(PostLineSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>Lines printed so far.</summary>
        public int Count { get; private set; }

        /// <summary>True from the first step until <see cref="Finish"/>.</summary>
        public bool IsRunning { get; private set; }

        /// <summary>True once the self-test has ended.</summary>
        public bool IsFinished { get; private set; }

        /// <summary>Seconds since the self-test ended (0 before).</summary>
        public float SinceFinished { get; private set; }

        /// <summary>Clears the log for a new wake.</summary>
        public void Reset()
        {
            Count = 0;
            IsRunning = false;
            IsFinished = false;
            SinceFinished = 0f;
            _dotClock = 0f;
        }

        /// <summary>
        /// Step <paramref name="step"/> began: an information line types <paramref name="text"/> whole; a check types
        /// its name (<paramref name="text"/>) and then runs its dots until <see cref="Completed"/>.
        /// </summary>
        public void Started(BootCheckStep step, string text, bool information)
        {
            int line = IndexOf(step);
            if (line < 0)
            {
                if (Count >= Capacity)
                {
                    return;
                }

                line = Count++;
                _steps[line] = step;
                _typed[line] = 0f;
            }

            IsRunning = true;
            _targets[line] = text ?? string.Empty;
            _checking[line] = !information;
            _alert[line] = false;
        }

        /// <summary>
        /// Step <paramref name="step"/> is done: its line becomes <paramref name="settled"/>, typed on from where it
        /// matches what is already shown, so the name stays and the result types in over the dots.
        /// </summary>
        public void Completed(BootCheckStep step, BootCheckResult result, string settled)
        {
            int line = IndexOf(step);
            if (line < 0)
            {
                Started(step, settled, true);
                line = IndexOf(step);
            }

            string before = _targets[line];
            string after = settled ?? string.Empty;
            int common = 0;
            int shown = (int)_typed[line];
            while (common < before.Length && common < after.Length && common < shown && before[common] == after[common])
            {
                common++;
            }

            _targets[line] = after;
            _typed[line] = common;
            _checking[line] = false;
            _alert[line] = IsAlert(result);
        }

        /// <summary>The self-test is over: control is the player's.</summary>
        public void Finish()
        {
            IsRunning = false;
            IsFinished = Count > 0;
        }

        /// <summary>Types the lines on; returns true when anything visible changed.</summary>
        public bool Tick(float deltaTime)
        {
            bool changed = false;
            float step = _settings.CharsPerSecond * deltaTime;
            for (int i = 0; i < Count; i++)
            {
                int before = (int)_typed[i];
                _typed[i] = Math.Min(_targets[i].Length, _typed[i] + step);
                changed |= (int)_typed[i] != before;
            }

            int dotsBefore = DotPhase;
            _dotClock += deltaTime;
            changed |= DotPhase != dotsBefore && AnyChecking();
            if (IsFinished)
            {
                SinceFinished += deltaTime;
            }

            return changed;
        }

        /// <summary>The full text line <paramref name="index"/> is typing toward.</summary>
        public string Target(int index) => _targets[index];

        /// <summary>Characters of line <paramref name="index"/> on screen.</summary>
        public int Visible(int index) => (int)_typed[index];

        /// <summary>The step of line <paramref name="index"/>.</summary>
        public BootCheckStep StepOf(int index) => _steps[index];

        /// <summary>True when line <paramref name="index"/> reports a part that does not work.</summary>
        public bool IsAlertLine(int index) => _alert[index];

        /// <summary>Running dots after line <paramref name="index"/>: 0 unless its check is still typing its name out or running.</summary>
        public int DotsOf(int index)
        {
            return _checking[index] && (int)_typed[index] >= _targets[index].Length ? DotPhase : 0;
        }

        /// <summary>True for results that mean the part does not work as it should.</summary>
        public static bool IsAlert(BootCheckResult result)
        {
            return result == BootCheckResult.Fault || result == BootCheckResult.Jammed ||
                   result == BootCheckResult.ReceiveOnly;
        }

        private int DotPhase => (int)(_dotClock / _settings.DotSeconds) % (MaxDots + 1);

        private bool AnyChecking()
        {
            for (int i = 0; i < Count; i++)
            {
                if (_checking[i])
                {
                    return true;
                }
            }

            return false;
        }

        private int IndexOf(BootCheckStep step)
        {
            for (int i = 0; i < Count; i++)
            {
                if (_steps[i] == step)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
