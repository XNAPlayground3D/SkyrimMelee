using SkyrimMelee.Combat;

namespace SkyrimMelee.Tests;

public class CombatStateMachineTests
{
	private static AttackDefinition Slash() =>
		AttackCatalog.Create(SwordClips.Create()).Single(a => a.Name == "SlashLeft");

	[Fact]
	public void StartsIdle()
	{
		var fsm = new CombatStateMachine();

		Assert.Equal(CombatState.Idle, fsm.State);
		Assert.Equal(0f, fsm.StateTime);
		Assert.False(fsm.IsAttacking);
		Assert.False(fsm.Blocks);
	}

	[Fact]
	public void AttackWalksWindupActiveRecoveryIdle()
	{
		var fsm = new CombatStateMachine();

		Assert.True(fsm.TryStartAttack(Slash()));
		Assert.Equal(CombatState.Windup, fsm.State);
		Assert.Equal(0.25f, fsm.StateDuration, 5);

		fsm.Update(0.25f);
		Assert.Equal(CombatState.Active, fsm.State);
		Assert.Equal(0.25f, fsm.AttackTime, 5);

		fsm.Update(0.22f);
		Assert.Equal(CombatState.Recovery, fsm.State);
		Assert.Equal(0.47f, fsm.AttackTime, 5);

		fsm.Update(0.35f);
		Assert.Equal(CombatState.Idle, fsm.State);
		Assert.Null(fsm.Attack);
		Assert.False(fsm.IsAttacking);
	}

	[Fact]
	public void CommitLockRejectsGuardAndNewAttackMidAttack()
	{
		var fsm = new CombatStateMachine();
		var attack = Slash();

		Assert.True(fsm.TryStartAttack(attack));
		fsm.Update(0.3f); // Active

		Assert.False(fsm.TryStartAttack(Slash()));
		Assert.False(fsm.TryStartGuard());
		Assert.False(fsm.TryReleaseGuard());
		Assert.Equal(CombatState.Active, fsm.State);
		Assert.Same(attack, fsm.Attack);
	}

	[Fact]
	public void StaggerInterruptsAttackAndReturnsToIdle()
	{
		var fsm = new CombatStateMachine();

		Assert.True(fsm.TryStartAttack(Slash()));
		fsm.Update(0.3f);
		fsm.ApplyStagger(0.25f);

		Assert.Equal(CombatState.Stagger, fsm.State);
		Assert.Null(fsm.Attack);
		Assert.False(fsm.TryStartAttack(Slash()));

		fsm.Update(0.24f);
		Assert.Equal(CombatState.Stagger, fsm.State);

		fsm.Update(0.01f);
		Assert.Equal(CombatState.Idle, fsm.State);
	}

	[Fact]
	public void DeathBeatsStaggerAndIsTerminalUntilReset()
	{
		var fsm = new CombatStateMachine();

		fsm.ApplyStagger(0.25f);
		fsm.Kill();

		Assert.Equal(CombatState.Dead, fsm.State);
		Assert.True(fsm.IsDead);

		fsm.ApplyStagger(1f);
		fsm.Update(10f);
		Assert.Equal(CombatState.Dead, fsm.State);
		Assert.False(fsm.TryStartAttack(Slash()));
		Assert.False(fsm.TryStartGuard());

		fsm.Reset();
		Assert.Equal(CombatState.Idle, fsm.State);
		Assert.False(fsm.IsDead);
	}

	[Fact]
	public void GuardRaisesHoldsAndLowers()
	{
		var fsm = new CombatStateMachine();

		Assert.True(fsm.TryStartGuard());
		Assert.Equal(CombatState.GuardRaise, fsm.State);
		Assert.False(fsm.Blocks);

		fsm.Update(CombatTuning.GuardRaiseDuration);
		Assert.Equal(CombatState.GuardHold, fsm.State);
		Assert.True(fsm.Blocks);

		fsm.Update(1f);
		Assert.Equal(CombatState.GuardHold, fsm.State);

		Assert.True(fsm.TryReleaseGuard());
		Assert.Equal(CombatState.GuardLower, fsm.State);
		Assert.False(fsm.Blocks);

		fsm.Update(CombatTuning.GuardLowerDuration);
		Assert.Equal(CombatState.Idle, fsm.State);
	}

