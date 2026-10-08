using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;
using MoonProject.Core.Save;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Salvage (docs/features/M3-13). Art's five wrecks stand on the World's <c>site.*</c> anchors
    /// (<see cref="SalvageSiteBuilder"/>), and a soft cone of aim (VISION ruling 2) picks the piece 07 looks at within
    /// reach. Holding Excavate asks 07 to ease to a stop, then a stitching beam cuts at the piece's cut point for two
    /// to four seconds with warm sparks (<see cref="SalvageCutStarted"/>); letting go keeps the progress (ruling 4,
    /// <see cref="SalvageCutStopped"/>). Cut free, the piece comes away from the wreck, folds into a bundle of its
    /// material and spirals into 07: a flash, <see cref="MaterialSalvaged"/> with the site's climbing melody step, the
    /// stock grows and the game saves. Drag pieces hang on until the tether pulls them a few metres clear; then they
    /// lie loose and the beam can cut them. When every piece is gone the skeleton stays, picked clean for good. Along
    /// Kestrel-3's debris trail, loose bits glint and fold into 07 as it drives through (<see cref="SalvageTrail"/>).
    /// A relic in a site's heart is dug by the excavation: whichever 07 looks at more directly, the heart or a piece,
    /// is what the hold works on. A drag piece lost off the drivable floor floats back beside its site (ruling 1).
    /// It is the <see cref="ISalvageStatus"/> the UI's hold ring and the audio's beam read. Everything is built at
    /// initialisation; the frame loop allocates nothing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SalvageField : MonoBehaviour, ISalvageStatus
    {
        /// <summary>Half-angle (degrees) inside which a relic's heart competes with the pieces for the hold.</summary>
        private const float HeartCone = 90f;

        [Tooltip("Salvage tuning (Assets/_Project/Data/Tuning/Gameplay/SalvageTuning.asset).")]
        [SerializeField] private SalvageTuning _tuning;

        [Tooltip("The sites, bundles and trail bits (Assets/_Project/Data/Content/SalvageCatalog.asset).")]
        [SerializeField] private SalvageCatalog _catalog;

        private readonly List<SalvageSite> _sites = new List<SalvageSite>();
        private readonly List<SalvagePiece> _pieces = new List<SalvagePiece>();
        private readonly List<SalvageDrag> _drags = new List<SalvageDrag>();
        private EventBus _events;
        private InputReader _input;
        private IRoverState _rover;
        private IRoverRig _rig;
        private IViewCamera _view;
        private ISaveService _save;
        private MaterialStock _stock;
        private ExcavationSystem _excavation;
        private PhysicsMaterial _dragMaterial;
        private SalvageTrail _trail;
        private RepairBeam _beam;
        private CutSparks _sparks;
        private GlowFlashPool _flashes;
        private PickupCadence _cadence;
        private SalvagePiece _cutting;
        private Vector3 _beamTarget;
        private Vector3 _beamNormal = Vector3.up;
        private int _inFlight;
        private bool _holding;
        private bool _gazing;
        private bool _glancing;
        private bool _initialized;

        public SalvageTuning Tuning => _tuning;

        public SalvageCatalog Catalog => _catalog;

        /// <summary>Every site, in catalog order.</summary>
        public IReadOnlyList<SalvageSite> Sites => _sites;

        /// <summary>Every piece of every site.</summary>
        public IReadOnlyList<SalvagePiece> Pieces => _pieces;

        /// <summary>The pieces that must be tethered clear first (what the tether can latch onto).</summary>
        internal IReadOnlyList<SalvageDrag> Drags => _drags;

        /// <summary>The piece a hold of Excavate would cut now (null when none): what the prompt points at.</summary>
        public SalvagePiece Candidate { get; private set; }

        /// <summary>The piece under the beam, or null.</summary>
        public SalvagePiece Cutting => _cutting;

        /// <summary>True while salvage owns the Excavate hold (a piece is picked or being cut).</summary>
        public bool ClaimsHold => Candidate != null || _cutting != null;

        public bool HasTarget => Aimed != null;

        public Vector3 CutPoint => Aimed != null ? Aimed.CutPosition : Vector3.zero;

        public bool IsCutting => _cutting != null;

        public float Progress => Aimed != null ? Aimed.Progress : 0f;

        public SalvageMaterial Material => Aimed != null ? Aimed.Material : default;

        /// <summary>The piece under the beam, else the one a hold would cut, else null.</summary>
        private SalvagePiece Aimed => _cutting ?? Candidate;

        /// <summary>Current cutting beam brightness (tests and debugging views).</summary>
        public float BeamLevel => _beam != null ? _beam.Level : 0f;

        internal CutSparks Sparks => _sparks;

        /// <summary>The trail bits' horizon glints (tests read how many were drawn).</summary>
        internal PickupGlints Glints => _trail.Glints;

        /// <summary>Loose bits along the Kestrel trail (taken or not).</summary>
        public int TrailCount => _trail != null ? _trail.Count : 0;

        internal void Wire(SalvageTuning tuning, SalvageCatalog catalog)
        {
            _tuning = tuning;
            _catalog = catalog;
        }

        /// <summary>
        /// Stands every site on its anchor and lays the trail bits. False (logged) when wiring, an anchor or a prefab
        /// node from the site contract is missing.
        /// </summary>
        internal bool Initialize(GameplayServices services)
        {
            string problem = _tuning == null ? "SalvageTuning is not assigned."
                : _catalog == null ? "SalvageCatalog is not assigned."
                : _catalog.Validate();
            if (problem != null)
            {
                Fail(problem);
                return false;
            }

            _events = services.Events;
            _input = services.Input;
            _rover = services.Rover;
            _rig = services.Rig;
            _view = services.View;
            _save = services.Save;
            _stock = services.Materials;
            _dragMaterial = new PhysicsMaterial("SalvageDrag")
            {
                bounciness = _tuning.DragBounciness,
                dynamicFriction = _tuning.DragFriction,
                staticFriction = _tuning.DragFriction,
                bounceCombine = PhysicsMaterialCombine.Average,
                frictionCombine = PhysicsMaterialCombine.Average,
            };

            IReadOnlyList<SalvageSiteEntry> entries = _catalog.Sites;
            for (int i = 0; i < entries.Count; i++)
            {
                if (!services.Anchors.TryGet(entries[i].AnchorId, out WorldAnchor anchor))
                {
                    Fail($"site '{entries[i].AnchorId}' stands on a world anchor the World does not publish " +
                         "(world anchors contract).");
                    return false;
                }

                problem = SalvageSiteBuilder.Build(i, entries[i], anchor, transform, services.Terrain, _tuning,
                    _catalog, services.Visuals.RelicHalo, _dragMaterial, _pieces, _drags, out SalvageSite site);
                if (problem != null)
                {
                    Fail($"site '{entries[i].AnchorId}': {problem} (salvage site contract, docs/features/M3-13).");
                    return false;
                }

                _sites.Add(site);
            }

            WorldAnchor trailAnchor = default;
            if (_catalog.Trail.Count > 0 && !services.Anchors.TryGet(WorldAnchorIds.KestrelTrail, out trailAnchor))
            {
                Fail($"the trail bits lie along world anchor '{WorldAnchorIds.KestrelTrail}', which the World does " +
                     "not publish (world anchors contract).");
                return false;
            }

            _trail = SalvageTrail.Lay(transform, _catalog.Trail, trailAnchor, services.Terrain, _tuning,
                services.Visuals.SalvageGlint, services.Glints, out problem);
            if (_trail == null)
            {
                Fail(problem + " (salvage site contract, docs/features/M3-13).");
                return false;
            }

            _cadence = new PickupCadence(_tuning.MinArrivalInterval);
            _flashes = new GlowFlashPool(transform, services.Meshes.Sphere, services.Visuals.Flash,
                _tuning.FlashPoolSize, _tuning.FlashDuration, _tuning.FlashRadius, _tuning.FlashIntensity);
            _beam = new RepairBeam("CuttingBeam", transform, services.Visuals.TetherBeam, _tuning.BeamSweepRate,
                _tuning.BeamSweep);
            _sparks = new CutSparks(transform, _tuning, services.Visuals.Spark);
            return true;
        }

        /// <summary>
        /// The excavation shares the Excavate hold. Connected once every gameplay part is initialised; salvage runs
        /// from then on.
        /// </summary>
        internal void Connect(ExcavationSystem excavation)
        {
            _excavation = excavation != null ? excavation : throw new ArgumentNullException(nameof(excavation));
            _initialized = true;
        }

        /// <summary>The site with anchor id <paramref name="id"/>, or null.</summary>
        public SalvageSite Find(string id)
        {
            for (int i = 0; i < _sites.Count; i++)
            {
                if (string.Equals(_sites[i].Id, id, StringComparison.Ordinal))
                {
                    return _sites[i];
                }
            }

            return null;
        }

        /// <summary>Trail bit <paramref name="index"/> still lies in the dust.</summary>
        public bool IsTrailBitWaiting(int index)
        {
            return _trail.IsWaiting(index);
        }

        /// <summary>Where trail bit <paramref name="index"/> rests.</summary>
        public Vector3 TrailBitPosition(int index)
        {
            return _trail.Position(index);
        }

        public SalvageMaterial TrailBitMaterial(int index)
        {
            return _trail.Material(index);
        }

        internal SalvageSaveData Capture()
        {
            var data = new SalvageSaveData { sites = new SalvageSiteSaveData[_sites.Count] };
            var taken = new List<int>();
            var pieces = new List<SalvagePieceSaveData>();
            for (int s = 0; s < _sites.Count; s++)
            {
                SalvageSite site = _sites[s];
                taken.Clear();
                pieces.Clear();
                for (int i = 0; i < site.Pieces.Count; i++)
                {
                    SalvagePiece piece = site.Pieces[i];
                    if (piece.State == SalvagePieceState.Taken)
                    {
                        taken.Add(piece.Number);
                    }
                    else if (piece.State == SalvagePieceState.Loose)
                    {
                        Transform body = piece.Drag.transform;
                        pieces.Add(new SalvagePieceSaveData
                        {
                            number = piece.Number,
                            progress = piece.Progress,
                            loose = true,
                            position = body.position,
                            rotation = body.rotation,
                        });
                    }
                    else if (piece.State == SalvagePieceState.Returning)
                    {
                        // Floating back beside its site: it is saved where it will rest.
                        pieces.Add(new SalvagePieceSaveData
                        {
                            number = piece.Number,
                            progress = piece.Progress,
                            loose = true,
                            position = piece.Drag.ReturnEnd,
                            rotation = piece.Drag.ReturnEndRotation,
                        });
                    }
                    else if (piece.State != SalvagePieceState.Attached)
                    {
                        // Breaking or flying, not yet in the stock: it comes back fully cut, a touch of the beam away.
                        pieces.Add(new SalvagePieceSaveData { number = piece.Number, progress = 1f });
                    }
                    else if (piece.Progress > 0f)
                    {
                        pieces.Add(new SalvagePieceSaveData { number = piece.Number, progress = piece.Progress });
                    }
                }

                data.sites[s] = new SalvageSiteSaveData
                {
                    id = site.Id,
                    discovered = site.Discovered,
                    taken = taken.ToArray(),
                    pieces = pieces.ToArray(),
                };
            }

            var trail = new List<int>();
            for (int i = 0; i < _trail.Count; i++)
            {
                if (_trail.IsTaken(i))
                {
                    trail.Add(i);
                }
            }

            data.trail = trail.ToArray();
            return data;
        }

        /// <summary>Applies saved salvage by site id and piece number (unknown ones are ignored).</summary>
        internal void Restore(SalvageSaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            foreach (SalvageSiteSaveData saved in data.sites)
            {
                SalvageSite site = saved != null ? Find(saved.id) : null;
                if (site == null)
                {
                    continue;
                }

                site.RestoreDiscovered(saved.discovered);
                foreach (int number in saved.taken)
                {
                    SalvagePiece piece = site.Find(number);
                    if (piece != null)
                    {
                        MarkTaken(piece);
                    }
                }

                foreach (SalvagePieceSaveData pieceData in saved.pieces)
                {
                    SalvagePiece piece = pieceData != null ? site.Find(pieceData.number) : null;
                    if (piece == null || piece.State == SalvagePieceState.Taken)
                    {
                        continue;
                    }

                    piece.Progress = Mathf.Clamp01(pieceData.progress);
                    if (pieceData.loose && piece.Drag != null)
                    {
                        piece.Drag.RestoreLoose(pieceData.position, pieceData.rotation);
                        piece.State = piece.Drag.IsClear ? SalvagePieceState.Loose : SalvagePieceState.Attached;
                    }
                }
            }

            foreach (int index in data.trail)
            {
                if (index >= 0 && index < _trail.Count)
                {
                    _trail.MarkTaken(index);
                }
            }
        }

        private void Update()
        {
            if (!_initialized)
            {
                return;
            }

            float now = Time.time;
            float deltaTime = Time.deltaTime;
            UpdateCut(now, deltaTime);
            for (int i = 0; i < _drags.Count; i++)
            {
                _drags[i].StepReach(deltaTime);
            }

            Vector3 socket = _rig.CargoSocket.position;
            StepPieces(socket, now, deltaTime);
            if (_trail.Step(now, deltaTime, _rover.Position, socket, _view.Camera.transform.position, _cadence,
                    out int arrived, out int launched))
            {
                Arrive(_trail.Material(arrived), _tuning.TrailYield, socket, WorldAnchorIds.KestrelTrail,
                    _trail.Combo.Register(now));
            }

            _inFlight += launched;
            UpdateGlance(_trail.Nearest);
            _flashes.Tick(deltaTime);
        }

        private void UpdateCut(float now, float deltaTime)
        {
            Candidate = _excavation.Lifting != null ? null : FindCandidate();
            bool wantsBeam = _input.ExcavateHeld && Candidate != null;
            SetHold(wantsBeam);
            float engageSpeed = _cutting != null ? _tuning.EngageSpeed * _tuning.EngageHysteresis : _tuning.EngageSpeed;
            bool engaged = wantsBeam && _rover.Speed <= engageSpeed;
            if (_cutting != null && (!engaged || Candidate != _cutting))
            {
                StopCut(true);
            }

            if (engaged && _cutting == null)
            {
                StartCut(Candidate);
            }

            if (_cutting != null)
            {
                StepCut(_cutting, deltaTime);
            }

            UpdateBeam(now, deltaTime);
            UpdateGaze(wantsBeam);
        }

        private SalvagePiece FindCandidate()
        {
            Vector3 rover = _rover.Position;
            float reach = _tuning.ReachRadius;
            if (_cutting != null && _cutting.IsCuttable &&
                SurfaceRules.HorizontalDistance(rover, _cutting.CutPosition) <= reach * _tuning.ReachHysteresis)
            {
                return _cutting;
            }

            Transform view = _view.Camera.transform;
            Vector3 eye = view.position;
            Vector3 forward = view.forward;

            // The heart of a site competes for the hold: whichever 07 looks at more directly is worked on.
            float best = float.MaxValue;
            Relic dig = _excavation.NearestLiftable();
            if (dig != null && TetherAim.TryScore(eye, forward, dig.Site.Position, HeartCone, float.MaxValue,
                    _tuning.DistanceWeight, out float digScore))
            {
                best = digScore;
            }

            SalvagePiece picked = null;
            float reachSq = reach * reach;
            for (int i = 0; i < _pieces.Count; i++)
            {
                SalvagePiece piece = _pieces[i];
                if (!piece.IsCuttable)
                {
                    continue;
                }

                Vector3 cut = piece.CutPosition;
                if (SurfaceRules.HorizontalDistanceSquared(rover, cut) > reachSq)
                {
                    continue;
                }

                float cone = piece == Candidate ? _tuning.StickyCone : _tuning.AimCone;
                if (TetherAim.TryScore(eye, forward, cut, cone, float.MaxValue, _tuning.DistanceWeight,
                        out float score) && score < best)
                {
                    best = score;
                    picked = piece;
                }
            }

            return picked;
        }

        private void StartCut(SalvagePiece piece)
        {
            _cutting = piece;
            _events.Publish(new SalvageCutStarted(piece.CutPosition, piece.Material));
        }

        /// <param name="announce">False when the scene is being torn down: listeners may already be gone.</param>
        private void StopCut(bool announce)
        {
            _cutting = null;
            if (announce)
            {
                _events.Publish(new SalvageCutStopped(false));
            }
        }

        private void StepCut(SalvagePiece piece, float deltaTime)
        {
            piece.Progress = Mathf.Min(1f, piece.Progress + deltaTime / Mathf.Max(0.01f, piece.CutSeconds));
            if (piece.Progress < 1f)
            {
                return;
            }

            _cutting = null;
            _events.Publish(new SalvageCutStopped(true));
            SetHold(false);
            BeginBreak(piece);
        }

        private void BeginBreak(SalvagePiece piece)
        {
            piece.BreakNormal = piece.CutNormal;
            if (piece.Drag != null)
            {
                piece.Drag.Freeze();
            }

            if (piece.WreckCollider != null)
            {
                piece.WreckCollider.enabled = false;
            }

            piece.State = SalvagePieceState.Breaking;
            piece.MotionStart = piece.Body.position;
            piece.MotionStartRotation = piece.Body.rotation;
            piece.MotionTime = 0f;
            piece.MotionDuration = _tuning.BreakDuration;
            piece.Bundle.localScale = Vector3.zero;
            piece.Bundle.gameObject.SetActive(true);
            _inFlight++;
        }

        private void StepPieces(Vector3 socket, float now, float deltaTime)
        {
            for (int i = 0; i < _pieces.Count; i++)
            {
                SalvagePiece piece = _pieces[i];
                switch (piece.State)
                {
                    case SalvagePieceState.Attached:
                    case SalvagePieceState.Loose:
                        float target = piece == _cutting ? _tuning.CutHalo
                            : piece == Candidate ? _tuning.HoverHalo
                            : 0f;
                        EaseHalo(piece, Mathf.Max(target, piece.TetherHighlight), deltaTime);
                        break;
                    case SalvagePieceState.Breaking:
                        EaseHalo(piece, 0f, deltaTime);
                        StepBreak(piece, socket, deltaTime);
                        break;
                    case SalvagePieceState.Flying:
                        StepFlight(piece, socket, now, deltaTime);
                        break;
                }
            }
        }

        private void EaseHalo(SalvagePiece piece, float target, float deltaTime)
        {
            if (piece.HaloLevel <= 0f && target <= 0f)
            {
                return;
            }

            piece.HaloLevel = Damp.Toward(piece.HaloLevel, target, _tuning.HaloEase, deltaTime);
            if (piece.HaloLevel < GlowRenderer.VisibleThreshold && target <= 0f)
            {
                piece.HaloLevel = 0f;
            }

            piece.Halo.Apply(piece.HaloLevel);
        }

        /// <summary>The piece eases out of its cut face, tumbling a little, and folds down into its bundle.</summary>
        private void StepBreak(SalvagePiece piece, Vector3 socket, float deltaTime)
        {
            piece.MotionTime += deltaTime;
            float progress = Mathf.Clamp01(piece.MotionTime / piece.MotionDuration);
            Vector3 normal = piece.BreakNormal;
            Vector3 axis = Vector3.Cross(Vector3.up, normal);
            axis = axis.sqrMagnitude > 1e-6f ? axis.normalized : Vector3.right;
            Vector3 position = piece.MotionStart + normal * (_tuning.BreakPop * Ease.OutCubic(progress));
            Quaternion rotation = Quaternion.AngleAxis(_tuning.BreakTumble * Ease.OutCubic(progress), axis) *
                                  piece.MotionStartRotation;
            float fold = Ease.InOutSine(progress);
            piece.Body.SetPositionAndRotation(position, rotation);
            piece.Body.localScale = piece.BodyScale * (1f - fold);
            piece.Bundle.SetPositionAndRotation(piece.Body.TransformPoint(piece.LocalCentre), rotation);
            piece.Bundle.localScale = piece.BundleScale * fold;
            if (progress < 1f)
            {
                return;
            }

            piece.Body.gameObject.SetActive(false);
            piece.State = SalvagePieceState.Flying;
            piece.MotionStart = piece.Bundle.position;
            piece.MotionStartRotation = piece.Bundle.rotation;
            piece.MotionTime = 0f;
            piece.MotionDuration = PickupFlight.Duration(Vector3.Distance(piece.MotionStart, socket),
                _tuning.FlightDuration, _tuning.FlightPerMetre);
        }

        private void StepFlight(SalvagePiece piece, Vector3 socket, float now, float deltaTime)
        {
            piece.MotionTime += deltaTime;
            float progress = piece.MotionTime / piece.MotionDuration;
            if (progress >= 1f && _cadence.TryClaim(now))
            {
                piece.State = SalvagePieceState.Taken;
                piece.Bundle.gameObject.SetActive(false);
                Arrive(piece.Material, piece.Units, socket, piece.Site.Id, piece.Site.Combo.Register(now));
                return;
            }

            float direction = (piece.Number & 1) == 0 ? 1f : -1f;
            piece.Bundle.SetPositionAndRotation(
                PickupFlight.Evaluate(piece.MotionStart, socket, progress, piece.Number, direction,
                    _tuning.FlightLift, _tuning.SpiralRadius, _tuning.SpiralTurns),
                Quaternion.Euler(0f, piece.MotionTime * _tuning.FlightSpin, 0f) * piece.MotionStartRotation);
            piece.Bundle.localScale = piece.BundleScale *
                                      Mathf.Lerp(1f, _tuning.ArrivalScale, Ease.InOutSine(Mathf.Clamp01(progress)));
        }

        /// <summary>Salvage folds into 07: a flash, the event, the stock, and a save once nothing else flies.</summary>
        private void Arrive(SalvageMaterial material, int units, Vector3 socket, string siteId, int comboStep)
        {
            _flashes.Spawn(socket);
            _events.Publish(new MaterialSalvaged(material, units, socket, siteId, comboStep));
            _stock.Add(material, units);
            _inFlight--;
            if (_inFlight == 0)
            {
                _save.SaveNow();
            }
        }

        private static void MarkTaken(SalvagePiece piece)
        {
            piece.State = SalvagePieceState.Taken;
            piece.Progress = 1f;
            piece.Body.gameObject.SetActive(false);
            piece.Bundle.gameObject.SetActive(false);
        }

        private void UpdateBeam(float now, float deltaTime)
        {
            SalvagePiece target = _cutting ?? Candidate;
            if (target != null)
            {
                _beamTarget = target.CutPosition;
                _beamNormal = target.CutNormal;
            }

            _beam.Step(_cutting != null, target != null || _beam.Level > 0f, _rig.TetherOrigin.position, _beamTarget,
                now, deltaTime);
            _sparks.Emit(_beamTarget, _beamNormal, _cutting != null ? _beam.Level : 0f);
        }

        private void UpdateGaze(bool wantsBeam)
        {
            SalvagePiece look = _cutting ?? Candidate;
            if (look != null)
            {
                _rig.SetGazeTarget(this, look.CutPosition,
                    wantsBeam || _cutting != null ? GazePriorities.Focus : GazePriorities.Interest);
                _gazing = true;
            }
            else if (_gazing)
            {
                _rig.ClearGazeTarget(this);
                _gazing = false;
            }
        }

        /// <summary>07 glances at the nearest loose trail bit (its own gaze request, under the trail).</summary>
        private void UpdateGlance(int nearest)
        {
            if (nearest >= 0)
            {
                _rig.SetGazeTarget(_trail, _trail.Position(nearest), GazePriorities.Glance);
                _glancing = true;
            }
            else if (_glancing)
            {
                _rig.ClearGazeTarget(_trail);
                _glancing = false;
            }
        }

        private void SetHold(bool hold)
        {
            if (hold == _holding)
            {
                return;
            }

            _holding = hold;
            _rig.SetHoldStill(this, hold);
        }

        private void Fail(string problem)
        {
            Debug.LogError($"{nameof(SalvageField)}: {problem}", this);
            enabled = false;
        }

        private void OnDisable()
        {
            if (!_initialized)
            {
                return;
            }

            if (_cutting != null)
            {
                StopCut(false);
            }

            SetHold(false);
            if (_gazing)
            {
                _rig.ClearGazeTarget(this);
                _gazing = false;
            }

            if (_glancing)
            {
                _rig.ClearGazeTarget(_trail);
                _glancing = false;
            }
        }

        private void OnDestroy()
        {
            _trail?.Dispose();
            if (_dragMaterial != null)
            {
                Object.Destroy(_dragMaterial);
            }
        }
    }
}
