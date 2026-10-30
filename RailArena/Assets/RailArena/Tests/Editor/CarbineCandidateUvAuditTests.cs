using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RailArena.Tests
{
    public sealed class CarbineCandidateUvAuditTests
    {
        [Serializable] private sealed class Entry { public int lod, triangles, unmatchedTriangles, ambiguousTriangles, samples; public string texture; public double meanAbsoluteRgba, fractionAboveTen, meanUvShift; }
        [Serializable] private sealed class Report { public string note; public Entry[] entries; }

        [Test]
        public void CompareExistingAtlasSamplesAcrossCandidateLods()
        {
            string[] textures = { "BaseColor.png", "MetallicSmoothness.png", "Normal.png", "MetalDetailMask.png", "Emission.png" };
            var entries = new System.Collections.Generic.List<Entry>();
            foreach (var textureName in textures)
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/RailArena/Art/PrecisionCarbine/" + textureName);
                Assert.That(texture, Is.Not.Null, textureName);
                var target = new RenderTexture(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                var readback = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false, true);
                var oldActive = RenderTexture.active;
                try
                {
                    target.Create(); Graphics.Blit(texture, target); RenderTexture.active = target;
                    readback.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); readback.Apply();
                    var pixels = readback.GetPixels32();
                    Color32 Sample(Vector2 uv)
                    {
                        int x = Mathf.Clamp(Mathf.FloorToInt(uv.x * target.width), 0, target.width - 1);
                        int y = Mathf.Clamp(Mathf.FloorToInt(uv.y * target.height), 0, target.height - 1);
                        return pixels[y * target.width + x];
                    }
                    for (int lod = 0; lod < 3; lod++)
                    {
                        var current = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/RailArena/Art/PrecisionCarbine/Carbine_LOD{lod}.fbx");
                        var candidate = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Validation/CarbineCandidate/Carbine_LOD{lod}.fbx");
                        Assert.That(current, Is.Not.Null); Assert.That(candidate, Is.Not.Null);
                        var candidates = candidate.GetComponentsInChildren<MeshFilter>(true).ToDictionary(f => f.name, f => f.sharedMesh);
                        int triangles = 0, unmatched = 0, ambiguous = 0, samples = 0, overTen = 0; double total = 0, uvShift = 0;
                        foreach (var filter in current.GetComponentsInChildren<MeshFilter>(true))
                        {
                            Assert.That(candidates.ContainsKey(filter.name), Is.True, filter.name);
                            var a = filter.sharedMesh; var b = candidates[filter.name];
                            var ai = a.triangles; var bi = b.triangles; var av = a.vertices; var bv = b.vertices; var au = a.uv; var bu = b.uv;
                            Assert.That(ai.Length, Is.EqualTo(bi.Length), filter.name);
                            string VertexKey(Vector3 p) => $"{Mathf.RoundToInt(p.x * 10000)},{Mathf.RoundToInt(p.y * 10000)},{Mathf.RoundToInt(p.z * 10000)}";
                            string TriangleKey(Vector3 x, Vector3 y, Vector3 z) => string.Join("|", new[] { VertexKey(x), VertexKey(y), VertexKey(z) }.OrderBy(s => s, StringComparer.Ordinal));
                            var byGeometry = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<int>>();
                            for (int i = 0; i < bi.Length; i += 3)
                            {
                                string key = TriangleKey(bv[bi[i]], bv[bi[i+1]], bv[bi[i+2]]);
                                if (!byGeometry.TryGetValue(key, out var matches)) byGeometry[key] = matches = new System.Collections.Generic.List<int>();
                                matches.Add(i);
                            }
                            for (int i = 0; i < ai.Length; i += 3)
                            {
                                triangles++;
                                string key = TriangleKey(av[ai[i]], av[ai[i+1]], av[ai[i+2]]);
                                if (!byGeometry.TryGetValue(key, out var matches)) { unmatched++; continue; }
                                if (matches.Count != 1) { ambiguous++; continue; }
                                int offset = matches[0]; var mapped = new int[3]; bool valid = true;
                                for (int vertex = 0; vertex < 3; vertex++)
                                {
                                    int found = -1;
                                    for (int j = 0; j < 3; j++) if (Vector3.Distance(av[ai[i + vertex]], bv[bi[offset + j]]) < .0001f) { found = bi[offset + j]; break; }
                                    if (found < 0) { valid = false; break; }
                                    mapped[vertex] = found;
                                }
                                if (!valid) { unmatched++; continue; }
                                foreach (var weights in new[] { new Vector3(1f/3, 1f/3, 1f/3), new Vector3(.6f, .2f, .2f), new Vector3(.2f, .6f, .2f), new Vector3(.2f, .2f, .6f) })
                                {
                                    var originalUv = au[ai[i]] * weights.x + au[ai[i+1]] * weights.y + au[ai[i+2]] * weights.z;
                                    var candidateUv = bu[mapped[0]] * weights.x + bu[mapped[1]] * weights.y + bu[mapped[2]] * weights.z;
                                    var ca = Sample(originalUv); var cb = Sample(candidateUv);
                                    double diff = (Math.Abs(ca.r - cb.r) + Math.Abs(ca.g - cb.g) + Math.Abs(ca.b - cb.b) + Math.Abs(ca.a - cb.a)) / 4.0;
                                    uvShift += Vector2.Distance(originalUv, candidateUv);
                                    total += diff; samples++; if (diff > 10) overTen++;
                                }
                            }
                        }
                        Assert.That(samples, Is.GreaterThan(0));
                        entries.Add(new Entry { lod = lod, texture = textureName, triangles = triangles, unmatchedTriangles = unmatched, ambiguousTriangles = ambiguous, samples = samples, meanAbsoluteRgba = total / samples, fractionAboveTen = (double)overTen / samples, meanUvShift = uvShift / samples });
                    }
                }
                finally { RenderTexture.active = oldActive; target.Release(); Object.DestroyImmediate(target); Object.DestroyImmediate(readback); }
            }
            string output = Path.GetFullPath("../Validation/SkyGardens-Playable-2026-10-29/carbine-uv-atlas-audit-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + ".json");
            File.WriteAllText(output, JsonUtility.ToJson(new Report { note = "Nearest RGBA atlas samples at four barycentric positions per geometrically matched triangle; 0-255 channel units. Ambiguous duplicate triangles and unmatched geometry are excluded; filtering, tangent-space interpretation and AO are not tested.", entries = entries.ToArray() }, true));
            Debug.Log("CARBINE_UV_ATLAS_AUDIT " + output);
        }
    }
}
