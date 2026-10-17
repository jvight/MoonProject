using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core.Events;
using MoonProject.UI.Editor;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// EditMode tests for <see cref="MenuNavigator"/>: directional navigation order across main/confirm/settings/credits
    /// pages, hold-to-repeat timing (<see cref="PauseSettings.AxisDeadzone"/>,
    /// <see cref="PauseSettings.RepeatInitialDelay"/>, <see cref="PauseSettings.RepeatInterval"/>), and focus restoration
    /// when focus is lost or a panel changes.
    /// </summary>
    public sealed class MenuNavigatorTests
    {
        private UiLayout _layout;
        private PauseSettings _settings;
        private Button[] _languageButtons;
        private MenuNavigationContext _context;
        private List<UiCueKind> _cues;
        private VisualElement _lastSteppedControl;
        private int _lastStepDelta;
        private int _creditsScrollSum;
        private MenuNavigator _navigator;

        [SetUp]
        public void SetUp()
        {
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiAssetPaths.SourceFolder + "/GameUI.uxml");
            Assert.IsNotNull(tree, "GameUI.uxml must exist");
            VisualElement root = tree.Instantiate();
            _layout = new UiLayout(root);
            _settings = new PauseSettings();
            _languageButtons = new[]
            {
                new Button { name = "lang-en", text = "English" },
                new Button { name = "lang-vi", text = "Tiếng Việt" },
            };
            for (int i = 0; i < _languageButtons.Length; i++)
            {
                _layout.PageLanguage.Add(_languageButtons[i]);
            }

            _cues = new List<UiCueKind>();
            _lastSteppedControl = null;
            _lastStepDelta = 0;
            _creditsScrollSum = 0;
            _context = new MenuNavigationContext(
                isActive: true,
                currentPage: PausePage.Main,
                pageDefaultFocus: PauseFocus.Resume,
                isCreditsOpen: false,
                isSettingsOpen: false,
                settingsTab: 0);

            _navigator = new MenuNavigator(
                _layout,
                _settings,
                _languageButtons,
                () => _context,
                cue => _cues.Add(cue),
                SelectTabInTest,
                (control, delta) =>
                {
                    _lastSteppedControl = control;
                    _lastStepDelta = delta;
                },
                scrollDelta => _creditsScrollSum += scrollDelta);
        }

        [Test]
        public void MainMenuNavigation_StepsThroughVisibleButtonsInOrder_AndClampsAtEnds()
        {
            // Simulate title menu with a save: Continue, New Game, Settings, Credits, Quit visible; QuitToTitle hidden.
            _layout.ResumeButton.style.display = DisplayStyle.Flex;
            _layout.NewGameButton.style.display = DisplayStyle.Flex;
            _layout.SettingsButton.style.display = DisplayStyle.Flex;
            _layout.CreditsButton.style.display = DisplayStyle.Flex;
            _layout.QuitToTitleButton.style.display = DisplayStyle.None;
            _layout.QuitButton.style.display = DisplayStyle.Flex;

            _navigator.FocusControl(_layout.ResumeButton, byCode: true);
            Assert.AreEqual(_layout.ResumeButton, _navigator.FocusedElement);

            // Up at top stays on ResumeButton.
            _navigator.HandleNavigation(NavigationMoveEvent.Direction.Up);
            Assert.AreEqual(_layout.ResumeButton, _navigator.FocusedElement);

            // Down steps through NewGame -> Settings -> Credits -> (skips hidden QuitToTitle) -> Quit.
            _navigator.HandleNavigation(NavigationMoveEvent.Direction.Down);
            Assert.AreEqual(_layout.NewGameButton, _navigator.FocusedElement);

            _navigator.HandleNavigation(NavigationMoveEvent.Direction.Down);
            Assert.AreEqual(_layout.SettingsButton, _navigator.FocusedElement);

            _navigator.HandleNavigation(NavigationMoveEvent.Direction.Down);
            Assert.AreEqual(_layout.CreditsButton, _navigator.FocusedElement);

            _navigator.HandleNavigation(NavigationMoveEvent.Direction.Down);
            Assert.AreEqual(_layout.QuitButton, _navigator.FocusedElement, "must skip hidden QuitToTitleButton");

            // Down at bottom stays on QuitButton.
            _navigator.HandleNavigation(NavigationMoveEvent.Direction.Down);
            Assert.AreEqual(_layout.QuitButton, _navigator.FocusedElement);

            // Up steps back to CreditsButton.
            _navigator.HandleNavigation(NavigationMoveEvent.Direction.Up);
            Assert.AreEqual(_layout.CreditsButton, _navigator.FocusedElement);
        }

        [Test]
        public void ConfirmAndLanguagePages_NavigateOnlyTheirOwnButtons()
        {
            _context = new MenuNavigationContext(
                isActive: true,
                currentPage: PausePage.NewGame,
                pageDefaultFocus: PauseFocus.NewGameKeep,
                isCreditsOpen: false,
                isSettingsOpen: false,
                settingsTab: 0);
            _navigator.RequestDefaultFocus();
            _navigator.EnsureFocusValid();
            Assert.AreEqual(_layout.NewGameKeepButton, _navigator.FocusedElement);

            _navigator.HandleNavigation(NavigationMoveEvent.Direction.Down);
            Assert.AreEqual(_layout.NewGameConfirmButton, _navigator.FocusedElement);

            _navigator.HandleNavigation(NavigationMoveEvent.Direction.Up);
            Assert.AreEqual(_layout.NewGameKeepButton, _navigator.FocusedElement);

            // Language page
            _context = new MenuNavigationContext(
                isActive: true,
                currentPage: PausePage.Language,
                pageDefaultFocus: PauseFocus.Language,
                isCreditsOpen: false,
                isSettingsOpen: false,
                settingsTab: 0);
            _navigator.RequestDefaultFocus();
            _navigator.EnsureFocusValid();
            Assert.AreEqual(_languageButtons[0], _navigator.FocusedElement);

            _navigator.HandleNavigation(NavigationMoveEvent.Direction.Down);
            Assert.AreEqual(_languageButtons[1], _navigator.FocusedElement);
        }

        [Test]
        public void SettingsNavigation_MovesBetweenTabsRowsAndBack_AndStepsControls()
        {
            _context = new MenuNavigationContext(
                isActive: true,
                currentPage: PausePage.Main,
                pageDefaultFocus: PauseFocus.Resume,
                isCreditsOpen: false,
                isSettingsOpen: true,
                settingsTab: 0);

            _navigator.RequestDefaultFocus();
            _navigator.EnsureFocusValid();
            Assert.AreEqual(_layout.MasterSlider, _navigator.FocusedElement);

            // Up from first row of Sound tab reaches TabSound.
            _navigator.HandleNavigation(NavigationMoveEvent.Direction.Up);
            Assert.AreEqual(_layout.TabSound, _navigator.FocusedElement);

            // Right on TabSound switches to TabCamera (tab 1) and focuses TabCamera.
            _navigator.HandleNavigation(NavigationMoveEvent.Direction.Right);
            Assert.AreEqual(1, _context.SettingsTab);
            Assert.AreEqual(_layout.TabCamera, _navigator.FocusedElement);

            // Down from TabCamera enters first row of Camera tab (LookSlider), then Invert, Gentle, Rumble, RumbleSlider, Guidance, then SettingsBackButton.
            VisualElement[] cameraRows =
            {
                _layout.LookSlider,
                _layout.InvertToggle,
                _layout.GentleCameraToggle,
                _layout.RumbleToggle,
                _layout.RumbleSlider,
                _layout.GuidanceToggle,
            };
            for (int i = 0; i < cameraRows.Length; i++)
            {
                _navigator.HandleNavigation(NavigationMoveEvent.Direction.Down);
                Assert.AreEqual(cameraRows[i], _navigator.FocusedElement, $"camera row {i}");
            }

            // Left/Right on a row steps that row's control.
            _navigator.HandleNavigation(NavigationMoveEvent.Direction.Right);
            Assert.AreEqual(_layout.GuidanceToggle, _lastSteppedControl);
            Assert.AreEqual(1, _lastStepDelta);

            // Down past last row focuses SettingsBackButton.
            _navigator.HandleNavigation(NavigationMoveEvent.Direction.Down);
            Assert.AreEqual(_layout.SettingsBackButton, _navigator.FocusedElement);

            // Up from SettingsBackButton returns to the last row (GuidanceToggle).
            _navigator.HandleNavigation(NavigationMoveEvent.Direction.Up);
            Assert.AreEqual(_layout.GuidanceToggle, _navigator.FocusedElement);
        }

        [Test]
        public void CreditsNavigation_ScrollsUpAndDown_WhileKeepingCreditsBackFocused()
        {
            _context = new MenuNavigationContext(
                isActive: true,
                currentPage: PausePage.Main,
                pageDefaultFocus: PauseFocus.Resume,
                isCreditsOpen: true,
                isSettingsOpen: false,
                settingsTab: 0);

            _navigator.RequestDefaultFocus();
            _navigator.EnsureFocusValid();
            Assert.AreEqual(_layout.CreditsBack, _navigator.FocusedElement);

            _navigator.HandleNavigation(NavigationMoveEvent.Direction.Down);
            Assert.AreEqual(1, _creditsScrollSum);
            Assert.AreEqual(_layout.CreditsBack, _navigator.FocusedElement);

            _navigator.HandleNavigation(NavigationMoveEvent.Direction.Up);
            Assert.AreEqual(0, _creditsScrollSum);
            Assert.AreEqual(_layout.CreditsBack, _navigator.FocusedElement);
        }

        [Test]
        public void RepeatTiming_RespectsDeadzone_InitialDelay_RepeatInterval_AndDirectionChanges()
        {
            float initialDelay = _settings.RepeatInitialDelay;
            float interval = _settings.RepeatInterval;
            float belowDeadzone = _settings.AxisDeadzone - 0.05f;

            // Below deadzone does not fire.
            Assert.IsFalse(_navigator.TryStepRepeat(new Vector2(0f, -belowDeadzone), 0.1f, out _));

            // Initial deflection past deadzone fires immediately.
            Assert.IsTrue(_navigator.TryStepRepeat(new Vector2(0f, -1f), 0.016f, out NavigationMoveEvent.Direction dir));
            Assert.AreEqual(NavigationMoveEvent.Direction.Down, dir);

            // Holding for less than RepeatInitialDelay does not fire.
            Assert.IsFalse(_navigator.TryStepRepeat(new Vector2(0f, -1f), initialDelay * 0.5f, out _));
            Assert.IsFalse(_navigator.TryStepRepeat(new Vector2(0f, -1f), initialDelay * 0.4f, out _));

            // Crossing RepeatInitialDelay fires once.
            Assert.IsTrue(_navigator.TryStepRepeat(new Vector2(0f, -1f), initialDelay * 0.2f, out dir));
            Assert.AreEqual(NavigationMoveEvent.Direction.Down, dir);

            // Subsequent hold fires after RepeatInterval.
            Assert.IsFalse(_navigator.TryStepRepeat(new Vector2(0f, -1f), interval * 0.5f, out _));
            Assert.IsTrue(_navigator.TryStepRepeat(new Vector2(0f, -1f), interval * 0.6f, out dir));
            Assert.AreEqual(NavigationMoveEvent.Direction.Down, dir);

            // Switching direction fires immediately and re-arms RepeatInitialDelay.
            Assert.IsTrue(_navigator.TryStepRepeat(new Vector2(1f, 0f), 0.016f, out dir));
            Assert.AreEqual(NavigationMoveEvent.Direction.Right, dir);
            Assert.IsFalse(_navigator.TryStepRepeat(new Vector2(1f, 0f), interval, out _),
                "direction change must re-arm initial delay, not repeat interval");

            // Releasing to zero resets the timer so a fresh press fires immediately.
            Assert.IsFalse(_navigator.TryStepRepeat(Vector2.zero, 0.016f, out _));
            Assert.IsTrue(_navigator.TryStepRepeat(new Vector2(0f, 1f), 0.016f, out dir));
            Assert.AreEqual(NavigationMoveEvent.Direction.Up, dir);
        }

        [Test]
        public void FocusRestore_RecoversLastValidFocus_OrFallsBackToDefaultWhenPanelChanges()
        {
            _layout.SettingsButton.style.display = DisplayStyle.Flex;
            _navigator.FocusControl(_layout.SettingsButton, byCode: false);
            Assert.AreEqual(_layout.SettingsButton, _navigator.FocusedElement);
            Assert.AreEqual(_layout.SettingsButton, _navigator.LastValidFocus);

            // Simulate losing focus (e.g. clicking empty background): EnsureFocusValid restores LastValidFocus.
            _navigator.Blur();
            Assert.IsNull(_navigator.FocusedElement);
            _navigator.EnsureFocusValid();
            Assert.AreEqual(_layout.SettingsButton, _navigator.FocusedElement,
                "lost focus must restore LastValidFocus when still valid for the current state");

            // Now switch context to Settings open without explicitly requesting focus: SettingsButton is no longer valid
            // for PauseSettings, so EnsureFocusValid falls back to DefaultFocus (MasterSlider).
            _context = new MenuNavigationContext(
                isActive: true,
                currentPage: PausePage.Main,
                pageDefaultFocus: PauseFocus.Resume,
                isCreditsOpen: false,
                isSettingsOpen: true,
                settingsTab: 0);
            _navigator.EnsureFocusValid();
            Assert.AreEqual(_layout.MasterSlider, _navigator.FocusedElement,
                "invalid focus from previous panel must fall back to DefaultFocus of active panel");
        }

        private void SelectTabInTest(int tab)
        {
            _context = new MenuNavigationContext(
                _context.IsActive,
                _context.CurrentPage,
                _context.PageDefaultFocus,
                _context.IsCreditsOpen,
                _context.IsSettingsOpen,
                tab);
        }
    }
}
