using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace RailArena.Editor
{
    /// <summary>Headless installation of the URP-matched VFX Graph package.</summary>
    public static class VfxPackageInstall
    {
        private static AddRequest request;
        private static double deadline;

        public static void Install()
        {
            request = Client.Add("com.unity.visualeffectgraph@17.6.0");
            deadline = EditorApplication.timeSinceStartup + 600;
            EditorApplication.update += Poll;
        }

        private static void Poll()
        {
            if (!request.IsCompleted)
            {
                if (EditorApplication.timeSinceStartup > deadline)
                {
                    EditorApplication.update -= Poll;
                    Debug.LogError("VFX_PACKAGE_INSTALL_TIMEOUT");
                    EditorApplication.Exit(2);
                }
                return;
            }
            EditorApplication.update -= Poll;
            if (request.Status != StatusCode.Success)
            {
                Debug.LogError("VFX_PACKAGE_INSTALL_FAILED " + request.Error?.message);
                EditorApplication.Exit(1);
                return;
            }
            Debug.Log("VFX_PACKAGE_INSTALLED " + request.Result.name + "@" + request.Result.version);
            EditorApplication.Exit(0);
        }
    }
}
