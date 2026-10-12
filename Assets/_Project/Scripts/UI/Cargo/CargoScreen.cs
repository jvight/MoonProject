using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Input;

namespace MoonProject.UI
{
    /// <summary>
    /// The cargo screen (docs/features/M3-25 §2.4; the PDA of docs/design/first-hour-qa.md R1): Tab or View opens a
    /// Moonlight Glass panel over the blur, and the game goes on. The first page has 07's hold on the left (a grid of
    /// slots, each with its material's icon and units out of its capacity, empty slots as faint outlines, the waiting
    /// bundles) and home's shelves on the right (a grid per built shelf, a pencil outline for a free socket), with one
    /// plain line under both. The second page, "07 can", lists each ability 07 has (its key, its word, one line) and the
    /// kits decoded but not yet fitted as pencil sheets. Everything is written when the screen opens or something
    /// changed (<see cref="MarkDirty"/>); an open screen with nothing new allocates nothing.
    /// </summary>
    internal sealed class CargoScreen
    {
        public const string CellClass = "cargo__cell";
        public const string EmptyClass = "cargo__cell--empty";
        private const string TabOnClass = "settings-tab--on";
        private const string TabSkinOnClass = "settings-tab-wrap--on";
        private const float CellGap = 6f;
        private const float ColumnGap = 42f;
        private const float ShelfGap = 22f;
        private const int ShelfSlotsAcross = 6;
        private const int HoldSlotsAcross = 4;
        private const int WaitingCache = 100;

        private readonly CargoScreenSettings _settings;
        private readonly ILocalization _localization;
        private readonly ICargoHold _hold;
        private readonly IHomeStorage _home;
        private readonly IRoverAbilities _abilities;
        private readonly ITechTree _tech;
        private readonly GlyphLabels _glyphs;
        private readonly IntText _numbers;
        private readonly Reveal _reveal;
        private readonly VisualElement _panel;
        private readonly Button[] _tabs;
        private readonly VisualElement[] _pages;
        private readonly Label _waiting;
        private readonly VisualElement _holdGrid;
        private readonly VisualElement _shelves;
        private readonly VisualElement _canRows;
        private readonly Label _summary;
        private readonly List<Cell> _holdCells = new List<Cell>();
        private readonly List<Shelf> _shelfBlocks = new List<Shelf>();
        private readonly CanRow[] _rows = new CanRow[AbilityTable.Count];
        private readonly Dictionary<int, string> _pairs = new Dictionary<int, string>();
        private readonly Dictionary<int, string> _shelfNames = new Dictionary<int, string>();
        private readonly string[] _waitingTexts = new string[WaitingCache];
        private int _tab;
        private bool _dirty = true;
        private float _cell;
        private float _writtenCell = -1f;
        private int _writtenSummary = -1;
        private int _writtenHomeShape = -1;
        private InputDeviceKind _device = InputDeviceKind.KeyboardMouse;

        public CargoScreen(UiLayout layout, CargoScreenSettings settings, ILocalization localization,
            ICargoHold hold, IHomeStorage home, IRoverAbilities abilities, ITechTree tech, GlyphLabels glyphs,
            IntText numbers)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _hold = hold ?? throw new ArgumentNullException(nameof(hold));
            _home = home ?? throw new ArgumentNullException(nameof(home));
            _abilities = abilities ?? throw new ArgumentNullException(nameof(abilities));
            _tech = tech ?? throw new ArgumentNullException(nameof(tech));
            _glyphs = glyphs ?? throw new ArgumentNullException(nameof(glyphs));
            _numbers = numbers ?? throw new ArgumentNullException(nameof(numbers));
            VisualElement root = layout.Cargo;
            _reveal = new Reveal(root, settings.Reveal);
            _reveal.Snap(false);
            _panel = Require<VisualElement>(root, "cargo-panel");
            _tabs = new[]
            {
                Require<Button>(root, "button-cargo-tab-cargo"),
                Require<Button>(root, "button-cargo-tab-can"),
            };
            _pages = new[]
            {
                Require<VisualElement>(root, "cargo-page-cargo"),
                Require<VisualElement>(root, "cargo-page-can"),
            };
            _waiting = Require<Label>(root, "cargo-waiting");
            _holdGrid = Require<VisualElement>(root, "cargo-hold-grid");
            _shelves = Require<VisualElement>(root, "cargo-shelves");
            _canRows = Require<VisualElement>(root, "cargo-can-rows");
            _summary = Require<Label>(root, "cargo-summary");
            for (int i = 0; i < _tabs.Length; i++)
            {
                int tab = i;
                VisualElement skin = _tabs[i].parent;
                new TabPainter(skin);
                _tabs[i].clicked += () => SelectTab(tab);
            }

            for (int i = 0; i < _rows.Length; i++)
            {
                _rows[i] = BuildCanRow(_canRows);
            }

