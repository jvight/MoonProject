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
        WaitingKit = 4,
    }

    /// <summary>
    /// The icon-first inventory and equipment screen (<c>FACE-INV view</c>, Variant A of <c>docs/design/screen-ui.md</c>
    /// Round 2): a flat <c>1152x768</c> (1.5:1) two-pane CRT layout with the 4x6 hold grid (24 cells at max size, locked
    /// cells as dotted outlines with padlocks) on the left and Rover 07 in side-3/4 line-art surrounded by 10 orbital
    /// socket bubbles (<c>r = 31 px</c>) on the right (or home's shelves when docked).
    /// </summary>
    public sealed class FaceInventoryView
    {
        public const float ReferenceWidth = 1152f;
        public const float ReferenceHeight = 768f;
        public const float ReferenceAspect = ReferenceWidth / ReferenceHeight;

        public const int GridColumns = 4;
        public const int GridRows = 6;
        public const int MaxHoldCells = GridColumns * GridRows;
        public const int OrbitalSocketCount = 10;
        public const int MaxShelfCells = 24;

        public const float HoldGridX = 52f;
        public const float HoldGridY = 74f;
        public const float ShelfGridX = 512f;
        public const float ShelfGridY = 74f;
        public const float CellSize = 90f;
        public const float CellGapX = 12f;
        public const float CellGapY = 6f;
        public const float DividerX = 480f;
        public const float BubbleRadius = 36f;
        public const float RoverOriginX = 804f;
        public const float RoverOriginY = 468f;
        public const float RoverScale = 156f;

        public const int MaxTooltipDescriptionChars = 52;
        private float ToastDurationSeconds => _settings.ToastDurationSeconds;

        /// <summary>The 10 orbital sockets around 07 in clockwise elliptical order starting from Wing.</summary>
        public static readonly RoverSocket[] OrbitalSockets =
        {
            RoverSocket.Wing,
            RoverSocket.Cutter,
            RoverSocket.Tether,
            RoverSocket.Lamp,
            RoverSocket.Flanks,
            RoverSocket.Belly,
            RoverSocket.Panniers,
            RoverSocket.Hold,
            RoverSocket.Deck,
            RoverSocket.Back,
        };

        private static readonly Vector2[] OrbitalCenters =
        {
            new Vector2(756f, 110f), // Wing
            new Vector2(948f, 130f), // Cutter
            new Vector2(1048f, 228f), // Tether
            new Vector2(1064f, 366f), // Lamp
            new Vector2(1040f, 504f), // Flanks
            new Vector2(908f, 608f), // Belly
            new Vector2(728f, 608f), // Panniers
            new Vector2(562f, 504f), // Hold
            new Vector2(548f, 348f), // Deck
            new Vector2(612f, 188f), // Back
        };

        // Two-colour amber CRT palette + cyan focus/drag accent read from CargoScreenSettings.
        private Color32 BgCell => _settings.CellBackground;
        private Color32 BgCellFocus => _settings.CellFocusBackground;
        private Color32 BgPlate => _settings.PlateBackground;
        private Color32 AmberBright => _settings.AmberBright;
        private Color32 AmberText => _settings.AmberText;
        private Color32 AmberDim => _settings.AmberDim;
        private Color32 AmberFaint => _settings.AmberFaint;
        private Color32 AmberMuted => _settings.AmberMuted;
        private Color32 AmberLocked => _settings.AmberLocked;
        private Color32 Cyan => _settings.CyanAccent;
        private Color32 CyanDim => _settings.CyanDim;

        private readonly VisualElement _panel;
        private readonly VisualElement _canvas;
        private readonly Label _holdTitle;
        private readonly Label _rightTitle;
        private readonly Label _bootLabel;
        private readonly VisualElement _holdGrid;
        private readonly VisualElement _socketsRoot;
        private readonly VisualElement _homeGrid;
        private readonly VisualElement _tooltip;
        private readonly Label _tooltipName;
        private readonly Label _tooltipDesc;
        private readonly VisualElement _toast;
        private readonly Label _toastText;

        private readonly CargoScreenSettings _settings;
        private readonly ILocalization _localization;
        private readonly ICargoHold _hold;
        private readonly IHomeStorage _home;
        private readonly IStackTransfer _transfer;
        private readonly IDockState _dock;
        private readonly IRoverEquipment _equipment;
        private readonly RoverDiagramData _diagram;
        private readonly IntText _numbers;
        private readonly EventBus _events;

        private readonly VisualElement[] _holdCells = new VisualElement[MaxHoldCells];
        private readonly Label[] _holdBadges = new Label[MaxHoldCells];
        private readonly HoldCellState[] _holdStates = new HoldCellState[MaxHoldCells];

        private readonly VisualElement[] _socketBubbles = new VisualElement[OrbitalSocketCount];
        private readonly VisualElement[] _homeCells = new VisualElement[MaxShelfCells];
        private readonly Label[] _homeBadges = new Label[MaxShelfCells];

        private int _unlockedHoldCount;
        private int _homeSlotCount;
        private int _focusedHoldCell = -1;
        private int _focusedSocketIndex = -1;
        private int _focusedHomeCell = -1;

        private int _dragHoldCell = -1;
        private int _dragRowIndex = -1;
        private RoverKitPiece _dragKitPiece;
        private Vector2 _dragStartRefPos;
        private Vector2 _dragCurrentRefPos;
        private bool _isDraggingKit;
        private int _dragTargetSocketIndex = -1;
        private int _lastPointerDownFrame = -1;
        private int _lastPointerUpFrame = -1;
        private int _lastNavigationFrame = -1;
        private readonly InputHandoff _handoff = new InputHandoff();
        private int _lastSubmitFrame = -1;

        private string _transientToastKey;
        private float _transientToastTimer;
        private bool _bootRunning;
        private bool _userInteractedDuringBoot;
        private int _bootVisibleSlots;
        private int _bootFilledSlots;
        private HoldBoot _activeBoot;
        private float _scale = 1f;

        internal FaceInventoryView(VisualElement panel, CargoScreenSettings settings, ILocalization localization,
            ICargoHold hold, IHomeStorage home, IStackTransfer transfer, IDockState dock, IRoverEquipment equipment,
            RoverDiagramData diagram, IntText numbers, EventBus events)
        {
            _panel = panel ?? throw new ArgumentNullException(nameof(panel));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _hold = hold ?? throw new ArgumentNullException(nameof(hold));
            _home = home ?? throw new ArgumentNullException(nameof(home));
            _transfer = transfer ?? throw new ArgumentNullException(nameof(transfer));
            _dock = dock ?? throw new ArgumentNullException(nameof(dock));
            _equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
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
            _homeGrid = Require<VisualElement>(_panel, "cargo-home-grid");
            _tooltip = Require<VisualElement>(_panel, "cargo-tooltip");
            _tooltipName = Require<Label>(_panel, "cargo-tooltip-name");
            _tooltipDesc = Require<Label>(_panel, "cargo-tooltip-desc");
            _toast = Require<VisualElement>(_panel, "cargo-toast");
            _toastText = Require<Label>(_panel, "cargo-toast-text");

            _canvas.generateVisualContent += OnGenerateCanvas;
            BuildHoldCells();
            BuildSocketBubbles();
            BuildHomeCells();

            _panel.RegisterCallback<PointerMoveEvent>(OnPanelPointerMove);
            _panel.RegisterCallback<PointerUpEvent>(OnPanelPointerUp);
        }

        /// <summary>True when fitting a part succeeded and the screen should close for the world install beat.</summary>
        public bool WantsClose { get; set; }

        /// <summary>Unlocked hold cell count (typically 8, 16, or 24).</summary>
        public int UnlockedCellCount => _unlockedHoldCount;

        /// <summary>Locked hold cell count (<c>24 - UnlockedCellCount</c>).</summary>
        public int LockedCellCount => MaxHoldCells - _unlockedHoldCount;

        /// <summary>Active home shelf slot count when docked.</summary>
        public int HomeCellCount => _homeSlotCount;

        /// <summary>True while a waiting kit is being dragged across the panel.</summary>
        public bool IsDraggingKit => _isDraggingKit;

        /// <summary>The currently focused or hovered hold cell index (0..23), or -1.</summary>
        public int FocusedHoldCell => _focusedHoldCell;

        /// <summary>The currently focused or hovered orbital socket index (0..9), or -1.</summary>
        public int FocusedSocketIndex => _focusedSocketIndex;

        /// <summary>The currently focused or hovered home shelf cell index (0..23), or -1.</summary>
        public int FocusedHomeCell => _focusedHomeCell;

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

        public VisualElement HomeCell(int index) => _homeCells[index];

        public VisualElement SocketElement(RoverSocket socket)
        {
            int idx = OrbitalIndexOf(socket);
            return idx >= 0 ? _socketBubbles[idx] : null;
        }

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

        /// <summary>Returns the reference <c>1152x768</c> centre of orbital socket <paramref name="orbitalIndex"/> (0..9).</summary>
        public static Vector2 GetOrbitalSocketReferenceCenter(int orbitalIndex)
        {
            return OrbitalCenters[orbitalIndex];
        }

        /// <summary>Returns the orbital index (0..9) of <paramref name="socket"/>, or -1 if not an orbital socket.</summary>
        public static int OrbitalIndexOf(RoverSocket socket)
        {
            for (int i = 0; i < OrbitalSockets.Length; i++)
            {
                if (OrbitalSockets[i] == socket)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// True only when <paramref name="socket"/>'s leader line to 07 is bright cyan (focused, hovered, or active drag
        /// target). By Director's tweak, default leader lines are faint (~27% alpha) and installed leader lines are subtle
        /// amber (~38% alpha).
        /// </summary>
        public bool IsSocketLeaderCyan(RoverSocket socket)
        {
            int idx = OrbitalIndexOf(socket);
            if (idx < 0)
            {
                return false;
            }

            int activeSocket = _isDraggingKit ? _dragTargetSocketIndex : _focusedSocketIndex;
            return idx == activeSocket;
        }

        /// <summary>Returns the current leader-line stroke color for <paramref name="socket"/> from <see cref="CargoScreenSettings"/>.</summary>
        public Color32 GetSocketLeaderColor(RoverSocket socket)
        {
            if (IsSocketLeaderCyan(socket))
            {
                return _settings.LeaderFocusColor;
            }

            return _equipment.TryGetFitted(socket, out _)
                ? _settings.LeaderInstalledColor
                : _settings.LeaderEmptyColor;
        }

        /// <summary>Returns the current leader-line stroke width (px at 1152x768) for <paramref name="socket"/>.</summary>
        public float GetSocketLeaderWidth(RoverSocket socket)
        {
            if (IsSocketLeaderCyan(socket))
            {
                return _settings.LeaderFocusWidth;
            }

            return _equipment.TryGetFitted(socket, out _)
                ? _settings.LeaderInstalledWidth
                : _settings.LeaderEmptyWidth;
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
                case HoldCellKind.WaitingKit:
                    return PartInfo.PartName(state.KitPiece, _localization);
                default:
                    return string.Empty;
            }
        }

        /// <summary>Index of the hold cell holding waiting kit <paramref name="rowIndex"/>, or -1.</summary>
        public int WaitingKitCellIndex(int rowIndex)
        {
            for (int i = 0; i < _unlockedHoldCount; i++)
            {
                if (_holdStates[i].Kind == HoldCellKind.WaitingKit && _holdStates[i].RowIndex == rowIndex)
                {
                    return i;
                }
            }

            return -1;
        }

        public VisualElement WaitingKitCellElement(int rowIndex)
        {
            int idx = WaitingKitCellIndex(rowIndex);
            return idx >= 0 ? _holdCells[idx] : null;
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
            WantsClose = false;
            _isDraggingKit = false;
            _dragHoldCell = -1;
            _dragRowIndex = -1;
            _dragTargetSocketIndex = -1;
            _transientToastKey = null;
            _transientToastTimer = 0f;
            _focusedHoldCell = -1;
            _focusedSocketIndex = -1;
            _focusedHomeCell = -1;
            _userInteractedDuringBoot = false;
            _handoff.Reset();
        }

        public void Tick(float deltaTime)
        {
            if (!HideForBoot && !_isDraggingKit)
            {
                SyncPendingFocus();
            }

            if (_transientToastTimer > 0f)
            {
                _transientToastTimer -= deltaTime;
                if (_transientToastTimer <= 0f)
                {
                    _transientToastTimer = 0f;
                    _transientToastKey = null;
                    UpdateToast();
                    _canvas.MarkDirtyRepaint();
                }
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
                _homeGrid.style.display = DisplayStyle.None;
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
            PositionElement(_rightTitle, ShelfGridX * s, 40f * s);
            PositionElement(_bootLabel, 72f * s, 40f * s);

            for (int i = 0; i < MaxHoldCells; i++)
            {
                Rect r = GetHoldCellReferenceRect(i);
                PositionBox(_holdCells[i], r.x * s, r.y * s, r.width * s, r.height * s);
            }

            float bubbleDiam = BubbleRadius * 2f;
            for (int i = 0; i < OrbitalSocketCount; i++)
            {
                Vector2 c = OrbitalCenters[i];
                PositionBox(_socketBubbles[i], (c.x - BubbleRadius) * s, (c.y - BubbleRadius) * s,
                    bubbleDiam * s, bubbleDiam * s);
            }

            for (int i = 0; i < MaxShelfCells; i++)
            {
                int col = i % GridColumns;
                int row = i / GridColumns;
                float x = ShelfGridX + col * (CellSize + CellGapX);
                float y = ShelfGridY + row * (CellSize + CellGapY);
                PositionBox(_homeCells[i], x * s, y * s, CellSize * s, CellSize * s);
            }

            PositionBox(_toast, 76f * s, 688f * s, 1000f * s, 38f * s);
            UpdateTooltip();
            _canvas.MarkDirtyRepaint();
        }

        public void Refresh()
        {
            bool docked = _dock.IsDocked;
            bool hideForBoot = HideForBoot;
            _holdTitle.text = _localization.Get(UiKeys.CargoHold);
            _rightTitle.text = _localization.Get(docked ? UiKeys.HintShelves : UiKeys.CargoTabSeven);

            int baseHoldSlots = Mathf.Clamp(_hold.SlotCount, 0, MaxHoldCells);
            int waitingKits = docked ? 0 : _equipment.RowCount;
            int filledCount = 0;

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
                    _demoValidateMaterialIcon(slot.Material);
                    _holdStates[i] = HoldCellState.ForMaterial(slot.Material, slot.Units);
                    filledCount++;
                }
                else if (!string.IsNullOrEmpty(partId))
                {
                    _diagram.GetPartIcon(partId);
                    _holdStates[i] = HoldCellState.ForStoryPart(partId);
                    filledCount++;
                }
                else
                {
                    _holdStates[i] = HoldCellState.ForEmpty();
                }
            }

            int unlocked = baseHoldSlots;
            for (int r = 0; r < waitingKits; r++)
            {
                RoverKitPiece kit = _equipment.RowPart(r);
                _diagram.GetIcon(kit);
                int targetCell = -1;
                for (int i = 0; i < unlocked; i++)
                {
                    if (_holdStates[i].Kind == HoldCellKind.Empty)
                    {
                        targetCell = i;
                        break;
                    }
                }

                if (targetCell < 0 && unlocked < MaxHoldCells)
                {
                    targetCell = unlocked;
                    unlocked++;
                }

                if (targetCell >= 0)
                {
                    _holdStates[targetCell] = HoldCellState.ForWaitingKit(kit, r);
                    filledCount++;
                }
            }

            _unlockedHoldCount = unlocked;
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

            _socketsRoot.style.display = !docked && !hideForBoot ? DisplayStyle.Flex : DisplayStyle.None;
            for (int i = 0; i < OrbitalSocketCount; i++)
            {
                RoverSocket socket = OrbitalSockets[i];
                _diagram.GetIcon(socket);
                bool fitted = _equipment.TryGetFitted(socket, out _);
                _socketBubbles[i].style.display = !docked && !hideForBoot ? DisplayStyle.Flex : DisplayStyle.None;
                _socketBubbles[i].focusable = !docked;
                _socketBubbles[i].EnableInClassList(CargoScreen.FittedClass, fitted);
            }

            _homeSlotCount = docked ? Mathf.Clamp(_home.SlotCount, 0, MaxShelfCells) : 0;
            _homeGrid.style.display = docked && !hideForBoot ? DisplayStyle.Flex : DisplayStyle.None;
            for (int i = 0; i < MaxShelfCells; i++)
            {
                bool active = docked && i < _homeSlotCount;
                _homeCells[i].style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
                if (!active)
                {
                    _homeCells[i].focusable = false;
                    _homeBadges[i].style.display = DisplayStyle.None;
                    continue;
                }

                MaterialSlot slot = _home.GetSlot(i);
                if (!slot.IsEmpty)
                {
                    _demoValidateMaterialIcon(slot.Material);
                }

                _homeCells[i].focusable = true;
                _homeCells[i].EnableInClassList(CargoScreen.EmptyClass, slot.IsEmpty);
                bool showBadge = !slot.IsEmpty && slot.Units > 1;
                _homeBadges[i].style.display = showBadge ? DisplayStyle.Flex : DisplayStyle.None;
                if (showBadge)
                {
                    _homeBadges[i].text = _numbers.Get(Mathf.Min(99, slot.Units));
                }
            }

            UpdateTooltip();
            UpdateToast();
            _canvas.MarkDirtyRepaint();
        }

        public void FocusFirst()
        {
            bool docked = _dock.IsDocked;
            for (int i = 0; i < _unlockedHoldCount; i++)
            {
                HoldCellState state = _holdStates[i];
                bool hasDefaultTarget = docked ? state.Kind == HoldCellKind.Material : state.Kind != HoldCellKind.Empty;
                if (hasDefaultTarget)
                {
                    FocusHoldCell(i);
                    return;
                }
            }

            if (docked)
            {
                for (int i = 0; i < _homeSlotCount; i++)
                {
                    if (!_home.GetSlot(i).IsEmpty)
                    {
                        FocusHomeCell(i);
                        return;
                    }
                }
            }

            if (_unlockedHoldCount > 0)
            {
                FocusHoldCell(0);
                return;
            }

            if (!docked && OrbitalSocketCount > 0)
            {
                FocusSocket(OrbitalSockets[0]);
            }
        }

        /// <summary>Focuses hold cell <paramref name="index"/> (for tests and captures).</summary>
        public void FocusHoldCell(int index)
        {
            _focusedHoldCell = index;
            _focusedSocketIndex = -1;
            _focusedHomeCell = -1;
            if (index >= 0 && index < MaxHoldCells)
            {
                _holdCells[index].Focus();
            }

            UpdateTooltip();
            _canvas.MarkDirtyRepaint();
        }

        /// <summary>Focuses orbital socket <paramref name="socket"/> (for tests and captures).</summary>
        public void FocusSocket(RoverSocket socket)
        {
            int idx = OrbitalIndexOf(socket);
            _focusedSocketIndex = idx;
            _focusedHoldCell = -1;
            _focusedHomeCell = -1;
            if (idx >= 0)
            {
                _socketBubbles[idx].Focus();
            }

            UpdateTooltip();
            _canvas.MarkDirtyRepaint();
        }

        /// <summary>Focuses home shelf cell <paramref name="index"/> when docked.</summary>
        public void FocusHomeCell(int index)
        {
            _focusedHomeCell = index;
            _focusedHoldCell = -1;
            _focusedSocketIndex = -1;
            if (index >= 0 && index < MaxShelfCells)
            {
                _homeCells[index].Focus();
            }

            UpdateTooltip();
            _canvas.MarkDirtyRepaint();
        }

        /// <summary>Clears any active cell or socket focus and hides the tooltip (for resting-state captures).</summary>
        public void ClearFocus()
        {
            _focusedHoldCell = -1;
            _focusedSocketIndex = -1;
            _focusedHomeCell = -1;
            _panel.Blur();
            UpdateTooltip();
            _canvas.MarkDirtyRepaint();
        }

        /// <summary>
        /// Begins a kit drag from hold cell <paramref name="holdCellIndex"/> to reference position
        /// <paramref name="referencePosition"/> (in <c>1152x768</c> coordinates), optionally locking onto
        /// <paramref name="targetSocket"/>.
        /// </summary>
        public void BeginKitDrag(int holdCellIndex, Vector2 referencePosition, RoverSocket? targetSocket = null)
        {
            if (holdCellIndex < 0 || holdCellIndex >= _unlockedHoldCount ||
                _holdStates[holdCellIndex].Kind != HoldCellKind.WaitingKit)
            {
                throw new InvalidOperationException($"Hold cell {holdCellIndex} does not contain a waiting kit.");
            }

            _dragHoldCell = holdCellIndex;
            _dragRowIndex = _holdStates[holdCellIndex].RowIndex;
            _dragKitPiece = _holdStates[holdCellIndex].KitPiece;
            Rect cellRect = GetHoldCellReferenceRect(holdCellIndex);
            _dragStartRefPos = cellRect.center;
            _dragCurrentRefPos = referencePosition;
            _isDraggingKit = true;
            _focusedHoldCell = -1;
            _focusedSocketIndex = -1;
            _dragTargetSocketIndex = targetSocket.HasValue
                ? OrbitalIndexOf(targetSocket.Value)
                : FindSocketAtReferencePos(referencePosition);
            UpdateTooltip();
            _canvas.MarkDirtyRepaint();
        }

        /// <summary>
        /// Completes an active kit drag by dropping onto <paramref name="dropSocket"/> (or <c>null</c> for open space).
        /// Matching socket fits the kit (if at station); non-matching socket snaps back cleanly without fitting.
        /// </summary>
        public bool CompleteKitDrag(RoverSocket? dropSocket)
        {
            if (!_isDraggingKit || _dragRowIndex < 0)
            {
                return false;
            }

            int rowIndex = _dragRowIndex;
            RoverKitPiece piece = _dragKitPiece;
            _isDraggingKit = false;
            _dragHoldCell = -1;
            _dragRowIndex = -1;
            _dragTargetSocketIndex = -1;
            UpdateTooltip();
            _canvas.MarkDirtyRepaint();

            RoverSocket matchingSocket = _equipment.SocketOf(piece);
            if (!dropSocket.HasValue || dropSocket.Value != matchingSocket)
            {
                Refresh();
                return false;
            }

            return TryFitKitAtRow(rowIndex);
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

        /// <summary>
        /// Handles a pointer-up at <paramref name="panelPos"/> in panel coordinates (mapped from 07's curved CRT
        /// <c>Screen</c> mesh via <see cref="FaceScreenMapper.ScreenToPanel"/>).
        /// </summary>
        public void PointerUpAtPanel(Vector2 panelPos)
        {
            if (_lastPointerUpFrame == Time.frameCount)
            {
                return;
            }

            _lastPointerUpFrame = Time.frameCount;
            HandlePointerUpAtReferencePos(PanelToReference(panelPos));
        }

        /// <summary>
        /// Steps focus in <paramref name="direction"/> across the hold grid and 07's 10 orbital socket bubbles (or home
        /// shelf cells when docked).
        /// </summary>
        public bool Navigate(NavigationMoveEvent.Direction direction)
        {
            if (_isDraggingKit || !IsCardinalDirection(direction))
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

            if (!_dock.IsDocked && _focusedSocketIndex >= 0 && _focusedSocketIndex < OrbitalSocketCount)
            {
                NavigateFromSocket(_focusedSocketIndex, direction);
                return true;
            }

            if (_dock.IsDocked && _focusedHomeCell >= 0 && _focusedHomeCell < _homeSlotCount)
            {
                NavigateFromHomeCell(_focusedHomeCell, direction);
                return true;
            }

            FocusFirst();
            return _focusedHoldCell >= 0 || _focusedSocketIndex >= 0 || _focusedHomeCell >= 0;
        }

        /// <summary>
        /// Submits the currently focused hold cell, orbital socket, or home shelf cell (triggered by Enter, Space, or
        /// Gamepad South / A).
        /// </summary>
        public bool SubmitFocused()
        {
            if (_isDraggingKit || _lastSubmitFrame == Time.frameCount)
            {
                return false;
            }

            EnsureInteractiveDuringBoot();
            if (_focusedHoldCell >= 0 && _focusedHoldCell < _unlockedHoldCount)
            {
                OnHoldCellSubmit(_focusedHoldCell);
                return true;
            }

            if (!_dock.IsDocked && _focusedSocketIndex >= 0 && _focusedSocketIndex < OrbitalSocketCount)
            {
                OnSocketSubmit(_focusedSocketIndex);
                return true;
            }

            if (_dock.IsDocked && _focusedHomeCell >= 0 && _focusedHomeCell < _homeSlotCount)
            {
                OnHomeCellSubmit(_focusedHomeCell);
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
            else if (!_dock.IsDocked && _focusedSocketIndex >= 0 && _focusedSocketIndex < OrbitalSocketCount)
            {
                VisualElement target = _socketBubbles[_focusedSocketIndex];
                if (current != target && target.canGrabFocus)
                {
                    target.Focus();
                }
            }
            else if (_dock.IsDocked && _focusedHomeCell >= 0 && _focusedHomeCell < _homeSlotCount)
            {
                VisualElement target = _homeCells[_focusedHomeCell];
                if (current != target && target.canGrabFocus)
                {
                    target.Focus();
                }
            }
        }

        private void _demoValidateMaterialIcon(SalvageMaterial material)
        {
            _diagram.GetIcon(material);
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
                cell.RegisterCallback<NavigationSubmitEvent>(_ =>
                {
                    if (_lastSubmitFrame != Time.frameCount)
                    {
                        OnHoldCellSubmit(index);
                    }
                });
                cell.RegisterCallback<NavigationMoveEvent>(evt => OnHoldNavigationMove(index, evt));

                _holdCells[i] = cell;
                _holdBadges[i] = badge;
                _holdGrid.Add(cell);
            }
        }

        private void BuildSocketBubbles()
        {
            for (int i = 0; i < OrbitalSocketCount; i++)
            {
                int orbitalIdx = i;
                RoverSocket socket = OrbitalSockets[i];
                var bubble = new VisualElement { name = "seven-socket-" + socket.ToString().ToLowerInvariant() };
                bubble.AddToClassList("cargo__socket-bubble");

                bubble.RegisterCallback<FocusInEvent>(_ => OnSocketFocus(orbitalIdx));
                bubble.RegisterCallback<PointerEnterEvent>(_ => OnSocketHover(orbitalIdx));
                bubble.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button == 0 && _lastPointerDownFrame != Time.frameCount)
                    {
                        _lastPointerDownFrame = Time.frameCount;
                        OnSocketSubmit(orbitalIdx);
                    }
                });
                bubble.RegisterCallback<NavigationSubmitEvent>(_ =>
                {
                    if (_lastSubmitFrame != Time.frameCount)
                    {
                        OnSocketSubmit(orbitalIdx);
                    }
                });
                bubble.RegisterCallback<NavigationMoveEvent>(evt => OnSocketNavigationMove(orbitalIdx, evt));

                _socketBubbles[i] = bubble;
                _socketsRoot.Add(bubble);
            }
        }

        private void BuildHomeCells()
        {
            for (int i = 0; i < MaxShelfCells; i++)
            {
                int index = i;
                var cell = new VisualElement { name = "cargo-home-" + i };
                cell.AddToClassList(CargoScreen.CellClass);

                var badge = new Label { name = "cargo-home-badge-" + i, pickingMode = PickingMode.Ignore };
                badge.AddToClassList("cargo__badge");
                cell.Add(badge);

                cell.RegisterCallback<FocusInEvent>(_ => OnHomeCellFocus(index));
                cell.RegisterCallback<PointerEnterEvent>(_ => OnHomeCellHover(index));
                cell.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button == 0 && _lastPointerDownFrame != Time.frameCount)
                    {
                        _lastPointerDownFrame = Time.frameCount;
                        OnHomeCellSubmit(index);
                    }
                });
                cell.RegisterCallback<NavigationSubmitEvent>(_ =>
                {
                    if (_lastSubmitFrame != Time.frameCount)
                    {
                        OnHomeCellSubmit(index);
                    }
                });
                cell.RegisterCallback<NavigationMoveEvent>(evt => OnHomeNavigationMove(index, evt));

                _homeCells[i] = cell;
                _homeBadges[i] = badge;
                _homeGrid.Add(cell);
            }
        }

        private void OnHoldCellFocus(int index)
        {
            if (index >= _unlockedHoldCount)
            {
                return;
            }

            _focusedHoldCell = index;
            _focusedSocketIndex = -1;
            _focusedHomeCell = -1;
            UpdateTooltip();
            _canvas.MarkDirtyRepaint();
        }

        private void OnHoldCellHover(int index)
        {
            if (_isDraggingKit || index >= _unlockedHoldCount || !_handoff.HoverMaySelect)
            {
                return;
            }

            EnsureInteractiveDuringBoot();
            _focusedHoldCell = index;
            _focusedSocketIndex = -1;
            _focusedHomeCell = -1;
            UpdateTooltip();
            _canvas.MarkDirtyRepaint();
        }

        private void OnSocketFocus(int orbitalIdx)
        {
            _focusedSocketIndex = orbitalIdx;
            _focusedHoldCell = -1;
            _focusedHomeCell = -1;
            UpdateTooltip();
            _canvas.MarkDirtyRepaint();
        }

        private void OnSocketHover(int orbitalIdx)
        {
            if (!_isDraggingKit && !_handoff.HoverMaySelect)
            {
                return;
            }

            EnsureInteractiveDuringBoot();
            if (_isDraggingKit)
            {
                _dragTargetSocketIndex = orbitalIdx;
            }
            else
            {
                _focusedSocketIndex = orbitalIdx;
                _focusedHoldCell = -1;
                _focusedHomeCell = -1;
            }

            UpdateTooltip();
            _canvas.MarkDirtyRepaint();
        }

        private void OnHomeCellHover(int index)
        {
            if (_handoff.HoverMaySelect)
            {
                OnHomeCellFocus(index);
            }
        }

        private void OnHomeCellFocus(int index)
        {
            if (index >= _homeSlotCount)
            {
                return;
            }

            _focusedHomeCell = index;
            _focusedHoldCell = -1;
            _focusedSocketIndex = -1;
            UpdateTooltip();
            _canvas.MarkDirtyRepaint();
        }

        private void OnHoldCellPointerDown(int index, PointerDownEvent evt)
        {
            if (evt.button != 0 || index >= _unlockedHoldCount || _lastPointerDownFrame == Time.frameCount)
            {
                return;
            }

            _lastPointerDownFrame = Time.frameCount;
            if (evt.pointerId >= 0)
            {
                _panel.CapturePointer(evt.pointerId);
            }

            BeginHoldCellPress(index, EventToReference(evt));
        }

        private void OnPanelPointerMove(PointerMoveEvent evt)
        {
            HandlePointerMoveAtReferencePos(EventToReference(evt));
        }

        private void OnPanelPointerUp(PointerUpEvent evt)
        {
            if (evt.pointerId >= 0 && _panel.HasPointerCapture(evt.pointerId))
            {
                _panel.ReleasePointer(evt.pointerId);
            }

            if (_lastPointerUpFrame == Time.frameCount)
            {
                return;
            }

            _lastPointerUpFrame = Time.frameCount;
            HandlePointerUpAtReferencePos(EventToReference(evt));
        }

        private void BeginHoldCellPress(int index, Vector2 refPos)
        {
            EnsureInteractiveDuringBoot();
            OnHoldCellFocus(index);
            HoldCellState state = _holdStates[index];
            if (!_dock.IsDocked && state.Kind == HoldCellKind.WaitingKit)
            {
                _dragHoldCell = index;
                _dragRowIndex = state.RowIndex;
                _dragKitPiece = state.KitPiece;
                _dragStartRefPos = refPos;
                _dragCurrentRefPos = refPos;
                _isDraggingKit = false;
                _dragTargetSocketIndex = -1;
                return;
            }

            OnHoldCellSubmit(index);
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
                BeginHoldCellPress(holdIdx, refPos);
                return;
            }

            if (!_dock.IsDocked)
            {
                int socketIdx = FindSocketAtReferencePos(refPos);
                if (socketIdx >= 0)
                {
                    EnsureInteractiveDuringBoot();
                    OnSocketFocus(socketIdx);
                    OnSocketSubmit(socketIdx);
                }
            }
            else
            {
                int homeIdx = FindHomeCellAtReferencePos(refPos);
                if (homeIdx >= 0)
                {
                    EnsureInteractiveDuringBoot();
                    OnHomeCellHover(homeIdx);
                    OnHomeCellSubmit(homeIdx);
                }
            }
        }

        private void HandlePointerMoveAtReferencePos(Vector2 refPos)
        {
            if (float.IsNaN(refPos.x) || float.IsNaN(refPos.y))
            {
                return;
            }

            if (_dragHoldCell >= 0 && _dragRowIndex >= 0)
            {
                EnsureInteractiveDuringBoot();
                _dragCurrentRefPos = refPos;
                if (!_isDraggingKit && Vector2.Distance(refPos, _dragStartRefPos) >= _settings.DragThreshold)
                {
                    _isDraggingKit = true;
                    _focusedHoldCell = -1;
                    _focusedSocketIndex = -1;
                }

                if (_isDraggingKit)
                {
                    _dragTargetSocketIndex = FindSocketAtReferencePos(refPos);
                    UpdateTooltip();
                    _canvas.MarkDirtyRepaint();
                }

                return;
            }

            int holdIdx = FindHoldCellAtReferencePos(refPos);
            if (holdIdx >= 0)
            {
                OnHoldCellHover(holdIdx);
                return;
            }

            if (!_dock.IsDocked)
            {
                int socketIdx = FindSocketAtReferencePos(refPos);
                if (socketIdx >= 0)
                {
                    OnSocketHover(socketIdx);
                }
            }
            else
            {
                int homeIdx = FindHomeCellAtReferencePos(refPos);
                if (homeIdx >= 0)
                {
                    EnsureInteractiveDuringBoot();
                    OnHomeCellFocus(homeIdx);
                }
            }
        }

        private void HandlePointerUpAtReferencePos(Vector2 refPos)
        {
            if (_dragHoldCell < 0)
            {
                return;
            }

            if (float.IsNaN(refPos.x) || float.IsNaN(refPos.y))
            {
                int fallbackCell = _dragHoldCell;
                if (_isDraggingKit)
                {
                    CompleteKitDrag(null);
                    if (fallbackCell >= 0 && fallbackCell < _unlockedHoldCount)
                    {
                        OnHoldCellFocus(fallbackCell);
                    }
                }
                else
                {
                    _dragHoldCell = -1;
                    _dragRowIndex = -1;
                }

                return;
            }

            if (_isDraggingKit)
            {
                int sourceCell = _dragHoldCell;
                int hitSocket = !_dock.IsDocked ? FindSocketAtReferencePos(refPos) : -1;
                RoverSocket? dropSocket = hitSocket >= 0 ? OrbitalSockets[hitSocket] : (RoverSocket?)null;
                bool fitted = CompleteKitDrag(dropSocket);
                if (!fitted)
                {
                    int holdIdx = FindHoldCellAtReferencePos(refPos);
                    if (holdIdx >= 0)
                    {
                        OnHoldCellFocus(holdIdx);
                    }
                    else if (hitSocket >= 0)
                    {
                        OnSocketFocus(hitSocket);
                    }
                    else if (sourceCell >= 0 && sourceCell < _unlockedHoldCount)
                    {
                        OnHoldCellFocus(sourceCell);
                    }
                }

                return;
            }

            int clickedCell = _dragHoldCell;
            _dragHoldCell = -1;
            _dragRowIndex = -1;
            OnHoldCellSubmit(clickedCell);
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

        private int FindHomeCellAtReferencePos(Vector2 refPos)
        {
            for (int i = 0; i < _homeSlotCount; i++)
            {
                int col = i % GridColumns;
                int row = i / GridColumns;
                var rect = new Rect(
                    ShelfGridX + col * (CellSize + CellGapX),
                    ShelfGridY + row * (CellSize + CellGapY),
                    CellSize,
                    CellSize);
                if (rect.Contains(refPos))
                {
                    return i;
                }
            }

            return -1;
        }

        private int FindSocketAtReferencePos(Vector2 refPos)
        {
            float hitRadius = BubbleRadius * 1.45f;
            float bestDistSq = hitRadius * hitRadius;
            int best = -1;
            for (int i = 0; i < OrbitalSocketCount; i++)
            {
                float dSq = (OrbitalCenters[i] - refPos).sqrMagnitude;
                if (dSq <= bestDistSq)
                {
                    bestDistSq = dSq;
                    best = i;
                }
            }

            return best;
        }

        private void OnHoldCellSubmit(int index)
        {
            if (index < 0 || index >= _unlockedHoldCount)
            {
                return;
            }

            _lastSubmitFrame = Time.frameCount;
            EnsureInteractiveDuringBoot();
            HoldCellState state = _holdStates[index];
            if (_dock.IsDocked)
            {
                if (state.Kind == HoldCellKind.Material &&
                    _transfer.TryMove(state.Material, true, out int moved) && moved > 0)
                {
                    _events.Publish(new StackTransferred(state.Material, moved, true));
                    _events.Publish(new UiCue(UiCueKind.Confirm));
                    Refresh();
                }

                return;
            }

            if (state.Kind == HoldCellKind.WaitingKit)
            {
                TryFitKitAtRow(state.RowIndex);
            }
        }

        private void OnHomeCellSubmit(int index)
        {
            if (!_dock.IsDocked || index < 0 || index >= _homeSlotCount)
            {
                return;
            }

            _lastSubmitFrame = Time.frameCount;
            EnsureInteractiveDuringBoot();
            MaterialSlot slot = _home.GetSlot(index);
            if (!slot.IsEmpty && _transfer.TryMove(slot.Material, false, out int moved) && moved > 0)
            {
                _events.Publish(new StackTransferred(slot.Material, moved, false));
                _events.Publish(new UiCue(UiCueKind.Confirm));
                Refresh();
            }
        }

        private void OnSocketSubmit(int orbitalIdx)
        {
            if (_dock.IsDocked || orbitalIdx < 0 || orbitalIdx >= OrbitalSocketCount)
            {
                return;
            }

            _lastSubmitFrame = Time.frameCount;
            EnsureInteractiveDuringBoot();
            RoverSocket socket = OrbitalSockets[orbitalIdx];
            if (!_equipment.TryGetFitted(socket, out RoverKitPiece fitted))
            {
                // If a waiting kit for this socket sits in the hold, allow fitting it directly when submitting its socket.
                for (int r = 0; r < _equipment.RowCount; r++)
                {
                    if (_equipment.SocketOf(_equipment.RowPart(r)) == socket)
                    {
                        TryFitKitAtRow(r);
                        return;
                    }
                }

                return;
            }

            if (!_equipment.CanFit(fitted))
            {
                ShowTransientToast(UiKeys.CargoFitAtBay);
                return;
            }

            if (_equipment.RowCount >= _equipment.RowCapacity)
            {
                ShowTransientToast(UiKeys.RowFull);
                return;
            }

            if (_equipment.TryUnequip(socket))
            {
                _events.Publish(new UiCue(UiCueKind.PartDrop));
                Refresh();
            }
        }

        private bool TryFitKitAtRow(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= _equipment.RowCount)
            {
                return false;
            }

            RoverKitPiece part = _equipment.RowPart(rowIndex);
            if (!_equipment.CanFit(part) || !_equipment.TryFit(rowIndex))
            {
                ShowTransientToast(UiKeys.CargoFitAtBay);
                return false;
            }

            _events.Publish(new UiCue(UiCueKind.Confirm));
            WantsClose = true;
            Refresh();
            return true;
        }

        private void ShowTransientToast(string key)
        {
            _transientToastKey = key;
            _transientToastTimer = ToastDurationSeconds;
            UpdateToast();
            _canvas.MarkDirtyRepaint();
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

        private void OnSocketNavigationMove(int orbitalIdx, NavigationMoveEvent evt)
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
            NavigateFromSocket(orbitalIdx, evt.direction);
        }

        private void OnHomeNavigationMove(int index, NavigationMoveEvent evt)
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
            NavigateFromHomeCell(index, evt.direction);
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
                    else if (!_dock.IsDocked)
                    {
                        FocusSocket(RoverSocket.Deck);
                    }
                    else if (_homeSlotCount > 0)
                    {
                        FocusHomeCell(0);
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

        private void NavigateFromSocket(int orbitalIdx, NavigationMoveEvent.Direction direction)
        {
            RoverSocket current = OrbitalSockets[orbitalIdx];
            switch (direction)
            {
                case NavigationMoveEvent.Direction.Up:
                    switch (current)
                    {
                        case RoverSocket.Cutter:
                        case RoverSocket.Back:
                            FocusSocket(RoverSocket.Wing);
                            break;
                        case RoverSocket.Tether:
                            FocusSocket(RoverSocket.Cutter);
                            break;
                        case RoverSocket.Lamp:
                            FocusSocket(RoverSocket.Tether);
                            break;
                        case RoverSocket.Flanks:
                            FocusSocket(RoverSocket.Lamp);
                            break;
                        case RoverSocket.Belly:
                            FocusSocket(RoverSocket.Flanks);
                            break;
                        case RoverSocket.Panniers:
                            FocusSocket(RoverSocket.Hold);
                            break;
                        case RoverSocket.Hold:
                            FocusSocket(RoverSocket.Deck);
                            break;
                        case RoverSocket.Deck:
                            FocusSocket(RoverSocket.Back);
                            break;
                    }

                    break;

                case NavigationMoveEvent.Direction.Down:
                    switch (current)
                    {
                        case RoverSocket.Wing:
                            FocusSocket(RoverSocket.Back);
                            break;
                        case RoverSocket.Back:
                            FocusSocket(RoverSocket.Deck);
                            break;
                        case RoverSocket.Deck:
                            FocusSocket(RoverSocket.Hold);
                            break;
                        case RoverSocket.Hold:
                            FocusSocket(RoverSocket.Panniers);
                            break;
                        case RoverSocket.Panniers:
                            FocusSocket(RoverSocket.Belly);
                            break;
                        case RoverSocket.Cutter:
                            FocusSocket(RoverSocket.Tether);
                            break;
                        case RoverSocket.Tether:
                            FocusSocket(RoverSocket.Lamp);
                            break;
                        case RoverSocket.Lamp:
                            FocusSocket(RoverSocket.Flanks);
                            break;
                        case RoverSocket.Flanks:
                            FocusSocket(RoverSocket.Belly);
                            break;
                        case RoverSocket.Belly:
                            FocusSocket(RoverSocket.Panniers);
                            break;
                    }

                    break;

                case NavigationMoveEvent.Direction.Right:
                    switch (current)
                    {
                        case RoverSocket.Back:
                            FocusSocket(RoverSocket.Wing);
                            break;
                        case RoverSocket.Wing:
                            FocusSocket(RoverSocket.Cutter);
                            break;
                        case RoverSocket.Cutter:
                            FocusSocket(RoverSocket.Tether);
                            break;
                        case RoverSocket.Tether:
                        case RoverSocket.Deck:
                        case RoverSocket.Flanks:
                            FocusSocket(RoverSocket.Lamp);
                            break;
                        case RoverSocket.Hold:
                            FocusSocket(RoverSocket.Panniers);
                            break;
                        case RoverSocket.Panniers:
                            FocusSocket(RoverSocket.Belly);
                            break;
                        case RoverSocket.Belly:
                            FocusSocket(RoverSocket.Flanks);
                            break;
                    }

                    break;

                case NavigationMoveEvent.Direction.Left:
                    switch (current)
                    {
                        case RoverSocket.Back:
                        case RoverSocket.Deck:
                        case RoverSocket.Hold:
                            FocusHoldCell(Mathf.Min(_unlockedHoldCount - 1, 3));
                            break;
                        case RoverSocket.Wing:
                            FocusSocket(RoverSocket.Back);
                            break;
                        case RoverSocket.Cutter:
                        case RoverSocket.Tether:
                            FocusSocket(RoverSocket.Wing);
                            break;
                        case RoverSocket.Lamp:
                            FocusSocket(RoverSocket.Deck);
                            break;
                        case RoverSocket.Flanks:
                            FocusSocket(RoverSocket.Belly);
                            break;
                        case RoverSocket.Belly:
                            FocusSocket(RoverSocket.Panniers);
                            break;
                        case RoverSocket.Panniers:
                            FocusSocket(RoverSocket.Hold);
                            break;
                    }

                    break;
            }
        }

        private void NavigateFromHomeCell(int index, NavigationMoveEvent.Direction direction)
        {
            int col = index % GridColumns;
            int row = index / GridColumns;
            switch (direction)
            {
                case NavigationMoveEvent.Direction.Right:
                    if (col + 1 < GridColumns && index + 1 < _homeSlotCount)
                    {
                        FocusHomeCell(index + 1);
                    }

                    break;
                case NavigationMoveEvent.Direction.Left:
                    if (col > 0)
                    {
                        FocusHomeCell(index - 1);
                    }
                    else if (_unlockedHoldCount > 0)
                    {
                        FocusHoldCell(Mathf.Min(_unlockedHoldCount - 1, row * GridColumns + GridColumns - 1));
                    }

                    break;
                case NavigationMoveEvent.Direction.Down:
                    if ((row + 1) * GridColumns + col < _homeSlotCount)
                    {
                        FocusHomeCell((row + 1) * GridColumns + col);
                    }

                    break;
                case NavigationMoveEvent.Direction.Up:
                    if (row > 0)
                    {
                        FocusHomeCell((row - 1) * GridColumns + col);
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
            if (_isDraggingKit)
            {
                string name = PartInfo.PartName(_dragKitPiece, _localization);
                string desc = PartInfo.PartDescription(_dragKitPiece, _localization);
                ShowTooltipBox(name, desc, 512f * s, 664f * s, 440f * s, 568f * s, 62f * s);
                return;
            }

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

            if (!_dock.IsDocked && _focusedSocketIndex >= 0 && _focusedSocketIndex < OrbitalSocketCount)
            {
                RoverSocket socket = OrbitalSockets[_focusedSocketIndex];
                string name = _equipment.TryGetFitted(socket, out RoverKitPiece fitted)
                    ? PartInfo.PartName(fitted, _localization)
                    : PartInfo.SocketName(socket, _localization);
                string desc = _equipment.TryGetFitted(socket, out RoverKitPiece fittedDesc)
                    ? PartInfo.PartDescription(fittedDesc, _localization)
                    : PartInfo.SocketDescription(socket, _localization);
                ShowTooltipBox(name, desc, 512f * s, 664f * s, 440f * s, 568f * s, 62f * s);
                return;
            }

            if (_dock.IsDocked && _focusedHomeCell >= 0 && _focusedHomeCell < _homeSlotCount)
            {
                MaterialSlot slot = _home.GetSlot(_focusedHomeCell);
                if (!slot.IsEmpty)
                {
                    string name = PartInfo.MaterialName(slot.Material, _localization);
                    string desc = _localization.Get(UiKeys.TransferToTrip);
                    ShowTooltipBox(name, desc, 512f * s, 664f * s, 440f * s, 568f * s, 62f * s);
                    return;
                }
            }

            _tooltip.style.display = DisplayStyle.None;
        }

        private string HoldItemDescription(int index)
        {
            HoldCellState state = _holdStates[index];
            if (_dock.IsDocked && state.Kind == HoldCellKind.Material)
            {
                return _localization.Get(UiKeys.TransferToHome);
            }

            switch (state.Kind)
            {
                case HoldCellKind.Material:
                    return PartInfo.MaterialDescription(state.Material, _localization);
                case HoldCellKind.StoryPart:
                    return PartInfo.StoryPartDescription(state.StoryPartId, _localization);
                case HoldCellKind.WaitingKit:
                    return PartInfo.PartDescription(state.KitPiece, _localization);
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
            if (!string.IsNullOrEmpty(_transientToastKey))
            {
                _toastText.text = _localization.Get(_transientToastKey);
                _toast.style.display = DisplayStyle.Flex;
                return;
            }

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
                RoverLineArtPainter.DrawLine(painter, new Vector2(ShelfGridX * s, 64f * s),
                    new Vector2(1080f * s, 64f * s), AmberMuted, 1.2f);
            }

            DrawHoldGrid(painter, s);

            if (!hideForBoot)
            {
                if (_dock.IsDocked)
                {
                    DrawHomeGrid(painter, s);
                }
                else
                {
                    DrawRoverAndSockets(painter, s);
                }

                DrawTooltipOverlay(painter, s);
                DrawDragOverlay(painter, s);
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
                bool draggedSource = _isDraggingKit && i == _dragHoldCell;
                bool hasItem = state.Kind != HoldCellKind.Empty;

                RoverLineArtPainter.FillRect(painter, cx, cy, cw, ch, focused ? BgCellFocus : BgCell);
                if (focused)
                {
                    RoverLineArtPainter.StrokeRect(painter, cx, cy, cw, ch, Cyan, 2.0f);
                    RoverLineArtPainter.DrawCornerTicks(painter, cx, cy, cw, ch, 8f * s, Cyan, 2.0f);
                }
                else if (draggedSource)
                {
                    RoverLineArtPainter.StrokeDashedRect(painter, cx, cy, cw, ch, CyanDim, 1.5f, 5f * s, 4f * s);
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
                Color32 tint = draggedSource
                    ? UiIconPainter.FaintAmber
                    : RoverLineArtPainter.WithAlpha(UiIconPainter.LitAmber, (byte)Mathf.RoundToInt(248f * bootProgress));
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
                case HoldCellKind.WaitingKit:
                    return _diagram.GetIcon(state.KitPiece);
                default:
                    throw new InvalidOperationException($"Cell kind {state.Kind} has no icon.");
            }
        }

        private void DrawHomeGrid(Painter2D painter, float s)
        {
            for (int i = 0; i < _homeSlotCount; i++)
            {
                int col = i % GridColumns;
                int row = i / GridColumns;
                float cx = (ShelfGridX + col * (CellSize + CellGapX)) * s;
                float cy = (ShelfGridY + row * (CellSize + CellGapY)) * s;
                float cw = CellSize * s;
                float ch = CellSize * s;

                MaterialSlot slot = _home.GetSlot(i);
                bool focused = i == _focusedHomeCell;
                RoverLineArtPainter.FillRect(painter, cx, cy, cw, ch, focused ? BgCellFocus : BgCell);
                RoverLineArtPainter.StrokeRect(painter, cx, cy, cw, ch,
                    focused ? Cyan : (!slot.IsEmpty ? AmberFaint : AmberMuted), focused ? 2.0f : 1.5f);

                if (!slot.IsEmpty)
                {
                    UiIconShape icon = _diagram.GetIcon(slot.Material);
                    UiIconPainter.Draw(painter, in icon, new Vector2(cx + cw * 0.5f, cy + ch * 0.5f - 2f * s),
                        60f * s, UiIconPainter.LitAmber);
                    if (slot.Units > 1)
                    {
                        RoverLineArtPainter.FillRect(painter, cx + cw - 34f * s, cy + ch - 28f * s, 32f * s, 26f * s,
                            focused ? BgCellFocus : BgCell);
                    }
                }
            }
        }

        private void DrawRoverAndSockets(Painter2D painter, float s)
        {
            uint mask = 0u;
            for (int i = 0; i < OrbitalSocketCount; i++)
            {
                RoverSocket socket = OrbitalSockets[i];
                if (_equipment.TryGetFitted(socket, out _))
                {
                    mask |= 1u << (int)socket;
                }
            }

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
                InstalledSocketsMask = mask,
            };

            RoverLineArtPainter.DrawRover07(painter, roverOrigin, roverScale, in pose, null, _settings);

            int activeSocket = _isDraggingKit ? _dragTargetSocketIndex : _focusedSocketIndex;
            float r = _settings.BubbleRadius * s;

            // 1. Leader lines from orbital bubbles to 07 mount anchors:
            //    default (empty) = faint, installed = subtle amber, focused or active drag-target = crisp cyan.
            for (int pass = 0; pass < 2; pass++)
            {
                bool drawCyanPass = pass == 1;
                for (int i = 0; i < OrbitalSocketCount; i++)
                {
                    bool isCyanLeader = i == activeSocket;
                    if (isCyanLeader != drawCyanPass)
                    {
                        continue;
                    }

                    RoverSocket socket = OrbitalSockets[i];
                    bool installed = (mask & (1u << (int)socket)) != 0u;
                    Vector2 b = OrbitalCenters[i] * s;
                    Vector2 m = RoverLineArtPainter.GetSocketMountPoint(socket, roverOrigin, roverScale,
                        pose.Pitch, pose.HeadNod, _settings.RoverBodyPivotHeight, _settings.RoverWingTilt);
                    Vector2 dir = m - b;
                    float dist = dir.magnitude;
                    if (dist <= r + 2f)
                    {
                        continue;
                    }

                    Vector2 p0 = b + (dir / dist) * r;
                    if (isCyanLeader)
                    {
                        Color32 focusCol = _settings.LeaderFocusColor;
                        RoverLineArtPainter.DrawLine(painter, p0, m, focusCol, _settings.LeaderFocusWidth * s);
                        RoverLineArtPainter.FillCircle(painter, m, _settings.LeaderFocusDotRadius * s, focusCol);
                    }
                    else if (installed)
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

            // 2. 10 orbital socket bubbles
            for (int i = 0; i < OrbitalSocketCount; i++)
            {
                RoverSocket socket = OrbitalSockets[i];
                Vector2 b = OrbitalCenters[i] * s;
                bool installed = (mask & (1u << (int)socket)) != 0u;
                bool focused = i == activeSocket;

                RoverLineArtPainter.FillCircle(painter, b, r, BgCell);
                if (focused)
                {
                    RoverLineArtPainter.StrokeCircle(painter, b, r, Cyan, 2.2f);
                }
                else if (installed)
                {
                    RoverLineArtPainter.StrokeCircle(painter, b, r, AmberBright, 2.0f);
                    RoverLineArtPainter.StrokeCircle(painter, b, r - 3.5f * s,
                        RoverLineArtPainter.WithAlpha(AmberText, 120), 1.0f);
                }
                else
                {
                    RoverLineArtPainter.StrokeCircle(painter, b, r, AmberMuted, 1.3f);
                }

                UiIconShape icon = _diagram.GetIcon(socket);
                UiIconState iconState = (_isDraggingKit && focused)
                    ? UiIconState.Cyan
                    : (installed || focused ? UiIconState.Lit : UiIconState.Faint);
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

            if (_isDraggingKit && _dragTargetSocketIndex >= 0 && _dragTargetSocketIndex < OrbitalSocketCount)
            {
                Vector2 b = OrbitalCenters[_dragTargetSocketIndex] * s;
                Vector2 from = new Vector2(b.x, b.y + _settings.BubbleRadius * s);
                Vector2 to = new Vector2(Mathf.Clamp(b.x, tx + 24f * s, tx + tw - 24f * s), ty);
                RoverLineArtPainter.DrawLine(painter, from, to, RoverLineArtPainter.WithAlpha(Cyan, 195), 1.5f);
            }
            else if (_focusedHoldCell >= 0 && _focusedHoldCell < _unlockedHoldCount)
            {
                Rect cell = GetHoldCellReferenceRect(_focusedHoldCell);
                float tickX = Mathf.Clamp(cell.center.x * s, tx + 16f * s, tx + tw - 16f * s);
                RoverLineArtPainter.DrawLine(painter, new Vector2(tickX, ty - 12f * s), new Vector2(tickX, ty),
                    RoverLineArtPainter.WithAlpha(Cyan, 195), 1.5f);
            }
            else if (!_dock.IsDocked && _focusedSocketIndex >= 0 && _focusedSocketIndex < OrbitalSocketCount)
            {
                Vector2 b = OrbitalCenters[_focusedSocketIndex];
                if (b.y > 460f)
                {
                    Vector2 bs = b * s;
                    Vector2 from = new Vector2(bs.x, bs.y + _settings.BubbleRadius * s);
                    Vector2 to = new Vector2(Mathf.Clamp(bs.x, tx + 24f * s, tx + tw - 24f * s), ty);
                    RoverLineArtPainter.DrawLine(painter, from, to, RoverLineArtPainter.WithAlpha(Cyan, 175), 1.3f);
                }
            }
        }

        private void DrawDragOverlay(Painter2D painter, float s)
        {
            if (!_isDraggingKit || _dragHoldCell < 0)
            {
                return;
            }

            Vector2 start = _dragStartRefPos * s;
            Vector2 cur = _dragCurrentRefPos * s;
            Vector2 ctrl = new Vector2((start.x + cur.x) * 0.5f, Mathf.Max(start.y, cur.y) + 42f * s);
            RoverLineArtPainter.StrokeDashedQuadratic(painter, start, ctrl, cur,
                RoverLineArtPainter.WithAlpha(Cyan, 195), 1.8f * s, 6f * s, 4f * s);
            RoverLineArtPainter.FillCircle(painter, start, 4.0f * s, RoverLineArtPainter.WithAlpha(Cyan, 185));

            float half = 34f * s;
            RoverLineArtPainter.FillRect(painter, cur.x - half, cur.y - half, half * 2f, half * 2f, BgPlate);
            RoverLineArtPainter.StrokeRect(painter, cur.x - half, cur.y - half, half * 2f, half * 2f, Cyan, 2.0f);
            RoverLineArtPainter.DrawCornerTicks(painter, cur.x - half, cur.y - half, half * 2f, half * 2f,
                7f * s, Cyan, 2.0f);

            UiIconShape icon = _diagram.GetIcon(_dragKitPiece);
            UiIconPainter.Draw(painter, in icon, cur, 48f * s, UiIconState.Cyan);
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
            public readonly RoverKitPiece KitPiece;
            public readonly int RowIndex;

            private HoldCellState(HoldCellKind kind, SalvageMaterial material, int units, string storyPartId,
                RoverKitPiece kitPiece, int rowIndex)
            {
                Kind = kind;
                Material = material;
                Units = units;
                StoryPartId = storyPartId;
                KitPiece = kitPiece;
                RowIndex = rowIndex;
            }

            public static HoldCellState ForEmpty() => new HoldCellState(HoldCellKind.Empty, default, 0, null, default, -1);

            public static HoldCellState ForLocked() => new HoldCellState(HoldCellKind.Locked, default, 0, null, default, -1);

            public static HoldCellState ForMaterial(SalvageMaterial material, int units) =>
                new HoldCellState(HoldCellKind.Material, material, units, null, default, -1);

            public static HoldCellState ForStoryPart(string partId) =>
                new HoldCellState(HoldCellKind.StoryPart, default, 1, partId, default, -1);

            public static HoldCellState ForWaitingKit(RoverKitPiece piece, int rowIndex) =>
                new HoldCellState(HoldCellKind.WaitingKit, default, 1, null, piece, rowIndex);
        }
    }
}
