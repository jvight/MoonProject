using System;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core.Events;

namespace MoonProject.UI
{
    /// <summary>
    /// Snapshot of the active pause/title menu state used by <see cref="MenuNavigator"/> to validate focus targets
    /// and route directional navigation.
    /// </summary>
    internal readonly struct MenuNavigationContext
    {
        public readonly bool IsActive;
        public readonly PausePage CurrentPage;
        public readonly PauseFocus PageDefaultFocus;
        public readonly bool IsCreditsOpen;
        public readonly bool IsSettingsOpen;
        public readonly int SettingsTab;

        public MenuNavigationContext(
            bool isActive,
            PausePage currentPage,
            PauseFocus pageDefaultFocus,
            bool isCreditsOpen,
            bool isSettingsOpen,
            int settingsTab)
        {
            IsActive = isActive;
            CurrentPage = currentPage;
            PageDefaultFocus = pageDefaultFocus;
            IsCreditsOpen = isCreditsOpen;
            IsSettingsOpen = isSettingsOpen;
            SettingsTab = settingsTab;
        }
    }

    /// <summary>
    /// Owns keyboard, gamepad, and pointer focus management, directional navigation order, hold-to-repeat timing,
    /// setting-row focus highlights, and title-mode <c>&gt;</c> cursor indicators for <see cref="PauseMenu"/>.
    /// </summary>
    internal sealed class MenuNavigator
    {
        private const string TabFocusClass = "settings-tab-wrap--focus";
        private const string FocusedRowClass = "setting-row--focused";

        private readonly UiLayout _layout;
        private readonly PauseSettings _settings;
        private readonly Func<MenuNavigationContext> _getContext;
        private readonly Action<UiCueKind> _onCue;
        private readonly Action<int> _onSelectTab;
        private readonly Action<VisualElement, int> _onStepSettingsControl;
        private readonly Action<int> _onScrollCredits;
        private readonly Button[] _languageButtons;
        private readonly Button[] _mainPageButtons;
        private readonly Button[] _newGamePageButtons;
        private readonly Button[] _quitPageButtons;
        private readonly Button[] _settingsTabs;
        private readonly VisualElement[][] _settingsTabControls;
        private readonly VisualElement[] _settingRowControls;
        private readonly VisualElement[] _settingRows;
        private readonly Label[] _titleCursors;
        private readonly VisualElement[] _cursorHosts;

        private Focusable _pendingFocus;
        private Focusable _lastValidFocus;
        private VisualElement _lastSyncedFocus;
        private VisualElement _fallbackFocus;
        private bool _focusingByCode;
        private bool _pointerPressed;
        private Vector2 _lastPointerPos = new Vector2(float.NaN, float.NaN);
        private int _lastNavigateFrame = -1;
        private Vector2Int _heldDir;
        private float _repeatTimer;
        private bool _lastCursorVisible = true;