            SelectTab(0);
        }

        /// <summary>True while the screen is up (or easing).</summary>
        public bool IsOpen => _reveal.Target;

        /// <summary>0..1 how far the screen has come, for the blur behind it.</summary>
        public float Openness => _reveal.Visibility;

        /// <summary>The page shown: 0 cargo, 1 "07 can".</summary>
        public int Tab => _tab;

        /// <summary>The button to focus when the screen opens (gamepad and keyboard move from it).</summary>
        public Button FirstTab => _tabs[0];

        /// <summary>The slot cells on 07's grid now (tests and captures).</summary>
        public int HoldCellCount => _holdCells.Count;

        /// <summary>Home's shelves as built on screen: built and free sockets together (tests and captures).</summary>
        public int ShelfBlockCount => _shelfBlocks.Count;

        /// <summary>How many rows of the "07 can" page are shown (tests and captures).</summary>
        public int CanRowCount
        {
            get
            {
                int shown = 0;
                for (int i = 0; i < _rows.Length; i++)
                {
                    shown += _rows[i].Shown ? 1 : 0;
                }

                return shown;
            }
        }

        /// <summary>The words of shown row <paramref name="shown"/>: verb, then line (tests and captures).</summary>
        public string CanRowText(int shown)
        {
            int seen = 0;
            for (int i = 0; i < _rows.Length; i++)
            {
                if (!_rows[i].Shown)
                {
                    continue;
                }

                if (seen == shown)
                {
                    return _rows[i].Verb.text + " | " + _rows[i].Line.text;
                }

                seen++;
            }

            throw new ArgumentOutOfRangeException(nameof(shown), shown, "No such shown row.");
        }

        /// <summary>True when shown row <paramref name="shown"/> is a pencil sheet (tests and captures).</summary>
        public bool CanRowIsPencil(int shown)
        {
            int seen = 0;
            for (int i = 0; i < _rows.Length; i++)
            {
                if (!_rows[i].Shown)
                {
                    continue;
                }

                if (seen == shown)
                {
                    return _rows[i].Pencil;
                }

                seen++;
            }

            throw new ArgumentOutOfRangeException(nameof(shown), shown, "No such shown row.");
        }

        /// <summary>The text under both grids (tests and captures).</summary>
        public string SummaryText => _summary.text;

        /// <summary>The "bundles waiting" note, empty when none wait (tests and captures).</summary>
        public string WaitingText => _waiting.text;

        public void Open()
        {
            if (_reveal.Target)
            {
                return;
            }

            _dirty = true;
            _reveal.Show();
        }

        public void Close()
        {
            _reveal.Hide();
        }

        /// <summary>The hold, the shelves or the abilities changed: write the screen again while it is open.</summary>
        public void MarkDirty()
        {
            _dirty = true;
        }

        /// <summary>Shows one of the pages and marks its tab.</summary>
        public void SelectTab(int tab)
        {
            _tab = tab;
            for (int i = 0; i < _pages.Length; i++)
            {
                _pages[i].style.display = i == tab ? DisplayStyle.Flex : DisplayStyle.None;
                _tabs[i].EnableInClassList(TabOnClass, i == tab);
                _tabs[i].parent.EnableInClassList(TabSkinOnClass, i == tab);
            }

            _dirty = true;
        }

        /// <summary>Re-reads every word in the new language.</summary>
        public void Relocalize()
        {
            _pairs.Clear();
            _shelfNames.Clear();
            Array.Clear(_waitingTexts, 0, _waitingTexts.Length);
            _writtenSummary = -1;
            _dirty = true;
        }

        /// <param name="deltaTime">Unscaled seconds.</param>
        /// <param name="panelSize">The UI panel's size, for the cell size.</param>
        /// <param name="device">The active device, for the glyphs on the "07 can" page.</param>
        public void Tick(float deltaTime, Vector2 panelSize, InputDeviceKind device)
        {
            _reveal.Tick(deltaTime);
            if (_reveal.IsHidden)
            {
                return;
            }

            if (device != _device)
            {
                _device = device;
                _dirty = true;
            }

            float cell = FitCell(panelSize);
            if (Mathf.Abs(cell - _writtenCell) > 0.5f)
            {
                _cell = cell;
                _dirty = true;
            }

            if (_dirty)
            {
                _dirty = false;
                Refresh();
            }
        }

        private float FitCell(Vector2 panelSize)
        {
            if (panelSize.x <= 0f)
            {
                return _settings.MaxCell;
            }

            // The widest row of cells: 07's grid, then the two shelves side by side, with the gaps between.
            float cells = HoldSlotsAcross + 2 * ShelfSlotsAcross;
            float gaps = cells * CellGap + ColumnGap + ShelfGap;
            float cell = (panelSize.x * _settings.WidthShare - gaps) / cells;
            return Mathf.Clamp(cell, _settings.MinCell, _settings.MaxCell);
        }

