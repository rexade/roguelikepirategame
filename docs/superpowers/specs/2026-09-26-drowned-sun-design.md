# The Drowned Sun: Theme and World Design

Date: 2026-09-26. Status: approved direction (owner chose theme A, "The Drowned Sun",
and asked to "find a theme and go max out on it"). Working title of the game:
**The Drowned Sun** (replaces "Pirate Prototype" in player-facing text; project,
assembly and executable names stay).

## 1. North Star

> A paradise that drowned. You sail the flooded remains of Aurelia, a sun-worshipping
> empire the sea swallowed whole. Its streets, colossi and sun temples lie just
> beneath glass-clear water. The world is the main character; you are a salvager
> relighting its dead sun beacons, one zone farther out each time.

The feeling: *serene beauty that turns ominous with distance.* The starting lagoon
is true paradise; every crossing north takes you into older, darker, stranger ruins.
References: Wind Waker (a drowned kingdom under the sea), Dark Souls (fallen
kingdom, bonfires, distant landmarks that pull you onward, escalating zones),
Spyro (big faceted shapes with sharp edges), Journey (monumental ruins, light).

## 2. Pillars (the trademark)

1. **The empire lies beneath you.** Shallows are windows into the past: plazas,
   stairs, fallen columns and colossi visible through clear water. Deep water between
   zones is dark and empty, which makes each ruin field a revelation.
2. **Colossal and faceted.** Everything monumental is built from large flat facets
   with sharp edges and chamfers: statues, arches, columns, cliffs, palms. Flat
   shading, no smooth blobs, strong silhouettes that read from the gameplay camera.
   The sun-kings have a moai-like face: long head, heavy brow, wedge nose, sun-ray crown.
3. **Gold and turquoise, then decay.** Sun-gold, bleached limestone, turquoise and
   jungle green in the start; farther zones desaturate toward amber haze, then slate
   and storm indigo, with black obsidian coral taking over the stone.
4. **One symbol: the sun disc.** Circles with rays on ruins, beacons, the player's
   sail, the logo and UI ornaments. Enemies (wreckers) fly the eclipse: a black
   disc with a thin gold ring.
5. **World first, UI second.** Diegetic guidance (beacon light pillars, the Sun Gate,
   the drowned road, landmarks on the horizon) over markers; a quiet HUD; lore
   through the environment and one-line inscriptions.

Paper artifacts (sea chart, beacon ledger, title, cards) use an **engraved chart**
style: parchment, ink contours, hatching, gold accents, Roman capitals (Cinzel) over
a book serif (EB Garamond), both SIL Open Font License. This is where hand-drawn
ink art can later drop straight in.

## 3. World: a Fixed, Large-Scale Map

Geography is fixed and authored (INV-10): x is east, z is north, metres. Zones are
separated by real open-water crossings so that each feels like its own place, with
danger rising with distance from the start. Regions are full-width bands so every
point of the sea belongs to exactly one region; each zone's content sits well inside
its band and the crossings straddle the band edges.