        public MenuNavigator(
            UiLayout layout,
            PauseSettings settings,
            Button[] languageButtons,
            Func<MenuNavigationContext> getContext,
            Action<UiCueKind> onCue,
            Action<int> onSelectTab,
            Action<VisualElement, int> onStepSettingsControl,
            Action<int> onScrollCredits)
        {
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _languageButtons = languageButtons ?? throw new ArgumentNullException(nameof(languageButtons));
            _getContext = getContext ?? throw new ArgumentNullException(nameof(getContext));
            _onCue = onCue ?? throw new ArgumentNullException(nameof(onCue));
            _onSelectTab = onSelectTab ?? throw new ArgumentNullException(nameof(onSelectTab));
            _onStepSettingsControl = onStepSettingsControl ?? throw new ArgumentNullException(nameof(onStepSettingsControl));
            _onScrollCredits = onScrollCredits ?? throw new ArgumentNullException(nameof(onScrollCredits));

            _settingsTabs = new[] { layout.TabSound, layout.TabCamera, layout.TabDisplay };
            _mainPageButtons = new[]
            {
                layout.ResumeButton,
                layout.NewGameButton,
                layout.SettingsButton,
                layout.CreditsButton,
                layout.QuitToTitleButton,
                layout.QuitButton,
            };
            _newGamePageButtons = new[]
            {
                layout.NewGameKeepButton,
                layout.NewGameConfirmButton,
            };
            _quitPageButtons = new[]
            {
                layout.QuitStayButton,
                layout.QuitConfirmButton,
            };
            _settingsTabControls = new[]
            {
                new VisualElement[]
                {
                    layout.MasterSlider,
                    layout.MusicSlider,
                    layout.SfxSlider,
                    layout.AmbienceSlider,
                },
                new VisualElement[]
                {
                    layout.LookSlider,
                    layout.InvertToggle,
                    layout.GentleCameraToggle,
                    layout.RumbleToggle,
                    layout.RumbleSlider,
                    layout.GuidanceToggle,
                },
                new VisualElement[]
                {
                    layout.ResolutionPrev,
                    layout.ModePrev,
                    layout.VSyncToggle,
                    layout.FpsPrev,
                    layout.TextSizeButton,
                    layout.LanguageButton,
                },
            };
            _settingRowControls = new VisualElement[]
            {
                layout.MasterSlider,
                layout.MusicSlider,
                layout.SfxSlider,
                layout.AmbienceSlider,
                layout.LookSlider,
                layout.InvertToggle,
                layout.GentleCameraToggle,
                layout.RumbleToggle,
                layout.RumbleSlider,
                layout.GuidanceToggle,
                layout.ResolutionPrev,
                layout.ModePrev,
                layout.VSyncToggle,
                layout.FpsPrev,
                layout.TextSizeButton,
                layout.LanguageButton,
            };
            _settingRows = new VisualElement[_settingRowControls.Length];
            for (int i = 0; i < _settingRowControls.Length; i++)
            {
                _settingRows[i] = FindSettingRow(_settingRowControls[i]);
            }

            _titleCursors = CreateTitleCursors(layout, _languageButtons, _settingRows, out _cursorHosts);
            BindCallbacks();
        }

        /// <summary>The element currently holding focus (either via UI Toolkit panel or fallback in EditMode).</summary>
        public VisualElement FocusedElement => _layout.Root.panel != null
            ? _layout.Root.panel.focusController?.focusedElement as VisualElement
            : _fallbackFocus;

        /// <summary>The last valid focus target recorded by the navigator.</summary>
        public VisualElement LastValidFocus => _lastValidFocus as VisualElement;

        /// <summary>Resets pointer/repeat state on menu open and queues initial focus.</summary>
        public void ResetOnOpen(Vector2 pointerPos, VisualElement initialFocus)
        {
            _lastPointerPos = pointerPos;
            _heldDir = Vector2Int.zero;
            _repeatTimer = 0f;
            RequestFocus(initialFocus);
        }

        /// <summary>Queues <paramref name="target"/> to receive focus and records it as the last valid focus.</summary>
        public void RequestFocus(VisualElement target)
        {
            _pendingFocus = target;
            _lastValidFocus = target;
        }

        /// <summary>Queues the button corresponding to <paramref name="focus"/> to receive focus.</summary>
        public void RequestFocus(PauseFocus focus)
        {
            RequestFocus(ButtonFor(focus));
        }

        /// <summary>Queues the default focus target for the current menu context.</summary>
        public void RequestDefaultFocus()
        {
            RequestFocus(DefaultFocus(_getContext()));
        }

        /// <summary>Clears pending and remembered focus, blurs the active element, and removes focus highlights.</summary>
        public void ClearFocus()
        {
            _pendingFocus = null;
            _lastValidFocus = null;
            Blur();
            SyncSettingRowFocus(null);
        }

        /// <summary>Drops current focus without clearing <see cref="LastValidFocus"/>.</summary>
        public void Blur()
        {
            _layout.Root.panel?.focusController?.focusedElement?.Blur();
            _fallbackFocus = null;
        }

        /// <summary>Immediately focuses <paramref name="target"/> if displayed and focusable.</summary>
        public void FocusControl(VisualElement target, bool byCode)
        {
            if (target == null || !IsDisplayed(target) || !target.canGrabFocus)
            {
                return;
            }

            _lastValidFocus = target;
            _fallbackFocus = target;
            if (FocusedElement == target)
            {
                SyncSettingRowFocus(target);
                return;
            }

            bool prev = _focusingByCode;
            _focusingByCode = byCode;
            target.Focus();
            _focusingByCode = prev;
            SyncSettingRowFocus(target);
        }

