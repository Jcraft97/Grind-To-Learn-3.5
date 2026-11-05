# Block research groups: maintainer guide

This is the practical guide for changing research groups in Grind to Learn.

If you are editing a group, start here:

1. Find the exact block ID in the game or mod definition files.
2. Look for the family rule in `Data/SPLIT_PROGRAM.txt`.
3. Change the normalization rule, not a one-off block exception.
4. Confirm the block is in a research-block file.
5. Test in-game and verify the group result in the log.

This document is meant to be easy to follow while you are actively editing the mod.

## What this file is for

`Data/SPLIT_PROGRAM.txt` is the source of truth for research grouping.

It is not a list of unlocks. It is a processing pipeline.

The runtime takes every block definition, rewrites its name into a normalized key, and groups blocks that end up with the same final key. The rules run in order, so a small change in the middle of the file can completely change later grouping.

The core rule:

- `MATCH` = normalize a family and keep the blocks in the active pool
- `GROUP` = take selected blocks out of the pool and make an explicit group immediately
- everything left over at the end is grouped by shared final key

## The mental model: how the file works

Think of each block as passing through a pipeline:

```text
raw block ID
  -> apply rule 1
  -> apply rule 2
  -> apply rule 3
  -> final key
  -> grouped with other blocks that share the same final key
```

The file starts with aliases:

```text
DEFINE D DEFINE
D M MATCH
D @ GROUP
```

That means:

- `D` is shorthand for `DEFINE`
- `M` is shorthand for `MATCH`
- `@` is shorthand for `GROUP`

So this line:

```text
M OxygenGenerator Lab=
```

means: for blocks whose current key contains `OxygenGenerator`, remove `Lab` from the key.

The block stays in the active set and may later join other variants that normalize to the same key.

This line:

```text
@ ^ArmorCenter$ ^ArmorCorner$ ^ArmorInvCorner$ ^ArmorSide$
```

means: create one explicit group from those exact matching keys, and remove those blocks from the later pipeline.

## The actual rule language

Each non-empty line is a command plus arguments. Comments start with `#`.

| Command | What it means |
|---|---|
| `DEFINE alias command` | Creates a shorthand alias. |
| `regex=replacement` | Applies a regex rewrite to every current key. |
| `MATCH selector regex=replacement` | Applies the rewrite only to matching blocks. They remain in the pool. |
| `GROUP selector ...` | Creates a group from matching blocks and removes them from future processing. |
| `IFMOD mod-name-regex` | Runs the following lines only when a matching mod is active. |
| `IGNORE selector ...` | Removes matching blocks from later grouping. |
| `DISABLE selector ...` | Marks matching blocks as non-public. |
| `UNLOCK selector ...` | Marks matching blocks as unlocked for every player. |
| `CHANCE number selector ...` | Overrides spawn chance. |
| `DUMP` | Logs the current keys and IDs to the debug log. |

Important syntax rules:

- Separate tokens with one ordinary space.
- A replacement must contain exactly one `=`.
- `MATCH` does not remove blocks from the set.
- `GROUP` does remove matching blocks from the remaining pipeline.
- Regexes are not implicitly anchored. Use `^` and `$` when you need the whole key to match.
- A later rule sees the transformed key from the earlier rule.

## The real patterns used in this mod

The file is full of family patterns. This is deliberate. Block families are normalized by shared naming rules, not by adding a hardcoded exception for every single subtype.

Examples from the actual file:

```text
M Cockpit ^(LargeBlockCockpitSeat|SmallBlockCockpit|DBSmallBlockFighterCockpit|SmallBlockCapCockpit)$=Cockpit
M Cockpit ^(Large|Small)BlockCockpitIndustrial$=Cockpit
M Cockpit ^(Large|Small)BlockOpenSlopedCockpit$=Cockpit
M Cockpit ^(Large|Small)BlockClosedSlopedCockpit$=Cockpit
SmallBlockFlushCockpit=Cockpit
```

This normalizes many cockpit variants into one `Cockpit` family.

Another example:

```text
M HydrogenTank Lab= Bulk=
M OxygenTank Lab=
M OxygenGenerator Lab=
```

