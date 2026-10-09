using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// The first time a friend wakes, its name (the localized "friend.&lt;id&gt;.name") drifts up over it, rests a
    /// moment while it rises into the air, and fades away (a <see cref="FloatingName"/>). Friends wake once, so each
    /// name shows once.
    /// </summary>
    internal sealed class FriendNameTag
    {
        private readonly FriendUiSettings _settings;
        private readonly IFriendStatuses _friends;
        private readonly ILocalization _localization;
        private readonly FloatingName _name;
        private int _friend = FriendFocus.None;

        public FriendNameTag(UiLayout layout, FriendUiSettings settings, PromptSettings anchoring,
            IFriendStatuses friends, ILocalization localization, IViewCamera view)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _friends = friends ?? throw new ArgumentNullException(nameof(friends));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _name = new FloatingName(layout.FriendNameAnchor, layout.FriendName, layout.FriendNameShadow,
                layout.FriendNameText, settings.Name, anchoring, view);
        }

        public bool IsVisible => _name.IsVisible;

        /// <summary>Shows the name of friend <paramref name="index"/> (its wake-up moment).</summary>
        public void Show(int index)
        {
            _friend = index;
            _name.Show(_localization.Get(UiKeys.FriendName(_friends.Definition(index).Id)), _settings.NameHoldSeconds);
        }

        /// <summary>Re-reads the name in the new language (a name on screen changes in place).</summary>
        public void Relocalize()
        {
            if (_friend != FriendFocus.None)
            {
                _name.Rename(_localization.Get(UiKeys.FriendName(_friends.Definition(_friend).Id)));
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

            FriendStatus status = _friends.Status(_friend);
            _name.Tick(status.Position + Vector3.up * _settings.NameLiftMetres, deltaTime, panelSize);
            if (!_name.IsUp)
            {
                _friend = FriendFocus.None;
            }
        }
    }
}
