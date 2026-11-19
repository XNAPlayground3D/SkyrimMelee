using Microsoft.Xna.Framework;

namespace SkyrimMelee.Combat;

/// <summary>
/// Authored sword clips. This task ships only SlashLeft; the other attacks are
/// separate tasks. Poses are authored as grip positions plus blade directions so
/// a swing traces a body-sized arc instead of pivoting in the wrist.
/// </summary>
public static class SwordClips
{
	// The fighter's sword-hand attachment in fighter-local space (+X forward, +Y up, +Z right).
	private static readonly Vector3 SwordHand = new(0.54f, 0.98f, 0.26f);

	public static IReadOnlyList<SwordClip> Create() => new[]
	{
		SlashLeft(),
		GuardRaise(),
		GuardHold(),
		GuardLower()
	};

	private static SwordPose IdlePose() => Pose(
		new Vector3(0.40f, 1.02f, 0.28f),
		new Vector3(-0.05f, 0.999f, 0f),
		new Vector3(0f, 0f, 1f));

	private static SwordPose GuardPose() => Pose(
		new Vector3(0.55f, 1.05f, 0.08f),
		new Vector3(0.15f, 0.88f, -0.45f),
		new Vector3(0f, 0f, -1f));

	private static SwordClip SlashLeft()
	{
		var duration = CombatTuning.Slash.Total;
		var windupEnd = CombatTuning.Slash.Windup / duration;
		var activeEnd = (CombatTuning.Slash.Windup + CombatTuning.Slash.Active) / duration;

		// Skyrim-style idle: hand at the right hip, blade nearly vertical.
		var guard = IdlePose();

		// The hands orbit the chest. The blade points radially outward, so the hilt
		// moves tangentially and the tip cuts a wide arc through the opponent's space.
		// Wind-up: the hands carry the whole sword right and behind the body. The blade
		// stays near-vertical and keeps the same roll as the idle, so the sword translates
		// instead of twisting in the wrist; the tip ends almost behind the fighter's back.
		var windupPull = Pose(
			new Vector3(0.05f, 1.15f, 0.55f),
			new Vector3(-0.10f, 0.97f, 0.20f),
			new Vector3(0f, 0f, 1f));
		var windup = Pose(
			new Vector3(-0.50f, 1.25f, 0.85f),
			new Vector3(-0.25f, 0.95f, 0.18f),
			new Vector3(0f, 0f, 1f));

		// Active: the hands drive forward and across the chest. The edge hint rolls
		// with the swing so the cutting edge leads all the way through.
		var swingEarly = Pose(
			new Vector3(0.48f, 1.15f, 0.58f),
			new Vector3(0.64f, 0.10f, 0.76f),
			new Vector3(0.74f, 0f, -0.67f));
		var contact = Pose(
			new Vector3(0.72f, 1.08f, -0.19f),
			new Vector3(0.95f, -0.10f, -0.30f),
			new Vector3(-0.57f, 0f, -0.82f));
		var activeEndPose = Pose(
			new Vector3(0.43f, 1.00f, -0.61f),
			new Vector3(0.57f, -0.30f, -0.76f),
			new Vector3(-0.96f, 0f, -0.28f));

		// Recovery: the hands finish on the left and the blade settles down-left.
		var follow = Pose(
			new Vector3(-0.06f, 0.95f, -0.75f),
			new Vector3(-0.09f, -0.55f, -0.83f),
			new Vector3(-0.96f, 0f, -0.28f));

		return new SwordClip("SlashLeft", duration, false,
			Key(0f, guard, Easing.EaseIn),
			Key(0.12f / duration, windupPull),
			Key(0.14f / duration, windup, Easing.Linear),
			Key(windupEnd, windup, Easing.EaseIn),   // 110 ms telegraph hold
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
			new Vector3(-0.10f, 0.99f, 0.04f),
			new Vector3(0f, 0f, 1f));

		return new SwordClip("GuardRaise", CombatTuning.GuardRaiseDuration, false,
			Key(0f, IdlePose(), Easing.EaseOut),
			Key(0.3f, anticipation, Easing.EaseIn),
			Key(1f, GuardPose()));
	}

	private static SwordClip GuardHold()
	{
		var breath = Pose(
			new Vector3(0.55f, 1.06f, 0.075f),
			new Vector3(0.17f, 0.885f, -0.435f),
			new Vector3(0f, 0f, -1f));

		return new SwordClip("GuardHold", CombatTuning.GuardHoldDuration, true,
			Key(0f, GuardPose()),
			Key(0.5f, breath),
			Key(1f, GuardPose()));
	}

	private static SwordClip GuardLower()
	{
		var settle = Pose(
			new Vector3(0.38f, 1.00f, 0.31f),
			new Vector3(0.01f, 0.999f, -0.03f),
			new Vector3(0f, 0f, 1f));

		return new SwordClip("GuardLower", CombatTuning.GuardLowerDuration, false,
			Key(0f, GuardPose(), Easing.EaseIn),
			Key(0.7f, settle, Easing.EaseOut),
			Key(1f, IdlePose()));
	}

	private static SwordKey Key(float time, SwordPose pose, Easing ease = Easing.SmoothStep) =>
		new(time, pose, ease);

	/// <summary>
	/// Builds a pose from a fighter-local grip position, the direction the blade
	/// points, and an edge hint. The hint is projected perpendicular to the blade,
	/// so it controls the roll: the edge leads the cut without twisting the blade.
	/// </summary>
	private static SwordPose Pose(Vector3 grip, Vector3 blade, Vector3 edgeHint)
	{
		var y = Vector3.Normalize(blade);
		var x = edgeHint - Vector3.Dot(edgeHint, y) * y;
		if (x.LengthSquared() < 1e-6f)
		{
			x = Vector3.Cross(y, Vector3.UnitY);
		}

		x = Vector3.Normalize(x);
		var z = Vector3.Cross(x, y);

		// Rows are the images of X, Y, Z under a row-vector rotation.
		var basis = new Matrix(
			x.X, x.Y, x.Z, 0f,
			y.X, y.Y, y.Z, 0f,
			z.X, z.Y, z.Z, 0f,
			0f, 0f, 0f, 1f);

		return new SwordPose(grip - SwordHand, Quaternion.CreateFromRotationMatrix(basis));
	}
}
