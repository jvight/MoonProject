using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.World
{
    /// <summary>
    /// Lights the habitat lander's belly lift and the crew's lab (docs/features/M3-19): the belly lamp and hatch rim,
    /// the lab's wall and reading lamps and the big porthole by home's wake (<see cref="IHomeLights"/>); one lamp round
    /// the big porthole per known tech node and the tech board's sheets and lamps from <see cref="TechNodeChanged"/>;
    /// the recovery panel's rows from <see cref="RecoveryProgressChanged"/>; the light table and Kenji's chart pen from
    /// the map's look (<see cref="MapLookStarted"/>, <see cref="MapInkCued"/>, <see cref="MapLookEnded"/>); the reading
    /// lamps brighten per keepsake (<see cref="RelicDeposited"/>); the hand-off's lamp pulse runs down the conduit to
    /// the bay (<see cref="DecodeHandOff"/>). Thin: the timing lives in <see cref="LampBank"/>,
    /// <see cref="HandOffPulse"/>, <see cref="RecoveryRowLamps"/> and <see cref="ChartPenPlot"/>. Allocation-free per
    /// frame.
    /// </summary>
    public sealed class LabLights : MonoBehaviour, IGameSystem
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        // The recovery panel's rows in RecoveryTarget order; the Link's ends in the larger Peak lamp.
        private const int RowCount = 4;
        private const int EndLampRow = (int)RecoveryTarget.Link;

        [Tooltip("World tuning (Assets/_Project/Data/Tuning/WorldSettings.asset); its Lab Lighting block.")]
        [SerializeField] private WorldSettings _settings;

        [Tooltip("The lander: the chart's middle is home, where it stands.")]
        [SerializeField] private Transform _home;

        [Tooltip("BellyLift/BellyLamp: the belly's work lamp.")]
        [SerializeField] private Renderer _bellyLamp;

        [Tooltip("BellyLift/HatchRim: the floor hatch's rim lamps.")]
        [SerializeField] private Renderer _hatchRim;

        [Tooltip("Lab/CabinLamps: the lab's four wall lamps.")]
        [SerializeField] private Renderer _cabinLamps;

        [Tooltip("BigPorthole: the big porthole's glass over the survey chart.")]
        [SerializeField] private Renderer _bigPorthole;

        [Tooltip("Lab/Station_Chart/LightTable/LightTableGlow: the light table's lit panel.")]
        [SerializeField] private Renderer _tableGlow;

        [Tooltip("PortholeLamps/Lamp_0..n round the big porthole, in order.")]
        [SerializeField] private Renderer[] _portholeLamps = new Renderer[0];

        [Tooltip("Lab/Station_Board/Sheet_n, one per tech node in TechNodeId order.")]
        [SerializeField] private GameObject[] _boardSheets = new GameObject[0];

        [Tooltip("Lab/Station_Board/Lamp_n, one per tech node in TechNodeId order.")]
        [SerializeField] private Renderer[] _boardLamps = new Renderer[0];

        [Tooltip("RecoveryPanel/Row_Power/Lamp_n.")]
        [SerializeField] private Renderer[] _powerRow = new Renderer[0];

        [Tooltip("RecoveryPanel/Row_Link/Lamp_n; the last socket is the larger Peak lamp.")]
        [SerializeField] private Renderer[] _linkRow = new Renderer[0];

        [Tooltip("RecoveryPanel/Row_Dish/Lamp_n.")]
        [SerializeField] private Renderer[] _dishRow = new Renderer[0];

        [Tooltip("RecoveryPanel/Row_Answer/Lamp_n.")]
        [SerializeField] private Renderer[] _answerRow = new Renderer[0];

        [Tooltip("Lab/Station_Keepsakes/ReadingLamp_n, one per crew section.")]
        [SerializeField] private Renderer[] _readingLamps = new Renderer[0];

        [Tooltip("Lab/Station_Keepsakes/Slot_n: where keepsakes are placed.")]
        [SerializeField] private Transform[] _keepsakeSlots = new Transform[0];

        [Tooltip("The crew section (reading lamp index) of each keepsake slot, in slot order (Art's shelf layout).")]
        [SerializeField] private int[] _slotSections = new int[0];

        [Tooltip("LightTable/ChartPen/ChartPenCarriage: rides along its local X across the paper.")]
        [SerializeField] private Transform _penCarriage;

        [Tooltip("ChartPenCarriage/ChartPenHead: rides along its local Y up the paper.")]
        [SerializeField] private Transform _penHead;

        [Tooltip("How far the carriage travels across the paper (Art's BaseModelBuilder.ChartDrawWidth).")]
        [Min(0.01f)]
        [SerializeField] private float _drawWidth = 0.5f;

        [Tooltip("How far the pen head travels up the paper (Art's BaseModelBuilder.ChartDrawHeight).")]
        [Min(0.01f)]
        [SerializeField] private float _drawHeight = 0.34f;

        [Tooltip("HandOffCable/Glow_n: the conduit's beads from the reader to the bay, in order.")]
        [SerializeField] private Renderer[] _handOffBeads = new Renderer[0];

        [Tooltip("HandOffPath/Point_n: the pulse's polyline from the reader's slot to the bay's back wall.")]
        [SerializeField] private Transform[] _handOffPath = new Transform[0];

        [Tooltip("The small warm light that travels with the pulse (the scene build hangs it on the path).")]
        [SerializeField] private Light _handOffLight;

        private readonly TechNodeTally _tally = new TechNodeTally();
        private readonly HandOffPulse _pulse = new HandOffPulse();
        private MaterialPropertyBlock _block;
        private IHomeLights _lights;
        private IDisposable[] _subscriptions;
        private Renderer[][] _rows;
        private LampBank[] _rowBanks;
        private int[] _rowLamps;
        private float[] _rowPulseAt;
        private LampBank _porthole;
        private LampBank _board;
        private int[] _keepsakes;
        private float[] _pathAt;
        private float _pathLength;
        private Vector3 _carriageHome;
        private Vector3 _headHome;
        private Vector2 _penFrom;
        private Vector2 _penTo;
        private float _penStartedAt = float.NegativeInfinity;
        private bool _looking;
        private float _look;

        // The glow last pushed to each renderer, so unchanged lamps cost nothing.
        private float _bellyApplied = -1f;
        private float _rimApplied = -1f;
        private float _cabinApplied = -1f;
        private float _portholeApplied = -1f;
        private float _tableApplied = -1f;
        private float[] _portholeLampsApplied;
        private float[] _boardApplied;
        private float[][] _rowsApplied;
        private float[] _readingApplied;
        private float[] _beadsApplied;

        public void Initialize(GameContext context)
        {
            string problem = SetupProblem();
            if (problem == null && !context.TryGet(out _lights))
            {
                problem = "no IHomeLights is registered (World's HomeLightingSystem must initialise first).";
            }

            if (problem != null)
            {
                Debug.LogError($"{nameof(LabLights)}: {problem}", this);
                enabled = false;
                return;
            }

            // Unity refuses to create a property block while it constructs a MonoBehaviour (a field initializer).
            _block = new MaterialPropertyBlock();
            _rows = new[] { _powerRow, _linkRow, _dishRow, _answerRow };
            _rowBanks = new LampBank[RowCount];
            _rowsApplied = new float[RowCount][];
            for (int row = 0; row < RowCount; row++)
            {
                _rowBanks[row] = new LampBank(_rows[row].Length);
                _rowsApplied[row] = Unapplied(_rows[row].Length);
                SetRowShown(row, 0);
            }

            _rowLamps = new int[RowCount];
            _rowPulseAt = new float[RowCount];
            for (int row = 0; row < RowCount; row++)
            {
                _rowPulseAt[row] = float.NegativeInfinity;
            }

            _porthole = new LampBank(_portholeLamps.Length);
            _board = new LampBank(_boardLamps.Length);
            _portholeLampsApplied = Unapplied(_portholeLamps.Length);
            _boardApplied = Unapplied(_boardLamps.Length);
            _readingApplied = Unapplied(_readingLamps.Length);
            _beadsApplied = Unapplied(_handOffBeads.Length);
            _keepsakes = new int[_readingLamps.Length];
            MeasurePath();
            _carriageHome = _penCarriage.localPosition;
            _headHome = _penHead.localPosition;
            _handOffLight.enabled = false;
            _handOffLight.shadows = LightShadows.None;
            _handOffLight.color = _settings.LabLighting.HandOffLightColour;
            _handOffLight.range = _settings.LabLighting.HandOffLightRange;
            EventBus events = context.Events;
            _subscriptions = new[]
            {
                events.Subscribe<TechNodeChanged>(OnTechNodeChanged),
                events.Subscribe<RecoveryProgressChanged>(OnRecoveryProgressChanged),
                events.Subscribe<MapLookStarted>(OnMapLookStarted),
                events.Subscribe<MapLookEnded>(OnMapLookEnded),
                events.Subscribe<MapInkCued>(OnMapInkCued),
                events.Subscribe<DecodeHandOff>(OnDecodeHandOff),
                events.Subscribe<RelicDeposited>(OnRelicDeposited),
            };
        }

        private void Update()
        {
            if (_lights == null)
            {
                return;
            }

            LabLightingSettings tuning = _settings.LabLighting;
            float now = Time.time;
            float delta = Time.deltaTime;
            float lift = _lights.Level(HomeLight.LiftLamps);
            float lab = _lights.Level(HomeLight.LabLamps);
            float chart = _lights.Level(HomeLight.ChartTable);
            Glow(_bellyLamp, ref _bellyApplied, lift * tuning.BellyLampGlow);
            Glow(_hatchRim, ref _rimApplied, lift * tuning.HatchRimGlow);
            Glow(_cabinLamps, ref _cabinApplied, lab * tuning.CabinLampGlow);
            Glow(_bigPorthole, ref _portholeApplied,
                lab * tuning.PortholeGlow + chart * tuning.TableThroughPorthole);
            _look = Mathf.MoveTowards(_look, _looking ? 1f : 0f, delta / tuning.TableLookEase);
            Glow(_tableGlow, ref _tableApplied, chart * (tuning.TableGlow + _look * tuning.TableLookBoost));

            _porthole.Tick(delta, tuning.LampWarmUp);
            ApplyBank(_portholeLamps, _portholeLampsApplied, _porthole, lab * tuning.PortholeLampGlow);
            _board.Tick(delta, tuning.LampWarmUp);
            ApplyBank(_boardLamps, _boardApplied, _board, lab * tuning.BoardLampGlow);
            for (int row = 0; row < RowCount; row++)
            {
                ApplyRow(row, now, delta, lab, tuning);
            }

            for (int section = 0; section < _readingLamps.Length; section++)
            {
                float reading = tuning.ReadingLampBase + tuning.ReadingLampPerKeepsake * _keepsakes[section];
                Glow(_readingLamps[section], ref _readingApplied[section], lab * reading);
            }

            MovePen(now, tuning);
            ApplyHandOff(now, tuning);
        }

        private void OnTechNodeChanged(TechNodeChanged change)
        {
            _tally.Apply(change.Node, change.State);
            int known = _tally.KnownCount;
            for (int i = 0; i < _porthole.Count; i++)
            {
                _porthole.Set(i, i < known ? 1f : 0f, change.Restored);
            }

            int node = (int)change.Node;
            if (node < _boardSheets.Length)
            {
                _boardSheets[node].SetActive(change.State != TechNodeState.Unknown);
                _board.Set(node, change.State == TechNodeState.Built ? 1f : 0f, change.Restored);
            }
        }

        private void OnRecoveryProgressChanged(RecoveryProgressChanged change)
        {
            int row = (int)change.Target;
            Renderer[] sockets = _rows[row];
            if (change.Lamps > sockets.Length)
            {
                Debug.LogError($"{nameof(LabLights)}: {change.Target} has {change.Lamps} lamps but the recovery " +
                               $"panel's row has {sockets.Length} sockets (Art's lab contract).", this);
            }

            bool endLamp = row == EndLampRow;
            int lamps = Mathf.Min(change.Lamps, sockets.Length);
            _rowLamps[row] = lamps;
            SetRowShown(row, lamps);
            for (int socket = 0; socket < sockets.Length; socket++)
            {
                bool lit = RecoveryRowLamps.Lit(socket, change.LitLamps, lamps, sockets.Length, endLamp);
                _rowBanks[row].Set(socket, lit ? 1f : 0f, change.Restored);
            }

            if (!change.Restored && lamps > 0 && change.LitLamps >= lamps)
            {
                _rowPulseAt[row] = Time.time;
            }
        }

        private void OnMapLookStarted(MapLookStarted look)
        {
            _looking = true;
        }

        private void OnMapLookEnded(MapLookEnded end)
        {
            _looking = false;
            StartPen(Vector2.zero);
        }

        private void OnMapInkCued(MapInkCued ink)
        {
            StartPen(ChartPenPlot.OnPaper(ink.Position, _home.position, _settings.LabLighting.ChartRadius));
        }

        private void OnDecodeHandOff(DecodeHandOff handOff)
        {
            _pulse.Begin(Time.time, handOff.Seconds);
        }

        private void OnRelicDeposited(RelicDeposited deposit)
        {
            int nearest = 0;
            float best = float.PositiveInfinity;
            for (int i = 0; i < _keepsakeSlots.Length; i++)
            {
                float distance = (deposit.Position - _keepsakeSlots[i].position).sqrMagnitude;
                if (distance < best)
                {
                    best = distance;
                    nearest = i;
                }
            }

            _keepsakes[_slotSections[nearest]]++;
        }

        private void StartPen(Vector2 target)
        {
            _penFrom = PenNow(Time.time);
            _penTo = target;
            _penStartedAt = Time.time;
        }

        private Vector2 PenNow(float now)
        {
            float t = Mathf.Clamp01((now - _penStartedAt) / _settings.LabLighting.PenTravel);
            return Vector2.Lerp(_penFrom, _penTo, Mathf.SmoothStep(0f, 1f, t));
        }

        private void MovePen(float now, LabLightingSettings tuning)
        {
            if (now - _penStartedAt > tuning.PenTravel + Time.deltaTime)
            {
                return;
            }

            Vector2 pen = PenNow(now);
            _penCarriage.localPosition = _carriageHome + Vector3.right * (pen.x * _drawWidth);
            _penHead.localPosition = _headHome + Vector3.up * (pen.y * _drawHeight);
        }

        private void ApplyRow(int row, float now, float delta, float lab, LabLightingSettings tuning)
        {
            LampBank bank = _rowBanks[row];
            bank.Tick(delta, tuning.LampWarmUp);
            Renderer[] sockets = _rows[row];
            float since = (now - _rowPulseAt[row]) / tuning.RowPulseSeconds;
            for (int socket = 0; socket < sockets.Length; socket++)
            {
                float along = sockets.Length > 1 ? (float)socket / (sockets.Length - 1) : 0f;
                float wave = since >= 0f && since <= 2f ? Mathf.Exp(-Mathf.Pow((since - along) * 4f, 2f)) : 0f;
                float level = bank.Level(socket) * (1f + wave * tuning.RowPulseSwell);
                Glow(sockets[socket], ref _rowsApplied[row][socket], lab * level * tuning.RecoveryLampGlow);
            }
        }

        private void SetRowShown(int row, int lamps)
        {
            Renderer[] sockets = _rows[row];
            for (int socket = 0; socket < sockets.Length; socket++)
            {
                sockets[socket].enabled = RecoveryRowLamps.Shows(socket, lamps, sockets.Length, row == EndLampRow);
            }
        }

        private void ApplyHandOff(float now, LabLightingSettings tuning)
        {
            bool running = _pulse.IsRunning(now, tuning.HandOffAfterglow);
            for (int i = 0; i < _handOffBeads.Length; i++)
            {
                float level = running ? _pulse.BeadLevel(i, _handOffBeads.Length, now, tuning.HandOffWidth,
                    tuning.HandOffAfterglow) : 0f;
                Glow(_handOffBeads[i], ref _beadsApplied[i], level * tuning.HandOffBeadGlow);
            }

            _handOffLight.enabled = running;
            if (!running)
            {
                return;
            }

            _handOffLight.intensity = _pulse.Strength(now, tuning.HandOffAfterglow) * tuning.HandOffLightIntensity;
            _handOffLight.transform.position = PointAlong(Mathf.Clamp01(_pulse.Progress(now)) * _pathLength);
        }

        private void MeasurePath()
        {
            _pathAt = new float[_handOffPath.Length];
            for (int i = 1; i < _handOffPath.Length; i++)
            {
                _pathAt[i] = _pathAt[i - 1] + Vector3.Distance(_handOffPath[i - 1].position, _handOffPath[i].position);
            }

            _pathLength = _pathAt[_pathAt.Length - 1];
        }

        private Vector3 PointAlong(float distance)
        {
            for (int i = 1; i < _pathAt.Length; i++)
            {
                if (distance <= _pathAt[i])
                {
                    float span = Mathf.Max(_pathAt[i] - _pathAt[i - 1], 1e-4f);
                    return Vector3.Lerp(_handOffPath[i - 1].position, _handOffPath[i].position,
                        (distance - _pathAt[i - 1]) / span);
                }
            }

            return _handOffPath[_handOffPath.Length - 1].position;
        }

        private void ApplyBank(Renderer[] lamps, float[] applied, LampBank bank, float glow)
        {
            for (int i = 0; i < lamps.Length; i++)
            {
                Glow(lamps[i], ref applied[i], bank.Level(i) * glow);
            }
        }

        private void Glow(Renderer target, ref float applied, float level)
        {
            if (Mathf.Approximately(level, applied))
            {
                return;
            }

            applied = level;
            _block.Clear();
            _block.SetVector(EmissionColorId, new Vector4(level, level, level, 1f));
            target.SetPropertyBlock(_block);
        }

        private void OnDestroy()
        {
            if (_subscriptions == null)
            {
                return;
            }

            foreach (IDisposable subscription in _subscriptions)
            {
                subscription.Dispose();
            }
        }

        private static float[] Unapplied(int count)
        {
            var applied = new float[count];
            for (int i = 0; i < count; i++)
            {
                applied[i] = -1f;
            }

            return applied;
        }

        private string SetupProblem()
        {
            if (_settings == null || _home == null)
            {
                return "World Settings and the lander (home) must be assigned.";
            }

            if (_bellyLamp == null || _hatchRim == null || _cabinLamps == null || _bigPorthole == null ||
                _tableGlow == null || _penCarriage == null || _penHead == null || _handOffLight == null)
            {
                return "the belly lamp, hatch rim, cabin lamps, big porthole, light table, chart pen and hand-off " +
                       "light must be assigned (the scene build wires Art's lab nodes).";
            }

            if (HasEmpty(_portholeLamps) || HasEmpty(_boardLamps) || HasEmpty(_readingLamps) ||
                HasEmpty(_handOffBeads) || HasEmpty(_powerRow) || HasEmpty(_linkRow) || HasEmpty(_dishRow) ||
                HasEmpty(_answerRow))
            {
                return "every lamp array must be assigned and hold no empty slot (the scene build wires Art's lab).";
            }

            if (_boardSheets.Length != _boardLamps.Length || Array.IndexOf(_boardSheets, null) >= 0)
            {
                return "the board needs one sheet per node lamp.";
            }

            if (_handOffPath.Length < 2 || Array.IndexOf(_handOffPath, null) >= 0)
            {
                return "the hand-off path needs at least two points.";
            }

            if (_keepsakeSlots.Length == 0 || _keepsakeSlots.Length != _slotSections.Length ||
                Array.IndexOf(_keepsakeSlots, null) >= 0)
            {
                return "the keepsake slots and their sections must be assigned one for one.";
            }

            foreach (int section in _slotSections)
            {
                if (section < 0 || section >= _readingLamps.Length)
                {
                    return $"a keepsake slot names section {section}, but there are {_readingLamps.Length} " +
                           "reading lamps.";
                }
            }

            return null;
        }

        private static bool HasEmpty(Renderer[] renderers)
        {
            return renderers == null || renderers.Length == 0 || Array.IndexOf(renderers, null) >= 0;
        }
    }
}
