using NUnit.Framework;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The HUD reveals itself one verb at a time: nothing is met in a fresh save, each verb is remembered once met,
    /// the save carries them (additively, so an older save starts with none), and unknown names are skipped.
    /// </summary>
    public sealed class HudVerbTests
    {
        [Test]
        public void Verbs_StartUnmet_AreRememberedOnceMet_AndRoundTripThroughTheSave()
        {
            var ledger = new PromptLedger(new PromptSettings());
            foreach (HudVerb verb in new[] { HudVerb.Material, HudVerb.Ping, HudVerb.LitMast, HudVerb.Tape })
            {
                Assert.IsFalse(ledger.Met(verb), verb.ToString());
            }

            ledger.RecordMet(HudVerb.Material);
            ledger.RecordMet(HudVerb.LitMast);
            PromptsSaveData data = ledger.Capture();
            CollectionAssert.AreEquivalent(new[] { "Material", "LitMast" }, data.metVerbs);

            var loaded = new PromptLedger(new PromptSettings());
            data.metVerbs = new[] { "Material", "LitMast", "SomethingFromTheFuture" };
            loaded.Restore(data);
            Assert.IsTrue(loaded.Met(HudVerb.Material));
            Assert.IsTrue(loaded.Met(HudVerb.LitMast));
            Assert.IsFalse(loaded.Met(HudVerb.Ping));

            loaded.Restore(new PromptsSaveData());
            Assert.IsFalse(loaded.Met(HudVerb.Material), "a save from before the verbs has met none");
        }
    }
}
