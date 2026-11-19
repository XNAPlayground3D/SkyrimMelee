using DigitalRiseModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SkyrimMelee.Graphics;

/// <summary>
/// Minimal BasicEffect renderer for DigitalRiseModel mesh parts.
/// Colour and material data come from <see cref="DrMeshPart.Material"/>; the caller supplies the world transform.
/// </summary>
public sealed class PrimitiveRenderer : IDisposable
{
	private static readonly RasterizerState CullBack = new() { CullMode = CullMode.CullCounterClockwiseFace };
	private static readonly RasterizerState CullFront = new() { CullMode = CullMode.CullClockwiseFace };
	private static readonly RasterizerState CullOff = new() { CullMode = CullMode.None };

	private readonly GraphicsDevice _device;
	private readonly BasicEffect _lit;
	private readonly BasicEffect _unlit;
	private Matrix _view;
	private Matrix _projection;

	public PrimitiveRenderer(GraphicsDevice device)
	{
		_device = device;

		_lit = new BasicEffect(device)
		{
			LightingEnabled = true,
			TextureEnabled = false,
			VertexColorEnabled = false,
			PreferPerPixelLighting = true
		};
		_lit.EnableDefaultLighting();
		_lit.DirectionalLight0.Direction = Vector3.Normalize(new Vector3(-0.6f, -1f, 0.35f));
		_lit.DirectionalLight0.DiffuseColor = new Vector3(1f, 0.95f, 0.85f);
		_lit.DirectionalLight1.Direction = Vector3.Normalize(new Vector3(0.7f, -0.5f, -0.8f));
		_lit.DirectionalLight1.DiffuseColor = new Vector3(0.25f, 0.35f, 0.5f);
		_lit.AmbientLightColor = new Vector3(0.30f, 0.33f, 0.40f);

		_unlit = new BasicEffect(device)
		{
			LightingEnabled = false,
			TextureEnabled = false,
			VertexColorEnabled = false
		};
	}

	public void Begin(Matrix view, Matrix projection)
	{
		_view = view;
		_projection = projection;

		_device.DepthStencilState = DepthStencilState.Default;
		_device.BlendState = BlendState.Opaque;
		_device.RasterizerState = CullBack;
	}

	public void Draw(DrMeshPart part, Matrix world, CullMode cullMode = CullMode.CullCounterClockwiseFace)
	{
		_device.RasterizerState = cullMode switch
		{
			CullMode.None => CullOff,
			CullMode.CullClockwiseFace => CullFront,
			_ => CullBack
		};

		var material = part.Material;
		_lit.World = world;
		_lit.View = _view;
		_lit.Projection = _projection;
		_lit.DiffuseColor = (material?.DiffuseColor ?? Color.White).ToVector3();
		_lit.SpecularColor = (material?.SpecularColor ?? Color.Black).ToVector3();
		_lit.SpecularPower = material is { Shininess: > 0f } ? material.Shininess : 16f;
		_lit.EmissiveColor = (material?.EmissiveColor ?? Color.Black).ToVector3();
		_lit.CurrentTechnique.Passes[0].Apply();

		DrawPart(part);
	}

	public void DrawLines(DrMeshPart part, Matrix world)
	{
		_device.RasterizerState = CullOff;

		_unlit.World = world;
		_unlit.View = _view;
		_unlit.Projection = _projection;
		_unlit.DiffuseColor = (part.Material?.DiffuseColor ?? Color.White).ToVector3();
		_unlit.CurrentTechnique.Passes[0].Apply();

		DrawPart(part);
	}

	public void End()
	{
		_device.SetVertexBuffer(null);
		_device.Indices = null;
	}

	public void Dispose()
	{
		_lit.Dispose();
		_unlit.Dispose();
	}

	private void DrawPart(DrMeshPart part)
	{
		_device.SetVertexBuffer(part.VertexBuffer);
		_device.Indices = part.IndexBuffer;
		_device.DrawIndexedPrimitives(
			part.PrimitiveType,
			part.VertexOffset,
			part.StartIndex,
			part.PrimitiveCount);
	}
}