This keeps the family together while removing variant words like `Lab` or `Bulk`.

And the more general normalization block:

```text
# buzz words
Block= Half= Short= Medium= Inverted= Angled= Left= Right= Colorable= Corner= Center= Offset= Modular= [Mm]irrored= Cylindrical=
```

This is why many unrelated blocks naturally collapse into a family key.

## The file order matters

This is one of the most important rules in the whole document.

A `GROUP` is not “after everything else.” It runs right where it appears.

This means:

- if you add a broad `GROUP` too early, it may steal blocks before they reach the family rules you expect
- if you add a later normalization rule after a `GROUP`, it will not affect those already removed blocks
- the same block can end up in a different group simply because a rule moved earlier or later

Example from the file:

```text
IFMOD .

@ ^ArmorCenter$ ^ArmorCorner$ ^ArmorInvCorner$ ^ArmorSide$
```

That explicit armor group happens before later broad family rules. That placement is intentional.

## Common change 1: add a new variant to an existing group

This is the most common task.

Use this workflow:

1. Find the exact block definition ID.
2. Look for the family rule near similar blocks in `Data/SPLIT_PROGRAM.txt`.
3. Check whether the new subtype is already covered by a general rule.
4. If it only differs by a family word, add or extend the `MATCH` rule.
5. Use `DUMP` before the group to confirm the transformed key.
6. Test the runtime grouping.
7. Remove the `DUMP` after you finish.

Example pattern:

```text
M Door Sliding= Slide= Side= Hatch= Narrow= EvenWide= Lab= Inv$= Glass= Centred= [ABC]$=
```

If a new door variant uses a naming pattern already covered by this rule, you should expand the existing rule rather than adding a single hardcoded subtype rule.

### Good rule change

```text
M OxygenGenerator Lab=
```

This is good when the family already has a pattern and only needs a suffix cleaned up.

### Bad rule change

```text
@ SomeSpecificBlockSubtype
```

This is bad when the real issue is family normalization. It creates a fragile one-off rule and risks repeated maintenance.

### Use `DUMP` to confirm a fix

Add a temporary debug line right before the relevant `GROUP` or before the end of the file:

```text
DUMP
```

Then check the game log after loading the mod. You want to see the transformed key for your intended block and confirm it matches the family you expect.

## Common change 2: split one group into multiple groups

This usually happens when two block families were merged too aggressively.

### The fix

1. Find the rule that is collapsing them together.
2. Add a more specific rule before the broader rule catches them both.
3. If needed, add a targeted `GROUP` before the family rule absorbs them together.
4. Make sure the selectors are precise enough to avoid other blocks.

Example approach:

```text
M Cockpit ...
```

A cockpit family is normalized by a shared expression. If one cockpit variant should stay separate, preserve the distinguishing part before the broader rule runs, or add a more specific `GROUP` before the normalization.

### Key principle

`GROUP` removes matches from the remaining pipeline. If you need a subset to stay available for a later family rule, the `GROUP` must be placed at the correct moment.

## Common change 3: merge two groups into one

This is the opposite problem.

If two families should be the same group, make their final rewritten keys match.

Typical fix:

- add a normalization rule that removes the distinguishing word from both families
- or align the rule order so they share the same final key before the group is finalized

Example:

```text
M Antenna Dish= Radio= Compact=
(Beacon|BroadcastController|Transponder)=Antenna
```

This shows family-level normalization that merges multiple closely related block names under a common final key.

### Important warning

If a `GROUP` already removed a block earlier, changing only the later rules will not fix it. You must adjust the earlier explicit group or the earlier normalization flow.

## Common change 4: add a completely new family

When a brand-new block family needs its own group, do not jump straight to a giant hardcoded list.

Use this pattern:

1. Check if the family already shares a general naming pattern.
2. Find a nearby family rule that matches the same style.
3. Normalize the family words with `MATCH`.
4. Use explicit `GROUP` only if the family must be separated at a precise point.

Good family example from the file:

```text
M HydrogenTank Lab= Bulk=
M OxygenTank Lab=
M OxygenGenerator Lab=
```

These are family rules, not per-block exceptions.

## Common change 5: fix a block with no research definition

