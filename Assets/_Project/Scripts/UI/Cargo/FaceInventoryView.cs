using System;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;

namespace MoonProject.UI
{
    /// <summary>What occupies one cell of the hold grid on <see cref="FaceInventoryView"/>.</summary>
    public enum HoldCellKind
    {
        Empty = 0,
        Locked = 1,
        Material = 2,
        StoryPart = 3,
    }

    /// <summary>
    /// 07's screen on its CRT face (<c>FACE-INV view</c>, <see cref="FaceLayout"/>): a flat <c>1152x768</c> (1.5:1)
    /// equipment diagnostic. The left half is the hold, a grid of four columns (up to 24 slots, three rows to a page)
    /// with each item named under its slot, one hint row where the hold will grow and the info box under it. The right
    /// half is Art's three-quarter line drawing of 07 with its four fixed devices (<see cref="FaceDevices"/>: the scan
    /// lens, the cutter and two sockets) in labelled frames above and below it, a leader from each to its part, and
    /// each state in its own colour and mark. While the spare igniter is in the hold and 07 stands on its pad, the
    /// cutter offers it and a confirm fits it. With the mouse a part can also be dragged out of the hold
    /// (<see cref="HoldDrag"/>) and dropped on a device: the cutter takes the igniter on the pad; any other drop eases
    /// the part home and the info box says why (<see cref="DropRule"/>).
    /// </summary>
    public sealed class FaceInventoryView
    {
        public const float ReferenceWidth = 1152f;
        public const float ReferenceHeight = 768f;
        public const float ReferenceAspect = ReferenceWidth / ReferenceHeight;

        public const int MaxHoldRows = 6;
        public const int MaxHoldCells = FaceLayout.Columns * MaxHoldRows;

        public const float CellSize = FaceLayout.CellSize;

        public const int MaxTooltipDescriptionChars = 72;

        private const string WarningIconId = "icon_warning";
        private const float CellTickLength = 10f;
        private const float FrameTickLength = 14f;
        private const float FrameRuleInset = 8f;
        private const float ToastDotX = 8f;
        private const float ToastDotRadius = 4f;
        private const float BadgeWidth = 28f;
        private const float BadgeHeight = 23f;
        private const float BadgeInsetX = 30f;
        private const float BadgeInsetY = 25f;
        private const float FullIconAlpha = 248f;

        // Two-colour amber CRT palette + cyan focus accent read from CargoScreenSettings.
        private Color32 BgCell => _settings.CellBackground;
        private Color32 AmberText => _settings.AmberText;
        private Color32 AmberFaint => _settings.AmberFaint;
        private Color32 AmberMuted => _settings.AmberMuted;
        private Color32 AmberLocked => _settings.AmberLocked;
        private Color32 Cyan => _settings.CyanAccent;

        private readonly VisualElement _panel;
        private readonly VisualElement _art;
        private readonly VisualElement _canvas;
        private readonly VisualElement _ghostLayer;
        private readonly Label _page;
        private readonly Label _holdTitle;
        private readonly Label _title;
        private readonly Label _bootLabel;
        private readonly VisualElement _holdGrid;
        private readonly VisualElement _socketsRoot;
        private readonly VisualElement _tooltip;
        private readonly Label _tooltipName;
        private readonly Label _tooltipDesc;
        private readonly VisualElement _toast;
        private readonly Label _toastText;
        private readonly Label _more;
        private readonly VisualElement _footerRoot;
        private readonly Label[] _footerKeys = new Label[FaceKeyHints.Count];
        private readonly Label[] _footerWords = new Label[FaceKeyHints.Count];

        private readonly CargoScreenSettings _settings;
        private readonly ILocalization _localization;
        private readonly ICargoHold _hold;
        private readonly IRoverSystems _systems;
        private readonly RoverDiagramData _diagram;
        private readonly IntText _numbers;
        private readonly EventBus _events;

        private readonly VisualElement[] _holdCells = new VisualElement[MaxHoldCells];
        private readonly Label[] _holdBadges = new Label[MaxHoldCells];
        private readonly Label[] _holdNames = new Label[MaxHoldCells];
        private readonly HoldCellState[] _holdStates = new HoldCellState[MaxHoldCells];

        private readonly VisualElement[] _deviceBubbles = new VisualElement[FaceDevices.Count];
        private readonly Label[] _deviceNames = new Label[FaceDevices.Count];
        private readonly Label[] _deviceStates = new Label[FaceDevices.Count];
        private readonly RoverSystemState[] _deviceStateOf = new RoverSystemState[FaceDevices.Count];
        private readonly UiIconShape[] _deviceIcons = new UiIconShape[FaceDevices.Count];
        private readonly Vector2[] _leaderTargets = new Vector2[FaceDevices.Count];
        private readonly UiIconShape _warningIcon;

        private int _unlockedHoldCount;
        private int _holdPage;
        private int _focusedHoldCell = -1;
        private int _focusedDevice = -1;
        private bool _igniterOffered;
        private InputDeviceKind _footerDevice = (InputDeviceKind)(-1);
        private float _pressFlash;
        private int _pressedHoldCell = -1;
        private int _pressedDevice = -1;

        private int _lastPointerDownFrame = -1;
        private int _lastNavigationFrame = -1;
        private readonly InputHandoff _handoff = new InputHandoff();
        private readonly HoldDrag _drag;
        private UiIconShape _dragIcon;
        private int _dropReasonCell = -1;
        private string _dropReasonKey;
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
            _drag = new HoldDrag(settings);

