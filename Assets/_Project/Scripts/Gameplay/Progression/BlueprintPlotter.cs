using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Kenji's pen plotter on the bay's back wall (docs/features/M3-16 P7, M3-19 D7 and §5; ruling 14: no hands pin
    /// anything). When the lab's reader decodes a tube, its hand-off pulse runs down to the bay
    /// (<see cref="DecodeHandOff"/>); as it arrives the plotter draws the sheet: the pen carriage sweeps the bed row by
    /// row while the ready lamp glows and the sheet lies on the bed, then the drawn sheet hangs on Kenji's pegboard
    /// (Blueprint_&lt;Ability&gt;), announced with <see cref="BlueprintDrawing"/>. A bay still asleep keeps the waiting
    /// sheets and draws them one by one the moment the tower's second level wakes it. Each drawing is saved; a loaded
    /// game shows every drawn sheet and draws the ones still waiting. Nothing allocates per frame.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BlueprintPlotter : MonoBehaviour
    {
        private const string SheetPrefix = "Blueprint_";

        [Tooltip("The plotter's root (RoverBay/Plotter).")]
        [SerializeField] private Transform _plotter;

        [Tooltip("Plotter/PenCarriage: rides along its local X across the bed.")]
        [SerializeField] private Transform _carriage;

        [Tooltip("PenCarriage/PenHead: moves along its local Y down the bed.")]
        [SerializeField] private Transform _head;

        [Tooltip("Plotter/PlotterSheet: the sheet on the bed while it is drawn (hidden otherwise).")]
        [SerializeField] private GameObject _sheet;

        [Tooltip("Plotter/PlotterLamp: the ready lamp's glow renderer.")]
        [SerializeField] private Renderer _lamp;

        [Tooltip("RoverBay/Blueprints: Kenji's pegboard with one Blueprint_<Ability> sheet per kit.")]
        [SerializeField] private Transform _board;

        [Tooltip("Metres the carriage rides across the bed (Art's PlotterDrawWidth).")]
        [Min(0.01f)] [SerializeField] private float _drawWidth = 0.5f;

        [Tooltip("Metres the pen head moves down the bed (Art's PlotterDrawHeight).")]
        [Min(0.01f)] [SerializeField] private float _drawHeight = 0.35f;

        private readonly Dictionary<string, GameObject> _sheets =
            new Dictionary<string, GameObject>(StringComparer.Ordinal);
        private readonly Dictionary<string, float> _arrivals = new Dictionary<string, float>(StringComparer.Ordinal);
        private Blueprints _blueprints;
        private Workshop _bay;
        private ISaveService _save;
        private EventBus _events;
        private ProgressionTuning _tuning;
        private EmissionGlow _lampGlow;
        private IDisposable _handOffs;
        private Vector3 _carriageRest;
        private Vector3 _headRest;
        private string _drawingKit;
        private float _drawStart;
        private float _lampLevel;
        private bool _initialized;

        /// <summary>True while the plotter draws a decoded sheet.</summary>
        public bool Drawing => _drawingKit != null;

        /// <summary>The kit whose sheet is on the bed, or null.</summary>
        public string DrawingKit => _drawingKit;

        /// <summary>How far through the current drawing (0..1; 0 when idle).</summary>
        public float DrawProgress { get; private set; }

        /// <summary>Where <paramref name="kitId"/>'s sheet hangs, while the pegboard shows it.</summary>
        public bool TryGetSheet(string kitId, out Vector3 position)
        {
            bool shown = _sheets.TryGetValue(kitId, out GameObject sheet) && sheet.activeSelf;
            position = shown ? sheet.transform.position : Vector3.zero;
            return shown;
        }

        /// <summary>True when the pegboard shows <paramref name="kitId"/>'s sheet.</summary>
        public bool ShowsSheet(string kitId)
        {
            return _sheets.TryGetValue(kitId, out GameObject sheet) && sheet.activeSelf;
        }

        internal void Wire(Transform plotter, Transform carriage, Transform head, GameObject sheet, Renderer lamp,
            Transform board, float drawWidth, float drawHeight)
        {
            _plotter = plotter;
            _carriage = carriage;
            _head = head;
            _sheet = sheet;
            _lamp = lamp;
            _board = board;
            _drawWidth = drawWidth;
            _drawHeight = drawHeight;
        }

        internal bool Initialize(GameplayServices services, Blueprints blueprints, Workshop bay,
            ProgressionTuning tuning, UpgradeService upgrades)
        {
            string problem = _plotter == null || _carriage == null || _head == null || _sheet == null ||
                             _lamp == null || _board == null
                ? "a plotter node (Plotter, PenCarriage, PenHead, PlotterSheet, PlotterLamp) or the pegboard is " +
                  "not assigned."
                : null;
            _blueprints = blueprints ?? throw new ArgumentNullException(nameof(blueprints));
            _bay = bay != null ? bay : throw new ArgumentNullException(nameof(bay));
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            foreach (string kit in blueprints.Kits)
            {
                if (problem != null)
                {
                    break;
                }

                UpgradeDefinition definition = upgrades.Find(kit);
                Transform sheet = definition != null && definition.Levels[0].GrantsAbility
                    ? _board.Find(SheetPrefix + definition.Levels[0].Ability)
                    : null;
                if (sheet == null)
                {
                    problem = $"the pegboard has no {SheetPrefix}<ability> sheet for kit '{kit}'.";
                    break;
                }

                _sheets.Add(kit, sheet.gameObject);
            }

            if (problem != null)
            {
                Debug.LogError($"{nameof(BlueprintPlotter)}: {problem}", this);
                enabled = false;
                return false;
            }

            _save = services.Save;
            _events = services.Events;
            _lampGlow = new EmissionGlow(_lamp);
            _carriageRest = _carriage.localPosition;
            _headRest = _head.localPosition;
            _sheet.SetActive(false);
            _lampGlow.Apply(0f);
            _handOffs = services.Events.Subscribe<DecodeHandOff>(OnHandOff);
            SyncSheets();
            _initialized = true;
            return true;
        }

        /// <summary>Shows exactly the drawn kits' sheets on the pegboard (boot, load).</summary>
        internal void SyncSheets()
        {
            foreach (KeyValuePair<string, GameObject> sheet in _sheets)
            {
                sheet.Value.SetActive(_blueprints.IsDrawn(sheet.Key) && sheet.Key != _drawingKit);
            }
        }

        /// <summary>
        /// The pulse for <paramref name="handOff"/>'s kit reaches the bay this many seconds from now.
        /// </summary>
        private void OnHandOff(DecodeHandOff handOff)
        {
            _arrivals[handOff.KitId] = Time.time + handOff.Seconds;
        }

        private void Update()
        {
            if (!_initialized)
            {
                return;
            }

            float now = Time.time;
            if (_drawingKit == null && _bay.Awake)
            {
                StartNext(now);
            }

            float lamp = _drawingKit != null ? _tuning.PlotterLampGlow : 0f;
            _lampLevel = Damp.Toward(_lampLevel, lamp, _tuning.PlotterLampEase, Time.deltaTime);
            _lampGlow.Apply(_lampLevel);
            if (_drawingKit == null)
            {
                return;
            }

            DrawProgress = Mathf.Clamp01((now - _drawStart) / _tuning.PlotterDrawSeconds);
            int rows = _tuning.PlotterRows;
            float sweep = DrawProgress * rows;
            int row = Mathf.Min(rows - 1, (int)sweep);
            float along = sweep - row;
            float x = row % 2 == 0 ? along : 1f - along;
            float y = rows > 1 ? row / (float)(rows - 1) : 0f;
            _carriage.localPosition = _carriageRest + Vector3.right * (x * _drawWidth);
            _head.localPosition = _headRest + Vector3.up * (y * _drawHeight);
            if (DrawProgress < 1f)
            {
                return;
            }

            _carriage.localPosition = _carriageRest;
            _head.localPosition = _headRest;
            _sheet.SetActive(false);
            _blueprints.MarkDrawn(_drawingKit);
            _drawingKit = null;
            DrawProgress = 0f;
            SyncSheets();
            _save.SaveNow();
        }

        /// <summary>
        /// Starts on a decoded sheet not drawn yet whose pulse has arrived (one decoded before a load, or before the
        /// bay woke, has long arrived).
        /// </summary>
        private void StartNext(float now)
        {
            string kit = _blueprints.NextToDraw();
            if (kit == null || (_arrivals.TryGetValue(kit, out float arrives) && now < arrives))
            {
                return;
            }

            _arrivals.Remove(kit);
            _drawingKit = kit;
            _drawStart = now;
            _sheet.SetActive(true);
            SyncSheets();
            _events.Publish(new BlueprintDrawing(kit, _plotter.position, _tuning.PlotterDrawSeconds));
        }

        private void OnDestroy()
        {
            _handOffs?.Dispose();
        }
    }
}
