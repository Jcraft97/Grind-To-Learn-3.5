# Block research groups: a simple maintainer guide

This document explains how Grind to Learn decides which blocks belong to the same research group.

If you need to change research grouping, the first place to look is [`Data/SPLIT_PROGRAM.txt`](./Data/SPLIT_PROGRAM.txt). Most fixes belong there, not in a one-off exception list.

## The short version

1. Find the exact block ID and subtype you want to adjust.
2. Check the existing rules in [`Data/SPLIT_PROGRAM.txt`](./Data/SPLIT_PROGRAM.txt).
3. Prefer a family rule over a single-block exception.
4. Confirm the block also exists in the research definition files.
5. Test in-game and check the log before considering the change finished.

## What each file is for

| File | Purpose |
|---|---|
| [`Data/SPLIT_PROGRAM.txt`](./Data/SPLIT_PROGRAM.txt) | Main source of truth. It rewrites block IDs into group keys and decides which blocks end up together. |
| [`Data/ResearchBlocks.sbc`](./Data/ResearchBlocks.sbc) | Base-game research-block definitions. These define which blocks are researchable. |
| [`Data/Mods/`](./Data/Mods) | Mod research-block definitions, organized by workshop ID. |
| [`Data/ResearchGroups.sbc`](./Data/ResearchGroups.sbc) | Placeholder group definitions. The runtime clears and fills these at runtime. |
| [`Data/UnlockersResearchGroups.sbc`](./Data/UnlockersResearchGroups.sbc) | Placeholder unlocker-to-group mappings for `U0`, `U1`, etc. |
| [`Data/Unlockers.sbc`](./Data/Unlockers.sbc) | Unlocker block definitions tied to those groups. |
| [`Data/regenerate.py`](./Data/regenerate.py) | Offline generator for research files. It is not part of runtime behavior and is known to be fragile. |

For routine group updates, start with `SPLIT_PROGRAM.txt`.

## How grouping works

The runtime does this in order:

1. Collects block definition IDs.
2. Converts them to strings.
3. Applies the rules from `SPLIT_PROGRAM.txt` one by one.
4. Rewrites each block's current key as it goes.
5. Groups blocks with the same final key.

A rule can do two different things:

- `MATCH`: normalize names but keep the block in the pool
- `GROUP`: pick matching blocks out of the pool and assign them to an explicit group immediately

This is the core idea behind the file:

- normalize a family until the names line up
- then let the remaining matching blocks become one research group

Example:

```text
M OxygenGenerator Lab=
```

This rule means: if a block key contains `OxygenGenerator`, remove `Lab` from it. The block stays in the current set and may later join other blocks with the same normalized key.

Example of an explicit group:

```text
@ ^ArmorCenter$ ^ArmorCorner$ ^ArmorSide$
```

This creates a single group from the three matching keys and removes them from later processing.

## The rule language, in plain English

Each non-empty line is a command plus arguments. `#` starts a comment.

| Command | What it does |
|---|---|
| `DEFINE alias command` | Creates a shorthand alias. This file defines `M` as `MATCH` and `@` as `GROUP`. |
| `regex=replacement` | Rewrites the current key for every block. |
| `MATCH selector regex=replacement` | Only rewrites blocks that match the selector. They remain in the current set. |
| `GROUP selector ...` | Creates a group from matching blocks and removes them from later rules. |
| `IFMOD mod-name-regex` | Only processes the following lines if the active mod name matches. `IFMOD .` means “match any non-empty mod name.” |
| `IGNORE selector ...` | Removes matching blocks from later grouping. |
| `DISABLE selector ...` | Makes matching blocks non-public. |
| `UNLOCK selector ...` | Marks matching blocks as unlocked for all players. |
| `CHANCE number selector ...` | Overrides spawn chance for matching blocks. |
| `DUMP` | Writes the current keys and IDs to the log for debugging. |

### Important syntax rules

- Separate tokens with one ordinary space.
- One rule = one line.
- A replacement must contain exactly one `=`.
- Replacements run in order, and later rules see the already-rewritten key.
- Regexes are not automatically anchored. Use `^` and `$` when you need the whole key to match.
- `MATCH` keeps blocks in the set. `GROUP` removes them.
- `GROUP` happens exactly where it appears in the file. Put it after the name has been normalized the way you want.

