using NUnit.Framework;
using UnityEngine.UIElements;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// While 07 stargazes the ambient HUD slowly leaves and soon comes back.
    /// </summary>
    public sealed class UiStargazeTests
    {
        private const float Step = 0.1f;

        [Test]
        public void TheAmbientHud_LeavesSlowly_AndComesBackSooner()
        {
            var settings = new StargazeSettings();
            var prompt = new VisualElement();
            var ring = new VisualElement();
            var veil = new StargazeVeil(settings.Hud, prompt, ring);
            veil.Tick(Step);
            Assert.IsFalse(veil.IsVeiled);
            Assert.AreEqual(1f, veil.Opacity);

            veil.SetStargazing(true);
            Ticks(veil, settings.Hud.FadeOut * 0.5f);
            Assert.IsTrue(veil.IsVeiled);
            Assert.That(prompt.style.opacity.value, Is.GreaterThan(0f).And.LessThan(1f), "it eases, never cuts");
            Assert.AreEqual(prompt.style.opacity.value, ring.style.opacity.value, "every ambient piece together");
            Ticks(veil, settings.Hud.FadeOut);
            Assert.AreEqual(0f, prompt.style.opacity.value);

            veil.SetStargazing(false);
            Ticks(veil, settings.Hud.FadeIn + Step);
            Assert.IsFalse(veil.IsVeiled);
            Assert.AreEqual(1f, ring.style.opacity.value);
            Assert.Less(settings.Hud.FadeIn, settings.Hud.FadeOut, "back sooner than it left");
        }

        private static void Ticks(StargazeVeil veil, float seconds)
        {
            for (float t = 0f; t < seconds; t += Step)
            {
                veil.Tick(Step);
            }
        }
    }
}
