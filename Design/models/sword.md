# Sword model

Shared delivery rules are in [overview.md](overview.md); the fighter's weapon socket is defined in [fighter.md](fighter.md).

## Geometry

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

## Mesh and materials

| Property | Requirement |
| --- | --- |
| Mesh | One static mesh; no skin, skeleton, or animations |
| Vertex attributes | `POSITION`, `NORMAL`, `TEXCOORD_0` |
| Tangents | Optional; not required by v1 |
| UV sets | 1 |
| Materials | ≤ 3 |
| Base-colour texture | 1024 PNG, embedded, sRGB |
| Transparency | Opaque only |
| Normal/roughness/metalness maps | May be authored but not rendered |
| Morph targets / LODs | None |

The sword has no hands; hands belong to the fighter. It is drawn as a static mesh attached to `weapon_r` with identity transform. It does not cast a separate hit volume; the runtime derives `BladeGeometry` from `weapon_r` and the base/tip/radii above.
