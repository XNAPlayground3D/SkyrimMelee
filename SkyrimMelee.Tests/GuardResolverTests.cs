using Microsoft.Xna.Framework;
using SkyrimMelee.Combat;

namespace SkyrimMelee.Tests;

public class GuardResolverTests
{
	private static readonly Vector3 DefenderPosition = new(1f, 0f, 0f);
	private static readonly Vector3 DefenderForward = -Vector3.UnitX;
	private static readonly Vector3 AttackerPosition = new(-1f, 0f, 0f);

	private static BladeHit Hit(HurtboxKind kind, float timeOfImpact) =>
		new(
			new Hurtbox(FighterId.Opponent, kind),
			new Vector3(0.5f, 1.2f, 0f),
			Vector3.UnitX,
			timeOfImpact);

	private static HitOutcome Classify(
		IReadOnlyList<BladeHit> hits,
		out BladeHit contact,
		bool guarding = true,
		float guardAge = 0.4f,
		Vector3? attackerPosition = null) =>
		GuardResolver.Classify(
			hits,
			FighterId.Opponent,
			guarding,
			guardAge,
			DefenderForward,
			DefenderPosition,
			attackerPosition ?? AttackerPosition,
			out contact);

	[Fact]
	public void GuardFirstInFrontBlocks()
	{
		var outcome = Classify(
			new[] { Hit(HurtboxKind.GuardBlade, 0.01f), Hit(HurtboxKind.Torso, 0.02f) },
			out var contact);

		Assert.Equal(HitOutcome.Block, outcome);
		Assert.Equal(HurtboxKind.GuardBlade, contact.Hurtbox.Kind);
	}

	[Fact]
	public void FreshGuardIsATimedBlock()
	{
		var outcome = Classify(
			new[] { Hit(HurtboxKind.GuardBlade, 0.01f), Hit(HurtboxKind.Torso, 0.02f) },
			out _,
			guardAge: 0.1f);

		Assert.Equal(HitOutcome.TimedBlock, outcome);
	}

	[Fact]
	public void GuardFromBehindIsClean()
	{
		var outcome = Classify(
			new[] { Hit(HurtboxKind.GuardBlade, 0.01f), Hit(HurtboxKind.Torso, 0.02f) },
			out var contact,
			attackerPosition: new Vector3(2f, 0f, 0f));

		Assert.Equal(HitOutcome.Clean, outcome);
		Assert.Equal(HurtboxKind.Torso, contact.Hurtbox.Kind);
	}

	[Fact]
	public void LoweringGuardIsClean()
	{
		var outcome = Classify(
			new[] { Hit(HurtboxKind.GuardBlade, 0.01f), Hit(HurtboxKind.Torso, 0.02f) },
			out var contact,
			guarding: false);

		Assert.Equal(HitOutcome.Clean, outcome);
		Assert.Equal(HurtboxKind.Torso, contact.Hurtbox.Kind);
	}

	[Fact]
	public void BodyFirstIsClean()
	{
		var outcome = Classify(
			new[] { Hit(HurtboxKind.Torso, 0.01f), Hit(HurtboxKind.GuardBlade, 0.02f) },
			out var contact);

		Assert.Equal(HitOutcome.Clean, outcome);
		Assert.Equal(HurtboxKind.Torso, contact.Hurtbox.Kind);
	}

	[Fact]
	public void TiedTimeOfImpactFavoursTheGuard()
	{
		var outcome = Classify(
			new[] { Hit(HurtboxKind.Torso, 0.01f), Hit(HurtboxKind.GuardBlade, 0.01005f) },
			out _);

		Assert.Equal(HitOutcome.Block, outcome);
	}

	[Fact]
	public void NoGuardContactIsClean()
	{
		var outcome = Classify(
			new[] { Hit(HurtboxKind.Torso, 0.01f) },
			out var contact);

		Assert.Equal(HitOutcome.Clean, outcome);
		Assert.Equal(HurtboxKind.Torso, contact.Hurtbox.Kind);
	}

	[Fact]
	public void SweptGuardInFrontBlocks()
	{
		using var world = new CombatWorld();
		world.AddFighter(FighterId.Opponent, Matrix.CreateTranslation(1f, 0f, 0f));

		var guard = new BladeGeometry(
			new Vector3(0.5f, 0.7f, 0f),
			new Vector3(0.5f, 0.7f, 0f),
			new Vector3(0.5f, 1.7f, 0f),
			0.04f,
			0.02f);
		world.RegisterGuardBlade(FighterId.Opponent, guard);

		var sweep = new BladeSweep(world);
		var previous = new BladeGeometry(
			new Vector3(0.2f, 1.2f, -0.3f),
			new Vector3(0.2f, 1.2f, -0.3f),
			new Vector3(0.2f, 1.2f, 0.7f),
			0.04f,
			0.02f);
		var current = new BladeGeometry(
			new Vector3(0.9f, 1.2f, -0.3f),
			new Vector3(0.9f, 1.2f, -0.3f),
			new Vector3(0.9f, 1.2f, 0.7f),
			0.04f,
			0.02f);

		var hits = sweep.Sweep(previous, current, 1f / 60f);
		Assert.Contains(hits, hit => hit.Hurtbox.Kind == HurtboxKind.GuardBlade);

		var outcome = GuardResolver.Classify(
			hits,
			FighterId.Opponent,
			isGuarding: true,
			guardAge: 0.4f,
			defenderForward: -Vector3.UnitX,
			defenderPosition: new Vector3(1f, 0f, 0f),
			attackerPosition: new Vector3(-1f, 0f, 0f),
			out var contact);

		Assert.Equal(HitOutcome.Block, outcome);
		Assert.Equal(HurtboxKind.GuardBlade, contact.Hurtbox.Kind);
	}

	[Fact]
	public void InFrontUsesTheHalfAngle()
	{
		Assert.True(GuardResolver.InFront(DefenderForward, DefenderPosition, AttackerPosition));
		Assert.True(GuardResolver.InFront(
			DefenderForward, DefenderPosition, new Vector3(0f, 0f, -1f)));
		Assert.False(GuardResolver.InFront(
			DefenderForward, DefenderPosition, new Vector3(1f, 0f, -2f)));
	}
}
