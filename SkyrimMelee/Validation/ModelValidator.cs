using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SkyrimMelee.Validation;

/// <summary>
/// Validates the <see cref="ModelFacts"/> read from a loaded <c>DrModel</c>.
/// The rules mirror <c>Design/models.md</c>; combat timing stays in <c>CombatTuning</c>.
/// </summary>
public static class ModelValidator
{
	public static ValidationReport Validate(ModelFacts facts)
	{
		var report = new ValidationReport();
		ValidateMaterials(facts, report);

		if (facts.Kind == ModelKind.Fighter)
		{
			ValidateFighter(facts, report);
		}
		else
		{
			ValidateSword(facts, report);
		}

		return report;
	}

	private static void ValidateMaterials(ModelFacts facts, ValidationReport report)
	{
		if (facts.Materials.Count == 0)
		{
			report.Error(facts.File, "materials", "at least one material with a base-colour texture is required");
			return;
		}

		if (facts.Materials.Count > ModelSpec.MaxMaterials)
		{
			report.Error(
				facts.File,
				"materials",
				$"{facts.Materials.Count} materials; the limit is {ModelSpec.MaxMaterials}");
		}

		var sizes = facts.Kind == ModelKind.Fighter
			? ModelSpec.FighterTextureSizes
			: ModelSpec.SwordTextureSizes;

		foreach (var material in facts.Materials)
		{
			var location = $"materials[{material.Index}]";
			if (material.TextureWidth == 0 || material.TextureHeight == 0)
			{
				report.Error(facts.File, location, "missing base-colour texture");
				continue;
			}

			if (material.TextureWidth != material.TextureHeight ||
				!sizes.Contains(material.TextureWidth))
			{
				report.Error(
					facts.File,
					location,
					$"{material.TextureWidth}x{material.TextureHeight} texture; expected one of {string.Join('/', sizes)} and square");
			}
		}
	}

	private static void ValidateFighter(ModelFacts facts, ValidationReport report)
	{
		if (facts.Parts.Count == 0)
		{
			report.Error(facts.File, "meshes", "no mesh parts");
			return;
		}

		if (facts.Skins.Count != 1)
		{
			report.Error(
				facts.File,
				"skins",
				$"{facts.Skins.Count} skins; the fighter must have exactly one");
			return;
		}

		var skin = facts.Skins[0];
		ValidateJoints(facts, report, skin);

		var triangles = 0;
		foreach (var part in facts.Parts)
		{
			ValidatePart(facts, report, part, expectedSkinIndex: skin.Index, expectedJointCount: skin.Joints.Count);
			triangles += part.TriangleCount;
		}

		if (triangles > ModelSpec.MaxFighterTriangles)
		{
			report.Error(
				facts.File,
				"meshes",
				$"{triangles} triangles; the limit is {ModelSpec.MaxFighterTriangles}");
		}

		if (facts.Bounds is not BoundingBox bounds)
		{
			report.Error(facts.File, "bounds", "model bounds are unavailable");
		}
		else
		{
			var height = bounds.Max.Y - bounds.Min.Y;
			if (MathF.Abs(height - ModelSpec.FighterHeight) > ModelSpec.LengthTolerance)
			{
				report.Error(
					facts.File,
					"bounds",
					$"height {height:0.###} m; expected {ModelSpec.FighterHeight:0.###} m ±{ModelSpec.LengthTolerance:0.###}");
			}

			if (MathF.Abs(bounds.Min.Y) > ModelSpec.LengthTolerance)
			{
				report.Error(
					facts.File,
					"bounds",
					$"lowest point at Y {bounds.Min.Y:0.###}; expected the floor at Y 0 ±{ModelSpec.LengthTolerance:0.###}");
			}
		}

		ValidateClips(facts, report);
	}