| Region id | Band (z) | Zone | Beacon (hub id, dock) | Mood | Threats |
| --- | --- | --- | --- | --- | --- |
| `gilded-shallows` | -200 to 300 | **The Gilded Shallows**: an atoll lagoon (about 260 x 220 m) ringed by a palm cay reef, the only exit north through the Sun Gate | Dawnrest Beacon (`home-harbor`, 0, -28) | Paradise late morning: turquoise shallows, palms, bright sun | two lone wrecker skiffs |
| `broken-causeway` | 300 to 860 | **The Broken Causeway**: a colossal collapsed bridge of arches marching from (100, 430) to (480, 740) on a shallow ridge | Causeway Beacon (`saltmarsh-harbor`, on the bridge platform near 300, 560) | Golden hour: amber haze, long shadows | skiffs and gunboats behind the piers |
| `colossus-deeps` | 860 to 1450 | **The Colossus Deeps**: sun-king colossi standing in dark water around the Kingsfall basin (about 120, 1150), obsidian coral, mist | Kingsfall Beacon (`stormwatch-harbor`, at the greatest colossus' feet) | Overcast, sea mist, rough water | corsairs and gunboats |
| (landmark only) | beyond 1450 | **The Drowned Crown**: ring of broken spires in a permanent storm near (-150, 1700) | none | seen from the Deeps and drawn on the chart as the lure | future |

World limit ("the Veil"): x -300 to 700, z -200 to 1450, enforced by invisible walls
with thickening fog and a warning; the chart draws it as a hatched border.

Crossings: **The Glass Reach** (Sun Gate at 0, 152 -> causeway, about 300 m, deep
sapphire water; the **Offering Hand**, a stone hand holding a sun disc, rises near
70, 300) and **The Sunken Road** (causeway north end -> Kingsfall, about 500 m, a
paved road on a drowned ridge visible below the water shows the way into the mist).
At the base speed of 8 m/s a crossing takes 40-65 s; home to Kingsfall is about 1.2 km.

Beacon towers carry tall light pillars once lit. Landmarks (Sun Gate, Dawn Watcher
head, Offering Hand, causeway arches, colossi, Crown) and seabed shapes are always
loaded so the horizon never pops; region scenes stream the collision, small ruins,
wreck hulls and pedestals (load margin 150 m, unload 200 m).

## 4. Zone Content

- **Ruin field** in shallows: sunken plaza, stairs descending into the water,
  standing and toppled columns, an arch, a sun disc; seabed shelves 3-6 m deep so
  the ruins are visible through the water; shelves slope to 30 m and then there is
  no seabed at all (deep water shows the scattering colour).
- **Landmark** per zone: the **Dawn Watcher** (half-sunken colossus head gazing east
  in the lagoon) and the **Sun Gate** (zone 0), the arches (zone 1), the colossi (zone 2).
- **Islets**: terraced limestone and sand cays with faceted palms (zones 0-1),
  obsidian crags and coral (zone 2).
- **Salvage** (replaces floating barrels): **relics** (gold sun idols on a ruin
  pedestal that breaks the surface, with a glint; give bronze) and **wrecks** (cargo
  at a wreck hull on a reef; give timber, sometimes bronze). Few, placed with intent:
  zone 0: 4 relics + 2 wrecks; zone 1: 5 + 3; zone 2: 5 + 3. Sunk enemies still
  leave floating debris to salvage. Entity definition `relic` replaces `barrel`;
  resource IDs stay `wood`/`iron` and display as **Timber** and **Bronze**.
- **Encounters** are fixed rosters per site (the same wreckers wait at the same
  ruins every voyage, Dark Souls style); one site per outer zone still draws from the
  zone's weighted table so voyages are not identical.
- **Lore lines**: the first time in a session the ship comes near a landmark, an
  inscription banner shows its name and one line, e.g. *"The Dawn Watcher. His gaze
  still turned to the dawn that drowned him."*

## 5. Kit (procedural, code-authored, low-poly)

All meshes are generated by editor code into mesh assets and prefabs, with hard
facet normals: chamfered limestone blocks, octagonal columns (whole, broken,
toppled), arches and piers, stepped stairs, plaza slabs, sun discs (whole/cracked),
the colossus head and standing colossus, the offering hand, obelisk, Sun Gate
pillars, faceted palms (segmented trunk, blade fronds), terraced islets, rocks,
obsidian coral spikes, beacon tower (stepped base, brazier, sun disc, flame, light
pillar), relic idol and pedestal, wreck hull and floating debris, the Crown
silhouette, and heightfield seabed shelves. Materials (HDRP Lit/Unlit, few):
limestone light/dark, sun-gold (metallic), jungle green, beach sand, seabed sand,
obsidian, weathered wood, sail cream with the sun emblem, wrecker sails with the
eclipse, flame, pillar light, glint. No paid or downloaded art except the two OFL fonts.

## 6. Atmosphere, Water, Camera

- **Zone atmosphere** replaces random voyage weather: each zone has a profile (sun
  angle/colour/intensity, gradient sky, exposure, fog colour and distance, water
  refraction/scattering colour and absorption distance, wind/swell, colour grading,
  bloom, ambience mix). The game blends between the profiles along z by the ship's
  position, so crossings visibly shift the mood (paradise -> golden -> mist).
  Presentation only (INV-14): rules, hit boxes and aim never read it.
- **Water**: very clear shallows (absorption about 12 m) in the lagoon; the HDRP
  water surface stays the single approved ocean; caustics on.
- **Camera** (D05 adjusted with this evidence): three blended framings.
  *Voyage* (no threat): low behind-the-ship view (pitch about 28 degrees) whose yaw
  follows the heading, showing the horizon, landmarks and pillars.
  *Tactical* (an enemy within 60 m or shots in the last 4 s): the D05 60-degree
  top-down framing, yaw frozen while it lasts so the view never spins mid-fight.
  *Approach* (within reach of a berth or salvage): 45 degrees, closer.
  *Harbor* (docked): a slow scenic shot of the beacon behind the ledger.
  All transitions ease over about a second.

## 7. Presentation and UI

- Title: **THE DROWNED SUN** (sun disc half-sunk in a horizon line), menu on a
  parchment card over a slow camera drift across the lagoon ruins.
- HUD: small parchment tags with ink text and gold accents; hull and hold as ink
  meters; zone name in Roman capitals with the zone's mood line.
- Beacon screen: the beacon's ledger (parchment, engraved headings); upgrades keep
  their IDs but get themed names (Beacon Storehouse, Shipwright's Slip, Reinforced
  Hull, Bronze-bound Guns, Beacon Paths = fast travel between lit beacons).
- Sea chart: engraved map (hatched sea, rippled ink coastlines, zone names in
  capitals, the Veil, the drowned road as a dotted line, the Crown as an ominous
  silhouette, beacons as sun discs, lit ones gilded).
- Cards: "THE SEA CLAIMS YOU" on sinking, "BEACON RELIT" on activation.
- Sound: per-zone ambience (gulls and gentle surf in the lagoon, warm wind in the
  Causeway, low wind and distant groans in the Deeps) and a warm chord when a
  beacon is relit; synthesized in code as today.

## 8. Save Compatibility

Hub, resource, upgrade and equipment IDs are unchanged, so banked progress, upgrades
and lit beacons carry over. A voyage saved at sea in the old geography cannot be
restored: when a loaded voyage does not match the current world (unknown region or
definition, or authored salvage that differs from the current sites), the load
resolves it as lost at sea: the campaign returns to its last safe beacon, the
voyage's cargo is lost, and a card explains that the sea has changed. The file is
only rewritten by the next normal commit.

## 9. Delivery: Spec -> Playground -> Integrate

1. **Playground** (`Scenes/Playground/DrownedSun.unity`, generated): a ~250 m
   vignette of the Gilded Shallows: lagoon shelf dropping to deep water, the Sunken
   Forum (plaza, stairs, columns), the Dawn Watcher head, a sun-disc arch, a palm
   islet with the lit Dawnrest Beacon, a relic on its pedestal, a wreck, the Sun Gate,
   the player ship and one wrecker, zone-0 atmosphere, and the three camera
   framings; plus one shot with the zone-2 profile. It must pass the DoD below on
   screenshots before integration.
2. **Integrate**: rebuild the three zones and crossings on the large map; zone
   atmosphere blending; beacon visuals and relighting; themed salvage and ships; UI
   reskin (HUD, ledger, chart, cards, title); zone ambience; lore lines; save
   compatibility; update tests to read positions from content instead of hard-coded
   coordinates; re-run the capture tour and benchmark.

### Playground Definition of Done

- From the gameplay camera, the sunken plaza and stairs are clearly visible through
  the water, and deep water reads as a distinct darker blue.
- The colossus head, arch and palms read as one family: faceted, sharp, monumental.
- The lit beacon is the brightest, most inviting thing on screen; its pillar is
  visible from 150 m away in the voyage view.
- The ship, a wrecker and a relic stay readable against the ruins (silhouette,
  value contrast).
- Nothing looks like a default primitive (no bare cubes, spheres or cylinders).
- Frame time on the reference PC stays under the D02 targets.

## 10. Out of Scope for This Pass

Boss or playable Drowned Crown, fog-of-war chart persistence, cross-voyage
"bloodstain" cargo recovery, controller support, new rules beyond display names and
fixed rosters. These are candidates for later passes.
