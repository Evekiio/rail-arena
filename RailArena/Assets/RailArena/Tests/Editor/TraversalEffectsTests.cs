using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
namespace RailArena.Tests
{
    public sealed class TraversalEffectsTests
    {
        [Test]
        public void SynthesizedClipsHaveBoundedEnvelopesAndQuietLoopSeams()
        {
            foreach (TraversalSound sound in System.Enum.GetValues(typeof(TraversalSound)))
            {
                var clip = TraversalAudio.Create(sound); var data = new float[clip.samples]; clip.GetData(data, 0);
                float peak = 0; foreach(float sample in data) { Assert.That(float.IsNaN(sample), Is.False); peak = Mathf.Max(peak, Mathf.Abs(sample)); }
                Assert.That(peak, Is.InRange(.02f,.75f)); Assert.That(Mathf.Abs(data[0]), Is.LessThan(.001f));
                Assert.That(Mathf.Abs(data[data.Length-1]), Is.LessThan(.01f)); Object.DestroyImmediate(clip);
            }
        }
        [UnityTest]
        public IEnumerator EffectsRespectPreferencesAndDoNotMoveColliders()
        {
            EditorSceneManager.OpenScene("Assets/RailArena/Generated/TwinPlatforms.unity"); yield return new EnterPlayMode(); yield return null;
            var session = Object.FindAnyObjectByType<ArenaSession>();
            var effects = Object.FindObjectsByType<TraversalPresentation>(FindObjectsSortMode.None);
            Assert.That(effects.Length, Is.EqualTo(4));
            var colliders = session.World.GetComponentsInChildren<Collider>(); var bounds = new Bounds[colliders.Length];
            for(int i=0;i<colliders.Length;i++) bounds[i]=colliders[i].bounds;
            session.enabled = false;
            var camera = session.View;
            var capture = typeof(TwinPlatformsTests).GetMethod("Capture", BindingFlags.Static | BindingFlags.NonPublic);
            foreach(var effect in effects)
            {
                Assert.That(effect.Surface.sharedMaterial.shader, Is.EqualTo(Resources.Load<Shader>(effect.Kind == TraversalEffectKind.SuperPad ? "Effects/PadEnergy" : "Effects/TraversalEnergy")));
                var center = effect.transform.position;
                camera.transform.position = center + (effect.Kind == TraversalEffectKind.Portal ? effect.transform.forward * 6 + Vector3.up * 2 : new Vector3(-4,4,4));
                camera.transform.LookAt(center + Vector3.up * (effect.Kind == TraversalEffectKind.Portal ? 1.7f : .2f));
                capture.Invoke(null,new object[]{camera,"FX-"+effect.name+"-Idle"});
                effect.Activate();
                capture.Invoke(null,new object[]{camera,"FX-"+effect.name+"-Active"}); effect.ResetPulse();
                Assert.That(UnityEditor.ShaderUtil.ShaderHasError(effect.Surface.sharedMaterial.shader), Is.False);
            }
            session.Sound.Effects = 0;
            foreach(var effect in effects) { Assert.That(effect.Surface, Is.Not.Null); Assert.That(effect.Activate(), Is.True); Assert.That(effect.Activate(), Is.False); Assert.That(effect.EventVolume, Is.Zero); }
            session.Comfort.ReducedFlash = true; yield return null;
            foreach(var effect in effects) { Assert.That(effect.Reduced, Is.True); Assert.That(effect.BurstStrength, Is.LessThanOrEqualTo(.22f)); Assert.That(effect.AmbientVolume, Is.Zero); }
            Physics.SyncTransforms(); for(int i=0;i<colliders.Length;i++) Assert.That(colliders[i].bounds, Is.EqualTo(bounds[i]));
            var present = typeof(ArenaSession).GetMethod("PresentPortal", BindingFlags.Instance | BindingFlags.NonPublic);
            var gate = session.World.Portals[0]; foreach(var effect in effects) effect.ResetPulse();
            present.Invoke(session,new object[]{0,123,gate}); Assert.That(gate.Presentation.Activations, Is.EqualTo(1));
            gate.Presentation.ResetPulse(); present.Invoke(session,new object[]{0,123,gate}); Assert.That(gate.Presentation.Activations, Is.Zero);
            session.Sound.Effects=1; session.Comfort.ReducedFlash=false;
            yield return new ExitPlayMode();
        }
    }
}
