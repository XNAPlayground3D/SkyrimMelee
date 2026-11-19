using Microsoft.Xna.Framework;

namespace SkyrimMelee.Combat;

/// <summary>
/// Authored sword clips. Poses use a fighter-local translation (the grip position
/// next to the fighter) plus rotation in degrees around X (pitch), Y (yaw), and
/// Z (roll).
/// </summary>
public static class SwordClips
{
	// The fighter's sword-hand attachment in fighter-local space (+X forward, +Y up, +Z right).
	private static readonly Vector3 SwordHand = new(0.54f, 0.98f, 0.26f);

	private const float DegreesToRadians = MathF.PI / 180f;

	/// <summary>Fighter-local sword-hand attachment; the mirror uses it to reflect around the body centre.</summary>
	public static Vector3 HandAnchor => SwordHand;

	public static IReadOnlyList<SwordClip> Create() => new[]
	{
		SlashLeft(),
		SlashRight(),
		PowerSlash(),
		Bash(),
		GuardRaise(),
		GuardHold(),
		GuardLower(),
		StaggerMicro(),
		StaggerSmall(),
		StaggerLarge(),
		Death(),
		Victory()
	};

	private static SwordPose IdlePose() => Pose(
		new Vector3(0.50f, 1.02f, 0.28f),
		new Vector3(2.9f, -90f, 0f));

	private static SwordPose GuardPose() => Pose(
		new Vector3(0.55f, 1.05f, 0.08f),
		new Vector3(9.7f, 90f, -26.8f));

	private static SwordClip SlashLeft()
	{
		var duration = CombatTuning.Slash.Total;
		var windupEnd = CombatTuning.Slash.Windup / duration;
		var activeEnd = (CombatTuning.Slash.Windup + CombatTuning.Slash.Active) / duration;

		// Idle: hand at the right hip, blade nearly vertical.
		var guard = IdlePose();

		// Wind-up: the hands carry the whole sword right and behind the body.
		var windupPull = Pose(
			new Vector3(0.05f, 1.15f, 0.55f),
			new Vector3(5.9f, -90f, -11.6f));
		var windup = Pose(
			new Vector3(-0.50f, 1.25f, 0.85f),
			new Vector3(14.7f, -90f, -10.4f));

		// Active: the hands sweep forward/left and the edge leads the cut.
		var swingEarly = Pose(
			new Vector3(0.48f, 1.15f, 0.58f),
			new Vector3(84.2f, 42.2f, 2f));
		var contact = Pose(
			new Vector3(0.72f, 1.08f, -0.19f),
			new Vector3(84f, -55.2f, -162.8f));
		var activeEndPose = Pose(
			new Vector3(0.43f, 1.00f, -0.61f),
			new Vector3(71.4f, -16.3f, -160.4f));

		// Recovery: the hands finish left and the blade settles down-left.
		var follow = Pose(
			new Vector3(-0.06f, 0.95f, -0.75f),
			new Vector3(54.5f, -16.3f, 161.4f));

		return new SwordClip("SlashLeft", duration, false,
			Key(0f, guard, Easing.EaseIn),
			Key(0.12f / duration, windupPull),
			Key(0.14f / duration, windup, Easing.Linear),
			Key(windupEnd, windup, Easing.EaseIn),
			Key(0.32f / duration, swingEarly, Easing.EaseIn),
			Key(0.39f / duration, contact, Easing.EaseOut),
			Key(activeEnd, activeEndPose, Easing.SmoothStep),
			Key(0.62f / duration, follow),
			Key(1f, guard));
	}

	private static SwordClip SlashRight()
	{
		var left = SlashLeft();
		var last = left.Keys.Count - 1;
		var keys = left.Keys
			.Select((key, index) => new SwordKey(
				key.Time,
				index == 0 || index == last ? IdlePose() : MirrorPose(key.Pose),
				key.EaseToNext))
			.ToArray();

		return new SwordClip("SlashRight", left.Duration, false, keys);
	}

	/// <summary>Reflects a pose across the fighter's X-Y plane.</summary>
	private static SwordPose MirrorPose(SwordPose pose)
	{
		var offset = new Vector3(
			pose.GripOffset.X,
			pose.GripOffset.Y,
			-pose.GripOffset.Z - 2f * SwordHand.Z);
		var mirror = Matrix.CreateScale(1f, 1f, -1f);
		var rotation = mirror * Matrix.CreateFromQuaternion(pose.Rotation) * mirror;

		return new SwordPose(offset, Quaternion.CreateFromRotationMatrix(rotation));
	}

