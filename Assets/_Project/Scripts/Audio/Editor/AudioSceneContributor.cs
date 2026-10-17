using UnityEngine;
using MoonProject.Editor.SceneBuild;

namespace MoonProject.Audio.Editor
{
    /// <summary>
    /// Adds the Audio domain to Main.unity: under [Audio], an AudioDirector (the system, initialised after World, Rover
    /// and Gameplay) wired to the library and tuning assets, plus its RoverAudio, JumpAudio, GameplayAudio, UiAudio,
    /// RadioStation, TitleMusic, FeelAudio, RoverChirpAudio, AmbienceBed, Soundscape, RoverSmallSounds, SalvageAudio
    /// and BatteryAudio parts.
    /// Voices and loop sources are created by the components at initialisation, not baked into the scene.
    /// </summary>
    public sealed class AudioSceneContributor : ISceneContributor
    {
        /// <summary>After Gameplay (500), whose services Audio reads at initialisation; before UI (600), which uses
        /// <see cref="MoonProject.Core.IAudioSettings"/>.</summary>
        public int Order => 550;

        public void Contribute(SceneBuildContext context)
        {
            var library = context.LoadAsset<AudioLibrary>(AudioAssetPaths.Library);
            var playlist = context.LoadAsset<RadioPlaylist>(AudioAssetPaths.Playlist);
            var titleMusicLibrary = context.LoadAsset<TitleMusicLibrary>(AudioAssetPaths.TitleMusic);
            var mixTuning = context.LoadAsset<AudioMixTuning>(AudioAssetPaths.MixTuning);
            var roverTuning = context.LoadAsset<RoverAudioTuning>(AudioAssetPaths.RoverTuning);
            var radioTuning = context.LoadAsset<RadioTuning>(AudioAssetPaths.RadioTuning);
            var gameplayTuning = context.LoadAsset<GameplayAudioTuning>(AudioAssetPaths.GameplayTuning);
            var uiTuning = context.LoadAsset<UiAudioTuning>(AudioAssetPaths.UiTuning);
            var jumpTuning = context.LoadAsset<JumpAudioTuning>(AudioAssetPaths.JumpTuning);
            var soundscapeTuning = context.LoadAsset<SoundscapeTuning>(AudioAssetPaths.SoundscapeTuning);
            var salvageTuning = context.LoadAsset<SalvageAudioTuning>(AudioAssetPaths.SalvageTuning);
            var feelTuning = context.LoadAsset<FeelAudioTuning>(AudioAssetPaths.FeelTuning);

            Transform root = context.AudioRoot.transform;
            var director = context.CreateChild("AudioDirector", root).AddComponent<AudioDirector>();
            var rover = context.CreateChild("RoverAudio", root).AddComponent<RoverAudio>();
            var jump = context.CreateChild("JumpAudio", root).AddComponent<JumpAudio>();
            var gameplay = context.CreateChild("GameplayAudio", root).AddComponent<GameplayAudio>();
            var ui = context.CreateChild("UiAudio", root).AddComponent<UiAudio>();
            var radio = context.CreateChild("RadioStation", root).AddComponent<RadioStation>();
            var titleMusic = context.CreateChild("TitleMusic", root).AddComponent<TitleMusic>();
            var feel = context.CreateChild("FeelAudio", root).AddComponent<FeelAudio>();
            var chirps = context.CreateChild("RoverChirpAudio", root).AddComponent<RoverChirpAudio>();
            var ambience = context.CreateChild("AmbienceBed", root).AddComponent<AmbienceBed>();
            var soundscape = context.CreateChild("Soundscape", root).AddComponent<Soundscape>();
            var smallSounds = context.CreateChild("RoverSmallSounds", root).AddComponent<RoverSmallSounds>();
            var salvage = context.CreateChild("SalvageAudio", root).AddComponent<SalvageAudio>();
            var battery = context.CreateChild("BatteryAudio", root).AddComponent<BatteryAudio>();

            rover.Wire(roverTuning);
            gameplay.Wire(gameplayTuning);
            ui.Wire(uiTuning);
            jump.Wire(jumpTuning);
            radio.Wire(radioTuning, playlist);
            titleMusic.Wire(titleMusicLibrary);
            feel.Wire(feelTuning);
            chirps.Wire(feelTuning, context.LoadAsset<ChirpBank>(AudioAssetPaths.Chirps));
            soundscape.Wire(soundscapeTuning);
            smallSounds.Wire(soundscapeTuning);
            salvage.Wire(salvageTuning);
            director.Wire(library, mixTuning, rover, jump, gameplay, ui, radio, ambience, soundscape, smallSounds,
                salvage);
            director.WireTitleMusic(titleMusic);
            director.WireBattery(battery);
            director.WireFeel(feel, chirps);
            context.AddSystem(director);
        }
    }
}
