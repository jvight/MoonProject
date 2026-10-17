using System;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;

namespace MoonProject.UI
{
    /// <summary>What occupies one cell in the 4x6 hold grid on <see cref="FaceInventoryView"/>.</summary>
    public enum HoldCellKind
    {
        Empty = 0,
        Locked = 1,
        Material = 2,
        StoryPart = 3,
    }

    /// <summary>
    /// 07's screen on its CRT face (<c>FACE-INV view</c>, <c>docs/design/screen-ui.md</c> Round 2 Variant A): a flat
    /// <c>1152x768</c> (1.5:1) two-pane layout with the 4x6 hold grid (24 cells at max size, locked cells as dotted
    /// outlines with padlocks) on the left, and on the right 07 in side-3/4 line art with its four fixed devices
    /// (<see cref="FaceDevices"/>): the scan lens, the cutter and two empty sockets, each saying its state. While the
    /// spare igniter is in the hold and 07 stands on its pad, the cutter offers it and a confirm fits it.
    /// </summary>
    public sealed class FaceInventoryView
    {
        public const float ReferenceWidth = 1152f;
        public const float ReferenceHeight = 768f;
        public const float ReferenceAspect = ReferenceWidth / ReferenceHeight;

        public const int GridColumns = 4;
        public const int GridRows = 6;
        public const int MaxHoldCells = GridColumns * GridRows;

        public const float HoldGridX = 52f;
        public const float HoldGridY = 74f;
        public const float RightPaneX = 512f;
        public const float CellSize = 90f;
        public const float CellGapX = 12f;
        public const float CellGapY = 6f;
        public const float DividerX = 480f;
        public const float BubbleRadius = 36f;
        public const float RoverOriginX = 804f;
        public const float RoverOriginY = 468f;
        public const float RoverScale = 156f;

        public const int MaxTooltipDescriptionChars = 52;

        // Two-colour amber CRT palette + cyan focus accent read from CargoScreenSettings.
        private Color32 BgCell => _settings.CellBackground;
        private Color32 BgCellFocus => _settings.CellFocusBackground;
        private Color32 AmberBright => _settings.AmberBright;
        private Color32 AmberText => _settings.AmberText;
        private Color32 AmberFaint => _settings.AmberFaint;
        private Color32 AmberMuted => _settings.AmberMuted;
        private Color32 AmberLocked => _settings.AmberLocked;
        private Color32 Cyan => _settings.CyanAccent;

        private readonly VisualElement _panel;
        private readonly VisualElement _canvas;
        private readonly Label _holdTitle;
        private readonly Label _rightTitle;
        private readonly Label _bootLabel;
        private readonly VisualElement _holdGrid;
        private readonly VisualElement _socketsRoot;
        private readonly VisualElement _tooltip;
        private readonly Label _tooltipName;
        private readonly Label _tooltipDesc;
        private readonly VisualElement _toast;
        private readonly Label _toastText;

        private readonly CargoScreenSettings _settings;
        private readonly ILocalization _localization;
        private readonly ICargoHold _hold;
        private readonly IRoverSystems _systems;
        private readonly RoverDiagramData _diagram;
        private readonly IntText _numbers;
        private readonly EventBus _events;

        private readonly VisualElement[] _holdCells = new VisualElement[MaxHoldCells];
        private readonly Label[] _holdBadges = new Label[MaxHoldCells];
        private readonly HoldCellState[] _holdStates = new HoldCellState[MaxHoldCells];

        private readonly VisualElement[] _deviceBubbles = new VisualElement[FaceDevices.Count];
        private readonly Label[] _deviceStates = new Label[FaceDevices.Count];
        private readonly bool[] _deviceOk = new bool[FaceDevices.Count];

        private int _unlockedHoldCount;
        private int _focusedHoldCell = -1;
        private int _focusedDevice = -1;
        private bool _igniterOffered;

        private int _lastPointerDownFrame = -1;
        private int _lastNavigationFrame = -1;
        private readonly InputHandoff _handoff = new InputHandoff();
        private int _lastSubmitFrame = -1;

        private bool _bootRunning;
        private bool _userInteractedDuringBoot;
        private int _bootVisibleSlots;
        private int _bootFilledSlots;
        private HoldBoot _activeBoot;
        private float _scale = 1f;

        internal FaceInventoryView(VisualElement panel, CargoScreenSettings settings, ILocalization localization,
            ICargoHold hold, IRoverSystems systems, RoverDiagramData diagram, IntText numbers, EventBus events)
        {
            _panel = panel ?? throw new ArgumentNullException(nameof(panel));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _hold = hold ?? throw new ArgumentNullException(nameof(hold));
            _systems = systems ?? throw new ArgumentNullException(nameof(systems));
            _diagram = diagram ?? throw new ArgumentNullException(nameof(diagram));
            _numbers = numbers ?? throw new ArgumentNullException(nameof(numbers));
            _events = events ?? throw new ArgumentNullException(nameof(events));

            _panel.pickingMode = PickingMode.Position;
            _canvas = Require<VisualElement>(_panel, "cargo-canvas");
            _holdTitle = Require<Label>(_panel, "cargo-hold-title");
            _rightTitle = Require<Label>(_panel, "cargo-right-title");
            _bootLabel = Require<Label>(_panel, "cargo-boot");
            _holdGrid = Require<VisualElement>(_panel, "cargo-hold-grid");
            _socketsRoot = Require<VisualElement>(_panel, "cargo-sockets");
            _tooltip = Require<VisualElement>(_panel, "cargo-tooltip");
            _tooltipName = Require<Label>(_panel, "cargo-tooltip-name");
            _tooltipDesc = Require<Label>(_panel, "cargo-tooltip-desc");
            _toast = Require<VisualElement>(_panel, "cargo-toast");
            _toastText = Require<Label>(_panel, "cargo-toast-text");

            _canvas.generateVisualContent += OnGenerateCanvas;
            BuildHoldCells();
            BuildDeviceBubbles();

            _panel.RegisterCallback<PointerMoveEvent>(OnPanelPointerMove);
        }

