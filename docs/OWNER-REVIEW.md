# Owner Review Checklist (2026-09-25 session)

The integrated game (T08-T11) is built and automatically verified; nothing in it
has been played by a person yet. This page lists what to try and what to decide.

## Play it

- Run `Builds/Game/PiratePrototype.exe` (Windows, windowed 1920x1080 by default).
- Title: **New campaign** (or **Continue voyage** once a save exists).
- Controls: W/S sail, A/D steer, Space brake, mouse aims, left mouse fires,
  right mouse braces, E salvages / claims a harbor / docks, M opens the sea chart,
  Esc pauses (and saves).
- Saves: `%USERPROFILE%/AppData/LocalLow/PiratePrototype/Pirate Prototype/Saves`.
  Delete that folder to start completely fresh. Starting a new campaign over an
  existing one keeps the old files under a `*.replaced-<date>.json` name.

Suggested route for a first session (about 15 minutes):

1. Salvage the two barrels north of Homeward Harbor, then dock (E in the berth
   ring) to bank them; buy the Harbor Storehouse.
2. Sink the ship at the first encounter (brace with right mouse when hit), loot
   its wreck, and return. Try sinking on purpose once: cargo is lost, the bank and
   upgrades stay, and the next departure is free.
3. Sail north-east to Saltmarsh Harbor and raise your flag (E in its berth).
4. Buy Navigator's Charts and fast-travel between harbors from the harbor screen.
5. Cross into Galewater Reach (north of Saltmarsh): corsairs, iron-rich salvage,
   Stormwatch Harbor. Quit mid-voyage and Continue: everything resumes where saved.

## Decisions needed

| Topic | Question |
| --- | --- |
| GATE-01 visuals | Do the integrated harbor villages, island kit, HUD, harbor screen, dusk and rough-sea looks meet the "approved for now" bar? Screenshots: `docs/evidence/T10/captures`, `docs/evidence/T08/captures-audio-weather`. |
| Render setting | HDRP `maximumWaterDecalCount` was raised 48 -> 96 so shore foam is not culled in the larger world (T11 report). Accept, or prefer less foam? |
| Balance | Escorted gunner pairs and corsairs are dangerous for a starting cutter; upgrade costs are 5-12 wood / 0-4 iron. Too hard, too grindy? All numbers live in `WorldAuthoring` (`AuthorRules`, `AuthorCombat`, `AuthorGalewater`). |
| Audio | All sound is synthesized in code (no assets). Keep, tune levels, or replace with recorded free sounds later? |
| Scope | Content now exceeds the original slice (2 regions, 3 harbors, 3 enemy types, 5 upgrades). Keep growing content, or polish feel (controls, AI, art) first? |
| Process | T08-T11 were implemented and verified by the Lead (Claude) rather than delegated; they are marked review, not done, pending your play/visual review. |

## Known gaps

- No controller support (deferred by D01), no settings menu (volume, resolution).
- Enemy AI is simple; ships can bump islands. No map fog of war.
- The T01 standalone keyboard test was updated for the new title menu but was not
  re-run (it needs a focused test player).
