# AGENTS.md

This repository is the Grind to Learn 3.5 mod for Space Engineers. Use the rules in this file when making changes or reviewing work related to the mod.

Always document info in the `README` and the `AGENTS` files, do not override preexisting info with approval.

## Mission

Keep the mod aligned with the base game’s progression model, maintain correct research grouping for new block variants, and preserve the non-admin design of the mod.

## Important project facts

- `Data/SPLIT_PROGRAM.txt` is the source of truth for how block names are reduced into research groups.
- `Data/regenerate.py` is a manual generator and should not be treated as runtime code.
- `Data/Scripts/GTL/GTL.cs` is the runtime logic for the mod.
- `Data/Scripts/GTL/GTL.csproj` targets .NET Framework 4.6.1 and includes `Microsoft.NETFramework.ReferenceAssemblies` so the target framework can be built with the .NET SDK.
- The mod uses the public Space Engineers progression API; do not reach through prohibited internal APIs.

To build the C# project from the repository root, run `dotnet build "Data/Scripts/GTL/GTL.csproj"`. The Space Engineers assembly references in the project use the default Steam installation path and must be adjusted if the game is installed elsewhere.

### The game files that we are overriding:

`C:\Program Files (x86)\Steam\steamapps\common\SpaceEngineers\Content\Data`

### Game modding docs

Official docs to reference can be found here:

- `https://www.spaceengineersgame.com/modding-guides/`

Other docs can be found here:

- `https://spaceengineers.wiki.gg/wiki/Modding`


### Log files

Ingame debug log files can be found here: `C:\Users\SYSTEM_NAME\AppData\Roaming\SpaceEngineers`

Note: `SYSTEM_NAME` is a placeholder and needs to be populated with the name of the users system.

## Core rules

### 1. Do not rely on runtime regeneration

`regenerate.py` is not run by the game. It is an offline generator used to create SBC outputs. If a change is made to the split program or generated data, update the generated files intentionally and keep the workflow reproducible.

This tool is currently known to be inconsistent. It can rewrite or drop research state in ways that make learned block group assignments appear to vanish or drift if the generated output is not kept synchronized. This is a known maintenance problem and should be treated as a future cleanup item rather than a normal runtime dependency.

For now, treat `regenerate.py` as a fragile manual build step only. Do not assume it is safe to run without verifying the generated output. A future fix will standardize the generation pipeline so it stops silently forgetting learned groups.

### 2. Stay within the public API surface

The Space Engineers modding environment rejects direct access to `MySessionComponentResearch` and similar prohibited members. This is non-negotiable. Any implementation must stay compatible with the whitelisted API list.

### 3. Keep the mod layer accessible

Do not gate behavior behind admin-only checks. The mod should operate on the current player’s own progression state. The chat commands should not require server admin rights.

### 4. Treat unlearn behavior as a compatibility constraint

The “unlearn” action may be limited by the game’s progression cache or other public API semantics. The correct approach is to use the supported API and keep the behavior aligned with normal game progression, not to force prohibited internals.

Observed in-game behavior: `/gtl learn` takes effect immediately, while `/gtl unlearn` does not fully revoke build access until the world is saved and reloaded. Treat save/reload as required for unlearn changes to take effect, and do not bypass the public API to force an immediate lock.

Chat commands accept a runtime research group ID (such as `U12`) or a member block subtype or `Type/Subtype` ID. Resolving a member block applies the action to its whole research group.

## Files you may need to touch

- `Data/SPLIT_PROGRAM.txt` — grouping and normalization logic.
- `Data/ResearchBlocks.sbc` — generated research block definitions.
- `Data/ResearchGroups.sbc` — generated research group definitions.
- `Data/Unlockers.sbc` — generated unlocker blocks.
- `Data/UnlockersResearchGroups.sbc` — generated unlocker-to-group definitions.
- `Data/Scripts/GTL/GTL.cs` — runtime commands and progression logic.
- `Data/regenerate.py` — generation script, not runtime logic.


## Reminders for future agents

- Do not assume the game is regenerating content at runtime.
- Do not rely on direct internal research state access.
- Do not make the mod admin-only.
- Always document info in the `README` and the `AGENTS` files, do not override preexisting info with approval.

# Git commits

After completing the requested work, commit the task-related changes by default; do not wait for the user to ask for a commit. Preserve pre-existing unrelated work by leaving it unstaged and out of the commit. Then push the commit. If a commit cannot be created, report the specific blocker and the uncommitted state.

Add this trailer to commits:

```text
Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
```
