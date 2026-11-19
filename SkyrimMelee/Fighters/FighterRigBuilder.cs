using DigitalRiseModel;
using Microsoft.Xna.Framework;
using SkyrimMelee.Combat;
using SkyrimMelee.Graphics;

namespace SkyrimMelee.Fighters;

/// <summary>
/// Builds the fighter placeholder from the same primitive definitions as its
/// hurtboxes: head sphere, torso, hips, and legs capsules, plus invisible hand
/// pivots. Local space: +X forward, +Y up, +Z side-to-side.
/// </summary>
public static class FighterRigBuilder
{
	public static DrModel Build(
		PrimitiveLibrary primitives,
		out Dictionary<HurtboxKind, DrMaterial> bodyMaterials)
	{
		bodyMaterials = new Dictionary<HurtboxKind, DrMaterial>();
		var bones = new List<DrModelBone>();

		foreach (var kind in new[]
		{
			HurtboxKind.Legs, HurtboxKind.Hips, HurtboxKind.Torso, HurtboxKind.Head
		})
		{
			var definition = HurtboxDefinition.For(kind);
			var material = primitives.CreateMaterial(Color.White);
			var part = definition.Shape == HurtboxShapeKind.Sphere
				? primitives.Sphere.Clone()
				: primitives.GetCapsule(definition.HalfLength * 2f, definition.Radius).Clone();
			part.Material = material;
			bodyMaterials[kind] = material;

			var scale = definition.Shape == HurtboxShapeKind.Sphere
				? Vector3.One * (definition.Radius * 2f)
				: Vector3.One;

			var mesh = new DrMesh(part) { Name = $"Body{kind}" };
			bones.Add(new DrModelBone($"Body{kind}", mesh)
			{
				DefaultPose = new SrtTransform(definition.Center, Quaternion.Identity, scale)
			});
		}

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
