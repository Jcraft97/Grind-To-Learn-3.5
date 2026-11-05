# Block research groups: maintainer guide

This guide explains how to add or change block research groups in Grind to Learn. It covers the rule language, how the game definitions feed into grouping, how to check your changes, and when generated files need attention.

## The short version

1. Find the affected block's exact definition ID and check the existing normalization rules in [`Data/SPLIT_PROGRAM.txt`](./Data/SPLIT_PROGRAM.txt).
2. Prefer adjusting a rule that groups a family of variants over listing individual blocks.
3. Use a `MATCH` rule to normalize names and a `GROUP` rule only when a family needs an explicit group separate from the remaining normalized-name groups.
4. Check that the block has a research-block definition in [`Data/ResearchBlocks.sbc`](./Data/ResearchBlocks.sbc) (or the relevant `Data/Mods/<workshop-id>.sbc` file).
5. Test the runtime grouping and inspect the game log. Do not assume the offline generator is safe to run or that the game regenerates SBCs at runtime.

## Which files do what

| File | Role |
|---|---|
| [`Data/SPLIT_PROGRAM.txt`](./Data/SPLIT_PROGRAM.txt) | Source of truth for reducing block IDs to group keys and explicitly grouping selected blocks. The runtime reads this file when loading the mod. |
| [`Data/ResearchBlocks.sbc`](./Data/ResearchBlocks.sbc) | Research-block definitions for base-game blocks. The runtime assigns their `UnlockedByGroups` membership based on the split program. |
| [`Data/Mods/`](./Data/Mods) | Research-block definitions for supported workshop mods, stored by workshop ID. |
| [`Data/ResearchGroups.sbc`](./Data/ResearchGroups.sbc) | Placeholder research-group definitions. Runtime code clears and repopulates their members. |
| [`Data/UnlockersResearchGroups.sbc`](./Data/UnlockersResearchGroups.sbc) | Placeholder unlocker groups (`U0`, `U1`, etc.). Runtime code assigns their members. |
| [`Data/Unlockers.sbc`](./Data/Unlockers.sbc) | Unlocker block definitions corresponding to those unlocker groups. |
| [`Data/regenerate.py`](./Data/regenerate.py) | Manual, offline generator for research-block and placeholder SBC files. It is not run by the game and is known to be fragile. |

For ordinary group changes, start with `SPLIT_PROGRAM.txt`; do not hand-edit placeholder memberships to try to implement runtime groups.

## How grouping works

At runtime, the mod collects block definition IDs, converts them to strings, and applies the split program's rules in file order. The rules progressively rewrite a key for each block. A `GROUP` command selects blocks by their current keys, removes those blocks from further rule processing, and creates a group from them. At the end, all remaining blocks with the same final key are grouped together.

The file begins with shorthand definitions:

```text
DEFINE D DEFINE
DEFINE M MATCH
DEFINE @ GROUP
```

The first line defines `D` as shorthand for `DEFINE`; the next two lines use it to define `M` as `MATCH` and `@` as `GROUP`. The first token on a line is expanded as a command name, so `M` and `@` are aliases, not separate rule types.

The early normalization rules remove common differences such as object-builder prefixes, block types, grid sizes, and generic words. For example, `Large`/`Small` prefixes and digits are normalized before many later family-specific rules run. Always inspect the existing rules before adding another rule: a new variant may already collapse to the desired key.

### Rule language

Each non-empty line is a command followed by space-separated arguments. `#` starts a comment, including when it appears after a rule. Matching uses .NET regular expressions and is case-sensitive by default.

