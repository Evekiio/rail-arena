using UnityEditor;
using UnityEngine;
namespace RailArena.Editor
{
    public static class HealthUpgradeBuilder
    {
        public static void ApplyToWorld(ArenaWorld world)
        {
            const string folder = "Assets/RailArena/Generated/TwinPlatformsArt/";
            var glow = Material(folder + "Health glow.mat", new Color(.12f,1,.6f), 2.2f);
            const string haloPath = folder + "Health halo.mat";
            var halo = AssetDatabase.LoadAssetAtPath<Material>(haloPath);
            if (halo == null) { halo = new Material(UnityEngine.Resources.Load<Shader>("Effects/RailPulse")); AssetDatabase.CreateAsset(halo, haloPath); }
            var frame = Material(folder + "Health cage.mat", new Color(.07f,.3f,.27f), .5f);
            foreach(var pickup in world.Pickups)
            {
                var presentation = pickup.Presentation;
                if (presentation.FloatingRoot != null) Object.DestroyImmediate(presentation.FloatingRoot.gameObject);
                foreach (var visual in pickup.Visuals) visual.enabled = false;
                if (presentation.Marker != null) presentation.Marker.enabled = false;
                var root = new GameObject("Floating health core").transform; root.SetParent(pickup.transform,false);
                root.localPosition = new Vector3(0,.45f,0); presentation.FloatingRoot=root;
                Part(root,"Medical cross vertical",PrimitiveType.Cube,new Vector3(0,0,-.31f),new Vector3(.16f,.65f,.09f),glow);
                presentation.Marker = Part(root,"Medical cross horizontal",PrimitiveType.Cube,new Vector3(0,0,-.31f),new Vector3(.55f,.17f,.09f),glow);
                Part(root,"Rear cross vertical",PrimitiveType.Cube,new Vector3(0,0,.31f),new Vector3(.16f,.65f,.09f),glow);
                Part(root,"Rear cross horizontal",PrimitiveType.Cube,new Vector3(0,0,.31f),new Vector3(.55f,.17f,.09f),glow);
                Part(root,"Luminous heart",PrimitiveType.Sphere,Vector3.zero,Vector3.one*.4f,glow);
                foreach(float y in new[]{-.4f,.4f})
                    foreach(float x in new[]{-.38f,.38f})
                        Part(root,"Cage rail",PrimitiveType.Cube,new Vector3(x,y,0),new Vector3(.07f,.07f,.7f),frame);
                foreach(float x in new[]{-.38f,.38f}) foreach(float z in new[]{-.32f,.32f})
                    Part(root,"Cage upright",PrimitiveType.Cube,new Vector3(x,0,z),new Vector3(.055f,.8f,.055f),frame);
                var haloRoot = new GameObject("Floating energy halo"); haloRoot.transform.SetParent(root,false);
                var ring = haloRoot.AddComponent<LineRenderer>(); ring.useWorldSpace=false; ring.positionCount=65;
                ring.sharedMaterial=halo; ring.widthMultiplier=.035f; ring.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
                ring.receiveShadows=false; ring.startColor=ring.endColor=new Color(.12f,1,.6f,.5f);
                for(int i=0;i<65;i++) { float angle=i/64f*Mathf.PI*2; ring.SetPosition(i,new Vector3(Mathf.Cos(angle)*.65f,-.35f,Mathf.Sin(angle)*.65f)); }
                var light=root.gameObject.AddComponent<Light>(); light.type=LightType.Point; light.color=new Color(.12f,1,.6f);
                light.range=3; light.intensity=.8f; light.shadows=LightShadows.None; presentation.Glow=light;
                pickup.Visuals = System.Array.Empty<Renderer>(); EditorUtility.SetDirty(pickup); EditorUtility.SetDirty(presentation);
            }
        }
        private static Renderer Part(Transform parent,string name,PrimitiveType type,Vector3 position,Vector3 scale,Material material)
        {
            var part=GameObject.CreatePrimitive(type); part.name=name; part.transform.SetParent(parent,false);
            part.transform.localPosition=position; part.transform.localScale=scale;
            Object.DestroyImmediate(part.GetComponent<Collider>()); var renderer=part.GetComponent<Renderer>(); renderer.sharedMaterial=material; return renderer;
        }
        private static Material Material(string path,Color color,float emission)
        {
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null) { material=new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material,path); }
            material.color=color; material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor",color*emission); EditorUtility.SetDirty(material); return material;
        }
    }
}
