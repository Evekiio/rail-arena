using System.Collections.Generic;
using UnityEngine;

namespace RailArena
{
    /// <summary>Depth-tested, physical route labels. Launch and portal authority remain untouched.</summary>
    public sealed class ArenaWayfindingArt : MonoBehaviour
    {
        sealed class Label
        {
            public TextMesh text;
            public string words;
            public Font font;
            public int size;
            public float characterSize;
            public Color color;
            public TextAnchor anchor;
            public TextAlignment alignment;
            public Vector3 position, scale;
            public Quaternion rotation;
            public Material[] materials;
        }
        readonly List<Label> originals = new();
        Font brandFont;
        Material glyphs;

        public void Initialize(ArenaWorld world)
        {
            brandFont = Resources.Load<Font>("UI/Brand/Fonts/BarlowCondensed-SemiBold");
            var shader = Resources.Load<Shader>("Effects/WorldGlyph");
            if (brandFont == null || shader == null) throw new System.InvalidOperationException("Wayfinding font/shader missing");
            brandFont.RequestCharactersInTexture("SUMMIT BRIDGE ARENA BASEMENT 1234+/ ", 64);
            glyphs = new Material(shader) { name = "Owned depth-tested route typography", mainTexture = brandFont.material.mainTexture };
            Font.textureRebuilt += FontRebuilt;
            foreach (var text in FindObjectsByType<TextMesh>())
            {
                originals.Add(new Label { text = text, words = text.text, font = text.font, size = text.fontSize,
                    characterSize = text.characterSize, color = text.color, anchor = text.anchor, alignment = text.alignment,
                    position = text.transform.localPosition, rotation = text.transform.localRotation, scale = text.transform.localScale,
                    materials = text.GetComponent<Renderer>().sharedMaterials });
                text.font = brandFont; text.fontSize = 64; text.color = new Color(.61f,.9f,.85f).linear;
                text.GetComponent<Renderer>().sharedMaterial = glyphs;
            }
            foreach (var pad in world.Pads)
            {
                var direction = Vector3.ProjectOnPlane(pad.Destination - pad.transform.position, Vector3.up).normalized;
                if (direction.sqrMagnitude < .01f) direction = Vector3.forward;
                foreach (var text in pad.GetComponentsInChildren<TextMesh>(true))
                {
                    text.text = pad.Destination.y - pad.transform.position.y > 18 ? "SUMMIT" : "BRIDGE";
                    text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center; text.characterSize = .22f;
                    text.transform.SetPositionAndRotation(pad.transform.position - direction * .65f + Vector3.up * .18f, Quaternion.LookRotation(Vector3.down, direction));
                    text.transform.localScale = Vector3.one;
                }
            }
            FontRebuilt(brandFont);
        }

        void FontRebuilt(Font font) { if (font == brandFont && glyphs != null) glyphs.mainTexture = font.material.mainTexture; }

        void OnDestroy()
        {
            Font.textureRebuilt -= FontRebuilt;
            foreach (var label in originals)
            {
                if (label.text == null) continue;
                label.text.text = label.words; label.text.font = label.font; label.text.fontSize = label.size;
                label.text.characterSize = label.characterSize; label.text.color = label.color;
                label.text.anchor = label.anchor; label.text.alignment = label.alignment;
                label.text.transform.SetLocalPositionAndRotation(label.position, label.rotation); label.text.transform.localScale = label.scale;
                label.text.GetComponent<Renderer>().sharedMaterials = label.materials;
            }
            if (glyphs != null) Destroy(glyphs);
        }
    }
}
