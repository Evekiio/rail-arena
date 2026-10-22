#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace RailArena
{
    /// <summary>Explicitly enabled, game-local evidence for native input acceptance.</summary>
    public sealed class NativeInputReview : MonoBehaviour
    {
        [Serializable]
        private sealed class Report
        {
            public string fingerprint;
            public int frames, focusedFrames, escapePresses, jumpPresses, movementPresses, firePresses;
            public bool focused, running, paused;
            public int tick;
            public Vector3 position;
            public float sensitivity, volume;
        }

        private readonly Report report = new();
        private string output;
        private float nextWrite;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Application.isEditor) return;
            string folder = Path.GetDirectoryName(Application.dataPath);
            if (!File.Exists(Path.Combine(folder, "enable-native-input-review.txt"))) return;
            var review = new GameObject("Native input review").AddComponent<NativeInputReview>();
            review.output = Path.Combine(folder, "native-input-report.json");
            review.report.fingerprint = Networking.BuildIdentity.Fingerprint;
        }

        private void OnEnable() => InputSystem.onAfterUpdate += ObserveInput;
        private void OnDisable() => InputSystem.onAfterUpdate -= ObserveInput;

        private void ObserveInput()
        {
            if (InputState.currentUpdateType != InputUpdateType.Dynamic) return;
            if (!Application.isFocused) return;
            var keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.escapeKey.wasPressedThisFrame) report.escapePresses++;
                if (keyboard.spaceKey.wasPressedThisFrame) report.jumpPresses++;
                if (keyboard.wKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame ||
                    keyboard.sKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) report.movementPresses++;
            }
            if (Mouse.current?.leftButton.wasPressedThisFrame == true) report.firePresses++;
        }

        private void Update()
        {
            report.frames++;
            report.focused = Application.isFocused;
            if (report.focused) report.focusedFrames++;
            if (Time.unscaledTime < nextWrite || output == null) return;
            nextWrite = Time.unscaledTime + 1;
            var session = FindFirstObjectByType<ArenaSession>();
            if (session == null) return;
            report.running = session.Running;
            report.paused = session.Paused;
            report.tick = session.Match?.Tick ?? 0;
            report.position = session.Human != null ? session.Human.transform.position : Vector3.zero;
            report.sensitivity = session.Sensitivity;
            report.volume = AudioListener.volume;
            File.WriteAllText(output, JsonUtility.ToJson(report, true));
        }
    }
}
#endif
