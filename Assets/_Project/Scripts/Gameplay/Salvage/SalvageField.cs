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
    /// reach. Holding Excavate asks 07 to ease to a stop, then the beam charges up and cuts along the piece's cut line
    /// for two to four seconds (<see cref="SalvageCutStarted"/>; the look is the feel kit's <see cref="FeelVfx"/>,
    /// which reads where to draw from this field as its <see cref="IFeelAnchors"/>); letting go keeps the progress (ruling 4,
    /// <see cref="SalvageCutStopped"/>). Cut free, the piece comes away from the wreck, folds into a bundle of its
    /// material and spirals into 07: a flash, <see cref="MaterialSalvaged"/> with the site's climbing melody step, the
    /// stock grows and the game saves. Drag pieces hang on until the tether pulls them a few metres clear; then they
    /// lie loose and the beam can cut them. When every piece is gone the skeleton stays, picked clean for good.
    /// Materials come only from the wreck sites: nothing lies loose on open ground (docs/features/M3-24). A relic in a site's heart is dug by the excavation: whichever 07 looks at more directly, the heart or a piece,
    /// is what the hold works on. A drag piece lost off the drivable floor floats back beside its site (ruling 1).
    /// It is the <see cref="ISalvageStatus"/> the UI's hold ring and the audio's beam read. Everything is built at
    /// initialisation; the frame loop allocates nothing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SalvageField : MonoBehaviour, ISalvageStatus, IFeelAnchors
    {
        /// <summary>Half-angle (degrees) inside which a relic's heart competes with the pieces for the hold.</summary>
        private const float HeartCone = 90f;

        /// <summary>The Moon's surface gravity (m/s²): a freed piece tumbles under it (feel-pass R11).</summary>
        private const float LunarGravity = 1.62f;

        /// <summary>The Cut Follow strength that marks the beam sliding to a piece's next segment (M3-26 §6).</summary>
        private const float SegmentSlide = 0.5f;
        [Tooltip("Salvage tuning (Assets/_Project/Data/Tuning/Gameplay/SalvageTuning.asset).")]
        [SerializeField] private SalvageTuning _tuning;

        [Tooltip("The sites and bundles (Assets/_Project/Data/Content/SalvageCatalog.asset).")]
        [SerializeField] private SalvageCatalog _catalog;

        private readonly List<SalvageSite> _sites = new List<SalvageSite>();
        private readonly List<SalvageSite> _derelicts = new List<SalvageSite>();
        private readonly Dictionary<string, SalvageSite> _hosts =
            new Dictionary<string, SalvageSite>(StringComparer.Ordinal);

        /// <summary>Kestrel-3's scrap field (0.5.5 §2): its id everywhere (saves, events).</summary>
        public const string ScrapFieldId = "scrap.kestrel_trail";

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private readonly List<SalvagePiece> _pieces = new List<SalvagePiece>();
        private readonly List<SalvageDrag> _drags = new List<SalvageDrag>();
        private EventBus _events;
        private InputReader _input;
        private BeamWork _beamWork;
        private Cutter _cutter;
        private SalvageSite _firedAt;
        private float _firedTime = float.NegativeInfinity;
        private float _slideLeft;
        private IRoverState _rover;
        private IRoverRig _rig;
        private IViewCamera _view;
        private ISaveService _save;
        private MaterialStock _stock;
        private ExcavationSystem _excavation;
        private CargoBundles _held;
        private PhysicsMaterial _dragMaterial;
        private FeelVfx _feel;
        private GlowFlashPool _flashes;
        private PickupCadence _cadence;
        private SalvagePiece _cutting;
        private CutState _cut;
        private int _inFlight;
        private bool _holding;
        private bool _gazing;
        private bool _initialized;

        public SalvageTuning Tuning => _tuning;

        public SalvageCatalog Catalog => _catalog;

        /// <summary>Every wreck site, in catalog order (the derelicts are not among them).</summary>
        public IReadOnlyList<SalvageSite> Sites => _sites;

        /// <summary>
        /// The derelicts on the routes (0.5.5 §1.7), in catalog order, then Kestrel-3's scrap field: their pieces are
        /// cut like any site's, but they hold no relics or crew crate. Those standing alone answer the sonar
        /// (<see cref="StandsAlone"/>); one inside a wreck counts as its wreck's.
        /// </summary>
        public IReadOnlyList<SalvageSite> Derelicts => _derelicts;

        /// <summary>
        /// The wreck <paramref name="derelict"/> stands inside (the haul bot in the drill site), or null for one on its
        /// own anchor.
        /// </summary>
        public SalvageSite HostOf(SalvageSite derelict)
        {
            return derelict != null && _hosts.TryGetValue(derelict.Id, out SalvageSite wreck) ? wreck : null;
        }

        /// <summary>
        /// A derelict on its own anchor, not inside a wreck and not Kestrel-3's scrap field: a place of its own that
        /// listening answers and the crew's map draws (D2).
        /// </summary>
        public bool StandsAlone(SalvageSite derelict)
        {
            return derelict != null && HostOf(derelict) == null &&
                   !string.Equals(derelict.Id, ScrapFieldId, StringComparison.Ordinal);
        }

        /// <summary>Every piece of every site and derelict.</summary>
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

        public bool IsCutting => _cutting != null && _cut.Phase == CutPhase.Cutting;

        /// <summary>The emitter is charging up on the piece before ignition (feel-pass §3.1).</summary>
        public bool IsBeamCharging => _cutting != null && _cut.Phase == CutPhase.Charging;

        /// <summary>How far the charge-up has come, 0..1 (0 when not charging).</summary>
        public float BeamChargeProgress => IsBeamCharging ? _cut.ChargeProgress : 0f;

        public Vector3 HotPoint => Aimed != null ? Aimed.HotPoint : Vector3.zero;

        public Vector3 CutNormal => Aimed != null ? Aimed.CutNormal : Vector3.up;

        public int CutLineCount => Aimed != null ? Aimed.CutLine.Length : 0;

        public Vector3 CutLinePoint(int index)
        {
            return Aimed.CutLinePoint(index);
        }

        public float Progress => Aimed != null ? Aimed.Progress : 0f;

        public SalvageMaterial Material => Aimed != null ? Aimed.Material : default;

        /// <summary>The piece under the beam, else the one a hold would cut, else null.</summary>
        private SalvagePiece Aimed => _cutting ?? Candidate;

        /// <summary>Current cutting beam brightness (tests and debugging views).</summary>
        /// <summary>How far the cutting beam reaches toward the cut (0..1; tests and debugging views).</summary>
        public float BeamLevel => _feel != null ? _feel.Beam.Reach : 0f;

        /// <summary>The cut's vacuum sparks and slag (tests and debugging views).</summary>
        internal VacuumDebris Sparks => _feel.Debris;

        /// <summary>07's cutter beam emitter: the cut's charge-up and beam leave from it.</summary>
        public Vector3 Emitter => _rig.CutterOrigin.position;

        /// <summary>07's scan lens emitter: the scan beam and ribbon connect to it.</summary>
        public Vector3 ScanEmitter => _rig.ScanOrigin.position;

        /// <summary>The aimed piece's CutPoint, the space its cut line lives in (it tumbles with the piece).</summary>
        public Transform CutPiece => Aimed != null ? Aimed.CutPoint : null;

        public IReadOnlyList<Vector3> CutLine => Aimed != null ? Aimed.CutLine : Array.Empty<Vector3>();

        /// <summary>The cut face's normal in the CutPoint's space: its +Z leaves the cut face.</summary>
        public Vector3 CutFaceNormal => Vector3.forward;

        internal void Wire(SalvageTuning tuning, SalvageCatalog catalog)
        {
            _tuning = tuning;
            _catalog = catalog;
        }

        /// <summary>
        /// Stands every site on its anchor. False (logged) when wiring, an anchor or a prefab node from the site
        /// contract is missing.
        /// </summary>
        /// <param name="scrapField">The World's lane slots for Kestrel-3's cuttable hull fragments (0.5.5 §2).</param>
        internal bool Initialize(GameplayServices services, Cutter cutter, IScrapField scrapField,
            ScanTuning scanTuning = null)
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
            _beamWork = services.Beam;
            _rover = services.Rover;
            _rig = services.Rig;
            _view = services.View;
            _save = services.Save;
            _stock = services.Materials;
            _cutter = cutter ?? throw new ArgumentNullException(nameof(cutter));
            _cut = new CutState(_events, FeelKind.Cut, cutter.ChargeSeconds, cutter.ResumeChargeSeconds,
                _tuning.CutSteps);
            _dragMaterial = new PhysicsMaterial("SalvageDrag")
            {
                bounciness = _tuning.DragBounciness,
                dynamicFriction = _tuning.DragFriction,
                staticFriction = _tuning.DragFriction,
                bounceCombine = PhysicsMaterialCombine.Average,
                frictionCombine = PhysicsMaterialCombine.Average,
            };

            if (!Stand(_catalog.Sites, false, _sites, services) ||
                !Stand(_catalog.Derelicts, true, _derelicts, services) || !StandScrapField(services, scrapField))
            {
                return false;
            }

            _cadence = new PickupCadence(_tuning.MinArrivalInterval);
            _flashes = new GlowFlashPool(transform, services.Meshes.Sphere, services.Visuals.Flash,
                _tuning.FlashPoolSize, _tuning.FlashDuration, _tuning.FlashRadius, _tuning.FlashIntensity);
            _feel = new FeelVfx(transform, services.Visuals.FeelTuning, services.Visuals, _events, this, scanTuning);
            return true;
        }

        private bool Stand(IReadOnlyList<SalvageSiteEntry> entries, bool derelicts, List<SalvageSite> sites,
            GameplayServices services)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (!services.Anchors.TryGet(entries[i].AnchorId, out WorldAnchor anchor))
                {
                    Fail($"'{entries[i].Id}' stands on '{entries[i].AnchorId}', a world anchor the World does not " +
                         "publish (world anchors contract).");
                    return false;
                }

                SalvageSite wreck = null;
                if (!string.IsNullOrEmpty(entries[i].HostSocket) && !TryHost(entries[i], anchor, out anchor, out wreck))
                {
                    return false;
                }

                string problem = SalvageSiteBuilder.Build(i, entries[i], derelicts, anchor, transform,
                    services.Terrain, _tuning, _catalog, services.Visuals.RelicHalo, _dragMaterial, _pieces, _drags,
                    out SalvageSite site);
                if (problem != null)
                {
                    Fail($"'{entries[i].Id}': {problem} (salvage site contract, docs/features/M3-13).");
                    return false;
                }

                sites.Add(site);
                if (wreck != null)
                {
                    _hosts.Add(site.Id, wreck);
                }
            }

            return true;
        }

        /// <summary>
        /// Kestrel-3's scrap field (0.5.5 §2): on each of the World's lane slots Art's hull fragment of its variant,
        /// dusted with the field's tint, stands as a Metal piece to cut; the field is saved by its id like a site.
        /// </summary>
        private bool StandScrapField(GameplayServices services, IScrapField field)
        {
            if (field == null || !services.Anchors.TryGet(WorldAnchorIds.KestrelTrail, out WorldAnchor trail))
            {
                Fail(field == null
                    ? "the World serves no IScrapField for Kestrel-3's scrap field (world scrap field contract)."
                    : $"the World publishes no '{WorldAnchorIds.KestrelTrail}' anchor for the scrap field.");
                return false;
            }

            var root = new GameObject("Scrap_kestrel_trail").transform;
            root.SetParent(transform, false);
            root.position = trail.Position;
            var cutLines = new PieceCutLine[field.Count];
            var sizes = new PieceSizeEntry[field.Count];
            var dust = new MaterialPropertyBlock();
            dust.SetVector(BaseColorId, field.Dust);
            for (int i = 0; i < field.Count; i++)
            {
                ScrapPlateInfo slot = field.Slot(i);
                GameObject prefab = _catalog.ScrapFragment(slot.Variant);
                Vector3[] seam = _catalog.ScrapCutLine(slot.Variant);
                if (prefab == null || seam == null)
                {
                    Fail($"the scrap field's slot {i} wants hull fragment {slot.Variant}, which the catalog lacks " +
                         "(run Gameplay/Content).");
                    return false;
                }

                Transform piece = Instantiate(prefab, slot.Position, slot.Rotation, root).transform;
                piece.localScale *= slot.Scale;
                piece.name = $"Salvage_{i}_{SalvageMaterial.Metal}";
                foreach (Renderer renderer in piece.GetComponentsInChildren<Renderer>())
                {
                    renderer.SetPropertyBlock(dust);
                }

                cutLines[i] = new PieceCutLine(i, seam);
                sizes[i] = new PieceSizeEntry(i, _catalog.ScrapSize);
            }

            string problem = SalvageSiteBuilder.BuildScrap(_derelicts.Count,
                SalvageSiteEntry.Scrap(ScrapFieldId, WorldAnchorIds.KestrelTrail, cutLines, sizes), root, trail.Radius,
                transform, services.Terrain, _tuning, _catalog, services.Visuals.RelicHalo, _dragMaterial, _pieces,
                _drags, out SalvageSite site);
            if (problem != null)
            {
                Fail($"'{ScrapFieldId}': {problem}.");
                return false;
            }

            _derelicts.Add(site);
            return true;
        }

        /// <summary>
        /// Where a derelict inside a wreck stands: the wreck's node, with the wreck's footprint. False (logged) when
        /// the wreck or its node is missing.
        /// </summary>
        private bool TryHost(SalvageSiteEntry entry, WorldAnchor wreckAnchor, out WorldAnchor seat,
            out SalvageSite wreck)
        {
            wreck = Find(entry.AnchorId);
            Transform socket = wreck != null ? wreck.Root.Find(entry.HostSocket) : null;
            if (socket == null)
            {
                Fail($"'{entry.Id}' stands inside '{entry.AnchorId}' on its '{entry.HostSocket}' node, which " +
                     $"{(wreck == null ? "is no salvage site" : "it does not have")} (Art's site contract).");
                seat = default;
                return false;
            }

            seat = new WorldAnchor(entry.Id, socket.position, socket.forward, wreckAnchor.Radius);
            return true;
        }

        /// <summary>
        /// What did not fit is held where it was cut (0.5.5 §2): a piece cut with no room stays on its wreck. Connected
        /// once the cargo stands, before any save is applied.
        /// </summary>
        internal void ConnectHeld(CargoBundles held)
        {
            _held = held ?? throw new ArgumentNullException(nameof(held));
        }

        /// <summary>A held piece, with room for it now: it comes away and folds into 07 like any cut piece.</summary>
        internal void Release(SalvagePiece piece)
        {
            BeginBreak(piece);
        }

        /// <summary>
        /// Tilly unclips a held piece (<see cref="TillyFetch"/>): it folds into its bundle on the spot, which she
        /// carries. It stays held (and saved so) until she sets it down at home.
        /// </summary>
        internal Transform Unclip(SalvagePiece piece)
        {
            if (piece.WreckCollider != null)
            {
                piece.WreckCollider.enabled = false;
            }

            piece.Halo.Apply(0f);
            piece.Bundle.SetPositionAndRotation(piece.Body.TransformPoint(piece.LocalCentre), piece.Body.rotation);
            piece.Bundle.localScale = piece.BundleScale;
            piece.Bundle.gameObject.SetActive(true);
            piece.Body.gameObject.SetActive(false);
            return piece.Bundle;
        }

        /// <summary>Tilly set a held piece down at home: it is taken for good.</summary>
        internal void Fetched(SalvagePiece piece)
        {
            MarkTaken(piece);
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

        /// <summary>The derelict on anchor <paramref name="id"/> ("micro.06"), or null.</summary>
        public SalvageSite FindDerelict(string id)
        {
            for (int i = 0; i < _derelicts.Count; i++)
            {
                if (string.Equals(_derelicts[i].Id, id, StringComparison.Ordinal))
                {
                    return _derelicts[i];
                }
            }

            return null;
        }

        internal SalvageSaveData Capture()
        {
            var data = new SalvageSaveData { sites = new SalvageSiteSaveData[_sites.Count + _derelicts.Count] };
            var taken = new List<int>();
            var pieces = new List<SalvagePieceSaveData>();
            for (int s = 0; s < data.sites.Length; s++)
            {
                SalvageSite site = s < _sites.Count ? _sites[s] : _derelicts[s - _sites.Count];
                taken.Clear();
                pieces.Clear();
                for (int i = 0; i < site.Pieces.Count; i++)
                {
                    SalvagePiece piece = site.Pieces[i];
                    if (piece.State == SalvagePieceState.Taken)
                    {
                        taken.Add(piece.Number);
                    }
                    else if (piece.State == SalvagePieceState.Held)
                    {
                        pieces.Add(new SalvagePieceSaveData { number = piece.Number, progress = 1f, held = true });
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

            return data;
        }

        /// <summary>
        /// Applies saved salvage by site or derelict id and piece number (unknown ones are ignored).
        /// </summary>
        internal void Restore(SalvageSaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            foreach (SalvageSiteSaveData saved in data.sites)
            {
                SalvageSite site = saved != null ? Find(saved.id) ?? FindDerelict(saved.id) : null;
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
                    if (pieceData.held)
                    {
                        piece.State = SalvagePieceState.Held;
                        _held.HoldPiece(piece, false);
                    }
                    else if (pieceData.loose && piece.Drag != null)
                    {
                        piece.Drag.RestoreLoose(pieceData.position, pieceData.rotation);
                        piece.State = piece.Drag.IsClear ? SalvagePieceState.Loose : SalvagePieceState.Attached;
                    }
                }
            }

            for (int i = 0; i < _sites.Count; i++)
            {
                _sites[i].SnapPilot();
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
            _feel.Step(deltaTime);
            for (int i = 0; i < _drags.Count; i++)
            {
                _drags[i].StepReach(deltaTime);
            }

            Vector3 socket = _rig.CargoSocket.position;
            StepPieces(socket, now, deltaTime);
            _flashes.Tick(deltaTime);
            for (int i = 0; i < _sites.Count; i++)
            {
                _sites[i].StepPilot(deltaTime);
                _sites[i].StepShudder(deltaTime, _tuning.ShudderAmplitude, _tuning.ShudderFrequency);
            }

            for (int i = 0; i < _derelicts.Count; i++)
            {
                _derelicts[i].StepShudder(deltaTime, _tuning.ShudderAmplitude, _tuning.ShudderFrequency);
            }
        }

        private void UpdateCut(float now, float deltaTime)
        {
            Candidate = _excavation.Lifting != null ? null : FindCandidate();
            bool wantsBeam = _input.ExcavateHeld && Candidate != null && (_cutting != null || _beamWork.MayStart);
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
                // The battery draws only from ignition on: a charge-up costs nothing.
                bool draws = _cut.Draws;
                StepCut(_cutting, deltaTime);
                if (draws)
                {
                    _beamWork.Work(deltaTime * _cutter.DrainFactor);
                }
            }

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

        /// <summary>
        /// 07 holds the beam on a piece: the charge builds the stages its size needs (0.5.5 §5.1), from a warm emitter
        /// (no brace, quicker stages) when the last firing was at this site moments ago.
        /// </summary>
        private void StartCut(SalvagePiece piece)
        {
            _cutting = piece;
            _slideLeft = 0f;
            bool warm = piece.Site == _firedAt && Time.time - _firedTime <= _cutter.WarmWindowSeconds;
            _cut.Engage(piece.Progress, piece.Material, piece.HotPoint, piece.CutNormal,
                _cutter.Plan(_tuning.Size(piece.Size).Stages, warm));
        }

        /// <summary>The hold lets go: a charge-up cancels, a lit cut pauses with its progress kept.</summary>
        /// <param name="announce">False when the scene is being torn down: listeners may already be gone.</param>
        private void StopCut(bool announce)
        {
            SalvagePiece piece = _cutting;
            bool lit = _cut.Draws;
            _cutting = null;
            if (!announce)
            {
                return;
            }

            _cut.Release(piece.Progress, piece.HotPoint, piece.CutNormal);
            if (lit)
            {
                _events.Publish(new SalvageCutStopped(false));
            }
        }

        /// <summary>
        /// The beam's work on a piece: the charge, then each segment in turn at the cutter's speed. Between segments
        /// the beam stays lit and slides to the next one (a Cut Follow of 0.5, docs/features/M3-26 §4.1), with no new
        /// charge and no progress lost.
        /// </summary>
        private void StepCut(SalvagePiece piece, float deltaTime)
        {
            if (_slideLeft > 0f)
            {
                _slideLeft -= deltaTime;
                return;
            }

            float before = piece.Progress;
            float progress = before;
            bool broke = _cut.Step(deltaTime, piece.CutSeconds / _cutter.SpeedFactor, ref progress, piece.HotPoint,
                piece.CutNormal, out bool ignited);
            if (!broke && CutSegments.ReachedSegmentEnd(before, ref progress, piece.Segments))
            {
                _slideLeft = _tuning.SegmentSlideSeconds;
                _events.Publish(new FeelBeat(FeelKind.Cut, FeelPhase.Follow, piece.HotPoint, piece.CutNormal,
                    SegmentSlide, 0f, piece.Material));
            }

            piece.Progress = progress;
            if (ignited)
            {
                _firedAt = piece.Site;
                _firedTime = Time.time;
                _events.Publish(new SalvageCutStarted(piece.CutPosition, piece.Material));
            }

            if (!broke)
            {
                return;
            }

            _cutting = null;
            _events.Publish(new SalvageCutStopped(true));
            SetHold(false);
            if (_stock.Hold.FreeUnits(piece.Material) < piece.Units)
            {
                piece.State = SalvagePieceState.Held;
                _held.HoldPiece(piece, true);
                _save.SaveNow();
                return;
            }

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

            piece.Site.Shudder(_tuning.ShudderSeconds);
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

        /// <summary>
        /// After the crack the piece pops out of its cut face and tumbles slowly, rising and settling a little under
        /// the Moon's gravity; then it folds into a bundle of its material, which ties off with a small pop
        /// (feel-pass §3.1). Follow beats mark the tumble and the fold.
        /// </summary>
        private void StepBreak(SalvagePiece piece, Vector3 socket, float deltaTime)
        {
            float before = piece.MotionTime;
            piece.MotionTime += deltaTime;
            float t = piece.MotionTime;
            float tumbleStart = _tuning.CrackSeconds;
            float foldStart = tumbleStart + _tuning.TumbleSeconds;
            float tieStart = foldStart + _tuning.FoldSeconds;
            Vector3 normal = piece.BreakNormal;
            if (before < tumbleStart && t >= tumbleStart)
            {
                Follow(piece, 0f);
            }

            if (before < foldStart && t >= foldStart)
            {
                Follow(piece, 1f);
            }

            float tumble = Mathf.Clamp01((t - tumbleStart) / _tuning.TumbleSeconds);
            float airborne = tumble * _tuning.TumbleSeconds;
            float launch = Mathf.Sqrt(2f * LunarGravity * _tuning.TumbleRise);
            float rise = Mathf.Max(0f, launch * airborne - 0.5f * LunarGravity * airborne * airborne);
            Vector3 axis = Vector3.Cross(Vector3.up, normal);
            axis = axis.sqrMagnitude > 1e-6f ? axis.normalized : Vector3.right;
            Vector3 position = piece.MotionStart + normal * (_tuning.BreakPop * Ease.OutCubic(tumble)) +
                               Vector3.up * rise;
            Quaternion rotation = Quaternion.AngleAxis(_tuning.BreakTumble * tumble, axis) * piece.MotionStartRotation;
            float fold = Ease.InOutSine(Mathf.Clamp01((t - foldStart) / _tuning.FoldSeconds));
            float tie = 1f + (_tuning.TiePop - 1f) * Ease.Hump(Mathf.Clamp01((t - tieStart) / _tuning.TieSeconds));
            piece.Body.SetPositionAndRotation(position, rotation);
            piece.Body.localScale = piece.BodyScale * (1f - fold);
            piece.Bundle.SetPositionAndRotation(piece.Body.TransformPoint(piece.LocalCentre), rotation);
            piece.Bundle.localScale = piece.BundleScale * (fold * tie);
            if (t < piece.MotionDuration)
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

            // 07's hatch opens for a bundle it has room for (M3-25 T1: Seconds = the flight, so Rover opens it just
            // before); a full hold keeps it shut and the bundle bumps it (the Cargo Impact on arrival).
            if (_stock.Hold.FreeUnits(piece.Material) > 0)
            {
                _events.Publish(new FeelBeat(FeelKind.Collect, FeelPhase.Anticipate, piece.MotionStart, Vector3.up,
                    1f, piece.MotionDuration, piece.Material));
            }
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

        /// <summary>A Follow beat of the cut at the piece (the tumble, then the fold).</summary>
        private void Follow(SalvagePiece piece, float strength)
        {
            _events.Publish(new FeelBeat(FeelKind.Cut, FeelPhase.Follow, piece.Body.position, piece.BreakNormal,
                strength, 0f, piece.Material));
        }

        /// <summary>
        /// Salvage folds into 07's hold: a flash, the event, the Collect reward beat and the stock for what fits. A
        /// piece is cut only when the hold has room for all of it (else it is held on its wreck); should the hold fill
        /// while it flies (a crate opened meanwhile), what does not fit bumps the closed hatch and goes to home's
        /// overflow crates, never onto the ground. The game saves once nothing else flies.
        /// </summary>
        private void Arrive(SalvageMaterial material, int units, Vector3 socket, string siteId, int comboStep)
        {
            int fitted = Math.Min(units, _stock.Hold.FreeUnits(material));
            int left = units - fitted;
            if (fitted > 0)
            {
                _flashes.Spawn(socket);
                _events.Publish(new MaterialSalvaged(material, fitted, socket, siteId, comboStep));
                _events.Publish(new FeelBeat(FeelKind.Collect, FeelPhase.Reward, socket, Vector3.up, 1f, 0f,
                    material));
                _stock.Collect(material, fitted);
            }

            if (left > 0)
            {
                _events.Publish(new FeelBeat(FeelKind.Cargo, FeelPhase.Impact, socket, Vector3.up, 1f, 0f, material));
                _stock.Home.AddOverflow(material, left);
            }

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
        }

        private void OnDestroy()
        {
            _feel?.Dispose();
            if (_dragMaterial != null)
            {
                Object.Destroy(_dragMaterial);
            }
        }
    }
}
