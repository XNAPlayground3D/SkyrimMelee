using Microsoft.Xna.Framework;
using SkyrimMelee.Combat;

namespace SkyrimMelee.Tests;

public class BodyClipTests
{
	private static IReadOnlyList<BodyClip> Clips => BodyClips.Create();
	private static BodyClip Clip(string name) => Clips.Single(c => c.Name == name);

	[Theory]
	[InlineData("StaggerSmall", 0.60f)]
	[InlineData("StaggerLarge", 1.00f)]
	[InlineData("Death", 1.20f)]
	[InlineData("Victory", 1.60f)]
	public void ReactionBodyClipsMatchTheTuning(string name, float duration)
	{
		var clip = Clip(name);

		Assert.Equal(duration, clip.Duration, 4);
		Assert.True(SamePose(BodyPose.Identity, clip.Keys[0].Pose),
			$"{name} must start from the identity body pose.");
	}

	[Fact]
	public void ReactionBodyClipsMatchTheirSwordClips()
	{
		var swords = SwordClips.Create().ToDictionary(clip => clip.Name);

		foreach (var body in Clips)
		{
			Assert.True(swords.TryGetValue(body.Name, out var sword),
				$"Missing sword clip '{body.Name}'.");
			Assert.Equal(sword.Duration, body.Duration, 4);
		}
	}

	[Theory]
	[InlineData("StaggerSmall")]
	[InlineData("StaggerLarge")]
	public void StaggersLeanAndRecover(string name)
	{
		var clip = Clip(name);
		var maxLean = clip.Keys.Max(key => LeanDegrees(key.Pose));

		Assert.True(maxLean > 8f, $"{name} leans only {maxLean:0.0} degrees.");
		Assert.Equal(0f, LeanDegrees(clip.Keys[^1].Pose), 3);
	}

	[Fact]
	public void DeathLiesTheBodyOnTheFloor()
	{
		var final = Clip("Death").Keys[^1].Pose;
		var up = Up(final);

		Assert.True(MathF.Abs(Vector3.Dot(up, Vector3.UnitY)) < 0.1f,
			$"Death leaves the body upright: up={up}");

		var matrix = final.ToMatrix();
		var rotation = Matrix.CreateFromQuaternion(final.Rotation);
		var lowest = float.MaxValue;

		foreach (var kind in new[]
		{
			HurtboxKind.Head, HurtboxKind.Torso, HurtboxKind.Hips, HurtboxKind.Legs
		})
		{
			var definition = HurtboxDefinition.For(kind);
			var center = Vector3.Transform(definition.Center, matrix);
			var bottom = definition.Shape == HurtboxShapeKind.Capsule
				? Vector3.TransformNormal(-Vector3.UnitY * definition.HalfLength, rotation)
				: Vector3.Zero;

			lowest = MathF.Min(lowest, center.Y + bottom.Y - definition.Radius);
		}

		Assert.InRange(lowest, -0.02f, 0.02f);
	}

	[Fact]
	public void VictoryLeansBackAndHolds()
	{
		var clip = Clip("Victory");
		var final = clip.Keys[^1].Pose;

		Assert.True(Up(final).X < -0.1f, "Victory should lean back.");
		Assert.True(SamePose(final, clip.Keys[^2].Pose), "Victory should hold its final pose.");
	}

	private static Vector3 Up(BodyPose pose) =>
		Vector3.TransformNormal(Vector3.UnitY, Matrix.CreateFromQuaternion(pose.Rotation));

	private static float LeanDegrees(BodyPose pose) =>
		MathHelper.ToDegrees(MathF.Acos(Math.Clamp(Vector3.Dot(Up(pose), Vector3.UnitY), -1f, 1f)));

	private static bool SamePose(BodyPose a, BodyPose b) =>
		Vector3.Distance(a.Offset, b.Offset) < 1e-5f &&
		MathF.Abs(Quaternion.Dot(a.Rotation, b.Rotation)) > 0.99999f;
}
