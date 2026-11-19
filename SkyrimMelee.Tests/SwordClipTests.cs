using Microsoft.Xna.Framework;
using SkyrimMelee.Combat;

namespace SkyrimMelee.Tests;

public class SwordClipTests
{
	private static SwordPose Pose(float offsetX, float yaw) =>
		new(new Vector3(offsetX, 0f, 0f), Quaternion.CreateFromYawPitchRoll(yaw, 0f, 0f));

	private static SwordClip BuildClip(bool loop = false) => new("Test", 1f, loop,
		new SwordKey(0f, Pose(0f, 0f), Easing.Linear),
		new SwordKey(0.5f, Pose(1f, 0.5f), Easing.Linear),
		new SwordKey(1f, Pose(2f, 1f), Easing.Linear));

	[Fact]
	public void StartAndEndReturnExactKeyPoses()
	{
		var clip = BuildClip();

		var start = clip.Sample(0f);
		var end = clip.Sample(clip.Duration);

		Assert.Equal(clip.Keys[0].Pose.GripOffset, start.GripOffset);
		Assert.Equal(clip.Keys[0].Pose.Rotation, start.Rotation);
		Assert.Equal(clip.Keys[^1].Pose.GripOffset, end.GripOffset);
		Assert.Equal(clip.Keys[^1].Pose.Rotation, end.Rotation);
	}

	[Fact]
	public void KeyTimeReturnsThatKey()
	{
		var clip = BuildClip();

		var middle = clip.Sample(0.5f);

		Assert.Equal(clip.Keys[1].Pose.GripOffset, middle.GripOffset);
		Assert.Equal(clip.Keys[1].Pose.Rotation, middle.Rotation);
	}

	[Fact]
	public void OneShotClampsPastTheEnd()
	{
		var clip = BuildClip();

		Assert.Equal(clip.Keys[^1].Pose.GripOffset, clip.Sample(2f).GripOffset);
	}

	[Fact]
	public void LoopWrapsToTheStart()
	{
		var clip = BuildClip(loop: true);

		Assert.Equal(clip.Keys[0].Pose.GripOffset, clip.Sample(1f).GripOffset);
		Assert.Equal(clip.Keys[1].Pose.GripOffset, clip.Sample(1.5f).GripOffset);
	}

	[Theory]
	[InlineData(Easing.Linear, 0.5f)]
	[InlineData(Easing.EaseIn, 0.25f)]
	[InlineData(Easing.EaseOut, 0.75f)]
	[InlineData(Easing.SmoothStep, 0.5f)]
	public void EasingCurvesAreApplied(Easing easing, float expected)
	{
		Assert.Equal(expected, SwordClip.ApplyEasing(easing, 0.5f), 5);
	}

	[Fact]
	public void InvalidClipsAreRejected()
	{
		var pose = SwordPose.Identity;

		Assert.Throws<ArgumentException>(() => new SwordClip("Too short", 1f, false,
			new SwordKey(0f, pose)));
		Assert.Throws<ArgumentException>(() => new SwordClip("Bad start", 1f, false,
			new SwordKey(0.1f, pose), new SwordKey(1f, pose)));
		Assert.Throws<ArgumentException>(() => new SwordClip("Bad end", 1f, false,
			new SwordKey(0f, pose), new SwordKey(0.9f, pose)));
		Assert.Throws<ArgumentException>(() => new SwordClip("Duplicate times", 1f, false,
			new SwordKey(0f, pose), new SwordKey(0f, pose), new SwordKey(1f, pose)));
		Assert.Throws<ArgumentOutOfRangeException>(() => new SwordClip("Zero duration", 0f, false,
			new SwordKey(0f, pose), new SwordKey(1f, pose)));
	}
}
