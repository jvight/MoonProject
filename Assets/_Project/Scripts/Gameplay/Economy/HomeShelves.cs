using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;
using MoonProject.Core.Save;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Home's open supply shelves as they stand under the belly ring (docs/features/M3-25 §3.1, T2): every slot shows
    /// its stack as crates in its material's colour, one to three by how full it is, so the stash visibly grows trip by
    /// trip; each built shelf's rack lamp lights once the tower powers home. A free socket's shelf is built with a hold
    /// of E beside it once home and 07 cover its recipe: it unfolds from its crate (a Build Impact as it starts, the
    /// Reward as it stands; <see cref="ShelfBuilt"/> from <see cref="HomeStorage"/>). Crates are made only when a slot
    /// changes; a frame allocates nothing.
    /// </summary>
    public sealed class HomeShelves : IDisposable
    {
        /// <summary>Crate seats per slot (Stack_0..2 on Art's rack).</summary>
        public const int Seats = 3;

        private readonly HomeStorage _home;
        private readonly MaterialStock _stock;
        private readonly CargoTuning _tuning;
        private readonly EventBus _events;
        private readonly InputReader _input;
        private readonly IRoverState _rover;
        private readonly ISaveService _save;
        private readonly Transform[] _racks;
        private readonly Vector3[] _rackScales;
        private readonly EmissionGlow[] _lamps;
        private readonly Transform[][] _seats;
        private readonly GameObject[] _cratePrefabs;
        private readonly Transform[][] _crates;
        private readonly SalvageMaterial[][] _crateMaterials;
        private readonly IDisposable _changed;
        private bool _dirty = true;
        private float _held;
        private int _unfolding = -1;
        private float _unfoldStart;

        /// <param name="racks">Shelf_1.. in shelf order (the built-in ones first).</param>
        /// <param name="lamps">Each rack's lamp.</param>
        /// <param name="seats">Each rack's crate seats, slot by slot (Slot_00/Stack_0..2, Slot_01/Stack_0..).</param>
        /// <param name="cratePrefabs">Art's ShelfCrate_&lt;Material&gt;, by material (null where none).</param>
        public HomeShelves(MaterialStock stock, CargoTuning tuning, EventBus events, InputReader input,
            IRoverState rover, ISaveService save, Transform[] racks, Renderer[] lamps, Transform[][] seats,
            GameObject[] cratePrefabs)
        {
            _stock = stock ?? throw new ArgumentNullException(nameof(stock));
            _home = stock.Home;
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _rover = rover ?? throw new ArgumentNullException(nameof(rover));
            _save = save ?? throw new ArgumentNullException(nameof(save));
            int count = _home.ShelfCount;
            if (racks == null || lamps == null || seats == null || racks.Length != count || lamps.Length != count ||
                seats.Length != count)
            {
                throw new ArgumentException($"Home has {count} shelves: each needs its rack, lamp and seats.");
            }

            _cratePrefabs = cratePrefabs ?? throw new ArgumentNullException(nameof(cratePrefabs));
            _racks = racks;
            _rackScales = new Vector3[count];
            _lamps = new EmissionGlow[count];
            _seats = seats;
            _crates = new Transform[count][];
            _crateMaterials = new SalvageMaterial[count][];
            for (int i = 0; i < count; i++)
            {
                HomeShelf shelf = _home.GetShelf(i);
                if (racks[i] == null || lamps[i] == null || seats[i] == null ||
                    seats[i].Length != shelf.SlotCount * Seats)
                {
                    throw new ArgumentException($"Shelf {i + 1} needs its rack, lamp and {shelf.SlotCount * Seats} " +
                                                "crate seats.");
                }

                _rackScales[i] = racks[i].localScale;
                _lamps[i] = new EmissionGlow(lamps[i]);
                _crates[i] = new Transform[seats[i].Length];
                _crateMaterials[i] = new SalvageMaterial[seats[i].Length];
            }

            _changed = events.Subscribe<HomeStorageChanged>(_ => _dirty = true);
        }

        /// <summary>The free socket 07 can build at now (beside it, the next in order), or -1.</summary>
        public int Candidate { get; private set; } = -1;

        /// <summary>True while home and 07 together cover <see cref="Candidate"/>'s shelf.</summary>
        public bool Affordable => Candidate >= 0 && _stock.Has(CostOf(Candidate));

        /// <summary>Where shelf <paramref name="index"/> stands.</summary>
        public Vector3 RackPosition(int index)
        {
            return _racks[index].position;
        }

        public void Step(float now, float deltaTime)
        {
            StepBuild(now, deltaTime);
            StepUnfold(now);
            for (int i = 0; i < _racks.Length; i++)
            {
                HomeShelf shelf = _home.GetShelf(i);
                bool standing = shelf.Built && i != _unfolding;
                _lamps[i].Apply(standing && shelf.Powered ? _tuning.RackLampGlow : 0f);
                if (_racks[i].gameObject.activeSelf != shelf.Built)
                {
                    _racks[i].gameObject.SetActive(shelf.Built);
                }
            }

            if (_dirty)
            {
                _dirty = false;
                LayCrates();
            }
        }

        public void Dispose()
        {
            _changed.Dispose();
        }

        private Recipe CostOf(int socket)
        {
            return _tuning.ShelfCost(socket - _home.BuiltIn);
        }

        private void StepBuild(float now, float deltaTime)
        {
            int next = _home.NextSocket;
            Candidate = next >= 0 && _unfolding < 0 &&
                        SurfaceRules.HorizontalDistance(_rover.Position, _racks[next].position) <= _tuning.SocketRadius
                ? next
                : -1;
            if (Candidate < 0 || !_input.ExcavateHeld || !Affordable)
            {
                _held = 0f;
                return;
            }

            _held += deltaTime;
            if (_held < _tuning.BuildHoldSeconds || !_stock.TrySpend(CostOf(Candidate)))
            {
                return;
            }

            _held = 0f;
            _unfolding = Candidate;
            _unfoldStart = now;
            _home.Build(Candidate);
            _racks[_unfolding].localScale = Vector3.zero;
            Publish(FeelPhase.Impact, _racks[_unfolding].position);
            _save.SaveNow();
        }

        private void StepUnfold(float now)
        {
            if (_unfolding < 0)
            {
                return;
            }

            float t = Mathf.Clamp01((now - _unfoldStart) / _tuning.UnfoldSeconds);
            _racks[_unfolding].localScale = _rackScales[_unfolding] * Ease.OutCubic(t);
            if (t < 1f)
            {
                return;
            }

            Publish(FeelPhase.Reward, _racks[_unfolding].position);
            _unfolding = -1;
        }

        /// <summary>Every slot's crates as its stack is now: made, swapped or put away only where they changed.</summary>
        private void LayCrates()
        {
            for (int i = 0; i < _racks.Length; i++)
            {
                HomeShelf shelf = _home.GetShelf(i);
                for (int s = 0; s < shelf.SlotCount; s++)
                {
                    MaterialSlot slot = shelf.Built ? _home.GetSlot(shelf.FirstSlot + s) : default;
                    int shown = slot.IsEmpty ? 0
                        : Mathf.Clamp(Mathf.CeilToInt((float)slot.Units * _tuning.CratesPerSlot / slot.Capacity), 1,
                            _tuning.CratesPerSlot);
                    for (int seat = 0; seat < Seats; seat++)
                    {
                        LayCrate(i, s * Seats + seat, seat < shown, slot.Material);
                    }
                }
            }
        }

        private void LayCrate(int shelf, int seat, bool wanted, SalvageMaterial material)
        {
            Transform crate = _crates[shelf][seat];
            if (crate != null && (!wanted || _crateMaterials[shelf][seat] != material))
            {
                Object.Destroy(crate.gameObject);
                _crates[shelf][seat] = null;
            }

            if (!wanted || _crates[shelf][seat] != null)
            {
                return;
            }

            GameObject prefab = (int)material < _cratePrefabs.Length ? _cratePrefabs[(int)material] : null;
            if (prefab == null)
            {
                throw new InvalidOperationException($"No shelf crate for {material} (Art's ShelfCrate_{material}).");
            }

            Transform made = Object.Instantiate(prefab, _seats[shelf][seat], false).transform;
            made.localPosition = Vector3.zero;
            made.localRotation = Quaternion.identity;
            _crates[shelf][seat] = made;
            _crateMaterials[shelf][seat] = material;
        }

        private void Publish(FeelPhase phase, Vector3 at)
        {
            _events.Publish(new FeelBeat(FeelKind.Build, phase, at, Vector3.up, 1f, 0f));
        }
    }
}
