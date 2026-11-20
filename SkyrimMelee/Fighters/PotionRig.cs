using DigitalRiseModel;
using Microsoft.Xna.Framework;
using SkyrimMelee.Graphics;

namespace SkyrimMelee.Fighters;

/// <summary>
/// A small red health potion built from primitives: liquid sphere, glass neck,
/// cork, and a floor glow ring. The local origin sits on the arena floor.
/// </summary>
public sealed class PotionRig : IDisposable
{
	public PotionRig(PrimitiveLibrary primitives)
	{
		var liquid = primitives.CreateMaterial(new Color(0xC8, 0x3A, 0x3A));
		var glass = primitives.CreateMaterial(new Color(0x9F, 0xB6, 0xC4));
		var cork = primitives.CreateMaterial(new Color(0x8A, 0x5A, 0x32));
		var glow = primitives.CreateMaterial(new Color(0xE0, 0x8A, 0x5A));

		var bones = new List<DrModelBone>();

		void Add(string name, DrMeshPart source, DrMaterial material,
			Vector3 position, Vector3 scale)
		{
			var part = source.Clone();
			part.Material = material;
			var mesh = new DrMesh(part) { Name = name };
			bones.Add(new DrModelBone(name, mesh)
			{
				DefaultPose = new SrtTransform(position, Quaternion.Identity, scale)
			});
		}

		Add("Liquid", primitives.Sphere, liquid,
			new Vector3(0f, 0.16f, 0f), new Vector3(0.30f));
		Add("Neck", primitives.Cylinder, glass,
			new Vector3(0f, 0.30f, 0f), new Vector3(0.11f, 0.22f, 0.11f));
		Add("Cork", primitives.Cylinder, cork,
			new Vector3(0f, 0.43f, 0f), new Vector3(0.12f, 0.07f, 0.12f));
		Add("Glow", primitives.LineCircle, glow,
			new Vector3(0f, 0.01f, 0f), new Vector3(0.44f));

		Model = new DrModel(new DrModelBone("Potion") { Children = bones.ToArray() });
		Instance = new DrModelInstance(Model);
	}

	public DrModel Model { get; }

	public DrModelInstance Instance { get; }

	/// <summary>
	/// Dispose only at shutdown: the model's cloned meshes share the primitive
	/// library's vertex/index buffers, so disposing a live rig breaks the arena.
	/// </summary>
	public void Dispose() => Model.Dispose();
}
