using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// The first time in a session that a salvage site answers the sonar, its name ("site.&lt;id&gt;.name") drifts up
    /// over it, rests a moment and fades away, like a friend's name when it wakes. One name at a time: a site that
    /// answers while another name is up keeps its turn for its next answer, so nothing queues up or stacks. Hidden
    /// while the site is off screen.
    /// </summary>
    internal sealed class SiteNameTag
    {
        private readonly SalvageSettings _settings;
        private readonly ILocalization _localization;
        private readonly Reveal _reveal;
        private readonly WorldAnchor _anchor;
        private readonly Label _label;
        private readonly HashSet<string> _named = new HashSet<string>(StringComparer.Ordinal);
        private string _siteId;
        private Vector3 _point;
        private float _rest;
        private bool _onScreen = true;

        public SiteNameTag(UiLayout layout, SalvageSettings settings, PromptSettings anchoring,
            ILocalization localization, IViewCamera view)
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
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _label = layout.SiteName;
            _reveal = new Reveal(_label, settings.SiteName);
            _reveal.Snap(false);
            _anchor = new WorldAnchor(layout.SiteNameAnchor, view, anchoring.ScreenMargin, anchoring.FollowHalfLife,
                false);
        }

        public bool IsVisible => !_reveal.IsHidden;

        /// <summary>The site whose name is up (null when none).</summary>
        public string Current => _siteId;

        /// <summary>A site answered the sonar: its name shows if this is its first answer and no name is up.</summary>
        public void Answered(string siteId, Vector3 position)
        {
            if (_siteId != null || string.IsNullOrEmpty(siteId) || !_named.Add(siteId))
            {
                return;
            }

            _siteId = siteId;
            _point = position + Vector3.up * _settings.SiteNameLiftMetres;
            _label.text = _localization.Get(UiKeys.SiteName(siteId));
            _rest = _settings.SiteNameHoldSeconds;
            _reveal.Show();
        }

        /// <summary>Re-reads the name on screen in the new language.</summary>
        public void Relocalize()
        {
            if (_siteId != null)
            {
                _label.text = _localization.Get(UiKeys.SiteName(_siteId));
            }
        }

        /// <param name="deltaTime">Unscaled seconds; 0 while paused, so the name waits too.</param>
        /// <param name="panelSize">Size of the UI panel.</param>
        public void Tick(float deltaTime, Vector2 panelSize)
        {
            if (_siteId == null)
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

            bool onScreen = _anchor.Track(_point, panelSize, deltaTime, _reveal.IsHidden);
            if (onScreen != _onScreen)
            {
                _label.visible = onScreen;
                _onScreen = onScreen;
            }

            _reveal.Tick(deltaTime);
            if (_reveal.IsHidden && !_reveal.Target)
            {
                _siteId = null;
            }
        }
    }
}
