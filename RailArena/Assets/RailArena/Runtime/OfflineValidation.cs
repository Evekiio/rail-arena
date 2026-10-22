#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RailArena
{
    /// <summary>Opt-in standalone smoke/soak runner. Uses the normal authority and actor movement.</summary>
    public sealed class OfflineValidation : MonoBehaviour
    {
        [Serializable]
        private sealed class Report
        {
            public string editorVersion;
            public string platform;
            public string processor;
            public string gpu;
            public int memoryMB;
            public float elapsedSeconds;
            public float meanFrameMs;
            public float p95FrameMs;
            public int exceptions;
            public int voidDeaths;
            public string telemetry;
            public string[] contenders;
            public string[] voidTraces;
            public bool passed;
        }

        private int errors;
        private readonly List<float> frames = new();
        private bool measuring;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--validate-offline") >= 0)
                new GameObject("Standalone offline validation").AddComponent<OfflineValidation>();
        }

        private void OnEnable() => Application.logMessageReceived += Log;
        private void OnDisable() => Application.logMessageReceived -= Log;
        private void Log(string message, string stack, LogType type)
        { if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) errors++; }
        private void Update() { if (measuring) frames.Add(Time.unscaledDeltaTime * 1000); }

        private IEnumerator Start()
        {
            yield return null;
            string[] args = Environment.GetCommandLineArgs();
            int outputIndex = Array.IndexOf(args, "--validation-output");
            string output = outputIndex >= 0 && outputIndex + 1 < args.Length ? Path.GetFullPath(args[outputIndex + 1]) :
                Path.Combine(Application.persistentDataPath, "offline-validation.json");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var session = FindFirstObjectByType<ArenaSession>();
            session.AutomatedHuman = true;
            session.StartSession(true, 7, 1, TargetScenario.Combat);
            float start = Time.realtimeSinceStartup;
            yield return new WaitForSecondsRealtime(5);
            measuring = true;
            yield return new WaitForSecondsRealtime(40);
            measuring = false;
            var names = new List<string>();
            foreach (var actor in session.Actors)
                names.Add($"{actor.DisplayName}: health={actor.Health}, position={actor.transform.position}, routeNodesThisLife={actor.Brain.NodesReached}");
            frames.Sort();
            float total = 0;
            foreach (float frame in frames) total += frame;
            var report = new Report
            {
                editorVersion = Application.unityVersion, platform = Application.platform.ToString(),
                processor = SystemInfo.processorType, gpu = SystemInfo.graphicsDeviceName,
                memoryMB = SystemInfo.systemMemorySize, elapsedSeconds = Time.realtimeSinceStartup - start,
                meanFrameMs = frames.Count == 0 ? 0 : total / frames.Count,
                p95FrameMs = frames.Count == 0 ? 0 : frames[Mathf.Min(frames.Count - 1, (int)(frames.Count * .95f))],
                exceptions = errors, voidDeaths = session.VoidDeaths,
                telemetry = session.Telemetry(), contenders = names.ToArray(),
                voidTraces = session.VoidTraces.ToArray(),
                passed = errors == 0 && session.Match.Tick >= 2400 && session.VoidDeaths <= 2 &&
                    session.ShotsFired > 30 && session.Deaths > 5 && session.PadLaunches > 8
            };
            File.WriteAllText(output, JsonUtility.ToJson(report, true));
            ScreenCapture.CaptureScreenshot(output + ".png");
            yield return new WaitForSecondsRealtime(1);
            Application.Quit(report.passed ? 0 : 1);
        }
    }
}
#endif
