# ADR 001: Engine and Rendering

Date: 2026-09-18

Status: Accepted for prototype on 2026-09-18 under explicit owner delegation:
"you choose engine and create tasks". The lead selects Unity 6 LTS with HDRP.
This later instruction resolves the earlier D07 hold; the earlier proposal alone
was not authoritative. Production suitability still requires T02 review.
Exact supported editor/package versions are pinned and verified in T01.

## Decision

Use a supported Unity 6 LTS editor, C#, and its compatible HDRP package. Pin exact
versions during project setup, after checking the chosen release's documentation.
Render a 3D world with a fixed, elevated perspective camera and movement on XZ.
Use simple low-poly geometry with restrained textures and procedural animation.

The ocean is the main visual investment. Begin with HDRP's existing water system;
do not build a fluid solver or an ocean renderer from scratch.

## Why

Pure 2D can produce beautiful water. It would still require deliberately authored
depth, reflections, foam, and shadow interactions for this particular look. A 3D
scene provides real spatial relationships between ships, shorelines, lights, and
water while leaving the gameplay as approachable as a top-down 2D game.

Unity's feature comparison lists water shading, wave simulation, local foam, and
surface deformation in HDRP, without those built-in water features in URP. That
directly fits the project's most important visual requirement. This is a choice
about available implementation support, not a claim that URP cannot render water.

## Alternatives

| Option | Benefit | Project-specific cost | Decision |
| --- | --- | --- | --- |
| Pure 2D | Easy sprite production and simple camera | More custom work to integrate the desired water and spatial lighting | Not the baseline |
| Godot 4, 3D Forward+ | Open-source engine with advanced 3D rendering | Need to evaluate a water implementation and its integration separately | Alternative if engine preference changes |
| Unity URP, 3D | More flexible performance targets | Need a validated third-party or custom water solution | Main fallback for hardware constraints |
| Unity HDRP, 3D | Existing water system and substantial lighting support | Higher rendering cost and configuration complexity | Selected for prototype under explicit owner delegation |

## Art and camera consequences

- Prefer low-poly ships over directional ship sprite sheets. Rotating a simple
  model avoids redrawing headings and gives natural shadows.
- Pixel-style textures and UI remain possible. Strict pixel-perfect rendering is
  not a requirement and should not compromise water readability.
- Animate bobbing, sail motion, recoil, and impacts procedurally. No character
  skeletons are needed for the first slice.
- Start with an elevated perspective camera around 55-65 degrees downward from
  the horizon. Tune distance and field of view at actual gameplay scale.
- Keep ocean detail readable from that view. A beautiful low-angle ocean demo is
  insufficient evidence for this game.
- Stylize water color, wave scale, foam, and roughness so simple geometry belongs
  in the scene. Photoreal water beneath unrelated pixel assets is a cohesion risk.

## Acceptance gate

T02 in the backlog must demonstrate shoreline foam, ship wakes, shallow/deep water
contrast, stable shadows, readable combat markers, and coherent simple geometry.
Test daylight, dusk, and rough water in a standalone build on recorded hardware.
Adopt 1080p/60 FPS as a provisional benchmark, with no ray tracing requirement.
Record frame-time percentiles, CPU/GPU timing, memory, and traversal stalls.

If a target is missed, report measurements/bottlenecks and review options with the
owner. Do not automatically reduce visual quality or change renderer. Workflow
overruns trigger analysis/revision, not automatic game failure. Camera values are
an adjustable initial baseline. A renderer comparison may be proposed after review.
Do not create a pipeline abstraction or maintain two rendering implementations.
An engine/pipeline switch requires updating this decision and dependent tasks.

## Evidence and limits

Reviewed 2026-09-18. These sources establish available engine features, not this
game's achieved visual quality or performance. Version-specific support must be
checked again against the exact pinned packages.

- [Unity 6 render-pipeline feature comparison](https://docs.unity3d.com/6000.0/Documentation/Manual/render-pipelines-feature-comparison.html)
- [Unity lighting and environment examples for HDRP](https://unity.com/blog/lighting-and-environments-hdrp-updates-unity-6)
- [Godot renderer overview](https://docs.godotengine.org/en/stable/tutorials/rendering/renderers.html)
