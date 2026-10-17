using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// The text size setting (docs/design/first-hour-v2 6.1): 100, 125 or 150% of every size in the UI. The panel scales
    /// by the screen's height from its reference resolution, so a bigger text size is a smaller reference height:
    /// every size, space and card grows together and the layouts wrap rather than clip. The panel's own reference
    /// resolution is put back when the UI goes away.
    /// </summary>
    internal sealed class TextScale
    {
        private readonly PanelSettings _panel;
        private readonly Vector2Int _normal;
        private float _applied = 1f;

        public TextScale(PanelSettings panel)
        {
            _panel = panel ?? throw new ArgumentNullException(nameof(panel));
            _panel.screenMatchMode = PanelScreenMatchMode.Expand;
            _normal = panel.referenceResolution;
        }

        /// <summary>Makes the UI <paramref name="scale"/> times the normal size (cheap when unchanged).</summary>
        public void Apply(float scale)
        {
            if (scale < 1f)
            {
                throw new ArgumentOutOfRangeException(nameof(scale), scale, "The text size cannot shrink the UI.");
            }

            if (Mathf.Approximately(scale, _applied))
            {
                return;
            }

            _panel.referenceResolution = new Vector2Int(Mathf.RoundToInt(_normal.x / scale),
                Mathf.RoundToInt(_normal.y / scale));
            _applied = scale;
        }

        /// <summary>Puts the panel's reference resolution back.</summary>
        public void Restore()
        {
            _panel.referenceResolution = _normal;
            _applied = 1f;
        }
    }
}
