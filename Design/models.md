# Authored models and animation — technical specification

This is the delivery contract for the fighter and sword assets used by M9. It is technical only: silhouette, colour, material styling, and weapon design are the modeler's choice. Every number here is enforced by the T9.2 validator where possible.

## Delivery

| Item | Requirement |
| --- | --- |
| Format | glTF 2.0 binary, `.glb`, single file |
| Files | `Content/Models/fighter.glb`, `Content/Models/sword.glb` |
| Units | metres |
| Up axis | `+Y` |
| Forward | `+X`; the runtime does **not** rotate the model |
| Origin | Fighter: floor midpoint between the feet. Sword: grip centre |
| Bind pose | Fighter T-pose, arms extended along ±Z; sword is not part of the fighter mesh |
| Source | A licence/source note is committed next to the assets or in this file |
| Forbidden | Root motion, IK, constraints, morph targets, LODs, custom shaders, Draco compression, external texture files |

`fighter.glb` contains one skinned mesh (one or more primitives sharing one skin) and all clips. `sword.glb` is a static mesh with no skeleton, no skin, and no animation.

## Fighter size and anchors

Height 1.800 m ± 0.010, measured from the floor to the top of the head. The camera eye line is 1.600 m.

| Landmark | World rest position (m) |
| --- | --- |
| Floor midpoint / root | (0, 0, 0) |
| Pelvis | (0, 0.95, 0) |
| Chest | (0, 1.25, 0) |
| Neck base | (0, 1.45, 0) |
| Head bone | (0, 1.58, 0) |
| Top of head | (0, 1.80, 0) |
| Left shoulder / hand | (-0.18, 1.40, -0.18) / (-0.18, 1.40, -0.78) |
| Right shoulder / hand | (+0.18, 1.40, +0.18) / (+0.18, 1.40, +0.78) |
| Left/right hip | (±0.12, 0.92, 0) |
| Left/right knee | (±0.12, 0.48, 0) |
| Left/right ankle | (±0.12, 0.08, 0) |

Right-handed coordinates: `+X` forward, `+Y` up, `+Z` right. The right side is `+Z`; the left side is `-Z`. Arms in the T-pose are straight along ±Z at Y = 1.40. Tolerances are ±0.01 m. Bone lengths follow from the joint positions; do not add scale to bones.

## Skeleton

21 joints, exact names. All bind transforms are identity rotations and unit scale except where noted; the hierarchy must match.

| Joint | Parent | Rest position / note |
| --- | --- | --- |
| `root` | — | (0, 0, 0) |
| `pelvis` | `root` | (0, 0.95, 0) |
| `spine_01` | `pelvis` | (0, 1.08, 0) |
| `spine_02` | `spine_01` | (0, 1.25, 0) |
| `neck` | `spine_02` | (0, 1.45, 0) |
| `head` | `neck` | (0, 1.58, 0) |
| `clavicle_l` | `spine_02` | (-0.18, 1.40, -0.18) |
| `upperarm_l` | `clavicle_l` | (-0.18, 1.40, -0.18) |
| `forearm_l` | `upperarm_l` | (-0.18, 1.40, -0.48) |
| `hand_l` | `forearm_l` | (-0.18, 1.40, -0.78) |
| `clavicle_r` | `spine_02` | (+0.18, 1.40, +0.18) |
| `upperarm_r` | `clavicle_r` | (+0.18, 1.40, +0.18) |
| `forearm_r` | `upperarm_r` | (+0.18, 1.40, +0.48) |
| `hand_r` | `forearm_r` | (+0.18, 1.40, +0.78) |
| `thigh_l` | `pelvis` | (-0.12, 0.92, 0) |
| `calf_l` | `thigh_l` | (-0.12, 0.48, 0) |
| `foot_l` | `calf_l` | (-0.12, 0.08, 0) |
| `thigh_r` | `pelvis` | (+0.12, 0.92, 0) |
| `calf_r` | `thigh_r` | (+0.12, 0.48, 0) |
| `foot_r` | `calf_r` | (+0.12, 0.08, 0) |
| `weapon_r` | `hand_r` | Grip point; local `+Y` is the blade direction, local `+X` the edge, local `+Z` the flat. The sword attaches with an identity local transform. |

