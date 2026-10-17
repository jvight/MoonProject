using System;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;

namespace MoonProject.UI
{
    /// <summary>
    /// 07's cargo and equipment screen, opened and closed with Tab / View (docs/design/screen-ui.md Round 2 Variant A).
    /// Hosts <see cref="FaceInventoryView"/> (<c>1152x768</c>, 1.5:1) over a soft blur without pausing the game. On the
    /// very first Tab press of a playthrough, <see cref="HoldBoot"/> runs its ~1.2 s terminal boot sequence.
    /// </summary>
    internal sealed class CargoScreen
    {
        public const string CellClass = "cargo__cell";
        public const string EmptyClass = "cargo__cell--empty";
        public const string LockedClass = "cargo__cell--locked";
        public const string FittedClass = "cargo__socket--fitted";
        private const string TransferClass = "cargo--transfer";

        private readonly VisualElement _root;
        private readonly VisualElement _panel;
        private readonly Reveal _reveal;
        private readonly CargoScreenSettings _settings;
        private readonly ILocalization _localization;
        private readonly ICargoHold _hold;
        private readonly IDockState _dock;
        private readonly EventBus _events;
        private readonly PromptLedger _ledger;
        private readonly HoldBoot _boot;
        private readonly FaceInventoryView _view;

        private bool _transferView;
        private bool _dirty = true;
        private string _bootText = string.Empty;
        private int _bootTyped = -1;
        private int _bootLit;
        private int _bootArrived;
        private int _bootSlots;
        private int _bootItems;
        private Vector2 _fittedFor;

        public CargoScreen(UiLayout layout, CargoScreenSettings settings, ILocalization localization,
            ICargoHold hold, IHomeStorage home, IStackTransfer transfer, IDockState dock, IRoverEquipment equipment,
            IRoverPortrait portrait, RoverDiagramData diagram, GlyphLabels glyphs, IntText numbers, EventBus events,
            PromptLedger ledger)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            if (portrait == null)
            {
                throw new ArgumentNullException(nameof(portrait));
            }

            if (glyphs == null)
            {
                throw new ArgumentNullException(nameof(glyphs));
            }

            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _hold = hold ?? throw new ArgumentNullException(nameof(hold));
            _dock = dock ?? throw new ArgumentNullException(nameof(dock));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            _boot = new HoldBoot(settings);

            _root = layout.Cargo;
            _reveal = new Reveal(_root, settings.Reveal);
            _reveal.Snap(false);
            _panel = Require<VisualElement>(_root, "cargo-panel");
            _view = new FaceInventoryView(_panel, settings, localization, hold, home, transfer, dock, equipment,
                diagram, numbers, events);
        }

        /// <summary>True while the screen is up (or easing).</summary>
        public bool IsOpen => _reveal.Target;

        /// <summary>True when this opening is the docked transfer view (hold beside shelves).</summary>
        public bool IsTransferView => _transferView;

        /// <summary>True once a part was fitted from the 07 pane: the screen eases away for the install moment.</summary>
        public bool WantsClose => _view.WantsClose;

        /// <summary>True while the first-press boot plays (tests and captures).</summary>
        public bool IsBooting => _boot.IsRunning;

        /// <summary>0..1 how far the screen has come, for the blur behind it.</summary>
        public float Openness => _reveal.Visibility;

        /// <summary>The hosted <see cref="FaceInventoryView"/> (<c>1152x768</c>, 1.5:1).</summary>
        public FaceInventoryView View => _view;

        /// <summary>Unlocked hold cell count (for tests).</summary>
        public int HoldCellCount => _view.UnlockedCellCount;

        /// <summary>Active home shelf slot count when docked (for tests).</summary>
        public int HomeCellCount => _view.HomeCellCount;

        public VisualElement HoldCell(int index) => _view.HoldCell(index);

        public VisualElement HomeCell(int index) => _view.HomeCell(index);

        public string HoldPartText(int index) => _view.HoldItemName(index);

        public string WaitingText => _view.ToastText;

        public string TransferHintText => _view.TooltipDescText;

        public Rect PanelRect => _panel.worldBound;

        public void Open()
        {
            if (_reveal.Target)
            {
                return;
            }

            _transferView = _dock.IsDocked;
            _root.EnableInClassList(TransferClass, _transferView);
            _view.ResetInteractionState();
            _dirty = true;
            _reveal.Show();
            if (!_ledger.HoldBooted)
            {
                StartBoot();
            }
            else
            {
                _view.SetBootState(false, null, 0, 0, string.Empty, 0);
            }
        }

