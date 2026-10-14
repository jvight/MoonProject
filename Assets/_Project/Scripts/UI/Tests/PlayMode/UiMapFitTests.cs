using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Map;
using MoonProject.Gameplay.Map;
using MoonProject.Testing;
using MoonProject.UI.Map;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MoonProject.UI.PlayModeTests
{
    /// <summary>
    /// The crew's map at every screen shape (docs/features/M3-24 part A): the paper is fitted to the shorter side, so
    /// on a wide or a tall screen the whole sheet, its title, its tally and its card stay on the screen.
    /// </summary>
    public sealed class UiMapFitTests : InputTestFixture
    {
        private const float Settle = 0.4f;
        private const float Step = 0.05f;
        private const float OpenSeconds = 2.5f;
        private const float SheetRadius = 500f;
        private const float ContourInterval = 5f;
        private const string ShapeFolder = "Logs/ui-captures/shapes";

        private InputActionAsset _controls;
        private string _slot;
        private UiTestRig _rig;
        private MapSheet _sheet;
        private MapViewTuning _tuning;
        private MapView _view;

        public override void Setup()
        {
            base.Setup();
            _controls = BootstrapHarness.LoadControlsCopy();
            _slot = BootstrapHarness.NewTestSlot();
        }

        public override void TearDown()
        {
            _view?.Dispose();
            _rig?.Dispose();
            _rig = null;
            UiTestRig.DeleteSlotFiles(_slot);
            Object.DestroyImmediate(_sheet);
            Object.DestroyImmediate(_tuning);
            Object.Destroy(_controls);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator TheMap_StaysOnTheScreen_AtEveryShape()
        {
#if UNITY_EDITOR
            ScreenShape first = ScreenShapes.All[0];
            _rig = UiTestRig.Boot(_controls, _slot, first.Width, first.Height);
            EventBus events = _rig.Bootstrap.Context.Events;
            var texts = new List<TextAsset>
            {
                AssetDatabase.LoadAssetAtPath<TextAsset>(UiTestRig.EnglishPath),
                AssetDatabase.LoadAssetAtPath<TextAsset>(UiTestRig.VietnamesePath),
            };
            var tables = new StringTable[texts.Count];
            for (int i = 0; i < tables.Length; i++)
            {
                tables[i] = StringTable.Parse(texts[i].text, texts[i].name);
            }

            _sheet = ScriptableObject.CreateInstance<MapSheet>();
            _sheet.Write(7, Vector2.zero, SheetRadius, ContourInterval,
                new[] { new Vector2(-400f, 0f), new Vector2(0f, 400f), new Vector2(400f, 0f) },
                new[] { new MapLine(MapLineKind.RimCrest, 0, 3, true, 40f) }, new string[0], new Vector2[0]);
            _tuning = ScriptableObject.CreateInstance<MapViewTuning>();
            var reach = new HomeReach();
            var places = new List<MapPlace>
            {
                new MapPlace(MapPlaces.HomeId, Vector3.zero, MapPlaceKind.Home, 20f),
                new MapPlace("site.depot", new Vector3(50f, 0f, 0f), MapPlaceKind.Site, 5f),
                new MapPlace("relay.1", new Vector3(-150f, 0f, 0f), MapPlaceKind.Relay, 3f),
            };
            var probes = new List<MapPlaceProbe>();
            foreach (MapPlace place in places)
            {
                probes.Add(new MapPlaceProbe(null, null, null, null, true));
            }

            var knowledge = new MapKnowledge(events, reach, places, probes);
            knowledge.Poll(Vector3.zero, 0f, false, false);
            knowledge.MarkVisited(1);
            knowledge.MarkSeen(2);
            _rig.Fakes.ScanSites.Sites.Add(("micro.06", new Vector3(120f, 0f, -90f), true, 2, 2));
            _view = new MapView(_rig.Ui.Layout.Pause, _sheet, _tuning, knowledge, reach, new HomeOverlay(),
                new LocalizationService(events, tables), events, _rig.Bootstrap.Context.Input, _rig.Fakes.ScanSites);
            yield return null;

            events.Publish(new MapLookStarted(Vector3.zero, false));
            Assert.AreEqual(1, _view.BracketPositions.Count, "one pencil bracket drawn for the known scan site");
            var problems = new List<string>();
            bool capturedBracket = false;
            foreach (ScreenShape shape in ScreenShapes.All)
            {
                _rig.Reframe(shape.Width, shape.Height);
                for (float t = 0f; t < OpenSeconds; t += Step)
                {
                    _view.Tick(Step, false);
                    yield return null;
                }

                yield return new WaitForSecondsRealtime(Settle);
                _view.Tick(Step, false);
                yield return ScreenFit.Probe(found =>
                    ScreenFit.CheckOnScreen(_rig.Ui.Layout.Root, shape.Name + " map", found), problems);
                WriteShot(ShapeFolder, shape.Name + "_map.png");
                if (!capturedBracket)
                {
                    capturedBracket = true;
                    WriteShot("Logs/ui-captures", "d2_chart_bracket.png");
                }
            }

            events.Publish(new MapLookEnded());
            for (float t = 0f; t < OpenSeconds; t += Step)
            {
                _view.Tick(Step, false);
                yield return null;
            }

            _rig.Reframe(first.Width, first.Height);
            _rig.Fakes.SetLitLamps(RecoveryTarget.Power, 2);
            _rig.Fakes.SetTech(TechNodeId.TowerL1, TechNodeState.Built);
            _rig.Fakes.SetTech(TechNodeId.HoverJump, TechNodeState.Decoded);
            _rig.Fakes.ScanSites.SetSizes["rover.cargo_cradle"] = 2;
            _rig.Fakes.ScanSites.Scanned["rover.cargo_cradle"] = 1;
            events.Publish(new LabEntered(LabStation.Board));
            events.Publish(new LabStationLooked(LabStation.Board, true));
            events.Publish(new FragmentScanned("frag.cradle.1", "rover.cargo_cradle", 0, 2, false, 0));
            yield return new WaitForSecondsRealtime(1f);
            Assert.IsTrue(_rig.Ui.Board.IsNodeTorn(TechNodeId.CargoCradle));
            yield return ScreenFit.Probe(found =>
                ScreenFit.CheckOnScreen(_rig.Ui.Layout.Root, "lab board torn sheet", found), problems);
            WriteShot("Logs/ui-captures", "d2_board_torn_sheet.png");

            Assert.IsEmpty(problems, "the map stays on every screen shape:\n" + string.Join("\n", problems));
#else
            Assert.Ignore("The map's assets load through the editor.");
            yield break;
#endif
        }

#if UNITY_EDITOR
        /// <summary>The UI frame as rendered, over black, saved to <paramref name="relativeFolder"/>.</summary>
        private void WriteShot(string relativeFolder, string fileName)
        {
            RenderTexture frame = _rig.Frame;
            var texture = new Texture2D(frame.width, frame.height, TextureFormat.RGBA32, false, false);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = frame;
            texture.ReadPixels(new Rect(0, 0, frame.width, frame.height), 0, 0, false);
            RenderTexture.active = previous;
            Color32[] pixels = texture.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i].a = byte.MaxValue;
            }

            texture.SetPixels32(pixels);
            texture.Apply(false);
            string folder = Path.GetFullPath(relativeFolder);
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, fileName), texture.EncodeToPNG());
            Object.Destroy(texture);
        }
#endif

        private sealed class HomeOverlay : IChartOverlay
        {
            private static readonly Vector3 Mast = new Vector3(-150f, 0f, 0f);

            public int ChargerCount => 2;

            public float ChargerRadius => 140f;

            public int LinkPointCount => 2;

            public Vector3 Peak => new Vector3(0f, 0f, 430f);

            public Vector3 ChargerPosition(int index)
            {
                return index == 0 ? Vector3.zero : Mast;
            }

            public Vector3 LinkPoint(int index)
            {
                return index == 0 ? Vector3.zero : Mast;
            }
        }

        private sealed class HomeReach : IStationReach
        {
            private const float ReachMetres = 100f;

            public int LitCount => 1;

            public int NodeCount => 1;

            public bool IsInReach(Vector3 position)
            {
                return new Vector2(position.x, position.z).magnitude <= ReachMetres;
            }

            public float DistanceToNearestNode(Vector3 position)
            {
                return new Vector2(position.x, position.z).magnitude;
            }

            public RelayNode GetNode(int index)
            {
                return new RelayNode("home", Vector3.zero, true, ReachMetres, Vector3.zero);
            }
        }
    }
}
