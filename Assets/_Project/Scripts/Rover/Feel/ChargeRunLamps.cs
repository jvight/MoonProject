using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover
{
    /// <summary>
    /// Drives the charge run's lamps on 07's back (docs/features/M3-26 §2) from the cut's and the dig's feel beats:
    /// Anticipate runs them over its Seconds from its Strength (0 fresh, >0 a resume), the first Act ignites them,
    /// each later Act pulses them, Cancel drains them and Impact fades them. The cutter's tier
    /// (<see cref="CutterTierChanged"/>) picks the row shown and its colour; the tier's faster charge arrives as a
    /// shorter Anticipate. Art's socket contract: RoverModel nodes <c>ChargeRun_{tier}</c>, each holding
    /// <c>ChargeLamp_0</c> (at the battery) to <c>ChargeLamp_N-1</c> (the emitter ring), each with a renderer whose
    /// material has an emission colour. Zero allocations per frame.
    /// </summary>
    public sealed class ChargeRunLamps : IDisposable
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private readonly ChargeRunRow[] _rows;
        private readonly ChargeRunSettings _settings;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private readonly IDisposable _beats;
        private readonly IDisposable _tiers;
        private ChargeRunRow _row;
        private ChargeRun _run;
        private Color _colour;
        private bool _ignited;

        /// <summary>The RoverModel node name of tier <paramref name="tier"/>'s row.</summary>
        public static string RowName(int tier)
        {
            return "ChargeRun_" + tier;
        }

        /// <summary>The node name of lamp <paramref name="index"/> in a row (0 at the battery).</summary>
        public static string SocketName(int index)
        {
            return "ChargeLamp_" + index;
        }

        public ChargeRunLamps(EventBus events, ChargeRunSettings settings, ChargeRunRow[] rows)
        {
            if (rows == null || rows.Length == 0)
            {
                throw new ArgumentException("The charge run needs at least one row.", nameof(rows));
            }

            _rows = rows;
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _beats = events.Subscribe<FeelBeat>(OnBeat);
            _tiers = events.Subscribe<CutterTierChanged>(changed => ShowTier(changed.Tier));
            ShowTier(0);
        }

        public ChargeRun Run => _run;

        public int Tier { get; private set; }

        public void Tick(float deltaTime)
        {
            _run.Step(deltaTime);
            Renderer[] lamps = _row.Lamps;
            for (int i = 0; i < lamps.Length; i++)
            {
                _block.SetColor(EmissionColorId, _colour * _run.Glow(i));
                lamps[i].SetPropertyBlock(_block);
            }
        }

        public void Dispose()
        {
            _beats.Dispose();
            _tiers.Dispose();
        }

        /// <summary>Shows tier <paramref name="tier"/>'s row (the highest built one when art has fewer) in its colour.</summary>
        private void ShowTier(int tier)
        {
            Tier = Mathf.Clamp(tier, 0, _rows.Length - 1);
            for (int i = 0; i < _rows.Length; i++)
            {
                _rows[i].Root.SetActive(i == Tier);
            }

            _row = _rows[Tier];
            _run = new ChargeRun(_settings, _row.Lamps.Length);
            _colour = _settings.TierColour(Tier);
            _ignited = false;
        }

        private void OnBeat(FeelBeat beat)
        {
            if (beat.Kind != FeelKind.Cut && beat.Kind != FeelKind.Dig)
            {
                return;
            }

            switch (beat.Phase)
            {
                case FeelPhase.Anticipate:
                    _ignited = false;
                    _run.Begin(beat.Seconds, beat.Strength);
                    break;
                case FeelPhase.Act:
                    if (!_ignited)
                    {
                        _ignited = true;
                        _run.Ignite();
                    }
                    else
                    {
                        _run.Pulse();
                    }

                    break;
                case FeelPhase.Cancel:
                    _run.Drain();
                    break;
                case FeelPhase.Impact:
                    _run.Fade();
                    break;
            }
        }
    }
}
