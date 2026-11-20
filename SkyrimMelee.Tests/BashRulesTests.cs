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

	[Fact]
	public void RangeIsTheWeaponMeleeReachNotTheBladeTip()
	{
		Assert.True(CombatTuning.BashReach < 2f);
	}
}
