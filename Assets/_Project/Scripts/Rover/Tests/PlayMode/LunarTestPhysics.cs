using System;
using UnityEngine;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// Puts physics in the game's configuration for a test (lunar gravity, automatic simulation in FixedUpdate at
    /// 50 Hz), whatever an earlier test or the project settings left behind, and restores the previous values.
    /// </summary>
    public sealed class LunarTestPhysics : IDisposable
    {
        private const float LunarGravity = -1.62f;
        private const float FixedStep = 0.02f;

        private readonly Vector3 _gravity;
        private readonly SimulationMode _simulationMode;
        private readonly float _fixedDeltaTime;

        public LunarTestPhysics()
        {
            _gravity = Physics.gravity;
            _simulationMode = Physics.simulationMode;
            _fixedDeltaTime = Time.fixedDeltaTime;
            Physics.gravity = new Vector3(0f, LunarGravity, 0f);
            Physics.simulationMode = SimulationMode.FixedUpdate;
            Time.fixedDeltaTime = FixedStep;
        }

        public void Dispose()
        {
            Physics.gravity = _gravity;
            Physics.simulationMode = _simulationMode;
            Time.fixedDeltaTime = _fixedDeltaTime;
        }
    }
}