	[Fact]
	public void GuardResumesDirectlyFromIdle()
	{
		var fsm = new CombatStateMachine();

		Assert.True(fsm.TryResumeGuard());
		Assert.Equal(CombatState.GuardHold, fsm.State);
		Assert.True(fsm.Blocks);

		var attacking = new CombatStateMachine();
		attacking.TryStartAttack(Slash());
		Assert.False(attacking.TryResumeGuard());
	}

	[Fact]
	public void AttackCanStartFromGuard()
	{
		var fsm = new CombatStateMachine();

		Assert.True(fsm.TryStartGuard());
		fsm.Update(CombatTuning.GuardRaiseDuration);
		Assert.Equal(CombatState.GuardHold, fsm.State);

		Assert.True(fsm.TryStartAttack(Slash()));
		Assert.Equal(CombatState.Windup, fsm.State);
		Assert.False(fsm.Blocks);
		Assert.NotNull(fsm.Attack);
	}

	[Fact]
	public void FacingLockAndActiveFollowTheAttackPhases()
	{
		var fsm = new CombatStateMachine();
		Assert.False(fsm.IsFacingLocked);

		Assert.True(fsm.TryStartAttack(Slash()));
		Assert.True(fsm.IsFacingLocked);
		Assert.False(fsm.IsActive);

		fsm.Update(0.25f);
		Assert.Equal(CombatState.Active, fsm.State);
		Assert.True(fsm.IsActive);
		Assert.True(fsm.IsFacingLocked);

		fsm.Update(0.22f);
		Assert.Equal(CombatState.Recovery, fsm.State);
		Assert.False(fsm.IsActive);
		Assert.True(fsm.IsFacingLocked);

		fsm.Update(0.35f);
		Assert.False(fsm.IsFacingLocked);
	}

	[Fact]
	public void ComboWindowStartsTheNextSwingDuringRecovery()
	{
		var fsm = new CombatStateMachine();
		Assert.True(fsm.TryStartAttack(Slash()));

		fsm.Update(0.5f); // recovery + 0.03 s: still before the cancel time
		Assert.Equal(CombatState.Recovery, fsm.State);
		Assert.False(fsm.TryStartAttack(Slash()));

		fsm.Update(CombatTuning.ComboCancelTime);
		Assert.True(fsm.TryStartAttack(Slash()));
		Assert.Equal(CombatState.Windup, fsm.State);
	}

	[Fact]
	public void GuardStillWaitsForIdleAfterAnAttack()
	{
		var fsm = new CombatStateMachine();
		Assert.True(fsm.TryStartAttack(Slash()));

		fsm.Update(0.5f);
		Assert.False(fsm.TryStartGuard());

		fsm.Update(0.35f);
		Assert.Equal(CombatState.Idle, fsm.State);
		Assert.True(fsm.TryStartGuard());
	}

	[Fact]
	public void LargeDeltaConsumesEveryPhaseAndEndsIdle()
	{
		var fsm = new CombatStateMachine();

		Assert.True(fsm.TryStartAttack(Slash()));
		fsm.Update(10f);

		Assert.Equal(CombatState.Idle, fsm.State);
		Assert.Null(fsm.Attack);
	}

	[Fact]
	public void TransitionTableMatchesTheDesign()
	{
		Assert.True(CombatStateMachine.CanTransition(CombatState.Idle, CombatState.Windup));
		Assert.True(CombatStateMachine.CanTransition(CombatState.Idle, CombatState.GuardRaise));
		Assert.True(CombatStateMachine.CanTransition(CombatState.GuardHold, CombatState.Windup));
		Assert.True(CombatStateMachine.CanTransition(CombatState.Dead, CombatState.Idle));

		Assert.False(CombatStateMachine.CanTransition(CombatState.Windup, CombatState.Idle));
		Assert.False(CombatStateMachine.CanTransition(CombatState.Active, CombatState.GuardRaise));
		Assert.False(CombatStateMachine.CanTransition(CombatState.Stagger, CombatState.Windup));
	}
}
