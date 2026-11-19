using SkyrimMelee.Combat;
using SkyrimMelee.Input;

namespace SkyrimMelee.Tests;

public class AttackChargeTests
{
	[Fact]
	public void QuickTapIsATap()
	{
		var charge = new AttackCharge();

		Assert.Equal(ChargeResult.None, charge.Update(down: true, 0.1f));
		Assert.Equal(ChargeResult.Tap, charge.Update(down: false, 0.02f));
	}

	[Fact]
	public void HoldingFiresPowerOnce()
	{
		var charge = new AttackCharge();

		Assert.Equal(ChargeResult.None, charge.Update(down: true, 0.2f));
		Assert.Equal(ChargeResult.Power,
			charge.Update(down: true, CombatTuning.PowerSlashHoldTime));
		Assert.Equal(ChargeResult.None, charge.Update(down: true, 0.2f));
		Assert.Equal(ChargeResult.None, charge.Update(down: false, 0.02f));
	}

	[Fact]
	public void NewPressCanChargeAgain()
	{
		var charge = new AttackCharge();
		charge.Update(down: true, 0.4f);
		charge.Update(down: false, 0.02f);

		Assert.Equal(ChargeResult.None, charge.Update(down: true, 0.1f));
		Assert.Equal(ChargeResult.Power,
			charge.Update(down: true, CombatTuning.PowerSlashHoldTime));
	}

	[Fact]
	public void ResetDropsTheHold()
	{
		var charge = new AttackCharge();
		charge.Update(down: true, 0.1f);

		charge.Reset();

		Assert.Equal(0f, charge.HeldSeconds, 4);
		Assert.Equal(ChargeResult.None, charge.Update(down: false, 0.02f));
	}
}
