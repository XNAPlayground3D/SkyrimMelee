using Microsoft.Xna.Framework;

namespace SkyrimMelee.Combat;

public enum Easing
{
	Linear,
	EaseIn,
	EaseOut,
	SmoothStep
}

public readonly record struct SwordPose(Vector3 GripOffset, Quaternion Rotation)
{
	public static SwordPose Identity => new(Vector3.Zero, Quaternion.Identity);

	public Matrix ToMatrix() =>
		Matrix.CreateFromQuaternion(Rotation) * Matrix.CreateTranslation(GripOffset);

	public static SwordPose Lerp(SwordPose from, SwordPose to, float amount) =>
		new(Vector3.Lerp(from.GripOffset, to.GripOffset, amount),
			Quaternion.Slerp(from.Rotation, to.Rotation, amount));
}

public readonly record struct SwordKey(float Time, SwordPose Pose, Easing EaseToNext = Easing.SmoothStep);

/// <summary>
/// A keyframed sword path. Normalized key times run 0..1; sampling is in seconds.
/// </summary>
public sealed class SwordClip
{
	private readonly SwordKey[] _keys;

	public SwordClip(string name, float duration, bool loop, params SwordKey[] keys)
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
		Loop = loop;
		_keys = keys;
	}

	public string Name { get; }
	public float Duration { get; }
	public bool Loop { get; }
	public IReadOnlyList<SwordKey> Keys => _keys;

	public SwordPose Sample(float timeSeconds)
	{
		var t = timeSeconds / Duration;

		if (Loop)
		{
			t -= MathF.Floor(t);
		}
		else
		{
			t = Math.Clamp(t, 0f, 1f);
		}

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
			u = ApplyEasing(from.EaseToNext, Math.Clamp(u, 0f, 1f));

			if (u <= 0f)
			{
				return from.Pose;
			}

			if (u >= 1f)
			{
				return to.Pose;
			}

			return SwordPose.Lerp(from.Pose, to.Pose, u);
		}

		return _keys[^1].Pose;
	}

	public static float ApplyEasing(Easing easing, float u) => easing switch
	{
		Easing.EaseIn => u * u,
		Easing.EaseOut => 1f - (1f - u) * (1f - u),
		Easing.SmoothStep => u * u * (3f - 2f * u),
		_ => u
	};
}
