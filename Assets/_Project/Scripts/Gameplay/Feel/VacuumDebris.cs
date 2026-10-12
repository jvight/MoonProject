using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Vacuum-honest debris (docs/design/feel-pass.md R11, §3.1): sparks from the cut and slag dropping off a
    /// tumbling piece fly in clean ballistic arcs under lunar gravity with no drag, bounce once, then lie as slag
    /// beads that glow and cool out. No smoke anywhere. Fixed pools in preallocated arrays: nothing is allocated
    /// after construction.
    /// </summary>
    public sealed class VacuumDebris
    {
        private const float GroundProbe = 0.3f;
        private const float GroundReach = 30f;

        // A dropped bead leaves the piece with this share of the slowest spark speed in a random direction, and a
        // bead shrinks by this share as it cools.
        private const float DropKick = 0.5f;
        private const float CoolShrink = 0.4f;

        // A fallen puff starts within this many seconds of its drift from the object's centre.
        private const float DustSpread = 0.3f;

        // Ring puffs are spaced evenly round the ring, each nudged by up to this share of the spacing.
        private const float RingJitter = 0.5f;

        private readonly FeelVfxTuning _tuning;
        private readonly FeelSprites _sparkSprites;
        private readonly FeelSprites _beadSprites;
        private readonly Vector3[] _position;
        private readonly Vector3[] _velocity;
        private readonly float[] _age;
        private readonly float[] _ground;
        private readonly float[] _size;
        private readonly bool[] _bounced;
        private readonly Vector3[] _beadPosition;
        private readonly float[] _beadAge;
        private readonly float[] _beadLife;
        private readonly float[] _beadSize;
        private readonly FeelSprites _dustSprites;
        private readonly Vector3[] _dustPosition;
        private readonly Vector3[] _dustVelocity;
        private readonly float[] _dustAge;
        private readonly float[] _dustGround;
        private int _dust;
        private int _flying;
        private int _resting;
        private float _owed;
        private Vector3 _sprayAt;
        private Vector3 _sprayNormal;
        private float _sprayLevel = -1f;

        public VacuumDebris(Transform parent, FeelVfxTuning tuning, Material sprite)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            int sparks = tuning.SparkPool;
            int beads = tuning.BeadPool;
            _sparkSprites = new FeelSprites("VacuumSparks", parent, sprite, sparks, true, tuning.SparkStreak);
            _beadSprites = new FeelSprites("SlagBeads", parent, sprite, beads, false, 0f);
            _position = new Vector3[sparks];
            _velocity = new Vector3[sparks];
            _age = new float[sparks];
            _ground = new float[sparks];
            _size = new float[sparks];
            _bounced = new bool[sparks];
            _beadPosition = new Vector3[beads];
            _beadAge = new float[beads];
            _beadLife = new float[beads];
            _beadSize = new float[beads];
            int dust = tuning.DustPool;
            _dustSprites = new FeelSprites("VacuumDust", parent, sprite, dust, false, 0f);
            _dustPosition = new Vector3[dust];
            _dustVelocity = new Vector3[dust];
            _dustAge = new float[dust];
            _dustGround = new float[dust];
        }

        /// <summary>Dust puffs in the air or settling (tests and debugging views).</summary>
        public int Dust => _dust;

        /// <summary>
        /// A ring of dust thrown out from <paramref name="centre"/> along the ground, <paramref name="count"/> puffs at
        /// a speed by <paramref name="strength"/> (0..1): they arc, land and settle (a leap's take-off, a landing).
        /// </summary>
        public void Ring(Vector3 centre, int count, float strength)
        {
            float speed = Mathf.Lerp(_tuning.DustSpeed.x, _tuning.DustSpeed.y, Mathf.Clamp01(strength));
            for (int i = 0; i < count; i++)
            {
                float angle = (i + UnityEngine.Random.value * RingJitter) * Mathf.PI * 2f / Mathf.Max(count, 1);
                var flat = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Puff(centre + flat * _tuning.RingRadius, (flat + Vector3.up * _tuning.DustRise) * speed);
            }
        }

        /// <summary>
        /// Dust shaken off an object at <paramref name="position"/>: it falls away under lunar gravity.
        /// </summary>
        public void Fall(Vector3 position, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 drift = UnityEngine.Random.insideUnitSphere * _tuning.DustSpeed.x * DropKick;
                Puff(position + drift * DustSpread, new Vector3(drift.x, -Mathf.Abs(drift.y), drift.z));
            }
        }

        /// <summary>Sparks and slag in the air (tests and debugging views).</summary>
        public int Flying => _flying;

        /// <summary>Slag beads lying on the ground, cooling (tests and debugging views).</summary>
        public int Resting => _resting;

        /// <summary>
        /// Sprays sparks from the cut at <paramref name="position"/> round <paramref name="normal"/>, at a rate rising
        /// with <paramref name="progress"/> (0..1); a negative progress stops the spray (sparks in the air keep
        /// falling).
        /// </summary>
        public void Spray(Vector3 position, Vector3 normal, float progress)
        {
            _sprayAt = position;
            _sprayNormal = normal.sqrMagnitude > 1e-6f ? normal.normalized : Vector3.up;
            _sprayLevel = progress;
        }

        /// <summary>
        /// Throws <paramref name="count"/> sparks out of <paramref name="position"/> round a direction.
        /// </summary>
        public void Vent(Vector3 position, Vector3 direction, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Launch(position, Scatter(direction), _tuning.SparkSize);
            }
        }

        /// <summary>Drops <paramref name="count"/> slag beads off a tumbling piece, flung gently from it.</summary>
        public void Drop(Vector3 position, Vector3 velocity, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 kick = UnityEngine.Random.insideUnitSphere * _tuning.SparkSpeed.x * DropKick;
                Launch(position, velocity + kick, _tuning.SlagSize);
            }
        }

        public void Step(float deltaTime)
        {
            StepFlying(deltaTime);
            StepResting(deltaTime);
            StepDust(deltaTime);
            if (_sprayLevel >= 0f)
            {
                float rate = Mathf.Lerp(_tuning.SparkRate.x, _tuning.SparkRate.y, _sprayLevel);
                _owed += rate * deltaTime;
                while (_owed >= 1f)
                {
                    _owed -= 1f;
                    Launch(_sprayAt, Scatter(_sprayNormal), _tuning.SparkSize);
                }
            }

            Draw();
        }

        /// <summary>Stops the spray at once; sparks already flying finish their arcs.</summary>
        public void StopSpray()
        {
            _sprayLevel = -1f;
            _owed = 0f;
        }

        private Vector3 Scatter(Vector3 direction)
        {
            Quaternion toward = Quaternion.FromToRotation(Vector3.forward, direction);
            float spread = _tuning.SparkSpread;
            Vector3 cone = Quaternion.Euler(UnityEngine.Random.Range(-spread, spread),
                UnityEngine.Random.Range(-spread, spread), 0f) * Vector3.forward;
            float speed = UnityEngine.Random.Range(_tuning.SparkSpeed.x, _tuning.SparkSpeed.y);
            return toward * cone * speed;
        }

        private void Launch(Vector3 position, Vector3 velocity, float size)
        {
            if (_flying >= _position.Length)
            {
                return;
            }

            int i = _flying++;
            _position[i] = position;
            _velocity[i] = velocity;
            _age[i] = 0f;
            _size[i] = size;
            _bounced[i] = false;
            _ground[i] = Physics.Raycast(position + Vector3.up * GroundProbe, Vector3.down, out RaycastHit hit,
                GroundReach) ? hit.point.y : position.y - GroundReach;
        }

        private void StepFlying(float deltaTime)
        {
            for (int i = _flying - 1; i >= 0; i--)
            {
                _age[i] += deltaTime;
                bool landed = FeelCurves.Ballistic(ref _position[i], ref _velocity[i], deltaTime, _tuning.Gravity,
                    _ground[i], _tuning.Restitution);
                if (landed && _bounced[i])
                {
                    Rest(_position[i], _size[i]);
                    RemoveFlying(i);
                }
                else if (landed)
                {
                    _bounced[i] = true;
                }
                else if (_age[i] > _tuning.SparkMaxFlight)
                {
                    RemoveFlying(i);
                }
            }
        }

        private void Puff(Vector3 position, Vector3 velocity)
        {
            if (_dust >= _dustPosition.Length)
            {
                return;
            }

            int i = _dust++;
            _dustPosition[i] = position;
            _dustVelocity[i] = velocity;
            _dustAge[i] = 0f;
            _dustGround[i] = Physics.Raycast(position + Vector3.up * GroundProbe, Vector3.down, out RaycastHit hit,
                GroundReach) ? hit.point.y : position.y - GroundReach;
        }

        private void StepDust(float deltaTime)
        {
            for (int i = _dust - 1; i >= 0; i--)
            {
                _dustAge[i] += deltaTime;
                if (_dustAge[i] >= _tuning.DustSettleSeconds)
                {
                    int last = --_dust;
                    _dustPosition[i] = _dustPosition[last];
                    _dustVelocity[i] = _dustVelocity[last];
                    _dustAge[i] = _dustAge[last];
                    _dustGround[i] = _dustGround[last];
                    continue;
                }

                if (FeelCurves.Ballistic(ref _dustPosition[i], ref _dustVelocity[i], deltaTime, _tuning.Gravity,
                    _dustGround[i], 0f))
                {
                    _dustVelocity[i] = Vector3.zero;
                }
            }
        }

        private void Rest(Vector3 position, float size)
        {
            if (_resting >= _beadPosition.Length)
            {
                return;
            }

            int b = _resting++;
            _beadPosition[b] = position;
            _beadAge[b] = 0f;
            _beadLife[b] = UnityEngine.Random.Range(_tuning.BeadLifetime.x, _tuning.BeadLifetime.y);
            _beadSize[b] = Mathf.Max(size, _tuning.SparkSize);
        }

        private void StepResting(float deltaTime)
        {
            for (int b = _resting - 1; b >= 0; b--)
            {
                _beadAge[b] += deltaTime;
                if (_beadAge[b] < _beadLife[b])
                {
                    continue;
                }

                int last = --_resting;
                _beadPosition[b] = _beadPosition[last];
                _beadAge[b] = _beadAge[last];
                _beadLife[b] = _beadLife[last];
                _beadSize[b] = _beadSize[last];
            }
        }

        private void RemoveFlying(int i)
        {
            int last = --_flying;
            _position[i] = _position[last];
            _velocity[i] = _velocity[last];
            _age[i] = _age[last];
            _ground[i] = _ground[last];
            _size[i] = _size[last];
            _bounced[i] = _bounced[last];
        }

        private void Draw()
        {
            _sparkSprites.Begin();
            for (int i = 0; i < _flying; i++)
            {
                _sparkSprites.Add(_position[i], _size[i], _tuning.SparkColor, _velocity[i]);
            }

            _sparkSprites.End();
            _beadSprites.Begin();
            for (int b = 0; b < _resting; b++)
            {
                float cooled = _beadAge[b] / _beadLife[b];
                Color color = Color.Lerp(_tuning.SparkColor, _tuning.BeadCoolColor, cooled);
                color.a = 1f - cooled * cooled;
                _beadSprites.Add(_beadPosition[b], _beadSize[b] * (1f - CoolShrink * cooled), color, Vector3.zero);
            }

            _beadSprites.End();
            _dustSprites.Begin();
            for (int i = 0; i < _dust; i++)
            {
                float settled = _dustAge[i] / _tuning.DustSettleSeconds;
                Color color = _tuning.DustColor;
                color.a *= 1f - settled * settled;
                _dustSprites.Add(_dustPosition[i], _tuning.DustSize * (1f + settled), color, Vector3.zero);
            }

            _dustSprites.End();
        }
    }
}
