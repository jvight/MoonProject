using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// What did not fit in 07's hold, held where it came from (docs/features/M3-25 §2.3, 0.5.5 §2: nothing pickable
    /// rests on open regolith). A salvage piece cut with no room stays on its wreck, cut but held, its halo shell
    /// breathing amber; once 07 has room for all of it, a press of E beside it lets it fold and fly in like any cut
    /// piece. Once per trip (until 07 next charges) the line says the hold is full. Each piece saves its own held
    /// state. Steady state allocates nothing.
    /// </summary>
    public sealed class CargoBundles : IDisposable
    {
        /// <summary>The once-per-trip line when something has to be held back.</summary>
        public const string TickerFull = "ticker.cargo.full";

        private readonly List<Stack> _stacks = new List<Stack>();
        private readonly SalvageField _salvage;
        private readonly IMaterialCatalog _catalog;
        private readonly CargoTuning _tuning;
        private readonly MaterialStock _stock;
        private readonly EventBus _events;
        private readonly InputReader _input;
        private readonly PickupReach _pickups;
        private readonly IRoverState _rover;
        private readonly IViewCamera _view;
        private readonly PickupGlints _glints;
        private int _nextId;
        private bool _warned;

        public CargoBundles(Transform parent, GameplayServices services, SalvageField salvage,
            IMaterialCatalog catalog, CargoTuning tuning)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            _salvage = salvage != null ? salvage : throw new ArgumentNullException(nameof(salvage));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _stock = services.Materials;
            _events = services.Events;
            _input = services.Input;
            _pickups = services.Pickups;
            _rover = services.Rover;
            _view = services.View;
            _glints = new PickupGlints(parent, services.Visuals.PartGlint, services.Glints, services.GlintVeil,
                tuning.BundleGlints, Layers.Pickup);
        }

        /// <summary>Pieces held on their wrecks.</summary>
        public int Count => _stacks.Count;

        /// <summary>Where held piece <paramref name="index"/> waits: its middle.</summary>
        public Vector3 RestPosition(int index)
        {
            return _stacks[index].Rest;
        }

        public SalvageMaterial MaterialAt(int index)
        {
            return _stacks[index].Material;
        }

        public int UnitsAt(int index)
        {
            return _stacks[index].Units;
        }

        /// <summary>The held piece <paramref name="index"/>.</summary>
        public SalvagePiece PieceAt(int index)
        {
            return _stacks[index].Piece;
        }

        /// <summary>07 has charged again: the next full hold says so once more.</summary>
        public void NewTrip()
        {
            _warned = false;
        }

        /// <summary>
        /// A piece cut with no room for it stays on its wreck, held, its halo amber. <paramref name="announce"/>: it
        /// was just cut (the hatch bump and the line), not loaded from a save.
        /// </summary>
        internal void HoldPiece(SalvagePiece piece, bool announce)
        {
            if (piece == null)
            {
                throw new ArgumentNullException(nameof(piece));
            }

            Vector3 middle = piece.Body.TransformPoint(piece.LocalCentre);
            _stacks.Add(new Stack
            {
                Id = _nextId++, Material = piece.Material, Units = piece.Units, Piece = piece, Rest = middle,
                Noun = _catalog.NameKey(piece.Material),
            });
            piece.Halo.Tint(_tuning.HeldHaloColor);
            if (announce)
            {
                Overflowed(piece.Material, piece.Units, middle);
            }
        }

        public void Step(float now)
        {
            _glints.Begin(_view.Camera.transform.position, now);
            for (int i = _stacks.Count - 1; i >= 0; i--)
            {
                StepWaiting(i, _stacks[i], now);
            }

            _glints.End();
            _stock.Hold.SetWaitingBundles(_stacks.Count);
        }

        public void Dispose()
        {
            _glints.Dispose();
            _stacks.Clear();
        }

        /// <summary>The hatch bump, the event and, once per trip, the line: something had to be held back.</summary>
        private void Overflowed(SalvageMaterial material, int units, Vector3 at)
        {
            _events.Publish(new FeelBeat(FeelKind.Cargo, FeelPhase.Impact, at, Vector3.up, 1f, 0f, material));
            _events.Publish(new CargoOverflowed(material, units, at));
            if (!_warned)
            {
                _warned = true;
                _events.Publish(new TickerLine(TickerFull));
            }
        }

        /// <summary>
        /// A held piece breathes its amber halo on its wreck and glints; with room in the hold for all of it, a press
        /// of E within the cutting reach takes it.
        /// </summary>
        private void StepWaiting(int index, Stack stack, float now)
        {
            float pulse = Ease.Hump(Mathf.Repeat(now / _tuning.BundlePulseSeconds + stack.Id * 0.37f, 1f));
            _glints.Add(stack.Rest, stack.Id);
            stack.Piece.Halo.Apply(_tuning.HeldHalo * (1f - _tuning.BundlePulse + _tuning.BundlePulse * pulse));
            if (_stock.Room(stack.Material) < stack.Units ||
                !_pickups.Offer(Time.frameCount, _input.ExcavateHeld, PickupReach.Bundles, stack.Id, stack.Rest,
                    _rover.Position, _salvage.Tuning.ReachRadius, stack.Noun))
            {
                return;
            }

            _salvage.Release(stack.Piece);
            _stacks.RemoveAt(index);
        }

        private sealed class Stack
        {
            public int Id;
            public SalvageMaterial Material;
            public int Units;
            public string Noun;
            public SalvagePiece Piece;
            public Vector3 Rest;
        }
    }
}
