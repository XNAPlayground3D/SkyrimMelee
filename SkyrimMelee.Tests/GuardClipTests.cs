using Microsoft.Xna.Framework;
using SkyrimMelee.Combat;

namespace SkyrimMelee.Tests;

public class GuardClipTests
{
	private static IReadOnlyList<SwordClip> Clips => SwordClips.Create();
	private static SwordClip Clip(string name) => Clips.Single(c => c.Name == name);
	private static SwordPose Idle => Clip("SlashLeft").Keys[0].Pose;
	private static SwordPose Guard => Clip("GuardHold").Keys[0].Pose;

	[Fact]
	public void GuardClipsExistWithTheRightDurations()
	{
		Assert.Equal(CombatTuning.GuardRaiseDuration, Clip("GuardRaise").Duration, 4);
		Assert.Equal(CombatTuning.GuardHoldDuration, Clip("GuardHold").Duration, 4);
		Assert.Equal(CombatTuning.GuardLowerDuration, Clip("GuardLower").Duration, 4);
		Assert.False(Clip("GuardRaise").Loop);
		Assert.True(Clip("GuardHold").Loop);
		Assert.False(Clip("GuardLower").Loop);
	}

	[Fact]
	public void RaiseStartsAtIdleAndEndsAtGuard()
	{
		var raise = Clip("GuardRaise");

		Same(raise.Keys[0].Pose, Idle);
		Same(raise.Keys[^1].Pose, Guard);
		Assert.True(raise.Keys.Count >= 3, "raise needs an anticipation key");
	}

	[Fact]
	public void LowerStartsAtGuardAndEndsAtIdle()
	{
		var lower = Clip("GuardLower");

		Same(lower.Keys[0].Pose, Guard);
		Same(lower.Keys[^1].Pose, Idle);
		Assert.True(lower.Keys.Count >= 3, "lower needs a follow-through key");
	}

	[Fact]
	public void HoldStartsAndEndsAtGuard()
	{
		var hold = Clip("GuardHold");

		Same(hold.Keys[0].Pose, Guard);
		Same(hold.Keys[^1].Pose, Guard);
	}

	[Fact]
	public void HoldBreathesSubtly()
	{
		var directions = Clip("GuardHold").Keys
			.Select(k => Vector3.TransformNormal(Vector3.UnitY,
				Matrix.CreateFromQuaternion(k.Pose.Rotation)))
			.ToArray();

		var maxAngle = 0f;
		for (var i = 0; i < directions.Length; i++)
		{
			for (var j = i + 1; j < directions.Length; j++)
			{
				var dot = Math.Clamp(Vector3.Dot(directions[i], directions[j]), -1f, 1f);
				maxAngle = MathF.Max(maxAngle, MathF.Acos(dot));
			}
		}

		var degrees = MathHelper.ToDegrees(maxAngle);
		Assert.True(degrees > 0.2f && degrees < 3f, $"breathing={degrees:0.00} degrees");
	}

	[Fact]
	public void GuardIsInFrontOfTheTorso()
	{
		var blade = Vector3.TransformNormal(Vector3.UnitY,
			Matrix.CreateFromQuaternion(Guard.Rotation));

		// The hand attachment is at x = 0.54; an offset above -0.1 keeps it in front of the chest.
		Assert.True(Guard.GripOffset.X > -0.1f, $"grip x={Guard.GripOffset.X:0.000}");
		Assert.True(blade.X > 0f, $"blade x={blade.X:0.000}");
	}

	[Fact]
	public void RaiseAndLowerActuallyMove()
	{
		var raise = Clip("GuardRaise");
		var lower = Clip("GuardLower");

		Assert.False(SamePose(raise.Keys[0].Pose, raise.Keys[1].Pose));
		Assert.False(SamePose(lower.Keys[^2].Pose, lower.Keys[^1].Pose));
	}

	private static void Same(SwordPose actual, SwordPose expected) =>
		Assert.True(SamePose(actual, expected), $"actual={actual} expected={expected}");

	private static bool SamePose(SwordPose a, SwordPose b) =>
		Vector3.Distance(a.GripOffset, b.GripOffset) < 1e-5f &&
		MathF.Abs(Quaternion.Dot(a.Rotation, b.Rotation)) > 0.99999f;
}
