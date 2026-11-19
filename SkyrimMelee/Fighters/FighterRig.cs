using DigitalRiseModel;
using Microsoft.Xna.Framework;
using SkyrimMelee.Graphics;

namespace SkyrimMelee.Fighters;

/// <summary>A static fighter statue plus its placement in the arena.</summary>
public sealed class FighterRig : IDisposable
{
	private readonly int _rightHandIndex;

	public FighterRig(PrimitiveLibrary primitives, FighterPalette palette, Vector3 position, float facingYaw)
	{
		Model = FighterRigBuilder.Build(primitives, palette);
		Instance = new DrModelInstance(Model);
		Transform = Matrix.CreateRotationY(facingYaw) * Matrix.CreateTranslation(position);

		var hand = Model.FindBoneByName("HandR")
			?? throw new InvalidOperationException("Fighter rig is missing the HandR bone.");
		_rightHandIndex = hand.Index;
	}

	public DrModel Model { get; }
	public DrModelInstance Instance { get; }
	public Matrix Transform { get; }

	/// <summary>World transform of the right hand, including the fighter root.</summary>
	public Matrix GetRightHandTransform() => Instance.GetBoneGlobalTransform(_rightHandIndex) * Transform;

	public void Dispose()
	{
		Model.Dispose();
	}
}
