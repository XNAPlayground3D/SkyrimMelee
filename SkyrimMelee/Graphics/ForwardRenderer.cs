using DigitalRiseModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SkyrimMelee.Graphics;

/// <summary>
/// Minimal BasicEffect renderer for DigitalRiseModel mesh parts.
/// The caller supplies the part, material, and world transform.
/// </summary>
public sealed class ForwardRenderer : IDisposable
{
	private static readonly RasterizerState CullBack = new() { CullMode = CullMode.CullCounterClockwiseFace };
	private static readonly RasterizerState CullFront = new() { CullMode = CullMode.CullClockwiseFace };
	private static readonly RasterizerState CullOff = new() { CullMode = CullMode.None };
	private static readonly RasterizerState WireframeState = new()
	{
		CullMode = CullMode.CullCounterClockwiseFace,
		FillMode = FillMode.WireFrame
	};

	private readonly GraphicsDevice _device;
	private readonly BasicEffect _lit;
	private readonly BasicEffect _unlit;
	private readonly DrMaterial _fallbackMaterial = new() { DiffuseColor = Color.White };
	private Matrix _view;
	private Matrix _projection;

	/// <summary>Debug wireframe mode, toggled with the overlay.</summary>
	public bool Wireframe { get; set; }

	/// <summary>Draw calls issued in the last frame.</summary>
	public int DrawCalls { get; private set; }

	/// <summary>Mesh primitives (triangles or lines) submitted in the last frame.</summary>
	public int PrimitivesDrawn { get; private set; }

	public ForwardRenderer(GraphicsDevice device)
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
		DrawCalls = 0;
		PrimitivesDrawn = 0;

		_view = view;
		_projection = projection;

		_device.DepthStencilState = DepthStencilState.Default;
		_device.BlendState = BlendState.Opaque;
		_device.RasterizerState = CullBack;
	}

	public void Draw(DrModelInstance model, Matrix transform)
	{
		var drModel = model.Model;
		if (drModel is null)
		{
			return;
		}

		foreach (var mesh in drModel.Meshes)
		{
			var world = model.GetBoneGlobalTransform(mesh.ParentBone.Index) * transform;

			foreach (var part in mesh.MeshParts)
			{
				var material = part.Material ?? _fallbackMaterial;

				if (part.PrimitiveType == PrimitiveType.LineList)
				{
					DrawLines(part, material, world);
				}
				else
				{
					Draw(part, material, world);
				}
			}
		}
	}

	public void Draw(DrMeshPart part, DrMaterial material, Matrix world,
		CullMode cullMode = CullMode.CullCounterClockwiseFace)
	{
		if (Wireframe)
		{
			// Flat, unlit wireframe: dense primitive meshes still read as a cage, not a solid surface.
			_device.RasterizerState = WireframeState;
			_unlit.World = world;
			_unlit.View = _view;
			_unlit.Projection = _projection;
			_unlit.DiffuseColor = new Vector3(0.75f, 0.90f, 1f);
			_unlit.CurrentTechnique.Passes[0].Apply();
		}
		else
		{
			_device.RasterizerState = cullMode switch
			{
				CullMode.None => CullOff,
				CullMode.CullClockwiseFace => CullFront,
				_ => CullBack
			};

			_lit.World = world;
			_lit.View = _view;
			_lit.Projection = _projection;
			_lit.DiffuseColor = material.DiffuseColor.ToVector3();
			_lit.SpecularColor = material.SpecularColor.ToVector3();
			_lit.SpecularPower = material.Shininess > 0f ? material.Shininess : 16f;
			_lit.EmissiveColor = material.EmissiveColor.ToVector3();
			_lit.CurrentTechnique.Passes[0].Apply();
		}

		DrawPart(part);
	}

	public void DrawLines(DrMeshPart part, DrMaterial material, Matrix world)
	{
		_device.RasterizerState = CullOff;

		_unlit.World = world;
		_unlit.View = _view;
		_unlit.Projection = _projection;
		_unlit.DiffuseColor = material.DiffuseColor.ToVector3();
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
		_fallbackMaterial.Dispose();
	}

	private void DrawPart(DrMeshPart part)
	{
		DrawCalls++;
		PrimitivesDrawn += part.PrimitiveCount;

		_device.SetVertexBuffer(part.VertexBuffer);
		_device.Indices = part.IndexBuffer;
		_device.DrawIndexedPrimitives(
			part.PrimitiveType,
			part.VertexOffset,
			part.StartIndex,
			part.PrimitiveCount);
	}
}
