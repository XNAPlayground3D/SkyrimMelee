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
	}
}
