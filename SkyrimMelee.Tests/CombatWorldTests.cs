using Microsoft.Xna.Framework;
using SkyrimMelee.Combat;

namespace SkyrimMelee.Tests;

public class CombatWorldTests
{
	[Fact]
	public void EachFighterGetsFourHurtboxes()
	{
		using var world = new CombatWorld();
		world.AddFighter(FighterId.Player, Matrix.Identity);
		world.AddFighter(FighterId.Opponent, Matrix.CreateTranslation(1f, 0f, 0f));

		Assert.Equal(8, world.Hurtboxes.Count);

		foreach (var fighter in new[] { FighterId.Player, FighterId.Opponent })
		{
			var kinds = world.Hurtboxes.Entries
				.Where(entry => entry.Value.Fighter == fighter)
				.Select(entry => entry.Value.Kind)
				.ToArray();

			Assert.Equal(4, kinds.Length);
			Assert.Contains(HurtboxKind.Head, kinds);
			Assert.Contains(HurtboxKind.Torso, kinds);
			Assert.Contains(HurtboxKind.Hips, kinds);
			Assert.Contains(HurtboxKind.Legs, kinds);
		}
	}

	[Fact]
	public void HurtboxSetMapsHandlesBackToOwners()
	{
		using var world = new CombatWorld();
		world.AddFighter(FighterId.Player, Matrix.Identity);
		world.AddFighter(FighterId.Opponent, Matrix.CreateTranslation(1f, 0f, 0f));

		foreach (var entry in world.Hurtboxes.Entries)
		{
			Assert.True(world.Hurtboxes.TryGet(entry.Key, out var hurtbox));
			Assert.Equal(entry.Value, hurtbox);
		}
	}

	[Fact]
	public void BodyTransformMovesTheHurtboxes()
	{
		using var world = new CombatWorld();
		world.AddFighter(FighterId.Player, Matrix.Identity);
		var torso = world.Hurtboxes.Entries.Single(entry => entry.Value.Kind == HurtboxKind.Torso);

		Assert.True(world.TryGetPose(torso.Key, out var center, out _));
		Assert.Equal(1.26f, center.Y, 3);

		// A 90 degree fall about X maps local +Y to +Z.
		world.UpdateFighter(FighterId.Player, Matrix.CreateRotationX(MathHelper.PiOver2) *
			Matrix.CreateTranslation(2f, 0f, 0f));

		Assert.True(world.TryGetPose(torso.Key, out center, out _));
		Assert.Equal(2f, center.X, 3);
		Assert.Equal(0f, center.Y, 3);
		Assert.Equal(1.26f, center.Z, 3);
	}

	[Fact]
	public void GuardBladeRegistersAndRemovesWithoutLeaking()
	{
		using var world = new CombatWorld();
		world.AddFighter(FighterId.Player, Matrix.Identity);

		var blade = new BladeGeometry(
			new Vector3(0.5f, 1.0f, 0.2f),
			new Vector3(0.5f, 1.2f, 0.2f),
			new Vector3(0.5f, 2.3f, 0.2f),
			0.0425f,
			0.018f);

		world.RegisterGuardBlade(FighterId.Player, blade);
		Assert.True(world.IsGuardBladeRegistered(FighterId.Player));
		Assert.Equal(5, world.Hurtboxes.Count);

		var handle = world.GuardBladeHandle(FighterId.Player)!.Value;
		Assert.True(world.Hurtboxes.TryGet(handle, out var hurtbox));
		Assert.Equal(FighterId.Player, hurtbox.Fighter);
		Assert.Equal(HurtboxKind.GuardBlade, hurtbox.Kind);

		world.RegisterGuardBlade(FighterId.Player, blade);
		Assert.Equal(5, world.Hurtboxes.Count);
		Assert.Equal(handle, world.GuardBladeHandle(FighterId.Player)!.Value);

		world.UnregisterGuardBlade(FighterId.Player);
		Assert.False(world.IsGuardBladeRegistered(FighterId.Player));
		Assert.Null(world.GuardBladeHandle(FighterId.Player));
		Assert.Equal(4, world.Hurtboxes.Count);
		Assert.False(world.TryGetPose(handle, out _, out _));
	}

	[Fact]
	public void BothFightersCanGuardIndependently()
	{
		using var world = new CombatWorld();
		world.AddFighter(FighterId.Player, Matrix.Identity);
		world.AddFighter(FighterId.Opponent, Matrix.CreateTranslation(1f, 0f, 0f));

		var blade = new BladeGeometry(
			new Vector3(0.5f, 1.0f, 0.2f),
			new Vector3(0.5f, 1.2f, 0.2f),
			new Vector3(0.5f, 2.3f, 0.2f),
			0.0425f,
			0.018f);

		world.RegisterGuardBlade(FighterId.Player, blade);
		world.RegisterGuardBlade(FighterId.Opponent, blade);

		Assert.True(world.IsGuardBladeRegistered(FighterId.Player));
		Assert.True(world.IsGuardBladeRegistered(FighterId.Opponent));
		Assert.Equal(10, world.Hurtboxes.Count);

		world.UnregisterGuardBlade(FighterId.Player);
		Assert.False(world.IsGuardBladeRegistered(FighterId.Player));
		Assert.True(world.IsGuardBladeRegistered(FighterId.Opponent));
		Assert.Equal(9, world.Hurtboxes.Count);
	}

	[Fact]
	public void GuardBladePoseFollowsTheBladeSegment()
	{
		using var world = new CombatWorld();
		world.AddFighter(FighterId.Player, Matrix.Identity);

		var basePoint = new Vector3(0.5f, 1.0f, 0.2f);
		var tip = new Vector3(1.5f, 1.8f, 0.2f);
		var blade = new BladeGeometry(basePoint, basePoint, tip, 0.0425f, 0.018f);

		world.RegisterGuardBlade(FighterId.Player, blade);
		var handle = world.GuardBladeHandle(FighterId.Player)!.Value;
		Assert.True(world.TryGetPose(handle, out var center, out var orientation));

		Assert.Equal((basePoint + tip) * 0.5f, center);
		Assert.True(world.Hurtboxes.TryGet(handle, out _));

		var axis = Vector3.Normalize(tip - basePoint);
		var posed = Vector3.TransformNormal(Vector3.UnitY, Matrix.CreateFromQuaternion(orientation));
		Assert.True(Vector3.Distance(axis, posed) < 0.001f, $"axis={axis} posed={posed}");
	}
}
