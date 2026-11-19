using Microsoft.Xna.Framework;

namespace SkyrimMelee.Core;

/// <summary>Small shared geometry helpers.</summary>
public static class GeometryMath
{
	/// <summary>Quaternion that maps <see cref="Vector3.UnitY"/> to the given direction.</summary>
	public static Quaternion RotationFromY(Vector3 direction)
	{
		var dir = Vector3.Normalize(direction);
		var dot = Math.Clamp(Vector3.Dot(Vector3.UnitY, dir), -1f, 1f);

		if (dot > 0.9999f)
		{
			return Quaternion.Identity;
		}

		if (dot < -0.9999f)
		{
			return Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI);
		}

		var axis = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, dir));
		return Quaternion.CreateFromAxisAngle(axis, MathF.Acos(dot));
	}
}
