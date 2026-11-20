using SkyrimMelee.Ai;
using SkyrimMelee.Ui;

namespace SkyrimMelee.Tests;

public class GameSettingsTests
{
	[Fact]
	public void SessionDefaultsAreSafe()
	{
		var settings = new GameSettings();

		Assert.Equal(1f, settings.MasterVolume);
		Assert.True(settings.ScreenShake);
		Assert.False(settings.DebugOverlay);
		Assert.Same(AiProfile.Novice, settings.Difficulty);
	}
}
