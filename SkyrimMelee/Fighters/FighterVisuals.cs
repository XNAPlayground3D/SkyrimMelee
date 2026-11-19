using Microsoft.Xna.Framework;

namespace SkyrimMelee.Fighters;

/// <summary>Colours for one fighter. Parts share these materials.</summary>
public readonly record struct FighterPalette(Color Skin, Color Cloth, Color ClothDark, Color Leather);

public static class FighterVisuals
{
	public static readonly FighterPalette Player = new(
		Skin: new Color(0xC9, 0xA0, 0x7A),
		Cloth: new Color(0x3E, 0x58, 0x71),
		ClothDark: new Color(0x26, 0x37, 0x4A),
		Leather: new Color(0x4A, 0x35, 0x27));

	public static readonly FighterPalette Opponent = new(
		Skin: new Color(0xB9, 0x8A, 0x68),
		Cloth: new Color(0x7A, 0x3B, 0x2E),
		ClothDark: new Color(0x45, 0x21, 0x1B),
		Leather: new Color(0x3E, 0x2A, 0x1F));
}
