using DigitalRiseModel;
using Microsoft.Xna.Framework;
using SkyrimMelee.Graphics;

namespace SkyrimMelee.Fighters;

/// <summary>A static fighter statue plus its placement in the arena.</summary>
public sealed class FighterRig : IDisposable
{
	public FighterRig(PrimitiveLibrary primitives, FighterPalette palette, Vector3 position, float facingYaw)
	{
		Model = FighterRigBuilder.Build(primitives, palette);
		Instance = new DrModelInstance(Model);
		Transform = Matrix.CreateRotationY(facingYaw) * Matrix.CreateTranslation(position);
	}

	public DrModel Model { get; }
	public DrModelInstance Instance { get; }
	public Matrix Transform { get; }

	public void Dispose()
	{
		Model.Dispose();
	}
}
