using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using MoonProject.App;
using MoonProject.Core;
using MoonProject.Core.Input;
using MoonProject.Testing;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// Build-mode placement with a REAL mouse and keyboard through the Input System (guidance audit row 13; the owner
    /// once softlocked at B6 on a mouse drag the bot never made): B opens the menu with the cursor free, a click on a
    /// card picks it (and that same press does not also place the ghost), C, Z and the wheel rotate the ghost, a right
    /// click does nothing, and a left click places it: no mouse drag anywhere in it. (The mouse look that moves the ghost
    /// needs the player's locked cursor, which the editor's batch run cannot take: the IL2CPP bot covers it.)
    /// </summary>
    [Category("BuildPointer")]
    public sealed class BuildPointerSessions : InputTestFixture
    {
        private const float WakeTimeout = 20f;
        private const float SettleSeconds = 0.4f;
        private const float ClearSpotEastOffset = 18f;
        private const float ClearSpotNorthOffset = 14f;
        private const float AheadMetres = 4.6f;
        private const float WheelStep = 120f;
        private const string StripName = "build-strip";

        private GameContext _context;
        private GameplaySystem _gameplay;
        private Keyboard _keyboard;
        private Mouse _mouse;
        private bool _awake;

        public override void Setup()
        {
            base.Setup();
            _gameplay = null;
            _awake = false;
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _mouse = InputSystem.AddDevice<Mouse>();
        }

        public override void TearDown()
        {
            Scene scene = SceneManager.GetSceneByPath(PlaythroughScene.ScenePath);
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    Object.DestroyImmediate(root);
                }
            }

            BootstrapHarness.DeleteSaveFiles(PlaythroughScene.SaveSlot);
            base.TearDown();
        }

        [UnityTest]
        [Timeout(300000)]
        [PrebuildSetup(typeof(PlaythroughScene))]
        [PostBuildCleanup(typeof(PlaythroughScene))]
        public IEnumerator ThePointer_PicksACard_RotationAndPlacementNeedNoDrag()
        {
            yield return Boot();
            BuildField build = _gameplay.Building;
            Assert.IsTrue(_gameplay.KnownStructures.Learn(StructureIds.ChargingPad));
            var terrain = _context.Get<ITerrainQuery>();
            Vector3 basePos = _context.Get<IWorldLayout>().BasePosition;
            Vector3 spot = new Vector3(basePos.x + ClearSpotEastOffset, 0f, basePos.z + ClearSpotNorthOffset);
            spot.y = terrain.SampleHeight(spot.x, spot.z);
            _context.Get<IRoverPlacement>().PlaceAt(spot, Quaternion.LookRotation(Vector3.forward, Vector3.up));
            yield return Wait(SettleSeconds);
            Assert.IsTrue(build.Catalog.TryGet(StructureIds.ChargingPad, out StructureDefinition pad));
            _gameplay.Materials.Stow(pad.BuildCost);

            Press(_keyboard.bKey);
            yield return null;
            Release(_keyboard.bKey);
            yield return Wait(SettleSeconds);
            Assert.AreEqual(BuildModeState.Menu, build.State, "B opens the build menu");

            UIDocument document = Object.FindFirstObjectByType<UIDocument>();
            VisualElement strip = document.rootVisualElement.Q<VisualElement>(StripName);
            Assert.IsNotNull(strip, "the build strip is in the HUD");
            VisualElement card = FirstCard(strip);
            Assert.IsNotNull(card, "the strip has a card to click");
            Vector2 at = ScreenOf(document, card.worldBound.center);
            Move(_mouse.position, at);
            yield return null;
            yield return null;
            Click(_mouse.leftButton);
            yield return null;
            yield return Wait(SettleSeconds);
            Assert.AreEqual(BuildModeState.Placing, build.State, "a click on a card picks it and enters placing");

            Assert.AreEqual(0, _gameplay.PlacedBase.Count,
                "the press that picked the card is the Excavate button too: it does not also place the ghost");

            // The editor cannot lock the cursor for the mouse look, so the aim is set: a spot inside weld range.
            Vector3 ahead = _context.Get<IRoverState>().Position + Vector3.forward * AheadMetres;
            ahead.y = terrain.SampleHeight(ahead.x, ahead.z);
            build.SetAimPointOverride(ahead);
            yield return null;

            float yaw = build.Controller.CandidatePose.Yaw;
            yield return Tap(_keyboard.cKey);
            float right = Mathf.DeltaAngle(yaw, build.Controller.CandidatePose.Yaw);
            yield return Tap(_keyboard.zKey);
            float back = Mathf.DeltaAngle(yaw, build.Controller.CandidatePose.Yaw);
            Assert.AreNotEqual(0f, right, "C rotates the ghost");
            Assert.AreEqual(0f, back, 1e-3f, "and Z rotates it back");
            Set(_mouse.scroll, new Vector2(0f, WheelStep));
            yield return null;
            Set(_mouse.scroll, Vector2.zero);
            yield return null;
            Assert.AreNotEqual(0f, Mathf.DeltaAngle(yaw, build.Controller.CandidatePose.Yaw), "the wheel still rotates it");

            Click(_mouse.rightButton);
            yield return Wait(SettleSeconds);
            Assert.AreEqual(BuildModeState.Placing, build.State, "a right click does not cancel");
            Assert.AreEqual(0, _gameplay.PlacedBase.Count);

            Click(_mouse.leftButton);
            yield return null;
            yield return null;
            Assert.AreEqual(1, _gameplay.PlacedBase.Count, "a left click places it");
            Assert.AreEqual(BuildModeState.Welding, build.State, "and the placed pad waits to be welded");
        }

        private static VisualElement FirstCard(VisualElement strip)
        {
            foreach (VisualElement child in strip.Children())
            {
                if (child.resolvedStyle.display != DisplayStyle.None && child.worldBound.width > 1f)
                {
                    return child;
                }
            }

            return null;
        }

        /// <summary>A point in the HUD panel's space as screen pixels (bottom-left origin): the inverse of the UI's own mapping.</summary>
        private static Vector2 ScreenOf(UIDocument document, Vector2 panelPoint)
        {
            Rect root = document.rootVisualElement.worldBound;
            float x = panelPoint.x / root.width * Screen.width;
            float yFromTop = panelPoint.y / root.height * Screen.height;
            return new Vector2(x, Screen.height - yFromTop);
        }

        private static IEnumerator Wait(float seconds)
        {
            float until = Time.time + seconds;
            while (Time.time < until)
            {
                yield return null;
            }
        }

        private IEnumerator Tap(UnityEngine.InputSystem.Controls.ButtonControl key)
        {
            Press(key);
            yield return null;
            Release(key);
            yield return null;
        }

        private IEnumerator Boot()
        {
            BootstrapHarness.DeleteSaveFiles(PlaythroughScene.SaveSlot);
#if UNITY_EDITOR
            AsyncOperation loading = EditorSceneManager.LoadSceneAsyncInPlayMode(PlaythroughScene.ScenePath,
                new LoadSceneParameters(LoadSceneMode.Single));
            while (!loading.isDone)
            {
                yield return null;
            }
#else
            throw new NotSupportedException("The build pointer sessions load the scene through the editor.");
#endif
            GameBootstrap bootstrap = null;
            foreach (GameObject root in SceneManager.GetSceneByPath(PlaythroughScene.ScenePath).GetRootGameObjects())
            {
                bootstrap = bootstrap != null ? bootstrap : root.GetComponent<GameBootstrap>();
                _gameplay = _gameplay != null ? _gameplay : root.GetComponentInChildren<GameplaySystem>();
            }

            Assert.IsNotNull(bootstrap);
            Assert.IsNotNull(_gameplay);
            _context = bootstrap.Context;
            using (_context.Events.Subscribe<MoonProject.Core.Events.RoverAwoke>(_ => _awake = true))
            {
                float deadline = Time.time + WakeTimeout;
                while (!_awake && Time.time < deadline)
                {
                    yield return null;
                }

                Assert.IsTrue(_awake, "07 wakes");
            }

            yield return null;
        }
    }
}
