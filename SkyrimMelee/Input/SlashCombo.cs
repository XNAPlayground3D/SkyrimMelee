using SkyrimMelee.Combat;

namespace SkyrimMelee.Input;

/// <summary>
/// Skyrim-style attack combo: each tap swings the other way, starting left.
/// The numeric keys still force a side for practice.
/// </summary>
public sealed class SlashCombo
{
	private bool _right;

	public CombatIntent Next()
	{
		var intent = _right ? CombatIntent.SlashRight : CombatIntent.SlashLeft;
		_right = !_right;
		return intent;
	}

	public void Reset() => _right = false;
}