            _panel.pickingMode = PickingMode.Position;
            _art = Require<VisualElement>(_panel, "cargo-art");
            _canvas = Require<VisualElement>(_panel, "cargo-canvas");
            _page = Require<Label>(_panel, "cargo-page");
            _ghostLayer = Require<VisualElement>(_panel, "cargo-ghost");
            _holdTitle = Require<Label>(_panel, "cargo-hold-title");
            _title = Require<Label>(_panel, "cargo-right-title");
            _bootLabel = Require<Label>(_panel, "cargo-boot");
            _holdGrid = Require<VisualElement>(_panel, "cargo-hold-grid");
            _socketsRoot = Require<VisualElement>(_panel, "cargo-sockets");
            _tooltip = Require<VisualElement>(_panel, "cargo-tooltip");
            _tooltipName = Require<Label>(_panel, "cargo-tooltip-name");
            _tooltipDesc = Require<Label>(_panel, "cargo-tooltip-desc");
            _toast = Require<VisualElement>(_panel, "cargo-toast");
            _toastText = Require<Label>(_panel, "cargo-toast-text");
            _more = Require<Label>(_panel, "cargo-more");
            _footerRoot = Require<VisualElement>(_panel, "cargo-footer");
            for (int i = 0; i < FaceKeyHints.Count; i++)
            {
                _footerKeys[i] = Require<Label>(_panel, "cargo-key-" + i);
                _footerWords[i] = Require<Label>(_panel, "cargo-word-" + i);
            }

            Texture2D art = diagram.FaceArt;
            if (art == null)
            {
                throw new InvalidOperationException("RoverDiagramData has no face line drawing (run UI/Rover Diagram).");
            }

            _art.style.backgroundImage = new StyleBackground(art);
            _art.style.unityBackgroundImageTintColor = (Color)settings.ArtTint;
            for (int i = 0; i < FaceDevices.Count; i++)
            {
                _deviceIcons[i] = diagram.GetIcon(FaceDevices.IconId(i));
                _leaderTargets[i] = FaceLayout.ArtPoint(diagram.FaceAnchor(i), diagram.FaceArtContent);
            }

            _warningIcon = diagram.GetIcon(WarningIconId);
            _page.pickingMode = PickingMode.Position;
            _page.RegisterCallback<PointerDownEvent>(OnPagePointerDown);

            _canvas.generateVisualContent += OnGenerateCanvas;
            _ghostLayer.generateVisualContent += OnGenerateGhost;
            BuildHoldCells();
            BuildDeviceBubbles();

            _panel.RegisterCallback<PointerMoveEvent>(OnPanelPointerMove);
            _panel.RegisterCallback<PointerUpEvent>(OnPanelPointerUp);
        }

        /// <summary>Unlocked hold cell count (typically 8, 16, or 24).</summary>
        public int UnlockedCellCount => _unlockedHoldCount;

        /// <summary>Locked hold cell count (<c>24 - UnlockedCellCount</c>).</summary>
        public int LockedCellCount => MaxHoldCells - _unlockedHoldCount;

        /// <summary>The currently focused or hovered hold cell index (0..23), or -1.</summary>
        public int FocusedHoldCell => _focusedHoldCell;

        /// <summary>The page of the hold shown (0 or 1: a page is three rows of four).</summary>
        public int HoldPage => _holdPage;

        /// <summary>The page count words on the hold's heading row ("1/2"), empty while the hold fits one page.</summary>
        public string PageText => _page.style.display == DisplayStyle.Flex ? _page.text : string.Empty;

        /// <summary>The currently focused or hovered device index (0..3, see <see cref="FaceDevices"/>), or -1.</summary>
        public int FocusedDeviceIndex => _focusedDevice;

        /// <summary>True while the cutter socket offers the spare igniter.</summary>
        public bool IsIgniterOffered => _igniterOffered;

        internal int LastNavigationFrame => _lastNavigationFrame;

        /// <summary>Where a drag of a hold part stands (tests and captures).</summary>
        internal HoldDragPhase DragPhase => _drag.Phase;

        /// <summary>The dragged icon's centre in the 1152x768 reference frame (tests and captures).</summary>
        internal Vector2 DragGhost => _drag.Ghost;

        public string LeftHeaderText => _holdTitle.text;

        public string RightHeaderText => _title.text;

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

        /// <summary>The state words in device <paramref name="device"/>'s frame.</summary>
        public string DeviceStateText(int device) => _deviceStates[device].text;

        public Label DeviceStateLabel(int device) => _deviceStates[device];

        /// <summary>
        /// Returns the reference <c>1152x768</c> rectangle of hold cell <paramref name="index"/> (0..23) on its page.
        /// </summary>
        public static Rect GetHoldCellReferenceRect(int index)
        {
            return FaceLayout.HoldCellRect(index);
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
                _title.style.display = DisplayStyle.Flex;
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
            _pressFlash = 0f;
            _userInteractedDuringBoot = false;
            _handoff.Reset();
            _drag.Clear();
            ClearDropReason();
        }

        /// <param name="deltaTime">Unscaled seconds.</param>
        /// <param name="device">The active device: the foot row names its real keys.</param>
        public void Tick(float deltaTime, InputDeviceKind device)
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

            if (device != _footerDevice)
            {
                WriteFooter(device);
            }

            if (_pressFlash > 0f)
            {
                _pressFlash = Mathf.Max(0f, _pressFlash - deltaTime / _settings.PressFlashSeconds);
                _canvas.MarkDirtyRepaint();
            }

