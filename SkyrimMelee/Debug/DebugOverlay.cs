using Microsoft.Xna.Framework;
using Myra.Graphics2D.UI;
using SkyrimMelee.Core;

namespace SkyrimMelee.Debug;

/// <summary>
/// Toggleable panel with loop internals. T0.4 scaffold; combat state lands in T5.3.
/// </summary>
public sealed class DebugOverlay
{
	private static readonly Color OverlayColor = new(150, 170, 190);

	private readonly Label _label;
	private readonly string _mode;

	public DebugOverlay(GameOptions options)
	{
		_mode = $"god={(options.GodMode ? "on" : "off")}  ui={(options.NoUi ? "off" : "on")}  debug={(options.Debug ? "on" : "off")}";
		_label = new Label
		{
			Font = Myra.DefaultAssets.DebugFontSystem.GetFont(16),
			TextColor = OverlayColor,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Left = 24,
			Top = 78,
			Visible = options.Debug
		};
	}

	public Widget Widget => _label;

	public bool Visible
	{
		get => _label.Visible;
		set => _label.Visible = value;
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

		_label.Text = FormattableString.Invariant(
			$"clock  frame {clock.FrameIndex}  sim {clock.SimTime:0.000}s  dt {clock.DeltaTime * 1000:0.00}ms  scale {clock.TimeScale:0.00}x\nloop   acc {clock.Accumulator * 1000:0.0}ms  dropped {clock.TotalDroppedSteps}\nmode   {_mode}");
	}
}
