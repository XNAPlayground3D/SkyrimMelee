using FontStashSharp;
using Microsoft.Xna.Framework;
using Myra;
using Myra.Graphics2D;
using Myra.Graphics2D.Brushes;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace SkyrimMelee.Ui;

/// <summary>
/// Flat parchment/steel skin for the menu screens. Fonts come from Myra's
/// embedded Inter face at three sizes; nothing here loads assets from disk.
/// </summary>
public static class UiSkin
{
	public static readonly Color Parchment = new(232, 222, 202);
	public static readonly Color Muted = new(150, 158, 170);
	public static readonly Color Steel = new(96, 110, 126);
	public static readonly Color Accent = new(198, 162, 96);
	public static readonly Color Danger = new(196, 85, 63);
	public static readonly Color Panel = new(22, 25, 32, 214);
	public static readonly Color PanelOver = new(38, 43, 54, 235);
	public static readonly Color PanelFocus = new(52, 60, 76, 245);
	public static readonly Color PanelPress = new(64, 72, 88, 255);

	public const float TitleSize = 46f;
	public const float HeadingSize = 30f;
	public const float BodySize = 21f;
	public const float SmallSize = 15f;

	public static SpriteFontBase TitleFont { get; private set; } = null!;

	public static SpriteFontBase HeadingFont { get; private set; } = null!;

	public static SpriteFontBase BodyFont { get; private set; } = null!;

	public static SpriteFontBase SmallFont { get; private set; } = null!;

	public static ButtonStyle MenuButtonStyle { get; private set; } = null!;

	/// <summary>Call once after Myra's game is assigned and before building menu widgets.</summary>
	public static void Initialize()
	{
		var fonts = DefaultAssets.DebugFontSystem;
		TitleFont = fonts.GetFont(TitleSize);
		HeadingFont = fonts.GetFont(HeadingSize);
		BodyFont = fonts.GetFont(BodySize);
		SmallFont = fonts.GetFont(SmallSize);

		MenuButtonStyle = new ButtonStyle(Stylesheet.Current.ButtonStyle)
		{
			Background = new SolidBrush(Panel),
			OverBackground = new SolidBrush(PanelOver),
			FocusedBackground = new SolidBrush(PanelFocus),
			PressedBackground = new SolidBrush(PanelPress),
			Border = new SolidBrush(Steel),
			OverBorder = new SolidBrush(Parchment),
			FocusedBorder = new SolidBrush(Accent),
			PressedBorder = new SolidBrush(Accent),
			BorderThickness = new Thickness(2),
			Padding = new Thickness(18, 7),
			Margin = new Thickness(0, 2),
			MinWidth = 300
		};
	}

	public static void StyleButton(Button button)
	{
		button.ApplyButtonStyle(MenuButtonStyle);
		if (button.Content is Label label)
		{
			StyleLabel(label, BodyFont, Parchment);
		}
	}

	public static void StyleLabel(Label label, SpriteFontBase font, Color color)
	{
		label.Font = font;
		label.TextColor = color;
	}
}
