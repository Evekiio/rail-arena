using UnityEngine;
namespace RailArena
{
    public sealed class SkyGardenMotion:MonoBehaviour
    {
        public bool Water;
        private ArenaSession session;private Renderer surface;private MaterialPropertyBlock properties;private Quaternion rest;private float clock,phase;
        private static readonly int ClockId=Shader.PropertyToID("_Clock");
        private void Awake(){session=FindFirstObjectByType<ArenaSession>();surface=GetComponent<Renderer>();properties=new MaterialPropertyBlock();rest=transform.localRotation;phase=transform.position.x*.31f+transform.position.z*.27f;}
        private void Update()
        {
            if(session!=null&&session.Running&&session.Paused&&!session.IsOnline)return;
            clock+=Mathf.Min(Time.deltaTime,.1f);
            if(Water){surface.GetPropertyBlock(properties);properties.SetFloat(ClockId,clock*(session!=null&&session.Comfort.ReducedFlash ? .35f : 1));surface.SetPropertyBlock(properties);}
            else transform.localRotation=rest*Quaternion.Euler(Mathf.Sin(clock*.8f+phase)*.65f,0,Mathf.Sin(clock*.65f+phase)*.9f);
        }
        private void OnDestroy(){if(!Water)transform.localRotation=rest;}
    }
}
