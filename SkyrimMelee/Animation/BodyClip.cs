using Microsoft.Xna.Framework;
using SkyrimMelee.Tuning;

namespace SkyrimMelee.Animation;

/// <summary>A rigid whole-body transform: an offset from the fighter root plus rotation about it.</summary>
public readonly record struct BodyPose(Vector3 Offset, Quaternion Rotation)
{
	public static BodyPose Identity => new(Vector3.Zero, Quaternion.Identity);

	public Matrix ToMatrix() =>
		Matrix.CreateFromQuaternion(Rotation) * Matrix.CreateTranslation(Offset);

	public static BodyPose Lerp(BodyPose from, BodyPose to, float amount) =>
		new(Vector3.Lerp(from.Offset, to.Offset, amount),
			Quaternion.Slerp(from.Rotation, to.Rotation, amount));
}

public readonly record struct BodyKey(float Time, BodyPose Pose, Easing EaseToNext = Easing.SmoothStep);

/// <summary>
/// A keyframed rigid body motion. There are no joints: the whole primitive
/// assembly rotates and shifts as one. Normalized key times run 0..1; sampling is in seconds.
/// </summary>
public sealed class BodyClip
{
	private readonly BodyKey[] _keys;

	public BodyClip(string name, float duration, params BodyKey[] keys)
	{
		if (string.IsNullOrWhiteSpace(name))
		{
			throw new ArgumentException("Clip name is required.", nameof(name));
		}

		if (duration <= 0f)
		{
			throw new ArgumentOutOfRangeException(nameof(duration));
		}

		if (keys.Length < 2)
		{
			throw new ArgumentException("A clip needs at least two keys.", nameof(keys));
		}

		if (MathF.Abs(keys[0].Time) > 0.0001f || MathF.Abs(keys[^1].Time - 1f) > 0.0001f)
		{
			throw new ArgumentException("First key must be at 0 and last key at 1.", nameof(keys));
		}

		for (var i = 1; i < keys.Length; i++)
		{
			if (keys[i].Time <= keys[i - 1].Time)
			{
				throw new ArgumentException("Key times must be strictly increasing.", nameof(keys));
			}
		}

		Name = name;
		Duration = duration;
		_keys = keys;
	}

	public string Name { get; }
	public float Duration { get; }
	public IReadOnlyList<BodyKey> Keys => _keys;

	public BodyPose Sample(float timeSeconds)
	{
		var t = Math.Clamp(timeSeconds / Duration, 0f, 1f);

		if (t <= 0f)
		{
			return _keys[0].Pose;
		}

		if (t >= 1f)
		{
			return _keys[^1].Pose;
		}

		for (var i = 0; i < _keys.Length - 1; i++)
		{
			var from = _keys[i];
			var to = _keys[i + 1];
			if (t > to.Time)
			{
				continue;
			}

			var span = to.Time - from.Time;
			var u = span <= 0f ? 0f : (t - from.Time) / span;
			u = SwordClip.ApplyEasing(from.EaseToNext, Math.Clamp(u, 0f, 1f));

			if (u <= 0f)
			{
				return from.Pose;
			}

			if (u >= 1f)
			{
				return to.Pose;
			}

			return BodyPose.Lerp(from.Pose, to.Pose, u);
		}

		return _keys[^1].Pose;
	}
}
