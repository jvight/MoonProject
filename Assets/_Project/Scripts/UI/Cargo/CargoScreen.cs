using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.UI
{
    /// <summary>
    /// The cargo screen (docs/features/M3-25 §2.4; the PDA of docs/design/first-hour-qa.md R1): Tab or View opens a
    /// Moonlight Glass panel over the blur, and the game goes on. It is 07's hold only (a grid of slots, each with its
    /// material's icon and units out of its capacity, empty slots as faint outlines, the waiting bundles) with one
    /// plain line under it; home's shelves belong to the docked storage view, not to Tab. Nothing on it explains what
    /// 07 can do (the owner: such notes read as wrong). Everything is written when the screen opens or something
    /// changed (<see cref="MarkDirty"/>); an open screen with nothing new allocates nothing.
    /// </summary>
    internal sealed class CargoScreen
    {
        public const string CellClass = "cargo__cell";
        public const string EmptyClass = "cargo__cell--empty";
        public const string UnlitClass = "cargo__cell--unlit";
        private const string BootingClass = "cargo--booting";
        private const float CellGap = 6f;
        private const int HoldSlotsAcross = 4;
        private const int WaitingCache = 100;

        private readonly CargoScreenSettings _settings;
        private readonly ILocalization _localization;
        private readonly ICargoHold _hold;
        private readonly IntText _numbers;
        private readonly EventBus _events;
        private readonly PromptLedger _ledger;
        private readonly HoldBoot _boot;
        private readonly VisualElement _root;
        private readonly Label _bootLine;
        private readonly Reveal _reveal;
        private readonly VisualElement _panel;
        private readonly Label _waiting;
        private readonly VisualElement _holdGrid;
        private readonly Label _summary;
        private readonly List<Cell> _holdCells = new List<Cell>();
        private readonly Dictionary<int, string> _pairs = new Dictionary<int, string>();
        private readonly string[] _waitingTexts = new string[WaitingCache];
        private string _bootText = string.Empty;
        private int _bootTyped = -1;
        private int _bootLit;
        private int _bootArrived;
        private bool _dirty = true;
        private float _cell;
        private float _shrink = 1f;
        private Vector2 _fittedFor;
        private float _writtenCell = -1f;
        private int _writtenSummary = -1;

        public CargoScreen(UiLayout layout, CargoScreenSettings settings, ILocalization localization,
            ICargoHold hold, IntText numbers, EventBus events, PromptLedger ledger)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _hold = hold ?? throw new ArgumentNullException(nameof(hold));
            _numbers = numbers ?? throw new ArgumentNullException(nameof(numbers));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            _boot = new HoldBoot(settings);
            VisualElement root = layout.Cargo;
            _root = root;
            _reveal = new Reveal(root, settings.Reveal);
            _reveal.Snap(false);
            _panel = Require<VisualElement>(root, "cargo-panel");
            _waiting = Require<Label>(root, "cargo-waiting");
            _bootLine = Require<Label>(root, "cargo-boot");
            _holdGrid = Require<VisualElement>(root, "cargo-hold-grid");
            _summary = Require<Label>(root, "cargo-summary");
        }

        /// <summary>True while the screen is up (or easing).</summary>
        public bool IsOpen => _reveal.Target;

        /// <summary>0..1 how far the screen has come, for the blur behind it.</summary>
        public float Openness => _reveal.Visibility;

        /// <summary>True while the first-press boot plays (tests and captures).</summary>
        public bool IsBooting => _boot.IsRunning;

        /// <summary>The slot cells on 07's grid now (tests and captures).</summary>
        public int HoldCellCount => _holdCells.Count;

        /// <summary>The text under the grid (tests and captures).</summary>
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
            if (!_ledger.HoldBooted)
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

        /// <summary>The hold changed: write the screen again while it is open.</summary>
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
        public void Tick(float deltaTime, Vector2 panelSize)
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
    }
}
