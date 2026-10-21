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
        private readonly Vector3[] _cooldownPoint;
        private float _now;
        private InteractionKind _candidate;
        private float _candidateTime;
        private float _candidateAway;
        private float _shownAway;
        private float _showTime;
        private bool _ending;
        private bool _pending;

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
            _cooldownPoint = new Vector3[count];
        }

        /// <summary>The prompt in the chip (None when the chip is empty).</summary>
        public InteractionKind Displayed { get; private set; }

        /// <summary>The table entry of <see cref="Displayed"/>, or null.</summary>
        public PromptEntry Entry { get; private set; }

        /// <summary>True while the chip should be visible; false while it fades out (or stays hidden).</summary>
        public bool WantsShown => Displayed != InteractionKind.None && !_ending;

        /// <summary>World point the chip floats at (the hint's position lifted by the entry's height).</summary>
        public Vector3 WorldPoint { get; private set; }

        /// <summary>The key of what the showing prompt acts on (a part's name), or null.</summary>
        public string NounKey { get; private set; }

        /// <summary>
        /// True on the step a new showing began: refresh the glyph and word, snap the screen position.
        /// </summary>
        public bool Started { get; private set; }

        /// <summary>
        /// True while a prompt is up, or one has run its dwell and waits only for the guidance slot: the slot is
        /// claimed when the prompt can start at once, so the element before it never leaves the screen early.
        /// </summary>
        public bool ReadyToShow => WantsShown || _pending;

        /// <param name="deltaTime">Unscaled seconds since the last step.</param>
        /// <param name="gateOpen">
        /// False while anything else has the player's attention (pause, opening, cards, panel): the dwell runs only
        /// while it is open.
        /// </param>
        /// <param name="primary">The gameplay's most relevant action right now.</param>
        /// <param name="chipHidden">True once the chip is fully faded out.</param>
        /// <param name="slotGranted">The guidance slot lets the prompt show; without it a ready prompt waits.</param>
        public void Step(float deltaTime, bool gateOpen, InteractionHint primary, bool chipHidden, bool slotGranted)
        {
            _now += deltaTime;
            Started = false;
            _pending = false;
            InteractionKind desired = Desire(deltaTime, gateOpen, primary);

            if (Displayed != InteractionKind.None && !_ending)
            {
                _showTime += deltaTime;
                // Out of reach for a moment keeps the prompt; another target in reach replaces it at once.
                _shownAway = desired == Displayed && slotGranted ? 0f : _shownAway + deltaTime;
                bool otherTarget = primary.Kind != InteractionKind.None && primary.Kind != Displayed;
                // A first-use key's chip has no time limit: only its key's use (NotifyUsed) or the target leaving
                // ends it.
                bool timedOut = !Entry.PersistUntilUsed && _showTime >= _settings.MaxShowSeconds;
                if (otherTarget || _shownAway > _settings.DwellGraceSeconds || timedOut)
                {
                    EndShowing();
                }
            }

            if (Displayed != InteractionKind.None && primary.Kind == Displayed)
            {
                WorldPoint = primary.Position + Vector3.up * Entry.LiftMetres;
                NounKey = primary.NounKey;
            }

            if (Displayed != InteractionKind.None && !(_ending && chipHidden))
            {
                return;
            }

            Displayed = InteractionKind.None;
            Entry = null;
            _ending = false;
            _pending = desired != InteractionKind.None && !slotGranted;
            if (desired == InteractionKind.None || !slotGranted)
            {
                return;
            }

            Displayed = desired;
            Entry = _settings.Find(desired);
            WorldPoint = primary.Position + Vector3.up * Entry.LiftMetres;
            NounKey = primary.NounKey;
            _showTime = 0f;
            _shownAway = 0f;
            Started = true;
            _ledger.RecordShown(desired);
        }

        /// <summary>The prompt for <paramref name="kind"/> bows out because the verb has begun, not yet learned.</summary>
        public void Yield(InteractionKind kind)
        {
            if (kind == Displayed && !_ending)
            {
                EndShowing();
            }
        }

        /// <summary>
        /// The next target of a set <paramref name="kind"/> has just begun lies beside the last: its prompt does not
        /// rest at that spot, it teaches again at once.
        /// </summary>
        public void ClearCooldown(InteractionKind kind)
        {
            _cooldownUntil[(int)kind] = 0f;
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

        /// <summary>
        /// True while a prompt is up, or <paramref name="primary"/> is something to teach right now (its verb not yet
        /// learned, past its cooldown): the guidance slot's claim, before the dwell has run.
        /// </summary>
        public bool Wants(InteractionHint primary)
        {
            if (Displayed != InteractionKind.None && !_ending)
            {
                return true;
            }

            if (_candidate != InteractionKind.None && _candidateAway <= _settings.DwellGraceSeconds &&
                _ledger.ShouldTeach(_candidate))
            {
                return true;
            }

            InteractionKind kind = primary.Kind;
            return kind != InteractionKind.None && primary.Ready && _settings.Find(kind) != null &&
                   _ledger.ShouldTeach(kind) && !CoolingDown(kind, primary.Position);
        }

        /// <summary>
        /// True while <paramref name="kind"/>'s prompt rests after going away unused, at the spot it went away: a
        /// target farther than the repeat radius is a new one and teaches again at once.
        /// </summary>
        private bool CoolingDown(InteractionKind kind, Vector3 target)
        {
            if (_settings.Find(kind).PersistUntilUsed)
            {
                return false;
            }

            int index = (int)kind;
            return _now < _cooldownUntil[index] &&
                   (target - _cooldownPoint[index]).sqrMagnitude <= _settings.RepeatRadius * _settings.RepeatRadius;
        }

        private InteractionKind Desire(float deltaTime, bool gateOpen, InteractionHint primary)
        {
            InteractionKind kind = primary.Kind;
            bool available = gateOpen && kind != InteractionKind.None && primary.Ready &&
                             _settings.Find(kind) != null;
            if (!available)
            {
                return Away(deltaTime);
            }

            if (kind == Displayed && !_ending)
            {
                return kind;
            }

            if (!_ledger.ShouldTeach(kind) || CoolingDown(kind, primary.Position))
            {
                return Away(deltaTime);
            }

            if (kind != _candidate)
            {
                _candidate = kind;
                _candidateTime = 0f;
            }

            _candidateAway = 0f;
            _candidateTime += deltaTime;
            return _candidateTime >= _settings.Find(kind).DwellSeconds ? kind : InteractionKind.None;
        }

        /// <summary>
        /// Nothing to teach this frame: a target that wavers out of reach for a moment (07 nosing in, the aim
        /// passing over another part) keeps its dwell; only a longer absence starts it over.
        /// </summary>
        private InteractionKind Away(float deltaTime)
        {
            _candidateAway += deltaTime;
            if (_candidateAway > _settings.DwellGraceSeconds)
            {
                _candidate = InteractionKind.None;
                _candidateTime = 0f;
            }

            return InteractionKind.None;
        }

        private void EndShowing()
        {
            _ending = true;
            _cooldownUntil[(int)Displayed] = _now + _settings.RepeatCooldown;
            _cooldownPoint[(int)Displayed] = WorldPoint - Vector3.up * Entry.LiftMetres;
        }
    }
}
