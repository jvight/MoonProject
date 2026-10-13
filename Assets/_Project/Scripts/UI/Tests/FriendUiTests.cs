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
    /// The friend UI rules: the repair prompt and the friend texts every language must carry.
    /// </summary>
    public sealed class FriendUiTests
    {
        private const string FriendCatalogPath = "Assets/_Project/Data/Content/FriendCatalog.asset";

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
                InteractionKind newest = tuning.Prompts.Entries[last].Kind;
                entries.DeleteArrayElementAtIndex(last);
                entries.GetArrayElementAtIndex(0).FindPropertyRelative("_dwellSeconds").floatValue = 9f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Assert.IsNull(tuning.Prompts.Find(newest), $"an asset made before {newest} existed");

                Assert.AreEqual(1, tuning.Prompts.AddMissingDefaults());
                Assert.IsNotNull(tuning.Prompts.Find(newest));
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
    }
}
