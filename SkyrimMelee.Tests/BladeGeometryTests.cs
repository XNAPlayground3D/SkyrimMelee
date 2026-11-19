using Microsoft.Xna.Framework;
using SkyrimMelee.Combat;

namespace SkyrimMelee.Tests;

public class BladeGeometryTests
{
	[Fact]
	public void WorldPointsFollowTheSwordTransform()
	{
		var hand = Matrix.CreateRotationY(0.4f) * Matrix.CreateTranslation(1f, 2f, 3f);
		var pose = new SwordPose(
			new Vector3(0.2f, -0.1f, 0.3f),
			Quaternion.CreateFromYawPitchRoll(0.2f, 0.3f, 0.4f));
		var swordWorld = pose.ToMatrix() * hand;
		var blade = BladeGeometry.FromPose(pose, hand);

		var grip = Vector3.Transform(Vector3.Zero, swordWorld);
		var basePoint = Vector3.Transform(new Vector3(0f, BladeGeometry.BladeBaseY, 0f), swordWorld);
		var tip = Vector3.Transform(new Vector3(0f, BladeGeometry.BladeTipY, 0f), swordWorld);

		Assert.True(Vector3.Distance(blade.Grip, grip) < 1e-5f);
		Assert.True(Vector3.Distance(blade.Base, basePoint) < 1e-5f);
		Assert.True(Vector3.Distance(blade.Tip, tip) < 1e-5f);
	}

	[Fact]
	public void EveryClipSampleKeepsTheBladeRigid()
	{
		var hand = Matrix.CreateRotationY(0.7f) * Matrix.CreateTranslation(-1f, 0f, 0.2f);
		var expectedTipDistance = BladeGeometry.BladeTipY;
		var expectedLength = BladeGeometry.BladeTipY - BladeGeometry.BladeBaseY;

		foreach (var clip in SwordClips.Create())
		{
			const int steps = 20;

			for (var i = 0; i <= steps; i++)
			{
				var time = clip.Duration * i / steps;
				var blade = BladeGeometry.FromPose(clip.Sample(time), hand);

				Assert.True(
					MathF.Abs(Vector3.Distance(blade.Grip, blade.Tip) - expectedTipDistance) < 1e-4f,
					$"{clip.Name} at {time:0.000}s: grip-tip distance");
				Assert.True(
					MathF.Abs(blade.Length - expectedLength) < 1e-4f,
					$"{clip.Name} at {time:0.000}s: base-tip length");
				Assert.True(
					MathF.Abs(blade.Direction.Length() - 1f) < 1e-4f,
					$"{clip.Name} at {time:0.000}s: direction");
			}
		}
	}

	[Fact]
	public void CapsuleMatchesTheBladeExtents()
	{
		Assert.Equal(1.47f, BladeGeometry.BladeTipY, 4);
		Assert.True(BladeGeometry.BladeBaseY > 0f);
		Assert.True(BladeGeometry.BladeBaseY < BladeGeometry.BladeTipY);
		Assert.True(BladeGeometry.BladeBaseRadius > 0f);
		Assert.True(BladeGeometry.BladeTipRadius > 0f);
		Assert.True(BladeGeometry.BladeBaseRadius > BladeGeometry.BladeTipRadius);
	}

	[Fact]
	public void AxisRotationMapsYToTheBladeAxis()
	{
		var pose = new SwordPose(
			Vector3.Zero,
			Quaternion.CreateFromYawPitchRoll(0.1f, 0.4f, 0.9f));
		var blade = BladeGeometry.FromPose(pose, Matrix.Identity);
		var mapped = Vector3.TransformNormal(
			Vector3.UnitY,
			Matrix.CreateFromQuaternion(blade.AxisRotation));

		Assert.True(Vector3.Distance(mapped, blade.Direction) < 1e-4f);
	}
}
