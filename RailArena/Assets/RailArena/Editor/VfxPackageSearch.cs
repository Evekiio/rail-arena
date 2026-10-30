using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace RailArena.Editor
{
    /// <summary>Headless registry check for the project's visual effects dependency.</summary>
    public static class VfxPackageSearch
    {
        private static SearchRequest request;
        private static double deadline;

        public static void Search()
        {
            request = Client.Search("com.unity.visualeffectgraph");
            deadline = EditorApplication.timeSinceStartup + 120;
            EditorApplication.update += Poll;
        }

        private static void Poll()
        {
            if (!request.IsCompleted)
            {
                if (EditorApplication.timeSinceStartup > deadline)
                {
                    EditorApplication.update -= Poll;
                    Debug.LogError("VFX_PACKAGE_SEARCH_TIMEOUT");
                    EditorApplication.Exit(2);
                }
                return;
            }
            EditorApplication.update -= Poll;
            if (request.Status != StatusCode.Success || request.Result.Length == 0)
            {
                Debug.LogError("VFX_PACKAGE_SEARCH_FAILED " + request.Error?.message);
                EditorApplication.Exit(1);
                return;
            }
            foreach (var package in request.Result.Where(p => p.name == "com.unity.visualeffectgraph"))
                Debug.Log("VFX_PACKAGE_FOUND " + package.name + " latestCompatible=" + package.versions.latestCompatible +
                          " versions=" + string.Join(",", package.versions.all));
            EditorApplication.Exit(0);
        }
    }
}
