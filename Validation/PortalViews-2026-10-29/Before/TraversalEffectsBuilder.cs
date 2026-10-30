using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace RailArena.Editor
{
    public static class TraversalEffectsBuilder
    {
        public static void Apply()
        {
            EditorSceneManager.OpenScene(TwinPlatformsBuilder.ScenePath);
            ApplyToWorld(Object.FindAnyObjectByType<ArenaSession>().World);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene()); AssetDatabase.SaveAssets();
            Debug.Log("TRAVERSAL_EFFECTS_APPLIED portals=2 pads=2");
        }
        public static void ApplyToWorld(ArenaWorld world)
        {
            foreach (var gate in world.Portals)
                gate.Presentation = Attach(gate.gameObject, TraversalEffectKind.Portal, "Opaque shot-blocking portal",
                    gate.transform.position.x < 0 ? new Color(.025f,.42f,1) : new Color(1,.055f,.025f));
            foreach (var pad in world.Pads)
                pad.Presentation = Attach(pad.gameObject, TraversalEffectKind.SuperPad, "Super launch coil", new Color(1,.57f,.04f));
        }
        private static TraversalPresentation Attach(GameObject root, TraversalEffectKind kind, string surface, Color tint)
        {
            var effect = root.GetComponent<TraversalPresentation>() ?? root.AddComponent<TraversalPresentation>();
            effect.Kind = kind; effect.Tint = tint;
            effect.Surface = root.GetComponentsInChildren<Renderer>().Single(renderer => renderer.name == surface);
            EditorUtility.SetDirty(effect); return effect;
        }
    }
}
