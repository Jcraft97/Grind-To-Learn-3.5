# Block research groups: maintainer guide

This is the practical guide for changing research groups in Grind to Learn.

If you are editing a group, start here:

1. Find the exact block ID in the `Block_Names_reference.txt`
2. Look for the family rule in `Data/SPLIT_PROGRAM.txt`.
3. Change the normalization rule, not a one-off block exception. (unless you have too)
4. Confirm the block is in a research-block file.
5. Test in-game and verify the group result in the log.

## What this file is for

`Data/SPLIT_PROGRAM.txt` is the source of truth for research grouping.

It is not a list of unlocks. It is a processing pipeline.

The runtime takes every block definition, rewrites its name into a normalized key, and groups blocks that end up with the same final key. The rules run in order, so a small change in the middle of the file can completely change later grouping.

The core rule:

- `MATCH` = normalize a family and keep the blocks in the active pool
- `GROUP` = take selected blocks out of the pool and make an explicit group immediately
- everything left over at the end is grouped by shared final key

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

means: for blocks whose current key contains `OxygenGenerator`, Add the `Lab` Subvarients.

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

## Common change 2: add a completely new family

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
