using System;
using System.Reflection;
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
    /// Keeps scripted input from vanishing in long PlayMode sessions. In the editor the Input System discards every
    /// state event stamped between "exit edit mode" and "enter play mode" (InputManager.ShouldDiscardEditModeTransitionEvent),
    /// with no log, to drop real input that arrived during the transition. Those two marks are real editor seconds,
    /// but an InputTestFixture stamps events with its own clock, which starts near zero and advances 1/60 s per update.
    /// A session that runs enough frames walks that clock into the window, and every Set inside it is thrown away:
    /// a stick that never reaches the game, a held key ignored, a click lost. Intermittent and order-dependent,
    /// because the window and the frame count differ per run. Diagnosed with <see cref="TestRunInputProbe"/>: the
    /// event was queued and never reached the input system's listeners; the device was never reset.
    /// While the editor runs tests (<see cref="SessionKey"/>, set by MoonProject.Editor.Automation.TestRunTitleSkip), a
    /// step at the start of every frame clears the exit-edit-mode mark once play mode has been entered, which turns the
    /// discard off. Real devices are unaffected: their events after entering play mode were never in the window.
    /// </summary>
    public static class TestRunInputGuard
    {
        /// <summary>Editor SessionState key, true while the editor runs tests.</summary>
        public const string SessionKey = "MoonProject.TestRunInputGuard";

        private const BindingFlags Internal = BindingFlags.NonPublic | BindingFlags.Static;
        private const string SystemObjectField = "s_SystemObject";
        private const string ExitEditModeField = "exitEditModeTime";
        private const string EnterPlayModeField = "enterPlayModeTime";

        private static readonly FieldInfo SystemObject = typeof(InputSystem).GetField(SystemObjectField, Internal);

        /// <summary>When the Input System saw play mode entered (its own clock, seconds); 0 before that.</summary>
        public static double EnterPlayModeTime => ReadMark(EnterPlayModeField);

        /// <summary>When the Input System saw edit mode left; 0 once this guard has cleared it.</summary>
        public static double ExitEditModeTime => ReadMark(ExitEditModeField);

        // Installed on every play-mode entry in the editor: a test run marks itself only after play mode has begun, so
        // the step checks the mark each frame and does nothing outside a run.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Install()
        {
#if UNITY_EDITOR
            if (SystemObject == null)
            {
                Debug.LogError($"{nameof(TestRunInputGuard)}: InputSystem.{SystemObjectField} not found; the Input " +
                               "System changed, so scripted input may vanish in long sessions again.");
                return;
            }

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
                    if (step.type == typeof(TestRunInputGuard))
                    {
                        return;
                    }
                }

                var updated = new PlayerLoopSystem[steps.Length + 1];
                updated[0] = new PlayerLoopSystem { type = typeof(TestRunInputGuard), updateDelegate = Guard };
                Array.Copy(steps, 0, updated, 1, steps.Length);
                loop.subSystemList[i].subSystemList = updated;
                PlayerLoop.SetPlayerLoop(loop);
                return;
            }

            Debug.LogError($"{nameof(TestRunInputGuard)}: the player loop has no EarlyUpdate phase to hook.");
#endif
        }

        private static void Guard()
        {
#if UNITY_EDITOR
            if (!SessionState.GetBool(SessionKey, false) || EnterPlayModeTime <= 0d || ExitEditModeTime <= 0d)
            {
                return;
            }

            object system = SystemObject.GetValue(null);
            system.GetType().GetField(ExitEditModeField).SetValue(system, 0d);
#endif
        }

        private static double ReadMark(string field)
        {
            object system = SystemObject?.GetValue(null);
            return system?.GetType().GetField(field)?.GetValue(system) is double value ? value : 0d;
        }
    }
}
