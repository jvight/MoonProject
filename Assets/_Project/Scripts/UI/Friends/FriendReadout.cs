using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// The parts readout: while 07 is near a broken friend, a small glass chip floats above it with one warm amber pip
    /// per missing part, filled for each part brought back (0/3 ... 3/3) — no glyphs, no numbers. It eases in and
    /// out, follows the friend on screen, rests on top of the repair prompt when that shares its point, hides while
    /// the friend is behind the camera, and leaves for good once the friend is repaired.
    /// </summary>
    internal sealed class FriendReadout
    {
        public const string PipClass = "pip";
        public const string PipFillClass = "pip__fill";
        public const string PipFullClass = "pip--full";

        private const float WriteEpsilon = 1e-3f;
        private const float RaiseEpsilon = 0.5f;

        private readonly FriendUiSettings _settings;
        private readonly IFriendStatuses _friends;
        private readonly IRoverState _rover;
        private readonly ILocalization _localization;
        private readonly Label _label;
        private readonly FriendFocus _focus;
        private readonly PipFill _fill;
        private readonly Reveal _reveal;
        private readonly WorldAnchor _anchor;
        private readonly VisualElement _pips;
        private readonly VisualElement _chip;
        private readonly List<VisualElement> _pipFills = new List<VisualElement>();
        private readonly List<float> _writtenFills = new List<float>();
        private int _shownFriend = FriendFocus.None;
        private float _writtenRaise = -1f;
        private int _writtenCollected = -1;
        private string[] _labels = new string[0];

        public FriendReadout(UiLayout layout, FriendUiSettings settings, PromptSettings anchoring,
            IFriendStatuses friends, IRoverState rover, IViewCamera view, ILocalization localization)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            if (anchoring == null)
            {
                throw new ArgumentNullException(nameof(anchoring));
            }

            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _friends = friends ?? throw new ArgumentNullException(nameof(friends));
            _rover = rover ?? throw new ArgumentNullException(nameof(rover));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _label = layout.FriendPartsLabel;
            _focus = new FriendFocus(settings);
            _fill = new PipFill(settings);
            _reveal = new Reveal(layout.FriendReadout, settings.Readout);
            _reveal.Snap(false);
            _anchor = new WorldAnchor(layout.FriendAnchor, view, anchoring.ScreenMargin, anchoring.FollowHalfLife,
                false);
            _pips = layout.FriendPips;
            _chip = layout.FriendReadout;
            new ShadowPainter(layout.FriendReadoutShadow);
        }

        public bool IsVisible => !_reveal.IsHidden;

        /// <summary>The friend the readout speaks about (or <see cref="FriendFocus.None"/>).</summary>
        public int Friend => _shownFriend;

        /// <summary>Number of pips on the chip.</summary>
        public int PipCount => _pipFills.Count;

        /// <summary>Parts shown as filled (fractional while a pip fills).</summary>
        public float ShownParts => _fill.Shown;

        /// <summary>Writes the label again (a new language).</summary>
        public void Relocalize()
        {
            _writtenCollected = -1;
            _labels = new string[0];
            if (_shownFriend != FriendFocus.None)
            {
                WriteLabel(_friends.Status(_shownFriend));
            }
        }

        /// <param name="deltaTime">Unscaled seconds since the last tick.</param>
        /// <param name="gateOpen">False while paused.</param>
        /// <param name="panelSize">Size of the UI panel.</param>
        /// <param name="raise">Pixels to rise to rest on top of the repair prompt (0 when it is not shown).</param>
        public void Tick(float deltaTime, bool gateOpen, Vector2 panelSize, float raise)
        {
            if (Mathf.Abs(raise - _writtenRaise) > RaiseEpsilon)
            {
                _chip.style.bottom = raise;
                _writtenRaise = raise;
            }

            int focus = _focus.Step(_friends, _rover.Position);
            if (focus != FriendFocus.None && focus != _shownFriend && _reveal.IsHidden)
            {
                Show(focus);
            }

            bool onScreen = false;
            if (_shownFriend != FriendFocus.None)
            {
                FriendStatus status = _friends.Status(_shownFriend);
                WriteLabel(status);
                if (_fill.Step(status.Collected, deltaTime))
                {
                    WritePips();
                }

                onScreen = _anchor.Track(status.Position + Vector3.up * _settings.ReadoutLiftMetres, panelSize,
                    deltaTime, _reveal.IsHidden);
            }

            _reveal.Set(gateOpen && focus != FriendFocus.None && focus == _shownFriend && onScreen);
            _reveal.Tick(deltaTime);
        }

        private void Show(int friend)
        {
            _shownFriend = friend;
            FriendStatus status = _friends.Status(friend);
            if (status.Total != _pipFills.Count)
            {
                BuildPips(status.Total);
            }

            _fill.Snap(status.Collected);
            WritePips();
            _writtenCollected = -1;
            _labels = new string[0];
            WriteLabel(status);
        }

        /// <summary>"Tilly's parts: 2 of 3", formatted only when the count or the friend changes.</summary>
        private void WriteLabel(FriendStatus status)
        {
            if (status.Collected == _writtenCollected)
            {
                return;
            }

            _writtenCollected = status.Collected;
            if (_labels.Length <= status.Total)
            {
                _labels = new string[status.Total + 1];
            }

            int count = Mathf.Clamp(status.Collected, 0, status.Total);
            _label.text = _labels[count] ?? (_labels[count] = string.Format(_localization.Get(UiKeys.FriendParts),
                _localization.Get(UiKeys.FriendName(_friends.Definition(_shownFriend).Id)), count, status.Total));
        }

        private void BuildPips(int count)
        {
            _pips.Clear();
            _pipFills.Clear();
            _writtenFills.Clear();
            for (int i = 0; i < count; i++)
            {
                var pip = new VisualElement { pickingMode = PickingMode.Ignore };
                pip.AddToClassList(PipClass);
                var fill = new VisualElement { pickingMode = PickingMode.Ignore };
                fill.AddToClassList(PipFillClass);
                pip.Add(fill);
                _pips.Add(pip);
                _pipFills.Add(fill);
                _writtenFills.Add(-1f);
            }
        }

        private void WritePips()
        {
            for (int i = 0; i < _pipFills.Count; i++)
            {
                float fill = _fill.Of(i);
                if (Mathf.Abs(fill - _writtenFills[i]) < WriteEpsilon)
                {
                    continue;
                }

                VisualElement element = _pipFills[i];
                element.style.opacity = fill;
                element.style.scale = new StyleScale(new Scale(new Vector2(fill, fill)));
                element.parent.EnableInClassList(PipFullClass, fill >= 1f);
                _writtenFills[i] = fill;
            }
        }
    }
}
