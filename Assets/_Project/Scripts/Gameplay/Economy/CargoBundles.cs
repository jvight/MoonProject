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
    /// What did not fit in 07's hold, held where it came from (docs/features/M3-25 §2.3, 0.5.5 §2: nothing pickable
    /// rests on open regolith). A salvage piece cut with no room stays on its wreck, cut but held, its halo shell
    /// breathing amber; once 07 has room for all of it, a press of E beside it lets it fold and fly in like any cut
    /// piece. A crew crate's remainder waits as a bundle in the crate's open mouth; a press of E takes what fits.
    /// Tilly fetches the stacks inside the station's reach to home (<see cref="TillyFetch"/>), and the survey chart
    /// marks them (<see cref="IWaitingBundles"/>). Once per trip the line says the hold is full. Each holder saves its
    /// own stack (the piece's held state, the crate's remainder). Steady state allocates nothing.
    /// </summary>
    public sealed class CargoBundles : IWaitingBundles, IDisposable
    {
        /// <summary>The once-per-trip line when something has to be held back.</summary>
        public const string TickerFull = "ticker.cargo.full";

        private readonly List<Stack> _stacks = new List<Stack>();
        private readonly Transform _parent;
        private readonly SalvageField _salvage;
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
        private readonly IViewCamera _view;
        private readonly ISaveService _save;
        private readonly PickupGlints _glints;
        private int _nextId;
        private bool _warned;

        private enum Phase
        {
            Waiting,
            Flying,
            Returning,
            Carried,
        }

        public CargoBundles(Transform parent, GameplayServices services, SalvageField salvage, SalvageCatalog models,
            IMaterialCatalog catalog, CargoTuning tuning, FriendTuning friends)
        {
            _parent = parent != null ? parent : throw new ArgumentNullException(nameof(parent));
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            _salvage = salvage != null ? salvage : throw new ArgumentNullException(nameof(salvage));
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
            _view = services.View;
            _save = services.Save;
            _glints = new PickupGlints(parent, services.Visuals.PartGlint, services.Glints, services.GlintVeil,
                tuning.BundleGlints, Layers.Pickup);
        }

        /// <summary>Stacks held in the world (carried by Tilly included).</summary>
        public int Count => _stacks.Count;

        /// <summary>
        /// Where stack <paramref name="index"/> is: on its holder, or under Tilly while she carries it.
        /// </summary>
        public Vector3 Position(int index)
        {
            Stack stack = _stacks[index];
            return stack.Phase == Phase.Waiting ? stack.Rest : stack.Model.position;
        }

        /// <summary>Where stack <paramref name="index"/> is held: its piece's middle, or its crate's mouth.</summary>
        public Vector3 RestPosition(int index)
        {
            return _stacks[index].Rest;
        }

        /// <summary>True while stack <paramref name="index"/> waits on its holder to be taken.</summary>
        public bool IsWaiting(int index)
        {
            return _stacks[index].Phase == Phase.Waiting;
        }

        /// <summary>The stable id of stack <paramref name="index"/> (indices shift as stacks leave).</summary>
        public int IdAt(int index)
        {
            return _stacks[index].Id;
        }

        /// <summary>The index of the stack with <paramref name="id"/>, or -1 once it is gone.</summary>
        public int IndexOf(int id)
        {
            for (int i = 0; i < _stacks.Count; i++)
            {
                if (_stacks[i].Id == id)
                {
                    return i;
                }
            }

            return -1;
        }

        public SalvageMaterial MaterialAt(int index)
        {
            return _stacks[index].Material;
        }

        public int UnitsAt(int index)
        {
            return _stacks[index].Units;
        }

        /// <summary>The held piece of stack <paramref name="index"/>, or null for a crate's remainder.</summary>
        public SalvagePiece PieceAt(int index)
        {
            return _stacks[index].Piece;
        }

        /// <summary>07 is home again (the dock's unload): the next full hold says so once more.</summary>
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
            var stack = new Stack
            {
                Id = _nextId++, Material = piece.Material, Units = piece.Units, Piece = piece, Rest = middle,
                Noun = _catalog.NameKey(piece.Material), Phase = Phase.Waiting,
            };
            _stacks.Add(stack);
            piece.Halo.Tint(_tuning.HeldHaloColor);
            if (announce)
            {
                Overflowed(piece.Material, piece.Units, middle);
            }
        }

        /// <summary>
        /// A crew crate opened with no room for all of it: the rest waits as a bundle in its open mouth.
        /// <paramref name="announce"/>: it was just opened, not loaded from a save.
        /// </summary>
        internal void HoldCrate(CrewCrate crate, SalvageMaterial material, int units, bool announce)
        {
            if (crate == null)
            {
                throw new ArgumentNullException(nameof(crate));
            }

            Vector3 mouth = crate.Root.TransformPoint(crate.LidRest);
            Stack stack = Spawn(material, units, mouth);
            stack.Crate = crate;
            stack.Rest = mouth;
            stack.Model.SetPositionAndRotation(mouth, crate.Root.rotation);
            if (announce)
            {
                Overflowed(material, units, mouth);
            }
        }

        /// <summary>The units still held in <paramref name="crate"/>'s mouth (0 when none).</summary>
        internal int HeldIn(CrewCrate crate)
        {
            for (int i = 0; i < _stacks.Count; i++)
            {
                if (_stacks[i].Crate == crate)
                {
                    return _stacks[i].Units;
                }
            }

            return 0;
        }

        public void Step(float now, float deltaTime)
        {
            Vector3 socket = _rig.CargoSocket.position;
            _glints.Begin(_view.Camera.transform.position, now);
            for (int i = _stacks.Count - 1; i >= 0; i--)
            {
                Stack stack = _stacks[i];
                switch (stack.Phase)
                {
                    case Phase.Waiting:
                        StepWaiting(i, stack, now);
                        break;
                    case Phase.Flying:
                        StepFlying(i, stack, socket, now);
                        break;
                    case Phase.Returning:
                        StepReturning(stack, now);
                        break;
                }
            }

            _glints.End();
            _stock.Hold.SetWaitingBundles(_stacks.Count);
        }

        /// <summary>
        /// The nearest held stack inside <paramref name="reach"/> whose material home still has room for, for Tilly to
        /// fetch; -1 when there is none.
        /// </summary>
        public int NearestToFetch(IStationReach reach, Vector3 from)
        {
            int best = -1;
            float bestSq = float.MaxValue;
            for (int i = 0; i < _stacks.Count; i++)
            {
                Stack stack = _stacks[i];
                if (stack.Phase != Phase.Waiting || !reach.IsInReach(stack.Rest) ||
                    _stock.Home.FreeUnits(stack.Material) < stack.Units)
                {
                    continue;
                }

                float distanceSq = (stack.Rest - from).sqrMagnitude;
                if (distanceSq < bestSq)
                {
                    bestSq = distanceSq;
                    best = i;
                }
            }

            return best;
        }

        /// <summary>
        /// Tilly unclips stack <paramref name="index"/>: a held piece folds into its bundle on the spot (it leaves its
        /// wreck), and from now on the bundle hangs where she carries it.
        /// </summary>
        public void Hook(int index)
        {
            Stack stack = _stacks[index];
            if (stack.Piece != null)
            {
                stack.Model = _salvage.Unclip(stack.Piece);
                stack.Scale = stack.Model.localScale;
            }

            stack.Phase = Phase.Carried;
        }

        /// <summary>Stack <paramref name="index"/> hangs at <paramref name="at"/> under Tilly.</summary>
        public void Carry(int index, Vector3 at, float now)
        {
            _stacks[index].Model.SetPositionAndRotation(at, Quaternion.Euler(0f, now * _friends.PartSpin, 0f));
        }

        /// <summary>
        /// Tilly sets stack <paramref name="index"/> down at home (<see cref="BundleFetched"/>): onto the shelves, and
        /// what they cannot hold into home's overflow crates. Nothing waits again.
        /// </summary>
        public void Deliver(int index)
        {
            Stack stack = _stacks[index];
            if (stack.Piece != null)
            {
                _salvage.Fetched(stack.Piece);
            }

            int left = _stock.Deliver(stack.Material, stack.Units);
            if (left > 0)
            {
                _stock.Home.AddOverflow(stack.Material, left);
            }

            _events.Publish(new BundleFetched(stack.Material, stack.Units));
            Remove(index);
            _save.SaveNow();
        }

        public void Dispose()
        {
            _glints.Dispose();
            for (int i = _stacks.Count - 1; i >= 0; i--)
            {
                Remove(i);
            }
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

        private Stack Spawn(SalvageMaterial material, int units, Vector3 at)
        {
            GameObject prefab = _models.Bundle(material);
            if (prefab == null)
            {
                throw new InvalidOperationException($"No bundle model for {material} (SalvageCatalog).");
            }

            Transform model = Object.Instantiate(prefab, at, Quaternion.identity, _parent).transform;
            model.name = "HeldBundle_" + material;
            SalvageSiteBuilder.SetLayer(model, Layers.Pickup);
            var stack = new Stack
            {
                Id = _nextId++, Material = material, Units = units, Model = model, Scale = model.localScale,
                Noun = _catalog.NameKey(material), Phase = Phase.Waiting,
            };
            _stacks.Add(stack);
            return stack;
        }

        /// <summary>
        /// A held stack breathes on its holder (a piece's amber halo, a bundle's slow swell in its crate's mouth) and
        /// glints; with room in the hold, a press of E takes it.
        /// </summary>
        private void StepWaiting(int index, Stack stack, float now)
        {
            float pulse = Ease.Hump(Mathf.Repeat(now / _tuning.BundlePulseSeconds + stack.Id * 0.37f, 1f));
            _glints.Add(stack.Rest, stack.Id);
            if (stack.Piece != null)
            {
                stack.Piece.Halo.Apply(_tuning.HeldHalo * (1f - _tuning.BundlePulse + _tuning.BundlePulse * pulse));
            }
            else
            {
                stack.Model.localScale = stack.Scale * (1f + _tuning.BundlePulse * pulse);
            }

            int room = _stock.Hold.FreeUnits(stack.Material);
            if (room <= 0 || (stack.Piece != null && room < stack.Units))
            {
                return;
            }

            // A held piece is taken from where it was cut: the cutting reach, not a pickup's.
            float reach = stack.Piece != null ? _salvage.Tuning.ReachRadius : _friends.PartReachRadius;
            if (!_pickups.Offer(Time.frameCount, _input.ExcavateHeld, PickupReach.Bundles, stack.Id, stack.Rest,
                    _rover.Position, reach, stack.Noun))
            {
                return;
            }

            if (stack.Piece != null)
            {
                _salvage.Release(stack.Piece);
                Remove(index);
                return;
            }

            stack.Phase = Phase.Flying;
            stack.Since = now;
            stack.Flight = PickupFlight.Duration(Vector3.Distance(stack.Rest, _rig.CargoSocket.position),
                _friends.PartFlightDuration, _friends.PartFlightPerMetre);
            PickupReach.Lifted(_events, stack.Rest, stack.Flight);
            _events.Publish(new FeelBeat(FeelKind.Collect, FeelPhase.Anticipate, stack.Rest, Vector3.up, 1f,
                stack.Flight, stack.Material));
        }

        /// <summary>
        /// A crate's bundle spirals into 07: what fits goes in; what does not glides back into the crate's mouth.
        /// </summary>
        private void StepFlying(int index, Stack stack, Vector3 socket, float now)
        {
            float progress = (now - stack.Since) / stack.Flight;
            if (progress < 1f)
            {
                stack.Model.SetPositionAndRotation(
                    PickupFlight.Evaluate(stack.Rest, socket, progress, stack.Id, 1f, _friends.PartFlightLift,
                        _friends.PartSpiralRadius, _friends.PartSpiralTurns),
                    Quaternion.Euler(0f, now * _friends.PartSpin, 0f));
                stack.Model.localScale = stack.Scale *
                                         Mathf.Lerp(1f, _friends.PartArrivalScale, Ease.InOutSine(progress));
                return;
            }

            int left = _stock.Collect(stack.Material, stack.Units);
            int taken = stack.Units - left;
            if (taken > 0)
            {
                _events.Publish(new FeelBeat(FeelKind.Collect, FeelPhase.Reward, socket, Vector3.up, 1f, 0f,
                    stack.Material));
                _events.Publish(new CargoPickupTaken(stack.Material, taken));
            }

            if (left > 0)
            {
                stack.Units = left;
                stack.Phase = Phase.Returning;
                stack.From = socket;
                stack.Since = now;
            }
            else
            {
                Remove(index);
            }

            _save.SaveNow();
        }

        /// <summary>What did not fit glides back into its crate's mouth and waits there again.</summary>
        private void StepReturning(Stack stack, float now)
        {
            float t = Mathf.Clamp01((now - stack.Since) / _tuning.BundleSettleSeconds);
            Vector3 at = Vector3.Lerp(stack.From, stack.Rest, Ease.OutCubic(t)) +
                         Vector3.up * (_friends.PartFlightLift * Ease.Hump(t));
            stack.Model.SetPositionAndRotation(at, stack.Crate.Root.rotation);
            stack.Model.localScale = stack.Scale;
            if (t >= 1f)
            {
                stack.Phase = Phase.Waiting;
            }
        }

        private void Remove(int index)
        {
            Stack stack = _stacks[index];
            if (stack.Model != null && stack.Piece == null)
            {
                Object.Destroy(stack.Model.gameObject);
            }

            _stacks.RemoveAt(index);
        }

        private sealed class Stack
        {
            public int Id;
            public SalvageMaterial Material;
            public int Units;
            public string Noun;
            public SalvagePiece Piece;
            public CrewCrate Crate;
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
