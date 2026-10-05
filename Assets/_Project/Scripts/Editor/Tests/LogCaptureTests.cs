using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Editor.Automation;

namespace MoonProject.Editor.Tests
{
    public sealed class LogCaptureTests
    {
        [Test]
        public void CountsErrorsOnlyWhileAlive()
        {
            var capture = new LogCapture();
            LogAssert.Expect(LogType.Error, "LogCaptureTests: expected error");
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException: LogCaptureTests: expected exception"));
            Debug.LogWarning("LogCaptureTests: warnings are not counted");
            Debug.LogError("LogCaptureTests: expected error");
            Debug.LogException(new InvalidOperationException("LogCaptureTests: expected exception"));
            capture.Dispose();
            LogAssert.Expect(LogType.Error, "LogCaptureTests: error after dispose");
            Debug.LogError("LogCaptureTests: error after dispose");

            Assert.AreEqual(2, capture.ErrorCount);
            Assert.AreEqual("LogCaptureTests: expected error", capture.FirstMessage);
            Assert.AreEqual(2, capture.Messages.Count);
        }
    }
}
