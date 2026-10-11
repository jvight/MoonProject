using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Runs Blender headless on one of the bpy scripts under <c>tools/blender/</c> (the scripts are the source of
    /// truth for Blender-built models; no .blend file is versioned). The executable is the owner-approved portable
    /// Blender, overridable with the <see cref="ExecutableVariable"/> environment variable on another machine.
    /// </summary>
    public static class BlenderProcess
    {
        public const string ExecutableVariable = "MOON_BLENDER";
        public const string DefaultExecutable = "D:/Tools/Blender/blender-4.5.9-windows-x64/blender.exe";

        private const int TimeoutMilliseconds = 180000;
        private const int ReportedOutputCharacters = 4000;

        /// <summary>The Blender executable this machine uses.</summary>
        public static string Executable
        {
            get
            {
                string configured = Environment.GetEnvironmentVariable(ExecutableVariable);
                return string.IsNullOrEmpty(configured) ? DefaultExecutable : configured;
            }
        }

        /// <summary>
        /// Runs <c>blender -b --factory-startup --python script -- arguments</c> and waits for it. Throws with
        /// Blender's output when the executable is missing, the script raises (exit code 1) or the run times out.
        /// </summary>
        public static void RunScript(string scriptPath, string arguments)
        {
            string executable = Executable;
            if (!File.Exists(executable))
            {
                throw new InvalidOperationException(
                    $"Blender not found at {executable}; install the approved build or set {ExecutableVariable}.");
            }

            if (!File.Exists(scriptPath))
            {
                throw new FileNotFoundException("Blender script missing.", scriptPath);
            }

            var start = new ProcessStartInfo(executable,
                $"-b --factory-startup --python-exit-code 1 --python \"{scriptPath}\" -- {arguments}")
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            var output = new StringBuilder();
            using (var process = new Process { StartInfo = start })
            {
                process.OutputDataReceived += (_, line) => Append(output, line.Data);
                process.ErrorDataReceived += (_, line) => Append(output, line.Data);
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                if (!process.WaitForExit(TimeoutMilliseconds))
                {
                    process.Kill();
                    throw new TimeoutException($"Blender ran over {TimeoutMilliseconds / 1000} s on {scriptPath}.");
                }

                process.WaitForExit();
                string text = Tail(output);
                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        $"Blender failed on {scriptPath} (exit {process.ExitCode}):\n{text}");
                }
            }
        }

        private static void Append(StringBuilder output, string line)
        {
            if (line == null)
            {
                return;
            }

            lock (output)
            {
                output.AppendLine(line);
            }
        }

        private static string Tail(StringBuilder output)
        {
            lock (output)
            {
                string text = output.ToString();
                return text.Length <= ReportedOutputCharacters
                    ? text
                    : text.Substring(text.Length - ReportedOutputCharacters);
            }
        }
    }
}
