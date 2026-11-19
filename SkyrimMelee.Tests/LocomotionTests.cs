using Microsoft.Xna.Framework;
using SkyrimMelee.Combat;

namespace SkyrimMelee.Tests;

public class LocomotionTests
{
	[Fact]
	public void ClampKeepsThePlayerInsideTheArena()
	{
		var position = Locomotion.Clamp(
			new Vector3(10f, 0f, 0f), new Vector3(1f, 0f, 0f), 5.5f, 1.15f);

		Assert.Equal(5.5f, new Vector2(position.X, position.Z).Length(), 3);
	}

	[Fact]
	public void ClampPushesAwayFromTheOpponent()
	{
		var position = Locomotion.Clamp(
			new Vector3(1.1f, 0f, 0f), new Vector3(1f, 0f, 0f), 5.5f, 1.15f);

		Assert.Equal(2.15f, position.X, 3);
	}

	[Theory]
	[InlineData(CombatState.Idle, 1f)]
	[InlineData(CombatState.Recovery, 1f)]
	[InlineData(CombatState.Windup, CombatTuning.AttackMoveMultiplier)]
	[InlineData(CombatState.Active, CombatTuning.AttackMoveMultiplier)]
	[InlineData(CombatState.GuardRaise, CombatTuning.GuardMoveMultiplier)]
	[InlineData(CombatState.GuardHold, CombatTuning.GuardMoveMultiplier)]
	[InlineData(CombatState.GuardLower, CombatTuning.GuardMoveMultiplier)]
	[InlineData(CombatState.Stagger, 0f)]
	[InlineData(CombatState.Dead, 0f)]
	public void MovementMultiplierMatchesTheState(CombatState state, float expected)
	{
		Assert.Equal(expected, Locomotion.MovementMultiplier(state), 4);
	}

	[Theory]
	[InlineData(CombatState.Idle)]
	[InlineData(CombatState.Recovery)]
	[InlineData(CombatState.Windup)]
	[InlineData(CombatState.Active)]
	[InlineData(CombatState.GuardRaise)]
	[InlineData(CombatState.GuardHold)]
	[InlineData(CombatState.GuardLower)]
	public void RootedActionStopsAllMovement(CombatState state)
	{
		Assert.Equal(0f, Locomotion.MovementMultiplier(state, rootLocked: true), 4);
	}

	[Theory]
	[InlineData(0f)]
	[InlineData(0.5f)]
	[InlineData(-0.7f)]
	public void CameraFacingIsTheInverseOfTheCameraYaw(float yaw)
	{
		var forward = new Vector3(MathF.Cos(yaw), 0f, MathF.Sin(yaw));
		var facing = Locomotion.FacingFor(forward);

		Assert.Equal(-yaw, facing, 4);
	}

	[Theory]
	[InlineData(1f, 0f, 0f)]
	[InlineData(0f, 0f, -1f)]
	[InlineData(-1f, 0f, 0f)]
	public void FacingTurnsLocalForwardTowardTheMove(float x, float y, float z)
	{
		var direction = new Vector3(x, y, z);
		var facing = Locomotion.FacingFor(direction);
		var forward = new Vector3(MathF.Cos(facing), 0f, -MathF.Sin(facing));

		Assert.True(Vector3.Distance(direction, forward) < 0.001f,
			$"direction={direction} forward={forward}");
	}
}