`weapon_r` is not animated directly; it follows `hand_r`. The two-handed grip is produced by the hand bones; the sword is never part of the fighter mesh.

## Mesh and materials

| Property | Fighter | Sword |
| --- | --- | --- |
| Triangle budget | ≤ 20,000 | ≤ 4,000 |
| Vertex attributes | `POSITION`, `NORMAL`, `TEXCOORD_0`, `JOINTS_0`, `WEIGHTS_0` | `POSITION`, `NORMAL`, `TEXCOORD_0` |
| Tangents | Optional; not required by v1 | Optional |
| Skin influences | 4 per vertex, normalised to 1 ± 0.001 | None |
| UV sets | 1 | 1 |
| Materials | ≤ 3 | ≤ 3 |
| Base-colour texture | 1024 or 2048 PNG, embedded, sRGB | 1024 PNG, embedded, sRGB |
| Transparency | Opaque only | Opaque only |
| Normal/roughness/metalness maps | May be authored but v1 does not render them | May be authored but not rendered |
| Morph targets / LODs | None | None |

The runtime uses MonoGame's stock `SkinnedEffect`: base-colour texture, diffuse/specular/emissive colours, default lighting, four weights, 72-bone maximum. Do not rely on PBR, normal maps, alpha test, or custom shader features.

## Animation contract

All clips are 60 Hz timecodes. Durations are in seconds; frame values are `seconds × 60` and may be fractional. Keyframes must land on the exact wind-up/active/recovery boundaries.

Each attack clip carries glTF `extras`:

```json
"extras": { "windup": 0.25, "active": 0.22, "recovery": 0.35, "loop": false }
```

DigitalRiseModel does not expose `extras`; the validator reads the GLB JSON chunk with `System.Text.Json`. The combat FSM in `CombatTuning` remains authoritative; a mismatch between clip `extras` and `CombatTuning` is a validation failure.

The Description column states the gameplay/body intent of each clip. It is not visual direction; silhouette, framing, and styling remain the designer's call.

| Clip | Description | Loop | Total frames | Wind-up | Active | Recovery | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `Idle` | Two-handed ready stance, weight even, blade held low at the right side. Slight breathing and weight shift keep it alive; no attack intent. | yes | 150.0 | — | — | — | 2.5 s, first/last pose equal |
| `GuardRaise` | Draw the blade up across the body into a front guard: elbows in, shoulders square, weight settles onto the back foot. | no | 12.0 | — | — | — | 0.20 s |
| `GuardHold` | Hold the front guard with the blade between the fighters. Small breathing and weight shifts only; no drift into an attack. | yes | 120.0 | — | — | — | 2.0 s loop |
| `GuardLower` | Release the guard and settle back into the relaxed idle stance. | no | 9.0 | — | — | — | 0.15 s |
| `SlashLeft` | Forehand cut: turn the hips and shoulders right, pull the sword behind the body, then step in and sweep the edge across the front to the left. Follow through before recovering. | no | 49.2 | 15.0 | 13.2 | 21.0 | 0.25/0.22/0.35 s |
| `SlashRight` | Mirrored backhand: wind left, rotate through the hips, sweep the edge across the front to the right, then recover. | no | 49.2 | 15.0 | 13.2 | 21.0 | mirrored |
| `PowerSlash` | Heavy overhead: shift weight back, raise the hilt above the head with both hands, hold the telegraph, then drive the torso forward and slam the blade down through the centreline. Deep follow-through with a long, committed recovery. | no | 87.6 | 36.0 | 9.6 | 42.0 | 0.60/0.16/0.70 s |
| `Bash` | Hilt-first shove from the guard: pull the hands to the chest, plant the feet, drive the flat forward with the hips, then snap back to the guard. Deliberately minimal blade arc. | no | 32.4 | 9.6 | 4.8 | 18.0 | 0.16/0.08/0.30 s |
| `StaggerMicro` | Timed-block recoil: a short jolt through the arms and a small body dip. Feet stay planted; balance returns within the clip. | no | 15.0 | — | — | — | 0.25 s |
| `StaggerSmall` | Light hit: the torso rocks back and to its left, the arms loosen and the sword dips, then the fighter straightens up. | no | 36.0 | — | — | — | 0.60 s |
| `StaggerLarge` | Heavy hit or guard break: the body is thrown back and to its left, the sword drops, the fighter catches balance with a step and hauls itself upright. | no | 60.0 | — | — | — | 1.00 s |
| `Death` | Balance fails: the knees buckle, the fighter topples and twists to the floor, and the sword comes to rest with the body. Final pose holds. | no (clamp) | 72.0 | — | — | — | 1.20 s; final pose holds |
| `Victory` | The fighter straightens, chest out, and raises the blade overhead in triumph. Final pose holds. | no (clamp) | 96.0 | — | — | — | 1.60 s; final pose holds |

