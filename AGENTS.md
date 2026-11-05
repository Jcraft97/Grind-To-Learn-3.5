# AGENTS.md

This repository is the private Grind to Learn mod for Space Engineers. Use the rules in this file when making changes or reviewing work related to the mod.

## Mission

Keep the mod aligned with the base game’s progression model, maintain correct research grouping for new block variants, and preserve the private, non-admin design of the mod.

## Important project facts

- `Data/SPLIT_PROGRAM.txt` is the source of truth for how block names are reduced into research groups.
- `Data/regenerate.py` is a manual generator and should not be treated as runtime code.
- `Data/Scripts/GTL/GTL.cs` is the runtime logic for the mod.
- The mod uses the public Space Engineers progression API; do not reach through prohibited internal APIs.
- The mod is meant to be private and player-owned, not server-admin-only.

## The game files that we are overriding:

`C:\Program Files (x86)\Steam\steamapps\common\SpaceEngineers\Content\Data`

## Game modding docs

Official docs to reference can be found here:

- `https://www.spaceengineersgame.com/modding-guides/`

Other docs can be found here:

- `https://spaceengineers.wiki.gg/wiki/Modding`


## Log files

Ingame debug log files can be found here: `C:\Users\SYSTEM_NAME\AppData\Roaming\SpaceEngineers`

Note: `SYSTEM_NAME` is a placeholder and needs to be populated with the name of the users system.

## Core rules

### 1. Keep grouping rule-based

When new blocks appear after a game update, the likely fix is in `Data/SPLIT_PROGRAM.txt`, not in hand-written block-by-block exceptions. The normalization patterns are broad on purpose and are designed to map new subtypes into the expected research groups.

Examples of prior fixes:

- H2 tank variants
- closed/open sloped cockpit variants
- server rack and cable run grouping
- light-family normalization

Do not add ad hoc one-off exceptions unless the pattern clearly cannot be expressed in the split language.

### 2. Do not rely on runtime regeneration

`regenerate.py` is not run by the game. It is an offline generator used to create SBC outputs. If a change is made to the split program or generated data, update the generated files intentionally and keep the workflow reproducible.

This tool is currently known to be inconsistent. It can rewrite or drop research state in ways that make learned block group assignments appear to vanish or drift if the generated output is not kept synchronized. This is a known maintenance problem and should be treated as a future cleanup item rather than a normal runtime dependency.

For now, treat `regenerate.py` as a fragile manual build step only. Do not assume it is safe to run without verifying the generated output. A future fix will standardize the generation pipeline so it stops silently forgetting learned groups.

### 3. Stay within the public API surface

The Space Engineers modding environment rejects direct access to `MySessionComponentResearch` and similar prohibited members. This is non-negotiable. Any implementation must stay compatible with the whitelisted API list.

### 4. Keep the mod private and player accessible

Do not gate behavior behind admin-only checks. The mod should operate on the current player’s own progression state. The chat commands should not require server admin rights.

### 5. Treat unlearn behavior as a compatibility constraint

The “unlearn” action may be limited by the game’s progression cache or other public API semantics. The correct approach is to use the supported API and keep the behavior aligned with normal game progression, not to force prohibited internals.

Use only `/gtl unlearn` to revoke a research unlock.

Observed in-game behavior: `/gtl learn` takes effect immediately, while `/gtl unlearn` does not fully revoke build access until the world is saved and reloaded. Treat save/reload as required for unlearn changes to take effect, and do not bypass the public API to force an immediate lock.

## Files you may need to touch

- `Data/SPLIT_PROGRAM.txt` — grouping and normalization logic.
- `Data/ResearchBlocks.sbc` — generated research block definitions.
- `Data/ResearchGroups.sbc` — generated research group definitions.
- `Data/Unlockers.sbc` — generated unlocker blocks.
- `Data/UnlockersResearchGroups.sbc` — generated unlocker-to-group definitions.
- `Data/Scripts/GTL/GTL.cs` — runtime commands and progression logic.
- `Data/regenerate.py` — generation script, not runtime logic.

## Change guidance

When updating the mod:

1. Confirm the issue is one of rule grouping, not a missing runtime hook.
2. Adjust the split program patterns first.
3. Check whether generated output needs to be refreshed.
4. Keep the runtime logic within public API constraints.
5. Preserve the intended private-player behavior.

## Expected outcomes for valid work

- New block variants map into the correct research groups.
- The mod remains compatible with the game’s allowed API list.
- Runtime commands work for the active player without admin restrictions.
- The generated outputs stay internally consistent with the design of the mod.

## Reminders for future agents

- Do not assume the game is regenerating content at runtime.
- Do not rely on direct internal research state access.
- Do not make the mod admin-only.
- Do not hardcode new blocks without checking the split normalization rules first.
- Prefer rule-based fixes over brittle one-off block exceptions.
- Always document info in the `README` and the `AGENTS` files, do not override preexisting info with approval.

# Git commits

After completing the requested work, commit the task-related changes by default; do not wait for the user to ask for a commit. Preserve pre-existing unrelated work by leaving it unstaged and out of the commit. Then push the commit. If a commit cannot be created, report the specific blocker and the uncommitted state.

Add this trailer to commits:

```text
Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
```