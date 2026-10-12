using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Testing;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// Where 07 can charge next (<see cref="IChargerReach"/>, docs/design/first-hour-qa.md #8): with no relay lit, the
    /// dock, named; the way there costs more of the battery the farther 07 goes, with the Low warning's margins.
    /// </summary>
    public sealed class ChargerReachSession : InputTestFixture
    {
        private const float Far = 120f;

        private InputActionAsset _controls;
        private GameplayFixture _fixture;

        public override void Setup()
        {
            base.Setup();
            _controls = BootstrapHarness.LoadControlsCopy();
        }

        public override void TearDown()
        {
            _fixture?.Dispose();
            Object.Destroy(_controls);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator TheDock_IsTheChargerToReach_AndFartherCostsMore()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            var reach = _fixture.Bootstrap.Context.Get<IChargerReach>();
            ChargingDock dock = _fixture.Gameplay.Home.Dock;
            _fixture.Rover.Place(dock.Position + Vector3.forward * 20f, 0f);
            yield return null;
            yield return null;
            Assert.IsTrue(reach.HasCharger);
            Assert.AreEqual("hop.node.home", reach.NearestNameKey, "the dock, by home's name");
            Assert.Less(SurfaceRules.HorizontalDistance(reach.NearestPosition, dock.Position), 0.01f);
            float near = reach.ChargeToReach;

            _fixture.Rover.Place(dock.Position + Vector3.forward * Far, 0f);
            yield return null;
            yield return null;
            Assert.AreEqual(Far, reach.DistanceToNearest, 1f);
            Assert.Greater(reach.ChargeToReach, near, "farther from home costs more of the battery");
            Assert.LessOrEqual(reach.ChargeToReach, 1f);
        }
    }
}
