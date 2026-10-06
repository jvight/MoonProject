using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.App;
using MoonProject.Core.Save;
using MoonProject.Testing;

namespace MoonProject.Tests.PlayMode.App
{
    /// <summary>
    /// The save flow through a real GameBootstrap: systems register sections in Initialize, the bootstrap loads after
    /// every system is initialised (state is restored before Start), SaveNow persists, and a new session restores it.
    /// </summary>
    public sealed class SaveFlowTests
    {
        private InputActionAsset _controls;
        private string _slot;
        private GameObject _bootstrap;
        private GameObject _system;

        [SetUp]
        public void SetUp()
        {
            _controls = BootstrapHarness.LoadControlsCopy();
            _slot = BootstrapHarness.NewTestSlot();
        }

        [TearDown]
        public void TearDown()
        {
            DestroySession();
            Object.Destroy(_controls);
            BootstrapHarness.DeleteSaveFiles(_slot);
            Assert.IsFalse(File.Exists(Path.Combine(SaveService.DefaultDirectory, _slot + SaveService.Extension)));
        }

        [UnityTest]
        public IEnumerator Progress_SurvivesASecondSession()
        {
            CounterSaveSystem first = StartSession();
            ISaveService save = Service();
            Assert.AreEqual(SaveLoadResult.NoSave, save.LoadResult, "a fresh slot is a new game");
            first.Count = 7;
            Assert.IsTrue(save.SaveNow());
            yield return null;
            DestroySession();

            CounterSaveSystem second = StartSession();
            yield return null;

            Assert.AreEqual(0, second.CountAtInitialize, "Initialize sees the default state");
            Assert.AreEqual(7, second.Count, "the bootstrap restored the section right after initialising systems");
            Assert.AreEqual(SaveLoadResult.Loaded, Service().LoadResult);
        }

        [UnityTest]
        public IEnumerator Bootstrap_RegistersTheSaveServiceForItsSlot()
        {
            StartSession();
            yield return null;

            ISaveService save = Service();
            Assert.IsTrue(save.IsLoaded);
            Assert.AreEqual(Path.GetFullPath(Path.Combine(SaveService.DefaultDirectory, _slot + SaveService.Extension)),
                save.FilePath);
        }

        private CounterSaveSystem StartSession()
        {
            _system = new GameObject("CounterSaveSystem");
            var system = _system.AddComponent<CounterSaveSystem>();
            _bootstrap = BootstrapHarness.Create(_controls, _slot, system).gameObject;
            return system;
        }

        private ISaveService Service()
        {
            return _bootstrap.GetComponent<GameBootstrap>().Context.Get<ISaveService>();
        }

        private void DestroySession()
        {
            if (_bootstrap != null)
            {
                Object.DestroyImmediate(_bootstrap);
            }

            if (_system != null)
            {
                Object.DestroyImmediate(_system);
            }
        }
    }
}
