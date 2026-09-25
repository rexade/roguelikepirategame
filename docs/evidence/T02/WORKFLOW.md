# T02 Asset Workflow

Tools: pinned Unity editor, C# authoring, built-in primitives/HDRP materials. No
paid assets, skeletal animation, external model dependency, or custom water solver.

## Reusable Assets

`Assets/_Game/Art/Prototype/StarterCutter.prefab` contains a seven-sided closed
hull, raised deck, mast/spar/sail, cabin, pennant and dummy cannons. Mesh vertices
are split per triangle for flat normals. A matching HDRP WaterExcluder protects
the hull. The route collider is outside the bobbing visual child.

`HarborIsland.prefab` contains a submerged shelf, shore, grassy crown, reusable
rock blocks, and a lookout. The shelf supplies real depth for HDRP refraction and
caustics. Island forms are intentionally simple prototype geometry; no claim of
final art approval is made. Shared materials keep the two variants coherent.

## Ship Variant

1. Duplicate StarterCutter in the Project window, retaining Unity-generated new
   asset metadata for the duplicate.
2. Open the duplicate in Prefab Mode. Change the pennant material and cabin
   material, retaining hull proportions, deck, and exclusion mesh.
3. Adjust the sail/cabin only within the existing silhouette. If changing the
   hull, regenerate a matching exclusion mesh and check intersections in motion.
4. Place the variant under the visual child of the T02 route root; preserve the
   root collider and independent marker. Check daylight, dusk and rough conditions.

Delivered example: CutterVariant.prefab, with amber pennant and stone-colored cabin.

## Island Variant

1. Duplicate HarborIsland, open Prefab Mode, and move the lookout from x=-4 to x=3.
2. Reuse the rock and shore materials. Resize the root to 0.65 for a small islet.
3. Retain the underwater shelf and move shore foam decals to the resized shoreline
   when composing another scene. Foam decals are scene-owned, not in the prefab.
4. Check shallow/deep contrast, light/shadows and the 60-degree camera composition.

Delivered example: IslandVariant.prefab, instantiated northeast of the harbor.

## Effort Accounting

Setup began approximately 15:20 CEST on 2026-09-18, after packet/toolchain reading.
Initial asset generation completed before the first successful build and smoke
capture at 15:27-15:28. That includes code-driven construction of both variants;
it is setup effort, not a timed demonstration of a novice using the editor.
Rendering fixes, inspection, profiling and capture are recorded separately in
REPORT.md. The 2-hour ship / 4-hour island workflow targets require a reviewed
disposition; fast automated asset generation alone does not establish owner
usability. The owner should assess these concrete prefab-editing steps and assets.

A separate repeatability demonstration ran at 15:44 CEST after setup. The Unity
`OceanProofAuthoring.DemonstrateVariants` command loaded the base prefabs, applied
the changes above, and saved/imported the variants in 0.298 seconds (ship) and
0.091 seconds (island). Exact results: variant-effort.json and variants.log.
These are automated authoring times; shared rendered QA is additional and the
owner's editor learning/iteration time is unmeasured. The scene contains the same
island-variant composition; the alternate ship is an inspectable prefab asset.

Shared in-engine rendering adjustment and QA occupied approximately 15:28-16:17
CEST (49 minutes), including lighting, foam, reflection-volume fixes and the
standalone navigation regression. This work benefited both variants and is not
hidden inside the subsecond authoring figures. Final benchmark/overhead/video
verification began at 16:17 and is additional machine time.
Final capture validation completed at approximately 16:40 CEST (23 minutes of
additional machine verification, excluding the earlier comparison runs).
No separate human
modeling/texturing/import/adjustment stopwatch was run for each variant, so the
2-hour/4-hour full-workflow targets are not declared passed by those figures.
