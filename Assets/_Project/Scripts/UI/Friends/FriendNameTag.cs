using System;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// The first time a friend wakes, its name (the localized "friend.&lt;id&gt;.name") drifts up over it, rests a
    /// moment while it rises into the air, and fades away. Friends wake once, so each name shows once.
    /// </summary>
    internal sealed class FriendNameTag
    {
        private readonly FriendUiSettings _settings;
        private readonly IFriendStatuses _friends;
        private readonly ILocalization _localization;
        private readonly Reveal _reveal;
        private readonly WorldAnchor _anchor;
        private readonly Label _label;
        private int _friend = FriendFocus.None;
        private float _rest;
        private bool _onScreen = true;

        public FriendNameTag(UiLayout layout, FriendUiSettings settings, PromptSettings anchoring,
            IFriendStatuses friends, ILocalization localization, IViewCamera view)
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
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _label = layout.FriendName;
            _reveal = new Reveal(_label, settings.Name);
            _reveal.Snap(false);
            _anchor = new WorldAnchor(layout.FriendNameAnchor, view, anchoring.ScreenMargin,
                anchoring.FollowHalfLife, false);
        }

        public bool IsVisible => !_reveal.IsHidden;

        /// <summary>Shows the name of friend <paramref name="index"/> (its wake-up moment).</summary>
        public void Show(int index)
        {
            _friend = index;
            _label.text = _localization.Get(UiKeys.FriendName(_friends.Definition(index).Id));
            _rest = _settings.NameHoldSeconds;
            _reveal.Show();
        }

        /// <summary>Re-reads the name in the new language (a name on screen changes in place).</summary>
        public void Relocalize()
        {
            if (_friend != FriendFocus.None)
            {
                _label.text = _localization.Get(UiKeys.FriendName(_friends.Definition(_friend).Id));
            }
        }

        /// <param name="deltaTime">Unscaled seconds; pass 0 while paused so the name waits too.</param>
        /// <param name="panelSize">Size of the UI panel.</param>
        public void Tick(float deltaTime, Vector2 panelSize)
        {
            if (_friend == FriendFocus.None)
            {
                return;
            }

            if (_reveal.IsShown)
            {
                _rest -= deltaTime;
                if (_rest <= 0f)
                {
                    _reveal.Hide();
                }
            }

            FriendStatus status = _friends.Status(_friend);
            bool onScreen = _anchor.Track(status.Position + Vector3.up * _settings.NameLiftMetres, panelSize, deltaTime,
                _reveal.IsHidden);
            if (onScreen != _onScreen)
            {
                _label.visible = onScreen;
                _onScreen = onScreen;
            }

            _reveal.Tick(deltaTime);
            if (_reveal.IsHidden && !_reveal.Target)
            {
                _friend = FriendFocus.None;
            }
        }
    }
}
