using System;
using UnityEngine;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// Decides which context prompt to show (pure logic, EditMode-tested). Design ruling 6, made concrete:
    /// <list type="bullet">
    /// <item>Only the gameplay's most relevant action (<see cref="IInteractionHints.Primary"/>), only while it is
    /// ready, and only after it stayed available for its dwell time (no flicker when driving past).</item>
    /// <item>Only while the prompt still has something to teach (<see cref="PromptLedger"/>): each showing and each
    /// use counts, and a retired prompt never comes back.</item>
    /// <item>One at a time: a new prompt waits until the old one has fully faded out.</item>
    /// <item>A showing ends when the action is done, when it stops being available, or after a few seconds; the same
    /// prompt then rests for a cooldown before it may return.</item>
    /// </list>
    /// The view reports whether the chip is fully hidden and shows <see cref="Displayed"/> while
    /// <see cref="WantsShown"/>.
    /// </summary>
    internal sealed class PromptDirector
    {
        private readonly PromptSettings _settings;
        private readonly PromptLedger _ledger;
        private readonly float[] _cooldownUntil;
        private float _now;
        private InteractionKind _candidate;
        private float _candidateTime;
        private float _showTime;
        private bool _ending;

        public PromptDirector(PromptSettings settings, PromptLedger ledger)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
            int count = 0;
            foreach (InteractionKind kind in Enum.GetValues(typeof(InteractionKind)))
            {
                count = Math.Max(count, (int)kind + 1);
            }

            _cooldownUntil = new float[count];
        }

        /// <summary>The prompt in the chip (None when the chip is empty).</summary>
        public InteractionKind Displayed { get; private set; }

        /// <summary>The table entry of <see cref="Displayed"/>, or null.</summary>
        public PromptEntry Entry { get; private set; }

        /// <summary>True while the chip should be visible; false while it fades out (or stays hidden).</summary>
        public bool WantsShown => Displayed != InteractionKind.None && !_ending;

        /// <summary>World point the chip floats at (the hint's position lifted by the entry's height).</summary>
        public Vector3 WorldPoint { get; private set; }

        /// <summary>
        /// True on the step a new showing began: refresh the glyph and word, snap the screen position.
        /// </summary>
        public bool Started { get; private set; }

        /// <param name="deltaTime">Unscaled seconds since the last step.</param>
        /// <param name="gateOpen">
        /// False while anything else has the player's attention (pause, opening, cards, panel).
        /// </param>
        /// <param name="primary">The gameplay's most relevant action right now.</param>
        /// <param name="chipHidden">True once the chip is fully faded out.</param>
        public void Step(float deltaTime, bool gateOpen, InteractionHint primary, bool chipHidden)
        {
            _now += deltaTime;
            Started = false;
            InteractionKind desired = Desire(deltaTime, gateOpen, primary);

            if (Displayed != InteractionKind.None && !_ending)
            {
                _showTime += deltaTime;
                if (desired != Displayed || _showTime >= _settings.MaxShowSeconds)
                {
                    EndShowing();
                }
            }

            if (Displayed != InteractionKind.None && primary.Kind == Displayed)
            {
                WorldPoint = primary.Position + Vector3.up * Entry.LiftMetres;
            }

            if (Displayed != InteractionKind.None && !(_ending && chipHidden))
            {
                return;
            }

            Displayed = InteractionKind.None;
            Entry = null;
            _ending = false;
            if (desired == InteractionKind.None)
            {
                return;
            }

            Displayed = desired;
            Entry = _settings.Find(desired);
            WorldPoint = primary.Position + Vector3.up * Entry.LiftMetres;
            _showTime = 0f;
            Started = true;
            _ledger.RecordShown(desired);
        }

        /// <summary>The player did <paramref name="kind"/>: it counts as learned, and its prompt bows out.</summary>
        public void NotifyUsed(InteractionKind kind)
        {
            _ledger.RecordUsed(kind);
            if (kind == Displayed && !_ending)
            {
                EndShowing();
            }
        }

        private InteractionKind Desire(float deltaTime, bool gateOpen, InteractionHint primary)
        {
            InteractionKind kind = primary.Kind;
            bool available = gateOpen && kind != InteractionKind.None && primary.Ready &&
                             _settings.Find(kind) != null;
            if (!available)
            {
                _candidate = InteractionKind.None;
                _candidateTime = 0f;
                return InteractionKind.None;
            }

            if (kind == Displayed && !_ending)
            {
                return kind;
            }

            if (!_ledger.ShouldTeach(kind) || _now < _cooldownUntil[(int)kind])
            {
                _candidate = InteractionKind.None;
                _candidateTime = 0f;
                return InteractionKind.None;
            }

            if (kind != _candidate)
            {
                _candidate = kind;
                _candidateTime = 0f;
            }

            _candidateTime += deltaTime;
            return _candidateTime >= _settings.Find(kind).DwellSeconds ? kind : InteractionKind.None;
        }

        private void EndShowing()
        {
            _ending = true;
            _cooldownUntil[(int)Displayed] = _now + _settings.RepeatCooldown;
        }
    }
}
