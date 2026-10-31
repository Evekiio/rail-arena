using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace RailArena.Editor
{
    public static class StoneSubmissionArtBuilder
    {
        const string Folder = "Assets/Validation/StoneSubmissionResearch/";
        [Serializable] sealed class MeshEvidence
        {
            public string source, derived, prefab;
            public int sourceVertices, derivedVertices, triangles, sourceSlots;
        }
        [Serializable] sealed class Evidence
        {
            public string scope = "Opt-in research GPU export. Canonical geometry/materials are retained; tint, painted/flat mask and highlight are baked into vertex attributes. Not a frame-time improvement or artistic approval.";
            public bool defaultEnabled = false;
            public MeshEvidence[] meshes;
        }
        static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

        static void ValidatePalette(Material[] palette)
        {
            var painted = palette[0];
            foreach (var material in palette)
            {
                Require(material.shader == painted.shader && material.renderQueue == painted.renderQueue, "Stone shader/queue differs");
                Require(Mathf.Abs(material.GetColor("_BaseColor").a - 1) < .000001f, "Stone palette must remain opaque");
                Require(material.GetColor("_EmissionColor").maxColorComponent < .000001f, "Emissive stone requires a separate palette contract");
                foreach (string property in new[] { "_BandLow", "_BandHigh", "_BandSoftness", "_Cull", "_Cutoff" })
                    Require(Mathf.Abs(material.GetFloat(property) - painted.GetFloat(property)) < .000001f, "Stone shared property differs: " + property);
                foreach (string property in new[] { "_ShadowTint", "_MidTint" })
                    Require(material.GetColor(property) == painted.GetColor(property), "Stone lighting palette differs: " + property);
                Require(material.GetTextureScale("_BaseMap") == Vector2.one && material.GetTextureOffset("_BaseMap") == Vector2.zero, "Stone UV transform must stay identity");
                if (material == palette[0] || material == palette[1])
                {
                    Require(material.IsKeywordEnabled("_NORMALMAP"), "Painted stone normal mapping is required");
                    foreach (string property in new[] { "_BaseMap", "_BumpMap", "_OcclusionMap" })
                        Require(material.GetTexture(property) == painted.GetTexture(property), "Painted stone texture differs: " + property);
                    foreach (string property in new[] { "_BumpScale", "_OcclusionStrength" })
                        Require(material.GetFloat(property) == painted.GetFloat(property), "Painted stone strength differs: " + property);
                }
                else
                {
                    Require(!material.IsKeywordEnabled("_NORMALMAP"), "Flat stone unexpectedly uses normal mapping");
                    foreach (string property in new[] { "_BaseMap", "_OcclusionMap" })
                        Require(material.GetTexture(property) == null || material.GetTexture(property) == Texture2D.whiteTexture, "Flat stone has a non-white texture: " + property);
                }
            }
        }

        static Mesh Derive(Mesh source, Material[] materials, Material[] palette)
        {
            Require(source.blendShapeCount == 0, "Stone source must be rigid geometry: " + source.name);
            Require(materials.Length == source.subMeshCount, "Stone source material slots differ from submeshes");
            using var snapshot = MeshUtility.AcquireReadOnlyMeshData(source);
            var data = snapshot[0];
            Require(data.HasVertexAttribute(VertexAttribute.Normal) && data.HasVertexAttribute(VertexAttribute.Tangent), "Stone source normals/tangents are incomplete: " + source.name);
            using var vectors = new NativeArray<Vector3>(data.vertexCount, Allocator.Temp);
            data.GetVertices(vectors); var points = vectors.ToArray();
            data.GetNormals(vectors); var normals = vectors.ToArray();
            using var tangentData = new NativeArray<Vector4>(data.vertexCount, Allocator.Temp);
            data.GetTangents(tangentData); var tangents = tangentData.ToArray();
            var inputUvs = new List<Vector4>[8]; var outputUvs = new List<Vector4>[8];
            for (int channel = 0; channel < 8; channel++)
            {
                inputUvs[channel] = new List<Vector4>();
                if (channel != 3 && data.HasVertexAttribute((VertexAttribute)((int)VertexAttribute.TexCoord0 + channel)))
                {
                    using var uv = new NativeArray<Vector4>(data.vertexCount, Allocator.Temp);
                    data.GetUVs(channel, uv); inputUvs[channel].AddRange(uv.ToArray());
                    Require(inputUvs[channel].Count == points.Length, "Stone source UV count differs");
                    outputUvs[channel] = new List<Vector4>();
                }
            }
            var vertices = new List<Vector3>(); var outNormals = new List<Vector3>(); var outTangents = new List<Vector4>();
            var colors = new List<Color>(); var properties = new List<Vector2>(); var triangles = new List<int>();
            for (int slot = 0; slot < materials.Length; slot++)
            {
                Require(source.GetTopology(slot) == MeshTopology.Triangles, "Stone source must use triangles");
                var remap = new Dictionary<int, int>();
                var material = materials[slot];
                float paint = material == palette[0] || material == palette[1] ? 1 : 0;
                using var sourceIndices = new NativeArray<int>(data.GetSubMesh(slot).indexCount, Allocator.Temp);
                data.GetIndices(sourceIndices, slot, true);
                foreach (int index in sourceIndices)
                {
                    if (!remap.TryGetValue(index, out int mapped))
                    {
                        mapped = vertices.Count; remap.Add(index, mapped);
                        vertices.Add(points[index]); outNormals.Add(normals[index]); outTangents.Add(tangents[index]);
                        var tint = material.GetColor("_BaseColor");
                        colors.Add(QualitySettings.activeColorSpace == ColorSpace.Linear ? tint.linear : tint);
                        properties.Add(new Vector2(paint, material.GetFloat("_HighlightStrength")));
                        for (int channel = 0; channel < 8; channel++) if (outputUvs[channel] != null) outputUvs[channel].Add(inputUvs[channel][index]);
                    }
                    triangles.Add(mapped);
                }
            }
            var result = new Mesh { name = source.name + " / Derived stone palette", indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            result.SetVertices(vertices); result.SetNormals(outNormals); result.SetTangents(outTangents); result.SetColors(colors);
            for (int channel = 0; channel < 8; channel++) if (outputUvs[channel] != null) result.SetUVs(channel, outputUvs[channel]);
            result.SetUVs(3, properties); result.SetTriangles(triangles, 0, false); result.bounds = source.bounds;
            return result;
        }

        public static void Prepare()
        {
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            string[] names = { "DeckIvory", "DeckStone", "DeckTeal", "DeckBronze", "DeckShade" };
            var palette = names.Select(name => AssetDatabase.LoadAssetAtPath<Material>("Assets/RailArena/Art/ArtPass/Environment/BasementDecks/" + name + ".mat")).ToArray();
            Require(palette.All(material => material != null), "Original stone palette is missing");
            ValidatePalette(palette);
            string materialPath = Folder + "StonePalette.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null) { material = new Material(palette[0]); AssetDatabase.CreateAsset(material, materialPath); }
            else { material.shader = palette[0].shader; material.CopyPropertiesFromMaterial(palette[0]); }
            material.SetFloat("_StoneVertexPaint", 1); material.EnableKeyword("_STONE_VERTEX_PAINT"); material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            var parts = new List<StoneSubmissionArt.Part>(); var records = new List<MeshEvidence>(); var seen = new HashSet<string>();
            foreach (string prefabPath in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Resources/Art" }).Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path))
            foreach (var filter in AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath).GetComponentsInChildren<MeshFilter>(true))
            {
                var renderer = filter.GetComponent<MeshRenderer>(); var source = filter.sharedMesh;
                if (renderer == null || source == null) continue;
                var materials = renderer.sharedMaterials;
                if (materials.Length < 3 || !materials.All(item => palette.Contains(item))) continue;
                Require(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source, out string guid, out long id), "Stone source has no persistent identity");
                string key = guid + "_" + id + "_" + string.Join("-", materials.Select(item => Array.IndexOf(palette, item)));
                if (!seen.Add(key)) continue;
                var output = Derive(source, materials, palette);
                string meshPath = Folder + key + ".asset";
                var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (existing == null) { AssetDatabase.CreateAsset(output, meshPath); existing = output; }
                else { EditorUtility.CopySerialized(output, existing); EditorUtility.SetDirty(existing); Object.DestroyImmediate(output); }
                parts.Add(new StoneSubmissionArt.Part { source = source, derived = existing, sourceMaterials = materials });
                records.Add(new MeshEvidence { source = AssetDatabase.GetAssetPath(source), derived = meshPath, prefab = prefabPath,
                    sourceVertices = source.vertexCount, derivedVertices = existing.vertexCount, triangles = (int)existing.GetIndexCount(0) / 3, sourceSlots = source.subMeshCount });
            }
            Require(parts.Count > 0, "No measured stone families were exported");
            const string path = "Assets/Resources/Art/StoneSubmissionResearch.asset";
            var art = AssetDatabase.LoadAssetAtPath<StoneSubmissionArt>(path);
            if (art == null) { art = ScriptableObject.CreateInstance<StoneSubmissionArt>(); AssetDatabase.CreateAsset(art, path); }
            art.palette = material; art.parts = parts.ToArray(); art.colorSpace = QualitySettings.activeColorSpace; EditorUtility.SetDirty(art); AssetDatabase.SaveAssets();
            Directory.CreateDirectory("../Validation"); File.WriteAllText("../Validation/stone-submission-prepared.json", JsonUtility.ToJson(new Evidence { meshes = records.ToArray() }, true));
            Debug.Log("STONE_SUBMISSION_RESEARCH_PREPARED " + parts.Count + " derived meshes; original sources retained; default disabled");
        }

        public static void PrepareWithDeckUpdate()
        {
            BasementDeckArtBuilder.Prepare();
            Prepare();
        }
    }
}
