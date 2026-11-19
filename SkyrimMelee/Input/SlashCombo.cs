using SkyrimMelee.Attacks;
using SkyrimMelee.Combat;

namespace SkyrimMelee.Input;

/// <summary>
/// Skyrim's rule: the swing follows the blade, not a counter. From the idle
/// right-side rest a tap always starts the same (left) cut; a tap pressed while
/// a slash is still playing answers from the other side, because the blade is
/// still on the side the cut finished on.
/// </summary>
public static class SlashCombo
{
	public static CombatIntent Next(CombatState state, AttackDefinition? current)
	{
		var chained = state is CombatState.Windup or CombatState.Active or CombatState.Recovery;

		if (chained && current?.Name == "SlashLeft")
		{
			return CombatIntent.SlashRight;
		}

		if (chained && current?.Name == "SlashRight")
		{
			return CombatIntent.SlashLeft;
		}

		return CombatIntent.SlashLeft;
	}
}
