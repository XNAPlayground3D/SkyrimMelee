using Microsoft.Xna.Framework;
using SkyrimMelee.Graphics;

namespace SkyrimMelee.Tests;

public class CameraTests
{
	private static readonly Vector3 Focus = new(-1f, 0f, 0f);

	[Fact]
	public void StartsBehindThePlayerLookingForward()
	{
		var camera = new Camera();
		camera.Update(Focus, 0f);

		Assert.True(camera.Eye.X < Focus.X);
		Assert.True(camera.LookAt.X > Focus.X);
		Assert.True(camera.Eye.Y > Focus.Y);
		Assert.Equal(0f, camera.Yaw, 4);
	}

	[Fact]
	public void MouseLookTurnsAndClampsPitch()
	{
		var camera = new Camera();
		var startYaw = camera.Yaw;

		camera.AddLook(100f, 0f);
		Assert.True(camera.Yaw > startYaw);

		camera.AddLook(0f, -100000f);
		Assert.Equal(1.15f, camera.Pitch, 3);

		camera.AddLook(0f, 100000f);
		Assert.Equal(-1.15f, camera.Pitch, 3);
	}

	[Fact]
	public void FollowsTheFocus()
	{
		var camera = new Camera();
		camera.Update(Focus, 0f);
		var firstEye = camera.Eye;

		camera.Update(Focus + new Vector3(5f, 0f, 0f), 0f);

		Assert.True(camera.Eye.X > firstEye.X + 4f);
		Assert.Equal(firstEye.Y, camera.Eye.Y, 3);
	}

	[Fact]
	public void ToggleSwitchesBetweenThirdAndFirstPerson()
	{
		var camera = new Camera();
		Assert.False(camera.FirstPerson);

		camera.ToggleMode();
		Assert.True(camera.FirstPerson);

		camera.ToggleMode();
		Assert.False(camera.FirstPerson);
	}

	[Fact]
	public void FirstPersonSitsAtEyeHeightLookingForward()
	{
		var camera = new Camera();
		camera.ToggleMode();
		camera.Update(Focus, 0f);

		Assert.Equal(Focus.X, camera.Eye.X, 3);
		Assert.Equal(Focus.Z, camera.Eye.Z, 3);
		Assert.Equal(Focus.Y + Camera.FirstPersonEyeHeight, camera.Eye.Y, 3);
		Assert.True(camera.LookAt.X > camera.Eye.X);

		var third = new Camera();
		third.Update(Focus, 0f);
		Assert.NotEqual(third.Eye.Y, camera.Eye.Y, 3);
	}

	[Fact]
	public void DistanceClampsToTheZoomRange()
	{
		var camera = new Camera
		{
			Distance = 100f
		};

		Assert.Equal(Camera.MaxDistance, camera.Distance, 3);

		camera.Distance = -5f;
		Assert.Equal(Camera.MinDistance, camera.Distance, 3);
	}

	[Fact]
	public void WheelZoomsTheThirdPersonDistance()
	{
		var camera = new Camera();
		var start = camera.Distance;

		camera.Zoom(1f);
		Assert.True(camera.Distance < start);

		camera.Zoom(1000f);
		Assert.Equal(Camera.MinDistance, camera.Distance, 3);

		camera.Zoom(-1000f);
		Assert.Equal(Camera.MaxDistance, camera.Distance, 3);
	}

	[Fact]
	public void WheelZoomsTheFirstPersonFov()
	{
		var camera = new Camera();
		camera.ToggleMode();

		Assert.Equal(Camera.FirstPersonFov, camera.FieldOfView, 4);

		camera.Zoom(1f);
		Assert.True(camera.FieldOfView < Camera.FirstPersonFov);

		camera.Zoom(1000f);
		Assert.Equal(Camera.MinFirstPersonFov, camera.FieldOfView, 4);

		camera.Zoom(-1000f);
		Assert.Equal(Camera.MaxFirstPersonFov, camera.FieldOfView, 4);
	}
}
