using SkyrimMelee.Core;

namespace SkyrimMelee.Tests;

public class GameOptionsTests
{
	[Fact]
	public void ScreenShakeIsOnByDefault()
	{
		Assert.True(new GameOptions().ScreenShake);
	}

	[Fact]
	public void NoShakeSwitchDisablesIt()
	{
		Assert.True(GameOptions.TryParse(
			new[] { "--no-shake" }, out var options, out var error));

		Assert.Null(error);
		Assert.False(options.ScreenShake);
	}
}
