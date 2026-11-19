using Microsoft.Xna.Framework;
using SkyrimMelee.Tuning;

namespace SkyrimMelee.Animation;

/// <summary>
/// Authored rigid body reactions. Offsets are fighter-local; rotation is
/// Euler X/Y/Z in degrees. The primitive body never bends or articulates.
/// </summary>
public static class BodyClips
{
	private const float DegreesToRadians = MathF.PI / 180f;

	public static IReadOnlyList<BodyClip> Create() => new[]
	{
		StaggerMicro(),
		StaggerSmall(),
		StaggerLarge(),
		Death(),
		Victory()
	};

	private static BodyClip StaggerMicro()
	{
		var duration = CombatTuning.StaggerMicroDuration;
		var jolt = Pose(-3f, 0f, 2f);

		return new BodyClip("StaggerMicro", duration,
			Key(0f, BodyPose.Identity, Easing.EaseOut),
			Key(0.08f / duration, jolt),
			Key(1f, BodyPose.Identity));
	}

	private static BodyClip StaggerSmall()
	{
		var duration = CombatTuning.StaggerSmallDuration;

		// A light hit rocks the whole body to its left and slightly back,
		// then it straightens up with the sword.
		var lean = Pose(-10f, 0f, 5f);
		var settle = Pose(-4f, 0f, 2f);

		return new BodyClip("StaggerSmall", duration,
			Key(0f, BodyPose.Identity, Easing.EaseOut),
			Key(0.12f / duration, lean),
			Key(0.30f / duration, settle),
			Key(1f, BodyPose.Identity));
	}

	private static BodyClip StaggerLarge()
	{
		var duration = CombatTuning.StaggerLargeDuration;

		// A power hit throws the body hard to its left and back before it
		// hauls itself upright.
		var jolt = Pose(-20f, 0f, 8f);
		var recover = Pose(-7f, 0f, 3f);

		return new BodyClip("StaggerLarge", duration,
			Key(0f, BodyPose.Identity, Easing.EaseOut),
			Key(0.14f / duration, jolt),
			Key(0.70f / duration, recover),
			Key(1f, BodyPose.Identity));
	}

	private static BodyClip Death()
	{
		var duration = CombatTuning.DeathDuration;

		// The body rocks back, then topples left about its feet. The lift keeps
		// the lowest primitive (the legs capsule) resting on the floor.
		var recoil = Pose(0f, 0f, 6f);
		var falling = Pose(-35f, 0f, 0f, 0.024f);
		var low = Pose(-75f, 0f, 0f, 0.096f);
		var floor = Pose(-88f, 0f, 0f, 0.126f);

		return new BodyClip("Death", duration,
			Key(0f, BodyPose.Identity, Easing.EaseOut),
			Key(0.18f / duration, recoil, Easing.EaseIn),
			Key(0.55f / duration, falling, Easing.EaseIn),
			Key(0.85f / duration, low, Easing.EaseIn),
			Key(1f, floor));
	}

	private static BodyClip Victory()
	{
		var duration = CombatTuning.VictoryDuration;

		// The body rocks back and to its left, chest out, then holds.
		var rise = Pose(-3f, 0f, 6f);
		var proud = Pose(-6f, 0f, 12f);

		return new BodyClip("Victory", duration,
			Key(0f, BodyPose.Identity, Easing.EaseIn),
			Key(0.35f / duration, rise),
			Key(0.55f / duration, proud),
			Key(1f, proud));
	}

	private static BodyPose Pose(float xDegrees, float yDegrees, float zDegrees,
		float lift = 0f)
	{
		var radians = new Vector3(xDegrees, yDegrees, zDegrees) * DegreesToRadians;
		return new BodyPose(
			new Vector3(0f, lift, 0f),
			Quaternion.CreateFromYawPitchRoll(radians.Y, radians.X, radians.Z));
	}

	private static BodyKey Key(float time, BodyPose pose, Easing ease = Easing.SmoothStep) =>
		new(time, pose, ease);
}
