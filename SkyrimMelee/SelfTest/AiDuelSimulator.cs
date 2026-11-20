using SkyrimMelee.Ai;
using SkyrimMelee.Animation;
using SkyrimMelee.Attacks;
using SkyrimMelee.Combat;
using SkyrimMelee.Input;
using SkyrimMelee.Tuning;

namespace SkyrimMelee.SelfTest;

public readonly record struct AiDuelResult(
	string Winner,
	int FirstHits,
	int SecondHits,
	int FirstBlocks,
	int SecondBlocks,
	float FirstDamage,
	float SecondDamage,
	float Seconds);

/// <summary>
/// Headless AI-vs-AI duel at a fixed 1.6 m spacing. Both sides run the real
/// state machines, stamina, guard/defense resolution and stagger rules; only
/// movement and rendering are skipped. Used to compare difficulty profiles.
/// </summary>
public static class AiDuelSimulator
{
	private const float Tick = 1f / 60f;
	private const float Spacing = 1.6f;

	public static AiDuelResult Run(
		AiProfile firstProfile, AiProfile secondProfile, int seed, float timeLimit = 30f)
	{
		var attacks = AttackCatalog.Create(SwordClips.Create());
		var slashLeft = Find(attacks, "SlashLeft");
		var slashRight = Find(attacks, "SlashRight");
		var power = Find(attacks, "PowerSlash");
		var bash = Find(attacks, "Bash");

		var firstFsm = new CombatStateMachine();
		var secondFsm = new CombatStateMachine();
		var firstStats = new FighterStats();
		var secondStats = new FighterStats();
		var firstAi = new DuelistAi(firstProfile, seed);
		var secondAi = new DuelistAi(secondProfile, seed + 1000);

		AttackDefinition? firstAttack = null;
		AttackDefinition? secondAttack = null;
		var firstResolved = false;
		var secondResolved = false;
		var firstHits = 0;
		var secondHits = 0;
		var firstBlocks = 0;
		var secondBlocks = 0;
		var firstDamage = 0f;
		var secondDamage = 0f;
		var elapsed = 0f;

		while (elapsed < timeLimit && !firstStats.IsDead && !secondStats.IsDead)
		{
			firstFsm.Update(Tick);
			secondFsm.Update(Tick);

			firstAi.Update(Tick, Context(firstFsm, firstStats, secondFsm, secondStats));
			secondAi.Update(Tick, Context(secondFsm, secondStats, firstFsm, firstStats));

			if (firstAi.Intent is { } firstIntent)
			{
				TryAttack(firstFsm, firstStats, attacks, firstIntent,
					slashLeft, slashRight, power, bash, ref firstAttack, ref firstResolved);
			}

			if (secondAi.Intent is { } secondIntent)
			{
				TryAttack(secondFsm, secondStats, attacks, secondIntent,
					slashLeft, slashRight, power, bash, ref secondAttack, ref secondResolved);
			}

			ApplyGuard(firstFsm, firstAi.GuardHeld);
			ApplyGuard(secondFsm, secondAi.GuardHeld);

			ResolveActive(firstFsm, firstStats, secondFsm, secondStats,
				firstAttack, ref firstResolved, ref firstHits, ref firstBlocks, ref firstDamage);
			ResolveActive(secondFsm, secondStats, firstFsm, firstStats,
				secondAttack, ref secondResolved, ref secondHits, ref secondBlocks, ref secondDamage);

			firstStats.Update(Tick, firstFsm.Blocks);
			secondStats.Update(Tick, secondFsm.Blocks);

			if (firstStats.IsDead)
			{
				firstFsm.Kill();
			}

			if (secondStats.IsDead)
			{
				secondFsm.Kill();
			}

			if (firstFsm.State == CombatState.Idle)
			{
				firstAttack = null;
			}

			if (secondFsm.State == CombatState.Idle)
			{
				secondAttack = null;
			}

			elapsed += Tick;
		}

		var winner = firstStats.IsDead && secondStats.IsDead
			? "draw"
			: secondStats.IsDead
				? "first"
				: firstStats.IsDead
					? "second"
					: firstStats.Health == secondStats.Health
						? "draw"
						: firstStats.Health > secondStats.Health
							? "first"
							: "second";

		return new AiDuelResult(
			winner, firstHits, secondHits, firstBlocks, secondBlocks,
			firstDamage, secondDamage, elapsed);
	}

	private static AiContext Context(
		CombatStateMachine selfFsm, FighterStats selfStats,
		CombatStateMachine otherFsm, FighterStats otherStats) =>
		new(otherFsm.State, otherStats.Stamina, selfFsm.State, selfStats.Stamina, Spacing);

	private static AttackDefinition Find(
		IReadOnlyList<AttackDefinition> attacks, string name) =>
		attacks.Single(attack => attack.Name == name);

	private static void ApplyGuard(CombatStateMachine fsm, bool held)
	{
		if (held)
		{
			if (fsm.State == CombatState.Idle)
			{
				fsm.TryStartGuard();
			}

			return;
		}

		if (fsm.State is CombatState.GuardRaise or CombatState.GuardHold)
		{
			fsm.TryReleaseGuard();
		}
	}

	private static void TryAttack(
		CombatStateMachine fsm, FighterStats stats,
		IReadOnlyList<AttackDefinition> attacks, CombatIntent intent,
		AttackDefinition slashLeft, AttackDefinition slashRight,
		AttackDefinition power, AttackDefinition bash,
		ref AttackDefinition? active, ref bool resolved)
	{
		var attack = intent switch
		{
			CombatIntent.SlashLeft => slashLeft,
			CombatIntent.SlashRight => slashRight,
			CombatIntent.PowerSlash => power,
			CombatIntent.Bash => bash,
			_ => null
		};

		if (attack is null || !stats.CanAfford(attack) || !fsm.TryStartAttack(attack))
		{
			return;
		}

		stats.Spend(attack);
		active = attack;
		resolved = false;
	}

	private static void ResolveActive(
		CombatStateMachine attackerFsm, FighterStats attackerStats,
		CombatStateMachine defenderFsm, FighterStats defenderStats,
		AttackDefinition? attack, ref bool resolved,
		ref int hits, ref int blocks, ref float damageTaken)
	{
		if (attack is null || resolved || attackerFsm.State != CombatState.Active)
		{
			return;
		}

		resolved = true;
		var outcome = !defenderFsm.Blocks
			? HitOutcome.Clean
			: defenderFsm.StateTime < CombatTuning.GuardTimedBlockWindow
				? HitOutcome.TimedBlock
				: HitOutcome.Block;

		var defense = DefenseResolver.Resolve(
			attack, attackerStats, defenderStats, outcome);
		defenderStats.ApplyDamage(defense.Damage.Final);
		damageTaken += defense.Damage.Final;

		if (outcome == HitOutcome.Clean)
		{
			hits++;
			var stagger = StaggerRules.Resolve(attack, defenderStats);
			if (stagger.Staggered)
			{
				defenderFsm.ApplyStagger(stagger.Duration);
			}
		}
		else
		{
			blocks++;
			if (defense.GuardBroken)
			{
				defenderFsm.ApplyStagger(defense.DefenderStagger);
			}
			else if (defense.AttackerStagger > 0f)
			{
				attackerFsm.ApplyStagger(defense.AttackerStagger);
			}
		}
	}
}