        /// <summary>Updates the visibility of the blinking <c>&gt;</c> title cursors.</summary>
        public void SetCursorVisibility(bool cursorVisible)
        {
            if (cursorVisible == _lastCursorVisible)
            {
                return;
            }

            _lastCursorVisible = cursorVisible;
            Visibility visibility = cursorVisible ? Visibility.Visible : Visibility.Hidden;
            for (int i = 0; i < _titleCursors.Length; i++)
            {
                _titleCursors[i].style.visibility = visibility;
            }
        }

        /// <summary>Per-frame update: pointer hover, held-axis repeat navigation, and focus restoration.</summary>
        public void Tick(float unscaledDeltaTime, Vector2 pointerPos, Vector2 navigateVector)
        {
            MenuNavigationContext ctx = _getContext();
            if (ctx.IsActive)
            {
                TickPointerHover(pointerPos, ctx);
                TickNavigation(unscaledDeltaTime, navigateVector);
                EnsureFocusValid();
            }

            _pointerPressed = false;
        }

        /// <summary>
        /// Ensures that a valid element for the current state holds focus, restoring <see cref="LastValidFocus"/>
        /// when valid or falling back to <see cref="DefaultFocus"/>.
        /// </summary>
        public void EnsureFocusValid()
        {
            MenuNavigationContext ctx = _getContext();
            if (_pendingFocus == null)
            {
                VisualElement current = FocusedElement;
                if (!IsValidFocusForState(current, ctx))
                {
                    _pendingFocus = (_lastValidFocus is VisualElement lastEl && IsValidFocusForState(lastEl, ctx))
                        ? _lastValidFocus
                        : DefaultFocus(ctx);
                }
            }

            if (_pendingFocus is VisualElement target && IsDisplayed(target) && target.canGrabFocus)
            {
                FocusControl(target, byCode: true);
                _pendingFocus = null;
            }

            SyncSettingRowFocus(FocusedElement);
        }

        /// <summary>
        /// Evaluates axis deadzone and hold-to-repeat timing for a 2D navigation vector. Returns true when a step
        /// should fire on this tick.
        /// </summary>
        public bool TryStepRepeat(Vector2 nav, float unscaledDeltaTime, out NavigationMoveEvent.Direction direction)
        {
            direction = NavigationMoveEvent.Direction.None;
            Vector2Int dir = Vector2Int.zero;
            float deadzone = _settings.AxisDeadzone;
            if (Mathf.Abs(nav.x) >= deadzone || Mathf.Abs(nav.y) >= deadzone)
            {
                if (Mathf.Abs(nav.y) >= Mathf.Abs(nav.x))
                {
                    dir = new Vector2Int(0, nav.y > 0f ? 1 : -1);
                }
                else
                {
                    dir = new Vector2Int(nav.x > 0f ? 1 : -1, 0);
                }
            }

            if (dir == Vector2Int.zero)
            {
                _heldDir = Vector2Int.zero;
                _repeatTimer = 0f;
                return false;
            }

            bool trigger = false;
            if (dir != _heldDir)
            {
                _heldDir = dir;
                _repeatTimer = _settings.RepeatInitialDelay;
                trigger = true;
            }
            else
            {
                _repeatTimer -= unscaledDeltaTime;
                if (_repeatTimer <= 0f)
                {
                    _repeatTimer += _settings.RepeatInterval;
                    trigger = true;
                }
            }

            if (!trigger)
            {
                return false;
            }

            if (dir.y > 0)
            {
                direction = NavigationMoveEvent.Direction.Up;
            }
            else if (dir.y < 0)
            {
                direction = NavigationMoveEvent.Direction.Down;
            }
            else if (dir.x < 0)
            {
                direction = NavigationMoveEvent.Direction.Left;
            }
            else
            {
                direction = NavigationMoveEvent.Direction.Right;
            }

            return true;
        }

