# Rail Arena brand kit

![Rail Arena wordmark](rail-arena-logo.png)

The mark combines an angular arena gateway with two parallel cyan rail strokes. Warm ivory lettering and graphite edges connect it to the contender armor; cyan supplies the rail-energy accent.

## Assets

| Asset | Native size | Use |
| --- | --- | --- |
| [Transparent logo](rail-arena-logo.png) | 2172 × 724 PNG, RGBA | README graphics, project presentations and overlays on light or dark backgrounds |
| [Brand banner](rail-arena-banner.png) | 2172 × 724 PNG | Wide README/project header; includes the logo and four contender concepts |
| [Prompts and provenance](PROMPTS.json) | JSON | Built-in image generation prompts, input roles and output provenance |

![Rail Arena banner](rail-arena-banner.png)

The banner is generated concept brand art based on the approved contender direction. Its environment and character finish are a visual target, not a gameplay screenshot or a claim about the current build.

## Palette

| Color | Hex | Role |
| --- | --- | --- |
| Graphite | `#121A22` | Main background and outline |
| Warm ivory | `#EFE9D9` | Wordmark and primary light surfaces |
| Electric cyan | `#48DAE8` | Rail strokes and restrained emphasis |
| Muted teal | `#254C58` | Secondary planes |
| Copper | `#C4865E` | Supporting warm detail |

These are working graphic palette values; the generated bevels and scene lighting produce tonal variation. They are not Unity material values.

## Usage

- Keep the logo's original 3:1 proportions and the emblem/wordmark together. Leave clear space around it, at least the height of the wordmark's inner letter stroke.
- Use the supplied transparent PNG on both light and dark backgrounds. Preserve the graphite edge for contrast; do not add another glow or stretch individual letters.
- Keep the banner at its 3:1 aspect ratio and avoid cropping the logo, character heads or feet.
- Use ordinary readable text for documentation. The wordmark is custom raster artwork; no font file or editable vector master is supplied.
- Keep real gameplay captures captioned separately from concept artwork. Current development/release status stays in the main README.

The source images were generated with the built-in `image_gen` tool. The final PNGs retain their original native pixels and transparency.