            if (_drag.ShowsGhost)
            {
                _drag.Step(deltaTime);
                _canvas.MarkDirtyRepaint();
                _ghostLayer.MarkDirtyRepaint();
            }
        }

        /// <summary>The foot row: each hint's keycap on <paramref name="device"/> and the word for what it does.</summary>
        private void WriteFooter(InputDeviceKind device)
        {
            _footerDevice = device;
            for (int i = 0; i < FaceKeyHints.Count; i++)
            {
                var hint = (FaceHint)i;
                _footerKeys[i].text = _localization.Get(FaceKeyHints.CapKey(hint, device));
                _footerWords[i].text = _localization.Get(FaceKeyHints.WordKey(hint));
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
                _title.style.display = DisplayStyle.None;
                _socketsRoot.style.display = DisplayStyle.None;
                _tooltip.style.display = DisplayStyle.None;
                _more.style.display = DisplayStyle.None;
                _page.style.display = DisplayStyle.None;
                _art.style.display = DisplayStyle.None;
                _footerRoot.style.display = DisplayStyle.None;
                for (int i = 0; i < MaxHoldCells; i++)
                {
                    bool showBadge = i < visibleSlots && i < filledSlots && _holdStates[i].Kind == HoldCellKind.Material &&
                                     _holdStates[i].Units > 1 && boot != null && boot.ItemProgress(i) > 0.01f;
                    _holdBadges[i].style.display = showBadge ? DisplayStyle.Flex : DisplayStyle.None;
                    if (showBadge)
                    {
                        _holdBadges[i].style.opacity = boot.ItemProgress(i);
                    }

                    _holdNames[i].style.display = DisplayStyle.None;
                }
            }
            else
            {
                _bootLabel.style.display = DisplayStyle.None;
                _holdTitle.style.display = DisplayStyle.Flex;
                _title.style.display = DisplayStyle.Flex;
                _footerRoot.style.display = DisplayStyle.Flex;
                _art.style.display = DisplayStyle.Flex;
                for (int i = 0; i < MaxHoldCells; i++)
                {
                    _holdBadges[i].style.opacity = 1f;
                    WriteHoldName(i);
                }

                WritePage(_scale);
            }

            _canvas.MarkDirtyRepaint();
        }

        public void ApplyScale(float panelWidth, float panelHeight)
        {
            float s = Mathf.Max(0.25f, Mathf.Min(panelWidth / ReferenceWidth, panelHeight / ReferenceHeight));
            _scale = s;

            PositionBox(_title, FaceLayout.Margin * s, FaceLayout.TitleY * s,
                (FaceLayout.RightEdge - FaceLayout.Margin) * s, FaceLayout.TitleHeight * s);
            PositionElement(_bootLabel, FaceLayout.Margin * s, FaceLayout.TitleY * s);
            PositionRect(_art, FaceLayout.ArtImageRect(_diagram.FaceArtContent), s);
            PositionRect(_holdTitle, FaceLayout.HeadingRect, s);

            for (int i = 0; i < MaxHoldCells; i++)
            {
                Rect r = FaceLayout.HoldCellRect(i);
                PositionBox(_holdCells[i], r.x * s, r.y * s, r.width * s, r.height * s);
                Rect name = FaceLayout.HoldNameRect(i);
                PositionBox(_holdNames[i], (name.x - r.x) * s, (name.y - r.y) * s, name.width * s, name.height * s);
            }

            for (int i = 0; i < FaceDevices.Count; i++)
            {
                Rect frame = FaceLayout.DeviceFrame(i);
                PositionBox(_deviceBubbles[i], frame.x * s, frame.y * s, frame.width * s, frame.height * s);
                PositionBox(_deviceNames[i], 0f, 0f, (frame.width - FaceLayout.FrameGlyphRoom) * s,
                    FaceLayout.FrameNameHeight * s);
                PositionBox(_deviceStates[i], FaceLayout.FrameTextX * s, FaceLayout.FrameNameHeight * s,
                    (frame.width - FaceLayout.FrameTextX - FaceLayout.FrameEdgeInset) * s,
                    (frame.height - FaceLayout.FrameNameHeight) * s);
            }

            PositionBox(_tooltip, FaceLayout.InfoX * s, FaceLayout.InfoY * s, FaceLayout.InfoWidth * s,
                FaceLayout.InfoHeight * s);
            PositionBox(_toast, FaceLayout.Margin * s, FaceLayout.ToastY * s, FaceLayout.HoldWidth * s,
                FaceLayout.ToastHeight * s);
            PositionBox(_footerRoot, FaceLayout.Margin * s, FaceLayout.FooterY * s,
                (FaceLayout.RightEdge - FaceLayout.Margin) * s, FaceLayout.FooterHeight * s);

            WritePage(s);
            UpdateTooltip();
            _canvas.MarkDirtyRepaint();
        }

        public void Refresh()
        {
            bool hideForBoot = HideForBoot;
            _holdTitle.text = _localization.Get(UiKeys.CargoHold);
            _title.text = _localization.Get(UiKeys.FaceTitle);

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
            _holdPage = Mathf.Clamp(_holdPage, 0, FaceLayout.PageCount(_unlockedHoldCount) - 1);
            for (int i = _unlockedHoldCount; i < MaxHoldCells; i++)
            {
                _holdStates[i] = HoldCellState.ForLocked();
            }

            for (int i = 0; i < MaxHoldCells; i++)
            {
                HoldCellState state = _holdStates[i];
                bool isUnlocked = i < _unlockedHoldCount;
                _holdCells[i].focusable = isUnlocked;
                _holdCells[i].EnableInClassList(CargoScreen.EmptyClass, state.Kind == HoldCellKind.Empty || state.Kind == HoldCellKind.Locked);
                _holdCells[i].EnableInClassList(CargoScreen.LockedClass, state.Kind == HoldCellKind.Locked);
                WriteHoldName(i);

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
                _deviceStateOf[i] = state;
                _deviceNames[i].text = _localization.Get(FaceDevices.NameKey(i));
                _deviceStates[i].text = _localization.Get(SystemText.StateKey(system, state));
                _deviceStates[i].style.color = (Color)StatusColorOf(state);
                _deviceBubbles[i].style.display = !hideForBoot ? DisplayStyle.Flex : DisplayStyle.None;
                _deviceBubbles[i].focusable = true;
                _deviceBubbles[i].EnableInClassList(CargoScreen.FittedClass, state == RoverSystemState.Ok);
                _deviceBubbles[i].EnableInClassList(CargoScreen.FaultClass, FaceDevices.IsFaulty(state));
                _deviceBubbles[i].EnableInClassList(CargoScreen.EmptyDeviceClass, state == RoverSystemState.Empty);
            }

            WritePage(_scale);
            _footerRoot.style.display = !hideForBoot ? DisplayStyle.Flex : DisplayStyle.None;
            if (_footerDevice >= 0)
            {
                WriteFooter(_footerDevice);
            }

            UpdateTooltip();
            UpdateToast();
            _canvas.MarkDirtyRepaint();
        }

        /// <summary>The words under hold slot <paramref name="index"/>: the item's name, or nothing for an empty slot.</summary>
        private void WriteHoldName(int index)
        {
            bool shown = !HideForBoot && index < _unlockedHoldCount && _holdStates[index].Kind != HoldCellKind.Empty;
            _holdNames[index].style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
            if (shown)
            {
                _holdNames[index].text = HoldItemName(index);
            }
        }

        /// <summary>True when hold slot <paramref name="index"/> is unlocked and on the page shown.</summary>
        private bool IsOnPage(int index)
        {
            return index < _unlockedHoldCount && FaceLayout.PageOf(index) == _holdPage;
        }

        /// <summary>
        /// The page shown: its slots answer the pointer and the rest step aside; the page count on the heading row
        /// while the hold has two pages; and, on the last page, the one hint row that says more slots come later.
        /// </summary>
        private void WritePage(float s)
        {
            for (int i = 0; i < MaxHoldCells; i++)
            {
                bool onPage = IsOnPage(i);
                _holdCells[i].style.display = onPage ? DisplayStyle.Flex : DisplayStyle.None;
                _holdCells[i].pickingMode = onPage ? PickingMode.Position : PickingMode.Ignore;
            }

            int pages = FaceLayout.PageCount(_unlockedHoldCount);
            bool paged = pages > 1 && !HideForBoot;
            _page.style.display = paged ? DisplayStyle.Flex : DisplayStyle.None;
            PositionRect(_page, FaceLayout.PageRect, s);
            if (paged)
            {
                _page.text = string.Format(_localization.Get(UiKeys.CargoPage), _numbers.Get(_holdPage + 1),
                    _numbers.Get(pages));
            }

            bool hinted = HintShown;
            _more.style.display = hinted ? DisplayStyle.Flex : DisplayStyle.None;
            if (!hinted)
            {
                return;
            }

            PositionRect(_more, FaceLayout.HintRect(_unlockedHoldCount), s);
            _more.text = string.Format(_localization.Get(UiKeys.CargoMoreSlots), _numbers.Get(LockedCellCount));
        }

        private bool HintShown => FaceLayout.HasMoreRow(_unlockedHoldCount) && !HideForBoot &&
                                  _holdPage == FaceLayout.PageCount(_unlockedHoldCount) - 1;

        /// <summary>Shows <paramref name="page"/> of the hold.</summary>
        private void ShowPage(int page)
        {
            if (page == _holdPage)
            {
                return;
            }

            _holdPage = page;
            WritePage(_scale);
            _canvas.MarkDirtyRepaint();
        }

        /// <summary>A click on the page count turns to the other page and selects its first slot.</summary>
        private void TurnPage()
        {
            int pages = FaceLayout.PageCount(_unlockedHoldCount);
            if (pages < 2 || HideForBoot)
            {
                return;
            }

            int first = (_holdPage + 1) % pages * FaceLayout.PageSize;
            bool changed = _focusedHoldCell != first;
            ClearDropReason();
            FocusHoldCell(first);
            NoteMoved(changed);
        }

        private void OnPagePointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0 || _lastPointerDownFrame == Time.frameCount)
            {
                return;
            }

            _lastPointerDownFrame = Time.frameCount;
            _handoff.NoteMouse();
            TurnPage();
        }

        /// <summary>
        /// Selects where the screen opens: the cutter while it offers the spare igniter (one confirm fits it), else the
        /// first thing in the hold, else the first slot.
        /// </summary>
        public void FocusFirst()
        {
            if (_systems.CanFitIgniter)
            {
                FocusDevice(FaceDevices.Cutter);
                return;
            }

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
                ShowPage(FaceLayout.PageOf(index));
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

        /// <summary>
        /// Handles a pointer-up at <paramref name="panelPos"/> in panel coordinates: it ends a drag of a hold part (the
        /// drop) or the click that never became one.
        /// </summary>
        public void PointerUpAtPanel(Vector2 panelPos)
        {
            HandlePointerUpAtReferencePos(PanelToReference(panelPos));
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
            int cell = _focusedHoldCell;
            int device = _focusedDevice;
            bool moved = true;
            if (_focusedHoldCell >= 0 && _focusedHoldCell < _unlockedHoldCount)
            {
                NavigateFromHoldCell(_focusedHoldCell, direction);
            }
            else if (_focusedDevice >= 0 && _focusedDevice < FaceDevices.Count)
            {
                NavigateFromDevice(_focusedDevice, direction);
            }
            else
            {
                FocusFirst();
                moved = _focusedHoldCell >= 0 || _focusedDevice >= 0;
            }

            NoteMoved(cell != _focusedHoldCell || device != _focusedDevice);
            return moved;
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

            if (_focusedHoldCell >= 0 && _focusedHoldCell < _unlockedHoldCount)
            {
                _lastSubmitFrame = Time.frameCount;
                Press(_focusedHoldCell, -1);
                return true;
            }

            return false;
        }

        /// <summary>
        /// A press on a cell or a bubble: the selection flashes bright and settles, and one soft tick says it was
        /// taken, whether or not the press did anything more.
        /// </summary>
        private void Press(int holdCell, int device)
        {
            _pressedHoldCell = holdCell;
            _pressedDevice = device;
            _pressFlash = 1f;
            _events.Publish(new UiCue(UiCueKind.Confirm));
            _canvas.MarkDirtyRepaint();
        }

        /// <summary>The selection moved by the keys, the pad or the hover: one softer tick.</summary>
        private void NoteMoved(bool changed)
        {
            if (changed)
            {
                _events.Publish(new UiCue(UiCueKind.FocusMove));
            }
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

                var name = new Label { name = "cargo-hold-name-" + i, pickingMode = PickingMode.Ignore };
                name.AddToClassList("cargo__cell-name");
                cell.Add(name);

                cell.RegisterCallback<FocusInEvent>(_ => OnHoldCellFocus(index));
                cell.RegisterCallback<PointerEnterEvent>(_ => OnHoldCellHover(index));
                cell.RegisterCallback<PointerDownEvent>(evt => OnHoldCellPointerDown(index, evt));
                cell.RegisterCallback<NavigationMoveEvent>(evt => OnHoldNavigationMove(index, evt));

                _holdCells[i] = cell;
                _holdBadges[i] = badge;
                _holdNames[i] = name;
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

                var deviceName = new Label { name = "seven-device-name-" + i, pickingMode = PickingMode.Ignore };
                deviceName.AddToClassList("cargo__device-name");
                bubble.Add(deviceName);

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
                _deviceNames[i] = deviceName;
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

            if (index != _dropReasonCell)
            {
                ClearDropReason();
            }

            _focusedHoldCell = index;
            _focusedDevice = -1;
            UpdateTooltip();
            _canvas.MarkDirtyRepaint();
        }

        private void OnHoldCellHover(int index)
        {
            if (index >= _unlockedHoldCount || !_handoff.HoverMaySelect || _drag.Phase != HoldDragPhase.Idle)
            {
                return;
            }

            EnsureInteractiveDuringBoot();
            bool changed = _focusedHoldCell != index;
            _focusedHoldCell = index;
            _focusedDevice = -1;
            UpdateTooltip();
            _canvas.MarkDirtyRepaint();
            NoteMoved(changed);
        }

        private void OnDeviceFocus(int device)
        {
            ClearDropReason();
            _focusedDevice = device;
            _focusedHoldCell = -1;
            UpdateTooltip();
            _canvas.MarkDirtyRepaint();
        }

        private void OnDeviceHover(int device)
        {
            if (!_handoff.HoverMaySelect || _drag.Phase != HoldDragPhase.Idle)
            {
                return;
            }

            EnsureInteractiveDuringBoot();
            bool changed = _focusedDevice != device;
            OnDeviceFocus(device);
            NoteMoved(changed);
        }

        private void OnHoldCellPointerDown(int index, PointerDownEvent evt)
        {
            if (evt.button != 0 || index >= _unlockedHoldCount || _lastPointerDownFrame == Time.frameCount)
            {
                return;
            }

            _lastPointerDownFrame = Time.frameCount;
            _handoff.NoteMouse();
            EnsureInteractiveDuringBoot();
            OnHoldCellFocus(index);
            Press(index, -1);
            if (BeginPress(index, EventToReference(evt)))
            {
                _panel.CapturePointer(evt.pointerId);
            }
        }

        private void OnPanelPointerMove(PointerMoveEvent evt)
        {
            HandlePointerMoveAtReferencePos(EventToReference(evt));
        }

        private void OnPanelPointerUp(PointerUpEvent evt)
        {
            if (evt.button != 0)
            {
                return;
            }

            if (_panel.HasPointerCapture(evt.pointerId))
            {
                _panel.ReleasePointer(evt.pointerId);
            }

            HandlePointerUpAtReferencePos(EventToReference(evt));
        }

        /// <summary>A press on a cell that holds something may become a drag of it; true when it can.</summary>
        private bool BeginPress(int cell, Vector2 refPos)
        {
            HoldCellKind kind = _holdStates[cell].Kind;
            if (HideForBoot || float.IsNaN(refPos.x) || float.IsNaN(refPos.y) || kind == HoldCellKind.Empty ||
                kind == HoldCellKind.Locked)
            {
                return false;
            }

            _dragIcon = GetHoldCellIcon(_holdStates[cell]);
            _drag.Press(cell, refPos, GetHoldCellReferenceRect(cell).center);
            return true;
        }

        /// <summary>
        /// The drop: the cutter takes the igniter on the pad (the icon eases into it); anything else eases home and the
        /// info box says why in one short line.
        /// </summary>
        private void HandlePointerUpAtReferencePos(Vector2 refPos)
        {
            if (float.IsNaN(refPos.x) || float.IsNaN(refPos.y) || !_drag.Release(refPos))
            {
                return;
            }

            int cell = _drag.Cell;
            DropOutcome outcome = DropRule.Decide(_holdStates[cell].StoryPartId, FindDeviceAtReferencePos(refPos),
                _systems.CanFitIgniter);
            if (outcome == DropOutcome.Fit && _systems.TryFitIgniter())
            {
                _drag.Fit(FaceDevices.CenterOf(FaceDevices.Cutter));
                _events.Publish(new UiCue(UiCueKind.PartDrop));
                Refresh();
                FocusDevice(FaceDevices.Cutter);
                return;
            }

            _drag.Return();
            _events.Publish(new UiCue(UiCueKind.PartRefuse));
            _dropReasonCell = cell;
            _dropReasonKey = DropRule.ReasonKey(outcome == DropOutcome.Fit ? DropOutcome.NotDocked : outcome);
            FocusHoldCell(cell);
        }

        private void ClearDropReason()
        {
            _dropReasonCell = -1;
            _dropReasonKey = null;
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
                Press(holdIdx, -1);
                BeginPress(holdIdx, refPos);
                return;
            }

            if (FaceLayout.PageRect.Contains(refPos))
            {
                _handoff.NoteMouse();
                TurnPage();
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

            if (_drag.Phase == HoldDragPhase.Pressed || _drag.Phase == HoldDragPhase.Dragging)
            {
                if (_drag.Move(refPos))
                {
                    _events.Publish(new UiCue(UiCueKind.PartPick));
                }

                _canvas.MarkDirtyRepaint();
                _ghostLayer.MarkDirtyRepaint();
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
                if (IsOnPage(i) && FaceLayout.HoldHitRect(i).Contains(refPos))
                {
                    return i;
                }
            }

            return -1;
        }

        private static int FindDeviceAtReferencePos(Vector2 refPos)
        {
            for (int i = 0; i < FaceDevices.Count; i++)
            {
                if (FaceLayout.DeviceHitRect(i).Contains(refPos))
                {
                    return i;
                }
            }

            return -1;
        }

        private void OnDeviceSubmit(int device)
        {
            if (device < 0 || device >= FaceDevices.Count)
            {
                return;
            }

            _lastSubmitFrame = Time.frameCount;
            EnsureInteractiveDuringBoot();
            Press(-1, device);
            if (FaceDevices.OffersIgniter(device, _systems.CanFitIgniter) && _systems.TryFitIgniter())
            {
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
            int cell = _focusedHoldCell;
            int device = _focusedDevice;
            NavigateFromHoldCell(index, evt.direction);
            NoteMoved(cell != _focusedHoldCell || device != _focusedDevice);
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
            int cell = _focusedHoldCell;
            int focused = _focusedDevice;
            NavigateFromDevice(device, evt.direction);
            NoteMoved(cell != _focusedHoldCell || focused != _focusedDevice);
        }

        private void NavigateFromHoldCell(int index, NavigationMoveEvent.Direction direction)
        {
            FaceNavigation.FromHoldCell(index, direction, _unlockedHoldCount, out int cell, out int device);
            MoveSelection(cell, device);
        }

        private void NavigateFromDevice(int device, NavigationMoveEvent.Direction direction)
        {
            FaceNavigation.FromDevice(device, direction, _unlockedHoldCount, _holdPage, out int cell, out int next);
            MoveSelection(cell, next);
        }

        /// <summary>Selects the hold slot or the device a step led to; neither keeps the selection where it is.</summary>
        private void MoveSelection(int cell, int device)
        {
            if (cell >= 0)
            {
                FocusHoldCell(cell);
            }
            else if (device >= 0)
            {
                FocusDevice(device);
            }
        }

        private void UpdateTooltip()
        {
            if (HideForBoot)
            {
                _tooltip.style.display = DisplayStyle.None;
                return;
            }

            if (_focusedHoldCell >= 0 && _focusedHoldCell < _unlockedHoldCount)
            {
                HoldCellState state = _holdStates[_focusedHoldCell];
                if (state.Kind == HoldCellKind.Empty)
                {
                    ShowTooltipBox(_localization.Get(UiKeys.CargoEmptyName), _localization.Get(UiKeys.CargoEmptyDesc));
                }
                else if (_focusedHoldCell == _dropReasonCell && _dropReasonKey != null)
                {
                    ShowTooltipBox(HoldItemName(_focusedHoldCell), _localization.Get(_dropReasonKey));
                }
                else
                {
                    ShowTooltipBox(HoldItemName(_focusedHoldCell), HoldItemDescription(_focusedHoldCell));
                }

                return;
            }

            if (_focusedDevice >= 0 && _focusedDevice < FaceDevices.Count)
            {
                RoverSystemState state = _systems.StateOf(FaceDevices.SystemOf(_focusedDevice));
                string name = _localization.Get(FaceDevices.NameKey(_focusedDevice));
                string desc = _localization.Get(FaceDevices.DescriptionKey(_focusedDevice, state, _systems.CanFitIgniter));
                ShowTooltipBox(name, desc);
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

        /// <summary>The info box under the hold grid: it names the selection and says in a line or two what it is for.</summary>
        private void ShowTooltipBox(string name, string desc)
        {
            _tooltipName.text = name;
            _tooltipDesc.text = TruncateDescription(desc);
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
                float rule = _settings.CellStrokeWidth * s;
                RoverLineArtPainter.DrawLine(painter, new Vector2(FaceLayout.Margin * s, FaceLayout.TitleRuleY * s),
                    new Vector2(FaceLayout.RightEdge * s, FaceLayout.TitleRuleY * s), AmberMuted, rule);
                RoverLineArtPainter.DrawLine(painter, new Vector2(FaceLayout.Margin * s, FaceLayout.HeadingRuleY * s),
                    new Vector2(FaceLayout.HoldRight * s, FaceLayout.HeadingRuleY * s), AmberMuted, rule);
            }

            DrawHoldGrid(painter, s);

            if (!hideForBoot)
            {
                DrawMoreRow(painter, s);
                DrawDevices(painter, s);
                DrawToastDot(painter, s);
            }
        }

        /// <summary>
        /// The dragged part's icon over everything (its own layer above the frames' words), on a cell-sized plate so
        /// it reads over the line art.
        /// </summary>
        private void OnGenerateGhost(MeshGenerationContext context)
        {
            Rect rect = _ghostLayer.contentRect;
            if (!_drag.ShowsGhost || HideForBoot || rect.width < 4f || rect.height < 4f)
            {
                return;
            }

            Painter2D painter = context.painter2D;
            float s = rect.width / ReferenceWidth;
            float alpha = _drag.GhostAlpha;
            Vector2 center = _drag.Ghost * s;
            float half = CellSize * 0.5f * s;
            RoverLineArtPainter.FillRect(painter, center.x - half, center.y - half, half * 2f, half * 2f,
                RoverLineArtPainter.WithAlpha(_settings.SelectionFill, (byte)Mathf.RoundToInt(BgCell.a * alpha)));
            RoverLineArtPainter.StrokeRect(painter, center.x - half, center.y - half, half * 2f, half * 2f,
                RoverLineArtPainter.WithAlpha(Cyan, (byte)Mathf.RoundToInt(Cyan.a * alpha)),
                _settings.FocusStrokeWidth * s);
            UiIconPainter.Draw(painter, in _dragIcon, center, _settings.HoldIconSize * s,
                RoverLineArtPainter.WithAlpha(AmberText, (byte)Mathf.RoundToInt(FullIconAlpha * alpha)));
        }

        private void DrawHoldGrid(Painter2D painter, float s)
        {
            bool hideForBoot = HideForBoot;
            int limit = hideForBoot ? Mathf.Min(_bootVisibleSlots, _unlockedHoldCount) : MaxHoldCells;
            for (int i = 0; i < limit; i++)
            {
                if (!IsOnPage(i))
                {
                    continue;
                }

                Rect r = FaceLayout.HoldCellRect(i);
                float cx = r.x * s;
                float cy = r.y * s;
                float cw = r.width * s;
                float ch = r.height * s;
                HoldCellState state = _holdStates[i];

                if (state.Kind == HoldCellKind.Locked)
                {
                    continue;
                }

                bool focused = !hideForBoot && i == _focusedHoldCell;
                bool hasItem = state.Kind != HoldCellKind.Empty;

                RoverLineArtPainter.FillRect(painter, cx, cy, cw, ch, focused ? SelectionFillOf(i, -1) : BgCell);
                if (focused)
                {
                    float ring = _settings.FocusStrokeWidth * s;
                    RoverLineArtPainter.StrokeRect(painter, cx, cy, cw, ch, Cyan, ring);
                    RoverLineArtPainter.DrawCornerTicks(painter, cx, cy, cw, ch, CellTickLength * s, Cyan, ring);
                }
                else
                {
                    RoverLineArtPainter.StrokeRect(painter, cx, cy, cw, ch, hasItem ? AmberFaint : AmberMuted,
                        (hasItem ? _settings.ItemStrokeWidth : _settings.CellStrokeWidth) * s);
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
                float lifted = _drag.ShowsGhost && i == _drag.Cell ? _settings.DragSourceAlpha : 1f;
                Color32 tint = RoverLineArtPainter.WithAlpha(AmberText,
                    (byte)Mathf.RoundToInt(FullIconAlpha * bootProgress * lifted));
                Vector2 iconCenter = new Vector2(cx + cw * 0.5f + slideX, cy + ch * 0.5f);
                UiIconPainter.Draw(painter, in icon, iconCenter, _settings.HoldIconSize * s, tint);

                if (!hideForBoot && state.Kind == HoldCellKind.Material && state.Units > 1)
                {
                    RoverLineArtPainter.FillRect(painter, cx + cw - BadgeInsetX * s, cy + ch - BadgeInsetY * s,
                        BadgeWidth * s, BadgeHeight * s, focused ? SelectionFillOf(i, -1) : BgCell);
                }
            }
        }

        /// <summary>The one hint row that stands for every locked slot: a padlock before its words.</summary>
        private void DrawMoreRow(Painter2D painter, float s)
        {
            if (!HintShown)
            {
                return;
            }

            Rect row = FaceLayout.HintRect(_unlockedHoldCount);
            RoverLineArtPainter.DrawPadlock(painter, new Vector2((row.x + FaceLayout.HintPadlockX) * s, row.center.y * s),
                AmberLocked);
        }

        /// <summary>The fill of the selected cell or frame: the selection colour, brighter just after a press.</summary>
        private Color32 SelectionFillOf(int holdCell, int device)
        {
            bool pressed = _pressFlash > 0f && (holdCell >= 0 ? holdCell == _pressedHoldCell : device == _pressedDevice);
            return pressed ? Color32.Lerp(_settings.SelectionFill, _settings.PressFill, _pressFlash)
                : _settings.SelectionFill;
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

        /// <summary>The leaders over 07's drawing (an element under the canvas), then the four device frames.</summary>
        private void DrawDevices(Painter2D painter, float s)
        {
            for (int pass = 0; pass < 2; pass++)
            {
                bool drawFocusPass = pass == 1;
                for (int i = 0; i < FaceDevices.Count; i++)
                {
                    if ((i == _focusedDevice) == drawFocusPass)
                    {
                        DrawLeader(painter, s, i);
                    }
                }
            }

            for (int i = 0; i < FaceDevices.Count; i++)
            {
                DrawDeviceFrame(painter, s, i);
            }
        }

        /// <summary>
        /// The leader from a device's frame to its part on 07's drawing (the anchor Art marked): straight out of the
        /// frame, then on to the part. Faint while the device is not working, subtle amber when it works, crisp cyan
        /// under focus; the focused one is drawn last so it sits on top.
        /// </summary>
        private void DrawLeader(Painter2D painter, float s, int device)
        {
            Vector2 target = _leaderTargets[device];
            Vector2 anchor = FaceLayout.LeaderStart(device, target) * s;
            Vector2 knee = FaceLayout.LeaderKneeOf(device, target) * s;
            Vector2 mount = target * s;
            Color32 color;
            float width;
            float dot;
            if (device == _focusedDevice)
            {
                color = _settings.LeaderFocusColor;
                width = _settings.LeaderFocusWidth;
                dot = _settings.LeaderFocusDotRadius;
            }
            else if (_deviceStateOf[device] == RoverSystemState.Ok)
            {
                color = _settings.LeaderInstalledColor;
                width = _settings.LeaderInstalledWidth;
                dot = _settings.LeaderInstalledDotRadius;
            }
            else
            {
                color = _settings.LeaderEmptyColor;
                width = _settings.LeaderEmptyWidth;
                dot = _settings.LeaderEmptyDotRadius;
            }

            RoverLineArtPainter.DrawLine(painter, anchor, knee, color, width * s);
            RoverLineArtPainter.DrawLine(painter, knee, mount, color, width * s);
            RoverLineArtPainter.FillCircle(painter, mount, dot * s, color);
        }

        /// <summary>
        /// A device's labelled frame, drawn in its state: working in teal with a solid outline and disc, faulty in
        /// amber with the warning mark on its name row, empty in a quiet grey with a broken outline and a thin disc;
        /// the selection (or the cutter offering the igniter) rings it in cyan. The words are the frame's labels.
        /// </summary>
        private void DrawDeviceFrame(Painter2D painter, float s, int device)
        {
            Rect frame = FaceLayout.DeviceFrame(device);
            RoverSystemState state = _deviceStateOf[device];
            bool empty = state == RoverSystemState.Empty;
            bool fault = FaceDevices.IsFaulty(state);
            Color32 status = StatusColorOf(state);
            bool focused = device == _focusedDevice;
            bool offered = FaceDevices.OffersIgniter(device, _igniterOffered);
            float x = frame.x * s;
            float y = frame.y * s;
            float w = frame.width * s;
            float h = frame.height * s;
            float stateWidth = (empty ? _settings.CellStrokeWidth : _settings.ItemStrokeWidth) * s;

            RoverLineArtPainter.FillRect(painter, x, y, w, h, focused ? SelectionFillOf(-1, device) : BgCell);
            if (focused || offered)
            {
                float ring = _settings.FocusStrokeWidth * s;
                RoverLineArtPainter.StrokeRect(painter, x, y, w, h, Cyan, ring);
                RoverLineArtPainter.DrawCornerTicks(painter, x, y, w, h, FrameTickLength * s, Cyan, ring);
            }
            else if (empty)
            {
                RoverLineArtPainter.StrokeDashedRect(painter, x, y, w, h, status, stateWidth, _settings.EmptyDash * s,
                    _settings.EmptyDashGap * s);
            }
            else
            {
                RoverLineArtPainter.StrokeRect(painter, x, y, w, h, status, stateWidth);
            }

            float nameRule = (frame.y + FaceLayout.FrameNameHeight) * s;
            float inset = FrameRuleInset * s;
            RoverLineArtPainter.DrawLine(painter, new Vector2(x + inset, nameRule), new Vector2(x + w - inset, nameRule),
                AmberMuted, _settings.CellStrokeWidth * s);

            Vector2 disc = FaceLayout.DeviceIconCenter(device) * s;
            RoverLineArtPainter.StrokeCircle(painter, disc, _settings.BubbleRadius * s, status, stateWidth);
            UiIconPainter.Draw(painter, in _deviceIcons[device], disc, _settings.DeviceIconSize * s, status);

            if (fault)
            {
                UiIconPainter.Draw(painter, in _warningIcon, FaceLayout.WarningCenter(device) * s,
                    _settings.WarningIconSize * s, status);
            }
        }

        /// <summary>The colour of a device's state: teal when it works, grey when its socket is empty, else amber.</summary>
        private Color32 StatusColorOf(RoverSystemState state)
        {
            return state == RoverSystemState.Ok ? _settings.StatusOk
                : state == RoverSystemState.Empty ? _settings.StatusEmpty
                : _settings.StatusFault;
        }

        private void DrawToastDot(Painter2D painter, float s)
        {
            if (_toast.style.display != DisplayStyle.Flex)
            {
                return;
            }

            RoverLineArtPainter.FillCircle(painter,
                new Vector2((FaceLayout.Margin + ToastDotX) * s, (FaceLayout.ToastY + FaceLayout.ToastHeight * 0.5f) * s),
                ToastDotRadius * s, _settings.AmberBright);
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

        private static void PositionRect(VisualElement element, Rect rect, float s)
        {
            PositionBox(element, rect.x * s, rect.y * s, rect.width * s, rect.height * s);
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