        private void Refresh()
        {
            _writtenCell = _cell;
            if (_tab == 0)
            {
                RefreshHold();
                RefreshHome();
                WriteSummary();
            }
            else
            {
                RefreshAbilities();
                _summary.text = string.Empty;
                _writtenSummary = -1;
            }
        }

        private void RefreshHold()
        {
            int count = _hold.SlotCount;
            Fit(_holdCells, _holdGrid, count);
            Fill(_holdCells, _hold, 0, count);
            _holdGrid.style.width = HoldSlotsAcross * (_cell + CellGap);
            int waiting = _hold.WaitingBundles;
            _waiting.style.display = waiting > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            if (waiting > 0)
            {
                _waiting.text = WaitingLine(waiting);
            }
        }

        private string WaitingLine(int waiting)
        {
            int index = Mathf.Min(waiting, WaitingCache - 1);
            return _waitingTexts[index] ?? (_waitingTexts[index] =
                string.Format(_localization.Get(UiKeys.CargoWaiting), waiting));
        }

        private void RefreshHome()
        {
            int shelves = _home.ShelfCount;
            int shape = shelves * 1000 + Mathf.RoundToInt(_cell);
            for (int i = 0; i < shelves; i++)
            {
                shape = shape * 31 + (_home.GetShelf(i).Built ? _home.GetShelf(i).SlotCount + 1 : 0);
            }

            if (shape != _writtenHomeShape || _shelfBlocks.Count != shelves)
            {
                BuildShelves(shelves);
                _writtenHomeShape = shape;
            }

            for (int i = 0; i < shelves; i++)
            {
                HomeShelf shelf = _home.GetShelf(i);
                Shelf block = _shelfBlocks[i];
                if (!shelf.Built)
                {
                    continue;
                }

                Fill(block.Cells, _home, shelf.FirstSlot, shelf.SlotCount);
                block.Grid.style.width = ShelfSlotsAcross * (_cell + CellGap);
            }

            _shelves.style.width = 2 * (ShelfSlotsAcross * (_cell + CellGap) + ShelfGap);
        }

        private void BuildShelves(int shelves)
        {
            _shelves.Clear();
            _shelfBlocks.Clear();
            for (int i = 0; i < shelves; i++)
            {
                HomeShelf shelf = _home.GetShelf(i);
                var root = new VisualElement { pickingMode = PickingMode.Ignore };
                root.AddToClassList("cargo__shelf");
                var name = new Label { pickingMode = PickingMode.Ignore };
                name.AddToClassList("cargo__shelf-name");
                name.text = ShelfName(i + 1);
                root.Add(name);
                var block = new Shelf { Root = root };
                if (shelf.Built)
                {
                    var grid = new VisualElement { pickingMode = PickingMode.Ignore };
                    grid.AddToClassList("cargo__grid");
                    root.Add(grid);
                    block.Grid = grid;
                    block.Cells = new List<Cell>();
                    Fit(block.Cells, grid, shelf.SlotCount);
                }
                else
                {
                    var pencil = new VisualElement { pickingMode = PickingMode.Ignore };
                    pencil.AddToClassList("cargo__pencil");
                    pencil.style.width = ShelfSlotsAcross * (_cell + CellGap) - CellGap;
                    pencil.style.height = 2 * (_cell + CellGap) - CellGap;
                    var text = new Label(_localization.Get(UiKeys.CargoShelfFree)) { pickingMode = PickingMode.Ignore };
                    text.AddToClassList("cargo__pencil-text");
                    pencil.Add(text);
                    root.Add(pencil);
                }

                _shelves.Add(root);
                _shelfBlocks.Add(block);
            }
        }

        private string ShelfName(int number)
        {
            return _shelfNames.TryGetValue(number, out string text)
                ? text
                : _shelfNames[number] = string.Format(_localization.Get(UiKeys.CargoShelf), number);
        }

        private void WriteSummary()
        {
            int holdUsed = _hold.UsedSlots;
            int holdSlots = _hold.SlotCount;
            int homeUsed = _home.UsedSlots;
            int homeSlots = _home.SlotCount;
            int key = ((holdUsed * 97 + holdSlots) * 97 + homeUsed) * 97 + homeSlots;
            if (key == _writtenSummary)
            {
                return;
            }

            _writtenSummary = key;
            _summary.text = string.Format(_localization.Get(UiKeys.CargoSummary), holdUsed, holdSlots, homeUsed,
                homeSlots);
        }

