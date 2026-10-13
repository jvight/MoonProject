using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The charge-up's Cells hardware as plain logic (docs/design/0.5.5-owner-feedback.md §5.2, §5.4):
    /// <list type="bullet">
    /// <item><see cref="Begin"/>: after the brace (none when warm) stage 1's cell fills, easing out over the stage
    /// time: fast at first, slowing into its lock. Each <see cref="Stage"/> lock is authoritative: that cell is full
    /// and the next one starts.</item>
    /// <item>The full lock brings in the lock ring and pushes every lit part toward white, held through the firing.
    /// </item>
    /// <item><see cref="Release"/> before firing drains the cells back toward the battery, the last stage first;
    /// after firing (or at the break, <see cref="End"/>) the cells fade and the conduits keep a faint warm trace
    /// through the warm window.</item>
    /// </list>
    /// The charge level runs 0..needed stages: cell n is full once it passes n. Zero allocations.
    /// </summary>
    public sealed class ChargeCells
    {
        /// <summary>The most stages a piece needs (large and hull plates).</summary>
        public const int MaxStages = 3;

        private enum Phase
        {
            Idle,
            Building,
            Locked,
            Draining,
            Fading,
        }

        private readonly ChargeCellsSettings _settings;
        private Phase _phase;
        private int _needed = 1;
        private int _locked;
        private float _level;
        private float _delay;
        private float _built;
        private float _stageSeconds;
        private float _drainRate;
        private float _heat;
        private float _fade = 1f;
        private float _warmLeft;
        private bool _fired;

        public ChargeCells(ChargeCellsSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>The stages this charge needs (1..3).</summary>
        public int StagesNeeded => _needed;

        /// <summary>0..1 toward the full lock's glow and whitening.</summary>
        public float Heat => _heat;

        /// <summary>Whether anything still shows: a charge, a drain or fade, or the warm trace.</summary>
        public bool IsShowing => _phase != Phase.Idle || _warmLeft > 0f;

        /// <summary>The colour of every lit part now: the energy colour toward white by the lock's heat.</summary>
        public Color Colour => Color.Lerp(_settings.Colour, Color.white, _heat * _settings.LockWhite);

        /// <summary>How full stage <paramref name="stage"/>'s cell is (1..3), 0..1: its sleeve's share of travel.
        /// </summary>
        public float Fill(int stage)
        {
            return stage < 1 || stage > _needed ? 0f : Mathf.Clamp01(_level - (stage - 1)) * _fade;
        }

        /// <summary>Stage <paramref name="stage"/>'s cell glow.</summary>
        public float StageGlow(int stage)
        {
            return Fill(stage) * Lit;
        }

        /// <summary>The conduits' glow: lit from stage 1, or the warm trace.</summary>
        public float ConduitGlow => Mathf.Max(Mathf.Clamp01(_level) * Lit, WarmTrace);

        /// <summary>The lock ring's glow around the lens: on at the full lock only.</summary>
        public float LockGlow => _heat * _settings.LockGlow * _fade;

        /// <param name="warm">A warm emitter: no brace, and the warm trace is already lit.</param>
        /// <param name="stagesNeeded">The stages the piece needs before the beam fires.</param>
        /// <param name="stageSeconds">Seconds each stage takes to build.</param>
        /// <param name="braceSeconds">Seconds the brace takes before stage 1 builds (ignored when warm).</param>
        public void Begin(bool warm, int stagesNeeded, float stageSeconds, float braceSeconds)
        {
            _phase = Phase.Building;
            _needed = Mathf.Clamp(stagesNeeded, 1, MaxStages);
            _locked = 0;
            _level = 0f;
            _built = 0f;
            _delay = warm ? 0f : Mathf.Max(0f, braceSeconds);
            _stageSeconds = Mathf.Max(stageSeconds, 1e-3f);
            _heat = 0f;
            _fade = 1f;
            _fired = false;
        }

        /// <summary>Stage <paramref name="stage"/> locked: its cell is full; the last is the full lock.</summary>
        public void Stage(int stage)
        {
            if (_phase != Phase.Building || stage <= _locked)
            {
                return;
            }

            _locked = Mathf.Min(stage, _needed);
            _level = _locked;
            _built = 0f;
            _delay = 0f;
            if (_locked >= _needed)
            {
                _phase = Phase.Locked;
            }
        }

        /// <summary>The beam ignited: the next release ends the cut rather than draining it.</summary>
        public void Fire()
        {
            if (_phase == Phase.Building || _phase == Phase.Locked)
            {
                _fired = true;
            }
        }

        /// <summary>The player let go: before firing the cells drain back; after it the cut ends.</summary>
        public void Release(float warmSeconds)
        {
            if (_fired)
            {
                End(warmSeconds);
                return;
            }

            if (_phase != Phase.Building && _phase != Phase.Locked)
            {
                return;
            }

            _phase = Phase.Draining;
            _drainRate = Mathf.Max(_level, 1e-3f) / _settings.DrainSeconds;
        }

        /// <summary>The cut is over (the break): the cells fade and the warm trace holds for
        /// <paramref name="warmSeconds"/>.</summary>
        public void End(float warmSeconds)
        {
            if (_phase == Phase.Idle)
            {
                return;
            }

            _phase = Phase.Fading;
            _fired = false;
            _warmLeft = Mathf.Max(0f, warmSeconds);
        }

        /// <summary>Dark at once, the warm trace too (07 was placed).</summary>
        public void Reset()
        {
            Clear();
            _warmLeft = 0f;
        }

        public void Step(float deltaTime)
        {
            float step = Mathf.Max(deltaTime, 0f);
            _warmLeft = Mathf.Max(0f, _warmLeft - step);
            switch (_phase)
            {
                case Phase.Building:
                    Build(step);
                    break;
                case Phase.Locked:
                    _heat = Mathf.MoveTowards(_heat, 1f, step / _settings.LockSeconds);
                    break;
                case Phase.Draining:
                    _heat = Mathf.MoveTowards(_heat, 0f, step / _settings.LockSeconds);
                    _level -= _drainRate * step;
                    if (_level <= 0f)
                    {
                        Clear();
                    }

                    break;
                case Phase.Fading:
                    _fade -= step / _settings.FadeSeconds;
                    if (_fade <= 0f)
                    {
                        Clear();
                    }

                    break;
            }
        }

        /// <summary>The charge is gone; a warm trace from the last firing keeps its time.</summary>
        private void Clear()
        {
            _phase = Phase.Idle;
            _level = 0f;
            _heat = 0f;
            _fade = 1f;
            _fired = false;
        }

        private float Lit => Mathf.Lerp(_settings.StageGlow, _settings.LockGlow, _heat) * _fade;

        private float WarmTrace => _settings.WarmGlow * Mathf.Clamp01(_warmLeft / _settings.FadeSeconds);

        private void Build(float step)
        {
            if (_delay > 0f)
            {
                _delay -= step;
                return;
            }

            _built += step;
            float t = Mathf.Clamp01(_built / _stageSeconds);
            _level = _locked + (1f - (1f - t) * (1f - t));
        }
    }
}
