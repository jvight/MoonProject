using UnityEngine;
using MoonProject.Editor.SceneBuild;

namespace MoonProject.Audio.Editor
{
    /// <summary>
    /// Adds the Audio domain to Main.unity: under [Audio], an AudioDirector (the system, initialised after World, Rover
    /// and Gameplay) wired to the library and tuning assets, plus its RoverAudio, JumpAudio, GameplayAudio,
    /// FriendAudio, UiAudio, RadioStation, AmbienceBed, CanyonAmbience, Soundscape, RoverSmallSounds, RelayAudio and
    /// SalvageAudio parts.
    /// Voices and loop sources are created by the components at initialisation, not baked into the scene.
    /// </summary>
    public sealed class AudioSceneContributor : ISceneContributor
    {
        /// <summary>After Gameplay (500), whose friend roster Audio reads at initialisation; before UI (600), which
        /// uses <see cref="MoonProject.Core.IAudioSettings"/>.</summary>
        public int Order => 550;

        public void Contribute(SceneBuildContext context)
        {
            var library = context.LoadAsset<AudioLibrary>(AudioAssetPaths.Library);
            var playlist = context.LoadAsset<RadioPlaylist>(AudioAssetPaths.Playlist);
            var tapes = context.LoadAsset<RadioTapeLibrary>(AudioAssetPaths.Tapes);
            var mixTuning = context.LoadAsset<AudioMixTuning>(AudioAssetPaths.MixTuning);
            var roverTuning = context.LoadAsset<RoverAudioTuning>(AudioAssetPaths.RoverTuning);
            var radioTuning = context.LoadAsset<RadioTuning>(AudioAssetPaths.RadioTuning);
            var gameplayTuning = context.LoadAsset<GameplayAudioTuning>(AudioAssetPaths.GameplayTuning);
            var uiTuning = context.LoadAsset<UiAudioTuning>(AudioAssetPaths.UiTuning);
            var friendTuning = context.LoadAsset<FriendAudioTuning>(AudioAssetPaths.FriendTuning);
            var jumpTuning = context.LoadAsset<JumpAudioTuning>(AudioAssetPaths.JumpTuning);
            var canyonTuning = context.LoadAsset<CanyonAudioTuning>(AudioAssetPaths.CanyonTuning);
            var soundscapeTuning = context.LoadAsset<SoundscapeTuning>(AudioAssetPaths.SoundscapeTuning);
            var relayTuning = context.LoadAsset<RelayAudioTuning>(AudioAssetPaths.RelayTuning);
            var salvageTuning = context.LoadAsset<SalvageAudioTuning>(AudioAssetPaths.SalvageTuning);

            Transform root = context.AudioRoot.transform;
            var director = context.CreateChild("AudioDirector", root).AddComponent<AudioDirector>();
            var rover = context.CreateChild("RoverAudio", root).AddComponent<RoverAudio>();
            var jump = context.CreateChild("JumpAudio", root).AddComponent<JumpAudio>();
            var gameplay = context.CreateChild("GameplayAudio", root).AddComponent<GameplayAudio>();
            var friends = context.CreateChild("FriendAudio", root).AddComponent<FriendAudio>();
            var ui = context.CreateChild("UiAudio", root).AddComponent<UiAudio>();
            var radio = context.CreateChild("RadioStation", root).AddComponent<RadioStation>();
            var ambience = context.CreateChild("AmbienceBed", root).AddComponent<AmbienceBed>();
            var canyon = context.CreateChild("CanyonAmbience", root).AddComponent<CanyonAmbience>();
            var soundscape = context.CreateChild("Soundscape", root).AddComponent<Soundscape>();
            var smallSounds = context.CreateChild("RoverSmallSounds", root).AddComponent<RoverSmallSounds>();
            var relays = context.CreateChild("RelayAudio", root).AddComponent<RelayAudio>();
            var salvage = context.CreateChild("SalvageAudio", root).AddComponent<SalvageAudio>();

            rover.Wire(roverTuning);
            gameplay.Wire(gameplayTuning);
            ui.Wire(uiTuning);
            friends.Wire(friendTuning);
            jump.Wire(jumpTuning);
            radio.Wire(radioTuning, playlist, tapes);
            canyon.Wire(canyonTuning);
            soundscape.Wire(soundscapeTuning);
            smallSounds.Wire(soundscapeTuning);
            relays.Wire(relayTuning);
            salvage.Wire(salvageTuning);
            director.Wire(library, mixTuning, rover, jump, gameplay, friends, ui, radio, ambience, canyon, soundscape,
                smallSounds, relays, salvage);
            context.AddSystem(director);
        }
    }
}
