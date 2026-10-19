using System;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Input;

namespace MoonProject.UI
{
    /// <summary>
    /// 07's databank page on its CRT (docs/design/databank.md §4): the category column with the open category's
    /// entries and their unread dots, the turning model (or a scanned print, leaning a little, with paper grain), the
    /// MAKER · YEAR · ERA row, 07's readout, the set line, the crew's history card, the era strip and the key row. It
    /// writes its labels only when what it shows changes, never per frame.
    /// </summary>
    internal sealed class DatabankView
    {
        public const string RootClass = "databank";
        private const string RowClass = "databank__row";
        private const string EntryRowClass = "databank__row--entry";
        private const string FocusClass = "databank__row--focus";
        private const string DotClass = "databank__dot";
        private const string MarkClass = "databank__mark";
        private const string MarkLitClass = "databank__mark--lit";
        private const int HintCount = 3;
        private const int GrainSize = 256;
        private const byte GrainAlpha = 12;

        private readonly ILocalization _localization;
        private readonly DatabankSettings _settings;
        private readonly VisualElement _root;
        private readonly Label _title;
        private readonly Label[] _rows;
        private readonly VisualElement[] _dots;
        private readonly bool[] _rowIsEntry;
        private readonly int[] _rowCategory;
        private readonly int[] _rowEntry;
        private readonly VisualElement _model;
        private readonly VisualElement _print;
        private readonly VisualElement _grain;
        private readonly Label _empty;
        private readonly Label _fields;
        private readonly Label _readout;
        private readonly Label _setLine;
        private readonly VisualElement _historyBox;
        private readonly Label _history;
        private readonly Label[] _marks = new Label[DatabankIndex.StripEras.Length];
        private readonly Label[] _keys = new Label[HintCount];
        private readonly Label[] _words = new Label[HintCount];
        private readonly VisualElement _footer;
        private readonly Texture2D _grainTexture;
        private InputDeviceKind _footerDevice = (InputDeviceKind)(-1);
        private int _rowCount;
        private bool _shown = true;
        private float _scale = 1f;

        public DatabankView(VisualElement parent, ILocalization localization, DatabankSettings settings)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _root = new VisualElement { name = "databank", pickingMode = PickingMode.Ignore };
            _root.AddToClassList(RootClass);
            _title = AddLabel("databank-title", "cargo__heading");
            int rows = DatabankLayout.VisibleRows;
            _rows = new Label[rows];
            _dots = new VisualElement[rows];
            _rowIsEntry = new bool[rows];
            _rowCategory = new int[rows];
            _rowEntry = new int[rows];
            for (int i = 0; i < rows; i++)
            {
                _rows[i] = AddLabel("databank-row-" + i, RowClass);
                _dots[i] = new VisualElement { name = "databank-dot-" + i, pickingMode = PickingMode.Ignore };
                _dots[i].AddToClassList(DotClass);
                _root.Add(_dots[i]);
            }

            _model = AddBox("databank-model", "databank__model");
            _print = AddBox("databank-print", "databank__print");
            _grain = new VisualElement { name = "databank-grain", pickingMode = PickingMode.Ignore };
            _grain.AddToClassList("databank__grain");
            _print.Add(_grain);
            _grainTexture = MakeGrain();
            _grain.style.backgroundImage = new StyleBackground(_grainTexture);
            _empty = AddLabel("databank-empty", "databank__empty");
            _fields = AddLabel("databank-fields", "databank__fields");
            _readout = AddLabel("databank-readout", "databank__readout");
            _setLine = AddLabel("databank-set", "databank__set");
            _historyBox = AddBox("databank-history-box", "databank__history-box");
            _history = new Label { name = "databank-history", pickingMode = PickingMode.Ignore };
            _history.AddToClassList("databank__history");
            _historyBox.Add(_history);
            for (int i = 0; i < _marks.Length; i++)
            {
                _marks[i] = AddLabel("databank-mark-" + i, MarkClass);
                _marks[i].text = DatabankText.EraMark(DatabankIndex.StripEras[i]);
            }

            _footer = AddBox("databank-footer", "cargo__footer");
            for (int i = 0; i < HintCount; i++)
            {
                var hint = new VisualElement { pickingMode = PickingMode.Ignore };
                hint.AddToClassList("cargo__hint");
                _keys[i] = new Label { name = "databank-key-" + i, pickingMode = PickingMode.Ignore };
                _keys[i].AddToClassList("cargo__key");
                _words[i] = new Label { name = "databank-word-" + i, pickingMode = PickingMode.Ignore };
                _words[i].AddToClassList("cargo__key-word");
                hint.Add(_keys[i]);
                hint.Add(_words[i]);
                _footer.Add(hint);
            }

            parent.Add(_root);
            SetShown(false);
        }

        public VisualElement Root => _root;

        public bool IsShown => _shown;

        /// <summary>The words of list row <paramref name="row"/> (tests).</summary>
        public string RowText(int row) => _rows[row].text;

