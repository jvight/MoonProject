using System;
using UnityEngine;

namespace MoonProject.Autoplay
{
    /// <summary>
    /// The only door into the bot: when the exe is started with <c>-autoplay</c>, a runner is added after the first
    /// scene loads. This assembly is compiled only into development builds (DEVELOPMENT_BUILD or LUNARION_AUTOPLAY);
    /// a release build has no code that reads the flag.
    /// </summary>
    public static class AutoplayEntry
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Begin()
        {
            if (!AutoplayOptions.TryParse(Environment.GetCommandLineArgs(), out AutoplayOptions options,
                    out string problem))
            {
                return;
            }

            if (options == null)
            {
                Debug.LogError("Autoplay: " + problem);
                Application.Quit(1);
                return;
            }

            if (!string.Equals(options.Scenario, "opening", StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogError("Autoplay: unknown scenario '" + options.Scenario + "' (only 'opening').");
                Application.Quit(1);
                return;
            }

            var host = new GameObject("Autoplay");
            UnityEngine.Object.DontDestroyOnLoad(host);
            host.AddComponent<AutoplayRunner>().Configure(options);
        }
    }
}
