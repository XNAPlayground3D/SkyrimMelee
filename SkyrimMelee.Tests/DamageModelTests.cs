using SkyrimMelee.Combat;

namespace SkyrimMelee.Tests;

public class DamageModelTests
{
	private static AttackDefinition Attack(string name) =>
		AttackCatalog.Create(SwordClips.Create()).Single(attack => attack.Name == name);

	[Theory]
	[InlineData(0f, 25f)]
	[InlineData(300f, 16f)]
	[InlineData(567f, 8f)]
	[InlineData(900f, 5f)]
	public void CleanDamageMatchesArmorTable(float armor, float expected)
	{
		var result = DamageModel.Compute(
			Attack("SlashLeft"), 60f, armor, 50f, DamageKind.Clean);

		Assert.Equal(expected, result.Final, 3);
		Assert.Equal(24.8f, result.Raw, 3);
	}

	[Theory]
	[InlineData(0f, 11f)]
	[InlineData(50f, 8f)]
	[InlineData(100f, 4f)]
	public void BlockDamageMatchesSkillTable(float blockSkill, float expected)
	{
		var result = DamageModel.Compute(
			Attack("SlashLeft"), 60f, 300f, blockSkill, DamageKind.Block);

		Assert.Equal(expected, result.Final, 3);
	}

	[Theory]
	[InlineData(0f, 9f)]
	[InlineData(50f, 5f)]
	[InlineData(100f, 2f)]
	public void TimedBlockDamageMatchesSkillTable(float blockSkill, float expected)
	{
		var result = DamageModel.Compute(
			Attack("SlashLeft"), 60f, 300f, blockSkill, DamageKind.TimedBlock);

		Assert.Equal(expected, result.Final, 3);
	}

	[Fact]
	public void GuardBreakUsesArmoredDamage()
	{
		var result = DamageModel.Compute(
			Attack("SlashLeft"), 60f, 300f, 50f, DamageKind.GuardBreak);

		Assert.Equal(16f, result.Final, 3);
		Assert.Equal(15.872f, result.Armored, 3);
		Assert.Equal(15.872f * CombatTuning.BlockStaminaMultiplier, result.StaminaCost, 3);
	}

	[Fact]
	public void PowerSlashCleanMatchesTable()
	{
		var result = DamageModel.Compute(
			Attack("PowerSlash"), 60f, 300f, 50f, DamageKind.Clean);

		Assert.Equal(29f, result.Final, 3);
	}

	[Fact]
	public void ReductionPercentIsRelativeToRaw()
	{
		var result = DamageModel.Compute(
			Attack("SlashLeft"), 60f, 300f, 50f, DamageKind.Clean);

		Assert.Equal((1f - 16f / 24.8f) * 100f, result.ReductionPercent, 2);
	}

	[Fact]
	public void MinimumDamageIsOne()
	{
		var result = DamageModel.Compute(1f, 1f, 0f, 900f, 100f, DamageKind.TimedBlock);

		Assert.Equal(1f, result.Final, 3);
	}

	[Fact]
	public void BlockStaminaCostsFollowTheTable()
	{
		var block = DamageModel.Compute(
			Attack("SlashLeft"), 60f, 300f, 50f, DamageKind.Block);
		var timed = DamageModel.Compute(
			Attack("SlashLeft"), 60f, 300f, 50f, DamageKind.TimedBlock);
		var clean = DamageModel.Compute(
			Attack("SlashLeft"), 60f, 300f, 50f, DamageKind.Clean);

		Assert.Equal(15.872f * CombatTuning.BlockStaminaMultiplier, block.StaminaCost, 3);
		Assert.Equal(15.872f * CombatTuning.TimedBlockStaminaMultiplier, timed.StaminaCost, 3);
		Assert.Equal(0f, clean.StaminaCost, 3);
	}

	[Theory]
	[InlineData(2.5f, 3f)]
	[InlineData(2.4f, 2f)]
	[InlineData(0f, 1f)]
	[InlineData(-0.4f, 1f)]
	public void DamageRoundsAwayFromZeroWithMinimumOne(float value, float expected)
	{
		Assert.Equal(expected, DamageModel.RoundDamage(value), 3);
	}
}
