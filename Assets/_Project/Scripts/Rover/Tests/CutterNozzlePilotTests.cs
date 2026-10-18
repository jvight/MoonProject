using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover.Tests
{
    /// <summary>The nozzle is dark while the cutter is offline and shows a small steady pilot light once it works.</summary>
    public sealed class CutterNozzlePilotTests
    {
        private const float Frame = 1f / 60f;
        private const float SettleSeconds = 3f;

        private GameObject _root;
        private RoverFxTuning _tuning;
        private CutterNozzleDevice _device;
        private EventBus _events;

        [SetUp]
        public void SetUp()
        {
            _tuning = ScriptableObject.CreateInstance<RoverFxTuning>();
            _root = new GameObject("Nozzle");
            _device = _root.AddComponent<CutterNozzleDevice>();
            var blades = new Transform[RoverModelNodes.NozzleBladeCount];
            for (int i = 0; i < blades.Length; i++)
            {
                blades[i] = new GameObject("Blade" + i).transform;
                blades[i].SetParent(_root.transform);
            }

            var core = GameObject.CreatePrimitive(PrimitiveType.Quad);
            core.transform.SetParent(_root.transform);
            var light = new GameObject("Spill").AddComponent<Light>();
            light.transform.SetParent(_root.transform);
            var serialized = new UnityEditor.SerializedObject(_device);
            serialized.FindProperty("_tuning").objectReferenceValue = _tuning;
            serialized.FindProperty("_core").objectReferenceValue = core.transform;
            serialized.FindProperty("_coreRenderer").objectReferenceValue = core.GetComponent<Renderer>();
            serialized.FindProperty("_spillLight").objectReferenceValue = light;
            UnityEditor.SerializedProperty array = serialized.FindProperty("_blades");
            array.arraySize = blades.Length;
            for (int i = 0; i < blades.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = blades[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            _events = new EventBus();
            Assert.IsTrue(_device.Initialize(_events));
        }

        [TearDown]
        public void TearDown()
        {
            _device.Dispose();
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_tuning);
        }

        private void Settle()
        {
            float time = 0f;
            for (float t = 0f; t < SettleSeconds; t += Frame)
            {
                time += Frame;
                _device.Tick(Frame, time);
            }
        }

        [Test]
        public void Offline_TheNozzleIsDark()
        {
            Settle();
            Assert.AreEqual(0f, _device.CoreGlow01, 1e-4f);
            Assert.AreEqual(0f, _device.SpillLightIntensity, 1e-4f);
        }

        [Test]
        public void Online_TheNozzleShowsASmallSteadyPilotLight()
        {
            _events.Publish(new RoverSystemChanged(RoverSystem.Cutter, RoverSystemState.Ok, true));
            Settle();
            CutterNozzleSettings settings = _tuning.CutterNozzle;
            Assert.AreEqual(settings.PilotCoreGlow, _device.CoreGlow01, 1e-3f);
            Assert.AreEqual(settings.PilotSpillIntensity, _device.SpillLightIntensity, 1e-3f);
            Assert.Less(_device.CoreGlow01, settings.ChargeInitialCoreGlow, "Smaller than a charge-up.");
            Assert.AreEqual(0f, _device.IrisOpen01, 1e-4f, "The iris stays shut.");
        }

        [Test]
        public void AnotherSystemChanging_LeavesTheNozzleDark()
        {
            _events.Publish(new RoverSystemChanged(RoverSystem.Antenna, RoverSystemState.Ok, true));
            Settle();
            Assert.AreEqual(0f, _device.CoreGlow01, 1e-4f);
        }
    }
}
