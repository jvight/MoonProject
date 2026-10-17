using System;

namespace MoonProject.UI
{
    /// <summary>How one line of the readout reads: calm by default, warm when a part is broken, dim when it waits.</summary>
    internal enum ReadoutLineStyle
    {
        Plain = 0,
        Fault = 1,
        Dim = 2,
    }

    /// <summary>
    /// The lines of the card on 07's readout (pure data, EditMode-tested): text and style per line, and which system a
    /// line tells about, so a line can be rewritten when its system changes. Built when a card starts or its words
    /// change, never per frame.
    /// </summary>
    internal sealed class ReadoutContent
    {
        public const int MaxLines = 12;
        public const int NoSystem = -1;

        private readonly string[] _text = new string[MaxLines];
        private readonly ReadoutLineStyle[] _style = new ReadoutLineStyle[MaxLines];
        private readonly int[] _system = new int[MaxLines];

        public int Count { get; private set; }

        public string TextAt(int index)
        {
            return _text[index];
        }

        public ReadoutLineStyle StyleAt(int index)
        {
            return _style[index];
        }

        public int SystemAt(int index)
        {
            return _system[index];
        }

        public void Clear()
        {
            Count = 0;
        }

        public void Add(string text, ReadoutLineStyle style, int system = NoSystem)
        {
            if (Count >= MaxLines)
            {
                throw new InvalidOperationException($"A readout card holds at most {MaxLines} lines.");
            }

            _text[Count] = text ?? throw new ArgumentNullException(nameof(text));
            _style[Count] = style;
            _system[Count] = system;
            Count++;
        }

        public void Set(int index, string text, ReadoutLineStyle style)
        {
            _text[index] = text ?? throw new ArgumentNullException(nameof(text));
            _style[index] = style;
        }

        /// <summary>The line telling about <paramref name="system"/>, or -1.</summary>
        public int IndexOfSystem(int system)
        {
            for (int i = 0; i < Count; i++)
            {
                if (_system[i] == system)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