| Command | Meaning |
|---|---|
| `DEFINE alias command` | Make the first token `alias` stand for `command`. Used in this file to define `M` and `@`. |
| `regex=replacement ...` | Without a command, apply each regex replacement in order to every current key. Regex replacements use .NET syntax, including `$1` for capture groups. |
| `MATCH selector regex=replacement ...` | Apply the listed replacements only to current keys matched by `selector`. Matched blocks remain in the current set and can be affected by later rules. |
| `GROUP selector ...` | Make one explicit group from current keys matching any selector; remove those blocks from later processing. |
| `IFMOD mod-name-regex` | Process subsequent lines only if an active mod name matches. A later `IFMOD` starts a new condition; this is not a nested conditional. `IFMOD .` matches any non-empty mod name and is used in this file as an always-true section. |
| `IGNORE selector ...` | Remove matching blocks from subsequent grouping. |
| `DISABLE selector ...` | Make matching blocks non-public; this is not just a grouping operation. |
| `UNLOCK selector ...` | Mark matching blocks to be unlocked for every player. This is not just a grouping operation. |
| `CHANCE number selector ...` | Override the spawn chance for matching blocks. |
| `DUMP` | Write the current keys and IDs to the game log without changing them. Useful for testing. |

In this file, `M` is shorthand for `MATCH`, and `@` is shorthand for `GROUP`. For example:

```text
M OxygenGenerator Lab=
```

The selector `OxygenGenerator` picks the current keys containing that text, then the replacement `Lab=` removes `Lab` from those keys. The variants remain in the current set and will join blocks that end with the same normalized key.

An explicit group looks like:

```text
@ ^ArmorCenter$ ^ArmorCorner$ ^ArmorSide$
```

All three selectors are alternatives: matching blocks are placed into one group and removed from later processing. Anchors such as `^` and `$` are important when a selector should match a whole key. Without anchors, regex matching can find a substring.

### Important syntax details

- Separate tokens with **one ordinary space**. The parser splits on literal spaces; do not use repeated spaces or rely on tabs for token separation.
- Keep each command on one line. There is no quoting or multi-line regex syntax.
- A `#` begins a comment, so it cannot be used literally in a regex on that line.
- A replacement token must contain exactly one `=`: the text before it is the regex and the text after it is the replacement. `=` is not a delimiter for the whole line.
- Replacements run in order, and later rules see the already-rewritten key.
- Regexes are not implicitly anchored. Use `^...$` when the entire key must match.
- `MATCH` does not remove its matches. `GROUP` does. Use the right operation for the intended behavior.
- `GROUP` runs at that point in the program, not after all normalization. Put it after the key has been normalized as intended.

## Add a block variant to an existing group

1. **Find the ID.** Look up the block's exact type and subtype in the relevant game or mod CubeBlocks definition. Research-block entries use IDs like `Type="..."` and `Subtype="..."`.
2. **Check coverage.** Follow the existing normalization steps in `SPLIT_PROGRAM.txt`. Use a temporary `DUMP` immediately before the relevant `GROUP` (or before the end of the program for a remainder group) to check the transformed key and ID in the game log.
3. **Choose the narrowest useful rule.** If the new subtype differs only by a family-specific word, extend the existing `MATCH` rule for that family. If it is a new naming pattern, add a family-level normalization rule near related rules. Do not add a one-off block exception when the difference can be expressed as a reusable pattern.
4. **Preserve boundaries.** Use anchors or a narrower selector if a short word could occur in unrelated block names. Check that removing a word does not merge unrelated families.
5. **Verify research eligibility.** A group is retained at runtime only if it has at least one public block and at least one research-block definition (except the special reserved group 0). If the new block has no research-block definition, the runtime cannot use it to create a learnable group.
6. **Test and review the log.** Confirm the intended IDs have the same normalized key or appear together in the intended explicit group, and confirm unrelated IDs did not join.

## Split one group into multiple groups, or merge groups

### Split

First find which normalization rule is collapsing the blocks together. Then preserve the distinguishing part of their names, or add an explicit `GROUP` for the intended subset before a broader rule consumes it. Remember that an explicit `GROUP` takes matching blocks out of the remaining pipeline, so its placement changes what later rules can see.

Avoid adding an early broad `GROUP` unless you have checked all its matches. Prefer specific selectors with anchors where the subtype format allows them.

