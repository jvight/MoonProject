using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace MoonProject.Autoplay
{
    /// <summary>
    /// Plays the scenario inside the running exe and writes what it saw to the output directory: a screenshot at
    /// each beat and every few seconds, the frame-time log, the Unity log, and <c>report.md</c>. The process exits
    /// with 0 when every beat passed and 1 otherwise.
    /// </summary>
    public sealed class AutoplayRunner : MonoBehaviour
    {
        private const string ReportFile = "report.md";
        private const string LogFile = "autoplay.log";
        private const string ScreenshotFolder = "screenshots";
        private const int SettleFrames = 3;
        private const int KeptErrors = 20;

        /// <summary>Seconds two guidance elements may show together while one fades over the other (a hand-over).</summary>
        private const float GuidanceGraceSeconds = 1f;

        private readonly BeatLog _beats = new BeatLog();
        private readonly GuidanceCounter _guidance = new GuidanceCounter(GuidanceGraceSeconds);
        private readonly List<string> _guidanceShown = new List<string>();
        private bool _guidanceReported;
        private readonly FrameTimeLog _frames = new FrameTimeLog();
        private readonly List<string> _screenshots = new List<string>();
        private readonly List<string> _errors = new List<string>();
        private readonly Queue<string> _shotQueue = new Queue<string>();
        private AutoplayOptions _options;
        private AutoplayWorld _world;
        private VirtualInput _input;
        private AutoplayBot _bot;
        private StreamWriter _log;
        private float _startedAt;
        private float _nextShotAt;
        private int _shotNumber;
        private bool _finished;
        private int _exceptions;

        public void Configure(AutoplayOptions options)
        {
            _options = options;
        }

        private void Start()
        {
            if (_options == null)
            {
                Debug.LogError("AutoplayRunner: Configure was not called before Start.", this);
                enabled = false;
                return;
            }

            Directory.CreateDirectory(Path.Combine(_options.OutDirectory, ScreenshotFolder));
            _log = new StreamWriter(Path.Combine(_options.OutDirectory, LogFile), false, new UTF8Encoding(false))
            {
                AutoFlush = true,
            };
            Application.logMessageReceived += OnLog;
            _startedAt = Time.unscaledTime;
            _nextShotAt = _startedAt + _options.ShotSeconds;
            _world = new AutoplayWorld();
            _input = new VirtualInput();
            _bot = new AutoplayBot(_world, _input, _beats, Shot);
            StartCoroutine(Play());
        }

        private void Update()
        {
            if (_bot == null || _finished)
            {
                return;
            }

            TakeQueuedShot();
            float milliseconds = Time.unscaledDeltaTime * 1000f;
            _frames.Record(Time.unscaledTime - _startedAt, milliseconds,
                milliseconds > FrameTimeLog.ThresholdMilliseconds ? _bot.Where() : string.Empty);
            if (Time.unscaledTime >= _nextShotAt)
            {
                _nextShotAt = Time.unscaledTime + _options.ShotSeconds;
                Shot("t" + ((int)(Time.unscaledTime - _startedAt)).ToString("0000", CultureInfo.InvariantCulture));
            }

            WatchGuidance();
            if (Time.unscaledTime - _startedAt > _options.TimeoutSeconds)
            {
                _bot.Fail("the run exceeded its " + _options.TimeoutSeconds.ToString("0", CultureInfo.InvariantCulture) +
                          " s timeout. " + _bot.Where());
            }
        }

        /// <summary>
        /// The one-element rule, every frame: two guidance elements showing together fail the run, and so does an
        /// objective line while the lines are not expected (a stall reminder fired where the world should have led).
        /// </summary>
        private void WatchGuidance()
        {
            if (_bot.Failed || !_world.Refresh() || _world.Ui.TitleShown())
            {
                return;
            }

            _world.Ui.GuidanceShown(_guidanceShown);
            _guidance.Step(Time.unscaledDeltaTime, _guidanceShown);
            if (_guidance.Failed && !_guidanceReported)
            {
                _guidanceReported = true;
                if (_options.StrictGuidance)
                {
                    _bot.Fail(_guidance.Message + " " + _bot.Where());
                }
                else
                {
                    _bot.Warn("guidance: " + _guidance.Message + " " + _bot.Where());
                }
            }
            else if (!_options.LinesExpected && _world.Ui.ObjectiveLine().Length > 0)
            {
                _bot.Fail("a fallback reminder fired: the objective line '" + _world.Ui.ObjectiveLine() +
                          "' showed where the world cues should have led. " + _bot.Where());
            }
        }

        private IEnumerator Play()
        {
            var scenario = new OpeningScenario(_bot, _options.UntilBeat, _options.LinesExpected);
            IEnumerator run = scenario.Run();
            while (!_bot.Failed && run.MoveNext())
            {
                yield return run.Current;
            }

            yield return Finish();
        }

        private IEnumerator Finish()
        {
            _finished = true;
            _bot.Shutdown();
            Shot("end");
            while (_shotQueue.Count > 0)
            {
                TakeQueuedShot();
                yield return null;
            }

            for (int i = 0; i < SettleFrames; i++)
            {
                yield return null;
            }

            string stop = _bot.Failed && _beats.Passed ? _bot.Failure : string.Empty;
            if (_exceptions > 0 && string.IsNullOrEmpty(stop))
            {
                stop = _exceptions.ToString(CultureInfo.InvariantCulture) + " exception(s) in the log.";
            }

            string report = _beats.Render(_options.Scenario, BuildInfo(), Time.unscaledTime - _startedAt, _frames,
                _screenshots, stop);
            report += ErrorSection();
            File.WriteAllText(Path.Combine(_options.OutDirectory, ReportFile), report, new UTF8Encoding(false));
            bool passed = _beats.Passed && string.IsNullOrEmpty(stop);
            Debug.Log("Autoplay: " + (passed ? "PASS" : "FAIL") + ", report at " +
                      Path.Combine(_options.OutDirectory, ReportFile));
            Application.logMessageReceived -= OnLog;
            _log.Dispose();
            _input.Dispose();
            Application.Quit(passed ? 0 : 1);
        }

        /// <summary>Asks for a screenshot; the engine writes one per frame, so they are taken in turn.</summary>
        private void Shot(string label)
        {
            _shotQueue.Enqueue(label);
        }

        private void TakeQueuedShot()
        {
            if (_shotQueue.Count == 0)
            {
                return;
            }

            string name = _shotNumber.ToString("00", CultureInfo.InvariantCulture) + "-" + _shotQueue.Dequeue() + ".png";
            _shotNumber++;
            ScreenCapture.CaptureScreenshot(Path.Combine(_options.OutDirectory, ScreenshotFolder, name));
            _screenshots.Add(ScreenshotFolder + "/" + name);
        }

        private void OnLog(string message, string stackTrace, LogType type)
        {
            _log.WriteLine("[" + (Time.unscaledTime - _startedAt).ToString("0.00", CultureInfo.InvariantCulture) + "] " +
                           type + ": " + message);
            if (type == LogType.Exception)
            {
                _exceptions++;
            }

            if ((type == LogType.Error || type == LogType.Exception) && _errors.Count < KeptErrors)
            {
                _errors.Add(message);
            }
        }

        private string ErrorSection()
        {
            var text = new StringBuilder("\n## Errors logged\n\n");
            if (_errors.Count == 0)
            {
                return text.Append("None.\n").ToString();
            }

            foreach (string error in _errors)
            {
                text.Append("- ").Append(error.Replace("\n", " ")).Append('\n');
            }

            return text.ToString();
        }

        private static string BuildInfo()
        {
            string path = Path.Combine(Application.dataPath, "..", "build_info.txt");
            return File.Exists(path) ? File.ReadAllText(path).Trim().Replace("\r", string.Empty).Replace("\n", "; ")
                : "no build_info.txt next to the exe";
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLog;
        }
    }
}
