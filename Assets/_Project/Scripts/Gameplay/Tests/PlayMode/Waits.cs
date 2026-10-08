using System;
using System.Collections;
using UnityEngine;

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>Frame waits for scripted sessions that a slow frame (a shader warming up) must not fail.</summary>
    internal static class Waits
    {
        /// <summary>Yields frames until <paramref name="condition"/> holds or <paramref name="timeout"/> s.</summary>
        public static IEnumerator Until(Func<bool> condition, float timeout)
        {
            float deadline = Time.time + timeout;
            while (!condition() && Time.time < deadline)
            {
                yield return null;
            }
        }
    }
}
