using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RailArena.Editor
{
    public static class RailAudioReview
    {
        [Serializable] private sealed class CueReport
        {
            public string cue, file;
            public int sampleRate, samples;
            public float peak, rms;
        }
        [Serializable] private sealed class ReviewReport
        {
            public string source = "RailArena/Assets/RailArena/Runtime/RailAudio.cs";
            public string license = "MIT, original synthesis with no third-party samples";
            public string scope = "Technical PCM review exports; sound-on quality acceptance remains pending";
            public List<CueReport> cues = new();
        }

        [MenuItem("Rail Arena/Export original rail audio review")]
        public static void Export()
        {
            string folder = Path.GetFullPath("../ArtSource/Original/Audio");
            Directory.CreateDirectory(folder);
            var report = new ReviewReport();
            foreach (RailCue cue in Enum.GetValues(typeof(RailCue)))
            {
                var clip = RailAudio.Create(cue);
                try
                {
                    var samples = new float[clip.samples]; clip.GetData(samples, 0);
                    string file = "RA01-" + cue + ".wav";
                    using (var output = new BinaryWriter(File.Create(Path.Combine(folder, file))))
                    {
                        output.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); output.Write(36 + samples.Length * 2);
                        output.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); output.Write(16);
                        output.Write((short)1); output.Write((short)1); output.Write(RailAudio.SampleRate);
                        output.Write(RailAudio.SampleRate * 2); output.Write((short)2); output.Write((short)16);
                        output.Write(System.Text.Encoding.ASCII.GetBytes("data")); output.Write(samples.Length * 2);
                        foreach (float sample in samples) output.Write((short)Mathf.RoundToInt(sample * 32767));
                    }
                    float peak = 0; double energy = 0;
                    foreach (float sample in samples) { peak = Mathf.Max(peak, Mathf.Abs(sample)); energy += sample * sample; }
                    report.cues.Add(new CueReport { cue = cue.ToString(), file = file, sampleRate = clip.frequency,
                        samples = samples.Length, peak = peak, rms = (float)Math.Sqrt(energy / samples.Length) });
                }
                finally { UnityEngine.Object.DestroyImmediate(clip); }
            }
            File.WriteAllText(Path.Combine(folder, "review.json"), JsonUtility.ToJson(report, true));
        }
    }
}
