using Microsoft.Xna.Framework;

namespace SkyrimMelee.Ui
{
	partial class HudPanel
	{
		public HudPanel()
		{
			BuildUI();
		}

		public void UpdateStats(float fps, int drawCalls, int primitives)
		{
			_fps.Text = FormattableString.Invariant($"FPS  {fps:0.0}");
			_drawCalls.Text = $"Draw calls  {drawCalls}";
			_primitives.Text = $"Primitives  {primitives}";
		}

		public void UpdatePause(bool paused)
		{
			_pause.Text = paused ? "PAUSE  ON" : "PAUSE  off";
			_pause.TextColor = paused
				? new Color(0xF0, 0xB0, 0x50)
				: new Color(0xE6, 0xDC, 0xC8);
		}

		public void UpdatePlayback(string clipName, float speed, float timeSeconds, float durationSeconds)
		{
			_clipName.Text = $"Clip  {clipName}";
			_clipSpeed.Text = FormattableString.Invariant($"Speed  {speed:0.00}x");

			var currentMs = (int)MathF.Round(timeSeconds * 1000f);
			var totalMs = (int)MathF.Round(durationSeconds * 1000f);
			_clipProgress.Text = FormattableString.Invariant($"Progress  {currentMs}ms/{totalMs}ms");
		}
	}
}
