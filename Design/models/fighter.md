# Fighter model

Shared delivery rules are in [overview.md](overview.md). This file contains the fighter's complete specification: size, skeleton, mesh, materials, and animation.

## Size and anchors

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

| Property | Requirement |
| --- | --- |
| Triangle budget | ≤ 20,000 |
| Vertex attributes | `POSITION`, `NORMAL`, `TEXCOORD_0`, `JOINTS_0`, `WEIGHTS_0` |
| Tangents | Optional; not required by v1 |
| Skin influences | 4 per vertex, normalised to 1 ± 0.001 |
| UV sets | 1 |
| Materials | ≤ 3 |
| Base-colour texture | 1024 or 2048 PNG, embedded, sRGB |
| Transparency | Opaque only |
| Normal/roughness/metalness maps | May be authored but v1 does not render them |
| Morph targets / LODs | None |

The runtime uses MonoGame's stock `SkinnedEffect`: base-colour texture, diffuse/specular/emissive colours, default lighting, four weights, 72-bone maximum. Do not rely on PBR, normal maps, alpha test, or custom shader features.

## Animation

All clips are 60 Hz timecodes. Durations are in seconds; frame values are `seconds × 60` and may be fractional. Keyframes must land on the exact wind-up/active/recovery boundaries.

Each clip's name and duration must match the table. The phase columns document where keyframes should land; `CombatTuning` remains the only source of combat timing, so the glTF does not need per-clip metadata.

The Description column states the gameplay/body intent of each clip. It is not visual direction; silhouette, framing, and styling remain the designer's call.

| Clip | Description | Loop | Total frames | Wind-up | Active | Recovery | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- |
| `Idle` | Two-handed ready stance, weight even, blade held low at the right side. Slight breathing and weight shift keep it alive; no attack intent. | yes | 150.0 | — | — | — | 2.5 s, first/last pose equal |
| `GuardRaise` | Draw the blade up across the body into a front guard: elbows in, shoulders square, weight settles onto the back foot. | no | 12.0 | — | — | — | 0.20 s |
| `GuardHold` | Hold the front guard with the blade between the fighters. Small breathing and weight shifts only; no drift into an attack. | yes | 120.0 | — | — | — | 2.0 s loop |
| `GuardLower` | Release the guard and settle back into the relaxed idle stance. | no | 9.0 | — | — | — | 0.15 s |
| `WalkForward` | In-place forward walk: alternating leg swing, opposite arm swing, subtle torso counter-rotation. | yes | 60.0 | — | — | — | 1.0 s loop |
| `WalkBackward` | In-place backpedal: reversed swing with a shorter stride and the same body counter-rotation. | yes | 60.0 | — | — | — | 1.0 s loop |
| `StrafeLeft` | In-place side-step to the left: alternating leg abduction, arms balancing, torso leaning into the step. | yes | 60.0 | — | — | — | 1.0 s loop |
| `StrafeRight` | Mirrored side-step to the right. | yes | 60.0 | — | — | — | 1.0 s loop |
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
- Locomotion clips are in-place and loop; the runtime translates the root. They do not rotate the body; facing stays on the camera.
