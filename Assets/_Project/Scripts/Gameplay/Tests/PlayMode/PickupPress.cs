using System.Collections;
using NUnit.Framework;
using UnityEngine.InputSystem;

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>A player picking a waiting part up: once its prompt shows beside 07, a tap of E.</summary>
    internal static class PickupPress
    {
        public static IEnumerator Take(InputTestFixture input, GameplaySystem gameplay, float timeout, string what)
        {
            yield return Waits.Until(() => gameplay.Hints.TryGet(InteractionKind.PickUp, out _), timeout);
            Assert.IsTrue(gameplay.Hints.TryGet(InteractionKind.PickUp, out InteractionHint hint),
                $"a pickup prompt by {what}");
            Assert.IsFalse(string.IsNullOrEmpty(hint.NounKey), $"the prompt names {what}");
            input.Press(Keyboard.current.eKey);
            yield return null;
            yield return null;
            input.Release(Keyboard.current.eKey);
            yield return null;
        }
    }
}