	private static SwordClip PowerSlash()
	{
		var duration = CombatTuning.PowerSlash.Total;
		var windupEnd = CombatTuning.PowerSlash.Windup / duration;
		var activeEnd = (CombatTuning.PowerSlash.Windup + CombatTuning.PowerSlash.Active) / duration;

		// Power overhead slam: the hilt rises high above the head with the blade
		// back/up, holds, then drives forward and down with a deep follow-through.
		// Roll-only Euler keeps the cutting edge in the chop plane, not the flat.
		var windupPull = Pose(
			new Vector3(0.15f, 1.60f, 0.20f),
			new Vector3(0f, 0f, 25f));
		var windup = Pose(
			new Vector3(0f, 2.05f, 0.15f),
			new Vector3(0f, 0f, 40f));
		var swingEarly = Pose(
			new Vector3(0.45f, 1.85f, 0.15f),
			new Vector3(0f, 0f, -40f));
		var contact = Pose(
			new Vector3(0.85f, 1.15f, 0.10f),
			new Vector3(0f, 0f, -130f));
		var activeEndPose = Pose(
			new Vector3(0.75f, 0.90f, 0.10f),
			new Vector3(0f, 0f, -120f));
		var follow = Pose(
			new Vector3(0.50f, 0.78f, 0.15f),
			new Vector3(0f, 0f, -110f));

		return new SwordClip("PowerSlash", duration, false,
			Key(0f, IdlePose(), Easing.EaseIn),
			Key(0.42f / duration, windupPull),
			Key(0.50f / duration, windup, Easing.Linear),
			Key(windupEnd, windup, Easing.EaseIn),   // 100 ms telegraph hold
			Key(0.64f / duration, swingEarly, Easing.EaseIn),
			Key(0.70f / duration, contact, Easing.EaseOut),
			Key(activeEnd, activeEndPose, Easing.SmoothStep),
			Key(0.98f / duration, follow),
			Key(1f, IdlePose()));
	}

	private static SwordClip Bash()
	{
		var duration = CombatTuning.Bash.Total;
		var windupEnd = CombatTuning.Bash.Windup / duration;
		var activeEnd = (CombatTuning.Bash.Windup + CombatTuning.Bash.Active) / duration;

		// A short guard shove: pull the hilt into the chest, hold the telegraph,
		// then drive the flat forward and return to the guard.
		var guard = GuardPose();
		var pull = Pose(
			new Vector3(0.38f, 1.10f, 0.06f),
			new Vector3(12f, 90f, -20f));
		var windup = Pose(
			new Vector3(0.34f, 1.12f, 0.06f),
			new Vector3(12f, 90f, -18f));
		var thrust = Pose(
			new Vector3(0.95f, 1.12f, 0.05f),
			new Vector3(4f, 90f, -6f));
		var follow = Pose(
			new Vector3(0.78f, 1.08f, 0.06f),
			new Vector3(7f, 90f, -14f));

		return new SwordClip("Bash", duration, false,
			Key(0f, guard, Easing.EaseIn),
			Key(0.04f / duration, pull),
			Key(0.06f / duration, windup),
			Key(windupEnd, windup, Easing.EaseIn),   // 100 ms telegraph hold
			Key(0.20f / duration, thrust, Easing.EaseOut),
			Key(activeEnd, thrust),
			Key(0.42f / duration, follow),
			Key(1f, guard));
	}

	private static SwordClip GuardRaise()
	{
		var anticipation = Pose(
			new Vector3(0.36f, 1.00f, 0.32f),
			new Vector3(5.8f, -90f, -2.3f));

		return new SwordClip("GuardRaise", CombatTuning.GuardRaiseDuration, false,
			Key(0f, IdlePose(), Easing.EaseOut),
			Key(0.3f, anticipation, Easing.EaseIn),
			Key(1f, GuardPose()));
	}

	private static SwordClip GuardHold()
	{
		var breath = Pose(
			new Vector3(0.55f, 1.06f, 0.075f),
			new Vector3(10.9f, 90f, -25.8f));

		return new SwordClip("GuardHold", CombatTuning.GuardHoldDuration, true,
			Key(0f, GuardPose()),
			Key(0.5f, breath),
			Key(1f, GuardPose()));
	}

	private static SwordClip GuardLower()
	{
		var settle = Pose(
			new Vector3(0.38f, 1.00f, 0.31f),
			new Vector3(-0.6f, -90f, 1.7f));

		return new SwordClip("GuardLower", CombatTuning.GuardLowerDuration, false,
			Key(0f, GuardPose(), Easing.EaseIn),
			Key(0.7f, settle, Easing.EaseOut),
			Key(1f, IdlePose()));
	}