        /// <summary>Executes a single directional navigation step in the current menu context.</summary>
        public void HandleNavigation(NavigationMoveEvent.Direction direction)
        {
            MenuNavigationContext ctx = _getContext();
            VisualElement focused = FocusedElement;
            if (!IsValidFocusForState(focused, ctx))
            {
                VisualElement fallback = (_lastValidFocus is VisualElement lastEl && IsValidFocusForState(lastEl, ctx))
                    ? lastEl
                    : DefaultFocus(ctx);
                FocusControl(fallback, byCode: false);
                return;
            }

            if (ctx.IsCreditsOpen)
            {
                if (direction == NavigationMoveEvent.Direction.Up)
                {
                    _onScrollCredits(-1);
                }
                else if (direction == NavigationMoveEvent.Direction.Down)
                {
                    _onScrollCredits(1);
                }

                return;
            }

            if (ctx.IsSettingsOpen)
            {
                HandleSettingsNavigation(focused, direction, ctx.SettingsTab);
                return;
            }

            if (direction == NavigationMoveEvent.Direction.Up || direction == NavigationMoveEvent.Direction.Down)
            {
                StepButtonList(CurrentPageButtons(ctx.CurrentPage), focused,
                    direction == NavigationMoveEvent.Direction.Down ? 1 : -1);
            }
        }

        /// <summary>Returns the button corresponding to <paramref name="focus"/>.</summary>
        public Button ButtonFor(PauseFocus focus)
        {
            switch (focus)
            {
                case PauseFocus.NewGame:
                    return _layout.NewGameButton;
                case PauseFocus.Quit:
                    return _layout.QuitButton;
                case PauseFocus.QuitStay:
                    return _layout.QuitStayButton;
                case PauseFocus.NewGameKeep:
                    return _layout.NewGameKeepButton;
                case PauseFocus.Language:
                    return _languageButtons[0];
                default:
                    return _layout.ResumeButton;
            }
        }

        /// <summary>Returns the default focus element for <paramref name="ctx"/>.</summary>
        public VisualElement DefaultFocus(MenuNavigationContext ctx)
        {
            if (ctx.IsCreditsOpen)
            {
                return _layout.CreditsBack;
            }

            if (ctx.IsSettingsOpen)
            {
                return _layout.MasterSlider;
            }

            return ButtonFor(ctx.PageDefaultFocus);
        }

        /// <summary>True when <paramref name="element"/> is displayed, focusable, and belongs to the active panel/page.</summary>
        public bool IsValidFocusForState(VisualElement element, MenuNavigationContext ctx)
        {
            if (element == null || !IsDisplayed(element) || !element.canGrabFocus)
            {
                return false;
            }

            if (ctx.IsCreditsOpen)
            {
                return element == _layout.CreditsBack;
            }

            if (ctx.IsSettingsOpen)
            {
                return _layout.PauseSettings.Contains(element);
            }

            return _layout.PauseMain.Contains(element);
        }

        private void BindCallbacks()
        {
            foreach (Button tab in _settingsTabs)
            {
                VisualElement skin = tab.parent;
                tab.RegisterCallback<FocusInEvent>(_ => skin.AddToClassList(TabFocusClass));
                tab.RegisterCallback<FocusOutEvent>(_ => skin.RemoveFromClassList(TabFocusClass));
            }

            _layout.Pause.RegisterCallback<FocusInEvent>(OnFocusIn);
            _layout.Pause.RegisterCallback<FocusOutEvent>(OnFocusOut);
            _layout.Pause.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            _layout.Pause.RegisterCallback<NavigationMoveEvent>(OnNavigationMove, TrickleDown.TrickleDown);

            BindHoverFocus(_layout.ResumeButton);
            BindHoverFocus(_layout.NewGameButton);
            BindHoverFocus(_layout.SettingsButton);
            BindHoverFocus(_layout.CreditsButton);
            BindHoverFocus(_layout.QuitToTitleButton);
            BindHoverFocus(_layout.QuitButton);
            BindHoverFocus(_layout.NewGameKeepButton);
            BindHoverFocus(_layout.NewGameConfirmButton);
            BindHoverFocus(_layout.QuitStayButton);
            BindHoverFocus(_layout.QuitConfirmButton);
            BindHoverFocus(_layout.SettingsBackButton);
            BindHoverFocus(_layout.CreditsBack);
            for (int i = 0; i < _languageButtons.Length; i++)
            {
                BindHoverFocus(_languageButtons[i]);
            }

            for (int i = 0; i < _settingsTabs.Length; i++)
            {
                BindHoverFocus(_settingsTabs[i].parent, _settingsTabs[i]);
            }

            for (int i = 0; i < _settingRowControls.Length; i++)
            {
                VisualElement control = _settingRowControls[i];
                VisualElement row = _settingRows[i] ?? control;
                BindHoverFocus(row, control);
            }
        }

