using System;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using Object = UnityEngine.Object;

namespace MoonProject.Rover.Tests
{
    public sealed class RoverAbilitiesTests
    {
        private GameObject _host;
        private IRoverAbilities _abilities;

        [SetUp]
        public void SetUp()
        {
            _host = new GameObject("Rover");
            _abilities = _host.AddComponent<RoverController>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_host);
        }

        [Test]
        public void NothingIsGrantedByDefault()
        {
            foreach (RoverAbility ability in Enum.GetValues(typeof(RoverAbility)))
            {
                Assert.IsFalse(_abilities.Has(ability), ability.ToString());
            }
        }

        [Test]
        public void Grant_IsIdempotent_AndOnlyGrantsThatAbility()
        {
            _abilities.Grant(RoverAbility.HoverJump);
            _abilities.Grant(RoverAbility.HoverJump);
            Assert.IsTrue(_abilities.Has(RoverAbility.HoverJump));
            Assert.IsFalse(_abilities.Has(RoverAbility.MagneticTreads));
            _abilities.Grant(RoverAbility.BoostCoils);
            Assert.IsTrue(_abilities.Has(RoverAbility.BoostCoils));
            Assert.IsTrue(_abilities.Has(RoverAbility.HoverJump));
        }
    }
}
