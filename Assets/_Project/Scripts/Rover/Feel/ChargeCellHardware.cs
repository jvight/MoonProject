using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover
{
    /// <summary>
    /// Drives 07's Cells charge-up hardware (docs/design/0.5.5-owner-feedback.md §5.4) through
    /// <see cref="ChargeCells"/>:
    /// <list type="bullet">
    /// <item>the charge's stages (<see cref="CutChargeStaged"/>) fill the cells; the cut's first Act fires, its Cancel
    /// releases (a drain before firing) and its Impact ends the cut;</item>
    /// <item>each cell's glass sleeve slides along its local +Z from its rest seat as it fills and glows; the conduits
    /// and the lock ring light as the cells say, all in one energy colour toward white at the lock;</item>
    /// <item>the cutter's tier (<see cref="CutterTierChanged"/>) shows the Focus Lens from its tier on and brightens
    /// the lock; a real upgrade plays the bay's test cut and 07 is proud (Install Reward).</item>
    /// </list>
    /// Art's contract: RoverModel's <c>ConduitGlow</c> renderers (the deck pair and the neck), <c>StageGlow_n</c>
    /// sleeves under <c>Stage_n_L</c>/<c>_R</c> and <c>LockGlow</c> on the eye's lock ring, on the glow-off material
    /// (linear <c>_EmissionColor</c>). Zero allocations per frame.
    /// </summary>
    public sealed class ChargeCellHardware : IDisposable
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private readonly ChargeCellsSettings _settings;
        private readonly ChargeCells _cells;
        private readonly Renderer[] _conduits;
        private readonly Renderer[] _sleeves;
        private readonly Vector3[] _sleeveRests;
        private readonly Renderer _lockGlow;
        private readonly GameObject _lens;
        private readonly Renderer _lensGlass;
        private readonly Transform _emitter;
        private readonly Func<float> _chargeSeconds;
        private readonly EventBus _events;
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private readonly TestCut _testCut = new TestCut();
        private readonly IDisposable[] _subscriptions;
        private float _lockBoost = 1f;

        /// <param name="sleeves">The stage sleeves, two per stage (left, right), stage 1 first.</param>
        /// <param name="chargeSeconds">The cutter's fresh charge length now (ICutter), for the bay's test cut.</param>
        /// <param name="emitter">07's beam emitter: the test cut's beats happen there.</param>
        /// <param name="lens">The Focus Lens kit on the eye (from its tier on).</param>
        /// <param name="lensGlass">The lens's glass, glowing with the lock ring.</param>
        public ChargeCellHardware(EventBus events, ChargeCellsSettings settings, Renderer[] conduits,
            Renderer[] sleeves, Renderer lockGlow, Func<float> chargeSeconds, Transform emitter, GameObject lens,
            Renderer lensGlass)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _conduits = conduits ?? throw new ArgumentNullException(nameof(conduits));
            _sleeves = sleeves ?? throw new ArgumentNullException(nameof(sleeves));
            _lockGlow = lockGlow != null ? lockGlow : throw new ArgumentNullException(nameof(lockGlow));
            _chargeSeconds = chargeSeconds ?? throw new ArgumentNullException(nameof(chargeSeconds));
            _emitter = emitter;
            _lens = lens;
            _lensGlass = lensGlass;
            _cells = new ChargeCells(settings);
            _sleeveRests = new Vector3[sleeves.Length];
            for (int i = 0; i < sleeves.Length; i++)
            {
                _sleeveRests[i] = sleeves[i].transform.localPosition;
            }

            ShowTier(0);
            _subscriptions = new[]
            {
                events.Subscribe<CutChargeStaged>(OnStaged),
                events.Subscribe<FeelBeat>(OnBeat),
                events.Subscribe<CutterTierChanged>(OnTierChanged),
            };
        }

        public ChargeCells Cells => _cells;

        public int Tier { get; private set; }

        public void Tick(float deltaTime)
        {
            StepTestCut(deltaTime);
            _cells.Step(deltaTime);
            Color colour = _cells.Colour.linear;
            for (int i = 0; i < _conduits.Length; i++)
            {
                Emit(_conduits[i], colour, _cells.ConduitGlow);
            }

            for (int i = 0; i < _sleeves.Length; i++)
            {
                int stage = i / 2 + 1;
                _sleeves[i].transform.localPosition =
                    _sleeveRests[i] + Vector3.forward * (_settings.SleeveTravel * _cells.Fill(stage));
                Emit(_sleeves[i], colour, _cells.StageGlow(stage));
            }

            Emit(_lockGlow, colour, _cells.RingGlow(_lockBoost));
            if (_lensGlass != null && _lens.activeSelf)
            {
                Emit(_lensGlass, colour, _cells.LockGlow * _lockBoost);
            }
        }

        /// <summary>07 was placed: dark at once.</summary>
        public void Reset()
        {
            _cells.Reset();
        }

        public void Dispose()
        {
            foreach (IDisposable subscription in _subscriptions)
            {
                subscription.Dispose();
            }
        }

        private void Emit(Renderer renderer, Color colour, float glow)
        {
            _block.SetVector(EmissionColorId, colour * glow);
            renderer.SetPropertyBlock(_block);
        }

        private void OnStaged(CutChargeStaged staged)
        {
            if (staged.Stage == 0)
            {
                _cells.Begin(staged.Warm, staged.StagesNeeded, staged.StageSeconds, _settings.BraceSeconds);
            }
            else
            {
                _cells.Stage(staged.Stage);
            }
        }

        private void OnBeat(FeelBeat beat)
        {
            if (beat.Kind != FeelKind.Cut)
            {
                return;
            }

            switch (beat.Phase)
            {
                case FeelPhase.Act:
                    _cells.Fire();
                    break;
                case FeelPhase.Cancel:
                    _cells.Release(_settings.WarmWindowSeconds);
                    break;
                case FeelPhase.Impact:
                    _cells.End(_settings.WarmWindowSeconds);
                    break;
            }
        }

        /// <summary>
        /// A real cutter upgrade (not a load) shows its hardware, and once the part has seated the bay's test cut
        /// charges one cell at the new speed, fires, cracks the coupon, and 07 is proud (Install Reward).
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
                    _cells.Begin(true, 1, _testCut.ChargeSeconds, 0f);
                    break;
                case TestCut.Beat.Ignite:
                    _cells.Stage(1);
                    _cells.Fire();
                    break;
                case TestCut.Beat.Crack:
                    _cells.End(_settings.WarmWindowSeconds);
                    Vector3 at = _emitter != null ? _emitter.position : Vector3.zero;
                    _events.Publish(new FeelBeat(FeelKind.Install, FeelPhase.Reward, at, Vector3.up, 1f, 0f));
                    break;
            }
        }

        private void ShowTier(int tier)
        {
            Tier = Mathf.Max(0, tier);
            _lockBoost = 1f + Tier * _settings.TierLockBoost;
            if (_lens != null)
            {
                _lens.SetActive(Tier >= _settings.LensFromTier);
            }
        }
    }
}
