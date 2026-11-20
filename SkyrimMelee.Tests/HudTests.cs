using SkyrimMelee.Ui;

namespace SkyrimMelee.Tests;

public class HudTests
{
	[Theory]
	[InlineData(0f, false)]
	[InlineData(0.1f, true)]
	[InlineData(0.2f, false)]
	[InlineData(0.4f, true)]
	[InlineData(0.5f, false)]
	public void StaminaFlashBlinksWhileTheCueIsFresh(float cueSeconds, bool expected)
	{
		Assert.Equal(expected, HudMath.StaminaFlashActive(cueSeconds));
	}
}
