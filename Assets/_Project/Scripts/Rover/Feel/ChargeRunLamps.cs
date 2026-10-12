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
    /// shorter Anticipate. Art's contract: each tier's lamps ride the parts they sit on, in RoverModel nodes named
    /// <c>ChargeRun_{tier}</c> under the body, the neck, the head and the eye, holding <c>ChargeLamp_{i}</c> glow
    /// renderers numbered from the battery (0) to the emitter rim. The Focus Lens shows from its tier on and its glass
    /// glows with the run's last lamp. Zero allocations per frame.
    /// </summary>
    public sealed class ChargeRunLamps : IDisposable
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private readonly ChargeRunRow[] _rows;
        private readonly ChargeRunSettings _settings;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private readonly IDisposable _beats;
        private readonly IDisposable _tiers;
        private readonly EventBus _events;
        private readonly Func<float> _chargeSeconds;
        private readonly Transform _emitter;
        private readonly GameObject _lens;
        private readonly Renderer _lensGlass;
        private readonly TestCut _testCut = new TestCut();
        private ChargeRunRow _row;
        private ChargeRunLights _run;
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

        /// <param name="chargeSeconds">The cutter's fresh charge length now (ICutter), for the bay's test cut.</param>
        /// <param name="emitter">07's beam emitter: the test cut's beats happen there.</param>
        /// <param name="lens">The Focus Lens kit on the eye (tier 1 on); <paramref name="lensGlass"/> glows with the run.</param>
        public ChargeRunLamps(EventBus events, ChargeRunSettings settings, ChargeRunRow[] rows,
            Func<float> chargeSeconds, Transform emitter, GameObject lens, Renderer lensGlass)
        {
            if (rows == null || rows.Length == 0)
            {
                throw new ArgumentException("The charge run needs at least one row.", nameof(rows));
            }

            _rows = rows;
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _events = events;
            _chargeSeconds = chargeSeconds ?? throw new ArgumentNullException(nameof(chargeSeconds));
            _emitter = emitter;
            _lens = lens;
            _lensGlass = lensGlass;
            _beats = events.Subscribe<FeelBeat>(OnBeat);
            _tiers = events.Subscribe<CutterTierChanged>(OnTierChanged);
            ShowTier(0);
        }

        public ChargeRunLights Run => _run;

        public int Tier { get; private set; }

        public void Tick(float deltaTime)
        {
            StepTestCut(deltaTime);
            _run.Step(deltaTime);
            Renderer[] lamps = _row.Lamps;
            for (int i = 0; i < lamps.Length; i++)
            {
                _block.SetColor(EmissionColorId, _colour * _run.Glow(i));
                lamps[i].SetPropertyBlock(_block);
            }

            if (_lensGlass != null && _lens.activeSelf)
            {
                _block.SetColor(EmissionColorId, _colour * _run.Glow(lamps.Length - 1));
                _lensGlass.SetPropertyBlock(_block);
            }
        }

        public void Dispose()
        {
            _beats.Dispose();
            _tiers.Dispose();
        }

        /// <summary>
        /// A real cutter upgrade (not a load): the new row shows, and once the part has seated the bay's test cut
        /// plays the first charge run at the new speed, ignites, cracks the coupon, and 07 is proud (Install Reward).
        /// </summary>
        private void OnTierChanged(CutterTierChanged changed)
        {
            bool upgrade = !changed.Restored && changed.Tier > Tier;
            ShowTier(changed.Tier);
            if (upgrade)
            {
                _testCut.Start(_settings.TestCutDelay, _chargeSeconds(), _settings.TestCutBurn);
            }
        }

        private void StepTestCut(float deltaTime)
        {
            switch (_testCut.Step(deltaTime))
            {
                case TestCut.Beat.Charge:
                    _ignited = false;
                    _run.Begin(_testCut.ChargeSeconds, 0f);
                    break;
                case TestCut.Beat.Ignite:
                    _ignited = true;
                    _run.Ignite();
                    break;
                case TestCut.Beat.Crack:
                    _run.Fade();
                    Vector3 at = _emitter != null ? _emitter.position : Vector3.zero;
                    _events.Publish(new FeelBeat(FeelKind.Install, FeelPhase.Reward, at, Vector3.up, 1f, 0f));
                    break;
            }
        }

        /// <summary>Shows tier <paramref name="tier"/>'s row (the highest built one when art has fewer) in its colour.</summary>
        private void ShowTier(int tier)
        {
            Tier = Mathf.Clamp(tier, 0, _rows.Length - 1);
            for (int i = 0; i < _rows.Length; i++)
            {
                _rows[i].Show(i == Tier);
            }

            _row = _rows[Tier];
            _run = new ChargeRunLights(_settings, _row.Lamps.Length);
            _colour = _settings.TierColour(Tier);
            if (_lens != null)
            {
                _lens.SetActive(Tier >= _settings.LensFromTier);
            }
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
