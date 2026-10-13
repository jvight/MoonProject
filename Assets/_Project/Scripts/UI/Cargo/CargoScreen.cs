using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;

namespace MoonProject.UI
{
    /// <summary>
    /// The cargo screen (docs/features/M3-25 §2.4; the PDA of docs/design/first-hour-qa.md R1): Tab or View opens a
    /// Moonlight Glass panel over the blur, and the game goes on. The first page is 07's hold only (a grid of slots,
    /// each with its material's icon and units out of its capacity, empty slots as faint outlines, the waiting
    /// bundles) with one plain line under it; home's shelves belong to the docked storage view, not to Tab. The second page, "07 can", lists each ability 07 has (its key, its word, one line) and the
    /// kits decoded but not yet fitted as pencil sheets. Everything is written when the screen opens or something
    /// changed (<see cref="MarkDirty"/>); an open screen with nothing new allocates nothing.
    /// </summary>
    internal sealed class CargoScreen
    {
        public const string CellClass = "cargo__cell";
        public const string EmptyClass = "cargo__cell--empty";
        public const string UnlitClass = "cargo__cell--unlit";
        private const string BootingClass = "cargo--booting";
        private const string TabOnClass = "settings-tab--on";
        private const string TabSkinOnClass = "settings-tab-wrap--on";
        private const string TabFocusClass = "settings-tab-wrap--focus";
        private const float CellGap = 6f;
        private const int HoldSlotsAcross = 4;
        private const int WaitingCache = 100;

        private readonly CargoScreenSettings _settings;
        private readonly ILocalization _localization;
        private readonly ICargoHold _hold;
        private readonly IRoverAbilities _abilities;
        private readonly ITechTree _tech;
        private readonly GlyphLabels _glyphs;
        private readonly IntText _numbers;
        private readonly EventBus _events;
        private readonly PromptLedger _ledger;
        private readonly HoldBoot _boot;
        private readonly VisualElement _root;
        private readonly Label _bootLine;
        private readonly Reveal _reveal;
        private readonly VisualElement _panel;
        private readonly Button[] _tabs;
        private readonly VisualElement[] _pages;
        private readonly Label _waiting;
        private readonly VisualElement _holdGrid;
        private readonly VisualElement _canRows;
        private readonly Label _summary;
        private readonly List<Cell> _holdCells = new List<Cell>();
        private readonly CanRow[] _rows = new CanRow[AbilityTable.Count];
        private readonly Dictionary<int, string> _pairs = new Dictionary<int, string>();
        private readonly string[] _waitingTexts = new string[WaitingCache];
        private string _bootText = string.Empty;
        private int _bootTyped = -1;
        private int _bootLit;
        private int _bootArrived;
        private int _tab;
        private bool _dirty = true;
        private float _cell;
        private float _shrink = 1f;
        private Vector2 _fittedFor;
        private float _writtenCell = -1f;
        private int _writtenSummary = -1;
        private InputDeviceKind _device = InputDeviceKind.KeyboardMouse;

        public CargoScreen(UiLayout layout, CargoScreenSettings settings, ILocalization localization,
            ICargoHold hold, IRoverAbilities abilities, ITechTree tech, GlyphLabels glyphs, IntText numbers,
            EventBus events, PromptLedger ledger)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _hold = hold ?? throw new ArgumentNullException(nameof(hold));
            _abilities = abilities ?? throw new ArgumentNullException(nameof(abilities));
            _tech = tech ?? throw new ArgumentNullException(nameof(tech));
            _glyphs = glyphs ?? throw new ArgumentNullException(nameof(glyphs));
            _numbers = numbers ?? throw new ArgumentNullException(nameof(numbers));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            _boot = new HoldBoot(settings);
            VisualElement root = layout.Cargo;
            _root = root;
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
            _bootLine = Require<Label>(root, "cargo-boot");
            _holdGrid = Require<VisualElement>(root, "cargo-hold-grid");
            _canRows = Require<VisualElement>(root, "cargo-can-rows");
            _summary = Require<Label>(root, "cargo-summary");
            for (int i = 0; i < _tabs.Length; i++)
            {
                int tab = i;
                VisualElement skin = _tabs[i].parent;
                new TabPainter(skin);
                _tabs[i].clicked += () => SelectTab(tab);
                _tabs[i].RegisterCallback<FocusInEvent>(_ => skin.AddToClassList(TabFocusClass));
                _tabs[i].RegisterCallback<FocusOutEvent>(_ => skin.RemoveFromClassList(TabFocusClass));
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

        /// <summary>True while the first-press boot plays (tests and captures).</summary>
        public bool IsBooting => _boot.IsRunning;

        /// <summary>The slot cells on 07's grid now (tests and captures).</summary>
        public int HoldCellCount => _holdCells.Count;

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
            if (!_ledger.HoldBooted && _tab == 0)
            {
                StartBoot();
            }
        }

