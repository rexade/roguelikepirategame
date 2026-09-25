# Game Context

## User requirements

- Top-down pirate action with ship weapons and abilities that can be combined.
- Start in a harbor with limited resources and a modest ship.
- Fight and pillage at sea; salvage wrecks and collect floating barrels.
- Bring resources home to improve the hub and upgrade the ship.
- Stronger ships can travel farther into dangerous waters.
- A fixed, authored archipelago with distinct islands, inspired by Wind Waker.
- Later, fast travel between discovered hubs; all hubs share progression unlocks.
- Art must be practical for someone with limited animation experience.
- Excellent water is the primary visual requirement, supported by convincing
  lighting and shadows.
- This phase creates architecture and plans, not gameplay code.

References: Battleships (Dota mod), Cat Quest, Dredge, Wind Waker, and the hub
travel aspect of Dark Souls. These are directional references, not specifications
to reproduce their complete systems.

## Proposed design defaults

Owner-approved prototype decisions (2026-09-18) are recorded in
[TARGETS.md](docs/TARGETS.md): Windows single-player, keyboard/mouse first, controller
after the prototype, no paid assets/tools, and simple low-poly art. Performance and
art-effort numbers are evaluation targets, not release/product requirements. The
camera is an adjustable initial target. D07 is resolved: the owner subsequently
instructed the lead to choose; Unity 6 LTS/HDRP is selected for the prototype.

These are technical-lead recommendations, not user-confirmed design requirements.

- Single-player Windows PC first; keyboard/mouse in the prototype (owner-approved).
- A roguelite expedition loop: persistent geography and upgrades, variable
  encounters, loot, and temporary expedition bonuses.
- Dock -> choose equipment -> embark -> explore/fight/salvage -> return or sink
  -> resolve expedition -> upgrade -> embark again.
- Cargo is at risk until safely docked. Sinking loses unbanked cargo and temporary
  bonuses; purchased equipment, unlocked hubs, and banked resources remain.
- Sinking returns the player to the last safely docked harbor with a usable ship.
  No mandatory repair debt that can leave the player unable to embark.
- Docking ends the expedition. Refitting and banking happen in a safe harbor.
- Shared storage and upgrade tiers across hubs for the first version; individual
  hubs have their own discovery state, appearance, and story flags.
- Fast travel requires a later global unlock and an activated destination. It is
  available only while docked, after expedition cargo has been resolved.
- Reach is limited by danger, hull durability, cargo capacity, and authored
  hazards. Fuel, hunger, and crew upkeep are deferred until they improve play.
- Initial combat: one directed weapon and one active ability, supported by hull
  stats and passive equipment. More slots follow only after this feels good.
- Sailing, ship combat, and harbor menus first. Walking around islands, boarding,
  crews, and character combat are outside the first slice.

## Vocabulary

| Term | Meaning |
| --- | --- |
| Campaign | The persistent player save and shared progression |
| Expedition | One departure-to-docking-or-sinking journey |
| Archipelago | Fixed geographic layout of islands, routes, and hubs |
| Region | Authored area with encounter rules and difficulty |
| Hub | A discoverable harbor that accesses campaign-wide progression |
| Bank | Resources safely owned by the campaign |
| Cargo | Resources carried by the active expedition |
| Equipment | Persistent owned weapon, ability, or passive item |
| Loadout | Equipment assigned to the ship's allowed slots |
| Expedition modifier | Temporary bonus that ends with the expedition |
| Unlock | Campaign-wide access to equipment, upgrades, routes, or travel |

## Open decisions

Resolve platform and engine preferences before engine setup. Record actual target
hardware before accepting rendering performance. The following can be tuned after
the visual prototype: combat aiming, expedition length, death penalties, economy
costs, art palette, and how much encounter variation each departure introduces.

Initial scope for a playable slice: one home hub, three small islands, two enemy
archetypes, barrels and wreck salvage, two alternative weapons, one active
ability, one hub upgrade, and one ship upgrade. Validate fast travel afterward with
a second hub; a large map is not needed to prove the system.
