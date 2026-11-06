# Block unlock groups guide

This guide is for maintainers of the Grind to Learn mod. It's not a guide for the average players.

If you do want to edit the groups, start here:

The `Data/SPLIT_PROGRAM.txt` file is for the unlock grouping.

This guide goes over the basics of how to read and edit the file. It is not a complete reference for the rule language.

## Quick Basic workflow:

1. Find the exact block ID in the `Block_Names_reference.txt`
2. Look for the family rule in `Data/SPLIT_PROGRAM.txt`. (if you don't find it, you can add a new rule)
3. Change the normalization rule, not a one-off block exception. (unless you have too)
4. Trial and error in-game and verify the group result in the log.

# How the SPLIT_PROGRAM.txt file works

The runtime takes every block definition, rewrites its name into a normalized key, and groups blocks that end up with the same final key. The rules run in order, so a small change in the middle of the file can completely change later grouping.

### Key tags

- `MATCH` = Normalize a family and keep the blocks in the active pool
- `GROUP` = Take selected blocks out of the pool and make an explicit group immediately
- `EXCLUDE` = Remove blocks from Unlock groups. This makes them not Visible in the research tree, but they can still be used in the game.
- everything left over at the end is grouped by shared final key

For easy block grouping we strip parts of the block name off.

Example:

```text 
MyObjectBuilder= # remove tag 
```
This means that when you are naming a block don't put that text in the name or else it will not find the block.

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