        /// <summary>Grows or trims <paramref name="cells"/> to <paramref name="count"/> cells in <paramref name="grid"/>.</summary>
        private void Fit(List<Cell> cells, VisualElement grid, int count)
        {
            while (cells.Count < count)
            {
                cells.Add(BuildCell(grid));
            }

            while (cells.Count > count)
            {
                Cell last = cells[cells.Count - 1];
                grid.Remove(last.Root);
                cells.RemoveAt(cells.Count - 1);
            }
        }

        private void Fill(List<Cell> cells, IMaterialSlots slots, int first, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Cell cell = cells[i];
                MaterialSlot slot = slots.GetSlot(first + i);
                cell.Root.style.width = _cell;
                cell.Root.style.height = _cell;
                if (slot.IsEmpty)
                {
                    cell.Root.AddToClassList(EmptyClass);
                    continue;
                }

                cell.Root.RemoveFromClassList(EmptyClass);
                cell.Painter.SetMaterial(slot.Material);
                cell.Units.text = Pair(slot.Units, slot.Capacity);
            }
        }

        private string Pair(int units, int capacity)
        {
            int key = units * 256 + capacity;
            return _pairs.TryGetValue(key, out string text)
                ? text
                : _pairs[key] = string.Concat(_numbers.Get(units), "/", _numbers.Get(capacity));
        }

        private static Cell BuildCell(VisualElement grid)
        {
            var root = new VisualElement { pickingMode = PickingMode.Ignore };
            root.AddToClassList(CellClass);
            var icon = new VisualElement { pickingMode = PickingMode.Ignore };
            icon.AddToClassList(MaterialSlots.IconClass);
            var painter = new MaterialIconPainter(icon, SalvageMaterial.Metal);
            var units = new Label { pickingMode = PickingMode.Ignore };
            units.AddToClassList("cargo__units");
            root.Add(icon);
            root.Add(units);
            grid.Add(root);
            return new Cell { Root = root, Painter = painter, Units = units };
        }

        private void RefreshAbilities()
        {
            for (int i = 0; i < _rows.Length; i++)
            {
                AbilityRow ability = AbilityTable.At(i);
                CanRow row = _rows[i];
                bool has = _abilities.Has(ability.Ability);
                bool sheet = !has && ability.HasNode && _tech.StateOf(ability.Node) == TechNodeState.Decoded;
                bool shown = has || sheet;
                if (shown != row.Shown)
                {
                    row.Root.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
                    row.Shown = shown;
                }

                if (!shown)
                {
                    continue;
                }

                row.Pencil = sheet;
                row.Root.EnableInClassList("can-row--pencil", sheet);
                row.Verb.text = _localization.Get(ability.VerbKey);
                row.Line.text = _localization.Get(sheet ? UiKeys.CanDecoded : ability.LineKey);
                row.Glyph.style.visibility = ability.HasAction && has ? Visibility.Visible : Visibility.Hidden;
                if (ability.HasAction)
                {
                    row.GlyphView.Set(_glyphs.For(ability.Action, _device), _device);
                }
            }
        }

        private static CanRow BuildCanRow(VisualElement container)
        {
            var root = new VisualElement { pickingMode = PickingMode.Ignore };
            root.AddToClassList("can-row");
            var holder = new VisualElement { pickingMode = PickingMode.Ignore };
            holder.AddToClassList("can-row__glyph");
            var glyph = new VisualElement { pickingMode = PickingMode.Ignore };
            glyph.AddToClassList("glyph");
            var glyphLabel = new Label { pickingMode = PickingMode.Ignore };
            glyphLabel.AddToClassList("glyph__label");
            glyph.Add(glyphLabel);
            holder.Add(glyph);
            var verb = new Label { pickingMode = PickingMode.Ignore };
            verb.AddToClassList("can-row__verb");
            var line = new Label { pickingMode = PickingMode.Ignore };
            line.AddToClassList("can-row__line");
            root.Add(holder);
            root.Add(verb);
            root.Add(line);
            root.style.display = DisplayStyle.None;
            container.Add(root);
            return new CanRow
            {
                Root = root, Glyph = holder, GlyphView = new GlyphView(glyph, glyphLabel), Verb = verb, Line = line,
            };
        }

        private static T Require<T>(VisualElement root, string name) where T : VisualElement
        {
            return root.Q<T>(name) ?? throw new InvalidOperationException(
                $"GameUI.uxml has no {typeof(T).Name} named '{name}' under '{root.name}'.");
        }

        private sealed class Cell
        {
            public VisualElement Root;
            public MaterialIconPainter Painter;
            public Label Units;
        }

        private sealed class Shelf
        {
            public VisualElement Root;
            public VisualElement Grid;
            public List<Cell> Cells;
        }

        private sealed class CanRow
        {
            public VisualElement Root;
            public VisualElement Glyph;
            public GlyphView GlyphView;
            public Label Verb;
            public Label Line;
            public bool Shown;
            public bool Pencil;
        }
    }
}
