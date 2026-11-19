using Microsoft.Xna.Framework;
using SkyrimMelee.Combat;

namespace SkyrimMelee.Tests;

public class SwingHitSetTests
{
	private static AttackDefinition Attack =>
		AttackCatalog.Create(SwordClips.Create()).Single(a => a.Name == "PowerSlash");

	private static BladeHit Hit(
		HurtboxKind kind, FighterId fighter = FighterId.Opponent, float time = 0.01f) =>
		new(
			new Hurtbox(fighter, kind),
			new Vector3(0.9f, 1.2f, 0.1f),
			Vector3.UnitX,
			time);

	[Fact]
	public void SwingRegistersEachHurtboxOnce()
	{
		var attack = Attack;
		var set = new SwingHitSet();
		set.Begin(attack);
		var time = attack.WindupEnd + attack.Tuning.Active * 0.5f;

		Assert.True(set.TryRegister(Hit(HurtboxKind.Torso), FighterId.Player, time, out var torso));
		Assert.False(set.TryRegister(Hit(HurtboxKind.Torso), FighterId.Player, time, out _));

		Assert.True(set.TryRegister(Hit(HurtboxKind.Head), FighterId.Player, time, out var head));
		Assert.False(set.TryRegister(Hit(HurtboxKind.Head), FighterId.Player, time, out _));

		Assert.Equal(2, set.Count);
		Assert.Equal(HurtboxKind.Torso, torso.Hurtbox);
		Assert.Equal(HurtboxKind.Head, head.Hurtbox);
		Assert.Equal(FighterId.Player, torso.Attacker);
		Assert.Equal(FighterId.Opponent, torso.Target);
		Assert.Equal(attack, torso.Attack);
		Assert.Equal(0.5f, torso.ActiveProgress, 3);
		Assert.Equal(Vector3.UnitX, torso.Normal);
	}

	[Fact]
	public void NewSwingClearsTheSet()
	{
		var attack = Attack;
		var set = new SwingHitSet();
		set.Begin(attack);

		Assert.True(set.TryRegister(Hit(HurtboxKind.Torso), FighterId.Player,
			attack.ActiveEnd - 0.01f, out _));

		set.Begin(attack);
		Assert.Equal(0, set.Count);
		Assert.True(set.TryRegister(Hit(HurtboxKind.Torso), FighterId.Player,
			attack.ActiveEnd - 0.01f, out _));
	}

	[Fact]
	public void DifferentTargetsWithTheSameHurtboxAreSeparate()
	{
		var attack = Attack;
		var set = new SwingHitSet();
		set.Begin(attack);

		Assert.True(set.TryRegister(
			Hit(HurtboxKind.Torso, FighterId.Opponent), FighterId.Player, 0.7f, out _));
		Assert.True(set.TryRegister(
			Hit(HurtboxKind.Torso, FighterId.Player), FighterId.Player, 0.7f, out _));
		Assert.Equal(2, set.Count);
	}

	[Fact]
	public void OnlyTheFirstHurtboxOfASwingDealsDamage()
	{
		var attack = Attack;
		var set = new SwingHitSet();
		set.Begin(attack);

		Assert.True(set.TryRegister(Hit(HurtboxKind.Hips), FighterId.Player, 0.7f, out _));
		Assert.True(set.TryMarkDamagingHit());

		Assert.True(set.TryRegister(Hit(HurtboxKind.Legs), FighterId.Player, 0.7f, out _));
		Assert.False(set.TryMarkDamagingHit());

		set.Begin(attack);
		Assert.True(set.TryMarkDamagingHit());
	}

	[Fact]
	public void ProgressClampsToTheActiveWindow()
	{
		var attack = Attack;
		var set = new SwingHitSet();

		set.Begin(attack);
		Assert.True(set.TryRegister(Hit(HurtboxKind.Torso), FighterId.Player,
			attack.WindupEnd - 0.1f, out var before));
		Assert.Equal(0f, before.ActiveProgress);

		set.Begin(attack);
		Assert.True(set.TryRegister(Hit(HurtboxKind.Head), FighterId.Player,
			attack.ActiveEnd + 0.1f, out var after));
		Assert.Equal(1f, after.ActiveProgress);
	}

	[Fact]
	public void ResetRejectsHits()
	{
		var set = new SwingHitSet();
		set.Begin(Attack);
		set.Reset();

		Assert.False(set.TryRegister(Hit(HurtboxKind.Torso), FighterId.Player, 0.7f, out _));
	}
}
