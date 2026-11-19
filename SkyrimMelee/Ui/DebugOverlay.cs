using SkyrimMelee.Core;

namespace SkyrimMelee.Ui
{
	partial class DebugOverlay
	{
		public DebugOverlay(GameOptions options)
		{
			BuildUI();

			_mode.Text = FormattableString.Invariant(
				$"mode  god={(options.GodMode ? "on" : "off")}  ui={(options.NoUi ? "off" : "on")}  debug-start={(options.Debug ? "on" : "off")}");
		}

		public void Toggle()
		{
			Visible = !Visible;
		}

		public void Update(GameClock clock)
		{
			if (!Visible)
			{
				return;
			}

			_frame.Text = $"frame {clock.FrameIndex}";
			_sim.Text = FormattableString.Invariant($"sim {clock.SimTime:0.000} s");
			_dt.Text = FormattableString.Invariant($"dt {clock.DeltaTime * 1000:0.00} ms");
			_scale.Text = FormattableString.Invariant($"scale {clock.TimeScale:0.00}x");
			_acc.Text = FormattableString.Invariant($"acc {clock.Accumulator * 1000:0.0} ms");
			_dropped.Text = $"dropped {clock.TotalDroppedSteps}";
		}
	}
}
