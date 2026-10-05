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
            LogAssert.ignoreFailingMessages = true;
            var capture = new LogCapture();
            Debug.Log("info is ignored");
            Debug.LogWarning("warnings are ignored");
            Debug.LogError("first error");
            Debug.LogException(new System.InvalidOperationException("boom"));
            capture.Dispose();
            Debug.LogError("after dispose");
            LogAssert.ignoreFailingMessages = false;

            Assert.AreEqual(2, capture.ErrorCount);
            Assert.AreEqual("first error", capture.FirstMessage);
        }
    }
}
