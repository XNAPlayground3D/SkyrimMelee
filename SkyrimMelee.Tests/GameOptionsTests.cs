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
	public void ProfileSwitchAcceptsKnownNames()
	{
		Assert.True(GameOptions.TryParse(
			new[] { "--profile=master" }, out var options, out var error));

		Assert.Null(error);
		Assert.Equal("master", options.AiProfile);
	}

	[Fact]
	public void UnknownProfileIsRejected()
	{
		Assert.False(GameOptions.TryParse(
			new[] { "--profile=dragon" }, out _, out var error));
		Assert.NotNull(error);
	}

	[Fact]
	public void PlaySwitchSkipsTheMenu()
	{
		Assert.True(GameOptions.TryParse(
			new[] { "--play" }, out var options, out var error));

		Assert.Null(error);
		Assert.True(options.Play);
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
