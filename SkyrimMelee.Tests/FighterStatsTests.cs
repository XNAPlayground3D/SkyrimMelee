using SkyrimMelee.Combat;

namespace SkyrimMelee.Tests;

public class FighterStatsTests
{
	private static AttackDefinition Attack(string name) =>
		AttackCatalog.Create(SwordClips.Create()).Single(attack => attack.Name == name);

	[Fact]
	public void PowerAttackNeedsItsFullCost()
	{
		var power = Attack("PowerSlash");
		var stats = new FighterStats(stamina: 29f);

		Assert.False(stats.CanAfford(power));
		Assert.Equal(29f, stats.Stamina, 4);

		stats.SetStamina(30f);
		Assert.True(stats.CanAfford(power));
		stats.Spend(power);
		Assert.Equal(0f, stats.Stamina, 4);
		Assert.False(stats.IsStaminaRegenerating);
	}

	[Fact]
	public void LightAttackIsAlwaysAllowedAndClamps()
	{
		var slash = Attack("SlashLeft");
		var stats = new FighterStats(stamina: 2f);

		Assert.True(stats.CanAfford(slash));
		stats.Spend(slash);
		Assert.Equal(0f, stats.Stamina, 4);
	}

	[Fact]
	public void RegenStartsAfterTheDelay()
	{
		var stats = new FighterStats(stamina: 50f);
		stats.Spend(Attack("SlashLeft"));

		stats.Update(0.6f, blocking: false);
		Assert.Equal(38f, stats.Stamina, 4);

		stats.Update(0.2f, blocking: false);
		Assert.Equal(43f, stats.Stamina, 4);
	}

	[Fact]
	public void BlockingHoldsTheRegenDelay()
	{
		var stats = new FighterStats(stamina: 50f);

		stats.Update(1f, blocking: true);
		Assert.Equal(50f, stats.Stamina, 4);

		stats.Update(0.6f, blocking: false);
		Assert.Equal(50f, stats.Stamina, 4);

		stats.Update(0.4f, blocking: false);
		Assert.Equal(60f, stats.Stamina, 4);
	}

	[Fact]
	public void PoiseRegeneratesAfterOneSecond()
	{
		var stats = new FighterStats();
		stats.TakePoiseDamage(40f);

		Assert.Equal(60f, stats.Poise, 4);

		stats.Update(1f, blocking: false);
		Assert.Equal(60f, stats.Poise, 4);

		stats.Update(0.4f, blocking: false);
		Assert.Equal(70f, stats.Poise, 4);
	}

	[Fact]
	public void PoiseDamageClampsAndRestartsTheDelay()
	{
		var stats = new FighterStats();
		stats.TakePoiseDamage(150f);
		Assert.Equal(0f, stats.Poise, 4);

		stats.Update(0.9f, blocking: false);
		stats.TakePoiseDamage(10f);
		stats.Update(0.9f, blocking: false);
		Assert.Equal(0f, stats.Poise, 4);

		stats.Update(0.2f, blocking: false);
		Assert.True(stats.Poise > 0f);
	}

	[Fact]
	public void DamageClampsAtZeroAndDeath()
	{
		var stats = new FighterStats();

		stats.ApplyDamage(40f);
		Assert.Equal(110f, stats.Health, 4);
		Assert.False(stats.IsDead);

		stats.ApplyDamage(200f);
		Assert.Equal(0f, stats.Health, 4);
		Assert.True(stats.IsDead);

		stats.Reset();
		Assert.Equal(CombatTuning.MaxHealth, stats.Health, 4);
		Assert.False(stats.IsDead);
	}

	[Fact]
	public void ResetRestoresResources()
	{
		var stats = new FighterStats(stamina: 10f);
		stats.Spend(Attack("SlashLeft"));
		stats.TakePoiseDamage(50f);

		stats.Reset();

		Assert.Equal(CombatTuning.MaxStamina, stats.Stamina, 4);
		Assert.Equal(CombatTuning.MaxPoise, stats.Poise, 4);
		Assert.Equal(CombatTuning.MaxHealth, stats.Health, 4);
	}
}
