using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// The page change between 07's face pages, HOLD and DATABANK (pure, EditMode-tested), as a CRT redraws: the old
    /// page's phosphor fades down to the tube's dark, the page switches, and the new page is drawn in from the top
    /// behind a soft scan edge. Nothing slides, bounces or flashes; the two pages are never on the tube together; the
    /// tab underline glides across for the whole change. A press while a change runs is ignored, so it never flips
    /// twice and settles exactly once (docs/design/databank.md §4).
    /// </summary>
    internal sealed class PageFlip
    {
        private enum Phase
        {
            Idle,
            Fading,
            Drawing,
        }

        private readonly DatabankSettings _settings;
        private Phase _phase;
        private float _t;
        private FacePage _target;
        private float _elapsed;

        public PageFlip(DatabankSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>The page on the tube (it changes when the old page has faded out).</summary>
        public FacePage Page { get; private set; } = FacePage.Hold;

        /// <summary>The page the tube is on or changing to.</summary>
        public FacePage Target => _phase == Phase.Idle ? Page : _target;

        public bool IsFlipping => _phase != Phase.Idle;

        /// <summary>True on the step the page changed.</summary>
        public bool Switched { get; private set; }

        /// <summary>True on the step the new page finished drawing in: it has settled on the tube.</summary>
        public bool Settled { get; private set; }

        /// <summary>Seconds a whole change takes: the fade down, then the draw in.</summary>
        public float Duration => _settings.FlipFadeSeconds + _settings.FlipDrawSeconds;

        /// <summary>The page's brightness: it fades down before the switch, and is whole while it draws in.</summary>
        public float ContentAlpha
        {
            get
            {
                if (_phase != Phase.Fading)
                {
                    return 1f;
                }

                float ease = 1f - _t;
                return ease * ease;
            }
        }

        /// <summary>The share of the page drawn from its top (1 at rest and while the old page fades).</summary>
        public float Drawn
        {
            get
            {
                if (_phase != Phase.Drawing)
                {
                    return 1f;
                }

                float left = 1f - _t;
                return 1f - left * left * left;
            }
        }

        /// <summary>Where the tab underline stands: 0 under HOLD, 1 under DATABANK, gliding for the whole change.</summary>
        public float TabPosition => _phase == Phase.Idle
            ? Position(Page)
            : Mathf.Lerp(Position(Other(_target)), Position(_target),
                Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_elapsed / Duration)));

        /// <summary>Changes to the other page; ignored while a change already runs.</summary>
        public void Toggle()
        {
            if (_phase != Phase.Idle)
            {
                return;
            }

            _target = Other(Page);
            _phase = Phase.Fading;
            _t = 0f;
            _elapsed = 0f;
        }

        /// <summary>The face closed: the next open starts on <paramref name="page"/>, at rest.</summary>
        public void Snap(FacePage page)
        {
            Page = page;
            _phase = Phase.Idle;
            _t = 0f;
            _elapsed = 0f;
        }

        /// <param name="deltaTime">Unscaled seconds.</param>
        public void Tick(float deltaTime)
        {
            Switched = false;
            Settled = false;
            if (_phase == Phase.Idle)
            {
                return;
            }

            _elapsed += deltaTime;
            if (_phase == Phase.Fading)
            {
                _t += deltaTime / _settings.FlipFadeSeconds;
                if (_t >= 1f)
                {
                    _t = 0f;
                    Page = _target;
                    Switched = true;
                    _phase = Phase.Drawing;
                }

                return;
            }

            _t += deltaTime / _settings.FlipDrawSeconds;
            if (_t >= 1f)
            {
                _t = 0f;
                _phase = Phase.Idle;
                Settled = true;
            }
        }

        private static FacePage Other(FacePage page)
        {
            return page == FacePage.Hold ? FacePage.Databank : FacePage.Hold;
        }

        private static float Position(FacePage page)
        {
            return page == FacePage.Databank ? 1f : 0f;
        }
    }
}
