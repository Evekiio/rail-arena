using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class RailFeedbackTests
    {
        [TestCase(RailCue.Shot)]
        [TestCase(RailCue.Hit)]
        [TestCase(RailCue.Elimination)]
        [TestCase(RailCue.Hurt)]
        [TestCase(RailCue.Pickup)]
        [TestCase(RailCue.Launch)]
        public void SynthesizedCuesHaveFiniteBoundedSamplesAndFadedEndpoints(RailCue cue)
        {
            var clip = RailAudio.Create(cue);
            try
            {
                var samples = new float[clip.samples]; clip.GetData(samples, 0);
                Assert.That(clip.channels, Is.EqualTo(1));
                Assert.That(clip.frequency, Is.EqualTo(44100));
                Assert.That(samples.All(s => float.IsFinite(s) && Mathf.Abs(s) <= .9f), Is.True);
                Assert.That(samples.Max(s => Mathf.Abs(s)), Is.GreaterThan(.2f));
                Assert.That(samples[0], Is.Zero);
                Assert.That(Mathf.Abs(samples[^1]), Is.LessThan(.001f));
                double Energy(int start, int count) => samples.Skip(start).Take(count).Average(s => (double)s * s);
                Assert.That(Energy(samples.Length - 1000, 1000), Is.LessThan(Energy(0, 1000) * .1));
                if(cue==RailCue.Pickup || cue==RailCue.Launch)WritePreview(cue,samples);
            }
            finally { Object.DestroyImmediate(clip); }
        }

        private static void WritePreview(RailCue cue,float[] samples)
        {
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Validation/TraversalAudio"));Directory.CreateDirectory(folder);
            using(var writer=new BinaryWriter(File.Create(Path.Combine(folder,cue+".wav"))))
            {
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+samples.Length*2);writer.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
                writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(RailAudio.SampleRate);writer.Write(RailAudio.SampleRate*2);writer.Write((short)2);writer.Write((short)16);
                writer.Write(Encoding.ASCII.GetBytes("data"));writer.Write(samples.Length*2);
                foreach(float sample in samples)writer.Write((short)Mathf.RoundToInt(sample*32767));
            }
        }

        [Test]
        public void RepeatedShotsReuseFixedPoolWithoutCollidersAndResetAllEffects()
        {
            var root = new GameObject("Feedback pool fixture");
            try
            {
                var feedback = root.AddComponent<RailFeedback>();
                typeof(RailFeedback).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(feedback, null);
                var renderers = root.GetComponentsInChildren<LineRenderer>(true);
                Assert.That(renderers.Length, Is.EqualTo(RailFeedback.Capacity * 4));
                Assert.That(renderers.All(r => r.sharedMaterial.shader.isSupported), Is.True);
                for (int i = 0; i < RailFeedback.Capacity * 3; i++)
                    feedback.Shot(Vector3.zero, new Vector3(0, 0, 20 + i), Vector3.zero, false, true);
                Assert.That(root.GetComponentsInChildren<LineRenderer>(true), Is.EqualTo(renderers));
                Assert.That(root.GetComponentsInChildren<Collider>(true), Is.Empty);
                Assert.That(renderers.Where(r => r.name == "Core").All(r => r.GetPosition(1).z >= 44), Is.True);
                feedback.ResetFeedback();
                Assert.That(renderers.All(r => !r.gameObject.activeInHierarchy), Is.True);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