        public void Close()
        {
            _reveal.Hide();
            if (_boot.IsRunning)
            {
                EndBoot(false);
            }
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

            bool booting = _boot.IsRunning;
            if (booting)
            {
                _boot.Step(deltaTime);
            }

            if (device != _device)
            {
                _device = device;
                _dirty = true;
            }

            if (panelSize != _fittedFor)
            {
                _fittedFor = panelSize;
                _shrink = 1f;
            }

            float height = _panel.resolvedStyle.height;
            if (!float.IsNaN(height) && panelSize.y > 0f && height > panelSize.y * _settings.HeightShare &&
                _cell > _settings.MinCell + 0.5f)
            {
                _shrink *= _settings.ShrinkStep;
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

            if (booting)
            {
                PlayBootFrame();
            }
        }

        /// <summary>
        /// The first Tab press (docs/design/0.5.5-owner-feedback.md §3): the screen opens dark, one line of system text
        /// types in, the slot outlines light and the items 07 starts with slide in. Once per save.
        /// </summary>
        private void StartBoot()
        {
            int slots = _hold.SlotCount;
            int items = 0;
            for (int i = 0; i < slots; i++)
            {
                items += _hold.GetSlot(i).IsEmpty ? 0 : 1;
            }

            _boot.Start(slots, items);
            _ledger.RecordHoldBooted();
            _bootText = string.Format(_localization.Get(UiKeys.HoldBoot), slots);
            _bootTyped = -1;
            _bootLit = 0;
            _bootArrived = 0;
            _root.AddToClassList(BootingClass);
            _events.Publish(new HoldBootStarted());
            _events.Publish(new RoverHatchCycleRequested(_boot.TotalSeconds + _settings.BootHatchLingerSeconds));
        }

        private void EndBoot(bool completed)
        {
            _root.RemoveFromClassList(BootingClass);
            for (int i = 0; i < _holdCells.Count; i++)
            {
                Cell cell = _holdCells[i];
                cell.Root.RemoveFromClassList(UnlitClass);
                SetItem(cell, 1f);
            }

            _boot.Stop();
            if (completed)
            {
                _events.Publish(new RoverChirpRequested(ChirpMood.Affirm, 1f));
            }
        }

        private void PlayBootFrame()
        {
            int typed = Mathf.CeilToInt(_boot.TypedFraction * _bootText.Length);
            if (typed != _bootTyped)
            {
                _bootTyped = typed;
                _bootLine.text = _bootText.Substring(0, typed);
            }

            int lit = _boot.LitCount;
            for (; _bootLit < lit; _bootLit++)
            {
                _events.Publish(new HoldSlotLit(_bootLit, _holdCells.Count));
            }

            int item = 0;
            for (int i = 0; i < _holdCells.Count; i++)
            {
                Cell cell = _holdCells[i];
                cell.Root.EnableInClassList(UnlitClass, i >= lit);
                if (_hold.GetSlot(i).IsEmpty)
                {
                    continue;
                }

                SetItem(cell, _boot.ItemProgress(item));
                item++;
            }

            int arrived = _boot.ArrivedCount;
            int seen = 0;
            for (int i = 0; i < _holdCells.Count && _bootArrived < arrived; i++)
            {
                if (_hold.GetSlot(i).IsEmpty)
                {
                    continue;
                }

                if (seen == _bootArrived)
                {
                    _events.Publish(new HoldItemArrived(i));
                    _bootArrived++;
                }

                seen++;
            }

            if (!_boot.IsRunning)
            {
                EndBoot(true);
            }
        }

        /// <summary>An item at <paramref name="progress"/> of its slide: it comes in from the left, fading up.</summary>
        private void SetItem(Cell cell, float progress)
        {
            float eased = 1f - (1f - progress) * (1f - progress);
            cell.Icon.style.opacity = eased;
            cell.Units.style.opacity = eased;
            cell.Icon.style.translate = new Translate(-_settings.BootSlideDistance * (1f - eased), 0f);
        }

        private float FitCell(Vector2 panelSize)
        {
            if (panelSize.x <= 0f)
            {
                return _settings.MaxCell;
            }

            float cell = panelSize.x * _settings.WidthShare / HoldSlotsAcross - CellGap;
            return Mathf.Clamp(cell * _shrink, _settings.MinCell, _settings.MaxCell);
        }

        private void Refresh()
        {
            _writtenCell = _cell;
            if (_tab == 0)
            {
                RefreshHold();
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
            Fit(count);
            Fill(count);
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

        private void WriteSummary()
        {
            int holdUsed = _hold.UsedSlots;
            int holdSlots = _hold.SlotCount;
            int key = holdUsed * 97 + holdSlots;
            if (key == _writtenSummary)
            {
                return;
            }

            _writtenSummary = key;
            _summary.text = string.Format(_localization.Get(UiKeys.CargoSummary), holdUsed, holdSlots);
        }

        /// <summary>Grows or trims 07's grid to <paramref name="count"/> cells.</summary>
        private void Fit(int count)
        {
            while (_holdCells.Count < count)
            {
                _holdCells.Add(BuildCell(_holdGrid));
            }

            while (_holdCells.Count > count)
            {
                Cell last = _holdCells[_holdCells.Count - 1];
                _holdGrid.Remove(last.Root);
                _holdCells.RemoveAt(_holdCells.Count - 1);
            }
        }

        private void Fill(int count)
        {
            for (int i = 0; i < count; i++)
            {
                Cell cell = _holdCells[i];
                MaterialSlot slot = _hold.GetSlot(i);
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
            return new Cell { Root = root, Icon = icon, Painter = painter, Units = units };
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
            public VisualElement Icon;
            public MaterialIconPainter Painter;
            public Label Units;
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
