using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Testing;
using MoonProject.UI.Editor;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// 07's databank page (docs/design/databank.md): its index of the archive, the focus walk, the turning model,
    /// the CRT page flip and the page's words.
    /// </summary>
    public sealed class DatabankPageTests
    {
        private const float Step = 0.02f;

        private DatabankSettings _settings;
        private FakeDatabank _databank;
        private LocalizationService _localization;

        [SetUp]
        public void SetUp()
        {
            _settings = new DatabankSettings();
            _databank = new FakeDatabank();
            var en = AssetDatabase.LoadAssetAtPath<TextAsset>(UiAssetPaths.StringTable("en"));
            var vi = AssetDatabase.LoadAssetAtPath<TextAsset>(UiAssetPaths.StringTable("vi"));
            _localization = new LocalizationService(new EventBus(),
                new[] { StringTable.Parse(en.text, "en"), StringTable.Parse(vi.text, "vi") });
        }

        [Test]
        public void TheIndex_SortsEntriesIntoTheirCategories_NewestFirst_AndKnowsWhereACardIsUnread()
        {
            _databank.Unlock(Entry("db.unit07", DatabankCategory.Machines, DatabankEra.Lumen));
            _databank.Unlock(Entry("db.earth_eclipse", DatabankCategory.Phenomena, DatabankEra.None));
            _databank.Unlock(Entry("db.scout3", DatabankCategory.Machines, DatabankEra.Probes));
            var index = new DatabankIndex();
            index.Rebuild(_databank);

            CollectionAssert.AreEqual(new[] { "db.scout3", "db.unit07" }, index.EntriesOf(0), "newest first");
            CollectionAssert.AreEqual(new[] { "db.earth_eclipse" }, index.EntriesOf(3));
            Assert.AreEqual(0, index.EntriesOf(4).Count);
            Assert.IsTrue(index.HasUnread(0, _databank));
            _databank.MarkRead("db.scout3");
            _databank.MarkRead("db.unit07");
            Assert.IsFalse(index.HasUnread(0, _databank), "every machine card read");
        }

        [Test]
        public void TheCursor_WalksTheOpenCategory_StepsOntoTheNext_AndRemembersTheLastCard()
        {
            _databank.Unlock(Entry("db.unit07", DatabankCategory.Machines, DatabankEra.Lumen));
            _databank.Unlock(Entry("db.scout3", DatabankCategory.Machines, DatabankEra.Probes));
            _databank.Unlock(Entry("db.habitat", DatabankCategory.Structures, DatabankEra.Lumen));
            var index = new DatabankIndex();
            index.Rebuild(_databank);
            var cursor = new DatabankCursor(index);

            Assert.AreEqual("db.scout3", cursor.ShownId, "a category's own row shows its newest card");
            cursor.Down();
            Assert.AreEqual(0, cursor.Entry);
            cursor.Down();
            Assert.AreEqual("db.unit07", cursor.ShownId);
            cursor.Down();
            Assert.AreEqual(1, cursor.Category, "past the last machine: the structures open");
            Assert.AreEqual(-1, cursor.Entry);
            cursor.Up();
            Assert.AreEqual(0, cursor.Category, "back up onto the last machine");
            Assert.AreEqual("db.unit07", cursor.ShownId);

            cursor.Focus(1, 0);
            Assert.AreEqual("db.habitat", cursor.ShownId);
            cursor.OpenCategory(4);
            Assert.IsNull(cursor.ShownId, "the archive category is empty");
            cursor.Restore();
            Assert.AreEqual("db.habitat", cursor.ShownId, "the page opens on the last card it showed");
        }

        [Test]
        public void TheTurntable_TurnsSlowlyByItself_ByHandWithinThePitchLimit_AndResumesAfterAWhile()
        {
            var table = new Turntable(_settings);
            table.Reset(20f);
            Run(table, 1f, Vector2.zero);
            Assert.AreEqual(20f + _settings.IdleTurnDegreesPerSecond, table.Yaw, 1f,
                "a slow idle turn (the soft follow trails it)");

            table.Tick(Step, new Vector2(0f, 1000f), Vector2.zero);
            Run(table, 1f, Vector2.zero);
            Assert.AreEqual(_settings.PitchLimitDegrees, table.Pitch, 0.1f, "the tip stops at its limit");
            float held = table.Yaw;
            Run(table, _settings.IdleResumeSeconds * 0.5f, Vector2.zero);
            Assert.AreEqual(held, table.Yaw, 0.2f, "a hand-turned model rests a while");
            Run(table, _settings.IdleResumeSeconds, Vector2.zero);
            Assert.Greater(table.Yaw, held + 0.5f, "then turns by itself again");

            float before = table.Yaw;
            Run(table, 0.5f, new Vector2(1f, 0f));
            Assert.Less(table.Yaw, before - 10f, "the stick turns it");
        }

        [Test]
        public void ThePageChange_FadesTheOldPageDown_SwitchesInTheDark_DrawsTheNewOneIn_AndSettlesOnce()
        {
            var flip = new PageFlip(_settings);
            Assert.AreEqual(FacePage.Hold, flip.Page);
            Assert.AreEqual(0f, flip.TabPosition, "the underline rests under HOLD");
            flip.Toggle();
            Assert.AreEqual(FacePage.Databank, flip.Target);
            int switched = 0;
            int settled = 0;
            float elapsed = 0f;
            float blank = 0f;
            float longestBlank = 0f;
            float underline = 0f;
            while (flip.IsFlipping)
            {
                Assert.Less(elapsed, 1f, "the change ends");
                flip.Tick(Step);
                elapsed += Step;
                if (flip.Switched)
                {
                    switched++;
                    Assert.AreEqual(0f, flip.Drawn, 0.05f, "the new page starts drawing from the top");
                }

                if (flip.IsFlipping && !flip.Switched && flip.Page == FacePage.Hold)
                {
                    Assert.AreEqual(1f, flip.Drawn, "the old page is never cut, only faded");
                }

                settled += flip.Settled ? 1 : 0;
                Assert.GreaterOrEqual(flip.TabPosition, underline - 1e-4f, "the underline glides one way");
                underline = flip.TabPosition;
                bool dark = flip.ContentAlpha * flip.Drawn < 0.1f;
                blank = dark ? blank + Step : 0f;
                longestBlank = Mathf.Max(longestBlank, blank);
            }

            Assert.AreEqual(1, switched, "the page changes once, in the dark");
            Assert.AreEqual(1, settled, "and settles exactly once (the tube's thunk)");
            Assert.LessOrEqual(flip.Duration, 0.35f, "the whole change takes at most 350 ms");
            Assert.AreEqual(flip.Duration, elapsed, Step * 1.5f);
            Assert.LessOrEqual(longestBlank, 0.15f, "the tube is never dark for long");
            Assert.AreEqual(FacePage.Databank, flip.Page);
            Assert.AreEqual(1f, flip.TabPosition, "the underline has arrived under DATABANK");
            Assert.AreEqual(1f, flip.ContentAlpha);
            Assert.AreEqual(1f, flip.Drawn);
        }

        [Test]
        public void APress_DuringTheChange_NeverFlipsTwice()
        {
            var flip = new PageFlip(_settings);
            flip.Toggle();
            int switched = 0;
            int settled = 0;
            while (flip.IsFlipping)
            {
                flip.Toggle();
                flip.Tick(Step);
                switched += flip.Switched ? 1 : 0;
                settled += flip.Settled ? 1 : 0;
            }

            Assert.AreEqual(1, switched, "a press while the page changes is ignored");
            Assert.AreEqual(1, settled);
            Assert.AreEqual(FacePage.Databank, flip.Page, "it lands on the page the first press asked for");

            flip.Toggle();
            flip.Snap(FacePage.Hold);
            Assert.AreEqual(FacePage.Hold, flip.Page, "closing the face mid-change rests on the hold");
            Assert.IsFalse(flip.IsFlipping);
        }

        [TestCase("en", "MAKER · VIS   YEAR · 1967   ERA · PROBES")]
        [TestCase("vi", "HÃNG · VIS   NĂM · 1967   THỜI KỲ · TÀU THĂM DÒ")]
        public void TheFieldRow_ReadsMakerYearAndEra(string language, string expected)
        {
            _localization.SetLanguage(language);
            var text = new DatabankText(_localization);
            DatabankEntryInfo scout = Entry("db.scout3", DatabankCategory.Machines, DatabankEra.Probes,
                DatabankMaker.VIS, "1967");
            Assert.AreEqual(expected, text.FieldRow(scout));
        }

        [Test]
        public void ASet_ShowsItsScanCount_ThenItsBlueprintAndHistory_AndNoCardReadsNoLumenRecord()
        {
            var text = new DatabankText(_localization);
            DatabankEntryInfo array = Entry("db.solar_array", DatabankCategory.Structures, DatabankEra.Lumen,
                nameKey: "structure.solar_array.name", historyKey: "structure.solar_array.desc");
            Assert.AreEqual("SCAN 1/3", text.SetLine(array, new DatabankProgress(1, 3)));
            Assert.AreEqual(string.Empty, text.History(array, new DatabankProgress(1, 3)),
                "the card waits for the whole set");
            Assert.AreEqual("BLUEPRINT · SOLAR ARRAY", text.SetLine(array, new DatabankProgress(3, 3)));
            Assert.AreEqual(_localization.Get("structure.solar_array.desc"),
                text.History(array, new DatabankProgress(3, 3)));

            DatabankEntryInfo unknown = Entry("db.mystery", DatabankCategory.Machines, DatabankEra.Now, historyKey: "");
            Assert.AreEqual("NO LUMEN RECORD", text.History(unknown, new DatabankProgress(1, 1)));
            Assert.AreEqual(string.Empty, text.SetLine(unknown, new DatabankProgress(1, 1)),
                "a whole entry has no set line");
            Assert.AreEqual("Added to databank: Solar Array", text.Toast(array));
            _localization.SetLanguage("vi");
            Assert.AreEqual("Đã thêm vào dữ liệu: Giàn pin mặt trời", text.Toast(array));
        }

        private static DatabankEntryInfo Entry(string id, DatabankCategory category, DatabankEra era,
            DatabankMaker maker = DatabankMaker.None, string year = "1984",
            string nameKey = "structure.solar_array.name", string historyKey = "structure.solar_array.desc")
        {
            return new DatabankEntryInfo(id, category, nameKey, maker, year, era, "ui.readout.power_on", historyKey,
                DatabankViewKind.Model, null, null, 0f, null);
        }

        private static void Run(Turntable table, float seconds, Vector2 stick)
        {
            for (float t = 0f; t < seconds; t += Step)
            {
                table.Tick(Step, Vector2.zero, stick);
            }
        }
    }
}
