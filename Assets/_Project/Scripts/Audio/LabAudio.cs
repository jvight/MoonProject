using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio
{
    /// <summary>
    /// The crew's lab up the belly lift (docs/features/M3-19) and its map (M3-07), all in D major pentatonic:
    /// <list type="bullet">
    /// <item>while 07 is up in the lab a close, warm room tone with a soft D lamp hum takes over
    /// (<see cref="LabEntered"/>, <see cref="LabLeft"/>);</item>
    /// <item>the turntable settles at each station with a felt detent and a tine, one pentatonic note per station
    /// (<see cref="LabStationFaced"/>);</item>
    /// <item>the reader takes a tube into its slot, its reel purrs and its tape ticks while it decodes, and a small
    /// chime rises as the find is decoded (<see cref="BlueprintDelivered"/>, <see cref="FindDecoded"/>); down in the
    /// bay Kenji's plotter scratches as it draws the sheet (<see cref="BlueprintDrawing"/>);</item>
    /// <item>on the map, a pencil hiss as a place blinks seen (<see cref="MapPlaceSeen"/>), the chart pen's scratch
    /// climbing the scale place by place (<see cref="MapInkCued"/>) and the light table's warm hum while the map is
    /// open (<see cref="MapLookStarted"/>, <see cref="MapLookEnded"/>);</item>
    /// <item>wherever 07 is, a warm chord rises as a row of the lab's recovery panel completes, never on a load
    /// (<see cref="RecoveryProgressChanged"/>, docs/features/M3-21).</item>
    /// </list>
    /// The lab's loops sit with 07 (the lab is one small room). Game-time loops duck while paused. Initialised by
    /// <see cref="AudioDirector"/>; nothing allocates per frame.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LabAudio : MonoBehaviour
    {
        private const float RoomSpatialBlend = 0f;
        private const float MachineSpatialBlend = 1f;

        [Tooltip("Assets/_Project/Data/Audio/StationAudioTuning.asset (its lab block).")]
        [SerializeField] private StationAudioTuning _tuning;

        private readonly LoopFader _room = new LoopFader();
        private readonly LoopFader _reel = new LoopFader();
        private readonly LoopFader _table = new LoopFader();
        private IDisposable[] _subscriptions = Array.Empty<IDisposable>();
        private AudioDirector _director;
        private IRoverState _rover;
        private CueHandle _detent;
        private CueHandle _take;
        private CueHandle _chime;
        private CueHandle _rowChord;
        private CueHandle _plotter;
        private CueHandle _pencil;
        private CueHandle _pen;
        private AudioSource _roomTone;
        private AudioSource _reelLoop;
        private AudioSource _tableHum;
        private float _roomCueVolume;
        private float _reelCueVolume;
        private float _tableCueVolume;
        private int _penNotes;

        /// <summary>The room tone's source (tests).</summary>
        internal AudioSource RoomSource => _roomTone;

        /// <summary>The reader's reel loop (tests).</summary>
        internal AudioSource ReelSource => _reelLoop;

        internal void Wire(StationAudioTuning tuning)
        {
            _tuning = tuning;
        }

        internal void Initialize(GameContext context, AudioDirector director)
        {
            if (_tuning == null)
            {
                Debug.LogError($"{nameof(LabAudio)}: {nameof(_tuning)} is not assigned.", this);
                enabled = false;
                return;
            }

            CueHandle room = director.Resolve(AudioCueIds.LabRoomTone);
            CueHandle reel = director.Resolve(AudioCueIds.ReaderReel);
            CueHandle table = director.Resolve(AudioCueIds.MapTableHum);
            _detent = director.Resolve(AudioCueIds.LabDetent);
            _take = director.Resolve(AudioCueIds.ReaderTake);
            _chime = director.Resolve(AudioCueIds.ReaderDecodeChime);
            _rowChord = director.Resolve(AudioCueIds.RecoveryRowChord);
            _plotter = director.Resolve(AudioCueIds.PlotterDraw);
            _pencil = director.Resolve(AudioCueIds.MapPencil);
            _pen = director.Resolve(AudioCueIds.MapPen);
            if (!room.IsValid || !reel.IsValid || !table.IsValid || !_detent.IsValid || !_take.IsValid ||
                !_chime.IsValid || !_rowChord.IsValid || !_plotter.IsValid || !_pencil.IsValid || !_pen.IsValid)
            {
                enabled = false;
                return;
            }

            _director = director;
            _rover = context.Get<IRoverState>();
            AudioLibrary library = director.Library;
            _roomCueVolume = library.GetCue(room).VolumeMax;
            _reelCueVolume = library.GetCue(reel).VolumeMax;
            _tableCueVolume = library.GetCue(table).VolumeMax;
            _penNotes = library.GetCue(_pen).ClipCount;
            _roomTone = director.CreateLoopSource(transform, "LabRoomTone", room, RoomSpatialBlend);
            _reelLoop = director.CreateLoopSource(transform, "ReaderReel", reel, MachineSpatialBlend);
            _tableHum = director.CreateLoopSource(transform, "TableHum", table, MachineSpatialBlend);
            EventBus events = context.Events;
            _subscriptions = new[]
            {
                events.Subscribe<LabEntered>(_ => _room.FadeIn()),
                events.Subscribe<LabLeft>(_ => _room.FadeOut()),
                events.Subscribe<LabStationFaced>(OnFaced),
                events.Subscribe<BlueprintDelivered>(OnTaken),
                events.Subscribe<FindDecoded>(OnDecoded),
                events.Subscribe<BlueprintDrawing>(OnDrawing),
                events.Subscribe<MapPlaceSeen>(_ => director.Play2D(_pencil, _tuning.MapPencilVolume)),
                events.Subscribe<MapInkCued>(OnInked),
                events.Subscribe<MapLookStarted>(_ => _table.FadeIn()),
                events.Subscribe<MapLookEnded>(_ => _table.FadeOut()),
                events.Subscribe<RecoveryProgressChanged>(OnRecovery),
            };
        }

        private void Update()
        {
            if (_director == null)
            {
                return;
            }

            float dt = Time.deltaTime;
            Vector3 here = _rover.Position;
            float sfx = _director.Buses.Effective(AudioBus.Sfx) * _director.WorldGain;
            float ambience = _director.Buses.Effective(AudioBus.Ambience) * _director.WorldGain;
            _room.Step(dt, _tuning.LabRoomFadeIn, _tuning.LabRoomFadeOut);
            Drive(_roomTone, _room.Gain * _tuning.LabRoomVolume * _roomCueVolume * ambience);
            _reel.Step(dt, _tuning.ReelFade, _tuning.ReelFade);
            _reelLoop.transform.position = here;
            Drive(_reelLoop, _reel.Gain * _tuning.ReelVolume * _reelCueVolume * sfx);
            _table.Step(dt, _tuning.TableHumFade, _tuning.TableHumFade);
            _tableHum.transform.position = here;
            Drive(_tableHum, _table.Gain * _tuning.TableHumVolume * _tableCueVolume * sfx);
        }

        private void OnFaced(LabStationFaced faced)
        {
            _director.PlayVariantAt(_detent, (int)faced.Station, _rover.Position, _tuning.DetentVolume);
        }

        private void OnTaken(BlueprintDelivered delivered)
        {
            _director.PlayAt(_take, _rover.Position, _tuning.ReaderTakeVolume);
            _reel.FadeIn();
        }

        private void OnDecoded(FindDecoded decoded)
        {
            _reel.FadeOut();
            _director.Play2D(_chime, _tuning.DecodeChimeVolume);
        }

        private void OnRecovery(RecoveryProgressChanged changed)
        {
            if (!changed.Restored && changed.Lamps > 0 && changed.LitLamps >= changed.Lamps)
            {
                _director.Play2D(_rowChord, _tuning.RowChordVolume);
            }
        }

        private void OnDrawing(BlueprintDrawing drawing)
        {
            _director.Play2D(_plotter, _tuning.PlotterVolume);
        }

        /// <summary>Each inked place one step higher up the pentatonic scale, wrapping after its top note.</summary>
        private void OnInked(MapInkCued inked)
        {
            _director.PlayVariant2D(_pen, inked.Order % _penNotes, _tuning.MapPenVolume);
        }

        private static void Drive(AudioSource loop, float volume)
        {
            loop.volume = volume;
            if (volume > 0f && !loop.isPlaying)
            {
                loop.Play();
            }
            else if (volume <= 0f && loop.isPlaying)
            {
                loop.Stop();
            }
        }

        private void OnDestroy()
        {
            foreach (IDisposable subscription in _subscriptions)
            {
                subscription.Dispose();
            }
        }
    }
}
