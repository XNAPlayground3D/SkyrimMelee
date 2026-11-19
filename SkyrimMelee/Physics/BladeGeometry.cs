using Microsoft.Xna.Framework;
using SkyrimMelee.Core;
using SkyrimMelee.Tuning;
using SkyrimMelee.Animation;

namespace SkyrimMelee.Physics;

/// <summary>
/// World-space blade segment. The same values feed the debug capsule and the
/// future physics sweep, so there is one source of blade geometry.
/// </summary>
public readonly record struct BladeGeometry(
	Vector3 Grip,
	Vector3 Base,
	Vector3 Tip,
	float BaseRadius,
	float TipRadius)
{
	// Sword local space: origin at the grip, blade along +Y.
	public const float BladeBaseY = 0.18f;
	public const float BladeTipY = 1.47f;
	public const float BladeBaseRadius = 0.0425f;
	public const float BladeTipRadius = 0.018f;

	public Vector3 Axis => Tip - Base;

	public float Length => Axis.Length();

	public Vector3 Direction
	{
		get
		{
			var axis = Axis;
			return axis.LengthSquared() < 1e-8f ? Vector3.UnitY : Vector3.Normalize(axis);
		}
	}

	/// <summary>Rotation that maps +Y to the blade axis; used to orient debug rings.</summary>
	public Quaternion AxisRotation => GeometryMath.RotationFromY(Direction);

	public static BladeGeometry FromWorld(Matrix swordWorld) => new(
		Vector3.Transform(Vector3.Zero, swordWorld),
		Vector3.Transform(new Vector3(0f, BladeBaseY, 0f), swordWorld),
		Vector3.Transform(new Vector3(0f, BladeTipY, 0f), swordWorld),
		BladeBaseRadius,
		BladeTipRadius);

	public static BladeGeometry FromPose(SwordPose pose, Matrix handWorld) =>
		FromWorld(pose.ToMatrix() * handWorld);
}