        private void BindHoverFocus(VisualElement hitArea, VisualElement focusTarget = null)
        {
            VisualElement target = focusTarget ?? hitArea;
            hitArea.RegisterCallback<PointerEnterEvent>(_ =>
            {
                MenuNavigationContext ctx = _getContext();
                if (ctx.IsActive && _pendingFocus == null && IsValidFocusForState(target, ctx))
                {
                    FocusControl(target, byCode: true);
                }
            });
            hitArea.RegisterCallback<PointerMoveEvent>(evt =>
            {
                MenuNavigationContext ctx = _getContext();
                if (ctx.IsActive && _pendingFocus == null &&
                    evt.deltaPosition.sqrMagnitude > 0f && IsValidFocusForState(target, ctx))
                {
                    FocusControl(target, byCode: true);
                }
            });
        }

        private void OnFocusIn(FocusInEvent evt)
        {
            MenuNavigationContext ctx = _getContext();
            if (evt.target is VisualElement el && IsValidFocusForState(el, ctx))
            {
                _lastValidFocus = el;
                _fallbackFocus = el;
                SyncSettingRowFocus(el);
            }

            if (ctx.IsActive && !_focusingByCode && !_pointerPressed)
            {
                _onCue(UiCueKind.FocusMove);
            }
        }

        private void OnFocusOut(FocusOutEvent evt)
        {
            if (FocusedElement == null)
            {
                SyncSettingRowFocus(null);
            }
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            _pointerPressed = true;
        }

        private void OnNavigationMove(NavigationMoveEvent evt)
        {
            MenuNavigationContext ctx = _getContext();
            if (!ctx.IsActive)
            {
                return;
            }

            evt.StopPropagation();
            _layout.Root.panel?.focusController?.IgnoreEvent(evt);
            if (evt.direction == NavigationMoveEvent.Direction.None || _lastNavigateFrame == Time.frameCount)
            {
                return;
            }

            _lastNavigateFrame = Time.frameCount;
            HandleNavigation(evt.direction);
        }

        private void TickNavigation(float unscaledDeltaTime, Vector2 nav)
        {
            if (!TryStepRepeat(nav, unscaledDeltaTime, out NavigationMoveEvent.Direction moveDir))
            {
                return;
            }

            if (_lastNavigateFrame == Time.frameCount)
            {
                return;
            }

            _lastNavigateFrame = Time.frameCount;
            HandleNavigation(moveDir);
        }

        private void TickPointerHover(Vector2 pointerPos, MenuNavigationContext ctx)
        {
            if (float.IsNaN(_lastPointerPos.x))
            {
                _lastPointerPos = pointerPos;
                return;
            }

            if ((pointerPos - _lastPointerPos).sqrMagnitude <= 0.25f)
            {
                return;
            }

            _lastPointerPos = pointerPos;
            IPanel panel = _layout.Root.panel;
            if (panel == null)
            {
                return;
            }

            Vector2 screenTopLeft = new Vector2(pointerPos.x, Screen.height - pointerPos.y);
            Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(panel, screenTopLeft);
            VisualElement hovered = HitTestInteractive(panelPos, ctx);
            if (hovered != null)
            {
                FocusControl(hovered, byCode: true);
            }
        }

