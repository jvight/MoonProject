using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// The CRT channel flip between 07's face pages, HOLD and DATABANK (pure, EditMode-tested): the page collapses to
    /// the tube's bright line, the other page takes its place at the thinnest moment, and the line opens again
    /// (docs/design/databank.md §4).
    /// </summary>
    internal sealed class PageFlip
    {
        private enum Phase
        {
            Idle,
            Collapsing,
            Opening,
        }

        private readonly DatabankSettings _settings;
        private Phase _phase;
        private float _t;
        private FacePage _target;

        public PageFlip(DatabankSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>The page on the tube (it changes at the flip's thinnest moment).</summary>
        public FacePage Page { get; private set; } = FacePage.Hold;

        /// <summary>The page the tube is on or flipping to.</summary>
        public FacePage Target => _phase == Phase.Idle ? Page : _target;

        public bool IsFlipping => _phase != Phase.Idle;

        /// <summary>True on the step the page changed.</summary>
        public bool Switched { get; private set; }

        /// <summary>The page's height as a share of its own (1 at rest, the line share at the thinnest).</summary>
        public float ScaleY
        {
            get
            {
                float line = _settings.FlipLineShare;
                switch (_phase)
                {
                    case Phase.Collapsing:
                        return Mathf.Lerp(1f, line, Mathf.SmoothStep(0f, 1f, _t));
                    case Phase.Opening:
                        return Mathf.Lerp(line, 1f, Mathf.SmoothStep(0f, 1f, _t));
                    default:
                        return 1f;
                }
            }
        }

        /// <summary>Flips to the other page (a flip already running turns toward it again).</summary>
        public void Toggle()
        {
            FacePage to = Target == FacePage.Hold ? FacePage.Databank : FacePage.Hold;
            switch (_phase)
            {
                case Phase.Idle:
                    _target = to;
                    _phase = Phase.Collapsing;
                    _t = 0f;
                    break;
                case Phase.Collapsing:
                    _target = to;
                    break;
                default:
                    _target = to;
                    _phase = Phase.Collapsing;
                    _t = 1f - _t;
                    break;
            }
        }

        /// <summary>The face closed: the next open starts on <paramref name="page"/>, at rest.</summary>
        public void Snap(FacePage page)
        {
            Page = page;
            _phase = Phase.Idle;
            _t = 0f;
        }

        /// <param name="deltaTime">Unscaled seconds.</param>
        public void Tick(float deltaTime)
        {
            Switched = false;
            switch (_phase)
            {
                case Phase.Collapsing:
                    _t += deltaTime / _settings.FlipCollapseSeconds;
                    if (_t >= 1f)
                    {
                        _t = 0f;
                        Switched = Page != _target;
                        Page = _target;
                        _phase = Phase.Opening;
                    }

                    break;
                case Phase.Opening:
                    _t += deltaTime / _settings.FlipOpenSeconds;
                    if (_t >= 1f)
                    {
                        _t = 0f;
                        _phase = Phase.Idle;
                    }

                    break;
            }
        }
    }
}
