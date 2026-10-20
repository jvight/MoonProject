using NUnit.Framework;
using UnityEngine;
using MoonProject.Gameplay;
using MoonProject.UI.Editor;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// Every bay of the socket rack (opening v3 4.3) has its words in every language: the part and where 07 saw it, one
    /// short line for the aim chip ("SSR-3 headlamp · from unit 06").
    /// </summary>
    public sealed class RackWordsTests
    {
        private const int MostChars = 40;

        [Test]
        public void EveryRackBay_HasOneShortLine_InEveryLanguage()
        {
            var tuning = ScriptableObject.CreateInstance<OpeningTuning>();
            try
            {
                foreach (TextAsset asset in StringTableAssets.Load())
                {
                    StringTable table = StringTable.Parse(asset.text, asset.name);
                    foreach (RackBaySettings bay in tuning.RackBays)
                    {
                        Assert.IsTrue(table.TryGet(bay.Id, out string words), $"{table.Language}: {bay.Id}");
                        Assert.LessOrEqual(words.Length, MostChars, $"{table.Language}: {bay.Id} stays one line");
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(tuning);
            }
        }
    }
}
