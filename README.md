# Pirate Roguelite

Architecture and production planning for a single-player, top-down pirate adventure.

Start with a small ship in a harbor. Sail into a fixed archipelago, fight, salvage,
and bring resources home. Improve the shared harbor network and equip a stronger
ship to reach more dangerous waters.

## Project status

**Playable prototype (2026-09-25).** Start `Builds/Game/PiratePrototype.exe`, or open
`Assets/_Game/Scenes/Bootstrap.unity` and press Play. The title menu leads into
the ocean world: refit at Homeward Harbor, sail out, salvage barrels and wrecks,
fight raiders and gunners, loot the wrecks of ships you sink, and dock to bank the
cargo. Sinking loses the hold but never your bank, upgrades or equipment. Five
upgrades in three tracks, a second harbor to claim (Saltmarsh), and fast travel
between claimed harbors. Everything is saved to disk and resumes exactly.

Controls: W/S sail, A/D steer, Space brake, mouse aims, left mouse fires, right
mouse braces (75% damage reduction for 2 s), E salvages / claims / docks,
Escape pauses (and saves).

Task status: T00-T05 accepted; T06/T07 technically accepted (owner visual review
pending); T08 (saves + integrated loop) and T09 (shared hubs + fast travel) are
implemented and in review; T10 (region streaming, encounter variation) and T11
(slice gate) remain. See [the dispatch board](docs/tasks/README.md), the
[T08 report](docs/evidence/T08/REPORT.md), the [T09 report](docs/evidence/T09/REPORT.md)
and [build instructions](docs/BUILD.md).

- [Game direction and terminology](CONTEXT.md)
- [Technical architecture](docs/ARCHITECTURE.md)
- [Invariants and acceptance gates](docs/INVARIANTS.md)
- [Engine and rendering decision](docs/decisions/001-engine-and-rendering.md)
- [Delegation-ready task backlog](docs/TASKS.md)
- [Individual task packets and dispatch board](docs/tasks/README.md)
- [Task, module, and package dependencies](docs/DEPENDENCIES.md)

## Technical direction

Selected for the prototype: Unity 6 LTS, C#, HDRP, and a 3D world with gameplay
constrained to the sea plane. The owner delegated the engine choice; ADR 001 records it.
Use simple low-poly ships and islands, pixel-style textures where useful, and a
high-quality ocean. Validate this combination in a small visual prototype first.
The exact supported editor and package versions will be pinned during setup.

Windows single-player with keyboard/mouse is owner-approved for the prototype.
Controller support is deferred beyond the prototype. See [owner decisions](docs/TARGETS.md).
No multiplayer, mobile, Steam Deck, or release-hardware commitment is implied.

## First milestone

One ship, one island, one harbor, and convincing water viewed from the actual
gameplay camera. Approve appearance, readability, production effort, and measured
performance before expanding into a playable expedition.
