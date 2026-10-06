using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Every relic in the world: plans the burial sites at initialisation, spawns one <see cref="Relic"/> per catalog
    /// entry, and keeps the "no loss, ever" promise — a loose relic that comes to rest off the drivable floor (or
    /// sinks under it) floats gently back to the nearest reachable spot on the way home.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RelicField : MonoBehaviour
    {
        /// <summary>Metres per step when searching for the nearest reachable spot toward home.</summary>
        private const float ReturnSearchStep = 2f;

        /// <summary>Above this speed (m/s) a relic is still moving and is given time to settle on its own.</summary>
        private const float RestingSpeed = 0.3f;

        [Tooltip("Relic definitions (Assets/_Project/Data/Content/RelicCatalog.asset).")]
        [SerializeField] private RelicCatalog _catalog;

        [Tooltip("Burial site placement (Assets/_Project/Data/Tuning/Gameplay/RelicPlacementTuning.asset).")]
        [SerializeField] private RelicPlacementTuning _placement;

        [Tooltip("Relic physics, halo and motion (Assets/_Project/Data/Tuning/Gameplay/RelicTuning.asset).")]
        [SerializeField] private RelicTuning _tuning;

        private Relic[] _relics = Array.Empty<Relic>();
        private RelicSite[] _sites = Array.Empty<RelicSite>();
        private float[] _offFloorTime = Array.Empty<float>();
        private float[] _returnTime = Array.Empty<float>();
        private Vector3[] _returnStart = Array.Empty<Vector3>();
        private Vector3[] _returnEnd = Array.Empty<Vector3>();
        private Quaternion[] _returnStartRotation = Array.Empty<Quaternion>();
        private PhysicsMaterial _physicsMaterial;
        private ITerrainQuery _terrain;
        private IWorldLayout _layout;
        private bool _initialized;

        public IReadOnlyList<Relic> Relics => _relics;

        public IReadOnlyList<RelicSite> Sites => _sites;

        public RelicTuning Tuning => _tuning;

        internal void Wire(RelicCatalog catalog, RelicPlacementTuning placement, RelicTuning tuning)
        {
            _catalog = catalog;
            _placement = placement;
            _tuning = tuning;
        }

        internal bool Initialize(GameplayServices services)
        {
            string problem = _catalog == null ? "RelicCatalog is not assigned."
                : _placement == null ? "RelicPlacementTuning is not assigned."
                : _tuning == null ? "RelicTuning is not assigned."
                : _catalog.Validate();
            if (problem != null)
            {
                Debug.LogError($"{nameof(RelicField)}: {problem}", this);
                enabled = false;
                return false;
            }

            _terrain = services.Terrain;
            _layout = services.Layout;
            IReadOnlyList<RelicDefinition> definitions = _catalog.Relics;
            var bands = new RelicPlacementBand[definitions.Count];
            for (int i = 0; i < bands.Length; i++)
            {
                bands[i] = definitions[i].Placement;
            }

            _sites = RelicSitePlanner.Plan(_terrain, _layout, _placement, bands);
            _physicsMaterial = new PhysicsMaterial("Relic")
            {
                bounciness = _tuning.Bounciness,
                dynamicFriction = _tuning.Friction,
                staticFriction = _tuning.Friction,
                bounceCombine = PhysicsMaterialCombine.Average,
                frictionCombine = PhysicsMaterialCombine.Average,
            };

            _relics = new Relic[definitions.Count];
            _offFloorTime = new float[_relics.Length];
            _returnTime = new float[_relics.Length];
            _returnStart = new Vector3[_relics.Length];
            _returnEnd = new Vector3[_relics.Length];
            _returnStartRotation = new Quaternion[_relics.Length];
            for (int i = 0; i < _relics.Length; i++)
            {
                var host = new GameObject("Relic_" + definitions[i].Id);
                host.transform.SetParent(transform, false);
                _relics[i] = host.AddComponent<Relic>();
                _relics[i].Setup(definitions[i], i, _sites[i], _tuning, _physicsMaterial,
                    services.Visuals.RelicHalo);
            }

            _initialized = true;
            return true;
        }

        /// <summary>The relic with <paramref name="id"/>, or null.</summary>
        public Relic Find(string id)
        {
            for (int i = 0; i < _relics.Length; i++)
            {
                if (string.Equals(_relics[i].Definition.Id, id, StringComparison.Ordinal))
                {
                    return _relics[i];
                }
            }

            return null;
        }

        internal RelicsSaveData Capture()
        {
            var data = new RelicsSaveData { relics = new RelicSaveData[_relics.Length] };
            for (int i = 0; i < _relics.Length; i++)
            {
                RelicSaveData relic = _relics[i].Capture();
                if (_relics[i].State == RelicState.Returning)
                {
                    relic.state = (int)RelicState.Loose;
                    relic.position = _returnEnd[i];
                }

                data.relics[i] = relic;
            }

            return data;
        }

        /// <summary>
        /// Applies saved relic states by id (unknown ids are ignored, missing ones stay buried). A relic saved
        /// mid-deposit comes back displayed: HomeBase places displayed relics on their slots afterwards.
        /// </summary>
        internal void Restore(RelicsSaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            foreach (RelicSaveData saved in data.relics)
            {
                Relic relic = saved != null ? Find(saved.id) : null;
                if (relic == null)
                {
                    continue;
                }

                var state = (RelicState)saved.state;
                if (state == RelicState.Depositing)
                {
                    state = RelicState.Displayed;
                }
                else if (state == RelicState.Returning || !Enum.IsDefined(typeof(RelicState), state))
                {
                    state = RelicState.Loose;
                }

                relic.Restore(state, saved.progress, saved.discovered, saved.position, saved.rotation, saved.slot);
                _offFloorTime[relic.Index] = 0f;
            }
        }

        private void Update()
        {
            if (!_initialized)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            for (int i = 0; i < _relics.Length; i++)
            {
                Relic relic = _relics[i];
                if (relic.State == RelicState.Returning)
                {
                    StepReturn(i, deltaTime);
                }
                else if (relic.State == RelicState.Loose && !relic.IsTethered)
                {
                    WatchReach(i, deltaTime);
                }
                else
                {
                    _offFloorTime[i] = 0f;
                }
            }
        }

        private void WatchReach(int index, float deltaTime)
        {
            Relic relic = _relics[index];
            Vector3 position = relic.transform.position;
            float ground = _terrain.SampleHeight(position.x, position.z);
            if (position.y < ground - _tuning.BelowSurfaceLimit)
            {
                BeginReturn(index);
                return;
            }

            bool resting = relic.Body.linearVelocity.sqrMagnitude < RestingSpeed * RestingSpeed;
            if (_terrain.IsDrivable(position.x, position.z) || !resting)
            {
                _offFloorTime[index] = 0f;
                return;
            }

            _offFloorTime[index] += deltaTime;
            if (_offFloorTime[index] >= _tuning.ReturnDelay)
            {
                BeginReturn(index);
            }
        }

        private void BeginReturn(int index)
        {
            Relic relic = _relics[index];
            Vector3 from = relic.transform.position;
            Vector3 spot = ReachableSpotToward(from, _layout.BasePosition);
            _returnStart[index] = from;
            _returnStartRotation[index] = relic.transform.rotation;
            _returnEnd[index] = SurfaceRules.OnSurface(_terrain, spot.x, spot.z) + Vector3.up * _tuning.ReturnHover;
            _returnTime[index] = 0f;
            _offFloorTime[index] = 0f;
            relic.BeginCarry(RelicState.Returning, -1);
        }

        private void StepReturn(int index, float deltaTime)
        {
            _returnTime[index] += deltaTime;
            float progress = _returnTime[index] / _tuning.ReturnDuration;
            Relic relic = _relics[index];
            Vector3 position = GlidePath.Position(_returnStart[index], _returnEnd[index], progress,
                _tuning.ReturnHover, 0f, 0f);
            Quaternion rotation = GlidePath.Rotation(_returnStartRotation[index],
                Quaternion.Euler(0f, _returnStartRotation[index].eulerAngles.y, 0f), progress);
            relic.SetCarryPose(position, rotation);
            if (progress >= 1f)
            {
                relic.Release();
            }
        }

        /// <summary>First point from <paramref name="from"/> toward <paramref name="home"/> inside the drivable floor
        /// with the return margin (home itself if the whole line is off the floor).</summary>
        private Vector3 ReachableSpotToward(Vector3 from, Vector3 home)
        {
            Vector3 toHome = home - from;
            toHome.y = 0f;
            float distance = toHome.magnitude;
            if (distance < 1e-3f)
            {
                return home;
            }

            Vector3 direction = toHome / distance;
            for (float travelled = 0f; travelled < distance; travelled += ReturnSearchStep)
            {
                Vector3 point = from + direction * travelled;
                if (SurfaceRules.InsideDrivable(_terrain, point.x, point.z, _tuning.ReturnMargin))
                {
                    return point;
                }
            }

            return home;
        }

        private void OnDestroy()
        {
            if (_physicsMaterial != null)
            {
                Object.Destroy(_physicsMaterial);
            }
        }
    }
}