## The easiest way to add a block variant

When a block is in the wrong group:

1. Find the exact block definition ID and subtype.
2. Review the relevant normalization rules in [`Data/SPLIT_PROGRAM.txt`](./Data/SPLIT_PROGRAM.txt).
3. Use a temporary `DUMP` before the relevant `GROUP` to inspect the transformed key in the log.
4. Adjust the narrowest family rule instead of creating a one-off exception.
5. Keep selectors precise so unrelated families do not merge.
6. Check that the block has a research-block definition.
7. Test in-game and remove the temporary debug rule.

Good fix:

- “This family has a naming pattern; normalize the family word once so all variants land together.”

Bad fix:

- “Add one hardcoded rule for this exact subtype because it is the only wrong one right now.”

## If you need to split one group into multiple groups

First find the rule that is collapsing the blocks together.

If you want them separated:

- keep the distinguishing part of their names, or
- add a more specific `GROUP` before a broader rule removes them

Important: once a block matches a `GROUP`, it is removed from the remaining pipeline. So placement matters.

If you want to merge groups:

- make the keys match before the final grouping step
- also check any earlier explicit `GROUP` rules that could already have taken those blocks out of the set

## If you are adding a new family

Use a `MATCH` rule when the family can share a normalized key. Use an explicit `GROUP` only when the family needs to be separated at a specific point in the process.

Before adding the rule:

1. Check whether the existing general rules already produce the desired common key.
2. Identify representative IDs and inspect them with `DUMP`.
3. Add the family rule next to related normalization rules.
4. Confirm it covers both grid sizes and common variants.
5. Make sure it does not catch unrelated blocks.
6. Verify the result in the runtime log.

This is how existing families are usually handled: oxygen generators, hydrogen tanks, doors, lights, and similar block families are normalized by pattern, not by enumerating every subtype by hand.

## A block without a research-block definition

Grouping and research eligibility are separate checks.

A block can be seen by the split program and still be unusable for learning if it does not appear in the proper research-block file.

Check:

- base-game blocks in [`Data/ResearchBlocks.sbc`](./Data/ResearchBlocks.sbc)
- mod blocks in the relevant `Data/Mods/<workshop-id>.sbc` file

Do not hand-edit `UnlockedByGroups` on a research-block entry to create a group. The runtime clears and repopulates those memberships based on the split program.

## Regeneration warnings

[`Data/regenerate.py`](./Data/regenerate.py) is a manual offline generator. It is not run by the game.

It can rewrite generated files, including:

- [`Data/ResearchBlocks.sbc`](./Data/ResearchBlocks.sbc)
- mod research-block files
- placeholder research and unlocker files

The generator is known to be fragile. It may drop or drift research state if used casually. Only run it intentionally, review every generated diff, and keep the generated output synchronized with the intended rule change.

## Runtime groups and group IDs

The runtime builds groups from the split program, filters out groups with no public or researchable blocks, and assigns the remaining groups to unlockers such as `U1`, `U2`, and so on.

This matters because:

- group order matters
- explicit `GROUP` placement affects later rules
- a regrouping change can shift unlocker indexes
- saved worlds may retain old progression assumptions

If the number of groups changes significantly, check that the placeholder research definitions still exist.

## Validation checklist

Before finishing any change, confirm all of the following:

- [ ] The exact block type and subtype were checked.
- [ ] Existing normalization rules were reviewed before adding a new rule.
- [ ] Regex selectors are specific and use anchors when needed.
- [ ] `DUMP` output shows the expected transformed keys.
- [ ] Intended variants end up in the same group; unrelated ones stay separate.
- [ ] The group has at least one public block and at least one research-block definition.
- [ ] The log has no parser or regex errors.
- [ ] Any generated SBC files were reviewed and kept synchronized.
- [ ] A test world was used to confirm learn/grind behavior.
- [ ] Temporary debug `DUMP` lines were removed.

## Final rule of thumb

The correct fix is usually to normalize the family name in `SPLIT_PROGRAM.txt`, not to add a special case for one block.

Keep grouping rule-based, keep runtime behavior inside the supported public API, and do not make chat command access admin-only.

The mod is meant to respect normal player progression. `/gtl unlearn` may require saving and reloading the world before build access is fully revoked.
