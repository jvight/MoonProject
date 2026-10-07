using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// The friend framework's M3-05 extensions: items a repair needs next to parts (Bell's cassette), the first
    /// homecoming, a home on the radio tower, and the save migration.
    /// </summary>
    public sealed class FriendItemTests
    {
        private readonly List<Object> _created = new List<Object>();
        [TearDown]
        public void TearDown()
        {
            foreach (Object created in _created)
            {
                Object.DestroyImmediate(created);
            }

            _created.Clear();
        }

        [Test]
        public void Progress_NeedsItsItemsAsWellAsItsParts()
        {
            var bell = new FriendProgress("bell", 3, 1);
            Assert.AreEqual(1, bell.ItemCount);
            for (int part = 0; part < 3; part++)
            {
                bell.CollectPart(part);
            }

            Assert.IsTrue(bell.AllPartsGathered);
            Assert.IsFalse(bell.AllGathered, "the tape is still missing");
            Assert.IsFalse(bell.CanRepair);
            Assert.Throws<InvalidOperationException>(bell.BeginRepair);
            Assert.IsTrue(bell.CollectItem(0));
            Assert.IsFalse(bell.CollectItem(0), "held once");
            Assert.AreEqual(1, bell.ItemsCollected);
            Assert.IsTrue(bell.CanRepair);
            Assert.Throws<ArgumentOutOfRangeException>(() => bell.CollectItem(1));

            var tapeFirst = new FriendProgress("bell", 3, 1);
            Assert.IsTrue(tapeFirst.CollectItem(0));
            Assert.AreEqual(FriendState.PartsGathering, tapeFirst.State, "the tape counts before any part");
            Assert.AreEqual(0, tapeFirst.Collected);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new FriendProgress("moss", 3, FriendProgress.MaxItems + 1));
        }

        [Test]
        public void Progress_WelcomesOnlyOnce_AndOnlyWhenAwake()
        {
            var bell = new FriendProgress("bell", 1, 1);
            Assert.IsFalse(bell.Welcome(), "a broken friend has no homecoming");
            bell.CollectPart(0);
            bell.CollectItem(0);
            bell.BeginRepair();
            bell.FinishRepair();
            Assert.IsTrue(bell.Welcome(), "the first homecoming");
            Assert.IsTrue(bell.Welcomed);
            Assert.IsFalse(bell.Welcome(), "only once");
        }

        [Test]
        public void Progress_SavesItemsAndTheFirstHomecoming()
        {
            var bell = new FriendProgress("bell", 3, 1);
            bell.CollectPart(2);
            bell.CollectItem(0);
            FriendSaveData data = JsonUtility.FromJson<FriendSaveData>(JsonUtility.ToJson(bell.Capture()));
            var loaded = new FriendProgress("bell", 3, 1);
            loaded.Restore(data);
            Assert.AreEqual(FriendState.PartsGathering, loaded.State);
            Assert.IsTrue(loaded.IsCollected(2));
            Assert.IsTrue(loaded.IsItemCollected(0));

            var awake = new FriendProgress("bell", 3, 1);
            awake.Restore(new FriendSaveData { id = "bell", state = (int)FriendState.Awake, welcomed = true });
            Assert.AreEqual(1, awake.ItemsCollected, "awake means everything was there");
            Assert.IsTrue(awake.Welcomed);

            var broken = new FriendProgress("bell", 3, 1);
            broken.Restore(new FriendSaveData { id = "bell", items = 0xFF, welcomed = true });
            Assert.AreEqual(1, broken.ItemsCollected, "foreign bits are ignored");
            Assert.IsFalse(broken.Welcomed, "no homecoming before the repair");
            Assert.AreEqual(FriendState.PartsGathering, broken.State, "the state follows the items");
        }

        [Test]
        public void Migration_ReadsAVersionOneFriendAsIs()
        {
            const string v1 = "{\"friends\":[{\"id\":\"tilly\",\"state\":1,\"parts\":3,\"discovered\":true}]}";
            string v2 = FriendsSaveMigrations.Migrate(v1, 1);
            FriendsSaveData data = JsonUtility.FromJson<FriendsSaveData>(v2);
            var tilly = new FriendProgress("tilly", 3);
            tilly.Restore(data.friends[0]);
            Assert.AreEqual(FriendState.PartsGathering, tilly.State);
            Assert.AreEqual(2, tilly.Collected);
            Assert.IsTrue(tilly.Discovered);
            Assert.IsFalse(tilly.Welcomed);
            Assert.Throws<InvalidOperationException>(() => FriendsSaveMigrations.Migrate(v2, 2));
            Assert.AreEqual(2, GameplaySaveKeys.FriendsVersion);
        }

        [Test]
        public void Definition_ReportsBadItemsAndHomes()
        {
            FriendDefinition bell = Bell(new[] { "after_dark_1" }, FriendHome.RadioTower);
            Assert.IsNull(bell.Validate());
            Assert.AreEqual(FriendHome.RadioTower, bell.Home);
            Assert.AreEqual("after_dark_1", bell.Items[0]);
            Assert.IsTrue(bell.AnnouncesHomecoming);
            StringAssert.Contains("twice", Bell(new[] { "tape", "tape" }, FriendHome.RadioTower).Validate());
            StringAssert.Contains("at most", Bell(new[] { "a", "b", "c", "d" }, FriendHome.Lander).Validate());
            StringAssert.Contains("no id", Bell(new[] { " " }, FriendHome.Lander).Validate());
            StringAssert.Contains("unknown home", Bell(Array.Empty<string>(), (FriendHome)9).Validate());
        }

        private FriendDefinition Bell(string[] items, FriendHome home)
        {
            GameObject model = Track(new GameObject("Bell"));
            var bell = Track(ScriptableObject.CreateInstance<FriendDefinition>());
            bell.Populate("bell", model, model, new[]
                {
                    new FriendPart("knob", model), new FriendPart("cone", model), new FriendPart("valve", model),
                }, items, home, "BellCorner", true, FriendDefinition.SpotterAbility, 4f, "bell",
                new FriendPlacement(5, new Vector2(10f, 20f), 0f, 30f, Vector2.zero, 5f, false, new Vector2(5f, 9f)));
            return bell;
        }

        private T Track<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
