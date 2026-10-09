using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// The first time in a session that a salvage site answers the sonar, its name ("site.&lt;id&gt;.name") drifts up
    /// over it, rests a moment and fades away, like a friend's name when it wakes (a <see cref="FloatingName"/>). One
    /// name at a time: a site that answers while another name is up keeps its turn for its next answer, so nothing
    /// queues up or stacks. Hidden while the site is off screen.
    /// </summary>
    internal sealed class SiteNameTag
    {
        private readonly SalvageSettings _settings;
        private readonly ILocalization _localization;
        private readonly FloatingName _name;
        private readonly HashSet<string> _named = new HashSet<string>(StringComparer.Ordinal);
        private string _siteId;
        private Vector3 _point;

        public SiteNameTag(UiLayout layout, SalvageSettings settings, PromptSettings anchoring,
            ILocalization localization, IViewCamera view)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _name = new FloatingName(layout.SiteNameAnchor, layout.SiteName, layout.SiteNameShadow,
                layout.SiteNameText, settings.SiteName, anchoring, view);
        }

        public bool IsVisible => _name.IsVisible;

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
            _name.Show(_localization.Get(UiKeys.SiteName(siteId)), _settings.SiteNameHoldSeconds);
        }

        /// <summary>Re-reads the name on screen in the new language.</summary>
        public void Relocalize()
        {
            if (_siteId != null)
            {
                _name.Rename(_localization.Get(UiKeys.SiteName(_siteId)));
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

            _name.Tick(_point, deltaTime, panelSize);
            if (!_name.IsUp)
            {
                _siteId = null;
            }
        }
    }
}
