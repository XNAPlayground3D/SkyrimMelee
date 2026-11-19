using DigitalRiseModel;
using Microsoft.Xna.Framework;
using SkyrimMelee.Graphics;

namespace SkyrimMelee.Fighters;

/// <summary>
/// Two-handed greatsword as a DrModel. Local space: origin at the grip centre,
/// blade along +Y, edge along X. <see cref="Pivot"/> is the only animated transform.
/// </summary>
public sealed class GreatswordRig : IDisposable
{
	public const float BladeTipY = 1.47f;

	public GreatswordRig(PrimitiveLibrary primitives, Color handColor)
	{
		var steel = primitives.CreateMaterial(new Color(0xAE, 0xB7, 0xC2));
		var steelDark = primitives.CreateMaterial(new Color(0x6E, 0x7A, 0x88));
		var leather = primitives.CreateMaterial(new Color(0x4A, 0x35, 0x27));
		var brass = primitives.CreateMaterial(new Color(0xC9, 0xA2, 0x4B));
		var skin = primitives.CreateMaterial(handColor);

		var bones = new List<DrModelBone>();

		void Add(string name, DrMeshPart source, DrMaterial material, Vector3 position, Vector3 scale)
		{
			var part = source.Clone();
			part.Material = material;

			var mesh = new DrMesh(part) { Name = name };
			bones.Add(new DrModelBone(name, mesh)
			{
				DefaultPose = new SrtTransform(position, Quaternion.Identity, scale)
			});
		}

		Add("Pommel", primitives.Sphere, brass, new Vector3(0f, -0.17f, 0f), new Vector3(0.07f));
		Add("Grip", primitives.Cylinder, leather, new Vector3(0f, 0f, 0f), new Vector3(0.035f, 0.30f, 0.035f));
		Add("Crossguard", primitives.Box, steelDark, new Vector3(0f, 0.16f, 0f), new Vector3(0.30f, 0.045f, 0.055f));
		Add("BladeLower", primitives.Box, steel, new Vector3(0f, 0.52f, 0f), new Vector3(0.085f, 0.62f, 0.024f));
		Add("BladeUpper", primitives.Box, steel, new Vector3(0f, 1.06f, 0f), new Vector3(0.070f, 0.48f, 0.020f));
		Add("Tip", primitives.Cone, steel, new Vector3(0f, 1.385f, 0f), new Vector3(0.07f, 0.17f, 0.07f));
		Add("Fuller", primitives.Box, steelDark, new Vector3(0f, 0.72f, 0f), new Vector3(0.018f, 0.80f, 0.026f));

		// Hands are part of the sword rig so they travel with the hilt; the arms stay static.
		Add("HandR", primitives.Box, skin, new Vector3(0f, 0.02f, 0f), new Vector3(0.09f, 0.13f, 0.07f));
		Add("HandL", primitives.Box, skin, new Vector3(0f, -0.10f, 0f), new Vector3(0.09f, 0.13f, 0.07f));

		Model = new DrModel(new DrModelBone("Sword") { Children = bones.ToArray() });
		Instance = new DrModelInstance(Model);
	}

	public DrModel Model { get; }

	public DrModelInstance Instance { get; }

	/// <summary>Grip-relative transform; the only animated transform in the game.</summary>
	public Matrix Pivot { get; set; } = Matrix.Identity;

	/// <summary>World transform of the sword for a given right-hand transform.</summary>
	public Matrix GetWorld(Matrix handWorld) => Pivot * handWorld;

	public void Dispose()
	{
		Model.Dispose();
	}
}
