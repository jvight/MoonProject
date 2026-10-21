using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// The SSR-3 headlamp assembled at the rack (opening v3 5.2): only while its bay is read, only once Kessler–Arai
    /// understanding has unlocked it (past the west signal), only with its cost in the hold; a held E fills the
    /// progress, letting go starts again, and full it spends Metal 2 + Wiring 1 and fits the lamp bar in the lamp
    /// socket, once.
    /// </summary>
    public sealed class RackAssemblyTests
    {
        private const string Bay = "rack.headlamp";
        private const float Hold = 1.6f;

        private EventBus _events;
        private MaterialCatalog _materials;
        private MaterialStock _stock;
        private DatabankCatalogue _catalogue;
        private Databank _databank;
        private MakerUnderstanding _understanding;
        private FakeRack _rack;
        private RackAssembly _assembly;
        private List<PartFitted> _fitted;

        [SetUp]
        public void SetUp()
        {
            _events = new EventBus();
            _fitted = new List<PartFitted>();
            _events.Subscribe<PartFitted>(_fitted.Add);
            _materials = CargoFixtures.Catalog();
            _stock = CargoFixtures.Stock(_events, _materials);
            _catalogue = ScriptableObject.CreateInstance<DatabankCatalogue>();
            _catalogue.Populate(new[]
            {
                Entry("db.six", DatabankUnlock.StoryScan("six")),
                Entry("db.post", DatabankUnlock.OnWake),
            });
            _databank = new Databank(_catalogue, _events, new NoSources());
            _understanding = new MakerUnderstanding(_events, _databank, _catalogue, new MakerUnderstandingSettings());
            _understanding.Announce();
            _rack = new FakeRack();
            var bays = new[]
            {
                new RackBaySettings(Bay, true, string.Empty, RoverKitPiece.LampBar),
                new RackBaySettings("rack.cell_pack", true, string.Empty, RoverKitPiece.BatterySpine),
            };
            _assembly = new RackAssembly(_events, _rack, _understanding, _stock, bays, new[]
            {
                new RackAssemblySettings(Bay, MakerUnderstandingSettings.Ssr3Headlamp, new Recipe(2, 1, 0),
                    RoverSocket.Lamp, Hold),
            });
        }

        [TearDown]
        public void TearDown()
        {
            _understanding.Dispose();
            _databank.Dispose();
            Object.DestroyImmediate(_catalogue);
            Object.DestroyImmediate(_materials);
        }

        [Test]
        public void BeforeTheUnlock_TheBayOffersNothing()
        {
            _stock.Stow(new Recipe(4, 2, 0));
            Assert.IsFalse(_assembly.CanAssemble(Bay, out _), "Kessler–Arai is not understood yet");
            HoldFor(Hold * 2f);
            Assert.IsEmpty(_fitted);

            Learn();
            Assert.IsFalse(_assembly.CanAssemble(Bay, out _), "understood, but the opening holds the unlock");
        }

        [Test]
        public void UnlockedAndCovered_AHeldE_FitsTheLampBarInTheLampSocket_AndSpendsItsCost()
        {
            Learn();
            Release();
            _stock.Stow(new Recipe(3, 2, 0));
            Assert.IsTrue(_assembly.CanAssemble(Bay, out bool covered));
            Assert.IsTrue(covered);

            HoldFor(Hold * 0.5f);
            Assert.AreEqual(0.5f, _assembly.Progress01, 0.05f);
            Assert.IsEmpty(_fitted);
            HoldFor(Hold * 0.6f);

            Assert.AreEqual(1, _fitted.Count);
            Assert.AreEqual(RoverSocket.Lamp, _fitted[0].Socket);
            Assert.AreEqual(RoverKitPiece.LampBar, _fitted[0].Part);
            Assert.IsFalse(_fitted[0].Gift);
            Assert.AreEqual(1, _stock.Metal, "Metal 2 spent");
            Assert.AreEqual(1, _stock.Wiring, "Wiring 1 spent");

            _rack.States[0] = RackBayState.Filled;
            HoldFor(Hold * 2f);
            Assert.AreEqual(1, _fitted.Count, "a filled bay assembles nothing more");
        }

        [Test]
        public void WithoutItsCost_TheBayIsAGoalButTheHoldDoesNothing()
        {
            Learn();
            Release();
            _stock.Stow(new Recipe(1, 1, 0));
            Assert.IsTrue(_assembly.CanAssemble(Bay, out bool covered), "the part is unlocked");
            Assert.IsFalse(covered, "one Metal short");
            HoldFor(Hold * 2f);
            Assert.IsEmpty(_fitted);
            Assert.AreEqual(1, _stock.Metal, "nothing spent");
        }

        [Test]
        public void LettingGo_OrReadingAnotherBay_StartsTheHoldAgain()
        {
            Learn();
            Release();
            _stock.Stow(new Recipe(2, 1, 0));
            HoldFor(Hold * 0.8f);
            _assembly.Step(0.1f, Bay, false);
            Assert.AreEqual(0f, _assembly.Progress01, "let go");
            HoldFor(Hold * 0.8f);
            _assembly.Step(0.1f, "rack.cell_pack", true);
            Assert.AreEqual(0f, _assembly.Progress01, "another bay has no assembly");
            _assembly.Step(0.1f, null, true);
            HoldFor(Hold * 0.8f);
            Assert.IsEmpty(_fitted, "never a full hold in one go");
        }

        private void HoldFor(float seconds)
        {
            const float Step = 0.05f;
            for (float t = 0f; t < seconds; t += Step)
            {
                _assembly.Step(Step, Bay, true);
            }
        }

        /// <summary>07's own post (read at the wake) and 06: Kessler–Arai level 1.</summary>
        private void Learn()
        {
            _events.Publish(new BootCheckFinished());
            _events.Publish(new StoryScanned(StoryScan.IdPrefix + "six", "k", Vector3.zero));
            _understanding.Announce();
        }

        private void Release()
        {
            _events.Publish(new OpeningMilestoneReached(OpeningMilestone.SignalWest, false));
        }

        private static DatabankEntry Entry(string id, DatabankUnlock unlock)
        {
            return new DatabankEntry(id, DatabankCategory.Machines, id + ".name", DatabankMaker.KesslerArai, "1978",
                DatabankEra.BuildOut, id + ".readout", id + ".history", DatabankViewKind.Print, null, null, 0f, null,
                unlock);
        }

        private sealed class FakeRack : IRackStatus
        {
            public readonly RackBayState[] States = { RackBayState.Outline, RackBayState.Outline };

            public bool Shown => true;
            public Vector3 Position => Vector3.zero;
            public Quaternion Rotation => Quaternion.identity;
            public int Count => States.Length;

            public string IdOf(int bay)
            {
                return bay == 0 ? Bay : "rack.cell_pack";
            }

            public RackBayState StateOf(int bay)
            {
                return States[bay];
            }
        }

        private sealed class NoSources : IDatabankSources
        {
            public bool IsStoryScanned(string storyId)
            {
                return false;
            }

            public int ScannedInSet(string setId)
            {
                return 0;
            }

            public int SetSize(string setId)
            {
                return 0;
            }

            public bool HasReached(OpeningMilestone milestone)
            {
                return false;
            }
        }
    }
}
