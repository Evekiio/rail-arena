# Sky Gardens lighting build diagnosis

The 2026-10-29 Windows playtest build timed out after 600 seconds while compiling `Universal Render Pipeline/Lit` fragment variants. It did not produce a verified player.

The prior successful Sky Gardens build retained 96 ForwardLit fragment variants per DirectX API after stripping. The integrated baked/mixed-lighting build retained 1,024 per API (2,048 total). The timed-out run reported 203 of those 2,048 ready at 575 seconds. These figures are build-log observations, not runtime performance measurements.

The build still targets both DirectX 11 and DirectX 12. The variant increase accompanies enabling lightmapping and mixed lighting; this is a correlation, not proof that every retained variant is required. Do not remove shader variants or a graphics API without verifying the shipped materials, baked lighting, shadows, and hardware compatibility.

Evidence: `build.log`, `gi-playtest-build.log`, `gi-main-integrated-verify.log`, and `gi-integrated-traversal.xml` in this directory. The original scene is preserved as `main-before-gi.unity`.

Next: run a longer bounded build, or first profile the retained shader keywords and introduce evidence-backed stripping with player-side rendering checks. Review the built game's lighting and traversal in a visible session only after the required window permission.
