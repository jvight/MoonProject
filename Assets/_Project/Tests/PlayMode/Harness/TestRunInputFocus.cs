using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MoonProject.Testing
{
    /// <summary>
    /// Keeps input flowing to the game during an editor test run, whatever the window focus. Batch Unity runs
    /// unfocused, and with the default background behaviour (reset and disable non-background devices) a focus change
    /// mid-test reset and disabled the virtual devices: Esc, clicks and taps ignored, a virtual stick wiped.
    /// While the run is marked (<see cref="SessionKey"/>, set by MoonProject.Editor.Automation.TestRunTitleSkip), a
    /// step at the start of every frame sets backgroundBehavior = IgnoreFocus and editorInputBehaviorInPlayMode =
    /// AllDeviceInputAlwaysGoesToGameView on the current settings. Each InputTestFixture installs fresh settings in
    /// its Setup and drops them in its TearDown, so every fixture is covered without a shared base, and nothing needs
    /// restoring. A settings asset saved in the project is never touched.
    /// </summary>
    public static class TestRunInputFocus
    {
        /// <summary>Editor SessionState key, true while the editor runs tests.</summary>
        public const string SessionKey = "MoonProject.TestRunIgnoresFocus";

        // Installed on every play-mode entry in the editor: a test run marks itself only after play mode has begun, so
        // the step checks the mark each frame and does nothing outside a run.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Install()
        {
#if UNITY_EDITOR
            PlayerLoopSystem loop = PlayerLoop.GetCurrentPlayerLoop();
            for (int i = 0; i < loop.subSystemList.Length; i++)
            {
                if (loop.subSystemList[i].type != typeof(EarlyUpdate))
                {
                    continue;
                }

                PlayerLoopSystem[] steps = loop.subSystemList[i].subSystemList;
                foreach (PlayerLoopSystem step in steps)
                {
                    if (step.type == typeof(TestRunInputFocus))
                    {
                        return;
                    }
                }

                var updated = new PlayerLoopSystem[steps.Length + 1];
                updated[0] = new PlayerLoopSystem { type = typeof(TestRunInputFocus), updateDelegate = Enforce };
                Array.Copy(steps, 0, updated, 1, steps.Length);
                loop.subSystemList[i].subSystemList = updated;
                PlayerLoop.SetPlayerLoop(loop);
                return;
            }

            Debug.LogError($"{nameof(TestRunInputFocus)}: the player loop has no EarlyUpdate phase to hook.");
#endif
        }

        private static void Enforce()
        {
#if UNITY_EDITOR
            InputSettings settings = InputSystem.settings;
            if (settings == null || EditorUtility.IsPersistent(settings) || !SessionState.GetBool(SessionKey, false))
            {
                return;
            }

            if (settings.backgroundBehavior != InputSettings.BackgroundBehavior.IgnoreFocus)
            {
                settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            }

            if (settings.editorInputBehaviorInPlayMode !=
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView)
            {
                settings.editorInputBehaviorInPlayMode =
                    InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            }
#endif
        }
    }
}
