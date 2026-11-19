using SkyrimMelee.Combat;
using SkyrimMelee.Input;

namespace SkyrimMelee.Tests;

public class InputRouterTests
{
	private static AttackDefinition Attack(string name) =>
		AttackCatalog.Create(SwordClips.Create()).Single(attack => attack.Name == name);

	private static CombatStateMachine InRecovery()
	{
		var fsm = new CombatStateMachine();
		fsm.TryStartAttack(Attack("SlashLeft"));
		fsm.Update(0.6f);
		Assert.Equal(CombatState.Recovery, fsm.State);
		return fsm;
	}

	[Fact]
	public void NewestPressWins()
	{
		var router = new InputRouter();
		router.Push(CombatIntent.SlashLeft);
		router.Push(CombatIntent.PowerSlash);

		Assert.True(router.TryPeek(out var intent));
		Assert.Equal(CombatIntent.PowerSlash, intent);
		Assert.Equal(0f, router.Age, 4);
	}

	[Fact]
	public void PeekDoesNotConsume()
	{
		var router = new InputRouter();
		router.Push(CombatIntent.Bash);

		Assert.True(router.TryPeek(out _));
		Assert.True(router.TryPeek(out _));
		Assert.True(router.IsPending);

		router.Clear();
		Assert.False(router.TryPeek(out _));
	}

	[Fact]
	public void IntentExpiresAfterBufferDuration()
	{
		var router = new InputRouter();
		router.Push(CombatIntent.SlashRight);

		Assert.False(router.Tick(0.11f));
		Assert.True(router.IsPending);
		Assert.True(router.Tick(0.02f));
		Assert.False(router.IsPending);
	}

	[Fact]
	public void BufferedAttackFiresOnTheFirstLegalFrame()
	{
		var fsm = InRecovery();
		var router = new InputRouter();
		router.Push(CombatIntent.SlashLeft);
		router.Push(CombatIntent.PowerSlash);

		Assert.False(fsm.TryStartAttack(Attack("PowerSlash")));
		fsm.Update(0.4f);
		Assert.Equal(CombatState.Idle, fsm.State);

		Assert.True(router.TryPeek(out var intent));
		Assert.Equal(CombatIntent.PowerSlash, intent);
		Assert.True(fsm.TryStartAttack(Attack("PowerSlash")));
		router.Clear();
		Assert.False(router.IsPending);
	}

	[Fact]
	public void GuardIsAvailableOnTheFirstLegalFrame()
	{
		var fsm = InRecovery();

		Assert.False(fsm.TryStartGuard());
		fsm.Update(0.4f);
		Assert.Equal(CombatState.Idle, fsm.State);
		Assert.True(fsm.TryStartGuard());
	}
}
