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
    /// Moonlight Glass panel over the blur, and the game goes on. It is 07's hold (a grid of slots, each with its
    /// material's icon and units out of its capacity, empty slots as faint outlines, the waiting bundles) with one
    /// plain line under it. Beside it is the 07 tab (<see cref="EquipmentPage"/>): 07's sockets and the parts row.
    /// Nothing on it explains what 07 can do (the owner: such notes read as wrong). Opened while
    /// 07 rests on the dock (docs/design/0.5.5-owner-feedback.md §3, ruling B3) it is the transfer view: home's shelves
    /// beside the hold, every slot a button, and one press moves that slot's stack to the other side; leaving the dock
    /// closes it. Everything is written when the screen opens or something changed (<see cref="MarkDirty"/>); an open
    /// screen with nothing new allocates nothing.
    /// </summary>
    internal sealed class CargoScreen
    {
        public const string CellClass = "cargo__cell";
        public const string EmptyClass = "cargo__cell--empty";
        public const string UnlitClass = "cargo__cell--unlit";
        private const string BootingClass = "cargo--booting";
        private const string TransferClass = "cargo--transfer";
        private const string TabOnClass = "settings-tab--on";
        private const string TabSkinOnClass = "settings-tab-wrap--on";
        private const string TabFocusClass = "settings-tab-wrap--focus";
        private const string ButtonClass = "cargo__cell--button";
        private const float CellGap = 6f;
        private const int HoldSlotsAcross = 4;
        private const int WaitingCache = 100;

        private readonly CargoScreenSettings _settings;
        private readonly ILocalization _localization;
        private readonly ICargoHold _hold;
        private readonly IHomeStorage _home;
        private readonly IStackTransfer _transfer;
        private readonly IDockState _dock;
        private readonly EquipmentPage _seven;
        private readonly Button[] _tabs;
        private readonly VisualElement[] _pages;
        private readonly IntText _numbers;
        private readonly EventBus _events;
        private readonly PromptLedger _ledger;
        private readonly HoldBoot _boot;
        private readonly VisualElement _root;
        private readonly Label _bootLine;
        private readonly Reveal _reveal;
        private readonly VisualElement _panel;
        private readonly Label _waiting;
        private readonly Label _transferHint;
        private readonly Label _summary;
        private readonly Grid _holdGrid;
        private readonly Grid _homeGrid;
        private readonly Dictionary<int, string> _pairs = new Dictionary<int, string>();
        private readonly string[] _waitingTexts = new string[WaitingCache];
        private string _bootText = string.Empty;
        private int _bootTyped = -1;
        private int _bootLit;
        private int _bootArrived;
        private bool _transferView;
        private int _tab;
        private bool _dirty = true;
        private float _cell;
        private float _shrink = 1f;
        private Vector2 _fittedFor;
        private float _writtenCell = -1f;
        private int _writtenSummary = -1;

        public CargoScreen(UiLayout layout, CargoScreenSettings settings, ILocalization localization,
            ICargoHold hold, IHomeStorage home, IStackTransfer transfer, IDockState dock, IRoverEquipment equipment,
            IRoverPortrait portrait, GlyphLabels glyphs, IntText numbers, EventBus events, PromptLedger ledger)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _hold = hold ?? throw new ArgumentNullException(nameof(hold));
            _home = home ?? throw new ArgumentNullException(nameof(home));
            _transfer = transfer ?? throw new ArgumentNullException(nameof(transfer));
            _dock = dock ?? throw new ArgumentNullException(nameof(dock));
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
                Require<Button>(root, "button-cargo-tab-hold"),
                Require<Button>(root, "button-cargo-tab-seven"),
            };
            _pages = new[]
            {
                Require<VisualElement>(root, "cargo-page-cargo"),
                Require<VisualElement>(root, "cargo-page-seven"),
            };
            _seven = new EquipmentPage(layout, settings, localization, equipment, portrait, glyphs, events);
            _waiting = Require<Label>(root, "cargo-waiting");
            _bootLine = Require<Label>(root, "cargo-boot");
            _transferHint = Require<Label>(root, "cargo-transfer-hint");
            _summary = Require<Label>(root, "cargo-summary");
            _holdGrid = new Grid(Require<VisualElement>(root, "cargo-hold-grid"), _hold, true);
            _homeGrid = new Grid(Require<VisualElement>(root, "cargo-home-grid"), _home, false);
            for (int i = 0; i < _tabs.Length; i++)
            {
                int tab = i;
                VisualElement skin = _tabs[i].parent;
                new TabPainter(skin);
                _tabs[i].clicked += () => SelectTab(tab);
                _tabs[i].RegisterCallback<FocusInEvent>(_ => skin.AddToClassList(TabFocusClass));
                _tabs[i].RegisterCallback<FocusOutEvent>(_ => skin.RemoveFromClassList(TabFocusClass));
            }

            SelectTab(0);
        }

        /// <summary>The page shown: 0 the hold, 1 the 07 tab.</summary>
        public int Tab => _tab;

        /// <summary>The button to focus when the screen opens (gamepad and keyboard move from it).</summary>
        public Button FirstTab => _tabs[0];

        /// <summary>The 07 tab's page (tests and captures).</summary>
        internal EquipmentPage Seven => _seven;

        /// <summary>True once a part was fitted from the 07 tab: the screen eases away for the install moment.</summary>
        public bool WantsClose => _seven.FitRequested;

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

            _transferHint.style.display = tab == 0 ? StyleKeyword.Null : DisplayStyle.None;
            _summary.style.display = tab == 0 ? StyleKeyword.Null : DisplayStyle.None;
            _dirty = true;
            _seven.MarkDirty();
            _seven.SetShown(tab == 1 && IsOpen);
        }

        /// <summary>True while the screen is up (or easing).</summary>
        public bool IsOpen => _reveal.Target;

        /// <summary>0..1 how far the screen has come, for the blur behind it.</summary>
        public float Openness => _reveal.Visibility;

        /// <summary>True while the first-press boot plays (tests and captures).</summary>
        public bool IsBooting => _boot.IsRunning;

        /// <summary>True when this opening is the docked transfer view (hold beside shelves).</summary>
        public bool IsTransferView => _transferView;

        /// <summary>The slot cells on 07's grid now (tests and captures).</summary>
        public int HoldCellCount => _holdGrid.Cells.Count;

        /// <summary>The slot cells on home's grid now (tests and captures).</summary>
        public int HomeCellCount => _homeGrid.Cells.Count;

        /// <summary>The text under the grids (tests and captures).</summary>
        public string SummaryText => _summary.text;

        /// <summary>The "bundles waiting" note, empty when none wait (tests and captures).</summary>
        public string WaitingText => _waiting.text;

        /// <summary>What a press on the focused slot does, under the grids (tests and captures).</summary>
        public string TransferHintText => _transferHint.text;

        /// <summary>The cell of slot <paramref name="index"/> on 07's side (tests and captures).</summary>
        public Button HoldCell(int index)
        {
            return _holdGrid.Cells[index].Root;
        }

        /// <summary>The cell of slot <paramref name="index"/> on home's side (tests and captures).</summary>
        public Button HomeCell(int index)
        {
            return _homeGrid.Cells[index].Root;
        }

        public void Open()
        {
            if (_reveal.Target)
            {
                return;
            }

            _transferView = _dock.IsDocked;
            _root.EnableInClassList(TransferClass, _transferView);
            _holdGrid.SetInteractive(_transferView);
            _homeGrid.SetInteractive(_transferView);
            _dirty = true;
            _seven.Opened();
            _seven.SetShown(_tab == 1);
            _reveal.Show();
            if (!_ledger.HoldBooted && _tab == 0)
            {
                StartBoot();
            }
        }

        public void Close()
        {
            _reveal.Hide();
            _seven.SetShown(false);
            if (_boot.IsRunning)
            {
                EndBoot(false);
            }
        }

        /// <summary>Puts the keyboard and gamepad focus on 07's first slot, in the transfer view.</summary>
        public void FocusFirst()
        {
            if (_transferView)
            {
                Refresh();
                _holdGrid.Cells[0].Root.Focus();
            }
        }

        /// <summary>The hold or the shelves changed: write the screen again while it is open.</summary>
        public void MarkDirty()
        {
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
        /// <param name="device">The active device, for the glyph of each part's key on the 07 tab.</param>
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

            if (_tab == 1)
            {
                _seven.Tick(device);
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
            List<Cell> cells = _holdGrid.Cells;
            for (int i = 0; i < cells.Count; i++)
            {
                Cell cell = cells[i];
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
            List<Cell> cells = _holdGrid.Cells;
            int typed = Mathf.CeilToInt(_boot.TypedFraction * _bootText.Length);
            if (typed != _bootTyped)
            {
                _bootTyped = typed;
                _bootLine.text = _bootText.Substring(0, typed);
            }

            int lit = _boot.LitCount;
            for (; _bootLit < lit; _bootLit++)
            {
                _events.Publish(new HoldSlotLit(_bootLit, cells.Count));
            }

            int item = 0;
            for (int i = 0; i < cells.Count; i++)
            {
                Cell cell = cells[i];
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
            for (int i = 0; i < cells.Count && _bootArrived < arrived; i++)
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

            int columns = _transferView ? 2 : 1;
            float cell = panelSize.x * _settings.WidthShare / (HoldSlotsAcross * columns) - CellGap;
            return Mathf.Clamp(cell * _shrink, _settings.MinCell, _settings.MaxCell);
        }

        private void Refresh()
        {
            _writtenCell = _cell;
            if (_tab != 0)
            {
                return;
            }

            _holdGrid.Write(this);
            if (_transferView)
            {
                _homeGrid.Write(this);
            }

            int waiting = _hold.WaitingBundles;
            _waiting.style.display = waiting > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            if (waiting > 0)
            {
                _waiting.text = WaitingLine(waiting);
            }

            WriteSummary();
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

        private string Pair(int units, int capacity)
        {
            int key = units * 256 + capacity;
            return _pairs.TryGetValue(key, out string text)
                ? text
                : _pairs[key] = string.Concat(_numbers.Get(units), "/", _numbers.Get(capacity));
        }

        /// <summary>One press on a slot: its stack goes to the other side, as much as fits.</summary>
        private void Press(Grid grid, int index)
        {
            MaterialSlot slot = grid.Source.GetSlot(index);
            if (slot.IsEmpty)
            {
                return;
            }

            SalvageMaterial material = slot.Material;
            if (_transfer.TryMove(material, grid.ToHome, out int moved) && moved > 0)
            {
                _events.Publish(new StackTransferred(material, moved, grid.ToHome));
                _dirty = true;
            }
        }

        private void WriteTransferHint(Grid grid)
        {
            _transferHint.text = _localization.Get(grid.ToHome ? UiKeys.TransferToHome : UiKeys.TransferToTrip);
        }

        private static T Require<T>(VisualElement root, string name) where T : VisualElement
        {
            return root.Q<T>(name) ?? throw new InvalidOperationException(
                $"GameUI.uxml has no {typeof(T).Name} named '{name}' under '{root.name}'.");
        }

        private sealed class Cell
        {
            public Button Root;
            public VisualElement Icon;
            public MaterialIconPainter Painter;
            public Label Units;
        }

        /// <summary>One grid of slots: 07's hold (a press stores at home) or home's shelves (a press takes along).</summary>
        private sealed class Grid
        {
            public readonly List<Cell> Cells = new List<Cell>();
            public readonly IMaterialSlots Source;
            public readonly bool ToHome;
            private readonly VisualElement _container;
            private bool _interactive;
            private CargoScreen _screen;

            public Grid(VisualElement container, IMaterialSlots source, bool toHome)
            {
                _container = container;
                Source = source;
                ToHome = toHome;
            }

            public void SetInteractive(bool interactive)
            {
                _interactive = interactive;
                for (int i = 0; i < Cells.Count; i++)
                {
                    Apply(Cells[i]);
                }
            }

            /// <summary>Grows or trims the cells to the source's slots and writes each one.</summary>
            public void Write(CargoScreen screen)
            {
                _screen = screen;
                int count = Source.SlotCount;
                while (Cells.Count < count)
                {
                    Cells.Add(Build());
                }

                while (Cells.Count > count)
                {
                    Cell last = Cells[Cells.Count - 1];
                    _container.Remove(last.Root);
                    Cells.RemoveAt(Cells.Count - 1);
                }

                _container.style.width = HoldSlotsAcross * (screen._cell + CellGap);
                for (int i = 0; i < count; i++)
                {
                    Cell cell = Cells[i];
                    MaterialSlot slot = Source.GetSlot(i);
                    cell.Root.style.width = screen._cell;
                    cell.Root.style.height = screen._cell;
                    if (slot.IsEmpty)
                    {
                        cell.Root.AddToClassList(EmptyClass);
                        continue;
                    }

                    cell.Root.RemoveFromClassList(EmptyClass);
                    cell.Painter.SetMaterial(slot.Material);
                    cell.Units.text = screen.Pair(slot.Units, slot.Capacity);
                }
            }

            private Cell Build()
            {
                int index = Cells.Count;
                var root = new Button { pickingMode = PickingMode.Ignore, focusable = false };
                root.AddToClassList(CellClass);
                root.AddToClassList(ButtonClass);
                var icon = new VisualElement { pickingMode = PickingMode.Ignore };
                icon.AddToClassList(MaterialSlots.IconClass);
                var painter = new MaterialIconPainter(icon, SalvageMaterial.Metal);
                var units = new Label { pickingMode = PickingMode.Ignore };
                units.AddToClassList("cargo__units");
                root.Add(icon);
                root.Add(units);
                root.clicked += () => _screen.Press(this, index);
                root.RegisterCallback<FocusInEvent>(_ => _screen.WriteTransferHint(this));
                _container.Add(root);
                var cell = new Cell { Root = root, Icon = icon, Painter = painter, Units = units };
                Apply(cell);
                return cell;
            }

            private void Apply(Cell cell)
            {
                cell.Root.pickingMode = _interactive ? PickingMode.Position : PickingMode.Ignore;
                cell.Root.focusable = _interactive;
            }
        }
    }
}
