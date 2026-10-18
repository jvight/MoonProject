using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World.Tests
{
    /// <summary>
    /// The lander's blast-swept zone (opening v2 §4): swept bare inside, untouched beyond, the swept pebbles piled
    /// round its edge, and the planned scatter keeping to that.
    /// </summary>
    public sealed class BlastZoneTests
    {
        private static readonly Vector2 Center = new Vector2(30f, 150f);

        private readonly BlastZoneSettings _settings = new BlastZoneSettings();

        [Test]
        public void TheGround_IsSweptInside_AndUntouchedBeyondTheEdge()
        {
            var zone = new BlastZone(Center, _settings);
            Assert.AreEqual(1f, zone.Swept(Center.x, Center.y), 1e-5f, "at the lander");
            Assert.AreEqual(1f, zone.Swept(Center.x + _settings.Radius - _settings.EdgeBand, Center.y), 1e-5f,
                "up to the edge band");
            float midBand = zone.Swept(Center.x + _settings.Radius - _settings.EdgeBand * 0.5f, Center.y);
            Assert.That(midBand, Is.GreaterThan(0f).And.LessThan(1f), "easing across the band");
            Assert.AreEqual(0f, zone.Swept(Center.x + _settings.Radius, Center.y), 1e-5f, "at the radius");
        }

        [Test]
        public void TheSweptPebbles_PileJustOutsideTheEdge()
        {
            var zone = new BlastZone(Center, _settings);
            float pile = Center.x + _settings.Radius + _settings.EdgePileWidth * 0.5f;
            Assert.AreEqual(1f, zone.EdgePile(pile, Center.y), 1e-5f, "the pile's crest");
            Assert.AreEqual(0f, zone.EdgePile(Center.x, Center.y), 1e-5f, "none at the lander");
            Assert.AreEqual(0f, zone.EdgePile(pile + _settings.EdgePileWidth, Center.y), 1e-5f, "none far out");
            Assert.AreEqual(0f, zone.ScatterChance(Center.x + 5f, Center.y, 1f, true), 1e-5f,
                "no pebble on swept ground");
            Assert.Greater(zone.ScatterChance(pile, Center.y, 0.2f, true), 0.2f, "more pebbles on the pile");
            Assert.AreEqual(0.2f, zone.ScatterChance(pile, Center.y, 0.2f, false), 1e-5f,
                "the pile is pebbles, not boulders");
        }

        [Test]
        public void ThePlannedScatter_LeavesTheSweptZoneBare_AndPilesItsEdge()
        {
            var surface = new MoonSurface(new SurfaceSettings(), WorldSettings.DefaultSeed);
            BlastZone zone = surface.Shaping.Blast;
            List<ScatterInstance> plan = new ScatterPlanner(surface, new ScatterSettings(),
                new WorldAnchors(surface, new SurfaceSettings().Canyon, new RelaySettings())).Plan();
            float radius = zone.Settings.Radius;
            float pileInner = radius;
            float pileOuter = radius + zone.Settings.EdgePileWidth;
            float beyondOuter = Mathf.Sqrt(2f * pileOuter * pileOuter - pileInner * pileInner);
            int onPile = 0;
            int beyond = 0;
            foreach (ScatterInstance rock in plan)
            {
                float distance = Vector2.Distance(new Vector2(rock.Position.x, rock.Position.z), zone.Center);
                Assert.IsFalse(zone.Swept(rock.Position.x, rock.Position.z) >= 1f,
                    $"a {rock.Kind} at {rock.Position} lies on the fully swept ground");
                onPile += rock.Kind == ScatterKind.Pebble && distance >= pileInner && distance < pileOuter ? 1 : 0;
                beyond += rock.Kind == ScatterKind.Pebble && distance >= pileOuter && distance < beyondOuter ? 1 : 0;
            }

            Assert.Greater(onPile, beyond, "the pile ring holds more pebbles than a ring of equal area beyond it");
        }
    }
}
