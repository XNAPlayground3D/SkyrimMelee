using Microsoft.Xna.Framework;
using SkyrimMelee.Combat;
using SkyrimMelee.Tuning;

namespace SkyrimMelee.Tests;

public class HealthPotionTests
{
	private static readonly Vector3 Away = new(0f, 0f, 4.5f);

	[Fact]
	public void FirstPotionSpawnsAfterTheInitialDelay()
	{
		var potions = new HealthPotions(seed: 3);

		potions.Update(CombatTuning.PotionInitialDelay - 0.1f, Away, Away);
		Assert.Empty(potions.Positions);

		potions.Update(0.1f, Away, Away);
		Assert.Single(potions.Positions);
	}

	[Fact]
	public void SpawnsWithinFiveSecondsAtSixtyHz()
	{
		var potions = new HealthPotions(seed: 20261121);
		for (var i = 0; i < 330; i++)
		{
			potions.Update(
				1f / 60f, new Vector3(-1f, 0f, 0f), new Vector3(1f, 0f, 0f));
		}

		Assert.NotEmpty(potions.Positions);
	}

	[Fact]
	public void StopsAtTheActiveCap()
	{
		var potions = new HealthPotions(seed: 3);

		for (var i = 0; i < 10; i++)
		{
			potions.Update(CombatTuning.PotionSpawnInterval, Away, Away);
		}

		Assert.Equal(CombatTuning.PotionMaxActive, potions.Positions.Count);
	}

	[Fact]
	public void SpawnsStayInsideTheSpawnRing()
	{
		var potions = new HealthPotions(seed: 9);

		for (var i = 0; i < 20; i++)
		{
			potions.Update(CombatTuning.PotionSpawnInterval, Away, Away);
			foreach (var position in potions.Positions)
			{
				var radius = MathF.Sqrt(position.X * position.X + position.Z * position.Z);
				Assert.InRange(
					radius,
					CombatTuning.PotionSpawnMinRadius,
					CombatTuning.PotionSpawnMaxRadius);
			}
		}
	}

	[Fact]
	public void SameSeedProducesTheSameSpawns()
	{
		var first = new HealthPotions(seed: 42);
		var second = new HealthPotions(seed: 42);

		for (var i = 0; i < 20; i++)
		{
			first.Update(CombatTuning.PotionSpawnInterval, Away, Away);
			second.Update(CombatTuning.PotionSpawnInterval, Away, Away);
			Assert.Equal(first.Positions, second.Positions);
		}
	}

	[Fact]
	public void NearestFighterTakesThePotion()
	{
		var positions = new[] { new Vector3(0.5f, 0f, 0f) };

		Assert.True(HealthPotions.TryTake(
			positions, Vector3.Zero, new Vector3(5f, 0f, 0f),
			out var index, out var playerTakes));
		Assert.Equal(0, index);
		Assert.True(playerTakes);
	}

	[Fact]
	public void OpponentTakesWhenCloser()
	{
		var positions = new[] { new Vector3(4.5f, 0f, 0f) };

		Assert.True(HealthPotions.TryTake(
			positions, Vector3.Zero, new Vector3(5f, 0f, 0f),
			out _, out var playerTakes));
		Assert.False(playerTakes);
	}

	[Fact]
	public void OutOfRangePotionStaysOnTheFloor()
	{
		var positions = new[] { new Vector3(2f, 0f, 0f) };

		Assert.False(HealthPotions.TryTake(
			positions, Vector3.Zero, new Vector3(5f, 0f, 0f),
			out _, out _));
	}

	[Fact]
	public void HealingStopsAtMaxHealth()
	{
		var stats = new FighterStats();
		stats.ApplyDamage(100f);

		stats.Heal(CombatTuning.PotionHeal);
		Assert.Equal(
			CombatTuning.MaxHealth - 100f + CombatTuning.PotionHeal,
			stats.Health,
			3);

		stats.Heal(1000f);
		Assert.Equal(CombatTuning.MaxHealth, stats.Health, 3);
	}
}