	private static void ValidateJoints(ModelFacts facts, ValidationReport report, SkinFact skin)
	{
		if (skin.Joints.Count > ModelSpec.MaxJoints)
		{
			report.Error(
				facts.File,
				"skins[0].joints",
				$"{skin.Joints.Count} joints; SkinnedEffect supports {ModelSpec.MaxJoints}");
		}

		if (skin.Joints.Count != ModelSpec.Joints.Count)
		{
			report.Error(
				facts.File,
				"skins[0].joints",
				$"{skin.Joints.Count} joints; expected {ModelSpec.Joints.Count}");
		}

		var byName = new Dictionary<string, BoneFact>(StringComparer.Ordinal);
		foreach (var joint in skin.Joints)
		{
			if (!byName.TryAdd(joint.Name, joint))
			{
				report.Error(facts.File, "skins[0].joints", $"duplicate joint '{joint.Name}'");
			}
		}

		foreach (var spec in ModelSpec.Joints)
		{
			if (!byName.TryGetValue(spec.Name, out var joint))
			{
				report.Error(facts.File, "skins[0].joints", $"missing joint '{spec.Name}'");
				continue;
			}

			var parent = spec.Parent ?? "<none>";
			if (joint.ParentName != spec.Parent)
			{
				report.Error(
					facts.File,
					$"joints['{spec.Name}']",
					$"parent is '{joint.ParentName ?? "<none>"}'; expected '{parent}'");
			}

			if (spec.RestPosition is Vector3 expected &&
				(joint.RestPosition - expected).Length() > ModelSpec.RestTolerance)
			{
				report.Error(
					facts.File,
					$"joints['{spec.Name}']",
					$"rest position is ({joint.RestPosition.X:0.###}, {joint.RestPosition.Y:0.###}, {joint.RestPosition.Z:0.###}); expected ({expected.X:0.###}, {expected.Y:0.###}, {expected.Z:0.###})");
			}
		}

		foreach (var joint in skin.Joints)
		{
			if (ModelSpec.FindJoint(joint.Name) is null)
			{
				report.Error(facts.File, "skins[0].joints", $"unexpected joint '{joint.Name}'");
			}
		}
	}

	private static void ValidatePart(
		ModelFacts facts,
		ValidationReport report,
		PartFact part,
		int expectedSkinIndex,
		int expectedJointCount)
	{
		var location = "meshes";

		if (!part.Skinned)
		{
			report.Error(facts.File, location, "fighter mesh part is not skinned");
		}
		else if (part.SkinIndex != expectedSkinIndex)
		{
			report.Error(
				facts.File,
				location,
				$"mesh part uses skin {part.SkinIndex}; expected {expectedSkinIndex}");
		}

		if (part.SkinJointCount != expectedJointCount)
		{
			report.Error(
				facts.File,
				location,
				$"mesh part skin has {part.SkinJointCount} joints; expected {expectedJointCount}");
		}

		if (part.PrimitiveType != PrimitiveType.TriangleList)
		{
			report.Error(facts.File, location, $"primitive type {part.PrimitiveType}; only TriangleList is supported");
		}

		if (part.InfluenceCount > ModelSpec.MaxInfluences)
		{
			report.Error(
				facts.File,
				location,
				$"{part.InfluenceCount} skin influences; the limit is {ModelSpec.MaxInfluences}");
		}

		if (!float.IsNaN(part.MinWeightSum) && !float.IsNaN(part.MaxWeightSum))
		{
			if (MathF.Abs(part.MinWeightSum - 1f) > ModelSpec.WeightTolerance ||
				MathF.Abs(part.MaxWeightSum - 1f) > ModelSpec.WeightTolerance)
			{
				report.Error(
					facts.File,
					location,
					$"weights sum between {part.MinWeightSum:0.####} and {part.MaxWeightSum:0.####}; expected 1 ±{ModelSpec.WeightTolerance}");
			}
		}
		else if (part.Skinned)
		{
			report.Warning(
				facts.File,
				location,
				$"weight sums could not be read: {part.SkinningIssue ?? "unknown reason"}");
		}

		if (part.MaxJointIndex >= expectedJointCount)
		{
			report.Error(
				facts.File,
				location,
				$"bone index {part.MaxJointIndex}; expected 0..{expectedJointCount - 1}");
		}

		if (!part.HasNormals)
		{
			report.Error(facts.File, location, "mesh part is missing normals");
		}
	}

	private static void ValidateClips(ModelFacts facts, ValidationReport report)
	{
		if (facts.Clips.Count != ModelSpec.AnimationClips.Count)
		{
			report.Error(
				facts.File,
				"animations",
				$"{facts.Clips.Count} clips; expected {ModelSpec.AnimationClips.Count}");
		}

		var byName = new Dictionary<string, ClipFact>(StringComparer.Ordinal);
		foreach (var clip in facts.Clips)
		{
			if (!byName.TryAdd(clip.Name, clip))
			{
				report.Error(facts.File, "animations", $"duplicate clip '{clip.Name}'");
			}
		}

		foreach (var spec in ModelSpec.AnimationClips)
		{
			if (!byName.TryGetValue(spec.Name, out var clip))
			{
				report.Error(facts.File, "animations", $"missing clip '{spec.Name}'");
				continue;
			}

			if (MathF.Abs(clip.Duration - spec.Duration) > ModelSpec.TimeTolerance)
			{
				report.Error(
					facts.File,
					$"animations['{spec.Name}']",
					$"duration {clip.Duration:0.###} s; expected {spec.Duration:0.###} s ±{ModelSpec.TimeTolerance}");
			}

			ValidateClipChannels(facts, report, spec, clip);
		}

		foreach (var clip in facts.Clips)
		{
			if (ModelSpec.FindClip(clip.Name) is null)
			{
				report.Error(facts.File, "animations", $"unexpected clip '{clip.Name}'");
			}
		}
	}

