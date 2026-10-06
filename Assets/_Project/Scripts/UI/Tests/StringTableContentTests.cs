using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
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
            Assert.IsTrue(System.Array.Exists(_tables, table => table.Language == "vi"));
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
                         UiKeys.CardCaption, UiKeys.CardClose, UiKeys.TowerLevel, UiKeys.TowerHold, UiKeys.TowerNeed,
                         UiKeys.TowerPurchased,
                     })
            {
                Assert.IsTrue(_english.TryGet(key, out _), key);
            }

            foreach (PromptEntry entry in new PromptSettings().Entries)
            {
                Assert.IsTrue(_english.TryGet(UiKeys.Hint(entry.Kind), out _), UiKeys.Hint(entry.Kind));
            }
        }

        [Test]
        public void EveryRelic_HasANameAndAMemory()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<RelicCatalog>(UiAssetPaths.RelicCatalog);
            Assert.IsNotNull(catalog, UiAssetPaths.RelicCatalog);
            Assert.Greater(catalog.Relics.Count, 0);
            foreach (RelicDefinition relic in catalog.Relics)
            {
                Assert.IsTrue(_english.TryGet(UiKeys.RelicName(relic.Id), out _), UiKeys.RelicName(relic.Id));
                Assert.IsTrue(_english.TryGet(UiKeys.RelicMemory(relic.Id), out _), UiKeys.RelicMemory(relic.Id));
            }
        }

        [Test]
        public void EveryUpgradeLevel_HasATitleAndAnEffect()
        {
            string[] guids = AssetDatabase.FindAssets("t:UpgradeDefinition", new[] { "Assets/_Project/Data/Content" });
            Assert.Greater(guids.Length, 0, "the radio tower upgrade exists");
            foreach (string guid in guids)
            {
                var upgrade = AssetDatabase.LoadAssetAtPath<UpgradeDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.IsTrue(_english.TryGet(UiKeys.UpgradeName(upgrade.Id), out _), UiKeys.UpgradeName(upgrade.Id));
                for (int level = 1; level <= upgrade.MaxLevel; level++)
                {
                    Assert.IsTrue(_english.TryGet(UiKeys.UpgradeTitle(upgrade.Id, level), out _),
                        UiKeys.UpgradeTitle(upgrade.Id, level));
                    Assert.IsTrue(_english.TryGet(UiKeys.UpgradeEffect(upgrade.Id, level), out _),
                        UiKeys.UpgradeEffect(upgrade.Id, level));
                }
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
