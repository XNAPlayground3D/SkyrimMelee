using Microsoft.Xna.Framework;
using SkyrimMelee.Combat;

namespace SkyrimMelee.Tests;

public class BashRulesTests
{
	private static readonly Vector3 Origin = Vector3.Zero;
	private static readonly Vector3 Forward = Vector3.UnitX;

	[Fact]
	public void InsideMeleeRangeInFrontConnects()
	{
		Assert.True(BashRules.InRange(Origin, Forward, new Vector3(1.2f, 0f, 0f)));
	}

	[Fact]
	public void OutsideMeleeRangeDoesNotConnect()
	{
		Assert.False(BashRules.InRange(
			Origin, Forward, new Vector3(CombatTuning.BashReach + 0.3f, 0f, 0f)));
	}

	[Fact]
	public void BehindTheAttackerDoesNotConnect()
	{
		Assert.False(BashRules.InRange(Origin, Forward, new Vector3(-1.2f, 0f, 0f)));
	}

	[Fact]
	public void OutsideTheBashConeDoesNotConnect()
	{
		var side = new Vector3(0.3f, 0f, 1.2f);
		Assert.False(BashRules.InRange(Origin, Forward, side));
	}

	[Theory]
	[InlineData(CombatState.Idle, false)]
	[InlineData(CombatState.Windup, true)]
	[InlineData(CombatState.Active, true)]
	[InlineData(CombatState.Recovery, false)]
	[InlineData(CombatState.GuardRaise, false)]
	[InlineData(CombatState.GuardHold, false)]
	[InlineData(CombatState.Stagger, false)]
	public void OnlyAnAttackOrHeldGuardForcesAStagger(CombatState state, bool expected)
	{
		Assert.Equal(expected, BashRules.ForcesStagger(state, guarding: false));
	}

	[Fact]
	public void AHeldGuardIsAlwaysShoved()
	{
		Assert.True(BashRules.ForcesStagger(CombatState.GuardHold, guarding: true));
	}

	[Fact]
	public void RangeIsTheWeaponMeleeReachNotTheBladeTip()
	{
		Assert.True(CombatTuning.BashReach < 2f);
	}
}
