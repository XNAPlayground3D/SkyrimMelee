using DigitalRiseModel;
using Microsoft.Xna.Framework;
using SkyrimMelee.Graphics;

namespace SkyrimMelee.Fighters;

/// <summary>
/// Builds the static humanoid statue as a DrModel. Local space: +X forward, +Y up,
/// +Z side-to-side; the root transform supplies position and facing.
/// </summary>
public static class FighterRigBuilder
{
	public static DrModel Build(PrimitiveLibrary primitives, FighterPalette palette)
	{
		var skin = primitives.CreateMaterial(palette.Skin);
		var cloth = primitives.CreateMaterial(palette.Cloth);
		var clothDark = primitives.CreateMaterial(palette.ClothDark);
		var leather = primitives.CreateMaterial(palette.Leather);

		var bones = new List<DrModelBone>();

		void Add(string name, DrMeshPart source, DrMaterial material, Vector3 position,
			Vector3 scale, Quaternion? rotation = null)
		{
			var part = source.Clone();
			part.Material = material;

			var mesh = new DrMesh(part) { Name = name };
			bones.Add(new DrModelBone(name, mesh)
			{
				DefaultPose = new SrtTransform(position, rotation ?? Quaternion.Identity, scale)
			});
		}

		void AddSegment(string name, Vector3 start, Vector3 end, float radius, DrMaterial material)
		{
			var delta = end - start;
			var length = MathF.Max(0.02f, delta.Length() - 2f * radius);
			Add(name, primitives.GetCapsule(length, radius), material,
				(start + end) * 0.5f, Vector3.One, RotationFromY(delta));
		}

		// Head and torso. The camera sees the fighters in profile, so keep some chest depth on X.
		Add("Head", primitives.Sphere, skin, new Vector3(0f, 1.67f, 0f), new Vector3(0.21f));
		Add("Neck", primitives.Cylinder, skin, new Vector3(0f, 1.55f, 0f), new Vector3(0.10f, 0.09f, 0.10f));
		Add("Chest", primitives.Box, cloth, new Vector3(0f, 1.28f, 0f), new Vector3(0.30f, 0.50f, 0.44f));
		Add("Abdomen", primitives.Box, clothDark, new Vector3(0f, 1.00f, 0f), new Vector3(0.26f, 0.20f, 0.33f));
		Add("Pelvis", primitives.Box, clothDark, new Vector3(0f, 0.845f, 0f), new Vector3(0.28f, 0.20f, 0.36f));

		// Right arm reaches forward to the future sword grip.
		var shoulderR = new Vector3(0.02f, 1.46f, -0.24f);
		var elbowR = new Vector3(0.30f, 1.20f, -0.26f);
		var wristR = new Vector3(0.50f, 1.00f, -0.26f);
		AddSegment("UpperArmR", shoulderR, elbowR, 0.06f, cloth);
		AddSegment("ForearmR", elbowR, wristR, 0.05f, cloth);
		Add("HandR", primitives.Box, skin,
			wristR + new Vector3(0.04f, -0.02f, 0f), new Vector3(0.09f, 0.13f, 0.07f));

		// Left arm braces across the torso; it does not grip the hilt.
		var shoulderL = new Vector3(0.02f, 1.46f, 0.24f);
		var elbowL = new Vector3(0.24f, 1.18f, 0.12f);
		var wristL = new Vector3(0.40f, 1.02f, -0.02f);
		AddSegment("UpperArmL", shoulderL, elbowL, 0.06f, cloth);
		AddSegment("ForearmL", elbowL, wristL, 0.05f, cloth);
		Add("HandL", primitives.Box, skin,
			wristL + new Vector3(0.04f, -0.02f, 0f), new Vector3(0.09f, 0.13f, 0.07f));

		// Legs in a bladed stance: left foot forward, right foot back.
		var hipL = new Vector3(0f, 0.90f, 0.11f);
		var kneeL = new Vector3(0.06f, 0.48f, 0.11f);
		var ankleL = new Vector3(0.10f, 0.12f, 0.11f);
		AddSegment("ThighL", hipL, kneeL, 0.09f, clothDark);
		AddSegment("ShinL", kneeL, ankleL, 0.075f, clothDark);
		Add("BootL", primitives.Box, leather,
			new Vector3(0.15f, 0.045f, 0.11f), new Vector3(0.26f, 0.09f, 0.15f));

		var hipR = new Vector3(0f, 0.90f, -0.11f);
		var kneeR = new Vector3(-0.05f, 0.48f, -0.11f);
		var ankleR = new Vector3(-0.08f, 0.12f, -0.11f);
		AddSegment("ThighR", hipR, kneeR, 0.09f, clothDark);
		AddSegment("ShinR", kneeR, ankleR, 0.075f, clothDark);
		Add("BootR", primitives.Box, leather,
			new Vector3(-0.03f, 0.045f, -0.11f), new Vector3(0.26f, 0.09f, 0.15f));

		var root = new DrModelBone("Fighter") { Children = bones.ToArray() };
		return new DrModel(root);
	}

	private static Quaternion RotationFromY(Vector3 direction)
	{
		var dir = Vector3.Normalize(direction);
		var dot = Math.Clamp(Vector3.Dot(Vector3.UnitY, dir), -1f, 1f);

		if (dot > 0.9999f)
		{
			return Quaternion.Identity;
		}

		if (dot < -0.9999f)
		{
			return Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathF.PI);
		}

		var axis = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, dir));
		return Quaternion.CreateFromAxisAngle(axis, MathF.Acos(dot));
	}
}
