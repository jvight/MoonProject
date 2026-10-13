using System.Collections;
using NUnit.Framework;
using UnityEngine.InputSystem;
using MoonProject.Testing;

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// Cuts a sealed container's seal as a player does (0.5.5 §2): 07 beside it, E held until the lid stands open.
    /// </summary>
    internal static class SealCut
    {
        /// <summary>Slack (s) on top of the cut and the lid's swing for a slow first frame.</summary>
        private const float Slack = 3f;

        public static IEnumerator Hold(InputTestFixture input, GameplaySystem gameplay, SealedContainer container,
            string what)
        {
            yield return Waits.Until(() => gameplay.Containers.Candidate == container, 2f);
            Assert.AreSame(container, gameplay.Containers.Candidate, $"{what}'s seal is the one to cut");
            input.Press(Keyboard.current.eKey);
            float timeout = gameplay.Cutter.Plan(3, false).TotalSeconds +
                            SalvageYield.CutSeconds(gameplay.Cargo.ContainerCut, gameplay.Cutter.SpeedFactor,
                                gameplay.Salvage.Tuning) + gameplay.Cargo.ContainerLidSeconds + Slack;
            yield return Waits.Until(() => container.Opened, timeout);
            input.Release(Keyboard.current.eKey);
            Assert.IsTrue(container.Opened, $"held E cuts {what}'s seal");
            yield return Waits.Until(() => container.StandsOpen, gameplay.Cargo.ContainerLidSeconds + Slack);
            Assert.IsTrue(container.StandsOpen, $"{what}'s lid swings open");
        }
    }
}
