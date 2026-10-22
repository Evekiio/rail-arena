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
            public int outboundClamps;
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
            var session = FindFirstObjectByType<ArenaSession>();
            session.AutomatedHuman = true;
            if (role == "host") session.HostDuel(port); else session.JoinDuel("127.0.0.1", port);
            float started = Time.realtimeSinceStartup;
            bool readied = false;
            while (Time.realtimeSinceStartup - started < duration)
            {
                if (!readied && session.IsOnline && session.ConnectionStatus.StartsWith("CONNECTED"))
                { session.ToggleReady(); readied = true; }
                yield return null;
            }
            int observed = 0;
            foreach (var actor in session.Actors) observed += actor.ShotSerial;
            var report = new Report { role = role, fingerprint = Networking.BuildIdentity.Fingerprint,
                round = session.NetworkRound, tick = session.Match?.Tick ?? 0, errors = errors,
                shots = session.ShotsFired, observedShots = observed, deaths = session.Deaths, pads = session.PadLaunches,
                snapshots = session.SnapshotsReceived, acknowledged = session.LastAcknowledgedInput,
                pending = session.PendingInputs, correction = session.MaximumCorrection, outboundClamps = session.OutboundClamps, connection = session.ConnectionStatus };
            report.passed = errors == 0 && session.IsOnline && report.round == 1 && report.tick > 900 &&
                observed > 1 && (role == "host" ? report.shots > 1 : report.snapshots > 30 && report.acknowledged > 300);
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
