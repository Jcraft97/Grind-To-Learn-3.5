# Grind to Learn

Grind to Learn is a private Space Engineers mod that turns the base game’s research system into a grind-to-learn progression model. This means that the only way for the player to "learn" new blocks to build is by grinding down a existing block in the world.

This mod is intentionally private and player-centric. It does not assume a server admin override model; instead it follows the normal player progression flow and uses the player’s own identity and research state.

## What this mod does

- Groups many block variants into logical research buckets.
- Rewrites the progression data used by the game so that a block set becomes learnable through the research system.
- Provides in-game commands for learning and forgetting groups using the public progression API.
- Keeps the mod compatible with the public Space Engineers mod API surface and avoids prohibited game internals.

## Key files

- `Data/SPLIT_PROGRAM.txt` — master normalization and grouping rules for block names.
- `Data/ResearchBlocks.sbc` — generated research definitions used by the game.
- `Data/ResearchGroups.sbc` — generated research groups.
- `Data/Unlockers.sbc` and `Data/UnlockersResearchGroups.sbc` — unlocker definitions used by the mod’s progression flow.
- `Data/regenerate.py` — offline generation script; not runtime code.
- `Data/Scripts/GTL/GTL.cs` — main runtime script for progression, grind logic, command handling, and integration with the game.

## Why Prosperity grouping had to be fixed

The Prosperity update introduced new block variants that did not match the mod’s existing split/normalization rules. The main examples were:

- H2 tank variants not joining the hydrogen tank group.
- Closed and open sloped cockpits not mapping into the cockpit group.
- Cable runs and server racks not being assigned to the decorative/stairs-style grouping.
- Light variants and other normalized block families not being reduced into their expected bucket.

The fix was not a game-side bug; it was a rule mismatch in the mod’s normalization logic. The mod groups blocks by normalized names, not by exact literal subtype names, so new variants must be folded into the same category as the older blocks they logically belong to.

## Block grouping model

`Data/SPLIT_PROGRAM.txt` is the important file. It uses pattern rules to collapse many variants into a single progression family. Examples include:

- `Cockpit` families
- `HydrogenTank` families
- `Light` families
- `CableRun` families
- `ServerRack` and decorative cluster entries

This is deliberate: Space Engineers research buckets are broader than arbitrary block subtypes, and the mod intentionally groups them to keep progression sensible and consistent with the base game’s own design.

## Regenerate script

`Data/regenerate.py` is an offline helper. It scans the game content and workshop content, then writes the generated SBCs. It is not run by the game at runtime.

Important rule:

- Do not expect the game to regenerate files while it is running.
- Keep generated SBCs in sync by running the script manually when data or grouping rules are intentionally changed.
- Treat the script as a build step, not as a runtime feature.

## Runtime commands

The mod includes in-game chat commands for toggling the player’s research state without requiring admin-only permissions. The intent is to align with the player’s own progression and the default leveling/research model used by Space Engineers.

Available behavior is based on the public research API and player identity:

- `/gtl learn <group-or-block>`
- `/gtl forget <group-or-block>`

These commands are not meant to be privileged. The mod is private and should operate on the current player’s research state rather than a server-admin override path.

## Important constraints

### Public API only

The game forbids direct access to `MySessionComponentResearch` in mod scripts. The mod must remain compatible with the public API surface. This means:

- No direct use of prohibited internal types.
- Prefer the research and player unlock/lock methods that are officially available.
- Avoid assumptions that internal state is directly readable or writable from a mod script.

### No admin-only gating

This mod is for private use and should not be restricted to server admins. The in-game commands should work for the player who owns the progression state in the current session.

### Expectation management for forget

The public progression API can lock or clear research state, but the game may still cache build permissions internally. This means a “forget” action can be limited by the game’s progression semantics or cached state, even when the call itself succeeds. The compatibility fix is to stay within the supported API and keep the flow aligned with normal Space Engineers progression behavior.

## Recommended workflow for updates

When Space Engineers updates and adds new content:

1. Compare new block names against the existing split patterns in `Data/SPLIT_PROGRAM.txt`.
2. Add or normalize patterns for the new variants so they match the intended research group.
3. Regenerate the research output if needed.
4. Recheck the grouping of the new content against base-game definitions and existing buckets.
5. Confirm mod commands remain functional and keep to the public API surface.

## Compatibility notes

- Do not rely on runtime regeneration.
- Keep the mod private and player-centered.
- Avoid forced admin checks.
- Keep grouping logic rule-based rather than hard-coded to a tiny list of known blocks.
- Use the public progression API; do not attempt to bypass game restrictions through prohibited internals.

## Maintainer notes

This project is intentionally narrow in scope: stay compatible with the game’s research model, keep the grouping logic consistent with the base game, and preserve the private nature of the mod. The generated data files are part of the build output, but the real logic lives in the split rules and mod script.
