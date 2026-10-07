using UnityEngine;
using MoonProject.Editor.SceneBuild;

namespace MoonProject.Audio.Editor
{
    /// <summary>
    /// Adds the Audio domain to Main.unity: under [Audio], an AudioDirector (the system, initialised after World, Rover
    /// and Gameplay) wired to the library and tuning assets, plus its RoverAudio, JumpAudio, GameplayAudio,
    /// FriendAudio, UiAudio, RadioStation and AmbienceBed parts.
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

            Transform root = context.AudioRoot.transform;
            var director = context.CreateChild("AudioDirector", root).AddComponent<AudioDirector>();
            var rover = context.CreateChild("RoverAudio", root).AddComponent<RoverAudio>();
            var jump = context.CreateChild("JumpAudio", root).AddComponent<JumpAudio>();
            var gameplay = context.CreateChild("GameplayAudio", root).AddComponent<GameplayAudio>();
            var friends = context.CreateChild("FriendAudio", root).AddComponent<FriendAudio>();
            var ui = context.CreateChild("UiAudio", root).AddComponent<UiAudio>();
            var radio = context.CreateChild("RadioStation", root).AddComponent<RadioStation>();
            var ambience = context.CreateChild("AmbienceBed", root).AddComponent<AmbienceBed>();

            rover.Wire(roverTuning);
            gameplay.Wire(gameplayTuning);
            ui.Wire(uiTuning);
            friends.Wire(friendTuning);
            jump.Wire(jumpTuning);
            radio.Wire(radioTuning, playlist, tapes);
            director.Wire(library, mixTuning, rover, jump, gameplay, friends, ui, radio, ambience);
            context.AddSystem(director);
        }
    }
}
