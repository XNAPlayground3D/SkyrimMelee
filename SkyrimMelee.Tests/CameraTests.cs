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
}
