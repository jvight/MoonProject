using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Gameplay;
using MoonProject.UI.Editor;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The shipped string tables: English is the complete source, every other language covers exactly its keys with
    /// the same placeholders, keys are stable dotted lower-case names, and every key the game asks for exists.
    /// </summary>
    public sealed class StringTableContentTests
    {
        private static readonly Regex KeyPattern = new Regex(@"^[a-z0-9_]+(\.[a-z0-9_]+)+$");
        private static readonly Regex Placeholder = new Regex(@"\{\d+\}");

        private StringTable[] _tables;
        private StringTable _english;

        [SetUp]
        public void SetUp()
        {
            TextAsset[] assets = StringTableAssets.Load();
            _tables = new StringTable[assets.Length];
            for (int i = 0; i < assets.Length; i++)
            {
                _tables[i] = StringTable.Parse(assets[i].text, assets[i].name);
                Assert.AreEqual(StringTableAssets.LanguageOf(assets[i]), _tables[i].Language,
                    $"{assets[i].name}: the file name is the language code");
            }

            _english = _tables[0];
        }

        [Test]
        public void EnglishIsThePrimaryTable_AndVietnameseShips()
        {
            Assert.AreEqual("en", _english.Language);
            Assert.IsTrue(Array.Exists(_tables, table => table.Language == "vi"));
        }

        [Test]
        public void EveryLanguage_CoversExactlyTheEnglishKeys()
        {
            var english = new HashSet<string>(_english.Keys);
            foreach (StringTable table in _tables)
            {
                var keys = new HashSet<string>(table.Keys);
                var missing = new List<string>(english);
                missing.RemoveAll(keys.Contains);
                var extra = new List<string>(keys);
                extra.RemoveAll(english.Contains);
                CollectionAssert.IsEmpty(missing, $"{table.Language} lacks keys");
                CollectionAssert.IsEmpty(extra, $"{table.Language} has keys English does not define");
            }
        }

        [Test]
        public void EveryText_IsNonEmpty_WithTheSamePlaceholdersAsEnglish()
        {
            foreach (string key in _english.Keys)
            {
                Assert.IsTrue(KeyPattern.IsMatch(key), $"'{key}' is not a dotted lower-case key");
                _english.TryGet(key, out string source);
                foreach (StringTable table in _tables)
                {
                    Assert.IsTrue(table.TryGet(key, out string text), $"{table.Language}: {key}");
                    Assert.IsFalse(string.IsNullOrWhiteSpace(text), $"{table.Language}: '{key}' is empty");
                    CollectionAssert.AreEquivalent(Placeholders(source), Placeholders(text),
                        $"{table.Language}: '{key}' placeholders differ from English");
                }
            }
        }

        [Test]
        public void EveryFixedWordOfTheUxml_HasAKey()
        {
            var layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiAssetPaths.Uxml);
            Assert.IsNotNull(layout, UiAssetPaths.Uxml);
            VisualElement root = layout.Instantiate();
            var localizer = new StaticTextLocalizer(root, new KeyCheck(_english));
            Assert.GreaterOrEqual(localizer.Count, 20, "the pause menu and settings words are localized");
            root.Query<TextElement>().ForEach(element =>
            {
                bool localized = element.ClassListContains(StaticTextLocalizer.MarkerClass);
                bool punctuation = element.text == "/";
                Assert.IsTrue(localized || punctuation || string.IsNullOrEmpty(element.text),
                    $"'{element.text}' is hard-coded in GameUI.uxml");
            });
        }

        [Test]
        public void EveryCodeKey_Exists()
        {
            foreach (string key in new[]
                     {
                         UiKeys.TitleContinueNote, UiKeys.TitleKeyHint,
                         UiKeys.RecipeRow, UiKeys.CargoWaiting, UiKeys.CargoSummary,
                          UiKeys.HintHold, UiKeys.VerbScan, UiKeys.VerbCut, UiKeys.VerbBuild, UiKeys.VerbFit,
                          UiKeys.CargoMoreSlots, UiKeys.CargoEmptyName, UiKeys.CargoEmptyDesc,
                          UiKeys.FaceClose, UiKeys.FaceFit, UiKeys.FaceBuild, UiKeys.CargoDragNotDocked,
                          UiKeys.CargoDragToCutter, UiKeys.CargoDragNowhere, UiKeys.KeyDragOrEnter, UiKeys.KeyMouseOrE,
                          UiKeys.KeyBOrEscape, UiKeys.KeyPadYOrB, UiKeys.KeyB,
                          UiKeys.BuildStripTitle, UiKeys.BuildCostAffordable, UiKeys.BuildCostShort,
                          UiKeys.BuildHintPick, UiKeys.BuildHintLeave, UiKeys.BuildStepPlace, UiKeys.BuildStepWeld,
                          UiKeys.BuildHintWeld, UiKeys.BuildHintDeconstruct, UiKeys.BuildLocked, UiKeys.BuildEnergyCard,
                      })
            {
                Assert.IsTrue(_english.TryGet(key, out _), key);
            }

            foreach (PlacementReason reason in Enum.GetValues(typeof(PlacementReason)))
            {
                string reasonKey = UiKeys.PlacementReasonKey(reason);
                if (reasonKey != null)
                {
                    Assert.IsTrue(_english.TryGet(reasonKey, out _), reasonKey);
                }
            }

            foreach (string id in new[]
                     {
                         StructureIds.ChargingPad, StructureIds.SolarArray, StructureIds.BatteryBank,
                         StructureIds.PowerPylon, StructureIds.AntennaMast,
                     })
            {
                Assert.IsTrue(_english.TryGet(UiKeys.StructureName(id), out _), UiKeys.StructureName(id));
                Assert.IsTrue(_english.TryGet(UiKeys.StructureDescription(id), out _), UiKeys.StructureDescription(id));
            }

            for (int i = 0; i < Materials.Count; i++)
            {
                string key = UiKeys.MaterialName(Materials.At(i));
                Assert.IsTrue(_english.TryGet(key, out _), key);
            }

            foreach (RoverSystem system in Enum.GetValues(typeof(RoverSystem)))
            {
                Assert.IsTrue(_english.TryGet(SystemText.NameKey(system), out _), SystemText.NameKey(system));
                foreach (RoverSystemState state in Enum.GetValues(typeof(RoverSystemState)))
                {
                    Assert.IsTrue(_english.TryGet(SystemText.StateKey(system, state), out _),
                        SystemText.StateKey(system, state));
                }
            }

            for (int device = 0; device < FaceDevices.Count; device++)
            {
                Assert.IsTrue(_english.TryGet(FaceDevices.NameKey(device), out _), FaceDevices.NameKey(device));
                foreach (RoverSystemState state in Enum.GetValues(typeof(RoverSystemState)))
                {
                    string key = FaceDevices.DescriptionKey(device, state, true);
                    Assert.IsTrue(_english.TryGet(key, out _), key);
                    key = FaceDevices.DescriptionKey(device, state, false);
                    Assert.IsTrue(_english.TryGet(key, out _), key);
                }
            }

            foreach (PromptEntry entry in new PromptSettings().Entries)
            {
                Assert.IsTrue(_english.TryGet(UiKeys.Hint(entry.Kind), out _), UiKeys.Hint(entry.Kind));
            }
        }

        private static List<string> Placeholders(string text)
        {
            var found = new List<string>();
            foreach (Match match in Placeholder.Matches(text))
            {
                found.Add(match.Value);
            }

            return found;
        }

        /// <summary>Fails the test for any key the English table lacks.</summary>
        private sealed class KeyCheck : MoonProject.Core.ILocalization
        {
            private readonly StringTable _table;

            public KeyCheck(StringTable table)
            {
                _table = table;
            }

            public string Language => _table.Language;

            public IReadOnlyList<string> Languages => new[] { _table.Language };

            public string GetLanguageName(string language)
            {
                return _table.Name;
            }

            public string Get(string key)
            {
                Assert.IsTrue(_table.TryGet(key, out string text), $"GameUI.uxml uses the unknown key '{key}'");
                return text;
            }

            public bool TryGet(string key, out string text)
            {
                return _table.TryGet(key, out text);
            }

            public void SetLanguage(string language)
            {
                Assert.AreEqual(_table.Language, language);
            }
        }
    }
}
