using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Testing;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// The feel checklist's last line: a busy steady state of every gameplay system allocates nothing.
    /// </summary>
    public sealed class GameplayAllocationSessions : InputTestFixture
    {
        private InputActionAsset _controls;
        private GameplayFixture _fixture;
        private Keyboard _keyboard;
        private Mouse _mouse;

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _mouse = InputSystem.AddDevice<Mouse>();
            _controls = BootstrapHarness.LoadControlsCopy();
        }

        public override void TearDown()
        {
            _fixture?.Dispose();
            Object.Destroy(_controls);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator BusySteadyState_AllocatesNothing()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            GameplaySystem gameplay = _fixture.Gameplay;
            Relic duck = gameplay.Relics.Find("rubber_duck");
            duck.BeginLift();
            duck.SetLiftPose(new Vector3(0f, 0.6f, 8f), Quaternion.identity, 1f);
            duck.Surface(Vector3.zero, Vector3.zero);
            yield return new WaitForSeconds(0.8f);

            Vector3 camera = _fixture.Rover.Camera.transform.position;
            _fixture.Rover.Aim(camera, duck.transform.position);
            yield return null;
            yield return null;
            Press(_mouse.rightButton, queueEventOnly: true);
            yield return null;
            yield return null;
            Assert.AreEqual(TetherAimState.Towing, gameplay.Tether.State);
            Press(_keyboard.spaceKey, queueEventOnly: true);
            yield return null;
            Release(_keyboard.spaceKey);
            yield return new WaitForSeconds(3f);
            Assert.Greater(_fixture.Events.RelicAnswered.Count, 0, "pillars are standing");

            var updates = new List<Action>();
            foreach (MonoBehaviour part in new MonoBehaviour[]
                     {
                         gameplay.Relics, gameplay.Scrap, gameplay.Sonar, gameplay.Excavation, gameplay.Tether,
                         gameplay.Home, gameplay.Tower,
                     })
            {
                updates.Add(Method(part, "Update"));
            }

            updates.Add(Method(gameplay.Tether, "FixedUpdate"));
            foreach (Relic relic in gameplay.Relics.Relics)
            {
                updates.Add(Method(relic, "Update"));
            }

            IInteractionHints hints = gameplay.Hints;
            Run(updates, hints, 30);
            long before = GC.GetAllocatedBytesForCurrentThread();
            int silent = Run(updates, hints, 300);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Release(_mouse.rightButton);
            Assert.AreEqual(0, silent, "a hint (reel) is always available while towing");
            Assert.AreEqual(0L, allocated,
                "bytes allocated by 300 frames of relics, scrap, sonar, excavation, tether, home, tower and hints");
        }

        /// <summary>
        /// Runs the frames and returns how many had no hint at all (asserted outside the measurement).
        /// </summary>
        private int Run(List<Action> updates, IInteractionHints hints, int frames)
        {
            int silent = 0;
            for (int frame = 0; frame < frames; frame++)
            {
                float angle = frame * 0.01f;
                _fixture.Rover.MoveTo(new Vector3(Mathf.Sin(angle) * 3f, 0f, Mathf.Cos(angle) * 3f), angle);
                for (int i = 0; i < updates.Count; i++)
                {
                    updates[i]();
                }

                if (hints.Primary.Kind == InteractionKind.None)
                {
                    silent++;
                }
            }

            return silent;
        }

        private static Action Method(MonoBehaviour component, string name)
        {
            MethodInfo method = component.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"{component.GetType().Name}.{name}");
            return (Action)Delegate.CreateDelegate(typeof(Action), component, method);
        }
    }
}
