# Art atmosphere / Render Graph review

## 1. Validation summary

Reviewed for Unity 6000.6.7f1 and installed URP 17.6.0 using the Unity Render Graph validation workflow. The promoted pass retains the previously reviewed atmosphere algorithm and applies to the main game camera before post-processing. Portal destination cameras and portrait cameras are excluded by the existing camera/tag guard.

## 2. Confirmed issues

The earlier renderer/shader/material lived under `Assets/Validation`, which the build-identity generator excludes. Production assets now have their own runtime feature, shader, material, renderer data and pipeline under normal production paths. Rifle UV/AO inputs are likewise promoted as copies. Original experimental assets remain intact.

## 3. Remaining assumptions

The depth and shadow textures use URP's global bindings. Input-depth configuration and resource declarations retain their lifetimes; their values still require graphics-enabled runtime verification. A skipped back-buffer/depth-unavailable camera does not establish an executed effect. Rendering performance and native-player presentation are separate from source review.

## 4. Resource and material checks

- The primary color input is explicitly supplied to `Blitter.BlitTexture`, which binds the shader's `_BlitTexture` input.
- The shader reads URP camera depth and main-light shadow resources. The pass requests depth and declares graph reads for both available resources.
- Every used pooled `PassData` field is assigned on each recording. The render function is static and does not capture camera state.
- Destination descriptors derive from the active graph color texture. Only name, clearing, MSAA and depth attachment are changed.
- The destination is a distinct texture, registered as the render attachment. `resources.cameraColor` passes that result onward to post-processing.
- Serialized material references are installed in the production renderer feature. Missing material, invalid depth and active back-buffer cases are guarded.

## 5. Corrected implementation

No algorithm rewrite was needed for promotion. The feature is `RailArena.Rendering.ArtAtmosphereFeature`; `ArtAtmosphereBuilder` creates production renderer subassets with the promoted material and shader. This avoids duplicate class names and preserves the experimental renderer for historical comparison.

## 6. Acceptance still required

Inspect the integrated graphics-enabled captures and actual packaged build for effect execution and artifacts. Measure representative combat/portal/foliage/basement views at 1080p. GPU timers that return no valid samples are unavailable evidence. Sanders performs the final native-input, sound and normal-speed playtest; intermediate art milestones do not stop production.