        public void Close()
        {
            _reveal.Hide();
            _view.ResetInteractionState();
            if (_boot.IsRunning)
            {
                EndBoot(false);
            }
        }

        public void FocusFirst()
        {
            if (_dirty)
            {
                _dirty = false;
                _view.Refresh();
            }

            _view.FocusFirst();
        }

        public void MarkDirty()
        {
            _dirty = true;
        }

        public void Relocalize()
        {
            if (_boot.IsRunning)
            {
                _bootText = string.Format(_localization.Get(UiKeys.HoldBoot), _bootSlots);
                _bootTyped = -1;
            }

            _dirty = true;
        }

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

            if (panelSize.x > 0f && panelSize != _fittedFor)
            {
                _fittedFor = panelSize;
                FitPanel(panelSize);
            }

            if (_dirty)
            {
                _dirty = false;
                _view.Refresh();
            }

            _view.Tick(step);

            if (booting)
            {
                PlayBootFrame();
            }
        }

        private void StartBoot()
        {
            int slots = Mathf.Clamp(_hold.SlotCount, 1, FaceInventoryView.MaxHoldCells);
            int items = 0;
            for (int i = 0; i < slots; i++)
            {
                items += IsSlotEmpty(i) ? 0 : 1;
            }

            _bootSlots = slots;
            _bootItems = items;
            _boot.Start(slots, items);
            _ledger.RecordHoldBooted();
            _bootText = string.Format(_localization.Get(UiKeys.HoldBoot), slots);
            _bootTyped = -1;
            _bootLit = 0;
            _bootArrived = 0;
            _events.Publish(new HoldBootStarted());
            _events.Publish(new RoverHatchCycleRequested(_boot.TotalSeconds + _settings.BootHatchLingerSeconds));
            PlayBootFrame();
        }

        private void EndBoot(bool completed)
        {
            _boot.Stop();
            _view.SetBootState(false, null, 0, 0, string.Empty, 0);
            _view.Refresh();
            if (completed)
            {
                _events.Publish(new RoverChirpRequested(ChirpMood.Affirm, 1f));
                if (_view.FocusedHoldCell < 0 && _view.FocusedSocketIndex < 0 && _view.FocusedHomeCell < 0)
                {
                    _view.FocusFirst();
                }
            }
        }

        private void PlayBootFrame()
        {
            int typed = Mathf.CeilToInt(_boot.TypedFraction * _bootText.Length);
            if (typed != _bootTyped)
            {
                _bootTyped = typed;
            }

            int lit = _boot.LitCount;
            for (; _bootLit < lit; _bootLit++)
            {
                _events.Publish(new HoldSlotLit(_bootLit, _bootSlots));
            }

            int arrived = _boot.ArrivedCount;
            int seen = 0;
            for (int i = 0; i < _bootSlots && _bootArrived < arrived; i++)
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

            _view.SetBootState(true, _boot, lit, _bootItems, _bootText, _bootTyped);

            if (!_boot.IsRunning)
            {
                EndBoot(true);
            }
        }

        private bool IsSlotEmpty(int index)
        {
            return _hold.GetSlot(index).IsEmpty && string.IsNullOrEmpty(_hold.GetPart(index));
        }

        /// <summary>True when hosted on 07's 3:2 face screen so the 1152x768 view maps 1:1 to the CRT render texture.</summary>
        internal bool FillPanelHeight { get; set; }

        private void FitPanel(Vector2 panelSize)
        {
            float maxW = FillPanelHeight ? panelSize.x : Mathf.Min(_settings.PanelWidth, panelSize.x * _settings.WidthShare);
            float maxH = FillPanelHeight ? panelSize.y : Mathf.Min(_settings.PanelHeight, panelSize.y * _settings.HeightShare);
            float scale = Mathf.Min(1f, Mathf.Min(maxW / FaceInventoryView.ReferenceWidth,
                maxH / FaceInventoryView.ReferenceHeight));
            float width = Mathf.Floor(FaceInventoryView.ReferenceWidth * scale);
            float height = Mathf.Floor(FaceInventoryView.ReferenceHeight * scale);
            _panel.style.width = width;
            _panel.style.height = height;
            _panel.style.backgroundColor = FillPanelHeight
                ? new StyleColor(Color.clear)
                : new StyleColor((Color)_settings.PlateBackground);
            _view.ApplyScale(width, height);
        }

        private static T Require<T>(VisualElement root, string name) where T : VisualElement
        {
            return root.Q<T>(name) ?? throw new InvalidOperationException(
                $"GameUI.uxml has no {typeof(T).Name} named '{name}' under '{root.name}'.");
        }
    }
}
