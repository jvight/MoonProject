using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The press that picks a waiting part up (tower parts, friends' parts, relay spares, blueprint tubes, Ro's tapes).
    /// Each frame the fields offer their resting parts; the nearest one within reach is the candidate, shown as a world
    /// prompt with the key and the verb (<see cref="InteractionKind.PickUp"/>, the part's name from
    /// <see cref="TryGet"/>), and a fresh Excavate press takes it: only then does it lift onto 07's back. Fields step
    /// in different components, so a frame's offers are settled on the next frame's first offer. Allocates nothing.
    /// </summary>
    public sealed class PickupReach
    {
        /// <summary>Owner id of the tower's parts (<see cref="TowerPartField"/>).</summary>
        public const int TowerParts = 0;

        /// <summary>Owner id of the blueprint tubes on the ground (<see cref="GroundTubes"/>).</summary>
        public const int Tubes = 1;

        /// <summary>Owner id of the friends' parts (index = friend x <see cref="FriendProgress.MaxParts"/> + part).</summary>
        public const int Friends = 2;

        /// <summary>Owner id of the relay masts' spare parts (<see cref="RelayField"/>).</summary>
        public const int Relays = 3;

        private const string NounPrefix = "pickup.";

        private int _frame = int.MinValue;
        private bool _wasHeld;
        private bool _pressed;
        private float _bestSq;
        private bool _nextHas;
        private Vector3 _nextPosition;
        private string _nextNoun;
        private int _nextOwner;
        private int _nextIndex;
        private int _owner;
        private int _index;
        private bool _has;
        private Vector3 _position;
        private string _noun;

        /// <summary>
        /// Offers the part <paramref name="index"/> of field <paramref name="owner"/> resting at <paramref name="at"/>.
        /// True when it is the candidate and the player has just pressed: the field lifts it now.
        /// </summary>
        public bool Offer(int frame, bool held, int owner, int index, Vector3 at, Vector3 rover, float reach,
            string nounKey)
        {
            Roll(frame, held);
            float distanceSq = (at - rover).sqrMagnitude;
            if (distanceSq > reach * reach)
            {
                return false;
            }

            if (!_nextHas || distanceSq < _bestSq)
            {
                _nextHas = true;
                _bestSq = distanceSq;
                _nextPosition = at;
                _nextNoun = nounKey;
                _nextOwner = owner;
                _nextIndex = index;
            }

            return _pressed && _has && _owner == owner && _index == index;
        }

        /// <summary>
        /// The candidate as of <paramref name="frame"/> (for the prompt): where it rests and its name key, the noun
        /// after the verb. False when nothing waits within reach.
        /// </summary>
        public bool TryGet(int frame, out Vector3 at, out string nounKey)
        {
            bool has = frame == _frame ? _has : frame == _frame + 1 && _nextHas;
            at = frame == _frame ? _position : _nextPosition;
            nounKey = has ? frame == _frame ? _noun : _nextNoun : null;
            return has;
        }

        /// <summary>Settles the last frame's offers on the first offer of a frame.</summary>
        private void Roll(int frame, bool held)
        {
            if (frame == _frame)
            {
                return;
            }

            bool consecutive = frame == _frame + 1;
            _has = consecutive && _nextHas;
            _position = _nextPosition;
            _noun = _nextNoun;
            _owner = _nextOwner;
            _index = _nextIndex;
            _pressed = held && !_wasHeld;
            _wasHeld = held;
            _nextHas = false;
            _frame = frame;
        }

        /// <summary>The name key of a part for the prompt's noun, lower-case (built once, never per frame).</summary>
        public static string NounKey(string partId)
        {
            return NounPrefix + partId.ToLowerInvariant();
        }

        /// <summary>The part leaves the ground for 07's back: the pickup's Act at the part, lasting its flight.</summary>
        public static void Lifted(EventBus events, Vector3 at, float flightSeconds)
        {
            events.Publish(new FeelBeat(FeelKind.Collect, FeelPhase.Act, at, Vector3.up, 1f, flightSeconds));
        }

        /// <summary>The part settles on 07's back with a clunk: the pickup's Impact at the cargo socket.</summary>
        public static void Landed(EventBus events, Vector3 socket)
        {
            events.Publish(new FeelBeat(FeelKind.Collect, FeelPhase.Impact, socket, Vector3.up, 1f, 0f));
        }
    }
}