A block can be correctly normalized but still never become a real research group if it has no research-block definition.

Check:

- base-game blocks in `Data/ResearchBlocks.sbc`
- mod blocks in the corresponding `Data/Mods/<workshop-id>.sbc` file

Important:

- do not hand-edit `UnlockedByGroups` just to force a group
- the runtime clears and repopulates those values based on the split program

If the block does not appear in the relevant research-block file, the group will not become a valid learnable group from the runtime’s perspective.

## Common change 6: debug a bad grouping result

When a block lands in the wrong group, use this workflow:

1. Place a temporary `DUMP` before the relevant `GROUP` or before the end of the file.
2. Load the mod and check the game log.
3. Inspect the block's current key.
4. Adjust the nearest rule rather than adding a custom-case exception.
5. Remove the `DUMP` once it is fixed.

Example debug stub:

```text
# DUMP
```

The file itself even includes examples:

```text
# DUMP # prints current block list to log
# DISABLE WHEELS ECONOMY # example
# UNLOCK WindTurbine Battery # example
# CHANCE 5 WindTurbine Battery # example
```

This is the expected way to debug a bad group result without altering the runtime rules permanently.

## Common change 7: check the generator and output files

[`Data/regenerate.py`](./Data/regenerate.py) is an offline generator. It is not runtime code.

It can rewrite generated data such as:

- `Data/ResearchBlocks.sbc`
- mod research-block files
- placeholder research and unlocker files

This generator is known to be fragile. It may lose or drift research state if you use it casually.

So the safe rule is:

- do not assume the generator is a normal part of the runtime
- do not run it casually as a test
- if you do use it, review every generated diff carefully
- keep the final generated files synchronized with the intended research change

## The real-world rules in this project

The project has a few important constraints:

- keep grouping rule-based, not hand-written block-by-block
- use the public Space Engineers research API only
- keep the mod player-owned and not admin-only
- do not force an unlearn state with prohibited internals
- use `/gtl unlearn` as the supported approach

These are not optional style choices. They are part of the mod’s design.

## Quick reference: when to use which tool

Use `MATCH` when:

- the family mostly shares the same name and only needs a suffix or variant word removed
- you are normalizing families like Cockpit, Door, Light, HydrogenTank, or OxygenGenerator

Use `GROUP` when:

- a set of blocks should become a single explicit group at a specific point in the pipeline
- you need a special family split while the pipeline is still active

Use `IGNORE` when:

- a block should never be included in a group at all

Use `DISABLE` when:

- the block should remain non-public

Use `UNLOCK` when:

- the block should be available to every player regardless of normal progression

## Validation checklist

Before considering a change complete, confirm all of these:

- [ ] The exact block ID and subtype were checked.
- [ ] The relevant family pattern in `Data/SPLIT_PROGRAM.txt` was reviewed before editing.
- [ ] The change uses a family rule instead of a one-off exception.
- [ ] The selector is specific enough to avoid unrelated blocks.
- [ ] The transformed key looks correct in the log.
- [ ] The intended variants end up in one group.
- [ ] Unintended variants do not collapse together.
- [ ] The block exists in a research-block definition file.
- [ ] Temporary `DUMP` lines were removed.
- [ ] A test world or runtime check was used to confirm the final behavior.

## Short recipe: the “do this” version

If you need to fix a block group, use this exact pattern:

1. Open `Data/SPLIT_PROGRAM.txt`.
2. Find the family rule near the matching block type.
3. Decide whether this is a family normalization problem or an explicit group problem.
4. Add or revise a `MATCH` rule to normalize the family.
5. Only use `GROUP` when you need a special explicit grouping point.
6. Add `DUMP` temporarily to confirm the final key.
7. Verify the block is in a valid research-block file.
8. Remove debug lines and retest.
9. If a group index changed, check the runtime unlocker mapping and test progression carefully.

## Final reminder

The right fix is usually not “add one more block exception”.

The right fix is usually:

- find the family pattern
- normalize the naming difference once
- keep the grouping rule-based
- verify the runtime and the research files

If the current shape of the file makes the block family look wrong, that is a sign to inspect the nearby normalization rules before you write a custom rule for a single block.
