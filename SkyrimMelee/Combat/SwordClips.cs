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

	public static IReadOnlyList<SwordClip> Create() => new[]
	{
		SlashLeft(),
		GuardRaise(),
		GuardHold(),
		GuardLower()
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