        /// <summary>Unlocked hold cell count (typically 8, 16, or 24).</summary>
        public int UnlockedCellCount => _unlockedHoldCount;

        /// <summary>Locked hold cell count (<c>24 - UnlockedCellCount</c>).</summary>
        public int LockedCellCount => MaxHoldCells - _unlockedHoldCount;

        /// <summary>The currently focused or hovered hold cell index (0..23), or -1.</summary>
        public int FocusedHoldCell => _focusedHoldCell;

        /// <summary>The currently focused or hovered device index (0..3, see <see cref="FaceDevices"/>), or -1.</summary>
        public int FocusedDeviceIndex => _focusedDevice;

        /// <summary>True while the cutter socket offers the spare igniter.</summary>
        public bool IsIgniterOffered => _igniterOffered;

        internal int LastNavigationFrame => _lastNavigationFrame;

        public string LeftHeaderText => _holdTitle.text;

        public string RightHeaderText => _rightTitle.text;

        public bool IsTooltipVisible => _tooltip.style.display == DisplayStyle.Flex;

        public string TooltipNameText => IsTooltipVisible ? _tooltipName.text : string.Empty;

        public string TooltipDescText => IsTooltipVisible ? _tooltipDesc.text : string.Empty;

        public string TooltipText => IsTooltipVisible
            ? (string.IsNullOrEmpty(_tooltipDesc.text) ? _tooltipName.text : _tooltipName.text + " — " + _tooltipDesc.text)
            : string.Empty;

        public bool IsToastVisible => _toast.style.display == DisplayStyle.Flex;

        public string ToastText => IsToastVisible ? _toastText.text : string.Empty;

        public VisualElement HoldCell(int index) => _holdCells[index];

        public Label HoldBadge(int index) => _holdBadges[index];

        public HoldCellKind HoldKind(int index) => _holdStates[index].Kind;

        public VisualElement DeviceElement(int device) => _deviceBubbles[device];

        /// <summary>The state words under device <paramref name="device"/>'s bubble.</summary>
        public string DeviceStateText(int device) => _deviceStates[device].text;

        public Label DeviceStateLabel(int device) => _deviceStates[device];

        /// <summary>Returns the reference <c>1152x768</c> rectangle of hold cell <paramref name="index"/> (0..23).</summary>
        public static Rect GetHoldCellReferenceRect(int index)
        {
            int col = index % GridColumns;
            int row = index / GridColumns;
            return new Rect(
                HoldGridX + col * (CellSize + CellGapX),
                HoldGridY + row * (CellSize + CellGapY),
                CellSize,
                CellSize);
        }

        /// <summary>Localized item or part name for hold cell <paramref name="index"/>, or empty.</summary>
        public string HoldItemName(int index)
        {
            if (index < 0 || index >= MaxHoldCells)
            {
                return string.Empty;
            }

            HoldCellState state = _holdStates[index];
            switch (state.Kind)
            {
                case HoldCellKind.Material:
                    return PartInfo.MaterialName(state.Material, _localization);
                case HoldCellKind.StoryPart:
                    return PartInfo.StoryPartName(state.StoryPartId, _localization);
                default:
                    return string.Empty;
            }
        }

        private bool HideForBoot => _bootRunning && !_userInteractedDuringBoot;

        private void EnsureInteractiveDuringBoot()
        {
            if (_userInteractedDuringBoot)
            {
                return;
            }

            _userInteractedDuringBoot = true;
            if (_bootRunning)
            {
                _bootLabel.style.display = DisplayStyle.None;
                _holdTitle.style.display = DisplayStyle.Flex;
                _rightTitle.style.display = DisplayStyle.Flex;
                for (int i = 0; i < MaxHoldCells; i++)
                {
                    _holdBadges[i].style.opacity = 1f;
                }

                Refresh();
            }
        }

        public void ResetInteractionState()
        {
            _focusedHoldCell = -1;
            _focusedDevice = -1;
            _userInteractedDuringBoot = false;
            _handoff.Reset();
        }

        public void Tick(float deltaTime)
        {
            if (!HideForBoot)
            {
                SyncPendingFocus();
            }

            bool offered = _systems.CanFitIgniter;
            if (offered != _igniterOffered)
            {
                Refresh();
            }
        }

        internal void SetBootState(bool running, HoldBoot boot, int visibleSlots, int filledSlots, string bootTitle,
            int typedLength)
        {
            _bootRunning = running;
            _activeBoot = boot;
            _bootVisibleSlots = visibleSlots;
            _bootFilledSlots = filledSlots;
            if (running && !_userInteractedDuringBoot)
            {
                int clamped = Mathf.Clamp(typedLength, 0, bootTitle.Length);
                _bootLabel.text = clamped >= bootTitle.Length ? bootTitle : bootTitle.Substring(0, clamped);
                _bootLabel.style.display = DisplayStyle.Flex;
                _holdTitle.style.display = DisplayStyle.None;
                _rightTitle.style.display = DisplayStyle.None;
                _socketsRoot.style.display = DisplayStyle.None;
                _tooltip.style.display = DisplayStyle.None;
                for (int i = 0; i < MaxHoldCells; i++)
                {
                    bool showBadge = i < visibleSlots && i < filledSlots && _holdStates[i].Kind == HoldCellKind.Material &&
                                     _holdStates[i].Units > 1 && boot != null && boot.ItemProgress(i) > 0.01f;
                    _holdBadges[i].style.display = showBadge ? DisplayStyle.Flex : DisplayStyle.None;
                    if (showBadge)
                    {
                        _holdBadges[i].style.opacity = boot.ItemProgress(i);
                    }
                }
            }
            else
            {
                _bootLabel.style.display = DisplayStyle.None;
                _holdTitle.style.display = DisplayStyle.Flex;
                _rightTitle.style.display = DisplayStyle.Flex;
                for (int i = 0; i < MaxHoldCells; i++)
                {
                    _holdBadges[i].style.opacity = 1f;
                }
            }

            _canvas.MarkDirtyRepaint();
        }

