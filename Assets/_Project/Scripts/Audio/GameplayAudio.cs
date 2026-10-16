using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio
{
    /// <summary>
    /// Gives every gameplay event its sound: sonar ping, relic answers (each relic on its own note, softer and darker
    /// with distance), salvage sites answering a ping (each site on its own note, warmer while it still holds a crew
    /// relic, softer and darker with distance), tether pluck / hum (follows the beam emitter while attached) / release
    /// or sighing snap, excavation rumble while the beam lifts plus the surfacing sparkle, the shelf "placed" cue and
    /// the upgrade sounds (the tower's arpeggio; for workshop upgrades, ids starting "rover.", the workbench's sparks,
    /// rattle and cadence, plus the coils' clunk-sproing when the Hover-Jump is bought), a cassette's click and
    /// spin, a crew log cache's tin and paper, and Bell's signals: a warm, very soft shimmer from the pillar when she
    /// picks something (mostly flat, a hint of direction, so even a far pillar is heard as distant rather than lost)
    /// and a resolved chime when it is found. Loops fade with <see cref="LoopFader"/> and stop when silent.
    /// Initialised by <see cref="AudioDirector"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameplayAudio : MonoBehaviour
    {
        private const int SubscriptionCount = 19;
        private const float WeldLoopFadeIn = 0.08f;
        private const float WeldLoopFadeOut = 0.12f;
        private const string SiteRelicSuffix = "_relic";
        private const string MicroLabelPrefix = "micro_";

        /// <summary>Upgrades sold at Kenji's workbench (rover abilities) use this id prefix.</summary>
        private const string WorkshopUpgradePrefix = "rover.";

        /// <summary>The Hover-Jump upgrade: buying it pops the spring coils in under 07.</summary>
        private const string HoverJumpUpgradeId = "rover.hover_jump";

        [Tooltip("Assets/_Project/Data/Audio/GameplayAudioTuning.asset.")]
        [SerializeField] private GameplayAudioTuning _tuning;

        private readonly IDisposable[] _subscriptions = new IDisposable[SubscriptionCount];
        private readonly LoopFader _tetherFader = new LoopFader();
        private readonly LoopFader _rumbleFader = new LoopFader();
        private readonly LoopFader _weldFader = new LoopFader();
        private readonly Dictionary<string, int> _relicVoices = new Dictionary<string, int>(StringComparer.Ordinal);

        // Site answers by site id ("site.depot", "micro.mule"): the plain voice and the warmer one while a relic waits
        // there (the lone derelicts have only the plain one).
        private readonly Dictionary<string, int> _siteVoices = new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _siteRelicVoices =
            new Dictionary<string, int>(StringComparer.Ordinal);
        private AudioDirector _director;
        private Transform _tetherOrigin;
        private CueHandle _sonarPing;
        private CueHandle _relicAnswer;
        private CueHandle _siteAnswer;
        private CueHandle _tetherAttach;
        private CueHandle _tetherRelease;
        private CueHandle _tetherSnap;
        private CueHandle _surfacingSparkle;
        private CueHandle _relicPlaced;
        private CueHandle _upgradeArpeggio;
        private CueHandle _workbenchUpgrade;
        private CueHandle _coilPop;
        private CueHandle _cassettePickup;
        private CueHandle _crewLogFound;
        private CueHandle _signalPick;
        private CueHandle _signalFound;
        private CueHandle _buildGhost;
        private CueHandle _buildSnap;
        private CueHandle _buildInvalid;
        private CueHandle _buildPlace;
        private CueHandle _buildStageComplete;
        private CueHandle _buildStructureComplete;
        private CueHandle _buildDeconstruct;
        private IRoverState _rover;
        private AudioSource _tetherHum;
        private AudioSource _signal;
        private AudioSource _rumble;
        private AudioSource _weldLoop;
        private float _tetherHumCueVolume;
        private float _rumbleCueVolume;
        private float _weldLoopCueVolume;

        /// <summary>True while the tether hum can be heard (including its fade-out).</summary>
        public bool TetherHumAudible => _tetherFader.IsAudible;

        /// <summary>True while the excavation rumble can be heard (including its fade-out).</summary>
        public bool RumbleAudible => _rumbleFader.IsAudible;

        /// <summary>True while the build weld loop can be heard (including its fade-out).</summary>
        public bool BuildWeldAudible => _weldFader.IsAudible;

        internal AudioSource TetherHumSource => _tetherHum;

        internal AudioSource RumbleSource => _rumble;

        internal AudioSource SignalSource => _signal;

        internal AudioSource BuildWeldSource => _weldLoop;

        internal void Initialize(GameContext context, AudioDirector director)
        {
            if (_tuning == null)
            {
                Debug.LogError($"{nameof(GameplayAudio)}: {nameof(_tuning)} is not assigned.", this);
                enabled = false;
                return;
            }

            _director = director;
            IRoverRig rig = context.Get<IRoverRig>();
            _tetherOrigin = rig.WinchOrigin != null ? rig.WinchOrigin : rig.TetherOrigin;
            if (_tetherOrigin == null)
            {
                Debug.LogError($"{nameof(GameplayAudio)}: {nameof(IRoverRig)}.WinchOrigin is null.", this);
                enabled = false;
                return;
            }

            _sonarPing = director.Resolve(AudioCueIds.SonarPing);
            _relicAnswer = director.Resolve(AudioCueIds.RelicAnswer);
            _siteAnswer = director.Resolve(AudioCueIds.SiteAnswer);
            _tetherAttach = director.Resolve(AudioCueIds.TetherAttach);
            _tetherRelease = director.Resolve(AudioCueIds.TetherRelease);
            _tetherSnap = director.Resolve(AudioCueIds.TetherSnap);
            _surfacingSparkle = director.Resolve(AudioCueIds.SurfacingSparkle);
            _relicPlaced = director.Resolve(AudioCueIds.RelicPlaced);
            _upgradeArpeggio = director.Resolve(AudioCueIds.UpgradeArpeggio);
            _workbenchUpgrade = director.Resolve(AudioCueIds.WorkbenchUpgrade);
            _coilPop = director.Resolve(AudioCueIds.CoilPop);
            _cassettePickup = director.Resolve(AudioCueIds.CassettePickup);
            _crewLogFound = director.Resolve(AudioCueIds.CrewLogFound);
            _signalPick = director.Resolve(AudioCueIds.BellSignalPick);
            _signalFound = director.Resolve(AudioCueIds.BellSignalFound);
            _buildGhost = director.Resolve(AudioCueIds.BuildGhost);
            _buildSnap = director.Resolve(AudioCueIds.BuildSnap);
            _buildInvalid = director.Resolve(AudioCueIds.BuildInvalid);
            _buildPlace = director.Resolve(AudioCueIds.BuildPlace);
            _buildStageComplete = director.Resolve(AudioCueIds.BuildStageComplete);
            _buildStructureComplete = director.Resolve(AudioCueIds.BuildStructureComplete);
            _buildDeconstruct = director.Resolve(AudioCueIds.BuildDeconstruct);
            _rover = context.Get<IRoverState>();
            CueHandle hum = director.Resolve(AudioCueIds.TetherHum);
            CueHandle rumble = director.Resolve(AudioCueIds.ExcavationRumble);
            CueHandle weldLoop = director.Resolve(AudioCueIds.BuildWeldLoop);
            if (!(_sonarPing.IsValid && _relicAnswer.IsValid && _siteAnswer.IsValid && _tetherAttach.IsValid &&
                  _tetherRelease.IsValid && _tetherSnap.IsValid && _surfacingSparkle.IsValid && _relicPlaced.IsValid &&
                  _upgradeArpeggio.IsValid && _workbenchUpgrade.IsValid && _coilPop.IsValid && hum.IsValid &&
                  rumble.IsValid && _cassettePickup.IsValid && _crewLogFound.IsValid && _signalPick.IsValid &&
                  _signalFound.IsValid && _buildGhost.IsValid && _buildSnap.IsValid && _buildInvalid.IsValid &&
                  _buildPlace.IsValid && weldLoop.IsValid && _buildStageComplete.IsValid &&
                  _buildStructureComplete.IsValid && _buildDeconstruct.IsValid))
            {
                enabled = false;
                return;
            }

            AudioCue answers = director.Library.GetCue(_relicAnswer);
            for (int i = 0; i < answers.ClipCount; i++)
            {
                _relicVoices[answers.GetVariantLabel(i)] = i;
            }

            AudioCue sites = director.Library.GetCue(_siteAnswer);
            for (int i = 0; i < sites.ClipCount; i++)
            {
                string label = sites.GetVariantLabel(i);
                if (label.EndsWith(SiteRelicSuffix, StringComparison.Ordinal))
                {
                    _siteRelicVoices[WorldAnchorIds.SitePrefix + label.Substring(0, label.Length -
                                                                                  SiteRelicSuffix.Length)] = i;
                }
                else if (label.StartsWith(MicroLabelPrefix, StringComparison.Ordinal))
                {
                    // A lone derelict (WorldAnchorIds.MicroPrefix + name): its label spells the id with an underscore.
                    _siteVoices[WorldAnchorIds.MicroPrefix + label.Substring(MicroLabelPrefix.Length)] = i;
                }
                else
                {
                    _siteVoices[WorldAnchorIds.SitePrefix + label] = i;
                }
            }

            _tetherHumCueVolume = director.Library.GetCue(hum).VolumeMax;
            _rumbleCueVolume = director.Library.GetCue(rumble).VolumeMax;
            _weldLoopCueVolume = director.Library.GetCue(weldLoop).VolumeMax;
            _tetherHum = director.CreateLoopSource(transform, "TetherHum", hum, _tuning.TetherHumSpatialBlend);
            _rumble = director.CreateLoopSource(transform, "ExcavationRumble", rumble, 1f);
            _weldLoop = director.CreateLoopSource(transform, "BuildWeldLoop", weldLoop, 1f);
            _signal = director.CreateLoopSource(transform, "BellSignal", default, _tuning.SignalSpatialBlend);
            _signal.loop = false;

            EventBus events = context.Events;
            _subscriptions[0] = events.Subscribe<SonarPinged>(OnSonarPinged);
            _subscriptions[1] = events.Subscribe<RelicAnswered>(OnRelicAnswered);
            _subscriptions[2] = events.Subscribe<SiteAnswered>(OnSiteAnswered);
            _subscriptions[3] = events.Subscribe<TetherAttached>(OnTetherAttached);
            _subscriptions[4] = events.Subscribe<TetherReleased>(OnTetherReleased);
            _subscriptions[5] = events.Subscribe<ExcavationStarted>(OnExcavationStarted);
            _subscriptions[6] = events.Subscribe<ExcavationStopped>(OnExcavationStopped);
            _subscriptions[7] = events.Subscribe<RelicSurfaced>(OnRelicSurfaced);
            _subscriptions[8] = events.Subscribe<RelicDeposited>(OnRelicDeposited);
            _subscriptions[9] = events.Subscribe<UpgradePurchased>(OnUpgradePurchased);
            _subscriptions[10] = events.Subscribe<CassetteCollected>(OnCassetteCollected);
            _subscriptions[11] = events.Subscribe<CrewLogFound>(OnCrewLogFound);
            _subscriptions[12] = events.Subscribe<BellSignalPicked>(OnBellSignalPicked);
            _subscriptions[13] = events.Subscribe<BellSignalFound>(OnBellSignalFound);
            _subscriptions[14] = events.Subscribe<StructurePlaced>(OnStructurePlaced);
            _subscriptions[15] = events.Subscribe<StructureBuilt>(OnStructureBuilt);
            _subscriptions[16] = events.Subscribe<StructureRemoved>(OnStructureRemoved);
            _subscriptions[17] = events.Subscribe<BuildCue>(OnBuildCue);
            _subscriptions[18] = events.Subscribe<BuildWeldStateChanged>(OnBuildWeldStateChanged);
        }

        internal void Wire(GameplayAudioTuning tuning)
        {
            _tuning = tuning;
        }

        private void Update()
        {
            if (_director == null)
            {
                return;
            }

            float dt = Time.deltaTime;
            float sfx = _director.Buses.Effective(AudioBus.Sfx) * _director.WorldGain;
            if (_tetherFader.IsAudible || _tetherFader.IsOn)
            {
                _tetherHum.transform.position = _tetherOrigin.position;
            }

            StepLoop(_tetherHum, _tetherFader, dt, _tuning.TetherHumFadeIn, _tuning.TetherHumFadeOut,
                _tetherHumCueVolume * _tuning.TetherHumVolume * sfx);
            StepLoop(_rumble, _rumbleFader, dt, _tuning.RumbleFadeIn, _tuning.RumbleFadeOut,
                _rumbleCueVolume * _tuning.RumbleVolume * sfx);
            StepLoop(_weldLoop, _weldFader, dt, WeldLoopFadeIn, WeldLoopFadeOut,
                _weldLoopCueVolume * sfx);
        }

        private static void StepLoop(AudioSource source, LoopFader fader, float dt, float fadeIn, float fadeOut,
            float volume)
        {
            fader.Step(dt, fadeIn, fadeOut);
            source.volume = fader.Gain * volume;
            if (fader.IsAudible && !source.isPlaying)
            {
                source.Play();
            }
            else if (!fader.IsAudible && source.isPlaying)
            {
                source.Stop();
            }
        }

        private void OnSonarPinged(SonarPinged ping)
        {
            _director.PlayAt(_sonarPing, ping.Origin, _tuning.ListenVolume);
        }

        private void OnRelicAnswered(RelicAnswered answer)
        {
            if (answer.RelicId == null || !_relicVoices.TryGetValue(answer.RelicId, out int voice))
            {
                Debug.LogError($"{nameof(GameplayAudio)}: no answer voice for relic '{answer.RelicId}'. Re-run " +
                               "tools/audio/build_sfx.py (it reads Data/Content/Relics) and the Audio/Library builder.",
                    this);
                return;
            }

            RelicAnswerTone tone = RelicAnswerTone.ForDistance(answer.Distance, _tuning);
            _director.PlayFilteredAt(_relicAnswer, voice, answer.Position, tone.Volume, tone.CutoffHz,
                _tuning.AnswerMinDistance);
        }

        private void OnSiteAnswered(SiteAnswered answer)
        {
            Dictionary<string, int> voices = answer.HoldsRelic ? _siteRelicVoices : _siteVoices;
            if (answer.SiteId == null || !voices.TryGetValue(answer.SiteId, out int voice))
            {
                Debug.LogError($"{nameof(GameplayAudio)}: no answer voice for site '{answer.SiteId}'. Add it to " +
                               "SITE_ANSWER_NOTES in tools/audio/cues.py and rebuild.", this);
                return;
            }

            RelicAnswerTone tone = RelicAnswerTone.ForDistance(answer.Distance, _tuning);
            _director.PlayFilteredAt(_siteAnswer, voice, answer.Position, tone.Volume, tone.CutoffHz,
                _tuning.AnswerMinDistance);
        }

        private void OnTetherAttached(TetherAttached attached)
        {
            float heft = Mathf.InverseLerp(_tuning.TetherLightMass, _tuning.TetherHeavyMass, attached.Mass);
            _director.PlayAt(_tetherAttach, attached.Position,
                Mathf.Lerp(_tuning.TetherLightVolume, _tuning.TetherHeavyVolume, heft));
            _tetherHum.transform.position = _tetherOrigin.position;
            _tetherFader.FadeIn();
        }

        private void OnTetherReleased(TetherReleased released)
        {
            _director.PlayAt(released.Snapped ? _tetherSnap : _tetherRelease, released.Position);
            _tetherFader.FadeOut();
        }

        private void OnExcavationStarted(ExcavationStarted started)
        {
            _rumble.transform.position = started.Position;
            _rumbleFader.FadeIn();
        }

        private void OnExcavationStopped(ExcavationStopped stopped)
        {
            _rumbleFader.FadeOut();
        }

        private void OnRelicSurfaced(RelicSurfaced surfaced)
        {
            _director.PlayAt(_surfacingSparkle, surfaced.Position);
        }

        private void OnRelicDeposited(RelicDeposited deposited)
        {
            _director.PlayAt(_relicPlaced, deposited.Position);
        }

        private void OnUpgradePurchased(UpgradePurchased upgrade)
        {
            string id = upgrade.UpgradeId;
            if (id == null || !id.StartsWith(WorkshopUpgradePrefix, StringComparison.Ordinal))
            {
                _director.Play2D(_upgradeArpeggio);
                return;
            }

            _director.Play2D(_workbenchUpgrade);
            if (string.Equals(id, HoverJumpUpgradeId, StringComparison.Ordinal))
            {
                _director.PlayAt(_coilPop, _rover.Position);
            }
        }

        private void OnCassetteCollected(CassetteCollected collected)
        {
            _director.PlayAt(_cassettePickup, collected.Position);
        }

        private void OnCrewLogFound(CrewLogFound found)
        {
            _director.PlayAt(_crewLogFound, found.Position);
        }

        private void OnBellSignalPicked(BellSignalPicked picked)
        {
            _signal.transform.position = picked.Position;
            _director.PlayOn(_signal, _signalPick, _tuning.SignalPickVolume);
        }

        private void OnBellSignalFound(BellSignalFound found)
        {
            _director.PlayAt(_signalFound, found.Position);
        }

        private void OnStructurePlaced(StructurePlaced placed)
        {
            if (!placed.Restored)
            {
                _director.PlayAt(_buildPlace, placed.Position);
            }
        }

        private void OnStructureBuilt(StructureBuilt built)
        {
            if (!built.Restored)
            {
                _director.PlayAt(_buildStructureComplete, built.Position);
            }

            _weldFader.FadeOut();
        }

        private void OnStructureRemoved(StructureRemoved removed)
        {
            _director.PlayAt(_buildDeconstruct, removed.Position);
        }

        private void OnBuildCue(BuildCue cue)
        {
            switch (cue.Kind)
            {
                case BuildCueKind.Ghost:
                    _director.PlayAt(_buildGhost, cue.Position);
                    break;
                case BuildCueKind.Snap:
                    _director.PlayAt(_buildSnap, cue.Position);
                    break;
                case BuildCueKind.Invalid:
                    _director.PlayAt(_buildInvalid, cue.Position);
                    break;
                case BuildCueKind.StageComplete:
                    _director.PlayAt(_buildStageComplete, cue.Position);
                    break;
            }
        }

        private void OnBuildWeldStateChanged(BuildWeldStateChanged weld)
        {
            if (weld.Welding)
            {
                _weldLoop.transform.position = weld.Position;
                _weldFader.FadeIn();
            }
            else
            {
                _weldFader.FadeOut();
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _subscriptions.Length; i++)
            {
                _subscriptions[i]?.Dispose();
                _subscriptions[i] = null;
            }
        }
    }
}
