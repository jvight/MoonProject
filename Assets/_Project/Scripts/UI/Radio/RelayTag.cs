using System;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// The price tag at a dark relay mast (docs/features/M3-06): while 07 holds the mast's part beside it, a small
    /// glass chip floats over the part socket with the scrap nut inside a ring and the scrap the restoration costs
    /// (<see cref="IRelayStatus.NextCost"/>). The ring fills as Interact is held
    /// (<see cref="IRelayStatus.RestoreHold"/>). When the scrap is not there yet the cost is simply dimmed: no words,
    /// no nagging. Unlike the Restore prompt, which teaches the action only the first few times, a price is shown at
    /// every mast. It rests on top of the Restore prompt when that shares its point, and hides while the socket is off
    /// screen.
    /// </summary>
    internal sealed class RelayTag
    {
        public const string ShortClass = "relay-tag--short";

        private const float RaiseEpsilon = 0.5f;

        private readonly RelaySettings _settings;
        private readonly IInteractionHints _hints;
        private readonly IRelayStatus _relays;
        private readonly IntText _numbers;
        private readonly Reveal _reveal;
        private readonly WorldAnchor _anchor;
        private readonly VisualElement _chip;
        private readonly Label _cost;
        private readonly ProgressRingPainter _ring;
        private int _writtenCost = -1;
        private bool _writtenShort;
        private float _writtenRaise = -1f;

        public RelayTag(UiLayout layout, RelaySettings settings, PromptSettings anchoring, IInteractionHints hints,
            IRelayStatus relays, IViewCamera view, IntText numbers)
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
            _hints = hints ?? throw new ArgumentNullException(nameof(hints));
            _relays = relays ?? throw new ArgumentNullException(nameof(relays));
            _numbers = numbers ?? throw new ArgumentNullException(nameof(numbers));
            _chip = layout.RelayTag;
            _cost = layout.RelayTagCost;
            _reveal = new Reveal(layout.RelayTag, settings.Tag);
            _reveal.Snap(false);
            _anchor = new WorldAnchor(layout.RelayTagAnchor, view, anchoring.ScreenMargin, anchoring.FollowHalfLife,
                false);
            _ring = new ProgressRingPainter(layout.RelayTagRing);
            new ShadowPainter(layout.RelayTagShadow);
            new ScrapIconPainter(layout.RelayTagIcon);
        }

        public bool IsVisible => !_reveal.IsHidden;

        /// <summary>True while the cost is shown dimmed (the scrap is not there yet).</summary>
        public bool IsShort => _writtenShort;

        /// <summary>The ring's fill (tests and captures).</summary>
        public float Hold => _ring.Progress;

        /// <param name="deltaTime">Unscaled seconds since the last tick.</param>
        /// <param name="gateOpen">False while something else has the player's attention.</param>
        /// <param name="panelSize">Size of the UI panel.</param>
        /// <param name="raise">Pixels to rise to rest on top of the Restore prompt (0 when it is not shown).</param>
        public void Tick(float deltaTime, bool gateOpen, Vector2 panelSize, float raise)
        {
            if (Mathf.Abs(raise - _writtenRaise) > RaiseEpsilon)
            {
                _chip.style.bottom = raise;
                _writtenRaise = raise;
            }

            bool offered = _hints.TryGet(InteractionKind.Restore, out InteractionHint hint);
            bool onScreen = offered && _anchor.Track(hint.Position + Vector3.up * _settings.TagLiftMetres, panelSize,
                deltaTime, _reveal.IsHidden);
            if (offered)
            {
                Write(_relays.NextCost.Total, !hint.Ready);
            }

            _ring.Progress = offered ? _relays.RestoreHold : 0f;
            _reveal.Set(gateOpen && offered && onScreen);
            _reveal.Tick(deltaTime);
        }

        private void Write(int cost, bool isShort)
        {
            if (cost != _writtenCost)
            {
                _cost.text = _numbers.Get(cost);
                _writtenCost = cost;
            }

            if (isShort != _writtenShort)
            {
                _chip.EnableInClassList(ShortClass, isShort);
                _writtenShort = isShort;
            }
        }
    }
}
