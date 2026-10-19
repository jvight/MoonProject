using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Gameplay.Editor;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// The first pad's home and the way a piece comes down (owner playtest, B5): the apron's snap and guide, a held
    /// take-down that fills and rewinds, and a taken-down piece paying back all of its cost exactly once.
    /// </summary>
    public sealed class TakeDownTests
    {
        private BuildingTuning _tuning;

        [SetUp]
        public void SetUp()
        {
            _tuning = ScriptableObject.CreateInstance<BuildingTuning>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tuning);
        }

        [Test]
        public void TheApronCatchesTheGhost_WithinItsSnapRadius_AndOnlyThere()
        {
            Vector3 apron = new Vector3(20f, 0f, 30f);
            Vector3 rover = apron + Vector3.back * 5f;
            float ahead = _tuning.GhostAheadMetres;

            Assert.AreEqual(apron, GhostAim.Point(rover, Vector3.forward, apron, _tuning),
                "the ghost rides to 0.5 m from the bay and settles onto it");
            Vector3 aside = apron + Vector3.back * 3f + Vector3.right * 3f;
            Assert.AreEqual(apron, GhostAim.Point(aside, Vector3.forward, apron, _tuning),
                "a generous radius: 07 a few metres to one side still gets it");
            Vector3 far = apron + Vector3.back * 3f + Vector3.right * (_tuning.ApronSnapRadius + 1f);
            Vector3 free = GhostAim.Point(far, Vector3.forward, apron, _tuning);
            Assert.AreNotEqual(apron, free, "beyond the radius the ghost rides free: placing elsewhere is allowed");
            Assert.AreEqual(far + Vector3.forward * ahead, free);
        }

        [Test]
        public void TheApronRadius_IsGenerous_AndTheTuningRefundsAllOfIt()
        {
            Assert.GreaterOrEqual(_tuning.ApronSnapRadius, 4f);
            Assert.AreEqual(1f, _tuning.DeconstructRefundShare, "the default pays back the whole cost");
            var asset = AssetDatabase.LoadAssetAtPath<BuildingTuning>(GameplayAssetPaths.BuildingTuning);
            Assert.IsNotNull(asset, GameplayAssetPaths.BuildingTuning);
            Assert.AreEqual(1f, asset.DeconstructRefundShare, "and so does the game's tuning asset");
            Assert.GreaterOrEqual(asset.ApronSnapRadius, 4f);
        }

        [Test]
        public void ATakenDownPiece_PaysBackItsWholeCost_Once()
        {
            StructureCatalog catalog = BaseStructureRecipes.CreateCatalog();
            MaterialCatalog materials = CargoFixtures.Catalog();
            try
            {
                var events = new EventBus();
                var removed = new List<StructureRemoved>();
                events.Subscribe<StructureRemoved>(removed.Add);
                var known = new KnownStructures(catalog, events);
                var placedBase = new PlacedBase(catalog, _tuning, events, known);
                MaterialStock stock = CargoFixtures.Stock(events, materials);
                var controller = new BuildModeController(catalog, known, placedBase, stock, _tuning);
                StructureDefinition pad = catalog.Find(StructureIds.ChargingPad);
                int id = placedBase.PlaceUnchecked(pad, new StructurePose(Vector3.forward * 4f, 0f), 1f);
                Assert.AreEqual(0, stock.Metal);

                controller.StepDeconstruct(0.5f, id, Vector3.zero, deconstructHeld: true, out int doneEarly, out _);
                Assert.AreEqual(0, doneEarly, "half a hold takes nothing down");

                Assert.IsTrue(controller.StepDeconstruct(2f, id, Vector3.zero, deconstructHeld: true, out int done,
                    out Recipe refund));
                Assert.AreEqual(id, done);
                Assert.AreEqual(pad.BuildCost, refund, "the whole cost, not a share");
                Assert.AreEqual(pad.BuildCost.Metal, stock.Metal);
                Assert.AreEqual(pad.BuildCost.Wiring, stock.Wiring);

                Assert.IsFalse(controller.StepDeconstruct(2f, id, Vector3.zero, deconstructHeld: true, out _, out _));
                Assert.AreEqual(pad.BuildCost.Metal, stock.Metal, "a held key after it is gone pays nothing more");
                Assert.AreEqual(1, removed.Count, "it is removed once");
            }
            finally
            {
                for (int i = 0; i < catalog.Count; i++)
                {
                    Object.DestroyImmediate(catalog.StructureAt(i));
                }

                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(materials);
            }
        }

        [Test]
        public void AHeldTakeDown_FillsOverTheHold_AndALetGoRewindsIt_WithoutPayingAnything()
        {
            StructureCatalog catalog = BaseStructureRecipes.CreateCatalog();
            MaterialCatalog materials = CargoFixtures.Catalog();
            try
            {
                var events = new EventBus();
                var known = new KnownStructures(catalog, events);
                var placedBase = new PlacedBase(catalog, _tuning, events, known);
                MaterialStock stock = CargoFixtures.Stock(events, materials);
                var controller = new BuildModeController(catalog, known, placedBase, stock, _tuning,
                    deconstructHoldSeconds: 1.2f, deconstructRewindSeconds: 0.6f);
                StructureDefinition pad = catalog.Find(StructureIds.ChargingPad);
                int id = placedBase.PlaceUnchecked(pad, new StructurePose(Vector3.forward * 4f, 0f), 1f);

                controller.StepDeconstruct(0.6f, id, Vector3.zero, deconstructHeld: true, out _, out _);
                Assert.AreEqual(0.5f, controller.DeconstructProgress01, 1e-4f, "half the hold, half taken down");
                Assert.AreEqual(id, controller.DeconstructTargetId);

                controller.StepDeconstruct(0.3f, id, Vector3.zero, deconstructHeld: false, out int early, out _);
                Assert.AreEqual(0, early);
                Assert.AreEqual(0f, controller.DeconstructProgress01, 1e-4f, "0.3 s of rewind at 0.6 s for the lot");
                Assert.AreEqual(0, controller.DeconstructTargetId, "nothing left of the hold, so the target is let go");
                Assert.AreEqual(0, stock.Metal, "a take-down let go of pays nothing");
                Assert.AreEqual(1, placedBase.Count);

                controller.StepDeconstruct(0.6f, id, Vector3.zero, deconstructHeld: true, out _, out _);
                controller.StepDeconstruct(0.1f, id, Vector3.zero, deconstructHeld: false, out _, out _);
                Assert.Greater(controller.DeconstructProgress01, 0.2f, "a short let-go only rewinds part of the way");
                Assert.Less(controller.DeconstructProgress01, 0.5f);
            }
            finally
            {
                for (int i = 0; i < catalog.Count; i++)
                {
                    Object.DestroyImmediate(catalog.StructureAt(i));
                }

                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(materials);
            }
        }

        [Test]
        public void TheMoteStream_RunsFromTheEdgeToTheEmitter_WithinItsPool_AndEmptiesOnceTheHoldLetsGo()
        {
            var host = new GameObject("MoteHost");
            var sprite = new Material(Shader.Find("Sprites/Default"));
            try
            {
                var motes = new MoteStream(host.transform, sprite, _tuning, _tuning.TakeDownMotePool);
                Vector3 edge = new Vector3(0f, 0.6f, 4f);
                Vector3 emitter = new Vector3(0f, 0.8f, 0f);
                int most = 0;
                for (int i = 0; i < 120; i++)
                {
                    motes.Step(1f / 60f, edge, emitter, 1f);
                    most = Mathf.Max(most, motes.Live);
                }

                Assert.Greater(most, 2, "a stream, not one mote");
                Assert.LessOrEqual(most, _tuning.TakeDownMotePool);
                for (int i = 0; i < 120; i++)
                {
                    motes.Step(1f / 60f, edge, emitter, 0f);
                }

                Assert.AreEqual(0, motes.Live, "motes already in flight finish their way, then none are left");
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(sprite);
            }
        }

        [Test]
        public void TheApronGuide_BreathesSlowlyAndEased_FadesInWhileWanted_AndOutForGood()
        {
            var bayHost = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bayHost.transform.localScale = new Vector3(6f, 0.05f, 9f);
            var host = new GameObject("GuideHost");
            var glow = new Material(Shader.Find("Sprites/Default"));
            try
            {
                var guide = new ApronGuide(host.transform, bayHost.GetComponent<Renderer>(), glow, _tuning);
                Assert.AreEqual(1.5f, _tuning.ApronGuidePeriodSeconds, 1e-4f, "about a 1.5 s breath");
                float floor = _tuning.ApronGuideFloorShare;
                Assert.AreEqual(floor, guide.Breath(0f), 1e-4f);
                Assert.AreEqual(1f, guide.Breath(0.75f), 1e-4f);
                Assert.AreEqual(floor, guide.Breath(1.5f), 1e-4f);
                Assert.Greater(guide.Breath(0.1f), floor);
                Assert.Less(guide.Breath(0.1f) - floor, 0.1f, "eased: it leaves its floor slowly");

                Assert.IsFalse(guide.IsShown);
                for (float t = 0f; t < _tuning.ApronGuideFadeSeconds + 0.1f; t += 0.05f)
                {
                    guide.Step(true, 0.05f);
                }

                Assert.AreEqual(1f, guide.Visible, 1e-4f, "it fades in while the first pad is wanted");
                Assert.AreEqual(2, host.GetComponentsInChildren<LineRenderer>().Length, "a frame and one centre marker");
                foreach (LineRenderer line in host.GetComponentsInChildren<LineRenderer>())
                {
                    Assert.AreEqual(_tuning.ApronGuideWidth, line.widthMultiplier, 1e-4f);
                }

                for (float t = 0f; t < _tuning.ApronGuideFadeSeconds + 0.1f; t += 0.05f)
                {
                    guide.Step(false, 0.05f);
                }

                Assert.IsFalse(guide.IsShown, "and out once the pad is placed");
                guide.Dispose();
            }
            finally
            {
                Object.DestroyImmediate(bayHost);
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(glow);
            }
        }
    }
}
