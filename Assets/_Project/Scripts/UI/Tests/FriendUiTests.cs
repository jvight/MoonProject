using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MoonProject.Core.Input;
using MoonProject.Gameplay;
using MoonProject.UI.Editor;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The friend UI rules: which broken friend the parts readout speaks about, how its pips fill, the repair prompt,
    /// and the friend texts every language must carry.
    /// </summary>
    public sealed class FriendUiTests
    {
        private const float Frame = 1f / 60f;
        private const string FriendCatalogPath = "Assets/_Project/Data/Content/FriendCatalog.asset";

        private FriendUiSettings _settings;
        private Statuses _friends;
        private FriendFocus _focus;

        [SetUp]
        public void SetUp()
        {
            _settings = new FriendUiSettings();
            _friends = new Statuses();
            _focus = new FriendFocus(_settings);
        }

        [Test]
        public void Focus_PicksTheNearestBrokenFriend_WithinTheShowDistance()
        {
            _friends.Add(FriendState.Dormant, new Vector3(30f, 0f, 0f));
            _friends.Add(FriendState.PartsGathering, new Vector3(0f, 0f, 10f));
            _friends.Add(FriendState.Awake, new Vector3(1f, 0f, 0f));

            Assert.AreEqual(1, _focus.Step(_friends, Vector3.zero), "the nearer broken friend; never an awake one");
            Assert.AreEqual(FriendFocus.None, _focus.Step(_friends, new Vector3(0f, 0f, -40f)), "nothing nearby");
        }

        [Test]
        public void Focus_IgnoresHeight_AndHoldsUntilTheHideDistance()
        {
            _friends.Add(FriendState.Dormant, new Vector3(0f, 25f, 0f));
            float between = (_settings.ShowDistance + _settings.HideDistance) * 0.5f;

            Assert.AreEqual(FriendFocus.None, _focus.Step(_friends, new Vector3(between, 0f, 0f)),
                "not shown until 07 is within the show distance");
            Assert.AreEqual(0, _focus.Step(_friends, new Vector3(_settings.ShowDistance - 1f, 0f, 0f)),
                "a friend in a crater below 07 still counts (horizontal distance)");
            Assert.AreEqual(0, _focus.Step(_friends, new Vector3(between, 0f, 0f)), "held between the two distances");
            Assert.AreEqual(FriendFocus.None, _focus.Step(_friends, new Vector3(_settings.HideDistance + 1f, 0f, 0f)));
        }

        [Test]
        public void Focus_LetsGo_OnceTheFriendIsBeingRepaired()
        {
            _friends.Add(FriendState.PartsGathering, Vector3.forward);
            Assert.AreEqual(0, _focus.Step(_friends, Vector3.zero));
            _friends.Set(0, FriendState.Repairing);
            Assert.AreEqual(FriendFocus.None, _focus.Step(_friends, Vector3.zero));
        }

        [Test]
        public void Pips_FillOneAfterAnother_NeverPopping()
        {
            var fill = new PipFill(_settings);
            fill.Snap(1);
            Assert.AreEqual(1f, fill.Of(0));
            Assert.AreEqual(0f, fill.Of(1));

            Assert.IsTrue(fill.Step(2, Frame));
            Assert.That(fill.Of(1), Is.InRange(0f, 0.1f), "a new part starts filling gently");
            for (float t = 0f; t < _settings.PipFillSeconds; t += Frame)
            {
                fill.Step(2, Frame);
            }

            Assert.AreEqual(1f, fill.Of(1), 1e-4f);
            Assert.AreEqual(0f, fill.Of(2), "the next pip is still empty");
            Assert.IsFalse(fill.Step(2, Frame), "settled: nothing to write");
        }

        [Test]
        public void RepairPrompt_NamesTheDigButton_LikeEveryOtherTeachableAction()
        {
            PromptEntry repair = new PromptSettings().Find(InteractionKind.Repair);
            Assert.IsNotNull(repair, "repairing a friend is taught the first few times");
            Assert.AreEqual(RoverAction.Excavate, repair.Action, "hold E / X, like digging");
        }

        [Test]
        public void TuningMigration_AddsANewKindsDefault_KeepingHandTunedEntries()
        {
            var tuning = ScriptableObject.CreateInstance<UiTuning>();
            try
            {
                var serialized = new SerializedObject(tuning);
                SerializedProperty entries = serialized.FindProperty("_prompts._entries");
                int last = entries.arraySize - 1;
                Assert.AreEqual(InteractionKind.Repair, tuning.Prompts.Entries[last].Kind);
                entries.DeleteArrayElementAtIndex(last);
                entries.GetArrayElementAtIndex(0).FindPropertyRelative("_dwellSeconds").floatValue = 9f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.IsNull(tuning.Prompts.Find(InteractionKind.Repair), "an asset made before Repair existed");

                Assert.AreEqual(1, tuning.Prompts.AddMissingDefaults());
                Assert.IsNotNull(tuning.Prompts.Find(InteractionKind.Repair));
                Assert.AreEqual(9f, tuning.Prompts.Entries[0].DwellSeconds, "hand tuning survives");
                Assert.AreEqual(0, tuning.Prompts.AddMissingDefaults(), "idempotent");
                Assert.IsNull(tuning.Validate());
            }
            finally
            {
                Object.DestroyImmediate(tuning);
            }
        }

        [Test]
        public void EveryFriend_HasANameAndARepairLog_InEveryLanguage()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<FriendCatalog>(FriendCatalogPath);
            Assert.IsNotNull(catalog, FriendCatalogPath);
            var ids = new List<string>();
            foreach (FriendDefinition friend in catalog.Friends)
            {
                ids.Add(friend.Id);
            }

            Assert.Contains("tilly", ids);
            foreach (TextAsset asset in StringTableAssets.Load())
            {
                StringTable table = StringTable.Parse(asset.text, asset.name);
                foreach (string id in ids)
                {
                    Assert.IsTrue(table.TryGet(UiKeys.FriendName(id), out _), $"{table.Language}: {id} name");
                    Assert.IsTrue(table.TryGet(UiKeys.FriendRepairLog(id), out string log),
                        $"{table.Language}: {id} repair log");
                    Assert.Less(log.Length, 220, "a log is a sticky note, one or two sentences");
                }

                Assert.IsTrue(table.TryGet(UiKeys.Hint(InteractionKind.Repair), out _), table.Language);
                Assert.IsTrue(table.TryGet(UiKeys.LogCaption, out _), table.Language);
            }
        }

        /// <summary>A scripted friend roster.</summary>
        private sealed class Statuses : IFriendStatuses
        {
            private readonly List<FriendStatus> _statuses = new List<FriendStatus>();

            public int Count => _statuses.Count;

            public void Add(FriendState state, Vector3 position)
            {
                _statuses.Add(new FriendStatus(state, 0, 3, true, false, position));
            }

            public void Set(int index, FriendState state)
            {
                FriendStatus old = _statuses[index];
                _statuses[index] = new FriendStatus(state, old.Collected, old.Total, true, false, old.Position);
            }

            public FriendDefinition Definition(int index)
            {
                return null;
            }

            public FriendStatus Status(int index)
            {
                return _statuses[index];
            }
        }
    }
}
