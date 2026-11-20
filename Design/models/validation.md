# Model validation

The fighter spec (including the clip contract) is in [fighter.md](fighter.md), and the sword spec is in [sword.md](sword.md). Shared delivery rules are in [overview.md](overview.md).

## Harness

`--validate-models` loads `Content/Models/fighter.glb` and `sword.glb` through DigitalRiseModel in a one-frame window and validates the returned `DrModel`/`DrModelInstance`. It does not parse the GLB itself. `ModelFacts` flattens the model (skins, parts, vertex declarations, clips, bounds, textures), so the rules run in xUnit without a graphics device. Missing or invalid assets exit non-zero with the file, joint/clip, and expected-vs-actual values in the message.

DigitalRiseModel adds a synthetic `_Root` bone when a scene has more than one root node, and some importers add an armature object above `root`. The validator accepts a non-joint parent above `root`; it is not a skin joint.

## Current state

No model files are committed yet. T9.3 supplies `Content/Models/fighter.glb` and `sword.glb`; until then `--validate-models` reports both files as missing.

## Acceptance

An asset is accepted only when:

1. T9.2 loads both files through DigitalRiseModel and validates the resulting `DrModel` with zero errors.
2. All 21 joints exist with the exact parent hierarchy and world rest positions within tolerance; the skin has at most 72 joints.
3. Every skinned part declares four weights per vertex, the weights sum to 1 ± 0.001, and no part declares more than four influences.
4. The fighter is within height/bounds budgets and touches the floor at rest; the sword is within the blade dimensions.
5. All 17 clip names exist; durations match the table within 0.001 s, and looped clips start and end on the same pose.
6. No root translation and no channels on `weapon_r`; the sword has no skin or animation. Morph targets, LODs, IK, constraints, and Draco are forbidden and must be absent from the source.
7. `weapon_r` exists under `hand_r` and the sword attaches with identity without a gap or offset.
8. The licence/source note is present.
9. The model loads and renders in the T-pose and Idle at 1600×900 without skinning artifacts.

Visual review (silhouette, palette, readability) is separate from this contract and is the designer's call.
