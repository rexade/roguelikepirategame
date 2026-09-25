# Pirate Roguelite

Architecture and production planning for a single-player, top-down pirate adventure.

Start with a small ship in a harbor. Sail into a fixed archipelago, fight, salvage,
and bring resources home. Improve the shared harbor network and equip a stronger
ship to reach more dangerous waters.

## Project status

T01 engine bootstrap is implemented and accepted: a Windows build, bootstrap UI,
and HDRP water test scene exist. T02 is owner-approved for now with documented
visual/workflow limitations; its technical measurement review passed. T03's shared
rules/contracts are implemented and accepted. T04-r2 ship controls are accepted
and T04-R1 is closed. T05-r2 is accepted and T05-R1 is closed;
T06 needs changes following Lead review; T07 is ready for delegation;
integrated gameplay is not implemented. See [build instructions](docs/BUILD.md).

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
