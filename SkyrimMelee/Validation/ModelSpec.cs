using Microsoft.Xna.Framework;
using SkyrimMelee.Tuning;

namespace SkyrimMelee.Validation;

/// <summary>A joint in the fighter's required skeleton, in world rest space.</summary>
public readonly record struct JointSpec(string Name, string? Parent, Vector3? RestPosition);

/// <summary>A required animation clip and its authoritative duration.</summary>
public readonly record struct ClipSpec(string Name, float Duration, bool Loop, bool Clamp);

/// <summary>
/// The T9.1 contract in code. Durations come from <see cref="CombatTuning"/>;
/// the rest mirrors <c>Design/models.md</c>.
/// </summary>
public static class ModelSpec
{
	public const string FighterFile = "fighter.glb";
	public const string SwordFile = "sword.glb";

	public const float FighterHeight = 1.80f;
	public const float EyeHeight = 1.60f;
	public const float LengthTolerance = 0.01f;
	public const float RestTolerance = 0.01f;
	public const float WeightTolerance = 0.001f;
	public const float TimeTolerance = 0.001f;

	public const int MaxFighterTriangles = 20_000;
	public const int MaxSwordTriangles = 4_000;
	public const int MaxInfluences = 4;
	public const int MaxJoints = 72;
	public const int MaxMaterials = 3;

	public const float SwordBaseY = 0.18f;
	public const float SwordTipY = 1.47f;
	public const float SwordBaseRadius = 0.0425f;
	public const float SwordTipRadius = 0.018f;
	public const float SwordCrossguardWidth = 0.30f;

	public static readonly int[] FighterTextureSizes = [1024, 2048];
	public static readonly int[] SwordTextureSizes = [1024];

	private static readonly JointSpec[] FighterJoints =
	[
		new("root", null, new Vector3(0f, 0f, 0f)),
		new("pelvis", "root", new Vector3(0f, 0.95f, 0f)),
		new("spine_01", "pelvis", new Vector3(0f, 1.08f, 0f)),
		new("spine_02", "spine_01", new Vector3(0f, 1.25f, 0f)),
		new("neck", "spine_02", new Vector3(0f, 1.45f, 0f)),
		new("head", "neck", new Vector3(0f, 1.58f, 0f)),
		new("clavicle_l", "spine_02", new Vector3(-0.18f, 1.40f, -0.18f)),
		new("upperarm_l", "clavicle_l", new Vector3(-0.18f, 1.40f, -0.18f)),
		new("forearm_l", "upperarm_l", new Vector3(-0.18f, 1.40f, -0.48f)),
		new("hand_l", "forearm_l", new Vector3(-0.18f, 1.40f, -0.78f)),
		new("clavicle_r", "spine_02", new Vector3(0.18f, 1.40f, 0.18f)),
		new("upperarm_r", "clavicle_r", new Vector3(0.18f, 1.40f, 0.18f)),
		new("forearm_r", "upperarm_r", new Vector3(0.18f, 1.40f, 0.48f)),
		new("hand_r", "forearm_r", new Vector3(0.18f, 1.40f, 0.78f)),
		new("thigh_l", "pelvis", new Vector3(-0.12f, 0.92f, 0f)),
		new("calf_l", "thigh_l", new Vector3(-0.12f, 0.48f, 0f)),
		new("foot_l", "calf_l", new Vector3(-0.12f, 0.08f, 0f)),
		new("thigh_r", "pelvis", new Vector3(0.12f, 0.92f, 0f)),
		new("calf_r", "thigh_r", new Vector3(0.12f, 0.48f, 0f)),
		new("foot_r", "calf_r", new Vector3(0.12f, 0.08f, 0f)),
		new("weapon_r", "hand_r", null)
	];

	private static readonly ClipSpec[] Clips =
	[
		new("Idle", CombatTuning.IdleDuration, Loop: true, Clamp: false),
		new("GuardRaise", CombatTuning.GuardRaiseDuration, Loop: false, Clamp: false),
		new("GuardHold", CombatTuning.GuardHoldDuration, Loop: true, Clamp: false),
		new("GuardLower", CombatTuning.GuardLowerDuration, Loop: false, Clamp: false),
		new("WalkForward", CombatTuning.WalkCycleDuration, Loop: true, Clamp: false),
		new("WalkBackward", CombatTuning.WalkCycleDuration, Loop: true, Clamp: false),
		new("StrafeLeft", CombatTuning.WalkCycleDuration, Loop: true, Clamp: false),
		new("StrafeRight", CombatTuning.WalkCycleDuration, Loop: true, Clamp: false),
		new("SlashLeft", CombatTuning.Slash.Total, Loop: false, Clamp: false),
		new("SlashRight", CombatTuning.Slash.Total, Loop: false, Clamp: false),
		new("PowerSlash", CombatTuning.PowerSlash.Total, Loop: false, Clamp: false),
		new("Bash", CombatTuning.Bash.Total, Loop: false, Clamp: false),
		new("StaggerMicro", CombatTuning.StaggerMicroDuration, Loop: false, Clamp: false),
		new("StaggerSmall", CombatTuning.StaggerSmallDuration, Loop: false, Clamp: false),
		new("StaggerLarge", CombatTuning.StaggerLargeDuration, Loop: false, Clamp: false),
		new("Death", CombatTuning.DeathDuration, Loop: false, Clamp: true),
		new("Victory", CombatTuning.VictoryDuration, Loop: false, Clamp: true)
	];

	public static IReadOnlyList<JointSpec> Joints => FighterJoints;

	public static IReadOnlyList<ClipSpec> AnimationClips => Clips;

	public static JointSpec? FindJoint(string name)
	{
		foreach (var joint in FighterJoints)
		{
			if (joint.Name == name)
			{
				return joint;
			}
		}

		return null;
	}

	public static ClipSpec? FindClip(string name)
	{
		foreach (var clip in Clips)
		{
			if (clip.Name == name)
			{
				return clip;
			}
		}

		return null;
	}
}
