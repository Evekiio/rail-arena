#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Diagnostics;
using UnityEngine;

namespace RailArena
{
    /// <summary>Bounds unattended players even if their launching shell is terminated.</summary>
    public sealed class ValidationLifetime : MonoBehaviour
    {
        private Process owner;
        private double deadline;
        private double nextCheck;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "--validation-owner-pid");
            if (index < 0 || Application.isEditor) return;
            var guard = new GameObject("Validation lifetime").AddComponent<ValidationLifetime>();
            int timeoutIndex = Array.IndexOf(args, "--validation-timeout");
            if (index + 1 >= args.Length || !int.TryParse(args[index + 1], out int pid) || pid <= 0 ||
                timeoutIndex < 0 || timeoutIndex + 1 >= args.Length ||
                !int.TryParse(args[timeoutIndex + 1], out int seconds) || seconds < 1 || seconds > 1860)
            {
                Application.Quit(2);
                return;
            }
            guard.deadline = Time.realtimeSinceStartupAsDouble + seconds;
            try { guard.owner = Process.GetProcessById(pid); }
            catch (ArgumentException) { Application.Quit(2); }
        }

        private void Update()
        {
            double now = Time.realtimeSinceStartupAsDouble;
            if (now < nextCheck) return;
            nextCheck = now + 1;
            if (owner == null || owner.HasExited || now >= deadline)
            {
                UnityEngine.Debug.LogError("Validation owner exited or its runtime limit expired; closing player.");
                Application.Quit(2);
            }
        }

        private void OnDestroy() => owner?.Dispose();
    }
}
#endif
