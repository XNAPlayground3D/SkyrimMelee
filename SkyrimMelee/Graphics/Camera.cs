using Microsoft.Xna.Framework;

namespace SkyrimMelee.Graphics;

/// <summary>
/// Third-person orbit camera over the player's shoulder. Mouse look drives yaw
/// and pitch; the target follows the player. Impact shake is layered on top.
/// </summary>
public sealed class Camera
{
	public const float LookSensitivity = 0.0022f;
	public const float DefaultDistance = 5.0f;
	public const float MinDistance = 2.5f;
	public const float MaxDistance = 7.0f;

	private const float TargetHeight = 1.35f;
	private const float LookAhead = 1.0f;
	private const float Shoulder = 0.6f;
	private const float MinPitch = -1.15f;
	private const float MaxPitch = 1.15f;

	private float _yaw;
	private float _pitch = -0.32f;
	private float _distance = DefaultDistance;
	private Vector3 _eye;
	private Vector3 _lookAt;
	private float _shakeAmplitude;
	private float _shakeDuration;
	private float _shakeRemaining;
	private float _shakeTime;

	public float FieldOfView { get; set; } = MathHelper.ToRadians(44f);
	public float NearPlane { get; set; } = 0.1f;
	public float FarPlane { get; set; } = 100f;

	public float Yaw => _yaw;
	public float Pitch => _pitch;

	public float Distance
	{
		get => _distance;
		set => _distance = Math.Clamp(value, MinDistance, MaxDistance);
	}

	public Vector3 Eye => _eye;
	public Vector3 LookAt => _lookAt;

	public Matrix View => Matrix.CreateLookAt(_eye, _lookAt, Vector3.Up);

	public Matrix GetProjection(float aspect) =>
		Matrix.CreatePerspectiveFieldOfView(FieldOfView, aspect, NearPlane, FarPlane);

	public void AddLook(float deltaX, float deltaY)
	{
		_yaw += deltaX * LookSensitivity;
		_pitch = Math.Clamp(_pitch - deltaY * LookSensitivity, MinPitch, MaxPitch);

		if (_yaw > MathHelper.TwoPi)
		{
			_yaw -= MathHelper.TwoPi;
		}
		else if (_yaw < -MathHelper.TwoPi)
		{
			_yaw += MathHelper.TwoPi;
		}
	}

	/// <summary>Follows the focus (the player's feet) and advances shake decay.</summary>
	public void Update(Vector3 focus, float elapsedSeconds)
	{
		var horizontal = new Vector3(MathF.Cos(_yaw), 0f, MathF.Sin(_yaw));
		var target = focus + new Vector3(0f, TargetHeight, 0f) + horizontal * LookAhead;

		var forward = new Vector3(
			MathF.Cos(_pitch) * MathF.Cos(_yaw),
			MathF.Sin(_pitch),
			MathF.Cos(_pitch) * MathF.Sin(_yaw));

		var right = Vector3.Cross(forward, Vector3.Up);
		if (right.LengthSquared() > 1e-6f)
		{
			right.Normalize();
		}

		var eye = target - forward * _distance + right * Shoulder;

		var shake = Vector3.Zero;
		if (_shakeRemaining > 0f)
		{
			_shakeRemaining = MathF.Max(0f, _shakeRemaining - elapsedSeconds);
			_shakeTime += elapsedSeconds;

			var fade = _shakeDuration > 0f ? _shakeRemaining / _shakeDuration : 0f;
			var amplitude = _shakeAmplitude * fade;
			shake = new Vector3(
				MathF.Sin(_shakeTime * 63f),
				MathF.Cos(_shakeTime * 47f),
				0f) * amplitude;
		}

		_eye = eye + shake;
		_lookAt = target + shake * 0.5f;
	}

	/// <summary>Screen shake hook; used by hit resolution in T5.2.</summary>
	public void AddShake(float amplitude, float duration)
	{
		_shakeAmplitude = MathF.Max(_shakeAmplitude, amplitude);
		_shakeDuration = MathF.Max(_shakeDuration, duration);
		_shakeRemaining = MathF.Max(_shakeRemaining, duration);
	}
}
