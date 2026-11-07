# Grind to Learn 3.5

This is a fork of this mod from SE `https://steamcommunity.com/sharedfiles/filedetails/?id=3207654953`. 

This is an updated version with some new features.

Grind to Learn is a Space Engineers mod that turns the base game’s research system into a grind-to-learn progression model. This means that the only way for the player to "learn" new blocks to build is by grinding down a existing block in the world.

This mod is player-centric. It follows the normal player progression flow and uses the player’s own identity and research state.

## What this mod does

- Groups many block variants into logical unlock groups.
- Rewrites the progression data used by the game so that a block set becomes learnable through the research system.
- Provides in-game commands for learning and unlearning groups using the public progression API(this is a fix for a large bug).

## Key files

- `Data/SPLIT_PROGRAM.txt` — master normalization and grouping rules for block names.
- `Data/ResearchBlocks.sbc` — generated research definitions used by the game.
- `Data/ResearchGroups.sbc` — generated research groups.
- `Data/Unlockers.sbc` and `Data/UnlockersResearchGroups.sbc` — unlocker definitions used by the mod’s progression flow.
- `Data/regenerate.py` — offline generation script; not runtime code.
- `Data/Scripts/GTL/GTL.cs` — main runtime script for progression, grind logic, command handling, and integration with the game.

## Build the C# script

The runtime project targets .NET Framework 4.6.1 and uses the `Microsoft.NETFramework.ReferenceAssemblies` NuGet package to provide its targeting assemblies during restore. With the .NET SDK installed, build it from the repository root with:

```powershell
dotnet build "Data/Scripts/GTL/GTL.csproj"
```

The project also references Space Engineers assemblies from `C:\Program Files (x86)\Steam\steamapps\common\SpaceEngineers\Bin64`. If the game is installed elsewhere, update those reference paths in `GTL.csproj` before building.

## Block grouping model

`Data/SPLIT_PROGRAM.txt` is the important file. It uses pattern rules to collapse many variants into a single progression family.

For the full maintainer procedure—including the split-rule syntax, adding or splitting a group, generated research-block definitions, and runtime validation—see [BLOCK_RESEARCH_GROUPS.md](./BLOCK_RESEARCH_GROUPS.md).

## Regenerate script

`Data/regenerate.py` is an offline helper. It scans the game content and workshop content, then writes the generated SBCs. It is not run by the game at runtime.

Important rule:

- Do not expect the game to regenerate files while it is running.
- Keep generated SBCs in sync by running the script manually when data or grouping rules are intentionally changed.
- Treat the script as a build step, not as a runtime feature.

### Known issue: generation is inconsistent

`regenerate.py` is intentionally a manual build tool, but it is currently inconsistent in practice. It can regenerate or overwrite content in a way that silently drops or mismatches learned block assignments if the source data or the generated output is not kept in a stable state.

This is a known project issue and is not something the runtime should depend on. The current design assumes that generated SBC files are kept consistent by the maintainer, and future work will standardize or replace the generation flow so that it does not forget or rewrite learned groups unexpectedly.

This is specifically called out because the file is not a runtime mechanism and should not be treated as a source of truth during gameplay. The fix is planned later, but the current guidance is to treat it as a fragile offline generation step that must be kept under control manually.

## Runtime commands

The mod includes in-game chat commands for toggling the player’s research state. This is a temporary fix for the bug mentioned above.

Available behavior is based on the public research API and player identity:

- `/gtl learn <group-or-block>`
- `/gtl unlearn <group-or-block>`

Use the runtime research group ID (for example, `U12`) or a member block's subtype or `Type/Subtype` ID. A block member resolves to its entire research group.

These commands dont require admin power. 

Observed in-game behavior: `/gtl learn` takes effect immediately, while `/gtl unlearn` may not revoke build access until the world is saved and reloaded. Treat a save/reload as required for unlearn changes to take effect.


### Unlearn timing and compatibility

The public progression API can lock or clear research state, but the game may still cache build permissions internally. In testing, Learn took effect immediately, while Unlearn did not take effect until the world was saved and reloaded.