### Merge

If two families should form one remainder group, make their keys identical by applying compatible replacements before the end of the program. If one family is already consumed by a `GROUP`, revise that explicit group or its selectors as well; changing only the later normalization rules cannot affect blocks already removed from `current`.

## Add a new family with its own group

If the variants can share a normalized key, use a `MATCH` rule to normalize their distinguishing variant words; the remaining blocks with that common key become a group automatically. Use an explicit `GROUP` when the family needs to be deliberately separated at a particular point in the rule sequence.

Before adding the rule:

1. Check whether the general rules already produce the desired common key.
2. Identify the exact selector and representative block IDs with `DUMP`.
3. Add the family rule alongside related normalization rules, before any `GROUP` that needs the normalized key.
4. Check that the pattern covers both grid sizes and known variants, while not swallowing similarly named unrelated blocks.
5. Verify the resulting group using the runtime log and the unlocker/research diagnostics.

For example, the existing rules use family-specific normalization for oxygen generators, hydrogen tanks, doors, lights, and other families. Extend the appropriate family rule rather than inserting a list of every known subtype.

## Add a block that has no research-block definition yet

Grouping and research eligibility are separate. The split program can see a cube-block ID, but the runtime filters out groups without research-block definitions. Check:

- Base-game block definitions against `Data/ResearchBlocks.sbc`.
- Supported mod definitions against the corresponding `Data/Mods/<workshop-id>.sbc`.

`regenerate.py` scans the configured local Space Engineers installation and the workshop IDs hard-coded in its `modlist`. It rewrites generated files, including `ResearchBlocks.sbc`, the mod research-block files, and the placeholder group/unlocker files. It may discard or mismatch existing research state, so do not run it as a casual test or assume its output is safe. If regeneration is needed, first preserve the current files, run it intentionally in a controlled working copy with the expected game and mod content available, then review every generated diff and verify that existing research-block entries and workshop outputs were not lost. Keep the final generated files synchronized with the intended change.

Do not hand-add only an `UnlockedByGroups` value to a research-block entry to create a group: the runtime clears those values and assigns unlocker groups based on the split program.

## Runtime groups and group IDs

The runtime builds the groups from the split program, filters groups without public/researchable blocks, and assigns the resulting groups to unlockers such as `U1`, `U2`, and so on. The placeholder files provide definitions the runtime expects; they are not a hand-maintained map of block membership. The generator currently creates 50 ordinary research-group placeholders and 250 unlocker-group placeholders. If a change materially increases the number of groups, check that the required definitions exist rather than extending only the split rules.

Because explicit groups and remaining-key groups are accumulated in program order, a rule change can change the resulting group sequence and therefore the runtime unlocker indices. Do not assume group numbering or saved progression assignments remain stable after regrouping. Back up a test world and validate the changed behavior before using it with an existing world.

## Validation checklist

- [ ] Exact block type/subtype IDs were confirmed from the definition files.
- [ ] Existing normalization and explicit group rules were checked before adding a new rule.
- [ ] Regex selectors match only the intended family; anchors are used where appropriate.
- [ ] `DUMP` output shows the expected transformed keys and IDs.
- [ ] Variants intended to be together resolve to one group; intended separate variants do not.
- [ ] Each intended group has public blocks and research-block definitions.
- [ ] The game log has no regex/parser errors and no unexpected “blocks not included in any group” warnings.
- [ ] Generated SBC files were changed only if needed, and all generated diffs were reviewed.
- [ ] A test world was used to verify learn/grind behavior and the effect of any group-index changes.
- [ ] Temporary `DUMP` rules were removed.

## Related constraints

Research grouping is independent from chat-command permissions and should not be made admin-only. Keep runtime progression behavior on the supported public API; do not use prohibited internal research APIs to force unlock or unlearn behavior. `/gtl unlearn` may require saving and reloading the world before build access is revoked.