	private static void ValidateClipChannels(
		ModelFacts facts,
		ValidationReport report,
		ClipSpec spec,
		ClipFact clip)
	{
		var location = $"animations['{spec.Name}']";
		if (clip.Channels.Count == 0)
		{
			report.Error(facts.File, location, "clip has no channels");
			return;
		}

		var minTime = float.MaxValue;
		var maxTime = float.MinValue;

		foreach (var channel in clip.Channels)
		{
			if (channel.BoneName == "weapon_r")
			{
				report.Error(facts.File, location, "weapon_r must not be animated; the sword follows hand_r");
			}

			if (channel.BoneName == "root" &&
				channel.Keyframes.Any(keyframe => keyframe.Translation.LengthSquared() > 1e-8f))
			{
				report.Error(facts.File, location, "root motion is forbidden");
			}

			foreach (var keyframe in channel.Keyframes)
			{
				minTime = MathF.Min(minTime, keyframe.Time);
				maxTime = MathF.Max(maxTime, keyframe.Time);
			}

			if (spec.Loop && channel.Keyframes.Count >= 2 &&
				!SamePose(channel.Keyframes[0], channel.Keyframes[^1]))
			{
				report.Error(
					facts.File,
					location,
					$"looped clip does not end on its first pose for '{channel.BoneName}'");
			}
		}

		if (MathF.Abs(minTime) > ModelSpec.TimeTolerance)
		{
			report.Error(facts.File, location, $"first keyframe is at {minTime:0.###} s; expected 0");
		}

		if (MathF.Abs(maxTime - spec.Duration) > ModelSpec.TimeTolerance)
		{
			report.Error(
				facts.File,
				location,
				$"last keyframe is at {maxTime:0.###} s; expected {spec.Duration:0.###} s");
		}
	}

	private static bool SamePose(KeyframeFact first, KeyframeFact last)
	{
		if ((first.Translation - last.Translation).LengthSquared() > 1e-8f ||
			(first.Scale - last.Scale).LengthSquared() > 1e-8f)
		{
			return false;
		}

		var dot = MathF.Abs(Quaternion.Dot(first.Rotation, last.Rotation));
		return MathF.Abs(dot - 1f) <= 1e-4f;
	}

	private static void ValidateSword(ModelFacts facts, ValidationReport report)
	{
		if (facts.Parts.Count == 0)
		{
			report.Error(facts.File, "meshes", "no mesh parts");
			return;
		}

		if (facts.Skins.Count != 0)
		{
			report.Error(facts.File, "skins", "the sword must not have a skin or skeleton");
		}

		if (facts.Clips.Count != 0)
		{
			report.Error(facts.File, "animations", "the sword must be a static mesh with no animations");
		}

		var triangles = 0;
		foreach (var part in facts.Parts)
		{
			if (part.Skinned)
			{
				report.Error(facts.File, "meshes", "the sword must not be skinned");
			}

			if (part.PrimitiveType != PrimitiveType.TriangleList)
			{
				report.Error(facts.File, "meshes", $"primitive type {part.PrimitiveType}; only TriangleList is supported");
			}

			if (!part.HasNormals)
			{
				report.Error(facts.File, "meshes", "mesh part is missing normals");
			}

			triangles += part.TriangleCount;
		}

		if (triangles > ModelSpec.MaxSwordTriangles)
		{
			report.Error(
				facts.File,
				"meshes",
				$"{triangles} triangles; the limit is {ModelSpec.MaxSwordTriangles}");
		}

		if (facts.Bounds is not BoundingBox bounds)
		{
			report.Error(facts.File, "bounds", "model bounds are unavailable");
			return;
		}

		if (MathF.Abs(bounds.Max.Y - ModelSpec.SwordTipY) > ModelSpec.LengthTolerance)
		{
			report.Error(
				facts.File,
				"bounds",
				$"blade tip at Y {bounds.Max.Y:0.###}; expected {ModelSpec.SwordTipY:0.###} ±{ModelSpec.LengthTolerance:0.###}");
		}

		var width = bounds.Max.X - bounds.Min.X;
		if (MathF.Abs(width - ModelSpec.SwordCrossguardWidth) > ModelSpec.LengthTolerance)
		{
			report.Error(
				facts.File,
				"bounds",
				$"crossguard width {width:0.###} m; expected {ModelSpec.SwordCrossguardWidth:0.###} ±{ModelSpec.LengthTolerance:0.###}");
		}
	}
}
