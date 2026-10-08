using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The radio-hop (<see cref="IRadioHop"/>). Parked on a lit node's pad and not towing, Interact opens the list of
    /// the other lit nodes (home first, then masts) and 07 glances toward the highlighted one; a tap highlights the
    /// next, holding Interact hops there, driving off closes the list. A hop holds 07 still, publishes
    /// <see cref="RadioHopStarted"/>, eases the view to dark (<see cref="HopSequence"/>), places 07 on the target pad
    /// facing out through Core's <see cref="IRoverPlacement"/> at the darkest moment, publishes
    /// <see cref="RadioHopFinished"/> and eases the view back in. Only lit nodes are targets, so 07 never hops into a
    /// region it has not reached (a mast beyond a gate is lit only once 07 has been there to restore it). Without a
    /// registered placement a hop fails loudly and does not start. <see cref="RadioHopListChanged"/> tells the camera
    /// when the list opens and closes. Allocation-free per frame.
    /// </summary>
    internal sealed class RadioHop : IRadioHop
    {
        private readonly EventBus _events;
        private readonly StationReach _reach;
        private readonly RelayTuning _tuning;
        private readonly HopSequence _sequence;
        private readonly IRoverState _rover;
        private readonly IRoverRig _rig;
        private readonly IRoverPlacement _placement;
        private readonly ITetherAim _tether;
        private readonly Quaternion[] _facings;
        private readonly string[] _labels;
        private readonly int[] _choices;
        private int _from = -1;
        private int _to = -1;
        private float _time;
        private float _hold;
        private bool _wasHeld;
        private bool _awaitRelease;
        private bool _placed;
        private bool _finished;
        private bool _gazing;

        /// <param name="placement">Core's rover placement, or null while no domain registers it.</param>
        /// <param name="facings">Per node, how 07 faces when it arrives there (out, away from home).</param>
        /// <param name="labels">Per node, the localization key of its name.</param>
        public RadioHop(EventBus events, StationReach reach, RelayTuning tuning, IRoverState rover, IRoverRig rig,
            IRoverPlacement placement, ITetherAim tether, Quaternion[] facings, string[] labels)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _reach = reach ?? throw new ArgumentNullException(nameof(reach));
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _rover = rover ?? throw new ArgumentNullException(nameof(rover));
            _rig = rig ?? throw new ArgumentNullException(nameof(rig));
            _placement = placement;
            _tether = tether ?? throw new ArgumentNullException(nameof(tether));
            _facings = facings ?? throw new ArgumentNullException(nameof(facings));
            _labels = labels ?? throw new ArgumentNullException(nameof(labels));
            if (facings.Length != reach.NodeCount || labels.Length != reach.NodeCount)
            {
                throw new ArgumentException("Every node needs a facing and a label.");
            }

            _sequence = HopSequence.For(tuning);
            _choices = new int[reach.NodeCount];
        }

        public RadioHopPhase Phase { get; private set; }

        public bool CanOpen => Phase == RadioHopPhase.Closed && Here >= 0 && _reach.LitCount > 1;

        public int Here { get; private set; } = -1;

        public int ChoiceCount { get; private set; }

        public int Selected { get; private set; }

        public float ConfirmHold => Phase == RadioHopPhase.Choosing && !_awaitRelease
            ? Mathf.Clamp01(_hold / _tuning.HopConfirmHold)
            : 0f;

        public float Fade => Hopping ? _sequence.Fade(_time) : 0f;

        public float Progress => Hopping ? _sequence.Progress(_time) : 0f;

        /// <summary>The node a hop in progress goes to, or -1.</summary>
        public int Target => Hopping ? _to : -1;

        private bool Hopping => Phase >= RadioHopPhase.Leaving;

        public int ChoiceNode(int choice)
        {
            return _choices[choice];
        }

        public string ChoiceLabelKey(int choice)
        {
            return _labels[_choices[choice]];
        }

        public bool Open()
        {
            if (!CanOpen)
            {
                return false;
            }

            ChoiceCount = 0;
            for (int node = 0; node < _reach.NodeCount; node++)
            {
                if (node != Here && _reach.IsLit(node))
                {
                    _choices[ChoiceCount++] = node;
                }
            }

            Selected = 0;
            _hold = 0f;
            Phase = RadioHopPhase.Choosing;
            _events.Publish(new RadioHopListChanged(true));
            return true;
        }

        public void Next()
        {
            if (Phase == RadioHopPhase.Choosing)
            {
                Selected = (Selected + 1) % ChoiceCount;
            }
        }

        public void Previous()
        {
            if (Phase == RadioHopPhase.Choosing)
            {
                Selected = (Selected + ChoiceCount - 1) % ChoiceCount;
            }
        }

        public bool Confirm()
        {
            if (Phase != RadioHopPhase.Choosing)
            {
                return false;
            }

            if (_placement == null)
            {
                Debug.LogError($"{nameof(RadioHop)}: no {nameof(IRoverPlacement)} is registered (the Rover domain " +
                               "registers it, docs/features/M3-06), so 07 cannot be moved: the hop does not start.");
                Cancel();
                return false;
            }

            _from = Here;
            _to = _choices[Selected];
            _time = 0f;
            _placed = false;
            _finished = false;
            Phase = RadioHopPhase.Leaving;
            _events.Publish(new RadioHopListChanged(false));
            _rig.SetHoldStill(this, true);
            if (_gazing)
            {
                _rig.ClearGazeTarget(this);
                _gazing = false;
            }

            _events.Publish(new RadioHopStarted(_reach.Id(_from), _reach.Id(_to)));
            return true;
        }

        public void Cancel()
        {
            if (Phase == RadioHopPhase.Choosing)
            {
                Phase = RadioHopPhase.Closed;
                _hold = 0f;
                _events.Publish(new RadioHopListChanged(false));
            }
        }

        /// <summary>
        /// One frame: steps a hop in progress, or follows 07's pad and Interact (<paramref name="interact"/> held,
        /// <paramref name="drive"/> the player's stick) for the list.
        /// </summary>
        public void Step(float deltaTime, bool interact, Vector2 drive)
        {
            if (Hopping)
            {
                StepHop(deltaTime);
                return;
            }

            Here = FindHere();
            if (Phase == RadioHopPhase.Choosing &&
                (Here < 0 || drive.magnitude > _tuning.HopCancelDrive || _reach.LitCount <= 1))
            {
                Cancel();
            }

            StepInput(interact, deltaTime);
            StepGaze();
        }

        /// <summary>Lets go of 07 (the field is disabled or destroyed mid-hop).</summary>
        public void Release()
        {
            _rig.SetHoldStill(this, false);
            if (_gazing)
            {
                _rig.ClearGazeTarget(this);
                _gazing = false;
            }
        }

        private int FindHere()
        {
            if (_tether.State == TetherAimState.Towing || _rover.Speed > _tuning.HopMaxSpeed)
            {
                return -1;
            }

            float padSq = _tuning.PadRadius * _tuning.PadRadius;
            for (int node = 0; node < _reach.NodeCount; node++)
            {
                if (_reach.IsLit(node) &&
                    SurfaceRules.HorizontalDistanceSquared(_rover.Position, _reach.Position(node)) <= padSq)
                {
                    return node;
                }
            }

            return -1;
        }

        /// <summary>Press opens; with the list open a tap picks the next node and a hold hops.</summary>
        private void StepInput(bool interact, float deltaTime)
        {
            bool pressed = interact && !_wasHeld;
            bool released = !interact && _wasHeld;
            _wasHeld = interact;
            if (Phase == RadioHopPhase.Closed)
            {
                if (pressed && Open())
                {
                    _awaitRelease = true;
                }

                return;
            }

            if (Phase != RadioHopPhase.Choosing)
            {
                return;
            }

            if (released)
            {
                if (!_awaitRelease && _hold < _tuning.HopConfirmHold)
                {
                    Next();
                }

                _awaitRelease = false;
                _hold = 0f;
                return;
            }

            if (interact && !_awaitRelease)
            {
                _hold += deltaTime;
                if (_hold >= _tuning.HopConfirmHold)
                {
                    _hold = 0f;
                    _awaitRelease = true;
                    Confirm();
                }
            }
        }

        private void StepHop(float deltaTime)
        {
            _time += deltaTime;
            if (!_placed && _time >= _sequence.PlaceAt)
            {
                _placed = true;
                Phase = RadioHopPhase.Dark;
                _placement.PlaceAt(_reach.Position(_to), _facings[_to]);
            }

            if (!_finished && _time >= _sequence.FinishAt)
            {
                _finished = true;
                Phase = RadioHopPhase.Arriving;
                _events.Publish(new RadioHopFinished(_reach.Id(_to)));
            }

            if (_sequence.Done(_time))
            {
                Phase = RadioHopPhase.Closed;
                Here = _to;
                _rig.SetHoldStill(this, false);
            }
        }

        /// <summary>With the list open, 07 glances toward the highlighted node.</summary>
        private void StepGaze()
        {
            if (Phase == RadioHopPhase.Choosing)
            {
                _rig.SetGazeTarget(this, _reach.Position(_choices[Selected]), GazePriorities.Interest);
                _gazing = true;
            }
            else if (_gazing)
            {
                _rig.ClearGazeTarget(this);
                _gazing = false;
            }
        }
    }
}
