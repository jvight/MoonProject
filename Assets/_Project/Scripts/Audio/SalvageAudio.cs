using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio
{
    /// <summary>
    /// Salvage (M3-13). While 07's beam cuts a piece (<see cref="SalvageCutStarted"/>) its material's texture plays at
    /// the cut point - Metal a deeper grind, Wiring a crackle and snap, Optics a glassy shimmer, sparks over all - with
    /// the beam's singing edge climbing D major pentatonic as the cut goes on (<see cref="CutBeamModel"/>). Letting go
    /// fades it softly; the piece coming loose (<see cref="SalvageCutStopped"/> completed) breaks off with its
    /// material's crack. Each piece folding into 07 (<see cref="MaterialSalvaged"/>) plays the salvage melody: a chime
    /// climbing the pentatonic with the site's combo step (<see cref="SalvageMelody"/>), back at the bottom for a new
    /// chain. Game-time loops duck while paused. Initialised by <see cref="AudioDirector"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SalvageAudio : MonoBehaviour
    {
        private const int SubscriptionCount = 3;
        private const int MaterialCount = 3;

        [Tooltip("Assets/_Project/Data/Audio/SalvageAudioTuning.asset.")]
        [SerializeField] private SalvageAudioTuning _tuning;

        private readonly IDisposable[] _subscriptions = new IDisposable[SubscriptionCount];
        private readonly AudioClip[] _cutClips = new AudioClip[MaterialCount];
        private readonly float[] _cutVolumes = new float[MaterialCount];
        private readonly int[] _breakVariants = new int[MaterialCount];
        private AudioDirector _director;
        private CutBeamModel _beam;
        private CueHandle _break;
        private CueHandle _chime;
        private AudioSource _texture;
        private AudioSource _tone;
        private Vector3 _cutPoint;
        private SalvageMaterial _material;
        private float _toneCueVolume;
        private int _chimeNotes;

        internal AudioSource TextureSource => _texture;

        internal AudioSource ToneSource => _tone;

        /// <summary>The pentatonic step the beam's tone is on (0 = D).</summary>
        public int BeamStep => _beam != null ? _beam.Step : 0;

        internal void Initialize(GameContext context, AudioDirector director)
        {
            if (_tuning == null)
            {
                Debug.LogError($"{nameof(SalvageAudio)}: {nameof(_tuning)} is not assigned.", this);
                enabled = false;
                return;
            }

            _break = director.Resolve(AudioCueIds.SalvageBreak);
            _chime = director.Resolve(AudioCueIds.SalvageChime);
            CueHandle tone = director.Resolve(AudioCueIds.SalvageCutTone);
            if (!_break.IsValid || !_chime.IsValid || !tone.IsValid || !TryResolveMaterials(director))
            {
                enabled = false;
                return;
            }

            _director = director;
            _beam = new CutBeamModel(_tuning);
            _chimeNotes = director.Library.GetCue(_chime).ClipCount;
            _toneCueVolume = director.Library.GetCue(tone).VolumeMax;
            _texture = director.CreateLoopSource(transform, "CutTexture", default, 1f);
            _tone = director.CreateLoopSource(transform, "CutTone", tone, 1f);
            EventBus events = context.Events;
            _subscriptions[0] = events.Subscribe<SalvageCutStarted>(OnCutStarted);
            _subscriptions[1] = events.Subscribe<SalvageCutStopped>(OnCutStopped);
            _subscriptions[2] = events.Subscribe<MaterialSalvaged>(OnMaterialSalvaged);
        }

        internal void Wire(SalvageAudioTuning tuning)
        {
            _tuning = tuning;
        }

        private void Update()
        {
            if (_director == null)
            {
                return;
            }

            _beam.Update(Time.deltaTime);
            float sfx = _director.Buses.Effective(AudioBus.Sfx) * _director.WorldGain * _beam.Gain;
            int material = (int)_material;
            Drive(_texture, sfx * _tuning.TextureVolume * _cutVolumes[material], _beam.TexturePitch);
            Drive(_tone, sfx * _tuning.ToneVolume * _toneCueVolume, _beam.TonePitch);
        }

        private void OnCutStarted(SalvageCutStarted started)
        {
            _cutPoint = started.Position;
            _texture.transform.position = _cutPoint;
            _tone.transform.position = _cutPoint;
            if (_material != started.Material || _texture.clip == null)
            {
                _texture.Stop();
                _texture.clip = _cutClips[(int)started.Material];
            }

            _material = started.Material;
            _beam.Start();
        }

        private void OnCutStopped(SalvageCutStopped stopped)
        {
            _beam.Stop(stopped.Completed);
            if (stopped.Completed)
            {
                _director.PlayVariantAt(_break, _breakVariants[(int)_material], _cutPoint);
            }
        }

        private void OnMaterialSalvaged(MaterialSalvaged salvaged)
        {
            int note = SalvageMelody.NoteIndex(salvaged.ComboStep, _chimeNotes, _tuning.MelodyTopWindow);
            _director.PlayVariantAt(_chime, note, salvaged.Position);
        }

        private bool TryResolveMaterials(AudioDirector director)
        {
            AudioCue breaks = director.Library.GetCue(_break);
            for (int m = 0; m < MaterialCount; m++)
            {
                var material = (SalvageMaterial)m;
                CueHandle cut = director.Resolve(SalvageSounds.CutCue(material));
                int variant = FindVariant(breaks, SalvageSounds.BreakLabel(material));
                if (!cut.IsValid || variant < 0)
                {
                    Debug.LogError($"{nameof(SalvageAudio)}: no cutting loop or break-off for {material}.", this);
                    return false;
                }

                AudioCue cue = director.Library.GetCue(cut);
                _cutClips[m] = cue.GetClip(0);
                _cutVolumes[m] = cue.VolumeMax;
                _breakVariants[m] = variant;
            }

            return true;
        }

        private static int FindVariant(AudioCue cue, string label)
        {
            for (int i = 0; i < cue.ClipCount; i++)
            {
                if (cue.GetVariantLabel(i) == label)
                {
                    return i;
                }
            }

            return -1;
        }

        private static void Drive(AudioSource loop, float volume, float pitch)
        {
            loop.volume = volume;
            loop.pitch = pitch;
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
            for (int i = 0; i < _subscriptions.Length; i++)
            {
                _subscriptions[i]?.Dispose();
                _subscriptions[i] = null;
            }
        }
    }
}
