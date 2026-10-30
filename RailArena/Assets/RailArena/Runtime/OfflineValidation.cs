#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using UnityEngine;

namespace RailArena
{
    /// <summary>Opt-in standalone smoke/soak runner. Uses the normal authority and actor movement.</summary>
    public sealed class OfflineValidation : MonoBehaviour
    {
        [Serializable]
        private sealed class ActorProgress
        {
            public string name;
            public int lives;
            public int routeNodes;
            public float sampledDistanceMetres;
            public float longestStationarySeconds;
            public float longestWithoutRouteSeconds;
            [NonSerialized] public int life, previousNodes;
            [NonSerialized] public Vector3 previousPosition, progressAnchor;
            [NonSerialized] public float stationary, withoutRoute;
        }

        [Serializable]
        private sealed class Report
        {
            public string editorVersion;
            public string platform;
            public string processor;
            public string gpu;
            public int memoryMB;
            public string fingerprint;
            public int requestedSeconds;
            public int difficulty;
            public int bots;
            public int minimumShots;
            public int minimumDeaths;
            public int minimumPadLaunches;
            public float elapsedSeconds;
            public float meanFrameMs;
            public float p95FrameMs;
            public int exceptions;
            public int voidDeaths;
            public string telemetry;
            public string[] contenders;
            public string[] voidTraces;
            public ActorProgress[] actorProgress;
            public int[] statisticsShots, statisticsHits, statisticsHeadshots, statisticsKills, statisticsDeaths, statisticsVoids;
            public bool statisticsPassed;
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
            int duration = ReadOption(args, "--validation-seconds", 45, 45, 1800);
            int difficulty = ReadOption(args, "--validation-difficulty", 1, 0, 2);
            int bots = ReadOption(args, "--validation-bots", 3, 1, Simulation.CombatRules.MaximumBots);
            // Require activity appropriate to the approved population.
            int minimumShots = (bots + 1) * 4 - 1;
            int minimumDeaths = Math.Max(1, bots - 1);
            int minimumPadLaunches = bots + 2;
            var session = FindFirstObjectByType<ArenaSession>();
            session.AutomatedHuman = true;
            session.StartSession(true, bots, difficulty, TargetScenario.Combat);
            var progress = new ActorProgress[session.Actors.Count];
            for (int i = 0; i < progress.Length; i++)
            {
                var actor = session.Actors[i];
                progress[i] = new ActorProgress { name = actor.DisplayName, life = actor.Life, lives = 1,
                    previousPosition = actor.transform.position, progressAnchor = actor.transform.position };
            }
            float start = Time.realtimeSinceStartup;
            yield return new WaitForSecondsRealtime(5);
            measuring = true;
            float lastSample = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - start < duration)
            {
                yield return new WaitForSecondsRealtime(1);
                float now = Time.realtimeSinceStartup;
                Sample(session, progress, now - lastSample);
                lastSample = now;
                File.WriteAllText(output + ".progress.tmp", JsonUtility.ToJson(new Report
                {
                    elapsedSeconds = now - start, difficulty = difficulty, bots = bots,
                    requestedSeconds = duration, actorProgress = progress,
                    telemetry = session.Telemetry(), exceptions = errors, voidDeaths = session.VoidDeaths
                }, true));
            }
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
                fingerprint = Networking.BuildIdentity.Fingerprint, requestedSeconds = duration, difficulty = difficulty, bots = bots,
                minimumShots = minimumShots, minimumDeaths = minimumDeaths, minimumPadLaunches = minimumPadLaunches,
                meanFrameMs = frames.Count == 0 ? 0 : total / frames.Count,
                p95FrameMs = frames.Count == 0 ? 0 : frames[Mathf.Min(frames.Count - 1, (int)(frames.Count * .95f))],
                exceptions = errors, voidDeaths = session.VoidDeaths,
                telemetry = session.Telemetry(), contenders = names.ToArray(),
                voidTraces = session.VoidTraces.ToArray(),
                actorProgress = progress,
                passed = errors == 0 && session.Match.Tick >= (duration - 5) * 60 && session.VoidDeaths <= 2 &&
                    session.ShotsFired >= minimumShots && session.Deaths >= minimumDeaths &&
                    session.PadLaunches >= minimumPadLaunches && ProgressPassed(progress)
            };
            var participants=session.Actors.ToArray();
            report.statisticsShots=Array.ConvertAll(participants,actor=>session.Match.Statistics[actor.Id].Shots);
            report.statisticsHits=Array.ConvertAll(participants,actor=>session.Match.Statistics[actor.Id].Hits);
            report.statisticsHeadshots=Array.ConvertAll(participants,actor=>session.Match.Statistics[actor.Id].Headshots);
            report.statisticsKills=Array.ConvertAll(participants,actor=>session.Match.Statistics[actor.Id].Kills);
            report.statisticsDeaths=Array.ConvertAll(participants,actor=>session.Match.Statistics[actor.Id].Deaths);
            report.statisticsVoids=Array.ConvertAll(participants,actor=>session.Match.Statistics[actor.Id].VoidDeaths);
            int shots=0,hits=0,hitEvents=0,deaths=0,kills=0,voids=0;
            report.statisticsPassed=true;
            foreach(var actor in participants)
            {
                var stats=session.Match.Statistics[actor.Id];
                shots+=stats.Shots; hits+=stats.Hits; hitEvents+=actor.HitSerial;
                deaths+=stats.Deaths; kills+=stats.Kills; voids+=stats.VoidDeaths;
                report.statisticsPassed &= stats.Shots==actor.ShotSerial && session.Match.Scores[actor.Id]==stats.Kills-stats.VoidDeaths;
            }
            report.statisticsPassed &= shots==session.ShotsFired && hits==hitEvents && deaths==session.Deaths && kills+voids==deaths;
            report.passed &= report.statisticsPassed;
            File.WriteAllText(output, JsonUtility.ToJson(report, true));
            ScreenCapture.CaptureScreenshot(output + ".png");
            yield return new WaitForSecondsRealtime(1);
            Application.Quit(report.passed ? 0 : 1);
        }

        private static int ReadOption(string[] args, string option, int fallback, int minimum, int maximum)
        {
            int index = Array.IndexOf(args, option);
            if (index < 0) return fallback;
            if (index + 1 >= args.Length || !int.TryParse(args[index + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ||
                value < minimum || value > maximum) throw new ArgumentException($"{option} requires {minimum} through {maximum}.");
            return value;
        }

        private static void Sample(ArenaSession session, ActorProgress[] progress, float seconds)
        {
            for (int i = 0; i < progress.Length; i++)
            {
                var actor = session.Actors[i];
                var sample = progress[i];
                Vector3 position = actor.transform.position;
                int nodes = actor.Brain.NodesReached;
                if (sample.life != actor.Life)
                {
                    sample.life = actor.Life; sample.lives++;
                    sample.previousPosition = sample.progressAnchor = position;
                    sample.previousNodes = 0;
                    sample.stationary = sample.withoutRoute = 0;
                }
                int delta = Mathf.Max(0, nodes - sample.previousNodes);
                sample.routeNodes += delta;
                sample.previousNodes = nodes;
                if (actor.Alive)
                {
                    sample.sampledDistanceMetres += Vector3.Distance(position, sample.previousPosition);
                    if (Vector3.Distance(position, sample.progressAnchor) >= .5f)
                    { sample.progressAnchor = position; sample.stationary = 0; }
                    else sample.stationary += seconds;
                    sample.withoutRoute = delta > 0 ? 0 : sample.withoutRoute + seconds;
                    sample.longestStationarySeconds = Mathf.Max(sample.longestStationarySeconds, sample.stationary);
                    sample.longestWithoutRouteSeconds = Mathf.Max(sample.longestWithoutRouteSeconds, sample.withoutRoute);
                }
                else sample.stationary = sample.withoutRoute = 0;
                sample.previousPosition = position;
            }
        }

        private static bool ProgressPassed(ActorProgress[] progress)
        {
            foreach (var actor in progress)
                if (actor.routeNodes < 3 || actor.longestStationarySeconds >= 10 || actor.longestWithoutRouteSeconds >= 20) return false;
            return true;
        }
    }
}
#endif
