using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;
using MoonProject.Core.Save;
using MoonProject.Gameplay.Map;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Bundles that did not fit in 07's hold (docs/features/M3-25 §2.3): each one that bumps the closed hatch
    /// (<see cref="CargoOverflowed"/>) settles beside 07 and waits there for ever, hovering, glinting from afar and
    /// swelling softly every few seconds. Once 07 has room, a press of E beside it (<see cref="PickupReach"/>) draws it
    /// in like any pickup; what still does not fit stays waiting. Tilly fetches the ones inside the station's reach
    /// (<see cref="TillyFetch"/>). Once per trip the line says the hold is full. Steady state allocates nothing; a new
    /// bundle instantiates its model.
    /// </summary>
    public sealed class CargoBundles : IWaitingBundles, IDisposable
    {
        /// <summary>The once-per-trip line when a bundle has to wait.</summary>
        public const string TickerFull = "ticker.cargo.full";

        private readonly List<Bundle> _bundles = new List<Bundle>();
        private readonly Transform _parent;
        private readonly SalvageCatalog _models;
        private readonly IMaterialCatalog _catalog;
        private readonly CargoTuning _tuning;
        private readonly FriendTuning _friends;
        private readonly MaterialStock _stock;
        private readonly EventBus _events;
        private readonly InputReader _input;
        private readonly PickupReach _pickups;
        private readonly IRoverState _rover;
        private readonly IRoverRig _rig;
        private readonly ITerrainQuery _terrain;
        private readonly IViewCamera _view;
        private readonly ISaveService _save;
        private readonly PickupGlints _glints;
        private readonly IDisposable _overflowed;
        private int _nextId;
        private bool _warned;

        private enum Phase
        {
            Settling,
            Waiting,
            Flying,
            Carried,
        }

        public CargoBundles(Transform parent, GameplayServices services, SalvageCatalog models,
            IMaterialCatalog catalog, CargoTuning tuning, FriendTuning friends)
        {
            _parent = parent != null ? parent : throw new ArgumentNullException(nameof(parent));
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            _models = models != null ? models : throw new ArgumentNullException(nameof(models));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _friends = friends != null ? friends : throw new ArgumentNullException(nameof(friends));
            _stock = services.Materials;
            _events = services.Events;
            _input = services.Input;
            _pickups = services.Pickups;
            _rover = services.Rover;
            _rig = services.Rig;
            _terrain = services.Terrain;
            _view = services.View;
            _save = services.Save;
            _glints = new PickupGlints(parent, services.Visuals.PartGlint, services.Glints, services.GlintVeil,
                tuning.BundleGlints, Layers.Pickup);
            _overflowed = _events.Subscribe<CargoOverflowed>(OnOverflowed);
        }

        /// <summary>Bundles waiting in the world (carried by Tilly included).</summary>
        public int Count => _bundles.Count;

        /// <summary>Where bundle <paramref name="index"/> is: where it rests, or under Tilly while she carries it.</summary>
        public Vector3 Position(int index)
        {
            Bundle bundle = _bundles[index];
            return bundle.Phase == Phase.Waiting || bundle.Phase == Phase.Settling ? bundle.Rest : bundle.Model.position;
        }

        /// <summary>Where bundle <paramref name="index"/> rests (tests and debugging views).</summary>
        public Vector3 RestPosition(int index)
        {
            return _bundles[index].Rest;
        }

        /// <summary>True while bundle <paramref name="index"/> rests, waiting to be taken.</summary>
        public bool IsWaiting(int index)
        {
            return _bundles[index].Phase == Phase.Waiting;
        }

        /// <summary>The stable id of bundle <paramref name="index"/> (indices shift as bundles leave).</summary>
        public int IdAt(int index)
        {
            return _bundles[index].Id;
        }

        /// <summary>The index of the bundle with <paramref name="id"/>, or -1 once it is gone.</summary>
        public int IndexOf(int id)
        {
            for (int i = 0; i < _bundles.Count; i++)
            {
                if (_bundles[i].Id == id)
                {
                    return i;
                }
            }

            return -1;
        }

        public SalvageMaterial MaterialAt(int index)
        {
            return _bundles[index].Material;
        }

        public int UnitsAt(int index)
        {
            return _bundles[index].Units;
        }

        /// <summary>07 is home again (the dock's unload): the next full hold says so once more.</summary>
        public void NewTrip()
        {
            _warned = false;
        }

        public void Step(float now, float deltaTime)
        {
            Vector3 socket = _rig.CargoSocket.position;
            _glints.Begin(_view.Camera.transform.position, now);
            for (int i = _bundles.Count - 1; i >= 0; i--)
            {
                Bundle bundle = _bundles[i];
                switch (bundle.Phase)
                {
                    case Phase.Settling:
                        StepSettling(bundle, now);
                        break;
                    case Phase.Waiting:
                        StepWaiting(bundle, now);
                        break;
                    case Phase.Flying:
                        StepFlying(i, bundle, socket, now);
                        break;
                }
            }

            _glints.End();
            _stock.Hold.SetWaitingBundles(_bundles.Count);
        }

        /// <summary>
        /// The nearest waiting bundle inside <paramref name="reach"/> whose material home still has room for, for
        /// Tilly to fetch; -1 when there is none.
        /// </summary>
        public int NearestToFetch(IStationReach reach, Vector3 from)
        {
            int best = -1;
            float bestSq = float.MaxValue;
            for (int i = 0; i < _bundles.Count; i++)
            {
                Bundle bundle = _bundles[i];
                if (bundle.Phase != Phase.Waiting || !reach.IsInReach(bundle.Rest) ||
                    _stock.Home.FreeUnits(bundle.Material) < bundle.Units)
                {
                    continue;
                }

                float distanceSq = (bundle.Rest - from).sqrMagnitude;
                if (distanceSq < bestSq)
                {
                    bestSq = distanceSq;
                    best = i;
                }
            }

            return best;
        }

        /// <summary>Tilly hooks bundle <paramref name="index"/>: from now on it hangs where she carries it.</summary>
        public void Hook(int index)
        {
            _bundles[index].Phase = Phase.Carried;
        }

        /// <summary>Bundle <paramref name="index"/> hangs at <paramref name="at"/> under Tilly.</summary>
        public void Carry(int index, Vector3 at, float now)
        {
            Bundle bundle = _bundles[index];
            bundle.Model.SetPositionAndRotation(at, Quaternion.Euler(0f, now * _friends.PartSpin, 0f));
        }

        /// <summary>
        /// Tilly sets bundle <paramref name="index"/> on home's shelves (<see cref="BundleFetched"/>). What home cannot
        /// hold any more (it filled while she flew) waits again where she is.
        /// </summary>
        public void Deliver(int index, Vector3 at, float now)
        {
            Bundle bundle = _bundles[index];
            int left = _stock.Deliver(bundle.Material, bundle.Units);
            int fetched = bundle.Units - left;
            if (fetched > 0)
            {
                _events.Publish(new BundleFetched(bundle.Material, fetched));
            }

            if (left > 0)
            {
                bundle.Units = left;
                Settle(bundle, at, Ground(at), now);
            }
            else
            {
                Remove(index);
            }

            _save.SaveNow();
        }

        /// <summary>Tilly lets bundle <paramref name="index"/> go (called away): it settles where she was.</summary>
        public void Drop(int index, Vector3 at, float now)
        {
            Bundle bundle = _bundles[index];
            Settle(bundle, at, Ground(at), now);
        }

        public WaitingBundleSaveData[] Capture()
        {
            var saved = new WaitingBundleSaveData[_bundles.Count];
            for (int i = 0; i < saved.Length; i++)
            {
                Bundle bundle = _bundles[i];
                Vector3 at = bundle.Phase == Phase.Waiting || bundle.Phase == Phase.Settling ? bundle.Rest
                    : bundle.Model.position;
                saved[i] = new WaitingBundleSaveData
                {
                    material = (int)bundle.Material, units = bundle.Units, x = at.x, y = at.y, z = at.z,
                };
            }

            return saved;
        }

        /// <summary>Lays a save's waiting bundles out again, each resting where it was.</summary>
        public void Restore(WaitingBundleSaveData[] saved)
        {
            for (int i = _bundles.Count - 1; i >= 0; i--)
            {
                Remove(i);
            }

            if (saved == null)
            {
                return;
            }

            foreach (WaitingBundleSaveData data in saved)
            {
                if (data.units <= 0 || data.material < 0 || data.material >= SalvageMaterials.Count)
                {
                    throw new FormatException($"A saved waiting bundle is broken ({data.material} x {data.units}).");
                }

                var at = new Vector3(data.x, data.y, data.z);
                Bundle bundle = Spawn((SalvageMaterial)data.material, data.units, at);
                bundle.Rest = Ground(at);
                bundle.Phase = Phase.Waiting;
                bundle.Model.position = bundle.Rest;
                bundle.Model.localScale = bundle.Scale;
            }

            _stock.Hold.SetWaitingBundles(_bundles.Count);
        }

        public void Dispose()
        {
            _overflowed.Dispose();
            _glints.Dispose();
            for (int i = _bundles.Count - 1; i >= 0; i--)
            {
                Remove(i);
            }
        }

        private void OnOverflowed(CargoOverflowed overflowed)
        {
            Bundle bundle = Spawn(overflowed.Material, overflowed.UnitsLeft, overflowed.Position);
            Vector3 side = _rover.Rotation * (_bundles.Count % 2 == 0 ? Vector3.right : Vector3.left);
            Settle(bundle, overflowed.Position, Ground(_rover.Position + side * _tuning.BundleSettleDistance),
                Time.time);
            if (!_warned)
            {
                _warned = true;
                _events.Publish(new TickerLine(TickerFull));
            }
        }

        private Bundle Spawn(SalvageMaterial material, int units, Vector3 at)
        {
            GameObject prefab = _models.Bundle(material);
            if (prefab == null)
            {
                throw new InvalidOperationException($"No bundle model for {material} (SalvageCatalog).");
            }

            Transform model = Object.Instantiate(prefab, at, Quaternion.identity, _parent).transform;
            model.name = "WaitingBundle_" + material;
            var bundle = new Bundle
            {
                Id = _nextId++, Material = material, Units = units, Model = model, Scale = model.localScale,
                Noun = _catalog.NameKey(material),
            };
            _bundles.Add(bundle);
            return bundle;
        }

        private void Settle(Bundle bundle, Vector3 from, Vector3 rest, float now)
        {
            bundle.Phase = Phase.Settling;
            bundle.From = from;
            bundle.Rest = rest;
            bundle.Since = now;
        }

        private void StepSettling(Bundle bundle, float now)
        {
            float t = Mathf.Clamp01((now - bundle.Since) / _tuning.BundleSettleSeconds);
            Vector3 at = Vector3.Lerp(bundle.From, bundle.Rest, Ease.OutCubic(t)) +
                         Vector3.up * (_friends.PartFlightLift * Ease.Hump(t));
            bundle.Model.SetPositionAndRotation(at, Quaternion.Euler(0f, now * _friends.PartSpin, 0f));
            bundle.Model.localScale = bundle.Scale;
            if (t >= 1f)
            {
                bundle.Phase = Phase.Waiting;
                bundle.Since = now;
            }
        }

        private void StepWaiting(Bundle bundle, float now)
        {
            float bob = Mathf.Sin(now * 2f * Mathf.PI * _friends.PartBobFrequency + bundle.Id) * _friends.PartBob;
            Vector3 at = bundle.Rest + Vector3.up * bob;
            float pulse = Ease.Hump(Mathf.Repeat((now - bundle.Since) / _tuning.BundlePulseSeconds, 1f));
            bundle.Model.SetPositionAndRotation(at, Quaternion.Euler(0f, now * _friends.PartSpin, 0f));
            bundle.Model.localScale = bundle.Scale * (1f + _tuning.BundlePulse * pulse);
            _glints.Add(bundle.Rest, bundle.Id);
            if (_stock.Hold.FreeUnits(bundle.Material) <= 0)
            {
                return;
            }

            if (_pickups.Offer(Time.frameCount, _input.ExcavateHeld, PickupReach.Bundles, bundle.Id, at,
                    _rover.Position, _friends.PartReachRadius, bundle.Noun))
            {
                bundle.Phase = Phase.Flying;
                bundle.From = at;
                bundle.Since = now;
                bundle.Flight = PickupFlight.Duration(Vector3.Distance(at, _rig.CargoSocket.position),
                    _friends.PartFlightDuration, _friends.PartFlightPerMetre);
                PickupReach.Lifted(_events, at, bundle.Flight);
                _events.Publish(new FeelBeat(FeelKind.Collect, FeelPhase.Anticipate, at, Vector3.up, 1f, bundle.Flight,
                    bundle.Material));
            }
        }

        private void StepFlying(int index, Bundle bundle, Vector3 socket, float now)
        {
            float progress = (now - bundle.Since) / bundle.Flight;
            if (progress < 1f)
            {
                bundle.Model.SetPositionAndRotation(
                    PickupFlight.Evaluate(bundle.From, socket, progress, bundle.Id, 1f, _friends.PartFlightLift,
                        _friends.PartSpiralRadius, _friends.PartSpiralTurns),
                    Quaternion.Euler(0f, now * _friends.PartSpin, 0f));
                bundle.Model.localScale = bundle.Scale *
                                          Mathf.Lerp(1f, _friends.PartArrivalScale, Ease.InOutSine(progress));
                return;
            }

            int left = _stock.Collect(bundle.Material, bundle.Units);
            int taken = bundle.Units - left;
            if (taken > 0)
            {
                _events.Publish(new FeelBeat(FeelKind.Collect, FeelPhase.Reward, socket, Vector3.up, 1f, 0f,
                    bundle.Material));
                _events.Publish(new CargoPickupTaken(bundle.Material, taken));
            }
            else
            {
                _events.Publish(new FeelBeat(FeelKind.Cargo, FeelPhase.Impact, socket, Vector3.up, 1f, 0f,
                    bundle.Material));
            }

            if (left > 0)
            {
                bundle.Units = left;
                Settle(bundle, socket, Ground(bundle.From), now);
            }
            else
            {
                Remove(index);
            }

            _save.SaveNow();
        }

        private Vector3 Ground(Vector3 at)
        {
            return new Vector3(at.x, _terrain.SampleHeight(at.x, at.z) + _tuning.BundleHover, at.z);
        }

        private void Remove(int index)
        {
            Bundle bundle = _bundles[index];
            if (bundle.Model != null)
            {
                Object.Destroy(bundle.Model.gameObject);
            }

            _bundles.RemoveAt(index);
        }

        private sealed class Bundle
        {
            public int Id;
            public SalvageMaterial Material;
            public int Units;
            public string Noun;
            public Transform Model;
            public Vector3 Scale;
            public Vector3 From;
            public Vector3 Rest;
            public float Since;
            public float Flight;
            public Phase Phase;
        }
    }
}
