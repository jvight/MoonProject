using System;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// Keeps a zero-size anchor element over a world point: projected through the player's camera, softly following
    /// the point so it floats rather than jitters. A prompt is held a margin inside the screen edges (it names
    /// something to do right now); a label about a thing only shows while that thing is on screen. The anchor's
    /// content hangs centred above it (USS .world-anchor__center). Writes the style only when it moves.
    /// </summary>
    internal sealed class WorldAnchor
    {
        private const float MoveEpsilon = 0.1f;

        private readonly VisualElement _anchor;
        private readonly IViewCamera _view;
        private readonly float _margin;
        private readonly float _followHalfLife;
        private readonly bool _keepOnScreen;
        private Vector2 _screen;
        private Vector2 _written = new Vector2(float.NaN, float.NaN);

        /// <param name="anchor">The zero-size element to move.</param>
        /// <param name="view">The player's camera.</param>
        /// <param name="margin">Pixels (at 1080p) kept between the anchor and the screen edges.</param>
        /// <param name="followHalfLife">Seconds to close half the gap to a moving point.</param>
        /// <param name="keepOnScreen">
        /// True: a point beside the screen is shown at the nearest edge. False: it is treated as not visible.
        /// </param>
        public WorldAnchor(VisualElement anchor, IViewCamera view, float margin, float followHalfLife,
            bool keepOnScreen)
        {
            _anchor = anchor ?? throw new ArgumentNullException(nameof(anchor));
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _margin = margin;
            _followHalfLife = followHalfLife;
            _keepOnScreen = keepOnScreen;
        }

        /// <summary>
        /// Moves the anchor towards <paramref name="worldPoint"/> (straight there when <paramref name="snap"/>).
        /// Returns false, leaving the anchor where it was, when the point is behind the camera (or beside the screen,
        /// unless kept on screen) or there is no panel.
        /// </summary>
        public bool Track(Vector3 worldPoint, Vector2 panelSize, float deltaTime, bool snap)
        {
            Camera camera = _view.Camera;
            if (camera == null || panelSize.x <= 0f || panelSize.y <= 0f)
            {
                return false;
            }

            Vector3 viewport = camera.WorldToViewportPoint(worldPoint);
            bool beside = viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f;
            if ((beside && !_keepOnScreen) ||
                !ScreenAnchor.TryToPanel(viewport, panelSize, _margin, out Vector2 target))
            {
                return false;
            }

            _screen = snap ? target : ScreenAnchor.Follow(_screen, target, _followHalfLife, deltaTime);
            if (float.IsNaN(_written.x) || (_screen - _written).sqrMagnitude > MoveEpsilon * MoveEpsilon)
            {
                _anchor.style.translate = new StyleTranslate(new Translate(_screen.x, _screen.y));
                _written = _screen;
            }

            return true;
        }
    }
}