        private VisualElement HitTestInteractive(Vector2 panelPos, MenuNavigationContext ctx)
        {
            if (ctx.IsCreditsOpen)
            {
                return ContainsPoint(_layout.CreditsBack, panelPos) ? _layout.CreditsBack : null;
            }

            if (ctx.IsSettingsOpen)
            {
                for (int i = 0; i < _settingsTabs.Length; i++)
                {
                    if (ContainsPoint(_settingsTabs[i].parent, panelPos))
                    {
                        return _settingsTabs[i];
                    }
                }

                VisualElement[] tabControls = _settingsTabControls[ctx.SettingsTab];
                for (int i = 0; i < tabControls.Length; i++)
                {
                    VisualElement control = tabControls[i];
                    VisualElement row = FindSettingRow(control) ?? control;
                    if (ContainsPoint(row, panelPos))
                    {
                        return control;
                    }
                }

                if (ContainsPoint(_layout.SettingsBackButton, panelPos))
                {
                    return _layout.SettingsBackButton;
                }

                return null;
            }

            Button[] buttons = CurrentPageButtons(ctx.CurrentPage);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (ContainsPoint(buttons[i], panelPos))
                {
                    return buttons[i];
                }
            }

            return null;
        }

        private void StepButtonList(Button[] buttons, VisualElement focused, int delta)
        {
            int currentIdx = -1;
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == focused)
                {
                    currentIdx = i;
                    break;
                }
            }

            if (currentIdx < 0)
            {
                for (int i = 0; i < buttons.Length; i++)
                {
                    if (IsDisplayed(buttons[i]) && buttons[i].canGrabFocus)
                    {
                        FocusControl(buttons[i], byCode: false);
                        return;
                    }
                }

                return;
            }

            for (int idx = currentIdx + delta; idx >= 0 && idx < buttons.Length; idx += delta)
            {
                Button candidate = buttons[idx];
                if (IsDisplayed(candidate) && candidate.canGrabFocus)
                {
                    FocusControl(candidate, byCode: false);
                    return;
                }
            }
        }

        private void HandleSettingsNavigation(VisualElement focused, NavigationMoveEvent.Direction direction, int settingsTab)
        {
            int tabIdx = -1;
            for (int i = 0; i < _settingsTabs.Length; i++)
            {
                if (_settingsTabs[i] == focused)
                {
                    tabIdx = i;
                    break;
                }
            }

            VisualElement[] rows = _settingsTabControls[settingsTab];
            int rowIdx = -1;
            VisualElement focusedRow = FindSettingRow(focused);
            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i] == focused || (focusedRow != null && FindSettingRow(rows[i]) == focusedRow))
                {
                    rowIdx = i;
                    break;
                }
            }

            bool isBack = focused == _layout.SettingsBackButton;

            if (direction == NavigationMoveEvent.Direction.Up || direction == NavigationMoveEvent.Direction.Down)
            {
                int delta = direction == NavigationMoveEvent.Direction.Down ? 1 : -1;
                if (tabIdx >= 0)
                {
                    if (delta > 0 && rows.Length > 0)
                    {
                        FocusControl(rows[0], byCode: false);
                    }
                }
                else if (rowIdx >= 0)
                {
                    int nextRow = rowIdx + delta;
                    if (nextRow < 0)
                    {
                        FocusControl(_settingsTabs[settingsTab], byCode: false);
                    }
                    else if (nextRow < rows.Length)
                    {
                        FocusControl(rows[nextRow], byCode: false);
                    }
                    else
                    {
                        FocusControl(_layout.SettingsBackButton, byCode: false);
                    }
                }
                else if (isBack)
                {
                    if (delta < 0 && rows.Length > 0)
                    {
                        FocusControl(rows[rows.Length - 1], byCode: false);
                    }
                }
                else if (rows.Length > 0)
                {
                    FocusControl(rows[0], byCode: false);
                }

                return;
            }

            if (direction == NavigationMoveEvent.Direction.Left || direction == NavigationMoveEvent.Direction.Right)
            {
                int delta = direction == NavigationMoveEvent.Direction.Right ? 1 : -1;
                if (tabIdx >= 0)
                {
                    int nextTab = Mathf.Clamp(tabIdx + delta, 0, _settingsTabs.Length - 1);
                    if (nextTab != tabIdx)
                    {
                        _onSelectTab(nextTab);
                        FocusControl(_settingsTabs[nextTab], byCode: false);
                        _onCue(UiCueKind.Confirm);
                    }

                    return;
                }

                if (rowIdx >= 0)
                {
                    _onStepSettingsControl(rows[rowIdx], delta);
                }
            }
        }

        private Button[] CurrentPageButtons(PausePage page)
        {
            switch (page)
            {
                case PausePage.NewGame:
                    return _newGamePageButtons;
                case PausePage.Quit:
                    return _quitPageButtons;
                case PausePage.Language:
                    return _languageButtons;
                default:
                    return _mainPageButtons;
            }
        }

        private void SyncSettingRowFocus(VisualElement focused)
        {
            if (focused == _lastSyncedFocus)
            {
                return;
            }

            _lastSyncedFocus = focused;
            VisualElement focusedRow = focused != null ? FindSettingRow(focused) : null;
            for (int i = 0; i < _settingRows.Length; i++)
            {
                VisualElement row = _settingRows[i];
                if (row != null)
                {
                    row.EnableInClassList(FocusedRowClass, row == focusedRow);
                }
            }

            for (int i = 0; i < _settingsTabs.Length; i++)
            {
                Button tab = _settingsTabs[i];
                tab.parent.EnableInClassList(TabFocusClass, focused == tab);
            }

            VisualElement activeCursorHost = focusedRow ??
                (focused != null && (focused == _layout.TabSound || focused == _layout.TabCamera || focused == _layout.TabDisplay)
                    ? focused.parent
                    : focused);
            for (int i = 0; i < _titleCursors.Length; i++)
            {
                Label cursor = _titleCursors[i];
                if (cursor != null)
                {
                    cursor.style.display = (_cursorHosts[i] != null && _cursorHosts[i] == activeCursorHost)
                        ? DisplayStyle.Flex
                        : DisplayStyle.None;
                }
            }
        }

        private static VisualElement FindSettingRow(VisualElement control)
        {
            for (VisualElement current = control; current != null; current = current.parent)
            {
                if (current.ClassListContains("setting-row"))
                {
                    return current;
                }
            }

            return null;
        }

        private static bool ContainsPoint(VisualElement element, Vector2 panelPos)
        {
            return element != null && IsDisplayed(element) && element.worldBound.Contains(panelPos);
        }

        private static bool IsDisplayed(VisualElement element)
        {
            for (VisualElement current = element; current != null; current = current.parent)
            {
                if (current.style.display.keyword != StyleKeyword.Undefined &&
                    current.style.display.value == DisplayStyle.None)
                {
                    return false;
                }

                if (current.panel != null && current.resolvedStyle.display == DisplayStyle.None)
                {
                    return false;
                }
            }

            return true;
        }

        private static Label[] CreateTitleCursors(UiLayout layout, Button[] languageButtons, VisualElement[] settingRows,
            out VisualElement[] hosts)
        {
            VisualElement[] targets =
            {
                layout.ResumeButton,
                layout.NewGameButton,
                layout.SettingsButton,
                layout.CreditsButton,
                layout.QuitToTitleButton,
                layout.QuitButton,
                layout.NewGameKeepButton,
                layout.NewGameConfirmButton,
                layout.QuitStayButton,
                layout.QuitConfirmButton,
                layout.SettingsBackButton,
                layout.CreditsBack,
                layout.TabSound.parent,
                layout.TabCamera.parent,
                layout.TabDisplay.parent,
            };

            var cursors = new Label[targets.Length + languageButtons.Length + settingRows.Length];
            hosts = new VisualElement[cursors.Length];
            int idx = 0;
            for (int i = 0; i < targets.Length; i++)
            {
                hosts[idx] = targets[i];
                cursors[idx++] = AttachCursor(targets[i]);
            }

            for (int i = 0; i < languageButtons.Length; i++)
            {
                hosts[idx] = languageButtons[i];
                cursors[idx++] = AttachCursor(languageButtons[i]);
            }

            for (int i = 0; i < settingRows.Length; i++)
            {
                hosts[idx] = settingRows[i];
                cursors[idx++] = AttachCursor(settingRows[i]);
            }

            return cursors;
        }

        private static Label AttachCursor(VisualElement host)
        {
            var cursor = new Label(">")
            {
                pickingMode = PickingMode.Ignore,
            };
            cursor.style.display = DisplayStyle.None;
            cursor.AddToClassList("menu-button__cursor");
            host?.Add(cursor);
            return cursor;
        }
    }
}
