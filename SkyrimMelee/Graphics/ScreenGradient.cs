using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SkyrimMelee.Graphics;

/// <summary>
/// Vertical backdrop gradient drawn as tinted 1x1 strips, then clears depth for the 3D pass.
/// No textures or shaders are shipped; the pixel is generated at runtime.
/// </summary>
public sealed class ScreenGradient : IDisposable
{
	private const int Strips = 24;
	private static readonly Color Top = new(11, 12, 17);
	private static readonly Color Horizon = new(23, 26, 34);
	private static readonly Color Bottom = new(14, 15, 20);

	private readonly GraphicsDevice _device;
	private readonly SpriteBatch _batch;
	private readonly Texture2D _pixel;

	public ScreenGradient(GraphicsDevice device)
	{
		_device = device;
		_batch = new SpriteBatch(device);
		_pixel = new Texture2D(device, 1, 1);
		_pixel.SetData(new[] { Color.White });
	}

	public void Draw()
	{
		var width = _device.PresentationParameters.BackBufferWidth;
		var height = _device.PresentationParameters.BackBufferHeight;
		if (width <= 0 || height <= 0)
		{
			return;
		}

		var stripHeight = Math.Max(1, (int)MathF.Ceiling(height / (float)Strips));

		_batch.Begin(
			SpriteSortMode.Deferred,
			BlendState.Opaque,
			SamplerState.PointClamp,
			DepthStencilState.None,
			RasterizerState.CullNone);

		for (var i = 0; i < Strips; i++)
		{
			var t = i / (float)(Strips - 1);
			var color = t < 0.55f
				? Color.Lerp(Top, Horizon, t / 0.55f)
				: Color.Lerp(Horizon, Bottom, (t - 0.55f) / 0.45f);

			_batch.Draw(_pixel, new Rectangle(0, i * stripHeight, width, stripHeight), color);
		}

		_batch.End();

		// The gradient wrote no depth; start the 3D pass from a clean buffer.
		_device.Clear(ClearOptions.DepthBuffer, Color.Black, 1f, 0);
	}

	public void Dispose()
	{
		_batch.Dispose();
		_pixel.Dispose();
	}
}
