using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Waking Lumen Station step by step (docs/features/M3-16). Each frame it notes whether 07 has woken and gone out,
    /// greets 07 at each place the first time (<see cref="PlaceApproaches"/>), lets the sleeping bay, lift and memory
    /// shelf answer the sonar with "not yet" (<see cref="DormantAnswers"/>) and keeps the soft "next" line on the
    /// first step not yet done (<see cref="StepFlow"/>). Saved as "gameplay.progression".
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BaseProgression : MonoBehaviour
    {
        private static readonly string[] Places =
        {
            ProgressionKeys.PlaceDock, ProgressionKeys.PlaceLander, ProgressionKeys.PlaceLadder,
            ProgressionKeys.PlaceLift, ProgressionKeys.PlaceTower, ProgressionKeys.PlaceShelf, ProgressionKeys.PlaceBay,
            ProgressionKeys.PlaceBoard, ProgressionKeys.PlaceRack,
        };

        private const int TowerSpot = 4;
        private const int StepTower = 2;
        private const int StepRelic = 4;
        private const int StepBay = 5;
        private const int StepLeap = 6;
        private const int StepLift = 8;

        [Tooltip("The wake-up's tuning (Assets/_Project/Data/Tuning/Gameplay/ProgressionTuning.asset).")]
        [SerializeField] private ProgressionTuning _tuning;

        [Tooltip("Where each place stands, in the order of the place ids (dock, lander, ladder, lift, tower, shelf, " +
                 "bay, board, rack).")]
        [SerializeField] private Transform[] _places = Array.Empty<Transform>();

        [Tooltip("The crew's lift platform: it answers the sonar until power reaches it.")]
        [SerializeField] private Transform _lift;

        private IRoverState _rover;
        private PlaceApproaches _approaches;
        private DormantAnswers _answers;
        private StepFlow _steps;
        private IDisposable _salvage;
        private Vector3 _dock;
        private bool _woke;
        private bool _salvaged;
        private bool _initialized;

        public ProgressionTuning Tuning => _tuning;

        /// <summary>The current "next" line key (empty once every step is done).</summary>
        public string NextStep => _steps != null ? _steps.Current : string.Empty;

        internal void Wire(ProgressionTuning tuning, Transform[] places, Transform lift)
        {
            _tuning = tuning;
            _places = places;
            _lift = lift;
        }

        internal bool Initialize(GameplayServices services, BasePower power, HomeBase home, Workshop bay,
            SonarTuning sonar, FriendField friends, IRelayStatus relays, UpgradeService upgrades)
        {
            string problem = WiringProblem();
            Friend signal = problem == null ? FindFriend(friends, _tuning.SignalFriend) : null;
            Friend leap = problem == null ? FindFriend(friends, _tuning.LeapFriend) : null;
            problem = problem
                ?? (signal == null ? $"no friend '{_tuning.SignalFriend}' for {ProgressionKeys.NextSignal}."
                    : leap == null ? $"no friend '{_tuning.LeapFriend}' for {ProgressionKeys.NextLeap}."
                    : upgrades.Find(_tuning.FirstKit) == null
                        ? $"no kit '{_tuning.FirstKit}' for {ProgressionKeys.NextBay}."
                        : null);
            if (problem != null)
            {
                Debug.LogError($"{nameof(BaseProgression)}: {problem}", this);
                enabled = false;
                return false;
            }

            _rover = services.Rover;
            _dock = _places[0].position;
            _approaches = new PlaceApproaches(services.Events, _tuning.PlaceLineGap);
            _steps = new StepFlow(services.Events);
            for (int i = 0; i < Places.Length; i++)
            {
                _approaches.Add(Places[i], _places[i].position, _tuning.RadiusOf(Places[i]),
                    Speaks(Places[i], bay, leap));
            }

            _answers = new DormantAnswers(services.Events, sonar);
            Func<bool> homePowered = () => power.HasReached(BasePowerStage.Home);
            _answers.Add(ProgressionKeys.NotYetTower, _places[TowerSpot].position, () => !homePowered());
            _answers.Add(ProgressionKeys.NotYetBay, bay.transform.position, () => homePowered() && !bay.Awake);
            _answers.Add(ProgressionKeys.NotYetLift, _lift.position,
                () => homePowered() && !power.HasReached(BasePowerStage.Lift));
            _answers.Add(ProgressionKeys.NotYetShelf, home.ShelfPosition,
                () => homePowered() && home.DisplayedCount == 0);
            _salvage = services.Events.Subscribe<MaterialSalvaged>(_ => _salvaged = true);

            _steps.Add(ProgressionKeys.NextWake, () => _woke);
            _steps.Add(ProgressionKeys.NextSalvage, () => _salvaged || power.HasReached(BasePowerStage.Home));
            _steps.Add(ProgressionKeys.NextTower, () => power.HasReached(BasePowerStage.Home));
            _steps.Add(ProgressionKeys.NextSignal, () => signal.Progress.State == FriendState.Awake);
            _steps.Add(ProgressionKeys.NextRelic, () => home.DisplayedCount > 0);
            _steps.Add(ProgressionKeys.NextBay, () => upgrades.LevelOf(_tuning.FirstKit) > 0);
            _steps.Add(ProgressionKeys.NextLeap, () => leap.Progress.State == FriendState.Awake);
            _steps.Add(ProgressionKeys.NextRelay, () => relays.LitMasts > 0);
            _steps.Add(ProgressionKeys.NextLift, () => power.HasReached(BasePowerStage.Lift));
            _initialized = true;
            return true;
        }

        internal ProgressionSaveData Capture()
        {
            return new ProgressionSaveData { woke = _woke, salvaged = _salvaged, places = _approaches.Capture() };
        }

        internal void Restore(ProgressionSaveData data)
        {
            _woke = data.woke;
            _salvaged = data.salvaged;
            _approaches.Restore(data.places);
        }

        private void Update()
        {
            if (!_initialized)
            {
                return;
            }

            float now = Time.time;
            Vector3 rover = _rover.Position;
            _woke |= SurfaceRules.HorizontalDistance(rover, _dock) > _tuning.WakeDistance;
            _approaches.Tick(rover, now);
            _answers.Tick(now);
            _steps.Tick();
        }

        /// <summary>
        /// Only the next thing speaks (M3-16 P4): a place greets 07 once the steps have come to it, the step it
        /// belongs to (0 wake: the dock; 2 tower: the lander and the tower; 4 relic: the shelf; 5 bay: the bay and,
        /// its shutter up, Kenji's board; 6 leap: Bell's rack, once she is home; 8 lift: the ladder and the lift).
        /// </summary>
        private Func<bool> Speaks(string placeId, Workshop bay, Friend bell)
        {
            switch (placeId)
            {
                case ProgressionKeys.PlaceDock: return null;
                case ProgressionKeys.PlaceLander:
                case ProgressionKeys.PlaceTower: return () => _steps.CurrentIndex >= StepTower;
                case ProgressionKeys.PlaceShelf: return () => _steps.CurrentIndex >= StepRelic;
                case ProgressionKeys.PlaceBay: return () => _steps.CurrentIndex >= StepBay;
                case ProgressionKeys.PlaceBoard: return () => _steps.CurrentIndex >= StepBay && bay.Awake;
                case ProgressionKeys.PlaceRack:
                    return () => _steps.CurrentIndex >= StepLeap && bell.Progress.State == FriendState.Awake;
                default: return () => _steps.CurrentIndex >= StepLift;
            }
        }

        private static Friend FindFriend(FriendField friends, string id)
        {
            foreach (Friend friend in friends.Friends)
            {
                if (friend.Id == id)
                {
                    return friend;
                }
            }

            return null;
        }

        private string WiringProblem()
        {
            if (_tuning == null)
            {
                return "ProgressionTuning is not assigned.";
            }

            if (_places == null || _places.Length != Places.Length)
            {
                return $"expects {Places.Length} place spots ({string.Join(", ", Places)}).";
            }

            for (int i = 0; i < _places.Length; i++)
            {
                if (_places[i] == null)
                {
                    return $"the spot of {Places[i]} is not assigned.";
                }
            }

            return _lift == null ? "the lift platform is not assigned." : null;
        }

        private void OnDestroy()
        {
            _salvage?.Dispose();
            _answers?.Dispose();
        }
    }
}
