using SkyrimMelee.Input;

namespace SkyrimMelee.Tests;

public class SlashComboTests
{
	[Fact]
	public void TapsAlternateSides()
	{
		var combo = new SlashCombo();

		Assert.Equal(CombatIntent.SlashLeft, combo.Next());
		Assert.Equal(CombatIntent.SlashRight, combo.Next());
		Assert.Equal(CombatIntent.SlashLeft, combo.Next());
		Assert.Equal(CombatIntent.SlashRight, combo.Next());
	}

	[Fact]
	public void ResetStartsFromTheLeftAgain()
	{
		var combo = new SlashCombo();
		combo.Next();
		combo.Next();

		combo.Reset();

		Assert.Equal(CombatIntent.SlashLeft, combo.Next());
	}
}
