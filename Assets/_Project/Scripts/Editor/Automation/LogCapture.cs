using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Editor.Automation
{
    /// <summary>
    /// Counts errors, exceptions and failed asserts logged on the main thread while it is alive. Automation uses it to
    /// fail a step that "succeeded" but logged an error (a builder that caught and logged its own exception, a missing
    /// reference reported by OnValidate, ...).
    /// </summary>
    public sealed class LogCapture : IDisposable
    {
        private const int MaxKeptMessages = 10;

        private readonly List<string> _messages = new List<string>();
        private bool _disposed;

        public LogCapture()
        {
            Application.logMessageReceived += OnLog;
        }

        public int ErrorCount { get; private set; }

        /// <summary>The first few error messages, in order.</summary>
        public IReadOnlyList<string> Messages => _messages;

        public string FirstMessage => _messages.Count > 0 ? _messages[0] : string.Empty;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            Application.logMessageReceived -= OnLog;
            _disposed = true;
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
            {
                return;
            }

            ErrorCount++;
            if (_messages.Count < MaxKeptMessages)
            {
                _messages.Add(condition);
            }
        }
    }
}
