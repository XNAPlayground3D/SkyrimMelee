using DigitalRiseModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SkyrimMelee.Graphics;

/// <summary>
/// Fixed duel camera with a slow idle sway and a shake hook for impacts.
/// </summary>
public sealed class Camera
{
	private const float SwayDegrees = 0.6f;
	private const float SwayHertz = 0.15f;

	private Vector3 _eye;
	private Vector3 _lookAt;
	private float _shakeAmplitude;
	private float _shakeDuration;
	private float _shakeRemaining;
	private float _shakeTime;

	public Camera()
	{
		_eye = Position;
		_lookAt = Target;
	}

	public Vector3 Position { get; set; } = new(0.3f, 2.6f, -5.2f);
	public Vector3 Target { get; set; } = new(0f, 1.0f, 0f);
	public float FieldOfView { get; set; } = MathHelper.ToRadians(44f);
	public float NearPlane { get; set; } = 0.1f;
	public float FarPlane { get; set; } = 100f;

	public Matrix View => Matrix.CreateLookAt(_eye, _lookAt, Vector3.Up);

	public Matrix GetProjection(float aspect) =>
		Matrix.CreatePerspectiveFieldOfView(FieldOfView, aspect, NearPlane, FarPlane);

	/// <summary>Advances sway and decays any active shake.</summary>
	public void Update(double simTime, float elapsedSeconds)
	{
		var yaw = MathHelper.ToRadians(SwayDegrees) * MathF.Sin((float)(simTime * MathF.Tau * SwayHertz));
		var eye = Target + Vector3.Transform(Position - Target, Matrix.CreateRotationY(yaw));

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
		_lookAt = Target + shake * 0.5f;
	}

	/// <summary>Screen shake hook; used by hit resolution in T5.2.</summary>
	public void AddShake(float amplitude, float duration)
	{
		_shakeAmplitude = MathF.Max(_shakeAmplitude, amplitude);
		_shakeDuration = MathF.Max(_shakeDuration, duration);
		_shakeRemaining = MathF.Max(_shakeRemaining, duration);
	}
}