Rules:

- Root motion is forbidden: `root` has no translation keys; hips may translate vertically.
- The first frame is the start pose; the last frame is the end pose. Looped clips must have matching first/last poses and no visual seam.
- Attack active windows must contain the blade path that produces contact; the runtime samples the weapon bone, not the clip's visual tip.
- `Death`/`Victory` clamp on their last pose; all other one-shots return to `Idle` (the runtime crossfades).
- Runtime crossfade is 0.1 s. The animation controller is advanced only from the fixed 60 Hz step; pause/alt-tab freezes it.
- No root motion, no IK, no constraints, no animation of `weapon_r` itself.

## Sword geometry

The sword origin is the grip centre. Local axes: `+Y` from grip to tip, `+X` across the blade edge, `+Z` through the flat.

| Measurement | Value |
| --- | --- |
| Blade base from origin | 0.18 m |
| Blade tip from origin | 1.47 m |
| Blade base radius | 0.0425 m |
| Blade tip radius | 0.018 m |
| Crossguard width | 0.30 m ± 0.01 |
| Grip length | 0.30 m ± 0.01 |
| Maximum triangles | 4,000 |

The sword has no hands; hands belong to the fighter. It is drawn as a static mesh attached to `weapon_r` with identity transform. It does not cast a separate hit volume; the runtime derives `BladeGeometry` from `weapon_r` and the base/tip/radii above.

## Acceptance

An asset is accepted only when:

1. T9.2 validation passes with zero errors.
2. All 21 joints exist with the exact parent hierarchy and bind positions within tolerance.
3. Every vertex has normalised 4-weight skinning and no vertex has more than 4 influences.
4. The fighter is within height/bounds budgets and touches the floor at rest; the sword is within the blade dimensions.
5. All 13 clip names exist; loop/clamp flags, durations, and attack `extras` match the table within 0.001 s.
6. No root translation, no IK/constraints, no morph targets, no LODs, no external files.
7. `weapon_r` exists under `hand_r` and the sword attaches with identity without a gap or offset.
8. The licence/source note is present.
9. The model loads and renders in the T-pose and Idle at 1600×900 without skinning artifacts.

Visual review (silhouette, palette, readability) is separate from this contract and is the designer's call.

## Source and licence

- Deliver the editable source (for example `.blend`) alongside the `.glb` when the modeler agrees; the runtime only consumes the `.glb`.
- Commit `Content/Models/LICENSES.md` naming the author, licence, source/URL, date, and any texture/font licences.
- No unlicensed third-party meshes, textures, or animation data. The project's MIT licence does not cover the artwork; the asset licence must be compatible with redistribution.
- Record any changes made to a third-party base mesh or animation and keep the original licence text.