        public int RowCount => _rowCount;

        public string Fields => _fields.text;

        public string ReadoutText => _readout.text;

        public string SetLineText => _setLine.text;

        public string HistoryText => _history.text;

        /// <summary>True while era mark <paramref name="index"/> (P S B L N) is lit.</summary>
        public bool IsMarkLit(int index) => _marks[index].ClassListContains(MarkLitClass);

        /// <summary>True while the model rect shows the stage, false while it shows a print or nothing.</summary>
        public bool ShowsModel => _model.resolvedStyle.display == DisplayStyle.Flex;

        public bool ShowsPrint => _print.resolvedStyle.display == DisplayStyle.Flex;

        public void SetShown(bool shown)
        {
            if (shown == _shown)
            {
                return;
            }

            _shown = shown;
            _root.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>Lays the page out at <paramref name="scale"/> panel pixels per reference pixel.</summary>
        public void ApplyScale(float scale)
        {
            _scale = scale;
            Position(_title, DatabankLayout.TitleRect);
            Position(_model, DatabankLayout.ModelRect);
            Position(_print, DatabankLayout.ModelRect);
            Position(_empty, DatabankLayout.ModelRect);
            Position(_fields, DatabankLayout.FieldRect);
            Position(_readout, DatabankLayout.ReadoutRect);
            Position(_setLine, DatabankLayout.SetLineRect);
            Position(_historyBox, DatabankLayout.HistoryRect);
            Position(_footer, DatabankLayout.FooterRect);
            for (int i = 0; i < _marks.Length; i++)
            {
                Position(_marks[i], DatabankLayout.StripMarkRect(i));
            }

            for (int i = 0; i < _rows.Length; i++)
            {
                PositionRow(i);
            }
        }

        /// <summary>Writes the title and the key row for <paramref name="device"/>.</summary>
        public void WriteChrome(InputDeviceKind device)
        {
            _title.text = _localization.Get(DatabankText.TitleKey);
            if (device == _footerDevice)
            {
                return;
            }

            _footerDevice = device;
            bool pad = device == InputDeviceKind.Gamepad;
            _keys[0].text = _localization.Get(pad ? UiKeys.KeyShoulders : UiKeys.KeyQ);
            _words[0].text = _localization.Get(DatabankText.HoldPageKey);
            _keys[1].text = _localization.Get(pad ? "glyph.gamepad.buttonnorth" : UiKeys.KeyF);
            _words[1].text = _localization.Get(DatabankText.ColourKey);
            _keys[2].text = _localization.Get(pad ? "glyph.gamepad.select" : "glyph.keyboard.tab");
            _words[2].text = _localization.Get(UiKeys.FaceClose);
        }

        /// <summary>
        /// Rewrites the category column: every category, the open one's entries under it, dots on unread.
        /// </summary>
        public void WriteList(DatabankIndex index, DatabankCursor cursor, IDatabank databank, DatabankText text)
        {
            int row = 0;
            for (int c = 0; c < DatabankIndex.Categories.Length && row < _rows.Length; c++)
            {
                var entries = index.EntriesOf(c);
                WriteRow(row++, false, c, -1, text.Category(DatabankIndex.Categories[c]) + "  " + entries.Count,
                    cursor.Category == c && cursor.Entry < 0, index.HasUnread(c, databank));
                if (c != cursor.Category)
                {
                    continue;
                }

                for (int e = 0; e < entries.Count && row < _rows.Length; e++)
                {
                    string name = databank.TryGetEntry(entries[e], out DatabankEntryInfo info)
                        ? text.Name(info)
                        : entries[e];
                    WriteRow(row++, true, c, e, name, cursor.Entry == e, databank.IsUnread(entries[e]));
                }
            }

            _rowCount = row;
            for (int i = row; i < _rows.Length; i++)
            {
                _rows[i].style.display = DisplayStyle.None;
                _dots[i].style.display = DisplayStyle.None;
            }
        }

        /// <summary>Lights the era strip's marks for the eras the collection has reached.</summary>
        public void WriteStrip(DatabankIndex index)
        {
            for (int i = 0; i < _marks.Length; i++)
            {
                _marks[i].EnableInClassList(MarkLitClass, index.EraFound(DatabankIndex.StripEras[i]));
            }
        }

        /// <summary>Shows <paramref name="entry"/>: its model on the stage texture, or its print.</summary>
        public void WriteEntry(DatabankEntryInfo entry, DatabankProgress progress, DatabankText text, Texture stage,
            bool colour)
        {
            _empty.style.display = DisplayStyle.None;
            bool print = entry.View == DatabankViewKind.Print;
            _model.style.display = print ? DisplayStyle.None : DisplayStyle.Flex;
            _print.style.display = print ? DisplayStyle.Flex : DisplayStyle.None;
            if (print)
            {
                _print.style.backgroundImage = BackgroundOf(entry.Print);
                Position(_print, FitPrint(entry.Print));
                _print.style.unityBackgroundImageTintColor = colour ? Color.white : _settings.PrintAmberTint;
                _print.style.rotate = new Rotate(new Angle(-_settings.PrintSkewDegrees, AngleUnit.Degree));
            }
            else
            {
                _model.style.backgroundImage =
                    new StyleBackground(Background.FromRenderTexture(stage as RenderTexture));
            }

            _fields.text = text.FieldRow(entry);
            _readout.text = text.Readout(entry);
            _setLine.text = text.SetLine(entry, progress);
            string history = text.History(entry, progress);
            _history.text = history;
            _historyBox.style.display = history.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>The archive is empty (it never is in play: 07's own card is seeded).</summary>
        public void WriteEmpty()
        {
            _model.style.display = DisplayStyle.None;
            _print.style.display = DisplayStyle.None;
            _historyBox.style.display = DisplayStyle.None;
            _empty.style.display = DisplayStyle.Flex;
            _empty.text = _localization.Get("db.empty");
            _fields.text = string.Empty;
            _readout.text = string.Empty;
            _setLine.text = string.Empty;
        }

        /// <summary>The list row under <paramref name="refPos"/> (reference pixels), or -1.</summary>
        public int RowAt(Vector2 refPos)
        {
            for (int i = 0; i < _rowCount; i++)
            {
                if (DatabankLayout.RowRect(i, _rowIsEntry[i]).Contains(refPos))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>The category a list row opens.</summary>
        public int RowCategory(int row) => _rowCategory[row];

        /// <summary>The entry a list row shows, or -1 for a category's own row.</summary>
        public int RowEntry(int row) => _rowEntry[row];

        private void WriteRow(int row, bool entry, int category, int entryIndex, string words, bool focus, bool unread)
        {
            _rowIsEntry[row] = entry;
            _rowCategory[row] = category;
            _rowEntry[row] = entryIndex;
            Label label = _rows[row];
            if (!string.Equals(label.text, words, StringComparison.Ordinal))
            {
                label.text = words;
            }

            label.EnableInClassList(EntryRowClass, entry);
            label.EnableInClassList(FocusClass, focus);
            label.style.display = DisplayStyle.Flex;
            _dots[row].style.display = unread ? DisplayStyle.Flex : DisplayStyle.None;
            PositionRow(row);
        }

        private void PositionRow(int row)
        {
            Rect rect = DatabankLayout.RowRect(row, _rowIsEntry[row]);
            Position(_rows[row], rect);
            // Unread rows wear a short bar at their left edge, never a dot: a run of unread rows must not stack into a
            // column of identical dots.
            float inset = DatabankLayout.UnreadBarInset;
            Position(_dots[row], new Rect(rect.x - DatabankLayout.UnreadBarGap, rect.y + inset,
                DatabankLayout.UnreadBarWidth, rect.height - 2f * inset));
        }

        private Label AddLabel(string name, params string[] classes)
        {
            var label = new Label { name = name, pickingMode = PickingMode.Ignore };
            foreach (string c in classes)
            {
                label.AddToClassList(c);
            }

            _root.Add(label);
            return label;
        }

        private VisualElement AddBox(string name, string className)
        {
            var box = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            box.AddToClassList(className);
            _root.Add(box);
            return box;
        }

        private void Position(VisualElement element, Rect rect)
        {
            element.style.left = rect.x * _scale;
            element.style.top = rect.y * _scale;
            element.style.width = rect.width * _scale;
            element.style.height = rect.height * _scale;
        }

        /// <summary>
        /// The print's sheet inside the model rect, its own shape kept, so the grain lies on the paper only.
        /// </summary>
        private static Rect FitPrint(Texture print)
        {
            Rect area = DatabankLayout.ModelRect;
            if (print == null || print.width <= 0 || print.height <= 0)
            {
                return area;
            }

            float aspect = print.width / (float)print.height;
            float width = Mathf.Min(area.width, area.height * aspect);
            float height = width / aspect;
            return new Rect(area.center.x - width * 0.5f, area.center.y - height * 0.5f, width, height);
        }

        private static StyleBackground BackgroundOf(Texture texture)
        {
            switch (texture)
            {
                case Texture2D image:
                    return new StyleBackground(Background.FromTexture2D(image));
                case RenderTexture render:
                    return new StyleBackground(Background.FromRenderTexture(render));
                default:
                    return new StyleBackground(StyleKeyword.None);
            }
        }

        /// <summary>Paper grain for prints: each texel its own faint grey, softly filtered (no dot pattern).</summary>
        private static Texture2D MakeGrain()
        {
            var texture = new Texture2D(GrainSize, GrainSize, TextureFormat.RGBA32, false)
            {
                name = "DatabankGrain",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
            };
            var random = new System.Random(GrainSize);
            var pixels = new Color32[GrainSize * GrainSize];
            for (int i = 0; i < pixels.Length; i++)
            {
                byte v = (byte)random.Next(256);
                pixels[i] = new Color32(v, v, v, (byte)random.Next(GrainAlpha));
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }
}
