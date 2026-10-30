using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace RailArena.Editor
{
    public static class BronzeObjectInventory
    {
        public static void Capture()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/SkyGardens.unity");
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/RailArena/Generated/SkyGardensArt/Material-bronze.mat");
            foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).Where(x => x.sharedMaterials.Contains(material)))
                Debug.Log("BRONZE_OBJECT " + r.name + " center=" + r.bounds.center + " size=" + r.bounds.size);
        }
    }
}
