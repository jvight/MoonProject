using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Audio.Tests
{
    public sealed class SoundscapeModelTests
    {
        private const float Edge = 200f;
        private const float Settled = 60f;
        private const float DbTolerance = 0.05f;

        private SoundscapeTuning _tuning;
        private CanyonAudioTuning _canyon;
        private SoundscapeModel _model;

        [SetUp]
        public void SetUp()
        {
            _tuning = ScriptableObject.CreateInstance<SoundscapeTuning>();
            _canyon = ScriptableObject.CreateInstance<CanyonAudioTuning>();
            _model = new SoundscapeModel(_tuning, _canyon);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tuning);
            Object.DestroyImmediate(_canyon);
        }

        private static float Db(float gain)
        {
            return SoundscapeModel.ToDb(gain);
        }

        private void At(float distance, float stillness = 0f, float canyon = 0f, bool quietHours = false,
            float wide = 0f)
        {
            _model.Step(distance, Edge, stillness, wide, canyon, quietHours, Settled);
        }

        [Test]
        public void AtHome_EveryLayerIsAtItsOwnLevel_TheRoomToneAndSmallSoundsTucked()
        {
            At(0f);
            Assert.AreEqual(0f, _model.Solitude);
            Assert.AreEqual(0f, Db(_model.RadioGain), DbTolerance);
            Assert.AreEqual(0f, Db(_model.BasinBedGain), DbTolerance);
            Assert.AreEqual(0f, Db(_model.CanyonBedGain), DbTolerance);
            Assert.AreEqual(_tuning.RoomToneNearDb, Db(_model.RoomToneGain), DbTolerance);
            Assert.AreEqual(_tuning.SmallSoundsNearDb, Db(_model.SmallSoundsGain), DbTolerance);
        }

        [Test]
        public void InsideTheSignal_NothingChanges_PastIt_TheRadioFadesToNearSilence_AndTheRoomToneTakesOver()
        {
            At(Edge);
            Assert.AreEqual(0f, _model.Farness, "the clear zone and the static zone are the radio's own business");

            At(Edge + _tuning.SilenceWidth * 0.5f);
            Assert.AreEqual(0.5f, _model.Farness, 1e-4f);
            Assert.AreEqual(_tuning.FarRadioDb * 0.5f, Db(_model.RadioGain), DbTolerance);

            At(Edge + _tuning.SilenceWidth);
            Assert.AreEqual(1f, _model.Solitude, 1e-4f);
            Assert.AreEqual(_tuning.FarRadioDb, Db(_model.RadioGain), DbTolerance);
            Assert.AreEqual(_tuning.FarBasinDb, Db(_model.BasinBedGain), DbTolerance);
            Assert.AreEqual(_tuning.RoomToneFarDb, Db(_model.RoomToneGain), DbTolerance);
            Assert.AreEqual(0f, Db(_model.SmallSoundsGain), DbTolerance, "07's own sounds come forward");
            Assert.Greater(_model.RoomToneGain, _model.BasinBedGain, "the room tone is the main bed out here");
        }

        [Test]
        public void DrivingAway_FadesSmoothly_AndComingHomeGivesItAllBack()
        {
            float lastRadio = 0f;
            float lastRoom = float.NegativeInfinity;
            for (float d = 0f; d <= Edge + _tuning.SilenceWidth + 50f; d += 1f)
            {
                At(d);
                float radio = Db(_model.RadioGain);
                float room = Db(_model.RoomToneGain);
                Assert.LessOrEqual(radio, lastRadio + 1e-4f, $"radio at {d} m");
                Assert.GreaterOrEqual(room, lastRoom - 1e-4f, $"room tone at {d} m");
                Assert.Less(lastRadio - radio, 0.5f, $"no step in the radio at {d} m");
                lastRadio = radio;
                lastRoom = room;
            }

            At(0f);
            Assert.AreEqual(0f, Db(_model.RadioGain), DbTolerance, "home again");
        }

        [Test]
        public void ATallerTower_PushesTheSilenceOut()
        {
            float far = Edge + _tuning.SilenceWidth;
            _model.Step(far, Edge + 300f, 0f, 0f, 0f, false, Settled);
            Assert.AreEqual(0f, _model.Farness, "a wider signal brings this spot back in range");
        }

        [Test]
        public void Stillness_PullsTheWorldBack_TheRoomToneAnd07Stay()
        {
            At(0f);
            float room = _model.RoomToneGain;
            float small = _model.SmallSoundsGain;

            At(0f, 1f);
            Assert.AreEqual(_tuning.StillDuckDb, Db(_model.RadioGain), DbTolerance);
            Assert.AreEqual(_tuning.StillDuckDb, Db(_model.BasinBedGain), DbTolerance);
            Assert.AreEqual(_tuning.StillDuckDb, Db(_model.CanyonBedGain), DbTolerance);
            Assert.AreEqual(room, _model.RoomToneGain, 1e-6f, "the space stays: it feels wider");
            Assert.AreEqual(small, _model.SmallSoundsGain, 1e-6f);
        }

        [Test]
        public void TheWideShot_BreathesOutFurther_TheRoomToneOpensWithTheFrame()
        {
            At(0f, 1f);
            float radio = Db(_model.RadioGain);
            float basin = Db(_model.BasinBedGain);
            float canyonBeds = Db(_model.CanyonBedGain);
            float room = Db(_model.RoomToneGain);
            float small = _model.SmallSoundsGain;

            At(0f, 1f, wide: 1f);
            Assert.AreEqual(radio + _tuning.WideDuckDb, Db(_model.RadioGain), DbTolerance);
            Assert.AreEqual(basin + _tuning.WideDuckDb, Db(_model.BasinBedGain), DbTolerance);
            Assert.AreEqual(canyonBeds + _tuning.WideDuckDb, Db(_model.CanyonBedGain), DbTolerance);
            Assert.AreEqual(room + _tuning.WideRoomToneDb, Db(_model.RoomToneGain), DbTolerance);
            Assert.AreEqual(small, _model.SmallSoundsGain, 1e-6f, "07 stays as close as ever");
            Assert.LessOrEqual(_tuning.WideDuckDb, -2f, "a gentle -2 to -3 dB");
            Assert.GreaterOrEqual(_tuning.WideDuckDb, -3f);

            At(0f, 1f, wide: 0.5f);
            Assert.AreEqual(radio + 0.5f * _tuning.WideDuckDb, Db(_model.RadioGain), DbTolerance, "follows the frame");
        }

        [Test]
        public void QuietHours_EasesIn_SwellsTheBasin_AndBringsTheRoomAnd07Forward()
        {
            _model.Step(0f, Edge, 0f, 0f, 0f, true, 0.1f);
            Assert.Greater(_model.QuietHours, 0f);
            Assert.Less(_model.QuietHours, 0.1f, "eased, never a switch");

            At(0f, quietHours: true);
            Assert.AreEqual(1f, _model.QuietHours, 1e-4f);
            Assert.AreEqual(_tuning.QuietHoursBasinDb, Db(_model.BasinBedGain), DbTolerance);
            Assert.AreEqual(_tuning.QuietHoursSolitude, _model.Solitude, 1e-4f);
            float expectedRoom = Mathf.Lerp(_tuning.RoomToneNearDb, _tuning.RoomToneFarDb, _tuning.QuietHoursSolitude);
            Assert.AreEqual(expectedRoom, Db(_model.RoomToneGain), DbTolerance);
            Assert.Greater(Db(_model.SmallSoundsGain), _tuning.SmallSoundsNearDb);
        }

        [Test]
        public void InTheCanyon_TheRadioThins_TheBasinRecedes_AndTheWhisperIsTheRoomsAir()
        {
            At(0f, canyon: 1f);
            Assert.AreEqual(SoundscapeModel.ToDb(_canyon.RadioMusicInside), Db(_model.RadioGain), DbTolerance);
            Assert.AreEqual(SoundscapeModel.ToDb(_canyon.BasinBedInside), Db(_model.BasinBedGain), DbTolerance);
            Assert.AreEqual(_tuning.CanyonSolitude, _model.Solitude, 1e-4f);
            float room = Mathf.Lerp(_tuning.RoomToneNearDb, _tuning.RoomToneFarDb, _tuning.CanyonSolitude) +
                         _tuning.RoomToneInCanyonDb;
            Assert.AreEqual(room, Db(_model.RoomToneGain), DbTolerance);
            Assert.AreEqual(_canyon.RadioCutoffInside, _model.RadioCutoff(22000f), 1f);

            At(0f, canyon: 0f);
            Assert.AreEqual(22000f, _model.RadioCutoff(22000f));
        }

        [Test]
        public void TheLayers_AddUp_FarStillAndQuiet()
        {
            At(Edge + _tuning.SilenceWidth, 1f, 0f, true);
            Assert.AreEqual(_tuning.FarRadioDb + _tuning.StillDuckDb, Db(_model.RadioGain), DbTolerance);
            Assert.AreEqual(_tuning.FarBasinDb + _tuning.QuietHoursBasinDb + _tuning.StillDuckDb,
                Db(_model.BasinBedGain), DbTolerance);
            Assert.AreEqual(1f, _model.Solitude, 1e-4f);
        }

        [Test]
        public void Decibels_RoundTrip()
        {
            Assert.AreEqual(1f, SoundscapeModel.FromDb(0f), 1e-6f);
            Assert.AreEqual(0.5f, SoundscapeModel.FromDb(SoundscapeModel.ToDb(0.5f)), 1e-5f);
            Assert.AreEqual(-100f, SoundscapeModel.ToDb(0f), 1e-3f);
        }
    }
}