        public void ApplyScale(float panelWidth, float panelHeight)
        {
            float s = Mathf.Max(0.25f, Mathf.Min(panelWidth / ReferenceWidth, panelHeight / ReferenceHeight));
            _scale = s;

            PositionElement(_holdTitle, 72f * s, 40f * s);
            PositionElement(_rightTitle, RightPaneX * s, 40f * s);
            PositionElement(_bootLabel, 72f * s, 40f * s);

            for (int i = 0; i < MaxHoldCells; i++)
            {
                Rect r = GetHoldCellReferenceRect(i);
                PositionBox(_holdCells[i], r.x * s, r.y * s, r.width * s, r.height * s);
            }

            float bubbleDiam = BubbleRadius * 2f;
            for (int i = 0; i < FaceDevices.Count; i++)
            {
                Vector2 c = FaceDevices.CenterOf(i);
                PositionBox(_deviceBubbles[i], (c.x - BubbleRadius) * s, (c.y - BubbleRadius) * s,
                    bubbleDiam * s, bubbleDiam * s);
            }

            PositionBox(_toast, 76f * s, 688f * s, 1000f * s, 38f * s);
            UpdateTooltip();
            _canvas.MarkDirtyRepaint();
        }

        public void Refresh()
        {
            bool hideForBoot = HideForBoot;
            _holdTitle.text = _localization.Get(UiKeys.CargoHold);
            _rightTitle.text = _localization.Get(UiKeys.CargoTabSeven);

            int baseHoldSlots = Mathf.Clamp(_hold.SlotCount, 0, MaxHoldCells);

            for (int i = 0; i < MaxHoldCells; i++)
            {
                _holdStates[i] = default;
            }

            for (int i = 0; i < baseHoldSlots; i++)
            {
                MaterialSlot slot = _hold.GetSlot(i);
                string partId = _hold.GetPart(i);
                if (!slot.IsEmpty)
                {
                    _diagram.GetIcon(slot.Material);
                    _holdStates[i] = HoldCellState.ForMaterial(slot.Material, slot.Units);
                }
                else if (!string.IsNullOrEmpty(partId))
                {
                    _diagram.GetPartIcon(partId);
                    _holdStates[i] = HoldCellState.ForStoryPart(partId);
                }
                else
                {
                    _holdStates[i] = HoldCellState.ForEmpty();
                }
            }

            _unlockedHoldCount = baseHoldSlots;
            for (int i = _unlockedHoldCount; i < MaxHoldCells; i++)
            {
                _holdStates[i] = HoldCellState.ForLocked();
            }

            for (int i = 0; i < MaxHoldCells; i++)
            {
                HoldCellState state = _holdStates[i];
                bool isUnlocked = i < _unlockedHoldCount;
                _holdCells[i].focusable = isUnlocked;
                _holdCells[i].pickingMode = isUnlocked ? PickingMode.Position : PickingMode.Ignore;
                _holdCells[i].EnableInClassList(CargoScreen.EmptyClass, state.Kind == HoldCellKind.Empty || state.Kind == HoldCellKind.Locked);
                _holdCells[i].EnableInClassList(CargoScreen.LockedClass, state.Kind == HoldCellKind.Locked);

                bool showBadge = !hideForBoot && state.Kind == HoldCellKind.Material && state.Units > 1;
                _holdBadges[i].style.display = showBadge ? DisplayStyle.Flex : DisplayStyle.None;
                if (showBadge)
                {
                    _holdBadges[i].text = _numbers.Get(Mathf.Min(99, state.Units));
                }
            }

            _igniterOffered = _systems.CanFitIgniter;
            _socketsRoot.style.display = !hideForBoot ? DisplayStyle.Flex : DisplayStyle.None;
            for (int i = 0; i < FaceDevices.Count; i++)
            {
                RoverSystem system = FaceDevices.SystemOf(i);
                RoverSystemState state = _systems.StateOf(system);
                _diagram.GetIcon(FaceDevices.MountOf(i));
                _deviceOk[i] = state == RoverSystemState.Ok;
                _deviceStates[i].text = _localization.Get(SystemText.StateKey(system, state));
                _deviceBubbles[i].style.display = !hideForBoot ? DisplayStyle.Flex : DisplayStyle.None;
                _deviceBubbles[i].focusable = true;
                _deviceBubbles[i].EnableInClassList(CargoScreen.FittedClass, _deviceOk[i]);
            }

            UpdateTooltip();
            UpdateToast();
            _canvas.MarkDirtyRepaint();
        }

        public void FocusFirst()
        {
            for (int i = 0; i < _unlockedHoldCount; i++)
            {
                if (_holdStates[i].Kind != HoldCellKind.Empty)
                {
                    FocusHoldCell(i);
                    return;
                }
            }

            if (_unlockedHoldCount > 0)
            {
                FocusHoldCell(0);
                return;
            }

            FocusDevice(0);
        }

        /// <summary>Focuses hold cell <paramref name="index"/> (for tests and captures).</summary>
        public void FocusHoldCell(int index)
        {
            _focusedHoldCell = index;
            _focusedDevice = -1;
            if (index >= 0 && index < MaxHoldCells)
            {
                _holdCells[index].Focus();
            }

            UpdateTooltip();
            _canvas.MarkDirtyRepaint();
        }

