using Microsoft.Xna.Framework;
using Myra;
using Myra.Graphics2D.UI;

namespace SkyrimMelee.Ui;

/// <summary>
/// Owns Myra at the window's native resolution. Widgets anchor to edges/centres, so no scaling layer is needed.
/// </summary>
public sealed class MyraHost : IDisposable
{
	private readonly Desktop _desktop;

	public MyraHost(Game game)
	{
		MyraEnvironment.Game = game;
		_desktop = new Desktop();
	}

	public Desktop Desktop => _desktop;

	/// <summary>True while a modal or keyboard-focused widget owns input.</summary>
	public bool CapturesInput =>
		_desktop.HasModalWidget || _desktop.FocusedKeyboardWidget is not null;

	public Widget? Root
	{
		get => _desktop.Root;
		set => _desktop.Root = value;
	}

	public void Draw()
	{
		_desktop.Render();
	}

	public void Dispose()
	{
		_desktop.Dispose();
	}
}
