using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The repair as a timeline from its start (pure curves): 07's beam stitches for the friend's repair duration
    /// while it shivers harder; then it boots — the eye flickers and steadies while the rotors spin up, it wobbles up
    /// into the air, then turns to look at 07. Once begun it always plays to the end (nothing to fail).
    /// </summary>
    public sealed class RepairSequence
    {
        // A flicker pattern (on/off steps per second), irregular so it reads as an old circuit catching.
        private const float FlickerSteps = 9f;
        private const float FlickerOn = 0.55f;

        /// <summary>How far the flicker's "on" share runs ahead of the progress (it steadies before the end).</summary>
        private const float FlickerLead = 0.15f;

        private readonly float _stitch;
        private readonly float _eye;
        private readonly float _rotors;
        private readonly float _lift;
        private readonly float _look;

        public RepairSequence(float stitchDuration, float eyeFlicker, float rotorSpinUp, float liftOff, float lookAt)
        {
            _stitch = Mathf.Max(0.01f, stitchDuration);
            _eye = Mathf.Max(0.01f, eyeFlicker);
            _rotors = Mathf.Max(0.01f, rotorSpinUp);
            _lift = Mathf.Max(0.01f, liftOff);
            _look = Mathf.Max(0.01f, lookAt);
        }

        public static RepairSequence For(FriendDefinition definition, FriendTuning tuning)
        {
            return new RepairSequence(definition.RepairDuration, tuning.EyeFlicker, tuning.RotorSpinUp,
                tuning.LiftOff, tuning.LookAt07);
        }

        /// <summary>When the stitching ends and the boot-up begins (s).</summary>
        public float StitchEnd => _stitch;

        /// <summary>When it starts to lift (s): once the eye is steady and the rotors are up to speed.</summary>
        public float LiftStart => _stitch + Mathf.Max(_eye, _rotors);

        /// <summary>When it is up and starts turning to 07 (s).</summary>
        public float LookStart => LiftStart + _lift;

        /// <summary>The whole sequence (s): at its end it is awake.</summary>
        public float Duration => LookStart + _look;

        public bool Stitching(float t)
        {
            return t >= 0f && t < _stitch;
        }

        public bool Done(float t)
        {
            return t >= Duration;
        }

        /// <summary>0..1 through the stitching.</summary>
        public float StitchProgress(float t)
        {
            return Mathf.Clamp01(t / _stitch);
        }

        /// <summary>0 dark .. 1 on: flickers after the stitching, then stays on.</summary>
        public float Eye(float t)
        {
            float boot = t - _stitch;
            if (boot < 0f)
            {
                return 0f;
            }

            if (boot >= _eye)
            {
                return 1f;
            }

            float progress = boot / _eye;
            float step = Mathf.Floor(boot * FlickerSteps);
            bool on = Mathf.Repeat(step * FlickerOn + progress, 1f) < progress + FlickerLead;
            return on ? Ease.OutCubic(progress) : 0f;
        }

        /// <summary>0..1 rotor speed: still while broken, spinning up after the stitching.</summary>
        public float Rotors(float t)
        {
            return Ease.InOutSine((t - _stitch) / _rotors);
        }

        /// <summary>0..1 up into the air (and its rig rolled upright).</summary>
        public float Lift(float t)
        {
            return Ease.InOutSine((t - LiftStart) / _lift);
        }

        /// <summary>0..1 turned toward 07.</summary>
        public float Look(float t)
        {
            return Ease.InOutSine((t - LookStart) / _look);
        }
    }
}
