using Microsoft.Xna.Framework;
using SkyrimMelee.Combat;

namespace SkyrimMelee.Tests;

public class ReactionClipTests
{
	private static IReadOnlyList<SwordClip> Clips => SwordClips.Create();
	private static SwordClip Clip(string name) => Clips.Single(c => c.Name == name);
	private static SwordPose Idle => Clip("SlashLeft").Keys[0].Pose;
	private static readonly Matrix Hand = Matrix.CreateTranslation(SwordClips.HandAnchor);
	private static Vector3 Tip(SwordPose pose) => BladeGeometry.FromPose(pose, Hand).Tip;

	[Theory]
	[InlineData("StaggerSmall", 0.60f)]
	[InlineData("StaggerLarge", 1.00f)]
	[InlineData("Death", 1.20f)]
	[InlineData("Victory", 1.60f)]
	public void ReactionClipsMatchTheTuning(string name, float duration)
	{
		var clip = Clip(name);

		Assert.Equal(duration, clip.Duration, 4);
		Assert.False(clip.Loop);
		Assert.True(SamePose(Idle, clip.Keys[0].Pose), $"{name} must start at the idle pose.");
	}

	[Fact]
	public void StaggerSmallTipsTheBladeThenRecovers()
	{
		var clip = Clip("StaggerSmall");
		var away = clip.Keys.Max(key => Vector3.Distance(Tip(key.Pose), Tip(Idle)));

		Assert.True(away > 0.4f, $"StaggerSmall tip moves only {away:0.00} m.");
		Assert.True(SamePose(Idle, clip.Keys[^1].Pose));
	}

	[Fact]
	public void StaggerLargeTipsTheBladeFurtherThanSmall()
	{
		var small = MaxTipAway(Clip("StaggerSmall"));
		var large = MaxTipAway(Clip("StaggerLarge"));

		Assert.True(large > small + 0.3f, $"large={large:0.00} small={small:0.00}");
	}

	[Fact]
	public void DeathEndsWithTheBladeOnTheGround()
	{
		var clip = Clip("Death");
		var tip = Tip(clip.Keys[^1].Pose);

		Assert.True(tip.Y < 0.1f, $"Death tip y={tip.Y:0.00}");
		Assert.False(SamePose(Idle, clip.Keys[^1].Pose), "Death must not settle back to idle.");
	}

	[Fact]
	public void VictoryEndsWithTheBladeRaised()
	{
		var clip = Clip("Victory");
		var tip = Tip(clip.Keys[^1].Pose);

		Assert.True(tip.Y > 2.8f, $"Victory tip y={tip.Y:0.00}");
		Assert.False(SamePose(Idle, clip.Keys[^1].Pose), "Victory must not settle back to idle.");
	}

	private static float MaxTipAway(SwordClip clip) =>
		clip.Keys.Max(key => Vector3.Distance(Tip(key.Pose), Tip(Idle)));

	private static bool SamePose(SwordPose a, SwordPose b) =>
		Vector3.Distance(a.GripOffset, b.GripOffset) < 1e-5f &&
		MathF.Abs(Quaternion.Dot(a.Rotation, b.Rotation)) > 0.99999f;
}
