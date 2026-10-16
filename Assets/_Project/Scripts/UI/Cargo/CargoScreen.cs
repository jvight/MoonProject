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
    /// single fixed-size Variant C CRT panel over the blur, and the game goes on. On the left is 07's hold (a grid of
    /// slots, each with its material's icon and units out of its capacity, empty slots as faint outlines, the stacks
    /// held back where they were cut) with one plain line under it. On the right is 07's live portrait with its fixed
    /// equipment sockets and parts row (<see cref="EquipmentPage"/>). Opened while 07 rests on the dock, the same frame
    /// and panel size shows the transfer view: 07's hold on the left, home's shelves on the right, every slot a button
    /// where one press moves that slot's stack to the other side; leaving the dock closes it. Everything is written when
    /// the screen opens or something changed (<see cref="MarkDirty"/>); an open screen with nothing new allocates nothing.
    /// </summary>
    internal sealed class CargoScreen
    {
        public const string CellClass = "cargo__cell";
        public const string EmptyClass = "cargo__cell--empty";
        public const string UnlitClass = "cargo__cell--unlit";
        public const string PartClass = "cargo__part";
        private const string BootingClass = "cargo--booting";
        private const string TransferClass = "cargo--transfer";
        private const string ButtonClass = "cargo__cell--button";
        private const float CellGap = 6f;
        private const int HoldSlotsAcross = 4;
        private const int HomeSlotsAcross = 8;
        private const int WaitingCache = 100;

        private readonly CargoScreenSettings _settings;
        private readonly ILocalization _localization;
        private readonly ICargoHold _hold;
        private readonly IHomeStorage _home;
        private readonly IStackTransfer _transfer;
        private readonly IDockState _dock;
        private readonly EquipmentPage _seven;
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
        private readonly Dictionary<string, string> _partNames = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly string[] _waitingTexts = new string[WaitingCache];
        private string _bootText = string.Empty;
        private int _bootTyped = -1;
        private int _bootLit;
        private int _bootArrived;
        private bool _transferView;
        private bool _dirty = true;
        private float _cell;
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
            _seven = new EquipmentPage(layout, settings, localization, equipment, portrait, glyphs, events);
            _waiting = Require<Label>(root, "cargo-waiting");
            _bootLine = Require<Label>(root, "cargo-boot");
            _transferHint = Require<Label>(root, "cargo-transfer-hint");
            _summary = Require<Label>(root, "cargo-summary");
            _holdGrid = new Grid(Require<VisualElement>(root, "cargo-hold-grid"), _hold, true, HoldSlotsAcross);
            _homeGrid = new Grid(Require<VisualElement>(root, "cargo-home-grid"), _home, false, HomeSlotsAcross);
            _cell = _settings.MaxCell;
            _seven.SetShown(false);
        }

        /// <summary>The 07 equipment pane on the right (tests and captures).</summary>
        internal EquipmentPage Seven => _seven;

        /// <summary>The fixed-size panel's world bounds (tests and captures).</summary>
        public Rect PanelRect => _panel.worldBound;

        /// <summary>True once a part was fitted from the 07 pane: the screen eases away for the install moment.</summary>
        public bool WantsClose => _seven.FitRequested;

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

        /// <summary>The text under the hold grid (tests and captures).</summary>
        public string SummaryText => _summary.text;

        /// <summary>The "left behind, no room aboard" note, empty when nothing is held back (tests and captures).</summary>
        public string WaitingText => _waiting.text;

        /// <summary>What a press on the focused slot does, under home's grid (tests and captures).</summary>
        public string TransferHintText => _transferHint.text;

        /// <summary>The cell of slot <paramref name="index"/> on 07's side (tests and captures).</summary>
        public Button HoldCell(int index)
        {
            return _holdGrid.Cells[index].Root;
        }

        /// <summary>The story part label shown in slot <paramref name="index"/> on 07's side, or empty (tests).</summary>
        public string HoldPartText(int index)
        {
            Cell cell = _holdGrid.Cells[index];
            return cell.Part.style.display == DisplayStyle.Flex ? cell.Part.text : string.Empty;
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
            _seven.SetShown(!_transferView);
            _reveal.Show();
            if (!_ledger.HoldBooted)
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

        /// <summary>Puts keyboard and gamepad focus on 07's first slot when docked, or the first part/socket otherwise.</summary>
        public void FocusFirst()
        {
            if (_transferView)
            {
                Refresh();
                _holdGrid.Cells[0].Root.Focus();
            }
            else
            {
                _seven.FocusFirst();
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
            _partNames.Clear();
            Array.Clear(_waitingTexts, 0, _waitingTexts.Length);
            _writtenSummary = -1;
            _dirty = true;
            _seven.MarkDirty();
        }

        /// <param name="deltaTime">Unscaled seconds.</param>
        /// <param name="panelSize">The UI panel's size, for clamping the fixed panel frame on narrow screens.</param>
        /// <param name="device">The active device, for the glyph of each part's key on the 07 pane.</param>
        public void Tick(float deltaTime, Vector2 panelSize, InputDeviceKind device)
        {
            float step = Mathf.Min(deltaTime, 0.1f);
            _reveal.Tick(step);
            if (_reveal.IsHidden)
            {
                return;
            }

            bool booting = _boot.IsRunning;
            if (booting)
            {
                _boot.Step(step);
            }

            if (panelSize != _fittedFor || _writtenCell < 0f)
            {
                _fittedFor = panelSize;
                FitPanel(panelSize);
            }

            if (!_transferView)
            {
                _seven.Tick(device);
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
                items += IsSlotEmpty(i) ? 0 : 1;
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
                if (IsSlotEmpty(i))
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
                if (IsSlotEmpty(i))
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

        private bool IsSlotEmpty(int index)
        {
            return _hold.GetSlot(index).IsEmpty && string.IsNullOrEmpty(_hold.GetPart(index));
        }

        /// <summary>An item at <paramref name="progress"/> of its slide: it comes in from the left, fading up.</summary>
        private void SetItem(Cell cell, float progress)
        {
            float eased = 1f - (1f - progress) * (1f - progress);
            Translate slide = new Translate(-_settings.BootSlideDistance * (1f - eased), 0f);
            cell.Icon.style.opacity = eased;
            cell.Units.style.opacity = eased;
            cell.Part.style.opacity = eased;
            cell.Icon.style.translate = slide;
            cell.Part.style.translate = slide;
        }

        /// <summary>
        /// Sets the single fixed panel size for the screen shape (never varying between field, bay, or transfer view).
        /// </summary>
        private void FitPanel(Vector2 panelSize)
        {
            float width = _settings.PanelWidth;
            float height = _settings.PanelHeight;
            if (panelSize.x > 0f && panelSize.y > 0f)
            {
                width = Mathf.Min(width, Mathf.Floor(panelSize.x * _settings.WidthShare));
                height = Mathf.Min(height, Mathf.Floor(panelSize.y * _settings.HeightShare));
            }

            _panel.style.width = width;
            _panel.style.height = height;
            float scale = _settings.PanelWidth > 0f ? width / _settings.PanelWidth : 1f;
            float cell = Mathf.Clamp(Mathf.Floor(_settings.MaxCell * scale), _settings.MinCell, _settings.MaxCell);
            if (Mathf.Abs(cell - _writtenCell) > 0.5f)
            {
                _cell = cell;
                _dirty = true;
            }
        }

        private void Refresh()
        {
            _writtenCell = _cell;
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

        private string PartName(string partId)
        {
            return _partNames.TryGetValue(partId, out string name)
                ? name
                : _partNames[partId] = _localization.Get(UiKeys.PickupName(partId));
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
            public Label Part;
        }

        /// <summary>One grid of slots: 07's hold (a press stores at home) or home's shelves (a press takes along).</summary>
        private sealed class Grid
        {
            public readonly List<Cell> Cells = new List<Cell>();
            public readonly IMaterialSlots Source;
            public readonly bool ToHome;
            private readonly VisualElement _container;
            private readonly int _slotsAcross;
            private bool _interactive;
            private CargoScreen _screen;

            public Grid(VisualElement container, IMaterialSlots source, bool toHome, int slotsAcross)
            {
                _container = container;
                Source = source;
                ToHome = toHome;
                _slotsAcross = slotsAcross;
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

                _container.style.width = _slotsAcross * (screen._cell + CellGap);
                for (int i = 0; i < count; i++)
                {
                    Cell cell = Cells[i];
                    MaterialSlot slot = Source.GetSlot(i);
                    string part = ToHome ? screen._hold.GetPart(i) : string.Empty;
                    cell.Root.style.width = screen._cell;
                    cell.Root.style.height = screen._cell;
                    if (!slot.IsEmpty)
                    {
                        cell.Root.RemoveFromClassList(EmptyClass);
                        cell.Icon.style.display = DisplayStyle.Flex;
                        cell.Units.style.display = DisplayStyle.Flex;
                        cell.Part.style.display = DisplayStyle.None;
                        cell.Painter.SetMaterial(slot.Material);
                        cell.Units.text = screen.Pair(slot.Units, slot.Capacity);
                        continue;
                    }

                    if (!string.IsNullOrEmpty(part))
                    {
                        cell.Root.RemoveFromClassList(EmptyClass);
                        cell.Icon.style.display = DisplayStyle.None;
                        cell.Units.style.display = DisplayStyle.None;
                        cell.Part.style.display = DisplayStyle.Flex;
                        cell.Part.text = screen.PartName(part);
                        continue;
                    }

                    cell.Root.AddToClassList(EmptyClass);
                    cell.Icon.style.display = StyleKeyword.Null;
                    cell.Units.style.display = StyleKeyword.Null;
                    cell.Part.style.display = DisplayStyle.None;
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
                var part = new Label { pickingMode = PickingMode.Ignore };
                part.AddToClassList(PartClass);
                part.style.display = DisplayStyle.None;
                root.Add(icon);
                root.Add(units);
                root.Add(part);
                root.clicked += () => _screen.Press(this, index);
                root.RegisterCallback<FocusInEvent>(_ => _screen.WriteTransferHint(this));
                _container.Add(root);
                var cell = new Cell { Root = root, Icon = icon, Painter = painter, Units = units, Part = part };
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
