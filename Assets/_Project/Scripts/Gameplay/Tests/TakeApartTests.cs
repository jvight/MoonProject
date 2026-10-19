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
    /// The first pad's home and the way a piece comes down (docs: owner playtest, B5): the apron's snap, a taken-down
    /// piece paying back all of its cost exactly once, and the take-apart picture that ends by itself without a pop.
    /// </summary>
    public sealed class TakeApartTests
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
        public void TheMotion_EasesDown_ThenEachPieceFlies_ShrinksToNothing_AndNothingJumps()
        {
            var motion = new TakeApartMotion(_tuning.TakeApartEaseShare, 3);
            Assert.AreEqual(0f, motion.Sink01(0f), 1e-5f);
            Assert.AreEqual(1f, motion.Sink01(1f), 1e-5f);
            for (int piece = 0; piece < 3; piece++)
            {
                Assert.AreEqual(0f, motion.Flight01(piece, 0f), 1e-5f, "nothing has left at the start");
                Assert.AreEqual(1f, motion.Flight01(piece, 1f), 1e-5f, "every piece has arrived at the end");
                Assert.AreEqual(1f, motion.Scale(piece, 0f), 1e-5f);
                Assert.AreEqual(TakeApartMotion.ArrivalScale, motion.Scale(piece, 1f), 1e-5f);
                float previousFlight = 0f;
                float previousScale = 1f;
                for (int step = 1; step <= 120; step++)
                {
                    float t = step / 120f;
                    float flight = motion.Flight01(piece, t);
                    float scale = motion.Scale(piece, t);
                    Assert.GreaterOrEqual(flight, previousFlight - 1e-5f, "a piece never flies back");
                    Assert.LessOrEqual(scale, previousScale + 1e-5f, "a piece only shrinks");
                    Assert.Less(flight - previousFlight, 0.08f, "no jump in the flight");
                    Assert.Less(previousScale - scale, 0.1f, "no pop in the scale");
                    previousFlight = flight;
                    previousScale = scale;
                }
            }

            Assert.Greater(motion.Flight01(0, 0.6f), motion.Flight01(2, 0.6f), "the first piece leaves before the last");
            Assert.IsFalse(motion.BeamRetracting(0.5f));
            Assert.IsTrue(motion.BeamRetracting(0.95f));
        }

        [Test]
        public void TheTakeApart_ComesApartInTwoOrThreePieces_AndEndsOnceWithOneDustPuff_InAboutOnePointTwoSeconds()
        {
            var root = new GameObject("TakenDown");
            var view = root.AddComponent<StructureView>();
            for (int i = 0; i < 5; i++)
            {
                GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
                part.transform.SetParent(root.transform, false);
                part.transform.localPosition = new Vector3(i - 2f, 0.5f, 0f);
            }

            var puffs = new List<(Vector3 Position, int Grains, float Strength)>();
            var destroyed = new List<GameObject>();
            Vector3 body = new Vector3(0f, 0.8f, -6f);
            try
            {
                var takeApart = new TakeApart(view, _tuning, () => body, go =>
                {
                    destroyed.Add(go);
                    Object.DestroyImmediate(go);
                });
                int pieces = 0;
                foreach (Transform child in root.transform)
                {
                    pieces += child.name.StartsWith("TakeApartPiece", System.StringComparison.Ordinal) ? 1 : 0;
                }

                Assert.That(pieces, Is.InRange(2, 3));

                float seconds = 0f;
                int endings = 0;
                while (!takeApart.IsDone && seconds < 3f)
                {
                    endings += takeApart.Step(1f / 60f, (at, grains, strength) => puffs.Add((at, grains, strength)))
                        ? 1
                        : 0;
                    seconds += 1f / 60f;
                }

                Assert.IsTrue(takeApart.IsDone);
                Assert.AreEqual(_tuning.TakeApartSeconds, seconds, 0.05f, "about 1.2 s");
                Assert.AreEqual(1, endings, "it ends once");
                Assert.AreEqual(1, puffs.Count, "one small puff of dust");
                Assert.AreEqual(_tuning.TakeApartDustCount, puffs[0].Grains);
                Assert.AreEqual(1, destroyed.Count, "the view goes once");
                Assert.IsFalse(takeApart.Step(1f / 60f, (a, g, s) => puffs.Add((a, g, s))), "and does nothing after");
                Assert.AreEqual(1, puffs.Count);
            }
            finally
            {
                if (root != null)
                {
                    Object.DestroyImmediate(root);
                }
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
