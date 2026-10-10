using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Save;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Kenji's pen plotter (docs/features/M3-16 P7; ruling 14: no hands pin anything). When 07 comes near it with a
    /// blueprint tube riding on its back and the bay is awake, the tube is delivered
    /// (<see cref="Blueprints.Deliver"/>, pinning the kit) and the plotter draws the sheet: the pen carriage sweeps
    /// the bed row by row while the ready lamp glows and the sheet lies on the bed; then the drawn sheet hangs on
    /// Kenji's pegboard (Blueprint_&lt;Ability&gt;). A delivery is saved at once; a loaded game simply shows every
    /// pinned sheet. Nothing allocates
    /// per frame.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BlueprintPlotter : MonoBehaviour
    {
        private const string SheetPrefix = "Blueprint_";

        [Tooltip("The plotter's root (RoverBay/Plotter): 07 delivers within reach of it.")]
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
        private Blueprints _blueprints;
        private Workshop _bay;
        private IRoverState _rover;
        private ISaveService _save;
        private ProgressionTuning _tuning;
        private EmissionGlow _lampGlow;
        private Vector3 _carriageRest;
        private Vector3 _headRest;
        private string _drawingKit;
        private float _drawStart;
        private float _lampLevel;
        private bool _initialized;

        /// <summary>True while the plotter draws a delivered sheet.</summary>
        public bool Drawing => _drawingKit != null;

        /// <summary>How far through the current drawing (0..1; 0 when idle).</summary>
        public float DrawProgress { get; private set; }

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

            _rover = services.Rover;
            _save = services.Save;
            _lampGlow = new EmissionGlow(_lamp);
            _carriageRest = _carriage.localPosition;
            _headRest = _head.localPosition;
            _sheet.SetActive(false);
            _lampGlow.Apply(0f);
            SyncSheets();
            _initialized = true;
            return true;
        }

        /// <summary>Shows exactly the pinned kits' sheets on the pegboard (boot, load).</summary>
        internal void SyncSheets()
        {
            foreach (KeyValuePair<string, GameObject> sheet in _sheets)
            {
                sheet.Value.SetActive(_blueprints.IsPinned(sheet.Key) && sheet.Key != _drawingKit);
            }
        }

        private void Update()
        {
            if (!_initialized)
            {
                return;
            }

            float now = Time.time;
            if (_drawingKit == null && _blueprints.IsCarrying && _bay.Awake &&
                SurfaceRules.HorizontalDistance(_rover.Position, _plotter.position) <= _tuning.PlotterReach)
            {
                _drawingKit = _blueprints.Deliver();
                _save.SaveNow();
                _drawStart = now;
                _sheet.SetActive(true);
                SyncSheets();
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
            _drawingKit = null;
            DrawProgress = 0f;
            SyncSheets();
        }
    }
}
