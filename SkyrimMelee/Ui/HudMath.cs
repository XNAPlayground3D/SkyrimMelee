namespace SkyrimMelee.Ui;

/// <summary>Small pure helpers shared by HUD drawing and tests.</summary>
public static class HudMath
{
	/// <summary>Blinks the stamina bar at 3 Hz while a denied action is fresh.</summary>
	public static bool StaminaFlashActive(float cueSeconds) =>
		cueSeconds > 0f && ((int)(cueSeconds * 6f) & 1) == 0;
}
