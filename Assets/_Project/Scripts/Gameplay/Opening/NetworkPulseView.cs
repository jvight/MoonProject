using System;
using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The answer's pulse of the old network, seen (<see cref="CallAnswer"/>): a bright streak that arcs from the top of
    /// the antenna mast out to relay.1, and on to the Peak, where it glints on the dark dish. Each leg runs its own
    /// seconds from <see cref="NetworkPulseLaunched"/>; the glint flares and fades from <see cref="PeakDishGlinted"/>.
    /// Allocation-free per frame.
    /// </summary>
    public sealed class NetworkPulseView : IDisposable
    {
        private const int Points = 24;
        private const float FadeOutShare = 0.08f;
        private const float TailStartWidth = 0.2f;

        private readonly CallAnswerSettings _settings;
        private readonly LineRenderer _line;
        private readonly GlowFlashPool _flashes;
        private readonly Vector3[] _points = new Vector3[Points];
        private readonly IDisposable[] _tokens;
        private Vector3 _from;
        private Vector3 _to;
        private float _seconds;
        private float _age;
        private bool _running;

        public NetworkPulseView(Transform parent, Material beam, Mesh sphere, Material flash, EventBus events,
            CallAnswerSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            var host = new GameObject("NetworkPulse");
            host.transform.SetParent(parent, false);
            _line = host.AddComponent<LineRenderer>();
            _line.sharedMaterial = beam;
            _line.useWorldSpace = true;
            _line.positionCount = Points;
            _line.numCapVertices = 2;
            _line.shadowCastingMode = ShadowCastingMode.Off;
            _line.receiveShadows = false;
            _line.lightProbeUsage = LightProbeUsage.Off;
            _line.reflectionProbeUsage = ReflectionProbeUsage.Off;
            _line.enabled = false;
            _flashes = new GlowFlashPool(host.transform, sphere, flash, 2, settings.GlintSeconds, settings.GlintRadius,
                settings.GlintIntensity);
            _tokens = new[]
            {
                events.Subscribe<NetworkPulseLaunched>(OnLaunched),
                events.Subscribe<PeakDishGlinted>(glint => _flashes.Spawn(glint.Position)),
            };
        }

        /// <summary>A leg of the pulse is running (tests).</summary>
        public bool IsRunning => _running;

        /// <summary>The pulse's head now (tests).</summary>
        public Vector3 Head { get; private set; }

        public void Step(float deltaTime)
        {
            _flashes.Tick(deltaTime);
            if (!_running)
            {
                return;
            }

            _age += deltaTime;
            float head = Mathf.Clamp01(_age / _seconds);
            float tail = Mathf.Clamp01(head - _settings.TailShare);
            for (int i = 0; i < Points; i++)
            {
                float t = Mathf.Lerp(tail, head, i / (float)(Points - 1));
                _points[i] = Along(t);
            }

            _line.SetPositions(_points);
            Head = _points[Points - 1];
            Color color = _settings.PulseColor;
            color.a = Mathf.Clamp01((1f - head) / FadeOutShare);
            _line.startColor = new Color(color.r, color.g, color.b, 0f);
            _line.endColor = color;
            _line.startWidth = _settings.PulseWidth * TailStartWidth;
            _line.endWidth = _settings.PulseWidth;
            if (_age >= _seconds)
            {
                _running = false;
                _line.enabled = false;
            }
        }

        public void Dispose()
        {
            foreach (IDisposable token in _tokens)
            {
                token.Dispose();
            }
        }

        private void OnLaunched(NetworkPulseLaunched launched)
        {
            _from = launched.Leg == 0 ? launched.From + Vector3.up * _settings.MastTopHeight
                : launched.From + Vector3.up * _settings.RelayLampHeight;
            _to = launched.Leg == 0 ? launched.To + Vector3.up * _settings.RelayLampHeight : launched.To;
            _seconds = Mathf.Max(0.1f, launched.Seconds);
            _age = 0f;
            _running = true;
            _line.enabled = true;
        }

        private Vector3 Along(float t)
        {
            float rise = Mathf.Min(_settings.ArcMaxRise, Vector3.Distance(_from, _to) * _settings.ArcRise);
            return Vector3.Lerp(_from, _to, t) + Vector3.up * (rise * Mathf.Sin(Mathf.PI * t));
        }
    }
}
