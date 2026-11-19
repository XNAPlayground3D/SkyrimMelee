using Microsoft.Xna.Framework;

namespace SkyrimMelee.Combat;

public readonly record struct SelfTestResult(string Name, bool Passed, string Detail);

/// <summary>
/// Headless, deterministic combat scenarios. Each scenario builds its own
/// CombatWorld and scripts the blade sweeps / FSM transitions directly; no
/// graphics device, wall clock, or RNG is involved.
/// </summary>
public static class SelfTestRunner
{
	private const float FixedStep = 1f / 60f;

	public static IReadOnlyList<SelfTestResult> Run() => new[]
	{
		CleanHit(),
		BlockedHit(),
		TimedBlock(),
		PowerRejected(),
		PowerStagger(),
		PoiseStagger(),
		Bash(),
		GuardBreak(),
		StaggerInterrupt(),
		Whiff(),
		OneHitPerSwing(),
		TunnelingStress()
	};

	private static SelfTestResult CleanHit()
	{
		using var world = CreateWorld(out _, out var sweep);
		var hits = SweepThroughTorso(sweep, -0.5f, 0.9f);

		var outcome = GuardResolver.Classify(
			hits, FighterId.Opponent, false, 0f, -Vector3.UnitX,
			new Vector3(1f, 0f, 0f), new Vector3(-1f, 0f, 0f), out var contact);

		var passed = outcome == HitOutcome.Clean &&
					 contact.Hurtbox.Kind == HurtboxKind.Torso;
		return new SelfTestResult("clean hit", passed,
			$"outcome={outcome} contact={contact.Hurtbox.Kind}");
	}

	private static SelfTestResult BlockedHit()
	{
		using var world = CreateWorld(out var guard, out var sweep);
		var hits = SweepThroughGuard(sweep);

		var outcome = GuardResolver.Classify(
			hits, FighterId.Opponent, true, 0.4f, -Vector3.UnitX,
			new Vector3(1f, 0f, 0f), new Vector3(-1f, 0f, 0f), out var contact);

		var passed = outcome == HitOutcome.Block &&
					 contact.Hurtbox.Kind == HurtboxKind.GuardBlade &&
					 world.IsGuardBladeRegistered(FighterId.Opponent);
		return new SelfTestResult("blocked hit", passed,
			$"outcome={outcome} contact={contact.Hurtbox.Kind} guard={guard}");
	}

	private static SelfTestResult TimedBlock()
	{
		using var world = CreateWorld(out _, out var sweep);
		var hits = SweepThroughGuard(sweep);

		var outcome = GuardResolver.Classify(
			hits, FighterId.Opponent, true, 0.1f, -Vector3.UnitX,
			new Vector3(1f, 0f, 0f), new Vector3(-1f, 0f, 0f), out _);

		return new SelfTestResult("timed block", outcome == HitOutcome.TimedBlock,
			$"outcome={outcome}");
	}

	private static SelfTestResult PowerRejected()
	{
		var attack = AttackCatalog.Create(SwordClips.Create())
			.Single(candidate => candidate.Name == "PowerSlash");
		var stats = new FighterStats(stamina: 5f);
		var fsm = new CombatStateMachine();

		var accepted = stats.CanAfford(attack) && fsm.TryStartAttack(attack);
		var passed = !accepted &&
					 fsm.State == CombatState.Idle &&
					 MathF.Abs(stats.Stamina - 5f) < 1e-4f;

		return new SelfTestResult("power rejected", passed,
			$"accepted={accepted} stamina={stats.Stamina:0.0} state={fsm.State}");
	}

	private static SelfTestResult PowerStagger()
	{
		var attack = AttackCatalog.Create(SwordClips.Create())
			.Single(candidate => candidate.Name == "PowerSlash");
		var stats = new FighterStats();
		var result = StaggerRules.Resolve(attack, stats);
		var fsm = new CombatStateMachine();
		fsm.ApplyStagger(result.Duration);

		var passed = result.Staggered &&
					 MathF.Abs(result.Duration - CombatTuning.StaggerLargeDuration) < 1e-4f &&
					 fsm.State == CombatState.Stagger;

		return new SelfTestResult("power stagger", passed,
			$"staggered={result.Staggered} duration={result.Duration:0.00} state={fsm.State}");
	}

	private static SelfTestResult PoiseStagger()
	{
		var attack = AttackCatalog.Create(SwordClips.Create())
			.Single(candidate => candidate.Name == "SlashLeft");
		var stats = new FighterStats();
		var fsm = new CombatStateMachine();
		var staggered = false;
		var hits = 0;

		while (!staggered && hits < 20)
		{
			var result = StaggerRules.Resolve(attack, stats);
			hits++;
			if (result.Staggered)
			{
				fsm.ApplyStagger(result.Duration);
				staggered = true;
			}
		}

		var locked = staggered &&
					 fsm.State == CombatState.Stagger &&
					 !fsm.TryStartAttack(attack) &&
					 !fsm.TryStartGuard();

		return new SelfTestResult("poise stagger", locked && hits == 10,
			$"hits={hits} staggered={staggered} state={fsm.State}");
	}

	private static SelfTestResult Bash()
	{
		var attack = AttackCatalog.Create(SwordClips.Create())
			.Single(candidate => candidate.Name == "Bash");
		var fsm = new CombatStateMachine();
		fsm.TryStartGuard();
		fsm.Update(CombatTuning.GuardRaiseDuration);

		var stats = new FighterStats();
		var accepted = stats.CanAfford(attack) && fsm.TryStartAttack(attack);
		stats.Spend(attack);

		var stagger = StaggerRules.Resolve(attack, new FighterStats());
		var passed = accepted &&
					 fsm.State == CombatState.Windup &&
					 MathF.Abs(stats.Stamina - 85f) < 1e-4f &&
					 !stagger.Staggered;

		return new SelfTestResult("bash", passed,
			$"accepted={accepted} stamina={stats.Stamina:0.0} stagger={stagger.Staggered}");
	}

