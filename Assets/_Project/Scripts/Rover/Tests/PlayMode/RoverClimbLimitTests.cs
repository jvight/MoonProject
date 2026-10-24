using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using MoonProject.Core;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>The climb limit other domains read is the Rover's own tuning, registered in the context.</summary>
    public sealed class RoverClimbLimitTests
    {
        [UnityTest]
        public IEnumerator TheContextCarriesTheRoversRealClimbLimit()
        {
            using var physics = new LunarTestPhysics();
            using var world = new TestWorld();
            using TestRover rover = TestRover.Spawn(world, TestWorld.Point(0f, -150f), 0f);
            yield return null;
            IRoverClimb climb = rover.Context.Get<IRoverClimb>();
            Assert.AreEqual(rover.Tuning.Ground.SlopeAssistZeroAngle, climb.ClimbLimitDegrees);
            Assert.Greater(climb.ClimbLimitDegrees, rover.Tuning.Ground.SlopeAssistFullAngle);
        }
    }
}
