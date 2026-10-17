using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio
{
    /// <summary>
    /// Plays the dedicated title-screen theme (<see cref="TitleMusicLibrary.SelectedTrack"/>) on the Music bus.
    /// Fades in over <see cref="TitleMusicLibrary.FadeInSeconds"/> (~2 s) when <see cref="TitleShown"/> is published,
    /// keeps playing through Settings and Credits on the title screen, and fades out over
    /// <see cref="TitleMusicLibrary.FadeOutSeconds"/> (0.8 s, under the CRT switch-off) when Continue (<see cref="SessionStarted"/>) or New Game
    /// (<see cref="NewGameRequested"/>) begins play, with a soft tube "tock" as the CRT switches off (the static and
    /// the power-on thump then own the black). Initialised by <see cref="AudioDirector"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TitleMusic : MonoBehaviour
    {
        private const int SubscriptionCount = 3;

        [Tooltip("Assets/_Project/Data/Audio/TitleMusicLibrary.asset (built from tools/music/title.json).")]
        [SerializeField] private TitleMusicLibrary _library;

        private readonly TitleMusicFader _fader = new TitleMusicFader();
        private readonly IDisposable[] _subscriptions = new IDisposable[SubscriptionCount];
        private AudioDirector _director;
        private AudioSource _source;
        private RadioTrack _currentTrack;
        private CueHandle _crtOff;

        public TitleMusicLibrary Library => _library;
        public AudioSource Source => _source;
        public RadioTrack CurrentTrack => _currentTrack;
        public float EnvelopeLevel => _fader.Level;
        public bool IsPlaying => _source != null && _source.isPlaying;

        internal void Initialize(GameContext context, AudioDirector director)
        {
            if (_library == null)
            {
                Debug.LogError($"{nameof(TitleMusic)}: TitleMusicLibrary is not assigned.", this);
                enabled = false;
                return;
            }

            string problem = _library.FindProblem();
            if (problem != null)
            {
                Debug.LogError($"{nameof(TitleMusic)}: invalid library ({problem}).", this);
                enabled = false;
                return;
            }

            _crtOff = director.Resolve(AudioCueIds.TitleCrtOff);
            if (!_crtOff.IsValid)
            {
                enabled = false;
                return;
            }

            _director = director;
            var host = new GameObject("TitleTheme");
            host.transform.SetParent(transform, false);
            _source = host.AddComponent<AudioSource>();
            director.Configure(_source, 0f);
            _source.loop = true;
            _source.volume = 0f;

            _subscriptions[0] = context.Events.Subscribe<TitleShown>(OnTitleShown);
            _subscriptions[1] = context.Events.Subscribe<SessionStarted>(OnSessionStarted);
            _subscriptions[2] = context.Events.Subscribe<NewGameRequested>(OnNewGameRequested);
        }

        internal void Wire(TitleMusicLibrary library)
        {
            _library = library;
        }

        private void Update()
        {
            if (_source == null)
            {
                return;
            }

            if (!_fader.IsPlaying)
            {
                if (_source.isPlaying)
                {
                    _source.volume = 0f;
                    _source.Stop();
                }

                return;
            }

            float dt = Time.unscaledDeltaTime;
            float envelope = _fader.Step(dt, _library.FadeInSeconds, _library.FadeOutSeconds);
            _source.volume = envelope * _library.Volume * _director.Buses.Effective(AudioBus.Music);
            if (!_fader.IsPlaying && _source.isPlaying)
            {
                _source.volume = 0f;
                _source.Stop();
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

        private void OnTitleShown(TitleShown shown)
        {
            RadioTrack track = _library.SelectedTrack;
            if (track == null || track.Clip == null)
            {
                return;
            }

            if (_currentTrack != track || _source.clip != track.Clip)
            {
                _currentTrack = track;
                _source.clip = track.Clip;
            }

            _fader.ShowTitle();
            _source.volume = _fader.Level * _library.Volume * _director.Buses.Effective(AudioBus.Music);
            if (!_source.isPlaying)
            {
                _source.timeSamples = 0;
                _source.Play();
            }
        }

        private void OnSessionStarted(SessionStarted started)
        {
            BeginFadeOut();
        }

        private void OnNewGameRequested(NewGameRequested request)
        {
            BeginFadeOut();
        }

        private void BeginFadeOut()
        {
            if (_fader.IsPlaying && !_fader.IsFadingOut)
            {
                _director.Play2D(_crtOff);
            }

            _fader.FadeOut();
            if (!_fader.IsPlaying && _source != null && _source.isPlaying)
            {
                _source.volume = 0f;
                _source.Stop();
            }
        }
    }
}
