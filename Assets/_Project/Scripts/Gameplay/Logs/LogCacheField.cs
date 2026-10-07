using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The crew's log caches (Ro's battered tin box at the canyon terminus, M3-05): each stands at its world anchor,
    /// lid ajar toward where 07 arrives, a warm glint inside that reads from afar, and 07 glances at it on the way.
    /// Coming within reach opens it once and for good: a warm puff of light, <see cref="CrewLogFound"/> (the UI shows
    /// the log card), then a save. The frame loop allocates nothing.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LogCacheField : MonoBehaviour
    {
        /// <summary>Caches stand far apart, so one puff of light is always free.</summary>
        private const int FlashPoolSize = 1;

        [Tooltip("The caches in the world (Assets/_Project/Data/Content/LogCacheCatalog.asset).")]
        [SerializeField] private LogCacheCatalog _catalog;

        [Tooltip("Cache tuning (Assets/_Project/Data/Tuning/Gameplay/LogCacheTuning.asset).")]
        [SerializeField] private LogCacheTuning _tuning;

        private EventBus _events;
        private IRoverState _rover;
        private IRoverRig _rig;
        private IViewCamera _view;
        private ISaveService _save;
        private ScrapGlints _glints;
        private GlowFlashPool _flashes;
        private Vector3[] _positions = Array.Empty<Vector3>();
        private bool[] _found = Array.Empty<bool>();
        private bool _glancing;
        private bool _initialized;

        /// <summary>Caches in the world.</summary>
        public int Count => _positions.Length;

        public LogCacheTuning Tuning => _tuning;

        public LogCacheDefinition Definition(int index)
        {
            return _catalog.Caches[index];
        }

        /// <summary>Where cache <paramref name="index"/> stands (on the surface).</summary>
        public Vector3 Position(int index)
        {
            return _positions[index];
        }

        /// <summary>07 has opened cache <paramref name="index"/>.</summary>
        public bool IsFound(int index)
        {
            return _found[index];
        }

        internal void Wire(LogCacheCatalog catalog, LogCacheTuning tuning)
        {
            _catalog = catalog;
            _tuning = tuning;
        }

        /// <summary>Stands every cache at its anchor. False (logged) when wiring or an anchor is missing.</summary>
        internal bool Initialize(GameplayServices services, ScrapTuning glintTuning)
        {
            string problem = _catalog == null ? "LogCacheCatalog is not assigned."
                : _tuning == null ? "LogCacheTuning is not assigned."
                : _catalog.Validate();
            if (problem != null)
            {
                Debug.LogError($"{nameof(LogCacheField)}: {problem}", this);
                enabled = false;
                return false;
            }

            _events = services.Events;
            _rover = services.Rover;
            _rig = services.Rig;
            _view = services.View;
            _save = services.Save;
            IReadOnlyList<LogCacheDefinition> caches = _catalog.Caches;
            _positions = new Vector3[caches.Count];
            _found = new bool[caches.Count];
            for (int i = 0; i < caches.Count; i++)
            {
                if (!caches[i].Anchor.TryResolve(services.Anchors, services.Terrain, out Vector3 position,
                        out Vector3 facing))
                {
                    Debug.LogError($"{nameof(LogCacheField)}: log cache '{caches[i].LogId}' stands at world anchor " +
                                   $"'{caches[i].Anchor.AnchorId}', which the World does not publish " +
                                   "(world anchors contract).", this);
                    enabled = false;
                    return false;
                }

                _positions[i] = position;
                GameObject cache = Instantiate(caches[i].Prefab, position, Quaternion.LookRotation(facing),
                    transform);
                cache.name = "LogCache_" + caches[i].LogId;
            }

            _glints = new ScrapGlints(transform, services.Visuals.PartGlint, glintTuning, Mathf.Max(1, Count),
                Layers.Prop);
            _flashes = new GlowFlashPool(transform, services.Meshes.Sphere, services.Visuals.WarmGlow,
                FlashPoolSize, _tuning.FlashDuration, _tuning.FlashRadius, _tuning.FlashIntensity);
            _initialized = true;
            return true;
        }

        internal LogsSaveData Capture()
        {
            var found = new List<string>();
            for (int i = 0; i < _found.Length; i++)
            {
                if (_found[i])
                {
                    found.Add(Definition(i).LogId);
                }
            }

            return new LogsSaveData { found = found.ToArray() };
        }

        /// <summary>Applies saved progress by log id; unknown ids (logs of later content) are kept out.</summary>
        internal void Restore(LogsSaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            foreach (string logId in data.found ?? Array.Empty<string>())
            {
                for (int i = 0; i < _found.Length; i++)
                {
                    _found[i] |= string.Equals(Definition(i).LogId, logId, StringComparison.Ordinal);
                }
            }
        }

        private void Update()
        {
            if (!_initialized)
            {
                return;
            }

            Vector3 rover = _rover.Position;
            Vector3 lift = Vector3.up * _tuning.GlowHeight;
            float reach = _tuning.ReachRadius;
            float nearestSq = _tuning.GlanceRadius * _tuning.GlanceRadius;
            int nearest = -1;
            _glints.Begin(_view.Camera.transform.position, Time.time);
            for (int i = 0; i < _positions.Length; i++)
            {
                if (_found[i])
                {
                    continue;
                }

                float distanceSq = SurfaceRules.HorizontalDistanceSquared(rover, _positions[i]);
                if (distanceSq <= reach * reach)
                {
                    Open(i, _positions[i] + lift);
                    continue;
                }

                _glints.Add(_positions[i] + lift, i);
                if (distanceSq < nearestSq)
                {
                    nearestSq = distanceSq;
                    nearest = i;
                }
            }

            _glints.End();
            _flashes.Tick(Time.deltaTime);
            UpdateGlance(nearest, lift);
        }

        private void Open(int index, Vector3 glow)
        {
            _found[index] = true;
            _flashes.Spawn(glow);
            _events.Publish(new CrewLogFound(Definition(index).LogId, _positions[index]));
            _save.SaveNow();
        }

        private void UpdateGlance(int nearest, Vector3 lift)
        {
            if (nearest >= 0)
            {
                _rig.SetGazeTarget(this, _positions[nearest] + lift, GazePriorities.Glance);
                _glancing = true;
            }
            else if (_glancing)
            {
                _rig.ClearGazeTarget(this);
                _glancing = false;
            }
        }

        private void OnDisable()
        {
            if (_glancing)
            {
                _rig.ClearGazeTarget(this);
                _glancing = false;
            }
        }

        private void OnDestroy()
        {
            _glints?.Dispose();
        }
    }
}
