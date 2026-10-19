using System;
using System.Text;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// The words of 07's readout cards (docs/design/opening-v2.md §10, §2): one system's line with its dotted leader,
    /// the protocol line, the lander's reserve and the story text of a scanned panel. Pure
    /// text work over <see cref="ILocalization"/>, EditMode-tested in English and Vietnamese.
    /// </summary>
    internal sealed class ReadoutText
    {
        public const string ColumnKey = "ui.system.column";
        public const string ReserveKey = "ui.readout.reserve";
        public const string ScanSetKey = "ui.readout.scan_set";
        public const string ScanSetLearnedKey = "ui.readout.scan_set_learned";
        public const string WheelsNoteKey = "ui.system.note.wheels";
        public const string BlueprintsNoteKey = "ui.system.note.blueprints";
        public const string BlueprintOneNoteKey = "ui.system.note.blueprint_one";

        private const char Leader = '·';
        private const string Separator = " · ";

        // A leader keeps a space either side of its dots; a label too long for dots gets two spaces.
        private const int LeaderSpaces = 2;

        private readonly ILocalization _localization;
        private readonly StringBuilder _builder = new StringBuilder(96);

        public ReadoutText(ILocalization localization)
        {
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
        }

        /// <summary>One system's line: <c>CUTTER ········ FAULT · NO IGNITER</c>.</summary>
        public string SystemLine(RoverSystem system, RoverSystemState state, int blueprints)
        {
            string value = _localization.Get(SystemText.StateKey(system, state));
            if (state == RoverSystemState.Ok)
            {
                if (system == RoverSystem.Wheels)
                {
                    value = value + " " + _localization.Get(WheelsNoteKey);
                }
                else if (system == RoverSystem.Assembly)
                {
                    value = value + Separator + (blueprints == 1
                        ? _localization.Get(BlueprintOneNoteKey)
                        : string.Format(_localization.Get(BlueprintsNoteKey), blueprints));
                }
            }

            return LabelLine(_localization.Get(SystemText.NameKey(system)), value);
        }

        public static ReadoutLineStyle StyleOf(RoverSystemState state)
        {
            switch (state)
            {
                case RoverSystemState.Ok:
                    return ReadoutLineStyle.Plain;
                case RoverSystemState.Fault:
                case RoverSystemState.Jammed:
                    return ReadoutLineStyle.Fault;
                default:
                    return ReadoutLineStyle.Dim;
            }
        }

        public void Protocol(ReadoutContent into, ProtocolLine line)
        {
            into.Clear();
            into.Add(_localization.Get(OpeningProtocol.KeyOf(line)), ReadoutLineStyle.Plain);
        }

        public void Reserve(ReadoutContent into, int percent)
        {
            into.Clear();
            into.Add(string.Format(_localization.Get(ReserveKey), percent), ReadoutLineStyle.Plain);
        }

        /// <summary>
        /// The blueprint progress of <paramref name="nameKey"/>'s scan set: <paramref name="step"/> of
        /// <paramref name="of"/>, and learned on the last piece.
        /// </summary>
        public void ScanSet(ReadoutContent into, string nameKey, int step, int of)
        {
            into.Clear();
            string format = _localization.Get(step >= of ? ScanSetLearnedKey : ScanSetKey);
            into.Add(string.Format(format, _localization.Get(nameKey), step, of), ReadoutLineStyle.Plain);
        }

        /// <summary>The story text <paramref name="textKey"/>, one line per line break.</summary>
        public void Story(ReadoutContent into, string textKey)
        {
            into.Clear();
            string[] lines = _localization.Get(textKey).Split('\n');
            foreach (string line in lines)
            {
                into.Add(line, ReadoutLineStyle.Plain);
            }
        }

        private string LabelLine(string label, string value)
        {
            int dots = Column() - LeaderSpaces - label.Length;
            _builder.Clear();
            _builder.Append(label);
            if (dots > 0)
            {
                _builder.Append(' ').Append(Leader, dots).Append(' ');
            }
            else
            {
                _builder.Append("  ");
            }

            return _builder.Append(value).ToString();
        }

        private int Column()
        {
            if (!int.TryParse(_localization.Get(ColumnKey), out int column) || column < LeaderSpaces)
            {
                throw new InvalidOperationException($"'{ColumnKey}' must be the readout's label column width.");
            }

            return column;
        }
    }
}