	private static SwordClip StaggerMicro()
	{
		var duration = CombatTuning.StaggerMicroDuration;

		// A short recoil after a timed block: a small dip and a quick recovery.
		var jolt = Pose(
			new Vector3(0.38f, 1.00f, 0.36f),
			new Vector3(3f, -90f, -8f));

		return new SwordClip("StaggerMicro", duration, false,
			Key(0f, IdlePose(), Easing.EaseOut),
			Key(0.08f / duration, jolt),
			Key(1f, IdlePose()));
	}

	private static SwordClip StaggerSmall()
	{
		var duration = CombatTuning.StaggerSmallDuration;

		// A light hit snaps the blade off to the right and dips the hilt; the
		// sword settles back to the idle guard as the fighter recovers.
		var jolt = Pose(
			new Vector3(0.35f, 1.00f, 0.42f),
			new Vector3(0f, -90f, -25f));
		var sag = Pose(
			new Vector3(0.42f, 0.95f, 0.36f),
			new Vector3(5f, -90f, -15f));

		return new SwordClip("StaggerSmall", duration, false,
			Key(0f, IdlePose(), Easing.EaseOut),
			Key(0.12f / duration, jolt),
			Key(0.30f / duration, sag),
			Key(1f, IdlePose()));
	}

	private static SwordClip StaggerLarge()
	{
		var duration = CombatTuning.StaggerLargeDuration;

		// A power hit throws the blade diagonally down and out; the body does
		// most of the staggering, so the sword stops short of horizontal.
		var jolt = Pose(
			new Vector3(0.30f, 1.05f, 0.42f),
			new Vector3(0f, -90f, -35f));
		var droop = Pose(
			new Vector3(0.42f, 0.88f, 0.42f),
			new Vector3(0f, -90f, -55f));
		var recovering = Pose(
			new Vector3(0.45f, 1.00f, 0.32f),
			new Vector3(4f, -90f, -20f));

		return new SwordClip("StaggerLarge", duration, false,
			Key(0f, IdlePose(), Easing.EaseOut),
			Key(0.14f / duration, jolt),
			Key(0.38f / duration, droop),
			Key(0.70f / duration, recovering),
			Key(1f, IdlePose()));
	}

	private static SwordClip Death()
	{
		var duration = CombatTuning.DeathDuration;

		// The sword is knocked up, falls forward out of the hand, and lands
		// flat on the floor with the whole blade down. The clip holds there.
		var hit = Pose(
			new Vector3(0.30f, 1.20f, 0.30f),
			new Vector3(0f, -90f, -12f));
		var drop = Pose(
			new Vector3(0.55f, 0.75f, 0.30f),
			new Vector3(0f, 0f, -110f));
		var ground = Pose(
			new Vector3(0.80f, 0.06f, 0.30f),
			new Vector3(0f, 0f, -90f));

		return new SwordClip("Death", duration, false,
			Key(0f, IdlePose(), Easing.EaseOut),
			Key(0.18f / duration, hit),
			Key(0.55f / duration, drop, Easing.EaseIn),
			Key(1f, ground));
	}

	private static SwordClip Victory()
	{
		var duration = CombatTuning.VictoryDuration;

		// The blade rises straight overhead and holds there in triumph.
		var raise = Pose(
			new Vector3(0.35f, 1.50f, 0.30f),
			new Vector3(0f, 0f, 10f));
		var peak = Pose(
			new Vector3(0.30f, 1.85f, 0.30f),
			new Vector3(0f, 0f, 0f));

		return new SwordClip("Victory", duration, false,
			Key(0f, IdlePose(), Easing.EaseIn),
			Key(0.35f / duration, raise),
			Key(0.55f / duration, peak),
			Key(1f, peak));
	}

	private static SwordKey Key(float time, SwordPose pose, Easing ease = Easing.SmoothStep) =>
		new(time, pose, ease);

	/// <summary>
	/// Builds a pose from the grip position in fighter-local space (the translation
	/// next to the fighter) and Euler rotation in degrees: X = pitch, Y = yaw,
	/// Z = roll.
	/// </summary>
	private static SwordPose Pose(Vector3 translation, Vector3 rotationDegrees)
	{
		var radians = rotationDegrees * DegreesToRadians;
		var rotation = Quaternion.CreateFromYawPitchRoll(radians.Y, radians.X, radians.Z);
		return new SwordPose(translation - SwordHand, rotation);
	}
}
