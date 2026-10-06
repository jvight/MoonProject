using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.App;
using MoonProject.Core.Input;
using MoonProject.Core.Save;
using MoonProject.Testing;

namespace MoonProject.Tests.PlayMode.App
{
    public sealed class GameBootstrapSmokeTests
    {
        private readonly List<Object> _created = new List<Object>();
        private InputActionAsset _controls;

        [SetUp]
        public void SetUp()
        {
            _controls = BootstrapHarness.LoadControlsCopy();
            _created.Add(_controls);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in _created)
            {
                if (created != null)
                {
                    Object.Destroy(created);
                }
            }

            _created.Clear();
        }

        [UnityTest]
        public IEnumerator Systems_AreInitialisedOnceInSerializedOrder_WithSharedContextAndEnabledInput()
        {
            var journal = new List<string>();
            RecordingSystem c = CreateSystem("C", journal);
            RecordingSystem a = CreateSystem("A", journal);
            RecordingSystem b = CreateSystem("B", journal);

            GameBootstrap bootstrap = Track(BootstrapHarness.Create(_controls, c, a, b));
            yield return null;

            CollectionAssert.AreEqual(new[] { "C", "A", "B" }, journal);
            Assert.IsNotNull(bootstrap.Context);
            foreach (RecordingSystem system in new[] { a, b, c })
            {
                Assert.AreEqual(1, system.InitializeCount, system.Label);
                Assert.AreSame(bootstrap.Context, system.Context, system.Label);
            }

            Assert.IsNotNull(bootstrap.Context.Events);
            Assert.AreEqual(SaveLoadResult.NoSave, bootstrap.Context.Get<ISaveService>().LoadResult,
                "the save is loaded (each harness bootstrap has its own empty test slot)");
            Assert.IsTrue(bootstrap.Context.Input.Enabled, "the Rover map is enabled before systems run");
            Assert.AreEqual(Vector2.zero, bootstrap.Context.Input.Drive);
        }

        [UnityTest]
        public IEnumerator MissingControls_LogsAnErrorAndDisablesTheBootstrap()
        {
            var journal = new List<string>();
            RecordingSystem system = CreateSystem("A", journal);
            LogAssert.Expect(LogType.Error, new Regex("Input Actions asset is not assigned"));

            GameBootstrap bootstrap = Track(BootstrapHarness.Create(null, system));
            yield return null;

            Assert.IsFalse(bootstrap.enabled);
            Assert.IsNull(bootstrap.Context);
            Assert.IsEmpty(journal, "no system is initialised without input");
        }

        [UnityTest]
        public IEnumerator DestroyingTheBootstrap_DisablesInput()
        {
            GameBootstrap bootstrap = BootstrapHarness.Create(_controls);
            yield return null;
            InputReader input = bootstrap.Context.Input;

            Object.Destroy(bootstrap.gameObject);
            yield return null;

            Assert.IsFalse(input.Enabled);
        }

        private RecordingSystem CreateSystem(string label, List<string> journal)
        {
            var host = new GameObject($"System {label}");
            _created.Add(host);
            var system = host.AddComponent<RecordingSystem>();
            system.Label = label;
            system.Journal = journal;
            return system;
        }

        private GameBootstrap Track(GameBootstrap bootstrap)
        {
            _created.Add(bootstrap.gameObject);
            return bootstrap;
        }
    }
}
