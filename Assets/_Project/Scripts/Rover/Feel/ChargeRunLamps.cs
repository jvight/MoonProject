using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover
{
    /// <summary>
    /// Drives the charge run's lamps on 07's back (docs/features/M3-26 §2) from the cut's and the dig's feel beats:
    /// Anticipate runs them over its Seconds from its Strength (0 fresh, >0 a resume), the first Act ignites them,
    /// each later Act pulses them, Cancel drains them and Impact fades them. Art's socket contract: RoverModel nodes
    /// <c>ChargeLamp_0</c> (at the battery) to <c>ChargeLamp_N-1</c> (the emitter ring), each with a renderer whose
    /// material has an emission colour. Zero allocations per frame.
    /// </summary>
    public sealed class ChargeRunLamps : IDisposable
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private readonly Renderer[] _lamps;
        private readonly ChargeRun _run;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private readonly IDisposable _beats;
        private bool _ignited;

        /// <summary>The RoverModel node name of lamp <paramref name="index"/> (0 at the battery).</summary>
        public static string SocketName(int index)
        {
            return "ChargeLamp_" + index;
        }

        public ChargeRunLamps(EventBus events, ChargeRunSettings settings, Renderer[] lamps)
        {
            if (lamps == null || lamps.Length == 0)
            {
                throw new ArgumentException("The charge run needs at least one lamp renderer.", nameof(lamps));
            }

            _lamps = lamps;
            _run = new ChargeRun(settings, lamps.Length);
            _beats = events.Subscribe<FeelBeat>(OnBeat);
        }

        public ChargeRun Run => _run;

        public void Tick(float deltaTime)
        {
            _run.Step(deltaTime);
            for (int i = 0; i < _lamps.Length; i++)
            {
                float glow = _run.Glow(i);
                _block.SetColor(EmissionColorId, new Color(glow, glow, glow, 1f));
                _lamps[i].SetPropertyBlock(_block);
            }
        }

        public void Dispose()
        {
            _beats.Dispose();
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
