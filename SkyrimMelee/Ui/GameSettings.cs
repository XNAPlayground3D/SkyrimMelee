using SkyrimMelee.Ai;

namespace SkyrimMelee.Ui;

/// <summary>Session-only settings shared by the main menu and the pause screen.</summary>
public sealed class GameSettings
{
	public float MasterVolume { get; set; } = 1f;

	public bool ScreenShake { get; set; } = true;

	public bool DebugOverlay { get; set; }

	public AiProfile Difficulty { get; set; } = AiProfile.Default;
}
