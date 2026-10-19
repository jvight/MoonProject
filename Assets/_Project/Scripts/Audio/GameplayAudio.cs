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
    /// structure events); a structure taking power (<see cref="StructurePowerChanged"/> from unpowered to powered,
    /// not on a restore, not just after its own build, one swell for several at once): a relay thunk and a
    /// transformer swell rising onto its lamp; and each base lamp warming on at dusk (<see cref="BaseLampLit"/>, one
    /// sound per lamp as its light starts easing up, at that structure, a step higher along the lighting order).
    /// Initialised by <see cref="AudioDirector"/>.
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
        private readonly Dictionary<int, Vector3> _builtPositions = new Dictionary<int, Vector3>(InitialStructureCapacity);
        private readonly Dictionary<int, bool> _powered = new Dictionary<int, bool>(InitialStructureCapacity);
        private readonly Dictionary<int, float> _builtAt = new Dictionary<int, float>(InitialStructureCapacity);
        private AudioDirector _director;
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
        private float _lastPowerOn = float.NegativeInfinity;

        /// <summary>True while the build weld loop can be heard (including its fade-out).</summary>
        public bool BuildWeldAudible => _weldFader.IsAudible;

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
            _subscriptions[6] = events.Subscribe<BaseLampLit>(OnBaseLampLit);
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
            _builtPositions[built.InstanceId] = built.Position;
            _builtAt[built.InstanceId] = built.Restored ? float.NegativeInfinity : Time.time;
            if (!built.Restored)
            {
                _director.PlayAt(_buildStructureComplete, built.Position);
            }

            _weldFader.FadeOut();
        }

        private void OnStructureRemoved(StructureRemoved removed)
        {
            _builtPositions.Remove(removed.InstanceId);
            _powered.Remove(removed.InstanceId);
            _builtAt.Remove(removed.InstanceId);
            _director.PlayAt(_buildDeconstruct, removed.Position);
        }

        /// <summary>Only the moment a structure takes power sounds; dimming and restoring stay quiet.</summary>
        private void OnStructurePowerChanged(StructurePowerChanged changed)
        {
            bool powered = changed.SupplyRatio > 0f;
            bool was = _powered.TryGetValue(changed.InstanceId, out bool before) && before;
            _powered[changed.InstanceId] = powered;
            if (!powered || was || changed.Restored)
            {
                return;
            }

            float now = Time.time;
            bool justBuilt = _builtAt.TryGetValue(changed.InstanceId, out float builtAt) &&
                             now - builtAt < _tuning.PowerOnBuildGrace;
            if (justBuilt || now - _lastPowerOn < _tuning.PowerOnMinInterval)
            {
                return;
            }

            _lastPowerOn = now;
            _director.PlayAt(_powerOn, changed.Position, _tuning.PowerOnVolume);
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

        /// <summary>A lamp starts easing up: its relay, filament and hum, at its structure, on its step.</summary>
        private void OnBaseLampLit(BaseLampLit lit)
        {
            if (!_builtPositions.TryGetValue(lit.InstanceId, out Vector3 position))
            {
                Debug.LogError($"{nameof(GameplayAudio)}: lamp lit on structure {lit.InstanceId}, which was never " +
                               $"announced built ({nameof(StructureBuilt)}).", this);
                return;
            }

            _director.PlayVariantAt(_baseLamp, Mathf.Clamp(lit.Index, 0, _lampNotes - 1), position,
                _tuning.BaseLampVolume);
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