	private static SelfTestResult GuardBreak()
	{
		using var world = CreateWorld(out _, out _);
		var fsm = new CombatStateMachine();
		fsm.TryStartGuard();
		fsm.Update(CombatTuning.GuardRaiseDuration);

		var guarding = fsm.State == CombatState.GuardHold;
		world.UnregisterGuardBlade(FighterId.Opponent);
		fsm.ApplyStagger(CombatTuning.StaggerLargeDuration);

		var staggered = fsm.State == CombatState.Stagger &&
						MathF.Abs(fsm.StateDuration - CombatTuning.StaggerLargeDuration) < 1e-4f;
		fsm.Update(CombatTuning.StaggerLargeDuration);
		var recovered = fsm.State == CombatState.Idle;
		var guardGone = !world.IsGuardBladeRegistered(FighterId.Opponent);

		return new SelfTestResult("guard break",
			guarding && staggered && recovered && guardGone,
			$"guard={guarding} stagger={staggered} recovered={recovered} removed={guardGone}");
	}

	private static SelfTestResult StaggerInterrupt()
	{
		var attack = AttackCatalog.Create(SwordClips.Create())
			.Single(candidate => candidate.Name == "SlashLeft");
		var fsm = new CombatStateMachine();
		fsm.TryStartAttack(attack);
		fsm.Update(0.1f);

		var windingUp = fsm.State == CombatState.Windup;
		fsm.ApplyStagger(CombatTuning.StaggerSmallDuration);
		var interrupted = fsm.State == CombatState.Stagger && fsm.Attack is null;
		fsm.Update(CombatTuning.StaggerSmallDuration);

		return new SelfTestResult("stagger interrupt",
			windingUp && interrupted && fsm.State == CombatState.Idle,
			$"windup={windingUp} interrupted={interrupted} state={fsm.State}");
	}

	private static SelfTestResult Whiff()
	{
		using var world = CreateWorld(out _, out var sweep);
		var previous = Blade(new Vector3(-2f, 1.2f, 1.8f), new Vector3(-2f, 1.2f, 2.6f));
		var current = Blade(new Vector3(-1.4f, 1.2f, 1.8f), new Vector3(-1.4f, 1.2f, 2.6f));
		var hits = sweep.Sweep(previous, current, FixedStep);

		return new SelfTestResult("whiff", hits.Count == 0, $"hits={hits.Count}");
	}

	private static SelfTestResult OneHitPerSwing()
	{
		var attack = AttackCatalog.Create(SwordClips.Create())
			.Single(candidate => candidate.Name == "SlashLeft");
		var set = new SwingHitSet();
		set.Begin(attack);

		var hit = new BladeHit(
			new Hurtbox(FighterId.Opponent, HurtboxKind.Torso),
			new Vector3(1f, 1.2f, 0f),
			Vector3.UnitX,
			0.01f);

		var first = set.TryRegister(hit, FighterId.Player, attack.WindupEnd, out _);
		var second = set.TryRegister(hit, FighterId.Player, attack.WindupEnd, out _);

		return new SelfTestResult("one hit per swing",
			first && !second && set.Count == 1,
			$"first={first} second={second} count={set.Count}");
	}

	private static SelfTestResult TunnelingStress()
	{
		using var world = CreateWorld(out _, out var sweep);
		var hits = SweepThroughTorso(sweep, -1f, 1.5f);
		var torsoHits = hits.Count(hit => hit.Hurtbox.Kind == HurtboxKind.Torso);

		return new SelfTestResult("tunneling stress",
			torsoHits == 1, $"torso hits={torsoHits} total={hits.Count}");
	}

	private static CombatWorld CreateWorld(out bool guardRegistered, out BladeSweep sweep)
	{
		var world = new CombatWorld();
		world.AddFighter(FighterId.Opponent, Matrix.CreateTranslation(1f, 0f, 0f));

		var guard = new BladeGeometry(
			new Vector3(0.5f, 0.7f, 0f),
			new Vector3(0.5f, 0.7f, 0f),
			new Vector3(0.5f, 1.7f, 0f),
			0.04f,
			0.02f);
		world.RegisterGuardBlade(FighterId.Opponent, guard);
		guardRegistered = true;

		sweep = new BladeSweep(world);
		return world;
	}

	private static IReadOnlyList<BladeHit> SweepThroughTorso(
		BladeSweep sweep, float startX, float endX)
	{
		var previous = Blade(new Vector3(startX, 1.2f, -0.2f), new Vector3(startX, 1.2f, 0.2f));
		var current = Blade(new Vector3(endX, 1.2f, -0.2f), new Vector3(endX, 1.2f, 0.2f));
		return sweep.Sweep(previous, current, FixedStep);
	}

	private static IReadOnlyList<BladeHit> SweepThroughGuard(BladeSweep sweep)
	{
		var previous = Blade(new Vector3(0.2f, 1.2f, -0.3f), new Vector3(0.2f, 1.2f, 0.7f));
		var current = Blade(new Vector3(0.9f, 1.2f, -0.3f), new Vector3(0.9f, 1.2f, 0.7f));
		return sweep.Sweep(previous, current, FixedStep);
	}

	private static BladeGeometry Blade(Vector3 basePoint, Vector3 tip) =>
		new(basePoint, basePoint, tip, 0.04f, 0.02f);
}
