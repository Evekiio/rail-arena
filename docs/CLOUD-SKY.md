# Foundry cloud-sky refinement

The static cloud sky now uses continuous direction-space billows instead of two repeating triangle-wave ridges. This removes the sharp sawtooth bands visible across the previous horizon and adds sparse soft cloud patches above it. The existing sky, horizon, cloud and sun colors remain unchanged.

The shader is still one opaque background pass. It has no animation/time input, ray marching, cloud meshes, external textures or extra render passes. Two fixed procedural noise scales shape and shade the banks. The normalized view direction is used directly, avoiding an angular wrap seam or a special pole projection.

This changes background pixels only. The sun transform/intensity, scene lighting, island meshes, arena geometry, collision, navigation and combat rules are untouched. This is a stylized sky refinement, not a volumetric weather system or final sky-art approval.

## Review

[Matching-camera gallery](../Validation/CloudSky/index.html) contains before/after overview, outward edge, upper horizon and upper-sky renders at 1920 x 1080. The baseline was captured on the current scene before the shader edit. Both sets use identical camera positions, rotations and FOVs, the same geometry and the same sky material values. These are actual headless Unity renders, not human gameplay recordings.

Scoped shader checks cover compilation/support, finite bounded pixel output, near-wrap/pole continuity and repeatability. The existing daylight fixture checks practice/pause/menu persistence; its original checkpoint capture is preserved, and a new automated solo-practice frame is included in this gallery. A pinned Unity 6000.6.7f1 Windows development build verifies player compilation.

The added shader arithmetic needs measured GPU qualification. No FPS, latency, device-wide compatibility, full-game or combat-readability acceptance is claimed. Human visual review and documented four-contender performance remain open. The public pre-alpha ZIP is unchanged.
