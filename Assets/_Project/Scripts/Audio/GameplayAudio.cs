using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio
{
    /// <summary>
    /// The base's sounds (docs/design/opening-v2.md B5-B10): build mode's ghost, snap, invalid spot, placement, weld
    /// loop (following the beam), stage and structure complete and deconstruct (<see cref="BuildCue"/> and the
    /// structure events); a structure taking power for the first time (<see cref="StructurePowerChanged"/> from
    /// unpowered to powered, not on a restore): a relay thunk and a transformer swell rising onto its lamp; and the
    /// first nightfall's light sequence (<see cref="SkyPhaseChanged"/> Sunset to Earthlit): every built structure's
    /// lamp clicking on one by one, nearest to the base first, climbing the pentatonic ladder
    /// (<see cref="NightLightsSequence"/>). Initialised by <see cref="AudioDirector"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameplayAudio : MonoBehaviour
    {
        private const int SubscriptionCount = 7;
        private const int InitialStructureCapacity = 32;

        [Tooltip("Assets/_Project/Data/Audio/GameplayAudioTuning.asset.")]
        [SerializeField] private GameplayAudioTuning _tuning;

        private readonly IDisposable[] _subscriptions = new IDisposable[SubscriptionCount];
        private readonly LoopFader _weldFader = new LoopFader();
        private readonly NightLightsSequence _nightLights = new NightLightsSequence();
        private readonly Dictionary<int, bool> _powered = new Dictionary<int, bool>(InitialStructureCapacity);
        private AudioDirector _director;
        private IWorldLayout _layout;
        private CueHandle _buildGhost;
        private CueHandle _buildSnap;
        private CueHandle _buildInvalid;
        private CueHandle _buildPlace;
        private CueHandle _buildStageComplete;
        private CueHandle _buildStructureComplete;
        private CueHandle _buildDeconstruct;
        private CueHandle _powerOn;
        private CueHandle _baseLamp;
        private AudioSource _weldLoop;
        private float _weldLoopCueVolume;
        private int _lampNotes;
        private SkyPhase _phase;

        /// <summary>True while the build weld loop can be heard (including its fade-out).</summary>
        public bool BuildWeldAudible => _weldFader.IsAudible;

        /// <summary>True while the nightfall's lamps are still clicking on.</summary>
        public bool NightLightsRunning => _nightLights.Running;

        /// <summary>How many built structures will light at nightfall.</summary>
        public int NightLampCount => _nightLights.Count;

        internal AudioSource BuildWeldSource => _weldLoop;

        internal void Wire(GameplayAudioTuning tuning)
        {
            _tuning = tuning;
        }

        internal void Initialize(GameContext context, AudioDirector director)
        {
            if (_tuning == null)
            {
                Debug.LogError($"{nameof(GameplayAudio)}: {nameof(_tuning)} is not assigned.", this);
                enabled = false;
                return;
            }

            _buildGhost = director.Resolve(AudioCueIds.BuildGhost);
            _buildSnap = director.Resolve(AudioCueIds.BuildSnap);
            _buildInvalid = director.Resolve(AudioCueIds.BuildInvalid);
            _buildPlace = director.Resolve(AudioCueIds.BuildPlace);
            _buildStageComplete = director.Resolve(AudioCueIds.BuildStageComplete);
            _buildStructureComplete = director.Resolve(AudioCueIds.BuildStructureComplete);
            _buildDeconstruct = director.Resolve(AudioCueIds.BuildDeconstruct);
            _powerOn = director.Resolve(AudioCueIds.StructurePowerOn);
            _baseLamp = director.Resolve(AudioCueIds.BaseLampOn);
            CueHandle weldLoop = director.Resolve(AudioCueIds.BuildWeldLoop);
            if (!(_buildGhost.IsValid && _buildSnap.IsValid && _buildInvalid.IsValid && _buildPlace.IsValid &&
                  weldLoop.IsValid && _buildStageComplete.IsValid && _buildStructureComplete.IsValid &&
                  _buildDeconstruct.IsValid && _powerOn.IsValid && _baseLamp.IsValid))
            {
                enabled = false;
                return;
            }

            _director = director;
            _layout = context.Get<IWorldLayout>();
            _phase = context.TryGet(out ISunlight sunlight) ? sunlight.Phase : SkyPhase.Earthlit;
            _weldLoopCueVolume = director.Library.GetCue(weldLoop).VolumeMax;
            _lampNotes = director.Library.GetCue(_baseLamp).ClipCount;
            _weldLoop = director.CreateLoopSource(transform, "BuildWeldLoop", weldLoop, 1f);

            EventBus events = context.Events;
            _subscriptions[0] = events.Subscribe<StructurePlaced>(OnStructurePlaced);
            _subscriptions[1] = events.Subscribe<StructureBuilt>(OnStructureBuilt);
            _subscriptions[2] = events.Subscribe<StructureRemoved>(OnStructureRemoved);
            _subscriptions[3] = events.Subscribe<StructurePowerChanged>(OnStructurePowerChanged);
            _subscriptions[4] = events.Subscribe<BuildCue>(OnBuildCue);
            _subscriptions[5] = events.Subscribe<BuildWeldStateChanged>(OnBuildWeldStateChanged);
            _subscriptions[6] = events.Subscribe<SkyPhaseChanged>(OnSkyPhaseChanged);
        }

        private void Update()
        {
            if (_director == null)
            {
                return;
            }

            float dt = Time.deltaTime;
            float sfx = _director.Buses.Effective(AudioBus.Sfx) * _director.WorldGain;
            _weldFader.Step(dt, _tuning.WeldFadeIn, _tuning.WeldFadeOut);
            _weldLoop.volume = _weldFader.Gain * _weldLoopCueVolume * sfx;
            if (_weldFader.IsAudible && !_weldLoop.isPlaying)
            {
                _weldLoop.Play();
            }
            else if (!_weldFader.IsAudible && _weldLoop.isPlaying)
            {
                _weldLoop.Stop();
            }

            if (_nightLights.Step(dt, _tuning.NightLampInterval, out Vector3 lamp, out int order))
            {
                _director.PlayVariantAt(_baseLamp, Mathf.Min(order, _lampNotes - 1), lamp,
                    _tuning.NightLampVolume);
            }
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
            _nightLights.Add(built.InstanceId, built.Position);
            if (!built.Restored)
            {
                _director.PlayAt(_buildStructureComplete, built.Position);
            }

            _weldFader.FadeOut();
        }

        private void OnStructureRemoved(StructureRemoved removed)
        {
            _nightLights.Remove(removed.InstanceId);
            _powered.Remove(removed.InstanceId);
            _director.PlayAt(_buildDeconstruct, removed.Position);
        }

        /// <summary>Only the moment a structure first takes power sounds; dimming and restoring stay quiet.</summary>
        private void OnStructurePowerChanged(StructurePowerChanged changed)
        {
            bool powered = changed.SupplyRatio > 0f;
            bool was = _powered.TryGetValue(changed.InstanceId, out bool before) && before;
            _powered[changed.InstanceId] = powered;
            if (powered && !was && !changed.Restored)
            {
                _director.PlayAt(_powerOn, changed.Position, _tuning.PowerOnVolume);
            }
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

        /// <summary>A real sunset into night starts the lamps; a load into night (no Sunset before) does not.</summary>
        private void OnSkyPhaseChanged(SkyPhaseChanged changed)
        {
            SkyPhase previous = _phase;
            _phase = changed.Phase;
            if (changed.Phase == SkyPhase.Earthlit && previous == SkyPhase.Sunset)
            {
                _nightLights.Begin(_layout.BasePosition, _tuning.NightLampDelay);
            }
            else if (changed.Phase != SkyPhase.Earthlit)
            {
                _nightLights.Stop();
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
