using SkyrimMelee.Animation;
using SkyrimMelee.Attacks;
using SkyrimMelee.Combat;
using SkyrimMelee.Input;

namespace SkyrimMelee.Tests;

public class SlashComboTests
{
	private static IReadOnlyList<AttackDefinition> Attacks =>
		AttackCatalog.Create(SwordClips.Create());

	private static AttackDefinition Attack(string name) =>
		Attacks.Single(a => a.Name == name);

	[Fact]
	public void TapFromIdleAlwaysStartsTheLeftCut()
	{
		Assert.Equal(CombatIntent.SlashLeft, SlashCombo.Next(CombatState.Idle, null));
		Assert.Equal(CombatIntent.SlashLeft,
			SlashCombo.Next(CombatState.Idle, Attack("SlashLeft")));
		Assert.Equal(CombatIntent.SlashLeft,
			SlashCombo.Next(CombatState.GuardHold, Attack("SlashRight")));
	}

	[Theory]
	[InlineData(CombatState.Windup)]
	[InlineData(CombatState.Active)]
	[InlineData(CombatState.Recovery)]
	public void ChainedTapAnswersFromTheOtherSide(CombatState state)
	{
		Assert.Equal(CombatIntent.SlashRight,
			SlashCombo.Next(state, Attack("SlashLeft")));
		Assert.Equal(CombatIntent.SlashLeft,
			SlashCombo.Next(state, Attack("SlashRight")));
	}

	[Fact]
	public void ChainedTapPastAPowerStartsLeft()
	{
		Assert.Equal(CombatIntent.SlashLeft,
			SlashCombo.Next(CombatState.Recovery, Attack("PowerSlash")));
	}
}
