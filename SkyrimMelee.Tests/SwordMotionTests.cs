using Microsoft.Xna.Framework;
using SkyrimMelee.Combat;

namespace SkyrimMelee.Tests;

public class SwordMotionTests
{
	private static SwordPose Pose(float offsetX) =>
		new(new Vector3(offsetX, 0f, 0f), Quaternion.Identity);

	private static SwordClip Clip(string name, float duration, bool loop = false, params (float Time, float X)[] keys) =>
		new(name, duration, loop,
			keys.Select(k => new SwordKey(k.Time, Pose(k.X), Easing.Linear)).ToArray());

	private static SwordClip FirstClip() => Clip("A", 0.8f, false, (0f, 0f), (1f, 1f));
	private static SwordClip SecondClip() => Clip("B", 0.5f, false, (0f, 2f), (1f, 3f));

	private static void AssertPoseEquals(SwordPose expected, SwordPose actual, int precision = 4)
	{
		Assert.Equal(expected.GripOffset.X, actual.GripOffset.X, precision);
		Assert.Equal(expected.GripOffset.Y, actual.GripOffset.Y, precision);
		Assert.Equal(expected.GripOffset.Z, actual.GripOffset.Z, precision);
		Assert.True(MathF.Abs(Quaternion.Dot(expected.Rotation, actual.Rotation)) > 0.9999f);
	}

	[Fact]
	public void PlayStartsAtClipStart()
	{
		var motion = new SwordMotion();
		var clip = FirstClip();

		motion.Play(clip);

		Assert.Equal(0f, motion.CurrentPose.GripOffset.X, 5);
		Assert.Equal(0f, motion.NormalizedTime, 5);
		Assert.Same(clip, motion.Clip);
	}

	[Fact]
	public void OneShotClampsAndFinishes()
	{
		var motion = new SwordMotion();
		var clip = FirstClip();
		motion.Play(clip);

		motion.Update(2f);

		Assert.True(motion.IsFinished);
		Assert.Equal(1f, motion.CurrentPose.GripOffset.X, 4);
		Assert.Equal(1f, motion.NormalizedTime, 4);
	}

	[Fact]
	public void PlayDoesNotSnapAndBlendReachesTheNewClip()
	{
		var motion = new SwordMotion();
		var first = FirstClip();
		var second = SecondClip();

		motion.Play(first);
		motion.Update(0.2f);
		var beforeSwitch = motion.CurrentPose;

		motion.Play(second);

		// No immediate jump: the old pose is held until the next update blends it.
		AssertPoseEquals(beforeSwitch, motion.CurrentPose);

		motion.Update(0.05f);
		var midBlend = motion.CurrentPose;
		Assert.NotEqual(beforeSwitch.GripOffset.X, midBlend.GripOffset.X, 4);

		motion.Update(0.05f);
		var expected = second.Sample(0.1f);
		AssertPoseEquals(expected, motion.CurrentPose);

		Assert.Equal(SwordMotion.CrossfadeDuration, 0.1f, 5);
	}

	[Fact]
	public void LoopingClipWrapsTime()
	{
		var motion = new SwordMotion();
		var clip = Clip("Loop", 1f, true, (0f, 0f), (1f, 1f));
		motion.Play(clip);

		motion.Update(0.25f);
		Assert.Equal(0.25f, motion.CurrentPose.GripOffset.X, 5);

		motion.Update(1f);
		Assert.Equal(0.25f, motion.CurrentPose.GripOffset.X, 5);
		Assert.False(motion.IsFinished);
	}

	[Fact]
	public void PlayingTheSameClipWithoutRestartIsIgnored()
	{
		var motion = new SwordMotion();
		var clip = FirstClip();
		motion.Play(clip);
		motion.Update(0.2f);

		motion.Play(clip);

		Assert.Equal(0.2f, motion.Time, 5);
	}
}
