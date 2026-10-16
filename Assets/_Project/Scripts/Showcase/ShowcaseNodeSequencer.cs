using System;
using UnityEngine;

namespace MoonProject.Showcase
{
    /// <summary>
    /// Runtime state for a single animated node rule in <see cref="ShowcaseNodeSequencer"/>. Pure C# with no
    /// UnityEngine.Object dependencies so easing, clamping, continuous spin, and external target overrides can be
    /// tested directly in EditMode.
    /// </summary>
    public sealed class ShowcaseNodeChannel
    {
        private readonly ShowcaseNodeRule _rule;
        private readonly Vector3 _normalizedAxis;
        private bool _isActive;
        private float _progress01;
        private float _spinVelocityFactor01;
        private float _currentValue;
        private bool _hasExternalTarget;
        private float _externalTargetValue;

        public ShowcaseNodeChannel(ShowcaseNodeRule rule)
        {
            _rule = rule ?? throw new ArgumentNullException(nameof(rule));
            _normalizedAxis = rule.Axis.sqrMagnitude > 1e-6f ? rule.Axis.normalized : Vector3.right;
            _isActive = rule.DefaultActive;
            _progress01 = _isActive ? 1f : 0f;
            _spinVelocityFactor01 = _isActive ? 1f : 0f;
            _currentValue = _rule.MotionKind == ShowcaseMotionKind.ContinuousSpin
                ? 0f
                : ClampToLimits(ShowcaseEasing.EvaluateEased(_progress01, _rule.RestValue, _rule.ActiveValue));
        }

        public ShowcaseNodeRule Rule => _rule;
        public Vector3 NormalizedAxis => _normalizedAxis;
        public bool IsActive => _isActive;
        public float Progress01 => _progress01;
        public float SpinVelocityFactor01 => _spinVelocityFactor01;
        public float CurrentValue => _currentValue;
        public bool HasExternalTarget => _hasExternalTarget;
        public float ExternalTargetValue => _externalTargetValue;

        public void Toggle()
        {
            _isActive = !_isActive;
        }

        public void SetActive(bool active)
        {
            _isActive = active;
        }

        /// <summary>
        /// Sets an external target value (e.g. solar tracking angle or camera look-at angle). The channel smoothly
        /// eases <see cref="CurrentValue"/> toward this target over <see cref="ShowcaseNodeRule.EaseDuration"/>.
        /// </summary>
        public void SetExternalTarget(float targetValue)
        {
            _hasExternalTarget = true;
            _externalTargetValue = ClampToLimits(targetValue);
        }

        public void ClearExternalTarget()
        {
            if (!_hasExternalTarget)
            {
                return;
            }

            _hasExternalTarget = false;
            float span = _rule.ActiveValue - _rule.RestValue;
            if (Mathf.Abs(span) > 1e-4f)
            {
                _progress01 = Mathf.Clamp01((_currentValue - _rule.RestValue) / span);
            }
        }

        public void Step(float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            float target01 = _isActive ? 1f : 0f;
            _progress01 = ShowcaseEasing.AdvanceNormalized(_progress01, target01, _rule.EaseDuration, deltaTime);

            if (_rule.MotionKind == ShowcaseMotionKind.ContinuousSpin)
            {
                _spinVelocityFactor01 = ShowcaseEasing.SmootherStep01(_progress01);
                float angularVelocity = _rule.ActiveValue * _spinVelocityFactor01;
                _currentValue = Mathf.Repeat(_currentValue + angularVelocity * deltaTime, 360f);
                return;
            }

            if (_hasExternalTarget)
            {
                _currentValue = ClampToLimits(
                    ShowcaseEasing.SmoothScalar(_currentValue, _externalTargetValue, _rule.EaseDuration, deltaTime));
                return;
            }

            float evaluated = ShowcaseEasing.EvaluateEased(_progress01, _rule.RestValue, _rule.ActiveValue);
            _currentValue = ClampToLimits(evaluated);
        }

        private float ClampToLimits(float value)
        {
            return Mathf.Clamp(value, _rule.MinLimit, _rule.MaxLimit);
        }
    }

    /// <summary>
    /// Plain C# sequencer managing all <see cref="ShowcaseNodeChannel"/>s for a single exhibit.
    /// </summary>
    public sealed class ShowcaseNodeSequencer
    {
        private readonly ShowcaseNodeChannel[] _channels;

        public ShowcaseNodeSequencer(ShowcaseNodeRule[] rules)
        {
            if (rules == null || rules.Length == 0)
            {
                _channels = Array.Empty<ShowcaseNodeChannel>();
                return;
            }

            _channels = new ShowcaseNodeChannel[rules.Length];
            for (int i = 0; i < rules.Length; i++)
            {
                _channels[i] = new ShowcaseNodeChannel(rules[i]);
            }
        }

        public int Count => _channels.Length;

        public ShowcaseNodeChannel GetChannel(int index)
        {
            return _channels[index];
        }

        public ShowcaseNodeChannel FindChannel(string ruleId)
        {
            if (string.IsNullOrEmpty(ruleId))
            {
                return null;
            }

            for (int i = 0; i < _channels.Length; i++)
            {
                if (string.Equals(_channels[i].Rule.RuleId, ruleId, StringComparison.Ordinal))
                {
                    return _channels[i];
                }
            }

            return null;
        }

        /// <summary>Toggles all channels bound to <paramref name="key"/> and returns the number of channels toggled.</summary>
        public int ToggleKey(ShowcaseTriggerKey key)
        {
            if (key == ShowcaseTriggerKey.None)
            {
                return 0;
            }

            int toggled = 0;
            for (int i = 0; i < _channels.Length; i++)
            {
                if (_channels[i].Rule.TriggerKey == key)
                {
                    _channels[i].Toggle();
                    toggled++;
                }
            }

            return toggled;
        }

        public bool IsKeyActive(ShowcaseTriggerKey key)
        {
            for (int i = 0; i < _channels.Length; i++)
            {
                if (_channels[i].Rule.TriggerKey == key)
                {
                    return _channels[i].IsActive;
                }
            }

            return false;
        }

        public void SetKeyActive(ShowcaseTriggerKey key, bool active)
        {
            for (int i = 0; i < _channels.Length; i++)
            {
                if (_channels[i].Rule.TriggerKey == key)
                {
                    _channels[i].SetActive(active);
                }
            }
        }

        public void SetRuleActive(string ruleId, bool active)
        {
            ShowcaseNodeChannel channel = FindChannel(ruleId);
            channel?.SetActive(active);
        }

        public void SetExternalTarget(string ruleId, float targetValue)
        {
            ShowcaseNodeChannel channel = FindChannel(ruleId);
            channel?.SetExternalTarget(targetValue);
        }

        public void ClearExternalTarget(string ruleId)
        {
            ShowcaseNodeChannel channel = FindChannel(ruleId);
            channel?.ClearExternalTarget();
        }

        public void Step(float deltaTime)
        {
            for (int i = 0; i < _channels.Length; i++)
            {
                _channels[i].Step(deltaTime);
            }
        }
    }
}
