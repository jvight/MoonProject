using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Audio.Tests
{
    public sealed class AudioLibraryTests
    {
        private AudioLibrary _library;
        private AudioClip _clip;

        [SetUp]
        public void SetUp()
        {
            _library = ScriptableObject.CreateInstance<AudioLibrary>();
            _clip = AudioClip.Create("test", 480, 1, 48000, false);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_library);
            Object.DestroyImmediate(_clip);
        }

        private AudioCue Cue(string id, params AudioClip[] clips)
        {
            var labels = new string[clips.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                labels[i] = $"{id}_{i}";
            }

            return new AudioCue(id, clips, labels, AudioBus.Sfx, true, false, 0.8f, 1f, 1f, 1f);
        }

        [Test]
        public void TryResolve_FindsCuesById_AndDefaultHandleIsInvalid()
        {
            _library.Populate(new[] { Cue("a", _clip), Cue("b", _clip, _clip) });

            Assert.IsTrue(_library.TryResolve("b", out CueHandle handle));
            Assert.AreEqual(2, _library.GetCue(handle).ClipCount);
            Assert.IsFalse(_library.TryResolve("missing", out CueHandle missing));
            Assert.IsFalse(missing.IsValid);
            Assert.IsFalse(default(CueHandle).IsValid);
            Assert.IsNull(_library.FindProblem());
        }

        [Test]
        public void FindProblem_ReportsDuplicatesAndMissingClips()
        {
            _library.Populate(new[] { Cue("a", _clip), Cue("a", _clip) });
            StringAssert.Contains("duplicated", _library.FindProblem());

            _library.Populate(new[] { Cue("a", _clip, null) });
            StringAssert.Contains("missing", _library.FindProblem());

            _library.Populate(new[] { Cue("a") });
            StringAssert.Contains("no clips", _library.FindProblem());
        }

        [Test]
        public void VariantLabels_FollowTheClips_AndMustMatchTheirCount()
        {
            _library.Populate(new[] { Cue("answer", _clip, _clip) });
            Assert.IsTrue(_library.TryResolve("answer", out CueHandle handle));
            Assert.AreEqual("answer_1", _library.GetCue(handle).GetVariantLabel(1));

            _library.Populate(new[]
            {
                new AudioCue("answer", new[] { _clip, _clip }, new[] { "only_one" }, AudioBus.Sfx, true, false,
                    1f, 1f, 1f, 1f),
            });
            StringAssert.Contains("variant label", _library.FindProblem());
        }

        [Test]
        public void EveryCueId_ExistsInTheSfxManifest()
        {
            string manifestPath = Path.Combine(Application.dataPath, "..", "tools", "audio", "sfx_manifest.json");
            Assert.IsTrue(File.Exists(manifestPath), $"missing {manifestPath}");
            string manifest = File.ReadAllText(manifestPath);

            FieldInfo[] ids = typeof(AudioCueIds).GetFields(BindingFlags.Public | BindingFlags.Static);
            Assert.Greater(ids.Length, 0);
            foreach (FieldInfo field in ids)
            {
                string id = (string)field.GetRawConstantValue();
                StringAssert.Contains($"\"id\": \"{id}\"", manifest, $"{field.Name} = '{id}' is not in the manifest");
            }
        }

        [Test]
        public void Playlist_ReportsEmptyAndMissingClips()
        {
            var playlist = ScriptableObject.CreateInstance<RadioPlaylist>();
            try
            {
                StringAssert.Contains("empty", playlist.FindProblem());
                playlist.Populate(new[]
                {
                    new RadioTrack("01", "One", _clip, 76f), new RadioTrack("02", "Two", null, 80f),
                });
                StringAssert.Contains("track 1", playlist.FindProblem());
                playlist.Populate(new[] { new RadioTrack("01", "One", _clip, 76f) });
                Assert.IsNull(playlist.FindProblem());
            }
            finally
            {
                Object.DestroyImmediate(playlist);
            }
        }
    }
}