        /// <summary>Focuses device <paramref name="device"/> (for tests and captures).</summary>
        public void FocusDevice(int device)
        {
            _focusedDevice = device;
            _focusedHoldCell = -1;
            if (device >= 0 && device < FaceDevices.Count)
            {
                _deviceBubbles[device].Focus();
            }

            UpdateTooltip();
            _canvas.MarkDirtyRepaint();
        }

        /// <summary>Clears any active cell or device focus and hides the tooltip (for resting-state captures).</summary>
        public void ClearFocus()
        {
            _focusedHoldCell = -1;
            _focusedDevice = -1;
            _panel.Blur();
            UpdateTooltip();
            _canvas.MarkDirtyRepaint();
        }

        /// <summary>
        /// Handles a pointer-down at <paramref name="panelPos"/> in panel coordinates (mapped from 07's curved CRT
        /// <c>Screen</c> mesh via <see cref="FaceScreenMapper.ScreenToPanel"/>).
        /// </summary>
        public void PointerDownAtPanel(Vector2 panelPos)
        {
            if (_lastPointerDownFrame == Time.frameCount)
            {
                return;
            }

            _lastPointerDownFrame = Time.frameCount;
            _handoff.NoteMouse();
            HandlePointerDownAtReferencePos(PanelToReference(panelPos));
        }

        /// <summary>
        /// Handles a pointer-move/hover at <paramref name="panelPos"/> in panel coordinates (mapped from 07's curved CRT
        /// <c>Screen</c> mesh via <see cref="FaceScreenMapper.ScreenToPanel"/>).
        /// </summary>
        public void PointerMoveAtPanel(Vector2 panelPos)
        {
            _handoff.NoteMouse();
            HandlePointerMoveAtReferencePos(PanelToReference(panelPos));
        }

        /// <summary>Steps focus in <paramref name="direction"/> across the hold grid and 07's four devices.</summary>
        public bool Navigate(NavigationMoveEvent.Direction direction)
        {
            if (!IsCardinalDirection(direction))
            {
                return false;
            }

            EnsureInteractiveDuringBoot();
            _lastNavigationFrame = Time.frameCount;
            _handoff.NoteNavigation();
            if (_focusedHoldCell >= 0 && _focusedHoldCell < _unlockedHoldCount)
            {
                NavigateFromHoldCell(_focusedHoldCell, direction);
                return true;
            }

            if (_focusedDevice >= 0 && _focusedDevice < FaceDevices.Count)
            {
                NavigateFromDevice(_focusedDevice, direction);
                return true;
            }

            FocusFirst();
            return _focusedHoldCell >= 0 || _focusedDevice >= 0;
        }

        /// <summary>
        /// Submits the focused device (Enter, Space, or Gamepad South / A): the cutter takes the spare igniter when it
        /// offers it. Returns true when a device was submitted.
        /// </summary>
        public bool SubmitFocused()
        {
            if (_lastSubmitFrame == Time.frameCount)
            {
                return false;
            }

            EnsureInteractiveDuringBoot();
            if (_focusedDevice >= 0 && _focusedDevice < FaceDevices.Count)
            {
                OnDeviceSubmit(_focusedDevice);
                return true;
            }

            return false;
        }

        private void SyncPendingFocus()
        {
            Focusable current = _panel.panel?.focusController?.focusedElement;
            if (_focusedHoldCell >= 0 && _focusedHoldCell < _unlockedHoldCount)
            {
                VisualElement target = _holdCells[_focusedHoldCell];
                if (current != target && target.canGrabFocus)
                {
                    target.Focus();
                }
            }
            else if (_focusedDevice >= 0 && _focusedDevice < FaceDevices.Count)
            {
                VisualElement target = _deviceBubbles[_focusedDevice];
                if (current != target && target.canGrabFocus)
                {
                    target.Focus();
                }
            }
        }

        private void BuildHoldCells()
        {
            for (int i = 0; i < MaxHoldCells; i++)
            {
                int index = i;
                var cell = new VisualElement { name = "cargo-hold-" + i };
                cell.AddToClassList(CargoScreen.CellClass);

                var badge = new Label { name = "cargo-hold-badge-" + i, pickingMode = PickingMode.Ignore };
                badge.AddToClassList("cargo__badge");
                cell.Add(badge);

                cell.RegisterCallback<FocusInEvent>(_ => OnHoldCellFocus(index));
                cell.RegisterCallback<PointerEnterEvent>(_ => OnHoldCellHover(index));
                cell.RegisterCallback<PointerDownEvent>(evt => OnHoldCellPointerDown(index, evt));
                cell.RegisterCallback<NavigationMoveEvent>(evt => OnHoldNavigationMove(index, evt));

                _holdCells[i] = cell;
                _holdBadges[i] = badge;
                _holdGrid.Add(cell);
            }
        }

        private void BuildDeviceBubbles()
        {
            for (int i = 0; i < FaceDevices.Count; i++)
            {
                int device = i;
                var bubble = new VisualElement { name = "seven-device-" + i };
                bubble.AddToClassList("cargo__socket-bubble");

                var state = new Label { name = "seven-device-state-" + i, pickingMode = PickingMode.Ignore };
                state.AddToClassList("cargo__device-state");
                bubble.Add(state);

                bubble.RegisterCallback<FocusInEvent>(_ => OnDeviceFocus(device));
                bubble.RegisterCallback<PointerEnterEvent>(_ => OnDeviceHover(device));
                bubble.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button == 0 && _lastPointerDownFrame != Time.frameCount)
                    {
                        _lastPointerDownFrame = Time.frameCount;
                        OnDeviceSubmit(device);
                    }
                });
                bubble.RegisterCallback<NavigationSubmitEvent>(_ =>
                {
                    if (_lastSubmitFrame != Time.frameCount)
                    {
                        OnDeviceSubmit(device);
                    }
                });
                bubble.RegisterCallback<NavigationMoveEvent>(evt => OnDeviceNavigationMove(device, evt));

