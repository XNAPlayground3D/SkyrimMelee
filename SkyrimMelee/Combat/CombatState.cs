using SkyrimMelee.Tuning;
using SkyrimMelee.Attacks;
using SkyrimMelee.Physics;

namespace SkyrimMelee.Combat;

public enum CombatState
{
	Idle,
	Windup,
	Active,
	Recovery,
	GuardRaise,
	GuardHold,
	GuardLower,
	Stagger,
	Dead
}
