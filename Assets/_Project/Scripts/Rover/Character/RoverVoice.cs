using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover
{
    /// <summary>
    /// 07 decides when it speaks machine (docs/design/feel-pass.md §7.2) from the events it already hears, and asks
    /// Audio for the chirp (<see cref="RoverChirpRequested"/>) when <see cref="ChirpVoice"/> allows it:
    /// <list type="bullet">
    /// <item>the dock locks: Affirm, or Relieved when 07 arrived at or under the tuned share; the dock full:
    /// Happy;</item>
    /// <item>a friend's gift: Happy; a find decoded: Affirm; a
    /// friend repaired: Happy; a friend greeting 07: Greeting; a mast filling a nearly empty battery: Relieved;</item>
    /// <item>the battery's Low warning, or 07 being recovered: Worried; the battery empty: Tired;</item>
    /// <item>the first answer of each friend, site or relic while listening, and a new place: Curious;</item>
    /// <item>home's windows lighting as the base wakes: Happy; a component lifting free: Curious the first time
    /// for each part, else Affirm.</item>
    /// </list>
    /// Loaded state (restored events) never chirps.
    /// </summary>
    public sealed class RoverVoice : IDisposable
    {
        private const float Gentle = 0.5f;

        private readonly EventBus _events;
        private readonly ChirpSettings _settings;
        private readonly ChirpVoice _voice;
        private readonly Func<float> _clock;
        private readonly HashSet<string> _answered = new HashSet<string>(StringComparer.Ordinal);
        private readonly IDisposable[] _subscriptions;
        private float _charge = 1f;
        private bool _upgradedCutPending;
        private bool _listening;
        private bool _chirpedThisListen;

        public RoverVoice(EventBus events, ChirpSettings settings, Func<float> clock)
        {
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _voice = new ChirpVoice(settings);
            _subscriptions = new[]
            {
                events.Subscribe<FeelBeat>(OnFeelBeat),
                events.Subscribe<BatteryChanged>(OnBatteryChanged),
                events.Subscribe<BatteryWarned>(OnBatteryWarned),
                events.Subscribe<LowPowerChanged>(OnLowPowerChanged),
                events.Subscribe<RoverRecovering>(_ => Say(ChirpMood.Worried, Gentle)),
                events.Subscribe<ChargingChanged>(OnChargingChanged),
                events.Subscribe<CutterTierChanged>(changed => _upgradedCutPending |= !changed.Restored),
                events.Subscribe<FriendRepaired>(_ => Say(ChirpMood.Happy, Gentle)),
                events.Subscribe<CallReplyFailed>(_ => Say(ChirpMood.Curious, 1f)),
                events.Subscribe<FriendGreeted>(_ => Say(ChirpMood.Greeting, Gentle)),
                events.Subscribe<SonarPinged>(_ =>
                {
                    _listening = true;
                    _chirpedThisListen = false;
                }),
                events.Subscribe<FriendAnswered>(answer => FirstAnswer("friend." + answer.FriendId)),
                events.Subscribe<SiteAnswered>(answer => FirstAnswer(answer.SiteId)),
                events.Subscribe<RelicAnswered>(answer => FirstAnswer(answer.RelicId)),
                events.Subscribe<PlaceFirstApproached>(_ => Say(ChirpMood.Curious, Gentle)),
                events.Subscribe<RoverChirpSyllable>(OnSyllable),
                events.Subscribe<BasePowerChanged>(OnBasePowerChanged),
                events.Subscribe<ComponentFound>(OnComponentFound),
                events.Subscribe<FragmentScanned>(OnFragmentScanned),
                events.Subscribe<BlueprintCompleted>(_ => Say(ChirpMood.Proud, 1f)),
            };
        }

        /// <summary>07 meets its elder 06 (0.5.5 §1.7): one soft, low Curious chirp, quiet rather than sad.</summary>
        public void Elder(float intensity)
        {
            Say(ChirpMood.Curious, intensity);
        }

        /// <summary>
        /// A fragment learned (0.5.5 §1.2): Curious for new data, Affirm for a known blueprint's. The set's last new
        /// fragment stays quiet, since its blueprint's Proud follows at once and must never be lost.
        /// </summary>
        private void OnFragmentScanned(FragmentScanned scanned)
        {
            if (scanned.Known)
            {
                Say(ChirpMood.Affirm, Gentle);
            }
            else if (scanned.Index + 1 < scanned.Count)
            {
                Say(ChirpMood.Curious, Gentle);
            }
        }

        public void Dispose()
        {
            foreach (IDisposable subscription in _subscriptions)
            {
                subscription.Dispose();
            }
        }

        private void OnFeelBeat(FeelBeat beat)
        {
            if (beat.Kind == FeelKind.Build && beat.Phase == FeelPhase.Impact)
            {
                Say(ChirpMood.Happy, 1f);
                return;
            }

            if (beat.Kind == FeelKind.Cut && beat.Phase == FeelPhase.Impact && _upgradedCutPending)
            {
                _upgradedCutPending = false;
                Say(ChirpMood.Happy, 1f);
                return;
            }

            if (beat.Kind != FeelKind.Dock)
            {
                return;
            }

            if (beat.Phase == FeelPhase.Impact)
            {
                float below = _settings.RelievedBelow;
                if (_charge <= below)
                {
                    Say(ChirpMood.Relieved, below > 0f ? 1f - _charge / below : 1f);
                }
                else
                {
                    Say(ChirpMood.Affirm, Gentle);
                }
            }
            else if (beat.Phase == FeelPhase.Reward)
            {
                Say(ChirpMood.Happy, 1f);
            }
        }

        /// <summary>A mast starts filling a nearly empty battery: phew.</summary>
        private void OnChargingChanged(ChargingChanged charging)
        {
            float below = _settings.RelievedBelow;
            if (charging.Charging && charging.Charger == ChargerKind.Mast && _charge <= below)
            {
                Say(ChirpMood.Relieved, below > 0f ? 1f - _charge / below : 1f);
            }
        }

        private void OnBatteryChanged(BatteryChanged battery)
        {
            _charge = battery.Cells > 0 ? (float)battery.FullCells / battery.Cells : 1f;
        }

        private void OnBatteryWarned(BatteryWarned warned)
        {
            if (warned.Warning == BatteryWarning.Low)
            {
                Say(ChirpMood.Worried, Gentle);
            }
        }

        private void OnLowPowerChanged(LowPowerChanged power)
        {
            if (power.LowPower && !power.Restored)
            {
                Say(ChirpMood.Tired, 1f);
            }
        }

        private void FirstAnswer(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            bool isNew = _answered.Add(id);
            if (_listening)
            {
                if (!_chirpedThisListen)
                {
                    _chirpedThisListen = true;
                    Say(ChirpMood.Curious, Gentle);
                }

                return;
            }

            if (isNew)
            {
                Say(ChirpMood.Curious, Gentle);
            }
        }

        private void OnBasePowerChanged(BasePowerChanged power)
        {
            if (!power.Restored && power.Stage != BasePowerStage.Asleep)
            {
                Say(ChirpMood.Happy, Gentle);
            }
        }

        private void OnComponentFound(ComponentFound found)
        {
            if (found.Restored)
            {
                return;
            }

            bool first = _answered.Add("part." + found.PartId);
            Say(first ? ChirpMood.Curious : ChirpMood.Affirm, Gentle);
        }

        private void OnSyllable(RoverChirpSyllable syllable)
        {
            _voice.Syllable(syllable.Index, syllable.Count, syllable.Seconds, _clock());
        }

        private void Say(ChirpMood mood, float intensity)
        {
            if (_voice.TryChirp(mood, _clock()))
            {
                _events.Publish(new RoverChirpRequested(mood, Mathf.Clamp01(intensity)));
            }
        }
    }
}
