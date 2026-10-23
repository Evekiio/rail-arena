#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace RailArena
{
    /// <summary>Opt-in two-process test driver. Each process supplies input for its own human slot only.</summary>
    public sealed class DuelValidation : MonoBehaviour
    {
        [Serializable]
        private sealed class Report
        {
            public string role;
            public string fingerprint;
            public int round;
            public int tick;
            public int errors;
            public int shots;
            public int observedShots;
            public int deaths;
            public int pads;
            public int snapshots;
            public int acknowledged;
            public int pending;
            public float correction;
            public float maximumActiveCorrection, maximumPhaseChangeCorrection;
            public ArenaSession.PredictionCorrection largestActiveCorrection, largestPhaseChangeCorrection;
            public ArenaSession.PredictionCorrection[] correctionSamples;
            public int outboundClamps;
            public int rewoundShots;
            public int rejectedTimes;
            public int[] actorShots;
            public int[] actorLives;
            public int[] statisticsShots, hits, headshots, kills, statisticsDeaths, voidDeaths;
            public bool statisticsPassed;
            public bool fullMatch;
            public bool resultsSeen;
            public bool rematchStarted;
            public int completedTick;
            public int completedWinner = -1;
            public int[] completedScores;
            public int[] completedShots;
            public int[] completedLives;
            public float elapsedSeconds;
            public string connection;
            public bool passed;
        }
        private int errors;
        private void OnEnable() => Application.logMessageReceived += Log;
        private void OnDisable() => Application.logMessageReceived -= Log;
        private void Log(string message, string stack, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors++; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--validate-duel-role") >= 0)
                new GameObject("Two-process duel validation").AddComponent<DuelValidation>();
        }

        private IEnumerator Start()
        {
            yield return null;
            string[] args = Environment.GetCommandLineArgs();
            string Value(string name, string fallback)
            { int index = Array.IndexOf(args, name); return index >= 0 && index + 1 < args.Length ? args[index + 1] : fallback; }
            string role = Value("--validate-duel-role", "client");
            string output = Path.GetFullPath(Value("--validation-output", "duel-" + role + ".json"));
            int port = int.Parse(Value("--validation-port", "7777"));
            float duration = Mathf.Clamp(float.Parse(Value("--validation-duration", "45"), System.Globalization.CultureInfo.InvariantCulture), 20, 900);
            bool fullMatch = Array.IndexOf(args, "--validation-full-match") >= 0;
            var session = FindFirstObjectByType<ArenaSession>();
            session.AutomatedHuman = true;
            if (role == "host") session.HostDuel(port); else session.JoinDuel("127.0.0.1", port);
            float started = Time.realtimeSinceStartup;
            bool readied = false;
            bool rematchReady = false;
            int completedTick = 0;
            int completedWinner = -1;
            int[] completedScores = null;
            int[] completedShots = null;
            int[] completedLives = null;
            float nextProgress = started + 30;
            while (Time.realtimeSinceStartup - started < duration)
            {
                if (Time.realtimeSinceStartup >= nextProgress)
                {
                    nextProgress += 30;
                    Debug.Log($"DUEL_PROGRESS role={role} round={session.NetworkRound} tick={session.Match?.Tick} shots={session.ShotsFired} lives={string.Join(",", Array.ConvertAll(session.Actors.ToArray(), actor => actor.Life))}");
                }
                if (!readied && session.IsOnline && session.ConnectionStatus.StartsWith("CONNECTED"))
                { session.ToggleReady(); readied = true; }
                if (fullMatch && session.NetworkRound == 1 && session.Match?.Phase == Simulation.MatchPhase.Results)
                {
                    if (!rematchReady)
                    {
                        completedTick = session.Match.Tick;
                        completedWinner = session.Match.Winner;
                        completedScores = new[] { session.Match.Scores[0], session.Match.Scores[1] };
                        completedShots = Array.ConvertAll(session.Actors.ToArray(), actor => actor.ShotSerial);
                        completedLives = Array.ConvertAll(session.Actors.ToArray(), actor => actor.Life);
                        session.ToggleReady();
                        rematchReady = true;
                        Debug.Log($"DUEL_RESULTS tick={completedTick} winner={completedWinner} scores={completedScores[0]},{completedScores[1]}");
                    }
                }
                if (fullMatch && rematchReady && session.NetworkRound == 2 && session.Match.Tick >= 900) break;
                yield return null;
            }
            int observed = 0;
            foreach (var actor in session.Actors) observed += actor.ShotSerial;
            var report = new Report { role = role, fingerprint = Networking.BuildIdentity.Fingerprint,
                round = session.NetworkRound, tick = session.Match?.Tick ?? 0, errors = errors,
                shots = session.ShotsFired, observedShots = observed, deaths = session.Deaths, pads = session.PadLaunches,
                snapshots = session.SnapshotsReceived, acknowledged = session.LastAcknowledgedInput,
                pending = session.PendingInputs, correction = session.MaximumCorrection, outboundClamps = session.OutboundClamps, connection = session.ConnectionStatus };
            report.rewoundShots = session.RewoundShots;
            report.rejectedTimes = session.RejectedShotTimes;
            report.correctionSamples = session.CorrectionSamples.ToArray();
            report.maximumActiveCorrection = session.MaximumActiveCorrection;
            report.maximumPhaseChangeCorrection = session.MaximumPhaseChangeCorrection;
            report.largestActiveCorrection = session.LargestActiveCorrection;
            report.largestPhaseChangeCorrection = session.LargestPhaseChangeCorrection;
            report.actorShots = Array.ConvertAll(session.Actors.ToArray(), actor => actor.ShotSerial);
            report.actorLives = Array.ConvertAll(session.Actors.ToArray(), actor => actor.Life);
            report.statisticsShots=Array.ConvertAll(session.Actors.ToArray(),actor=>session.Match.Statistics[actor.Id].Shots);
            report.hits=Array.ConvertAll(session.Actors.ToArray(),actor=>session.Match.Statistics[actor.Id].Hits);
            report.headshots=Array.ConvertAll(session.Actors.ToArray(),actor=>session.Match.Statistics[actor.Id].Headshots);
            report.kills=Array.ConvertAll(session.Actors.ToArray(),actor=>session.Match.Statistics[actor.Id].Kills);
            report.statisticsDeaths=Array.ConvertAll(session.Actors.ToArray(),actor=>session.Match.Statistics[actor.Id].Deaths);
            report.voidDeaths=Array.ConvertAll(session.Actors.ToArray(),actor=>session.Match.Statistics[actor.Id].VoidDeaths);
            report.statisticsPassed=true;
            for(int actor=0;actor<session.Actors.Count;actor++)
                report.statisticsPassed &= report.statisticsShots[actor]==report.actorShots[actor] &&
                    session.Match.Scores[actor]==report.kills[actor]-report.voidDeaths[actor] &&
                    report.hits[actor]==session.Actors[1-actor].HitSerial &&
                    report.kills[actor]==report.statisticsDeaths[1-actor]-report.voidDeaths[1-actor];
            report.fullMatch = fullMatch;
            report.resultsSeen = rematchReady;
            report.rematchStarted = rematchReady && report.round == 2;
            report.completedTick = completedTick;
            report.completedWinner = completedWinner;
            report.completedScores = completedScores;
            report.completedShots = completedShots;
            report.completedLives = completedLives;
            report.elapsedSeconds = Time.realtimeSinceStartup - started;
            bool matchPassed = fullMatch ? report.rematchStarted && completedTick >= Simulation.CombatRules.CountdownTicks + Simulation.CombatRules.MatchTicks &&
                completedWinner >= 0 && completedShots[0] > 0 && completedShots[1] > 0 : report.round == 1;
            report.passed = errors == 0 && report.statisticsPassed && session.IsOnline && matchPassed && report.tick >= 900 &&
                report.actorShots.Length == 2 && report.actorShots[0] > 0 && report.actorShots[1] > 0 &&
                Array.Exists(fullMatch ? completedLives ?? Array.Empty<int>() : report.actorLives, life => life > 1) &&
                (role == "host" ? report.shots > 1 && report.rewoundShots > 0 : report.snapshots > 30 && report.acknowledged > 300);
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output, JsonUtility.ToJson(report, true));
            Debug.Log("DUEL_VALIDATION " + JsonUtility.ToJson(report));
            // Let both processes save while their peer is still connected.
            yield return new WaitForSecondsRealtime(4);
            Application.Quit(report.passed ? 0 : 1);
        }
    }
}
#endif
