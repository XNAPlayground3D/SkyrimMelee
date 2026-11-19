using DigitalRiseModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SkyrimMelee.Graphics;

namespace SkyrimMelee.Debug;

/// <summary>
/// Temporary T1.1 scene: every primitive in the library, side by side.
/// Replaced by the arena and fighter rigs in T1.2/T1.3.
/// </summary>
public sealed class PrimitiveShowcase
{
	private readonly List<(DrMeshPart Part, Matrix World)> _solids = new();
	private readonly List<(DrMeshPart Part, Matrix World)> _lines = new();
	private readonly (DrMeshPart Part, Matrix World) _disc;

	public PrimitiveShowcase(PrimitiveLibrary primitives)
	{
		var plane = primitives.Plane;
		Paint(primitives, plane, new Color(0x2A, 0x2C, 0x33));
		_solids.Add((plane, World(new Vector3(0f, 0f, 0f), new Vector3(16f, 1f, 8f))));

		AddSolid(primitives, primitives.Box, new Color(0x3E, 0x58, 0x71),
			new Vector3(-4f, 0.5f, 0f), Vector3.One);
		AddSolid(primitives, primitives.Sphere, new Color(0x4E, 0x8A, 0x8A),
			new Vector3(-2.4f, 0.7f, 0f), new Vector3(1.4f));
		AddSolid(primitives, primitives.Cylinder, new Color(0xB0, 0x8A, 0x3A),
			new Vector3(-0.8f, 1f, 0f), new Vector3(1.2f, 2f, 1.2f));
		AddSolid(primitives, primitives.Cone, new Color(0xC4, 0x55, 0x3F),
			new Vector3(0.8f, 1f, 0f), new Vector3(1.4f, 2f, 1.4f));
		AddSolid(primitives, primitives.GetCapsule(0.8f, 0.4f), new Color(0x6F, 0xA7, 0x6F),
			new Vector3(2.4f, 0.8f, 0f), Vector3.One);

		// The disc's +Y face uses the opposite winding to the other primitives.
		Paint(primitives, primitives.Disc, new Color(0x6E, 0x6E, 0x7A));
		_disc = (primitives.Disc, World(new Vector3(4f, 0.01f, 0f), new Vector3(2f)));

		var lineBox = primitives.LineBox;
		Paint(primitives, lineBox, new Color(0x9F, 0xD8, 0xFF));
		_lines.Add((lineBox, World(new Vector3(-2.5f, 2.2f, 0f), new Vector3(1.5f))));

		var lineCircle = primitives.LineCircle;
		Paint(primitives, lineCircle, new Color(0xFF, 0xD2, 0x7A));
		_lines.Add((lineCircle, World(new Vector3(2.5f, 2.2f, 0f), new Vector3(1.5f))));
	}

	public void Draw(PrimitiveRenderer renderer)
	{
		foreach (var (part, world) in _solids)
		{
			renderer.Draw(part, world);
		}

		renderer.Draw(_disc.Part, _disc.World, CullMode.CullClockwiseFace);

		foreach (var (part, world) in _lines)
		{
			renderer.DrawLines(part, world);
		}
	}

	private void AddSolid(PrimitiveLibrary primitives, DrMeshPart part, Color color, Vector3 position, Vector3 scale)
	{
		Paint(primitives, part, color);
		_solids.Add((part, World(position, scale)));
	}

	private static void Paint(PrimitiveLibrary primitives, DrMeshPart part, Color color) =>
		part.Material = primitives.CreateMaterial(color);

	private static Matrix World(Vector3 position, Vector3 scale) =>
		Matrix.CreateScale(scale) * Matrix.CreateTranslation(position);
}
