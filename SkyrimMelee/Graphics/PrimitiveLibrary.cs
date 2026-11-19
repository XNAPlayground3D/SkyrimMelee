using DigitalRiseModel;
using DigitalRiseModel.Primitives;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SkyrimMelee.Graphics;

/// <summary>
/// Owns every generated primitive mesh. Meshes are created once, shared by instances,
/// and disposed with the library.
/// </summary>
public sealed class PrimitiveLibrary : IDisposable
{
	private const int Tessellation = 16;
	private const int CapsuleTessellation = 12;

	private readonly GraphicsDevice _device;
	private readonly List<DrMeshPart> _parts = new();
	private readonly List<DrMaterial> _materials = new();
	private readonly Dictionary<(int Length, int Radius), DrMeshPart> _capsules = new();

	public PrimitiveLibrary(GraphicsDevice device)
	{
		_device = device;

		// Unit dimensions: scale through the draw transform.
		Box = Track(MeshPrimitives.CreateBoxMeshPart(device, Vector3.One));
		Sphere = Track(MeshPrimitives.CreateSphereMeshPart(device, 0.5f, Tessellation));
		Cylinder = Track(MeshPrimitives.CreateCylinderMeshPart(device, 1f, 0.5f, Tessellation, capped: true));
		Cone = Track(MeshPrimitives.CreateConeMeshPart(device, 0.5f, 1f, Tessellation));
		Disc = Track(MeshPrimitives.CreateDiscMeshPart(device, 0.5f, MathF.PI * 2f, 48));
		Plane = Track(MeshPrimitives.CreatePlaneMeshPart(device, 1f, 1f,
			generateBackFace: true, normalDirection: NormalDirection.UpY));

		// DigitalRiseModel line primitives are position-only; BasicEffect needs PNT, so build PNT line lists.
		LineBox = Track(CreateLineBox(device));
		LineCircle = Track(CreateLineCircle(device, 48));
	}

	public DrMeshPart Box { get; }
	public DrMeshPart Sphere { get; }
	public DrMeshPart Cylinder { get; }
	public DrMeshPart Cone { get; }
	public DrMeshPart Disc { get; }
	public DrMeshPart Plane { get; }

	/// <summary>Unit wireframe box (edge length 1).</summary>
	public DrMeshPart LineBox { get; }

	/// <summary>Unit circle in the XZ plane (radius 1).</summary>
	public DrMeshPart LineCircle { get; }

	/// <summary>
	/// Capsule mesh with the given distance between sphere centres and radius, cached by millimetre.
	/// Total height is <paramref name="length"/> + 2 × <paramref name="radius"/>.
	/// </summary>
	public DrMeshPart GetCapsule(float length, float radius)
	{
		var key = (Length: (int)MathF.Round(length * 1000f), Radius: (int)MathF.Round(radius * 1000f));
		if (_capsules.TryGetValue(key, out var part))
		{
			return part;
		}

		part = Track(MeshPrimitives.CreateCapsuleMeshPart(_device, length, radius, CapsuleTessellation));
		_capsules[key] = part;
		return part;
	}

	/// <summary>Creates a material owned by the library; pass it to draw calls.</summary>
	public DrMaterial CreateMaterial(Color color)
	{
		var material = new DrMaterial { DiffuseColor = color };
		_materials.Add(material);
		return material;
	}

	public void Dispose()
	{
		foreach (var material in _materials)
		{
			material.Dispose();
		}

		foreach (var part in _parts)
		{
			part.Dispose();
		}

		_materials.Clear();
		_parts.Clear();
		_capsules.Clear();
	}

	private DrMeshPart Track(DrMeshPart part)
	{
		_parts.Add(part);
		return part;
	}

	private static DrMeshPart CreateLineBox(GraphicsDevice device)
	{
		const float h = 0.5f;
		var corners = new[]
		{
			new Vector3(-h, -h, -h), new Vector3(h, -h, -h),
			new Vector3(h, h, -h), new Vector3(-h, h, -h),
			new Vector3(-h, -h, h), new Vector3(h, -h, h),
			new Vector3(h, h, h), new Vector3(-h, h, h)
		};

		var edges = new[]
		{
			0, 1, 1, 2, 2, 3, 3, 0,
			4, 5, 5, 6, 6, 7, 7, 4,
			0, 4, 1, 5, 2, 6, 3, 7
		};

		var positions = new Vector3[edges.Length];
		for (var i = 0; i < edges.Length; i++)
		{
			positions[i] = corners[edges[i]];
		}

		return CreateLineList(device, positions);
	}

	private static DrMeshPart CreateLineCircle(GraphicsDevice device, int segments)
	{
		var positions = new Vector3[segments * 2];
		for (var i = 0; i < segments; i++)
		{
			var a0 = i * MathF.PI * 2f / segments;
			var a1 = (i + 1) * MathF.PI * 2f / segments;

			positions[i * 2] = new Vector3(MathF.Cos(a0), 0f, MathF.Sin(a0));
			positions[i * 2 + 1] = new Vector3(MathF.Cos(a1), 0f, MathF.Sin(a1));
		}

		return CreateLineList(device, positions);
	}

	private static DrMeshPart CreateLineList(GraphicsDevice device, Vector3[] positions)
	{
		var vertices = new VertexPositionNormalTexture[positions.Length];
		var indices = new ushort[positions.Length];

		for (var i = 0; i < positions.Length; i++)
		{
			vertices[i] = new VertexPositionNormalTexture(positions[i], Vector3.UnitY, Vector2.Zero);
			indices[i] = (ushort)i;
		}

		return new DrMeshPart(device, vertices, indices, PrimitiveType.LineList);
	}
}
