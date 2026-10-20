using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MoonProject.World.PlayModeTests
{
    /// <summary>
    /// The call's point of light (<see cref="CallSkyPoint"/>) through its play-mode path: with no call signal in the
    /// scene it holds its glow, steady, and logs nothing; once a signal is heard it keys with it; with no call at all
    /// it stays dark.
    /// </summary>
    public sealed class CallSkyPointTests
    {
        // Mirrors WorldPaths (editor assemblies cannot be referenced from a PlayMode test assembly).
        private const string SettingsPath = "Assets/_Project/Data/Tuning/WorldSettings.asset";
        private const string MaterialPath = "Assets/_Project/Generated/World/M_CallSkyPoint.mat";
        private const string ControlsPath = "Assets/_Project/Data/Input/Controls.inputactions";
        private const int Frames = 90;

        private GameObject _host;
        private InputReader _input;

        [UnityTest]
        public IEnumerator WithNoSignal_ThePointHoldsItsGlow_Steady_AndWithNoCall_StaysDark()
        {
            GameContext context = Boot(out CallSkyPoint point, out Renderer renderer, out WorldSettings settings);
            for (int i = 0; i < Frames; i++)
            {
                yield return null;
                Assert.IsFalse(renderer.enabled, "dark with no call to point at");
            }

            context.Register<ICallSource>(new StillCall(CallState.Locked));
            yield return null;
            float rest = settings.Sky.CallPoint.Rest;
            for (int i = 0; i < Frames; i++)
            {
                yield return null;
                Assert.IsTrue(renderer.enabled);
                Assert.AreEqual(rest, point.Level, 1e-5f, "steady with no signal heard");
            }

            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator OnceASignalIsHeard_ThePointKeysWithIt()
        {
            GameContext context = Boot(out CallSkyPoint point, out _, out WorldSettings settings);
            context.Register<ICallSource>(new StillCall(CallState.Clear));
            yield return null;
            var signal = new HeldSignal();
            context.Register<ICallSignal>(signal);
            signal.Keying = 1f;
            yield return null;
            Assert.AreEqual(1f, point.Level, 1e-5f, "a full tone lifts it to its peak");
            signal.Keying = 0f;
            yield return null;
            Assert.AreEqual(settings.Sky.CallPoint.Rest, point.Level, 1e-5f, "between tones it holds its glow");
        }

        [TearDown]
        public void TearDown()
        {
            if (_host != null)
            {
                Object.Destroy(_host);
                _host = null;
            }
        }

        private GameContext Boot(out CallSkyPoint point, out Renderer renderer, out WorldSettings settings)
        {
#if UNITY_EDITOR
            settings = AssetDatabase.LoadAssetAtPath<WorldSettings>(SettingsPath);
            _input ??= new InputReader(AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath));
            _host = new GameObject("CallSkyPoint");
            _host.SetActive(false);
            _host.AddComponent<MeshFilter>();
            renderer = _host.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            point = _host.AddComponent<CallSkyPoint>();
            var serialized = new SerializedObject(point);
            serialized.FindProperty("_settings").objectReferenceValue = settings;
            serialized.FindProperty("_renderer").objectReferenceValue = renderer;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _host.SetActive(true);
            var context = new GameContext(new EventBus(), _input);
            point.Initialize(context);
            return context;
#else
            throw new System.NotSupportedException("The sky point tests load their assets through the editor.");
#endif
        }

        private sealed class StillCall : ICallSource
        {
            public StillCall(CallState state)
            {
                State = state;
            }

            public Vector3 Direction => new Vector3(-0.3f, 0.3f, 0.9f).normalized;
            public CallState State { get; }
            public int Decoded => 0;
            public int Total => 1;
        }

        private sealed class HeldSignal : ICallSignal
        {
            public float Keying { get; set; }
        }
    }
}
