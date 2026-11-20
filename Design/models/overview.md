# Authored models — overview

This is the delivery contract for the fighter and sword assets used by M9. It is technical only: silhouette, colour, material styling, and weapon design are the modeler's choice. Each model is described in exactly one file; shared rules live here. T9.2 loads both assets with DigitalRiseModel and validates the resulting `DrModel`; rules that only exist in the authored source are called out as authoring rules.

| Document | Contents |
| --- | --- |
| [fighter.md](fighter.md) | Fighter size, anchors, skeleton, mesh, materials, and all 17 animation clips |
| [sword.md](sword.md) | Sword geometry and static mesh |
| [validation.md](validation.md) | `--validate-models`, acceptance checklist, and regeneration |

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
| Forbidden | Root motion, IK, constraints, morph targets, LODs, custom shaders, Draco compression, external texture files |

`fighter.glb` contains one skinned mesh (one or more primitives sharing one skin) and all clips. `sword.glb` is a static mesh with no skeleton, no skin, and no animation.

## Source and licence

- The modeler supplies `Content/Models/fighter.glb` and `sword.glb`; commit them with an editable source file when available. The runtime only consumes the `.glb` files.
- Commit `Content/Models/LICENSES.md` naming the author, licence, source/URL, date, and any texture/font licences.
- No unlicensed third-party meshes, textures, or animation data. The project's MIT licence does not cover the artwork; the asset licence must be compatible with redistribution.
- Record any changes made to a third-party base mesh or animation and keep the original licence text.

Visual review (silhouette, palette, readability) is separate from this contract and is the designer's call.
