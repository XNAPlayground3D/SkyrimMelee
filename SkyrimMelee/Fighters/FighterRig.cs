using DigitalRiseModel;
using Microsoft.Xna.Framework;
using SkyrimMelee.Combat;
using SkyrimMelee.Graphics;

namespace SkyrimMelee.Fighters;

/// <summary>White primitive body parts plus their placement in the arena.</summary>
public sealed class FighterRig : IDisposable
{
	private readonly int _rightHandIndex;
	private readonly Dictionary<HurtboxKind, DrMaterial> _bodyMaterials;

	public FighterRig(PrimitiveLibrary primitives, Vector3 position, float facingYaw)
	{
		Model = FighterRigBuilder.Build(primitives, out _bodyMaterials);
		Instance = new DrModelInstance(Model);
		Transform = Matrix.CreateRotationY(facingYaw) * Matrix.CreateTranslation(position);

		var hand = Model.FindBoneByName("HandR")
			?? throw new InvalidOperationException("Fighter rig is missing the HandR bone.");
		_rightHandIndex = hand.Index;
	}

	public DrModel Model { get; }
	public DrModelInstance Instance { get; }
	public Matrix Transform { get; }

	/// <summary>Rigid reaction pose applied to the capsule only; the sword rig keeps its own clip.</summary>
	public Matrix BodyTransform { get; set; } = Matrix.Identity;

	/// <summary>Root transform used to draw the body, including the rigid reaction pose.</summary>
	public Matrix DrawTransform => BodyTransform * Transform;

	/// <summary>World transform of the right hand, including the fighter root.</summary>
	public Matrix GetRightHandTransform() => Instance.GetBoneGlobalTransform(_rightHandIndex) * Transform;

	/// <summary>Tints one body primitive; used for hit feedback.</summary>
	public void SetBodyPartColor(HurtboxKind kind, Color color)
	{
		if (_bodyMaterials.TryGetValue(kind, out var material))
		{
			material.DiffuseColor = color;
		}
	}

	public void Dispose()
	{
		Model.Dispose();
	}
}