                _deviceBubbles[i] = bubble;
                _deviceStates[i] = state;
                _socketsRoot.Add(bubble);
            }
        }

        private void OnHoldCellFocus(int index)
        {
            if (index >= _unlockedHoldCount)
            {
                return;
            }

            _focusedHoldCell = index;
            _focusedDevice = -1;
            UpdateTooltip();
            _canvas.MarkDirtyRepaint();
        }

        private void OnHoldCellHover(int index)
        {
            if (index >= _unlockedHoldCount || !_handoff.HoverMaySelect)
            {
                return;
            }

            EnsureInteractiveDuringBoot();
            _focusedHoldCell = index;
            _focusedDevice = -1;
            UpdateTooltip();
            _canvas.MarkDirtyRepaint();
        }

        private void OnDeviceFocus(int device)
        {
            _focusedDevice = device;
            _focusedHoldCell = -1;
            UpdateTooltip();
            _canvas.MarkDirtyRepaint();
        }

        private void OnDeviceHover(int device)
        {
            if (!_handoff.HoverMaySelect)
            {
                return;
            }

            EnsureInteractiveDuringBoot();
            OnDeviceFocus(device);
        }

        private void OnHoldCellPointerDown(int index, PointerDownEvent evt)
        {
            if (evt.button != 0 || index >= _unlockedHoldCount || _lastPointerDownFrame == Time.frameCount)
            {
                return;
            }

            _lastPointerDownFrame = Time.frameCount;
            EnsureInteractiveDuringBoot();
            OnHoldCellFocus(index);
        }

        private void OnPanelPointerMove(PointerMoveEvent evt)
        {
            HandlePointerMoveAtReferencePos(EventToReference(evt));
        }

        private void HandlePointerDownAtReferencePos(Vector2 refPos)
        {
            if (float.IsNaN(refPos.x) || float.IsNaN(refPos.y))
            {
                return;
            }

            int holdIdx = FindHoldCellAtReferencePos(refPos);
            if (holdIdx >= 0)
            {
                EnsureInteractiveDuringBoot();
                OnHoldCellFocus(holdIdx);
                return;
            }

            int device = FindDeviceAtReferencePos(refPos);
            if (device >= 0)
            {
                EnsureInteractiveDuringBoot();
                OnDeviceFocus(device);
                OnDeviceSubmit(device);
            }
        }

        private void HandlePointerMoveAtReferencePos(Vector2 refPos)
        {
            if (float.IsNaN(refPos.x) || float.IsNaN(refPos.y))
            {
                return;
            }

            int holdIdx = FindHoldCellAtReferencePos(refPos);
            if (holdIdx >= 0)
            {
                OnHoldCellHover(holdIdx);
                return;
            }

            int device = FindDeviceAtReferencePos(refPos);
            if (device >= 0)
            {
                OnDeviceHover(device);
            }
        }

        private int FindHoldCellAtReferencePos(Vector2 refPos)
        {
            for (int i = 0; i < _unlockedHoldCount; i++)
            {
                if (GetHoldCellReferenceRect(i).Contains(refPos))
                {
                    return i;
                }
            }

            return -1;
        }

        private static int FindDeviceAtReferencePos(Vector2 refPos)
        {
            float hitRadius = BubbleRadius * 1.45f;
            float bestDistSq = hitRadius * hitRadius;
            int best = -1;
            for (int i = 0; i < FaceDevices.Count; i++)
            {
                float dSq = (FaceDevices.CenterOf(i) - refPos).sqrMagnitude;
                if (dSq <= bestDistSq)
                {
                    bestDistSq = dSq;
                    best = i;
                }
            }

            return best;
        }

        private void OnDeviceSubmit(int device)
        {
            if (device < 0 || device >= FaceDevices.Count)
            {
                return;
            }

            _lastSubmitFrame = Time.frameCount;
            EnsureInteractiveDuringBoot();
            if (FaceDevices.OffersIgniter(device, _systems.CanFitIgniter) && _systems.TryFitIgniter())
            {
                _events.Publish(new UiCue(UiCueKind.Confirm));
                Refresh();
            }
        }

        private static bool IsCardinalDirection(NavigationMoveEvent.Direction direction)
        {
            return direction == NavigationMoveEvent.Direction.Left
                || direction == NavigationMoveEvent.Direction.Right
                || direction == NavigationMoveEvent.Direction.Up
                || direction == NavigationMoveEvent.Direction.Down;
        }

        private void OnHoldNavigationMove(int index, NavigationMoveEvent evt)
        {
            _panel.panel?.focusController?.IgnoreEvent(evt);
            evt.StopPropagation();
            if (!IsCardinalDirection(evt.direction) || _lastNavigationFrame == Time.frameCount)
            {
                return;
            }

            EnsureInteractiveDuringBoot();
            _lastNavigationFrame = Time.frameCount;
            _handoff.NoteNavigation();
            NavigateFromHoldCell(index, evt.direction);
        }

        private void OnDeviceNavigationMove(int device, NavigationMoveEvent evt)
        {
            _panel.panel?.focusController?.IgnoreEvent(evt);
            evt.StopPropagation();
            if (!IsCardinalDirection(evt.direction) || _lastNavigationFrame == Time.frameCount)
            {
                return;
            }

            EnsureInteractiveDuringBoot();
            _lastNavigationFrame = Time.frameCount;
            _handoff.NoteNavigation();
            NavigateFromDevice(device, evt.direction);
        }

        private void NavigateFromHoldCell(int index, NavigationMoveEvent.Direction direction)
        {
            int col = index % GridColumns;
            int row = index / GridColumns;
            switch (direction)
            {
                case NavigationMoveEvent.Direction.Right:
                    if (col + 1 < GridColumns && index + 1 < _unlockedHoldCount)
                    {
                        FocusHoldCell(index + 1);
                    }
                    else
                    {
                        FocusDevice(FaceDevices.NearestTo(GetHoldCellReferenceRect(index).center.y));
                    }

                    break;
                case NavigationMoveEvent.Direction.Left:
                    if (col > 0)
                    {
                        FocusHoldCell(index - 1);
                    }

                    break;
                case NavigationMoveEvent.Direction.Down:
                    if ((row + 1) * GridColumns + col < _unlockedHoldCount)
                    {
                        FocusHoldCell((row + 1) * GridColumns + col);
                    }

                    break;
                case NavigationMoveEvent.Direction.Up:
                    if (row > 0)
                    {
                        FocusHoldCell((row - 1) * GridColumns + col);
                    }

                    break;
            }
        }

        private void NavigateFromDevice(int device, NavigationMoveEvent.Direction direction)
        {
            switch (direction)
            {
                case NavigationMoveEvent.Direction.Up:
                    if (device > 0)
                    {
                        FocusDevice(device - 1);
                    }

                    break;
                case NavigationMoveEvent.Direction.Down:
                    if (device + 1 < FaceDevices.Count)
                    {
                        FocusDevice(device + 1);
                    }

                    break;
                case NavigationMoveEvent.Direction.Left:
                    if (_unlockedHoldCount > 0)
                    {
                        int row = Mathf.Clamp(Mathf.FloorToInt((FaceDevices.CenterOf(device).y - HoldGridY) /
                                                               (CellSize + CellGapY)), 0, GridRows - 1);
                        FocusHoldCell(Mathf.Min(_unlockedHoldCount - 1, row * GridColumns + GridColumns - 1));
                    }

                    break;
            }
        }

        private void UpdateTooltip()
        {
            if (HideForBoot)
            {
                _tooltip.style.display = DisplayStyle.None;
                return;
            }

            float s = _scale;
            if (_focusedHoldCell >= 0 && _focusedHoldCell < _unlockedHoldCount)
            {
                HoldCellState state = _holdStates[_focusedHoldCell];
                if (state.Kind != HoldCellKind.Empty && state.Kind != HoldCellKind.Locked)
                {
                    string name = HoldItemName(_focusedHoldCell);
                    string desc = HoldItemDescription(_focusedHoldCell);
                    ShowTooltipBox(name, desc, 72f * s, 664f * s, 376f * s, 568f * s, 62f * s);
                    return;
                }
            }

            if (_focusedDevice >= 0 && _focusedDevice < FaceDevices.Count)
            {
                RoverSystemState state = _systems.StateOf(FaceDevices.SystemOf(_focusedDevice));
                string name = _localization.Get(FaceDevices.NameKey(_focusedDevice));
                string desc = _localization.Get(FaceDevices.DescriptionKey(_focusedDevice, state, _systems.CanFitIgniter));
                ShowTooltipBox(name, desc, 512f * s, 664f * s, 440f * s, 568f * s, 62f * s);
                return;
            }

            _tooltip.style.display = DisplayStyle.None;
        }

        private string HoldItemDescription(int index)
        {
            HoldCellState state = _holdStates[index];
            switch (state.Kind)
            {
                case HoldCellKind.Material:
                    return PartInfo.MaterialDescription(state.Material, _localization);
                case HoldCellKind.StoryPart:
                    return PartInfo.StoryPartDescription(state.StoryPartId, _localization);
                default:
                    return string.Empty;
            }
        }

        private void ShowTooltipBox(string name, string desc, float x, float y, float minW, float maxW, float h)
        {
            _tooltipName.text = name;
            _tooltipDesc.text = TruncateDescription(desc);
            _tooltip.style.left = x;
            _tooltip.style.top = y;
            _tooltip.style.minWidth = minW;
            _tooltip.style.maxWidth = maxW;
            _tooltip.style.width = StyleKeyword.Auto;
            _tooltip.style.height = h;
            _tooltip.style.display = DisplayStyle.Flex;
        }

        private static string TruncateDescription(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= MaxTooltipDescriptionChars)
            {
                return text ?? string.Empty;
            }

            return text.Substring(0, MaxTooltipDescriptionChars - 1) + "…";
        }

        private void UpdateToast()
        {
            int waiting = _hold.WaitingBundles;
            if (waiting > 0)
            {
                _toastText.text = string.Format(_localization.Get(UiKeys.CargoWaiting), _numbers.Get(waiting));
                _toast.style.display = DisplayStyle.Flex;
                return;
            }

            _toastText.text = string.Empty;
            _toast.style.display = DisplayStyle.None;
        }

        private void OnGenerateCanvas(MeshGenerationContext context)
        {
            Rect rect = _canvas.contentRect;
            if (rect.width < 4f || rect.height < 4f)
            {
                return;
            }

            bool hideForBoot = HideForBoot;
            Painter2D painter = context.painter2D;
            float s = rect.width / ReferenceWidth;

            if (!hideForBoot)
            {
                // Vertical divider & header rules inside the glass safe area (no inner rectangular panel box)
                RoverLineArtPainter.DrawLine(painter, new Vector2(DividerX * s, 42f * s),
                    new Vector2(DividerX * s, 726f * s), AmberMuted, 1.2f);
                RoverLineArtPainter.DrawLine(painter, new Vector2(72f * s, 64f * s),
                    new Vector2(448f * s, 64f * s), AmberMuted, 1.2f);
                RoverLineArtPainter.DrawLine(painter, new Vector2(RightPaneX * s, 64f * s),
                    new Vector2(1080f * s, 64f * s), AmberMuted, 1.2f);
            }

            DrawHoldGrid(painter, s);

            if (!hideForBoot)
            {
                DrawRoverAndDevices(painter, s);
                DrawTooltipOverlay(painter, s);
                DrawToastDot(painter, s);
            }
        }

        private void DrawHoldGrid(Painter2D painter, float s)
        {
            bool hideForBoot = HideForBoot;
            int limit = hideForBoot ? Mathf.Min(_bootVisibleSlots, _unlockedHoldCount) : MaxHoldCells;
            for (int i = 0; i < limit; i++)
            {
                Rect r = GetHoldCellReferenceRect(i);
                float cx = r.x * s;
                float cy = r.y * s;
                float cw = r.width * s;
                float ch = r.height * s;
                HoldCellState state = _holdStates[i];

                if (state.Kind == HoldCellKind.Locked)
                {
                    RoverLineArtPainter.StrokeDashedRect(painter, cx, cy, cw, ch, AmberLocked, 1.2f, 5f * s, 4f * s);
                    RoverLineArtPainter.DrawPadlock(painter, new Vector2(cx + cw * 0.5f, cy + ch * 0.5f), AmberLocked);
                    continue;
                }

                bool focused = !hideForBoot && i == _focusedHoldCell;
                bool hasItem = state.Kind != HoldCellKind.Empty;

                RoverLineArtPainter.FillRect(painter, cx, cy, cw, ch, focused ? BgCellFocus : BgCell);
                if (focused)
                {
                    RoverLineArtPainter.StrokeRect(painter, cx, cy, cw, ch, Cyan, 2.0f);
                    RoverLineArtPainter.DrawCornerTicks(painter, cx, cy, cw, ch, 8f * s, Cyan, 2.0f);
                }
                else
                {
                    RoverLineArtPainter.StrokeRect(painter, cx, cy, cw, ch,
                        hasItem ? AmberFaint : AmberMuted, hasItem ? 1.6f : 1.2f);
                }

                if (!hasItem)
                {
                    continue;
                }

                float bootProgress = 1f;
                float slideX = 0f;
                if (hideForBoot)
                {
                    if (i >= _bootFilledSlots || _activeBoot == null)
                    {
                        continue;
                    }

                    bootProgress = _activeBoot.ItemProgress(i);
                    if (bootProgress <= 0.01f)
                    {
                        continue;
                    }

                    slideX = -(1f - bootProgress) * _settings.BootSlideDistance * s;
                }

                UiIconShape icon = GetHoldCellIcon(state);
                Color32 tint = RoverLineArtPainter.WithAlpha(UiIconPainter.LitAmber, (byte)Mathf.RoundToInt(248f * bootProgress));
                Vector2 iconCenter = new Vector2(cx + cw * 0.5f + slideX, cy + ch * 0.5f - 2f * s);
                UiIconPainter.Draw(painter, in icon, iconCenter, 60f * s, tint);

                if (!hideForBoot && state.Kind == HoldCellKind.Material && state.Units > 1)
                {
                    RoverLineArtPainter.FillRect(painter, cx + cw - 34f * s, cy + ch - 28f * s, 32f * s, 26f * s,
                        focused ? BgCellFocus : BgCell);
                }
            }
        }

        private UiIconShape GetHoldCellIcon(HoldCellState state)
        {
            switch (state.Kind)
            {
                case HoldCellKind.Material:
                    return _diagram.GetIcon(state.Material);
                case HoldCellKind.StoryPart:
                    return _diagram.GetPartIcon(state.StoryPartId);
                default:
                    throw new InvalidOperationException($"Cell kind {state.Kind} has no icon.");
            }
        }

        private void DrawRoverAndDevices(Painter2D painter, float s)
        {
            Vector2 roverOrigin = new Vector2(_settings.RoverOriginX * s, _settings.RoverOriginY * s);
            float roverScale = _settings.RoverScale * s;
            var pose = new RoverPose
            {
                WheelAngle = _settings.RoverWheelAngle,
                Bob = 0f,
                Pitch = 0f,
                BumpRear = 0f,
                BumpFront = 0f,
                MoonDropAtAxle = 0f,
                ScoopNod = 0f,
                SolarTilt = 0f,
                AntennaSway = _settings.RoverAntennaSway,
                HeadNod = _settings.RoverHeadNod,
                MorseOn = true,
                EyeBlink = false,
                DrawGroundArc = true,
                AlwaysDrawHeadlampBeam = false,
                InstalledSocketsMask = 0u,
            };

            RoverLineArtPainter.DrawRover07(painter, roverOrigin, roverScale, in pose, null, _settings);

            float r = _settings.BubbleRadius * s;

            // Leader lines from each bubble to its mount on 07: faint when the device is not working, subtle amber
            // when it works, crisp cyan under focus. The focused one is drawn last so it sits on top.
            for (int pass = 0; pass < 2; pass++)
            {
                bool drawFocusPass = pass == 1;
                for (int i = 0; i < FaceDevices.Count; i++)
                {
                    bool focused = i == _focusedDevice;
                    if (focused != drawFocusPass)
                    {
                        continue;
                    }

                    Vector2 b = FaceDevices.CenterOf(i) * s;
                    Vector2 m = RoverLineArtPainter.GetSocketMountPoint(FaceDevices.MountOf(i), roverOrigin, roverScale,
                        pose.Pitch, pose.HeadNod, _settings.RoverBodyPivotHeight, _settings.RoverWingTilt);
                    Vector2 dir = m - b;
                    float dist = dir.magnitude;
                    if (dist <= r + 2f)
                    {
                        continue;
                    }

                    Vector2 p0 = b + (dir / dist) * r;
                    if (focused)
                    {
                        Color32 focusCol = _settings.LeaderFocusColor;
                        RoverLineArtPainter.DrawLine(painter, p0, m, focusCol, _settings.LeaderFocusWidth * s);
                        RoverLineArtPainter.FillCircle(painter, m, _settings.LeaderFocusDotRadius * s, focusCol);
                    }
                    else if (_deviceOk[i])
                    {
                        Color32 instCol = _settings.LeaderInstalledColor;
                        RoverLineArtPainter.DrawLine(painter, p0, m, instCol, _settings.LeaderInstalledWidth * s);
                        RoverLineArtPainter.FillCircle(painter, m, _settings.LeaderInstalledDotRadius * s, instCol);
                    }
                    else
                    {
                        Color32 emptyCol = _settings.LeaderEmptyColor;
                        RoverLineArtPainter.DrawLine(painter, p0, m, emptyCol, _settings.LeaderEmptyWidth * s);
                        RoverLineArtPainter.FillCircle(painter, m, _settings.LeaderEmptyDotRadius * s, emptyCol);
                    }
                }
            }

            for (int i = 0; i < FaceDevices.Count; i++)
            {
                Vector2 b = FaceDevices.CenterOf(i) * s;
                bool ok = _deviceOk[i];
                bool focused = i == _focusedDevice;
                bool offered = FaceDevices.OffersIgniter(i, _igniterOffered);

                RoverLineArtPainter.FillCircle(painter, b, r, BgCell);
                if (focused || offered)
                {
                    RoverLineArtPainter.StrokeCircle(painter, b, r, Cyan, 2.2f);
                    if (offered)
                    {
                        RoverLineArtPainter.StrokeCircle(painter, b, r - 4f * s, AmberBright, 1.6f);
                    }
                }
                else if (ok)
                {
                    RoverLineArtPainter.StrokeCircle(painter, b, r, AmberBright, 2.0f);
                    RoverLineArtPainter.StrokeCircle(painter, b, r - 3.5f * s,
                        RoverLineArtPainter.WithAlpha(AmberText, 120), 1.0f);
                }
                else
                {
                    RoverLineArtPainter.StrokeCircle(painter, b, r, AmberMuted, 1.3f);
                }

                UiIconShape icon = _diagram.GetIcon(FaceDevices.MountOf(i));
                UiIconState iconState = ok || focused || offered ? UiIconState.Lit : UiIconState.Faint;
                UiIconPainter.Draw(painter, in icon, b, 46f * s, iconState);
            }
        }

        private void DrawTooltipOverlay(Painter2D painter, float s)
        {
            if (_tooltip.style.display != DisplayStyle.Flex)
            {
                return;
            }

            float tx = _tooltip.style.left.value.value;
            float ty = _tooltip.style.top.value.value;
            float tw = _tooltip.resolvedStyle.width > 4f ? _tooltip.resolvedStyle.width : _tooltip.style.minWidth.value.value;

            if (_focusedHoldCell >= 0 && _focusedHoldCell < _unlockedHoldCount)
            {
                Rect cell = GetHoldCellReferenceRect(_focusedHoldCell);
                float tickX = Mathf.Clamp(cell.center.x * s, tx + 16f * s, tx + tw - 16f * s);
                RoverLineArtPainter.DrawLine(painter, new Vector2(tickX, ty - 12f * s), new Vector2(tickX, ty),
                    RoverLineArtPainter.WithAlpha(Cyan, 195), 1.5f);
            }
            else if (_focusedDevice >= 0 && _focusedDevice < FaceDevices.Count)
            {
                Vector2 b = FaceDevices.CenterOf(_focusedDevice);
                if (b.y > 460f)
                {
                    Vector2 bs = b * s;
                    Vector2 from = new Vector2(bs.x, bs.y + _settings.BubbleRadius * s);
                    Vector2 to = new Vector2(Mathf.Clamp(bs.x, tx + 24f * s, tx + tw - 24f * s), ty);
                    RoverLineArtPainter.DrawLine(painter, from, to, RoverLineArtPainter.WithAlpha(Cyan, 175), 1.3f);
                }
            }
        }

        private void DrawToastDot(Painter2D painter, float s)
        {
            if (_toast.style.display != DisplayStyle.Flex)
            {
                return;
            }

            RoverLineArtPainter.FillCircle(painter, new Vector2(64f * s, 707f * s), 3.5f * s, AmberBright);
        }

        private Vector2 EventToReference(IPointerEvent evt)
        {
            Rect wb = _panel.worldBound;
            Vector2 local = !float.IsNaN(wb.width) && wb.width > 0f
                ? (Vector2)evt.position - wb.position
                : (Vector2)evt.localPosition;
            return PanelToReference(local);
        }

        private Vector2 PanelToReference(Vector2 localPos)
        {
            float inv = 1f / Mathf.Max(0.01f, _scale);
            return localPos * inv;
        }

        private static void PositionElement(VisualElement element, float x, float y)
        {
            element.style.left = x;
            element.style.top = y;
        }

        private static void PositionBox(VisualElement element, float x, float y, float w, float h)
        {
            element.style.left = x;
            element.style.top = y;
            element.style.width = w;
            element.style.height = h;
        }

        private static T Require<T>(VisualElement root, string name) where T : VisualElement
        {
            T found = root.Q<T>(name);
            if (found == null)
            {
                throw new InvalidOperationException($"GameUI.uxml has no {typeof(T).Name} named '{name}'.");
            }

            return found;
        }

        private readonly struct HoldCellState
        {
            public readonly HoldCellKind Kind;
            public readonly SalvageMaterial Material;
            public readonly int Units;
            public readonly string StoryPartId;

            private HoldCellState(HoldCellKind kind, SalvageMaterial material, int units, string storyPartId)
            {
                Kind = kind;
                Material = material;
                Units = units;
                StoryPartId = storyPartId;
            }

            public static HoldCellState ForEmpty() => new HoldCellState(HoldCellKind.Empty, default, 0, null);

            public static HoldCellState ForLocked() => new HoldCellState(HoldCellKind.Locked, default, 0, null);

            public static HoldCellState ForMaterial(SalvageMaterial material, int units) =>
                new HoldCellState(HoldCellKind.Material, material, units, null);

            public static HoldCellState ForStoryPart(string partId) =>
                new HoldCellState(HoldCellKind.StoryPart, default, 1, partId);
        }
    }
}
