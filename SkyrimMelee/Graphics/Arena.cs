using DigitalRiseModel;
using Microsoft.Xna.Framework;

namespace SkyrimMelee.Graphics;

/// <summary>
/// Stone duelling circle built as a <see cref="DrModel"/>: one bone per item,
/// one mesh part per bone. Rendered through a cached <see cref="DrModelInstance"/>.
/// </summary>
public sealed class Arena : IDisposable
{
	public Arena(PrimitiveLibrary primitives)
	{
		var stone = primitives.CreateMaterial(new Color(0x4A, 0x4A, 0x52));
		var darkStone = primitives.CreateMaterial(new Color(0x3A, 0x3A, 0x44));
		var ring = primitives.CreateMaterial(new Color(0x6E, 0x6E, 0x7A));

		var bones = new List<DrModelBone>();

		void Add(string name, DrMeshPart source, DrMaterial material, Matrix world)
		{
			var part = source.Clone();
			part.Material = material;

			var mesh = new DrMesh(part) { Name = name };
			bones.Add(new DrModelBone(name, mesh) { DefaultPose = new SrtTransform(world) });
		}

		// Floor: a shallow cylinder keeps consistent winding and gives the disc a rim.
		Add("Floor", primitives.Cylinder, stone, World(new Vector3(0f, -0.04f, 0f), new Vector3(12f, 0.08f, 12f)));

		var radii = new[] { 1.5f, 3f, 4.5f, 6f };
		for (var i = 0; i < radii.Length; i++)
		{
			Add($"Ring{i}", primitives.LineCircle, ring,
				World(new Vector3(0f, 0.01f, 0f), new Vector3(radii[i])));
		}

		var pillars = new[] { (-4f, 4f), (4f, 4f), (-2f, 5.4f), (2f, 5.4f) };
		for (var i = 0; i < pillars.Length; i++)
		{
			var (x, z) = pillars[i];
			Add($"Pillar{i}", primitives.Cylinder, darkStone,
				World(new Vector3(x, 1.8f, z), new Vector3(0.55f, 3.6f, 0.55f)));
		}

		var root = new DrModelBone("Arena") { Children = bones.ToArray() };
		Model = new DrModel(root);
		Instance = new DrModelInstance(Model);
	}

	public DrModel Model { get; }

	public DrModelInstance Instance { get; }

	public void Dispose()
	{
		Model.Dispose();
	}

	private static Matrix World(Vector3 position, Vector3 scale) =>
		Matrix.CreateScale(scale) * Matrix.CreateTranslation(position);
}
