using NUnit.Framework;
using UnityEngine.UIElements;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// While 07 stargazes the ambient HUD slowly leaves and soon comes back; the Look up hint shows once per save, the
    /// first time 07 rests under the sky, and steps aside when the player looks up.
    /// </summary>
    public sealed class StargazeTests
    {
        private const float Step = 0.1f;

        [Test]
        public void TheAmbientHud_LeavesSlowly_AndComesBackSooner()
        {
            var settings = new StargazeSettings();
            var prompt = new VisualElement();
            var ticker = new VisualElement();
            var veil = new StargazeVeil(settings.Hud, prompt, ticker);
            veil.Tick(Step);
            Assert.IsFalse(veil.IsVeiled);
            Assert.AreEqual(1f, veil.Opacity);

            veil.SetStargazing(true);
            Ticks(veil, settings.Hud.FadeOut * 0.5f);
            Assert.IsTrue(veil.IsVeiled);
            Assert.That(prompt.style.opacity.value, Is.GreaterThan(0f).And.LessThan(1f), "it eases, never cuts");
            Assert.AreEqual(prompt.style.opacity.value, ticker.style.opacity.value, "every ambient piece together");
            Ticks(veil, settings.Hud.FadeOut);
            Assert.AreEqual(0f, prompt.style.opacity.value);

            veil.SetStargazing(false);
            Ticks(veil, settings.Hud.FadeIn + Step);
            Assert.IsFalse(veil.IsVeiled);
            Assert.AreEqual(1f, ticker.style.opacity.value);
            Assert.Less(settings.Hud.FadeIn, settings.Hud.FadeOut, "back sooner than it left");
        }

        [Test]
        public void TheLookUpHint_ShowsOncePerSave_TheFirstTime07RestsUnderTheSky()
        {
            var settings = new StargazeSettings();
            var ledger = new PromptLedger(new PromptSettings());
            var hint = new LookUpHint(settings, ledger);
            Ticks(hint, settings.HintDelay + 1f);
            Assert.IsFalse(hint.WantsShown, "nothing while 07 drives");

            hint.SetWide(true);
            Ticks(hint, settings.HintDelay * 0.5f);
            Assert.IsFalse(hint.WantsShown, "a moment of stillness first");
            Ticks(hint, settings.HintDelay);
            Assert.IsTrue(hint.WantsShown);
            Assert.IsTrue(ledger.LookUpHinted);

            hint.SetStargazing(true);
            hint.Tick(Step);
            Assert.IsFalse(hint.WantsShown, "looking up is the answer");
            hint.SetStargazing(false);
            hint.SetWide(false);
            hint.SetWide(true);
            Ticks(hint, settings.HintDelay + settings.HintHoldSeconds);
            Assert.IsFalse(hint.WantsShown, "once only");

            var loaded = new PromptLedger(new PromptSettings());
            loaded.Restore(ledger.Capture());
            var next = new LookUpHint(settings, loaded);
            next.SetWide(true);
            Ticks(next, settings.HintDelay + 1f);
            Assert.IsFalse(next.WantsShown, "the save remembers it was shown");
        }

        [Test]
        public void TheLookUpHint_LeavesOnItsOwn_WhenThePlayerDoesNotLookUp()
        {
            var settings = new StargazeSettings();
            var hint = new LookUpHint(settings, new PromptLedger(new PromptSettings()));
            hint.SetWide(true);
            Ticks(hint, settings.HintDelay + Step);
            Assert.IsTrue(hint.WantsShown);
            Ticks(hint, settings.HintHoldSeconds + Step);
            Assert.IsFalse(hint.WantsShown);
        }

        private static void Ticks(StargazeVeil veil, float seconds)
        {
            for (float t = 0f; t < seconds; t += Step)
            {
                veil.Tick(Step);
            }
        }

        private static void Ticks(LookUpHint hint, float seconds)
        {
            for (float t = 0f; t < seconds; t += Step)
            {
                hint.Tick(Step);
            }
        }
    }
}
