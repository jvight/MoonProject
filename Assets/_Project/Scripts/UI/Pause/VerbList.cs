using System;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Input;

namespace MoonProject.UI
{
    /// <summary>
    /// The pause menu's "07 can" (docs/design/first-hour-v2 6.5): a short list of the verbs the player has used so far,
    /// each with its glyph on the active device, and nothing not yet learned. A reference, never a tutorial; the whole
    /// block stays out of the layout until the first verb is learned. Rows are built once; <see cref="Refresh"/> only
    /// writes what changed.
    /// </summary>
    internal sealed class VerbList
    {
        private const string RowClass = "verb-row";
        private const string WordClass = "verb-row__word";
        private const string GlyphClass = "glyph";
        private const string GlyphSmallClass = "glyph--small";
        private const string GlyphLabelClass = "glyph__label";

        private readonly VisualElement _block;
        private readonly PromptEntry[] _entries;
        private readonly PromptLedger _ledger;
        private readonly GlyphLabels _labels;
        private readonly ILocalization _localization;
        private readonly Row[] _rows;
        private bool _blockShown = true;

        public VerbList(UiLayout layout, PromptSettings settings, PromptLedger ledger, GlyphLabels labels,
            ILocalization localization)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            _block = layout.PauseCan;
            _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            _labels = labels ?? throw new ArgumentNullException(nameof(labels));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _entries = settings.Entries;
            _rows = new Row[_entries.Length];
            layout.PauseCanRows.Clear();
            for (int i = 0; i < _rows.Length; i++)
            {
                _rows[i] = BuildRow(layout.PauseCanRows);
            }

            SetBlockShown(false);
        }

        /// <summary>How many verbs the list shows now (tests and captures).</summary>
        public int ShownCount { get; private set; }

        /// <summary>The label texts of the shown rows, in order: the glyph and the word (tests and captures).</summary>
        public string RowText(int shown)
        {
            int seen = 0;
            foreach (Row row in _rows)
            {
                if (!row.Shown)
                {
                    continue;
                }

                if (seen == shown)
                {
                    return row.Glyph.text + " " + row.Word.text;
                }

                seen++;
            }

            throw new ArgumentOutOfRangeException(nameof(shown), shown, "No such shown verb.");
        }

        /// <summary>Shows the verbs used so far, worded and glyphed for the active device.</summary>
        public void Refresh(InputDeviceKind device)
        {
            int shown = 0;
            for (int i = 0; i < _rows.Length; i++)
            {
                PromptEntry entry = _entries[i];
                Row row = _rows[i];
                bool learned = _ledger.Used(entry.Kind) > 0;
                if (learned)
                {
                    row.Word.text = _localization.Get(UiKeys.Hint(entry.Kind));
                    row.Glyph.text = _labels.For(entry.Action, device);
                    row.Glyph.parent.EnableInClassList(GlyphView.PadClass, device == InputDeviceKind.Gamepad);
                    shown++;
                }

                if (learned != row.Shown)
                {
                    row.Root.style.display = learned ? DisplayStyle.Flex : DisplayStyle.None;
                    row.Shown = learned;
                }
            }

            ShownCount = shown;
            SetBlockShown(shown > 0);
        }

        private void SetBlockShown(bool shown)
        {
            if (shown != _blockShown)
            {
                _block.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
                _blockShown = shown;
            }
        }

        private static Row BuildRow(VisualElement container)
        {
            var root = new VisualElement { pickingMode = PickingMode.Ignore };
            root.AddToClassList(RowClass);
            var cap = new VisualElement { pickingMode = PickingMode.Ignore };
            cap.AddToClassList(GlyphClass);
            cap.AddToClassList(GlyphSmallClass);
            var glyph = new Label { pickingMode = PickingMode.Ignore };
            glyph.AddToClassList(GlyphLabelClass);
            cap.Add(glyph);
            var word = new Label { pickingMode = PickingMode.Ignore };
            word.AddToClassList(WordClass);
            root.Add(cap);
            root.Add(word);
            container.Add(root);
            return new Row(root, glyph, word);
        }

        private sealed class Row
        {
            public Row(VisualElement root, Label glyph, Label word)
            {
                Root = root;
                Glyph = glyph;
                Word = word;
            }

            public VisualElement Root { get; }

            public Label Glyph { get; }

            public Label Word { get; }

            public bool Shown { get; set; } = true;
        }
    }
}
