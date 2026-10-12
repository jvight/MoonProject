using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The feel kit's look, as one listener of FeelBeats: the cut, and from polish wave 1 the leap's and landing's dust
    /// rings with the coil flash, dust falling off a tethered object, and an install's alignment light and power-on
    /// sweep (docs/design/polish-pass.md §3.2). For the cut's FeelBeats (docs/design/feel-pass.md §3.1, §6): Anticipate
    /// charges
    /// up at the emitter; the first Act ignites (flash, beam, cut light, sparks) and each later Act moves the hot seam
    /// and the spark rate with progress; Cancel disperses a charge or retracts the beam, leaving the seam to cool on
    /// the piece; Impact cracks; Follow drops slag off the tumbling piece, then lets its seam go as it folds. Beats
    /// say when, <see cref="IFeelAnchors"/> says where. Its owner calls <see cref="Step"/> once a frame. Every piece is
    /// pooled: steady state allocates nothing.
    /// </summary>
    public sealed class FeelVfx : IDisposable
    {
        private const int FlashSlots = 4;
        private const int SeamSlots = 4;

        // A flash shrinks by this share of its size as it fades.
        private const float FlashShrink = 0.3f;

        private readonly FeelVfxTuning _tuning;
        private readonly IFeelAnchors _anchors;
        private readonly IDisposable _subscription;
        private readonly ChargeUp _charge;
        private readonly CutBeam _beam;
        private readonly VacuumDebris _debris;
        private readonly BundleTieOff _tieOff;
        private readonly LightPulse _flashLight;
        private readonly LightPulse _cutLight;
        private readonly LightPulse _installLight;
        private readonly FeelSprites _flashSprites;
        private readonly HeatSeam[] _seams = new HeatSeam[SeamSlots];
        private readonly Vector3[] _flashAt = new Vector3[FlashSlots];
        private readonly float[] _flashAge = new float[FlashSlots];
        private readonly float[] _flashLife = new float[FlashSlots];
        private readonly float[] _flashSize = new float[FlashSlots];
        private HeatSeam _seam;
        private bool _ignited;
        private bool _cutting;
        private float _progress;
        private Vector3 _sweepFrom;
        private Vector3 _sweepTo;
        private float _sweepAge = float.PositiveInfinity;
        private float _sweepSeconds;

        public FeelVfx(Transform parent, FeelVfxTuning tuning, GameplayVisuals visuals, EventBus events,
            IFeelAnchors anchors)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _anchors = anchors ?? throw new ArgumentNullException(nameof(anchors));
            if (visuals == null)
            {
                throw new ArgumentNullException(nameof(visuals));
            }

            if (events == null)
            {
                throw new ArgumentNullException(nameof(events));
            }

            var root = new GameObject("FeelVfx").transform;
            root.SetParent(parent, false);
            _charge = new ChargeUp(root, tuning, visuals.FeelSprite);
            _beam = new CutBeam(root, tuning, visuals.BeamCore, visuals.BeamSheath);
            _debris = new VacuumDebris(root, tuning, visuals.FeelSprite);
            _tieOff = new BundleTieOff(tuning);
            _flashLight = new LightPulse("FlashLight", root, Color.white, tuning.FlashRange);
            _cutLight = new LightPulse("CutLight", root, tuning.CutLightColor, tuning.CutRange);
            _installLight = new LightPulse("InstallLight", root, tuning.MoteColor, tuning.EmitterRange);
            _flashSprites = new FeelSprites("Flashes", root, visuals.FeelSprite, FlashSlots, false, 0f);
            for (int i = 0; i < SeamSlots; i++)
            {
                _seams[i] = new HeatSeam(root, tuning, visuals.HeatSeam);
            }

            _subscription = events.Subscribe<FeelBeat>(Handle);
        }

        public ChargeUp Charge => _charge;
        public CutBeam Beam => _beam;
        public VacuumDebris Debris => _debris;
        public LightPulse CutLight => _cutLight;
        public LightPulse FlashLight => _flashLight;

        /// <summary>The seam on the piece under the beam, or null.</summary>
        public HeatSeam Seam => _seam;

        /// <summary>
        /// The bundle a piece folded into ties off: it pops (gameplay calls this when the fold ends).
        /// </summary>
        public void TieOff(Transform bundle)
        {
            if (bundle == null)
            {
                throw new ArgumentNullException(nameof(bundle));
            }

            _tieOff.Play(bundle);
        }

        public void Handle(FeelBeat beat)
        {
            switch (beat.Kind)
            {
                case FeelKind.Cut:
                    HandleCut(beat);
                    break;
                case FeelKind.Leap when beat.Phase == FeelPhase.Act:
                    Leap(beat);
                    break;
                case FeelKind.Land when beat.Phase == FeelPhase.Impact:
                    _debris.Ring(beat.Position, Mathf.RoundToInt(_tuning.RingDust * beat.Strength), beat.Strength);
                    break;
                case FeelKind.Tether when beat.Phase == FeelPhase.Follow:
                    _debris.Fall(beat.Position, _tuning.TetherDust);
                    break;
                case FeelKind.Install when beat.Phase == FeelPhase.Anticipate:
                    _installLight.MoveTo(beat.Position);
                    _installLight.Ramp(_tuning.AlignLight, beat.Seconds, true);
                    break;
                case FeelKind.Install when beat.Phase == FeelPhase.Act:
                    Sweep(beat);
                    break;
            }
        }

        private void HandleCut(FeelBeat beat)
        {
            switch (beat.Phase)
            {
                case FeelPhase.Anticipate:
                    Anticipate(beat);
                    break;
                case FeelPhase.Act when !_ignited:
                    Ignite();
                    break;
                case FeelPhase.Act:
                    _progress = beat.Strength;
                    _seam?.Cut(_progress);
                    break;
                case FeelPhase.Cancel:
                    Cancel();
                    break;
                case FeelPhase.Impact:
                    Crack();
                    break;
                case FeelPhase.Follow:
                    Follow(beat.Strength);
                    break;
            }
        }

        public void Step(float deltaTime)
        {
            Vector3 emitter = _anchors.Emitter;
            Vector3 target = _seam != null && _seam.Piece != null ? _seam.HotPoint : _anchors.CutPoint;
            _charge.Step(deltaTime, emitter);
            _beam.Step(deltaTime, emitter, target);
            if (_cutting)
            {
                _debris.Spray(target, _anchors.CutNormal, _progress);
                _cutLight.MoveTo(target);
            }

            _debris.Step(deltaTime);
            _flashLight.Step(deltaTime);
            _cutLight.Step(deltaTime);
            StepSweep(deltaTime);
            for (int i = 0; i < SeamSlots; i++)
            {
                _seams[i].Step(deltaTime);
            }

            _tieOff.Step(deltaTime);
            DrawFlashes(deltaTime);
        }

        public void Dispose()
        {
            _subscription.Dispose();
        }

        private void Anticipate(FeelBeat beat)
        {
            _ignited = false;
            _cutting = false;
            _charge.Begin(beat.Seconds);
            Transform piece = _anchors.CutPiece;
            if (piece == null)
            {
                _seam = null;
                return;
            }

            _seam = SeamFor(piece);
        }

        private void Ignite()
        {
            _ignited = true;
            _cutting = true;
            Vector3 emitter = _anchors.Emitter;
            Vector3 cut = _anchors.CutPoint;
            _charge.Ignite();
            _beam.Extend();
            Flash(emitter, _tuning.FlashRadius * 2f, _tuning.FlashSeconds);
            Flash(cut, _tuning.FlashRadius * 2f, _tuning.FlashSeconds);
            _flashLight.MoveTo(cut);
            _flashLight.Ramp(_tuning.FlashLight, 0f, false);
            _flashLight.Ramp(0f, _tuning.FlashDecaySeconds, false);
            _cutLight.MoveTo(cut);
            _cutLight.Flicker(_tuning.CutFlicker, _tuning.CutFlickerRate);
            _cutLight.Ramp(_tuning.CutLight, _tuning.ExtendSeconds, false);
            _progress = 0f;
            _seam?.Cut(0f);
            _debris.Spray(cut, _anchors.CutNormal, 0f);
        }

        private void Cancel()
        {
            if (!_ignited)
            {
                _charge.Cancel();
                return;
            }

            EndBeam(_tuning.CutFadeSeconds);
        }

        private void Crack()
        {
            Vector3 end = _seam != null && _seam.Piece != null ? _seam.HotPoint : _anchors.CutPoint;
            Flash(end, _tuning.FlashRadius, _tuning.CrackFlashSeconds);
            _cutLight.Ramp(_cutLight.Level * _tuning.CrackSpike, 0f, false);
            Vector3 emitter = _anchors.Emitter;
            _debris.Vent(emitter, (emitter - end).normalized + Vector3.up, _tuning.VentSparks);
            EndBeam(_tuning.EmitterFadeSeconds);
        }

        /// <summary>The tumble (Strength 0) drops slag off the piece; the fold (Strength 1) lets its seam go.</summary>
        private void Follow(float strength)
        {
            if (_seam == null || _seam.Piece == null)
            {
                return;
            }

            if (strength < 1f)
            {
                Vector2Int range = _tuning.TumbleBeads;
                _debris.Drop(_seam.Piece.position, Vector3.zero, UnityEngine.Random.Range(range.x, range.y + 1));
                return;
            }

            _seam.Clear();
            _seam = null;
        }

        /// <summary>The leap's take-off: a ring of dust and a flash of the coils under 07.</summary>
        private void Leap(FeelBeat beat)
        {
            _debris.Ring(beat.Position, Mathf.RoundToInt(_tuning.RingDust * _tuning.LeapRingShare), beat.Strength);
            Flash(beat.Position, _tuning.CoilFlashSize, _tuning.FlashSeconds);
            _flashLight.MoveTo(beat.Position);
            _flashLight.Ramp(_tuning.CoilFlashLight, 0f, false);
            _flashLight.Ramp(0f, _tuning.FlashDecaySeconds, false);
        }

        /// <summary>
        /// The installed part powers on: a light sweeps along it (from its socket along the beat's normal).
        /// </summary>
        private void Sweep(FeelBeat beat)
        {
            _sweepFrom = beat.Position;
            _sweepTo = beat.Position + beat.Normal.normalized * _tuning.SweepLength;
            _sweepSeconds = beat.Seconds > 0f ? beat.Seconds : _tuning.SweepSeconds;
            _sweepAge = 0f;
            _installLight.Ramp(_tuning.AlignLight * _tuning.CrackSpike, 0f, false);
        }

        private void StepSweep(float deltaTime)
        {
            _installLight.Step(deltaTime);
            if (_sweepAge > _sweepSeconds)
            {
                return;
            }

            _sweepAge += deltaTime;
            float t = Mathf.Clamp01(_sweepAge / _sweepSeconds);
            Vector3 at = Vector3.Lerp(_sweepFrom, _sweepTo, t * t * (3f - 2f * t));
            _installLight.MoveTo(at);
            Flash(at, _tuning.FlashRadius, _tuning.FlashSeconds);
            if (t >= 1f)
            {
                _installLight.Ramp(0f, _tuning.CutFadeSeconds, false);
            }
        }

        private void EndBeam(float fadeSeconds)
        {
            _cutting = false;
            _ignited = false;
            _beam.Retract();
            _debris.StopSpray();
            _charge.Fade(fadeSeconds);
            _cutLight.Ramp(0f, _tuning.CutFadeSeconds, false);
        }

        private HeatSeam SeamFor(Transform piece)
        {
            HeatSeam free = null;
            HeatSeam coldest = _seams[0];
            for (int i = 0; i < SeamSlots; i++)
            {
                if (_seams[i].Piece == piece)
                {
                    return _seams[i];
                }

                free = free ?? (_seams[i].Piece == null ? _seams[i] : null);
                coldest = _seams[i].Idle > coldest.Idle ? _seams[i] : coldest;
            }

            HeatSeam seam = free ?? coldest;
            seam.Begin(piece, _anchors.CutLine, _anchors.CutFaceNormal);
            return seam;
        }

        private void Flash(Vector3 position, float size, float seconds)
        {
            int slot = 0;
            for (int i = 1; i < FlashSlots; i++)
            {
                slot = _flashAge[i] / Mathf.Max(_flashLife[i], 1e-3f) > _flashAge[slot] / Mathf.Max(_flashLife[slot],
                    1e-3f) ? i : slot;
            }

            _flashAt[slot] = position;
            _flashAge[slot] = 0f;
            _flashLife[slot] = seconds;
            _flashSize[slot] = size;
        }

        private void DrawFlashes(float deltaTime)
        {
            _flashSprites.Begin();
            for (int i = 0; i < FlashSlots; i++)
            {
                if (_flashAge[i] >= _flashLife[i])
                {
                    continue;
                }

                _flashAge[i] += deltaTime;
                float u = Mathf.Clamp01(_flashAge[i] / _flashLife[i]);
                var white = new Color(1f, 1f, 1f, 1f - u * u);
                _flashSprites.Add(_flashAt[i], _flashSize[i] * (1f - FlashShrink * u), white, Vector3.zero);
            }

            _flashSprites.End();
        }
    }
}
