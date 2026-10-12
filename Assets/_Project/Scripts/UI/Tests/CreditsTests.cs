using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MoonProject.UI.Editor;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The credits data (UI/Credits/credits.json) is complete and true to the repo: every font file in UI/Fonts has an
    /// entry, each font's copyright line is the one in its OFL file, every licence text it names is present, and every
    /// localized line it uses has a key in both languages.
    /// </summary>
    public sealed class CreditsTests
    {
        private const string LicensesFolder = "Assets/_Project/UI/Credits/Licenses";

        private static readonly string[] RequiredLicences =
        {
            "OFL-NunitoSans.txt", "OFL-Exo2.txt", "OFL-BeVietnamPro.txt", "CC-BY-4.0.txt", "Apache-2.0.txt",
        };

        [Test]
        public void EveryFontFile_HasACreditsEntry_WithItsOwnCopyrightLine()
        {
            CreditsData credits = Load();
            var families = new Dictionary<string, CreditsEntry>();
            foreach (CreditsSection section in credits.sections)
            {
                foreach (CreditsEntry entry in section.entries)
                {
                    if (!string.IsNullOrEmpty(entry.family))
                    {
                        families[entry.family] = entry;
                    }
                }
            }

            foreach (string path in Directory.GetFiles(UiAssetPaths.FontFolder, "*.ttf"))
            {
                string family = Path.GetFileNameWithoutExtension(path).Split('-')[0];
                Assert.IsTrue(families.TryGetValue(family, out CreditsEntry entry), $"{path} has no credits entry");
                Assert.IsNotEmpty(entry.name, family);
                StringAssert.Contains("SIL Open Font License", entry.detail, family);
                string licence = File.ReadAllText(Path.Combine(LicensesFolder, "OFL-" + family + ".txt"));
                StringAssert.Contains(entry.note, licence, $"{family}: the credit's copyright line is not the OFL file's");
            }
        }

        [Test]
        public void EveryLicenceTheCreditsNames_IsInTheLicensesFolder()
        {
            foreach (string file in RequiredLicences)
            {
                string path = Path.Combine(LicensesFolder, file);
                Assert.IsTrue(File.Exists(path), path + " is missing");
                Assert.Greater(new FileInfo(path).Length, 1000, path + " looks empty");
            }
        }

        [Test]
        public void EveryLocalizedLine_HasAKeyInEveryLanguage()
        {
            CreditsData credits = Load();
            var keys = new List<string>();
            foreach (CreditsSection section in credits.sections)
            {
                keys.Add(section.headingKey);
                foreach (CreditsEntry entry in section.entries)
                {
                    Assert.IsTrue(!string.IsNullOrEmpty(entry.name) ^ !string.IsNullOrEmpty(entry.nameKey),
                        "an entry has a name or a name key, not both and not neither");
                    if (!string.IsNullOrEmpty(entry.nameKey))
                    {
                        keys.Add(entry.nameKey);
                    }

                    if (!string.IsNullOrEmpty(entry.detailKey))
                    {
                        keys.Add(entry.detailKey);
                    }
                }
            }

            foreach (TextAsset table in StringTableAssets.Load())
            {
                StringTable parsed = StringTable.Parse(table.text, table.name);
                foreach (string key in keys)
                {
                    Assert.IsTrue(parsed.TryGet(key, out _), $"{table.name} lacks \"{key}\"");
                }
            }
        }

        [Test]
        public void TheCredits_NameTheStationVoiceAndItsLicence()
        {
            string json = File.ReadAllText(UiAssetPaths.Credits);
            StringAssert.Contains("CC BY 4.0", json);
            StringAssert.Contains("LibriTTS-R", json);
            StringAssert.Contains("sherpa-onnx", json);
        }

        private static CreditsData Load()
        {
            return JsonUtility.FromJson<CreditsData>(File.ReadAllText(UiAssetPaths.Credits));
        }
    }
}
