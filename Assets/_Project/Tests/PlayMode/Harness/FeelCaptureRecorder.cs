using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace MoonProject.Testing
{
    /// <summary>
    /// Reusable PlayMode feel-capture recorder for <c>Main.unity</c> scenarios. Records per-frame driving, camera,
    /// clipping, FPS and GC allocation metrics, captures world (or world + UI composite) PNG frames, assembles a
    /// contact-sheet strip PNG, and writes <c>metrics.json</c> under <c>&lt;out&gt;/&lt;scenario&gt;/</c>.
    /// </summary>
    public sealed class FeelCaptureRecorder : IDisposable
    {
        private const string OutEnvVar = "MOON_FEEL_OUT";
        private const string AllocatedInFrameCounter = "GC Allocated In Frame";
        private const float SampleIntervalSeconds = 0.1f;
        private const float JerkWindowSeconds = 0.05f;
        private const float ClippingClearanceMeters = 0.08f;
        private const float StopSpeedThreshold = 0.15f;
        private const int ThumbWidth = 480;
        private const int ThumbHeight = 270;
        private const int StripColumns = 4;
        private const int StripPadding = 8;

        private readonly string _scenario;
        private readonly string _scenarioDir;
        private readonly Camera _camera;
        private readonly IRoverState _rover;
        private readonly ITerrainQuery _terrain;
        private readonly UIDocument _uiDocument;
        private readonly int _width;
        private readonly int _height;
        private readonly float _startTime;

        private readonly List<TimeSampleJson> _samples = new List<TimeSampleJson>();
        private readonly List<CaptureEntryJson> _captures = new List<CaptureEntryJson>();
        private readonly List<NamedMetricJson> _customMetrics = new List<NamedMetricJson>();
        private readonly List<NamedNoteJson> _customNotes = new List<NamedNoteJson>();
        private readonly List<Color32[]> _thumbnails = new List<Color32[]>();
        private readonly List<float> _frameFps = new List<float>();
        private readonly List<long> _frameGcBytes = new List<long>();

        private ProfilerRecorder _gcRecorder;
        private PanelSettings _originalPanelSettings;
        private PanelSettings _capturePanelSettings;
        private RenderTexture _uiTarget;
        private bool _uiRedirected;

        private long _lastWallTimestamp;
        private int _skipPerfFrames;
        private int _frameCount;
        private float _nextSampleTime;

        private Vector3 _prevRoverPos;
        private float _prevRoverYaw;
        private Vector3 _prevCamPos;
        private Vector3 _prevCamForward;
        private Vector3 _prevWindowCamPos;
        private Vector3 _prevWindowCamVel;
        private Vector3 _prevWindowCamAcc;
        private Vector3 _prevWindowCamForward;
        private float _prevWindowAngularSpeed;
        private float _jerkWindowElapsed;
        private int _jerkWindowCount;

        private float _throttleStartTime = -1f;
        private float _timeToCruise = -1f;
        private float _coastStartTime = -1f;
        private float _timeToStop = -1f;
        private bool _wasAtCruise;

        private float _peakYawRateLowSpeed;
        private float _peakYawRateHighSpeed;
        private float _highSpeedTurnSpeedSum;
        private float _highSpeedTurnYawRateSum;
        private int _highSpeedTurnSamples;
        private float _steerReleaseTime = -1f;
        private float _steerReleaseHeading;
        private float _postReleaseYawOvershoot;

        private float _posLagSum;
        private float _maxPosLag;
        private float _yawLagSum;
        private float _maxYawLag;
        private int _lagSamples;

        private float _jerkSum;
        private float _maxJerk;
        private float _maxTurnRate;
        private float _maxAngularJerk;

        private int _terrainClipFrames;
        private int _geometryClipFrames;
        private float _minCameraClearance = 100f;
        private float _maxRoverGroundPenetration;

        public FeelCaptureRecorder(
            string scenario,
            Camera camera,
            IRoverState rover,
            ITerrainQuery terrain,
            UIDocument uiDocument = null,
            int width = 1280,
            int height = 720)
        {
            _scenario = scenario ?? throw new ArgumentNullException(nameof(scenario));
            _camera = camera ?? throw new ArgumentNullException(nameof(camera));
            _rover = rover ?? throw new ArgumentNullException(nameof(rover));
            _terrain = terrain;
            _uiDocument = uiDocument;
            _width = width;
            _height = height;
            _startTime = Time.time;

            string envOut = Environment.GetEnvironmentVariable(OutEnvVar);
            string rootOut = string.IsNullOrWhiteSpace(envOut)
                ? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "audit"))
                : Path.GetFullPath(envOut);
            _scenarioDir = Path.Combine(rootOut, _scenario);
            PrepareDirectory(_scenarioDir);

            _gcRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, AllocatedInFrameCounter);
            _lastWallTimestamp = Stopwatch.GetTimestamp();
            _skipPerfFrames = 2;

            _prevRoverPos = _rover.Position;
            _prevRoverYaw = _rover.Rotation.eulerAngles.y;
            _prevCamPos = _camera.transform.position;
            _prevCamForward = _camera.transform.forward;
            _prevWindowCamPos = _prevCamPos;
            _prevWindowCamForward = _prevCamForward;

            Texture2D warmup = FrameCapture.Render(_camera, _width, _height);
            Object.Destroy(warmup);
        }

        public string ScenarioDirectory => _scenarioDir;

        public float ElapsedSeconds => Time.time - _startTime;

        public void EnableUiComposite(bool enable)
        {
            if (enable == _uiRedirected || _uiDocument == null || _uiDocument.panelSettings == null)
            {
                return;
            }

            if (enable)
            {
                _originalPanelSettings = _uiDocument.panelSettings;
                _uiTarget = new RenderTexture(_width, _height, 24, RenderTextureFormat.ARGB32)
                {
                    name = $"FeelCaptureUI_{_scenario}",
                };
                _uiTarget.Create();
                _capturePanelSettings = Object.Instantiate(_originalPanelSettings);
                _capturePanelSettings.targetTexture = _uiTarget;
                _capturePanelSettings.clearColor = true;
                _capturePanelSettings.colorClearValue = new Color(0f, 0f, 0f, 0f);
                _uiDocument.panelSettings = _capturePanelSettings;
                _uiDocument.rootVisualElement?.MarkDirtyRepaint();
                _uiRedirected = true;
            }
            else
            {
                RestoreUiPanel();
            }
        }

        public void ResetBaseline()
        {
            _prevRoverPos = _rover.Position;
            _prevRoverYaw = _rover.Rotation.eulerAngles.y;
            _prevCamPos = _camera.transform.position;
            _prevCamForward = _camera.transform.forward;
            _prevWindowCamPos = _prevCamPos;
            _prevWindowCamVel = Vector3.zero;
            _prevWindowCamAcc = Vector3.zero;
            _prevWindowCamForward = _prevCamForward;
            _prevWindowAngularSpeed = 0f;
            _jerkWindowElapsed = 0f;
            _skipPerfFrames = 2;
            _lastWallTimestamp = Stopwatch.GetTimestamp();
        }

        public void StepFrame(
            float desiredCameraDistance = 5.4f,
            float targetCruiseSpeed = 8.0f)
        {
            long nowWall = Stopwatch.GetTimestamp();
            double wallSeconds = (double)(nowWall - _lastWallTimestamp) / Stopwatch.Frequency;
            _lastWallTimestamp = nowWall;

            if (_skipPerfFrames > 0)
            {
                _skipPerfFrames--;
            }
            else
            {
                if (wallSeconds > 1e-5 && wallSeconds < 2.0)
                {
                    _frameFps.Add((float)(1.0 / wallSeconds));
                }

                if (_gcRecorder.Valid)
                {
                    _frameGcBytes.Add(Math.Max(0L, _gcRecorder.LastValue));
                }
            }

            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            float t = Time.time - _startTime;
            _frameCount++;

            Vector3 roverPos = _rover.Position;
            Quaternion roverRot = _rover.Rotation;
            float roverYaw = roverRot.eulerAngles.y;
            Vector3 velocity = _rover.Velocity;
            float speed = _rover.Speed;
            Vector3 roverForward = roverRot * Vector3.forward;
            roverForward.y = 0f;
            if (roverForward.sqrMagnitude > 1e-4f)
            {
                roverForward.Normalize();
            }
            else
            {
                roverForward = Vector3.forward;
            }

            float forwardSpeed = Vector3.Dot(velocity, roverForward);
            float yawRate = Mathf.DeltaAngle(_prevRoverYaw, roverYaw) / dt;
            Vector2 input = _rover.DriveInput;

            StepCruiseAndStopTimers(t, speed, input, targetCruiseSpeed);
            StepYawMetrics(t, speed, roverYaw, yawRate, input, targetCruiseSpeed);

            Vector3 camPos = _camera.transform.position;
            Vector3 camForward = _camera.transform.forward;
            float camYaw = _camera.transform.eulerAngles.y;

            Vector3 pivot = roverPos + Vector3.up * 1.1f;
            float actualDist = Vector3.Distance(camPos, pivot);
            float posLag = Mathf.Abs(actualDist - desiredCameraDistance);
            float yawLag = Mathf.Abs(Mathf.DeltaAngle(roverYaw, camYaw));

            _posLagSum += posLag;
            _maxPosLag = Mathf.Max(_maxPosLag, posLag);
            _yawLagSum += yawLag;
            _maxYawLag = Mathf.Max(_maxYawLag, yawLag);
            _lagSamples++;

            float currentJerk = StepCameraJerk(dt, camPos, camForward);

            float camClearance = _terrain != null
                ? camPos.y - _terrain.SampleHeight(camPos.x, camPos.z)
                : 10f;
            _minCameraClearance = Mathf.Min(_minCameraClearance, camClearance);
            if (camClearance < ClippingClearanceMeters)
            {
                _terrainClipFrames++;
            }

            if (Physics.CheckSphere(camPos, ClippingClearanceMeters, Layers.PropMask, QueryTriggerInteraction.Ignore))
            {
                _geometryClipFrames++;
            }

            if (_terrain != null)
            {
                float groundY = _terrain.SampleHeight(roverPos.x, roverPos.z);
                float penetration = Mathf.Max(0f, groundY - roverPos.y);
                _maxRoverGroundPenetration = Mathf.Max(_maxRoverGroundPenetration, penetration);
            }

            if (t >= _nextSampleTime)
            {
                _nextSampleTime = t + SampleIntervalSeconds;
                _samples.Add(new TimeSampleJson
                {
                    t = Round3(t),
                    speed = Round3(speed),
                    forwardSpeed = Round3(forwardSpeed),
                    throttleInput = Round3(input.y),
                    steerInput = Round3(input.x),
                    yawRate = Round2(yawRate),
                    grounded = _rover.IsGrounded,
                    cameraLagMeters = Round3(posLag),
                    cameraYawLagDeg = Round2(yawLag),
                    cameraJerk = Round2(currentJerk),
                    cameraClearance = Round3(camClearance),
                });
            }

            _prevRoverPos = roverPos;
            _prevRoverYaw = roverYaw;
            _prevCamPos = camPos;
            _prevCamForward = camForward;
        }

        public void CaptureFrame(string label)
        {
            float t = Time.time - _startTime;
            int index = _captures.Count;
            string safeLabel = SanitizeLabel(label);
            string fileName = string.Format(CultureInfo.InvariantCulture, "frame_{0:D3}_{1}.png", index, safeLabel);
            string fullPath = Path.Combine(_scenarioDir, fileName);

            Texture2D tex = FrameCapture.Render(_camera, _width, _height);
            try
            {
                if (_uiRedirected && _uiTarget != null)
                {
                    CompositeUiOnto(tex);
                }

                FrameCapture.WritePng(tex, fullPath);
                _thumbnails.Add(Downscale(tex, ThumbWidth, ThumbHeight));
            }
            finally
            {
                Object.Destroy(tex);
            }

            Vector3 camPos = _camera.transform.position;
            float clearance = _terrain != null
                ? camPos.y - _terrain.SampleHeight(camPos.x, camPos.z)
                : 10f;
            float roverYaw = _rover.Rotation.eulerAngles.y;
            float camYaw = _camera.transform.eulerAngles.y;
            float yawRate = Mathf.DeltaAngle(_prevRoverYaw, roverYaw) / Mathf.Max(Time.deltaTime, 1e-4f);

            _captures.Add(new CaptureEntryJson
            {
                index = index,
                file = fileName,
                label = label,
                time = Round3(t),
                speed = Round3(_rover.Speed),
                yawRate = Round2(yawRate),
                cameraClearance = Round3(clearance),
                cameraYawLagDeg = Round2(Mathf.Abs(Mathf.DeltaAngle(roverYaw, camYaw))),
            });

            _skipPerfFrames = 2;
            _lastWallTimestamp = Stopwatch.GetTimestamp();
        }

        public void SetMetric(string key, float value)
        {
            for (int i = 0; i < _customMetrics.Count; i++)
            {
                if (_customMetrics[i].key == key)
                {
                    _customMetrics[i] = new NamedMetricJson { key = key, value = Round3(value) };
                    return;
                }
            }

            _customMetrics.Add(new NamedMetricJson { key = key, value = Round3(value) });
        }

        public void SetNote(string key, string value)
        {
            for (int i = 0; i < _customNotes.Count; i++)
            {
                if (_customNotes[i].key == key)
                {
                    _customNotes[i] = new NamedNoteJson { key = key, value = value ?? string.Empty };
                    return;
                }
            }

            _customNotes.Add(new NamedNoteJson { key = key, value = value ?? string.Empty });
        }

        public void SaveReport()
        {
            WriteContactStrip();

            var report = new ScenarioReportJson
            {
                scenario = _scenario,
                durationSeconds = Round3(Time.time - _startTime),
                frameCount = _frameCount,
                timeToCruiseSeconds = Round3(_timeToCruise),
                timeToStopSeconds = Round3(_timeToStop),
                fps = BuildFpsSummary(),
                gcAllocationBytesPerFrame = BuildGcSummary(),
                yawRateVsInput = new YawSummaryJson
                {
                    peakYawRateLowSpeedDegPerSec = Round2(_peakYawRateLowSpeed),
                    peakYawRateHighSpeedDegPerSec = Round2(_peakYawRateHighSpeed),
                    turnRadiusHighSpeedMeters = _highSpeedTurnSamples > 0 && _highSpeedTurnYawRateSum > 1e-3f
                        ? Round2((_highSpeedTurnSpeedSum / _highSpeedTurnSamples)
                            / ((_highSpeedTurnYawRateSum / _highSpeedTurnSamples) * Mathf.Deg2Rad))
                        : 0f,
                    postReleaseYawOvershootDeg = Round2(_postReleaseYawOvershoot),
                },
                cameraLag = new CameraLagSummaryJson
                {
                    meanPositionLagMeters = _lagSamples > 0 ? Round3(_posLagSum / _lagSamples) : 0f,
                    maxPositionLagMeters = Round3(_maxPosLag),
                    meanYawLagDeg = _lagSamples > 0 ? Round2(_yawLagSum / _lagSamples) : 0f,
                    maxYawLagDeg = Round2(_maxYawLag),
                },
                cameraJerk = new CameraJerkSummaryJson
                {
                    meanJerkMetersPerSec3 = _jerkWindowCount > 0 ? Round2(_jerkSum / _jerkWindowCount) : 0f,
                    maxJerkMetersPerSec3 = Round2(_maxJerk),
                    maxTurnRateDegPerSec = Round2(_maxTurnRate),
                    maxAngularJerkDegPerSec2 = Round2(_maxAngularJerk),
                },
                clipping = new ClippingSummaryJson
                {
                    terrainClippingFrames = _terrainClipFrames,
                    geometryClippingFrames = _geometryClipFrames,
                    minCameraTerrainClearanceMeters = Round3(_minCameraClearance),
                    roverGroundPenetrationMaxMeters = Round3(_maxRoverGroundPenetration),
                },
                scenarioMetrics = _customMetrics.ToArray(),
                scenarioNotes = _customNotes.ToArray(),
                captures = _captures.ToArray(),
                speedOverTime = _samples.ToArray(),
            };

            string json = JsonUtility.ToJson(report, true);
            string metricsPath = Path.Combine(_scenarioDir, "metrics.json");
            File.WriteAllText(metricsPath, json);
            Debug.Log($"[feel-capture] wrote {_captures.Count} frames and {metricsPath}");
        }

        public void Dispose()
        {
            RestoreUiPanel();
            if (_gcRecorder.Valid)
            {
                _gcRecorder.Dispose();
            }
        }

        private void StepCruiseAndStopTimers(float t, float speed, Vector2 input, float targetCruiseSpeed)
        {
            if (_throttleStartTime < 0f && input.y > 0.5f && speed < 0.4f)
            {
                _throttleStartTime = t;
            }

            if (_throttleStartTime >= 0f && _timeToCruise < 0f && speed >= targetCruiseSpeed * 0.92f)
            {
                _timeToCruise = t - _throttleStartTime;
            }

            if (speed >= targetCruiseSpeed * 0.75f)
            {
                _wasAtCruise = true;
            }

            if (_wasAtCruise && _coastStartTime < 0f && Mathf.Abs(input.y) < 0.05f)
            {
                _coastStartTime = t;
            }

            if (_coastStartTime >= 0f && _timeToStop < 0f && speed <= StopSpeedThreshold)
            {
                _timeToStop = t - _coastStartTime;
            }
        }

        private void StepYawMetrics(
            float t,
            float speed,
            float roverYaw,
            float yawRate,
            Vector2 input,
            float targetCruiseSpeed)
        {
            float absYawRate = Mathf.Abs(yawRate);
            float absSteer = Mathf.Abs(input.x);
            if (absSteer > 0.4f)
            {
                if (speed < targetCruiseSpeed * 0.45f)
                {
                    _peakYawRateLowSpeed = Mathf.Max(_peakYawRateLowSpeed, absYawRate);
                }
                else if (speed >= targetCruiseSpeed * 0.7f)
                {
                    _peakYawRateHighSpeed = Mathf.Max(_peakYawRateHighSpeed, absYawRate);
                    _highSpeedTurnSpeedSum += speed;
                    _highSpeedTurnYawRateSum += absYawRate;
                    _highSpeedTurnSamples++;
                }

                _steerReleaseTime = -1f;
            }
            else if (absSteer < 0.05f && absYawRate > 8f && _steerReleaseTime < 0f)
            {
                _steerReleaseTime = t;
                _steerReleaseHeading = roverYaw;
            }
            else if (_steerReleaseTime >= 0f && t - _steerReleaseTime <= 1.2f)
            {
                float drift = Mathf.Abs(Mathf.DeltaAngle(_steerReleaseHeading, roverYaw));
                _postReleaseYawOvershoot = Mathf.Max(_postReleaseYawOvershoot, drift);
            }
        }

        private float StepCameraJerk(float dt, Vector3 camPos, Vector3 camForward)
        {
            _jerkWindowElapsed += dt;
            if (_jerkWindowElapsed < JerkWindowSeconds)
            {
                return 0f;
            }

            float w = _jerkWindowElapsed;
            Vector3 vel = (camPos - _prevWindowCamPos) / w;
            Vector3 acc = (vel - _prevWindowCamVel) / w;
            float jerk = (acc - _prevWindowCamAcc).magnitude / w;

            float turnRate = Vector3.Angle(_prevWindowCamForward, camForward) / w;
            float angularJerk = Mathf.Abs(turnRate - _prevWindowAngularSpeed) / w;

            if (_jerkWindowCount >= 2)
            {
                _jerkSum += jerk;
                _maxJerk = Mathf.Max(_maxJerk, jerk);
                _maxTurnRate = Mathf.Max(_maxTurnRate, turnRate);
                _maxAngularJerk = Mathf.Max(_maxAngularJerk, angularJerk);
            }

            _prevWindowCamPos = camPos;
            _prevWindowCamVel = vel;
            _prevWindowCamAcc = acc;
            _prevWindowCamForward = camForward;
            _prevWindowAngularSpeed = turnRate;
            _jerkWindowElapsed = 0f;
            _jerkWindowCount++;
            return jerk;
        }

        private FpsSummaryJson BuildFpsSummary()
        {
            if (_frameFps.Count == 0)
            {
                return new FpsSummaryJson { mean = 60f, min = 60f, p5 = 60f };
            }

            var copy = new List<float>(_frameFps);
            copy.Sort();
            float sum = 0f;
            for (int i = 0; i < copy.Count; i++)
            {
                sum += copy[i];
            }

            int p5Index = Mathf.Clamp(copy.Count / 20, 0, copy.Count - 1);
            return new FpsSummaryJson
            {
                mean = Round2(sum / copy.Count),
                min = Round2(copy[0]),
                p5 = Round2(copy[p5Index]),
            };
        }

        private GcSummaryJson BuildGcSummary()
        {
            if (_frameGcBytes.Count == 0)
            {
                return new GcSummaryJson();
            }

            long sum = 0L;
            long max = 0L;
            long min = long.MaxValue;
            int nonZero = 0;
            for (int i = 0; i < _frameGcBytes.Count; i++)
            {
                long b = _frameGcBytes[i];
                sum += b;
                if (b > max)
                {
                    max = b;
                }

                if (b < min)
                {
                    min = b;
                }

                if (b > 0L)
                {
                    nonZero++;
                }
            }

            return new GcSummaryJson
            {
                mean = Round2((float)sum / _frameGcBytes.Count),
                max = max,
                steadyStateMin = min == long.MaxValue ? 0L : min,
                nonZeroFrames = nonZero,
                sampledFrames = _frameGcBytes.Count,
            };
        }

        private void CompositeUiOnto(Texture2D world)
        {
            var ui = new Texture2D(_width, _height, TextureFormat.RGBA32, false, false);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = _uiTarget;
            try
            {
                ui.ReadPixels(new Rect(0, 0, _width, _height), 0, 0, false);
                ui.Apply(false);
                Color32[] bg = world.GetPixels32();
                Color32[] fg = ui.GetPixels32();
                int count = Mathf.Min(bg.Length, fg.Length);
                for (int i = 0; i < count; i++)
                {
                    Color32 over = fg[i];
                    if (over.a == 0)
                    {
                        continue;
                    }

                    Color32 under = bg[i];
                    int keep = 255 - over.a;
                    bg[i] = new Color32(
                        (byte)Mathf.Min(255, over.r + under.r * keep / 255),
                        (byte)Mathf.Min(255, over.g + under.g * keep / 255),
                        (byte)Mathf.Min(255, over.b + under.b * keep / 255),
                        255);
                }

                world.SetPixels32(bg);
                world.Apply(false);
            }
            finally
            {
                RenderTexture.active = previous;
                Object.Destroy(ui);
            }
        }

        private void WriteContactStrip()
        {
            if (_thumbnails.Count == 0)
            {
                return;
            }

            int count = _thumbnails.Count;
            int cols = Mathf.Min(StripColumns, count);
            int rows = (count + cols - 1) / cols;
            int sheetW = cols * ThumbWidth + (cols + 1) * StripPadding;
            int sheetH = rows * ThumbHeight + (rows + 1) * StripPadding;

            var sheet = new Texture2D(sheetW, sheetH, TextureFormat.RGBA32, false, false);
            try
            {
                var pixels = new Color32[sheetW * sheetH];
                var bg = new Color32(14, 16, 22, 255);
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = bg;
                }

                for (int idx = 0; idx < count; idx++)
                {
                    int col = idx % cols;
                    int row = idx / cols;
                    int flippedRow = rows - 1 - row;
                    int startX = StripPadding + col * (ThumbWidth + StripPadding);
                    int startY = StripPadding + flippedRow * (ThumbHeight + StripPadding);
                    Color32[] thumb = _thumbnails[idx];
                    for (int y = 0; y < ThumbHeight; y++)
                    {
                        int srcRow = y * ThumbWidth;
                        int dstRow = (startY + y) * sheetW + startX;
                        Array.Copy(thumb, srcRow, pixels, dstRow, ThumbWidth);
                    }
                }

                sheet.SetPixels32(pixels);
                sheet.Apply(false);
                FrameCapture.WritePng(sheet, Path.Combine(_scenarioDir, "strip.png"));
            }
            finally
            {
                Object.Destroy(sheet);
            }
        }

        private static Color32[] Downscale(Texture2D source, int targetW, int targetH)
        {
            Color32[] src = source.GetPixels32();
            int srcW = source.width;
            int srcH = source.height;
            var dst = new Color32[targetW * targetH];
            for (int y = 0; y < targetH; y++)
            {
                int sy = Mathf.Clamp(y * srcH / targetH, 0, srcH - 1);
                int srcRowOffset = sy * srcW;
                int dstRowOffset = y * targetW;
                for (int x = 0; x < targetW; x++)
                {
                    int sx = Mathf.Clamp(x * srcW / targetW, 0, srcW - 1);
                    dst[dstRowOffset + x] = src[srcRowOffset + sx];
                }
            }

            return dst;
        }

        private void RestoreUiPanel()
        {
            if (_uiDocument != null && _originalPanelSettings != null)
            {
                _uiDocument.panelSettings = _originalPanelSettings;
                _originalPanelSettings = null;
            }

            if (_capturePanelSettings != null)
            {
                Object.Destroy(_capturePanelSettings);
                _capturePanelSettings = null;
            }

            if (_uiTarget != null)
            {
                _uiTarget.Release();
                Object.Destroy(_uiTarget);
                _uiTarget = null;
            }

            _uiRedirected = false;
        }

        private static void PrepareDirectory(string dir)
        {
            Directory.CreateDirectory(dir);
            foreach (string existing in Directory.GetFiles(dir, "*.png"))
            {
                File.Delete(existing);
            }

            string metrics = Path.Combine(dir, "metrics.json");
            if (File.Exists(metrics))
            {
                File.Delete(metrics);
            }
        }

        private static string SanitizeLabel(string label)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                return "shot";
            }

            char[] chars = label.Trim().ToLowerInvariant().ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                bool ok = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '-' || c == '_';
                if (!ok)
                {
                    chars[i] = '_';
                }
            }

            return new string(chars);
        }

        private static float Round2(float value)
        {
            return Mathf.Round(value * 100f) / 100f;
        }

        private static float Round3(float value)
        {
            return Mathf.Round(value * 1000f) / 1000f;
        }

        [Serializable]
        private struct ScenarioReportJson
        {
            public string scenario;
            public float durationSeconds;
            public int frameCount;
            public float timeToCruiseSeconds;
            public float timeToStopSeconds;
            public FpsSummaryJson fps;
            public GcSummaryJson gcAllocationBytesPerFrame;
            public YawSummaryJson yawRateVsInput;
            public CameraLagSummaryJson cameraLag;
            public CameraJerkSummaryJson cameraJerk;
            public ClippingSummaryJson clipping;
            public NamedMetricJson[] scenarioMetrics;
            public NamedNoteJson[] scenarioNotes;
            public CaptureEntryJson[] captures;
            public TimeSampleJson[] speedOverTime;
        }

        [Serializable]
        private struct FpsSummaryJson
        {
            public float mean;
            public float min;
            public float p5;
        }

        [Serializable]
        private struct GcSummaryJson
        {
            public float mean;
            public long max;
            public long steadyStateMin;
            public int nonZeroFrames;
            public int sampledFrames;
        }

        [Serializable]
        private struct YawSummaryJson
        {
            public float peakYawRateLowSpeedDegPerSec;
            public float peakYawRateHighSpeedDegPerSec;
            public float turnRadiusHighSpeedMeters;
            public float postReleaseYawOvershootDeg;
        }

        [Serializable]
        private struct CameraLagSummaryJson
        {
            public float meanPositionLagMeters;
            public float maxPositionLagMeters;
            public float meanYawLagDeg;
            public float maxYawLagDeg;
        }

        [Serializable]
        private struct CameraJerkSummaryJson
        {
            public float meanJerkMetersPerSec3;
            public float maxJerkMetersPerSec3;
            public float maxTurnRateDegPerSec;
            public float maxAngularJerkDegPerSec2;
        }

        [Serializable]
        private struct ClippingSummaryJson
        {
            public int terrainClippingFrames;
            public int geometryClippingFrames;
            public float minCameraTerrainClearanceMeters;
            public float roverGroundPenetrationMaxMeters;
        }

        [Serializable]
        private struct NamedMetricJson
        {
            public string key;
            public float value;
        }

        [Serializable]
        private struct NamedNoteJson
        {
            public string key;
            public string value;
        }

        [Serializable]
        private struct CaptureEntryJson
        {
            public int index;
            public string file;
            public string label;
            public float time;
            public float speed;
            public float yawRate;
            public float cameraClearance;
            public float cameraYawLagDeg;
        }

        [Serializable]
        private struct TimeSampleJson
        {
            public float t;
            public float speed;
            public float forwardSpeed;
            public float throttleInput;
            public float steerInput;
            public float yawRate;
            public bool grounded;
            public float cameraLagMeters;
            public float cameraYawLagDeg;
            public float cameraJerk;
            public float cameraClearance;
        }
    }
}
