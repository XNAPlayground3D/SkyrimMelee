using DigitalRiseModel;
using Microsoft.Xna.Framework;
using SkyrimMelee.Graphics;

namespace SkyrimMelee.Fighters;

/// <summary>
/// Builds the fighter placeholder: one white capsule plus invisible hand pivots.
/// Local space: +X forward, +Y up, +Z side-to-side; the root transform supplies
/// position and facing.
/// </summary>
public static class FighterRigBuilder
{
	public static DrModel Build(PrimitiveLibrary primitives)
	{
		var white = primitives.CreateMaterial(Color.White);
		var bones = new List<DrModelBone>();

		// Total height 1.8 m: 1.24 m cylinder + two 0.28 m caps.
		var bodyPart = primitives.GetCapsule(1.24f, 0.28f).Clone();
		bodyPart.Material = white;

		var body = new DrMesh(bodyPart) { Name = "Body" };
		bones.Add(new DrModelBone("Body", body)
		{
			DefaultPose = new SrtTransform(new Vector3(0f, 0.9f, 0f), Quaternion.Identity, Vector3.One)
		});

		// Scale-free attachment points; the visible hands belong to the sword rig.
		void AddHandPivot(string name, Vector3 position)
		{
			bones.Add(new DrModelBone(name)
			{
				DefaultPose = new SrtTransform(position, Quaternion.Identity, Vector3.One)
			});
		}

		AddHandPivot("HandR", new Vector3(0.54f, 0.98f, 0.26f));
		AddHandPivot("HandL", new Vector3(0.54f, 0.98f, 0.18f));

		var root = new DrModelBone("Fighter") { Children = bones.ToArray() };
		return new DrModel(root);
	}
}
