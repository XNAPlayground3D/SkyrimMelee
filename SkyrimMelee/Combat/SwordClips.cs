using Microsoft.Xna.Framework;

namespace SkyrimMelee.Combat;

/// <summary>
/// Placeholder clip library for the T2.1 dev harness. Attack clips are authored in T2.2.
/// </summary>
public static class SwordClips
{
	public static IReadOnlyList<SwordClip> Create() => new[]
	{
		new SwordClip("Idle", 2.5f, true,
			Key(0f, 0f, 0.18f, -0.85f),
			Key(1f, 0f, 0.18f, -0.85f)),

		new SwordClip("GuardRaise", 0.35f, false,
			Key(0f, 0f, 0.18f, -0.85f, Easing.EaseOut),
			Key(1f, 0f, 0.05f, -0.35f)),

		new SwordClip("SweepLeft", 0.8f, false,
			Key(0f, 0f, 0.18f, -0.85f, Easing.EaseIn),
			Key(0.35f, -0.6f, 0.30f, -0.30f),
			Key(0.55f, 0.5f, 0.0f, -1.05f),
			Key(1f, 0.9f, -0.1f, -1.25f)),

		new SwordClip("Overhead", 1.0f, false,
			Key(0f, 0f, 0.18f, -0.85f, Easing.EaseIn),
			Key(0.4f, 0f, 0.55f, -0.10f),
			Key(0.6f, 0f, -0.25f, -0.95f),
			Key(1f, 0f, -0.45f, -1.20f))
	};

	private static SwordKey Key(float time, float yaw, float pitch, float roll,
		Easing ease = Easing.SmoothStep) =>
		new(time,
			new SwordPose(Vector3.Zero, Quaternion.CreateFromYawPitchRoll(yaw, pitch, roll)),
			ease);
}
