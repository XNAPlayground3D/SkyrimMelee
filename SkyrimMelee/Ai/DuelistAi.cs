using SkyrimMelee.Combat;
using SkyrimMelee.Input;
using SkyrimMelee.Tuning;

namespace SkyrimMelee.Ai;

/// <summary>A snapshot the AI is allowed to see on this tick.</summary>
public readonly record struct AiContext(
	CombatState PlayerState,
	float PlayerStamina,
	CombatState SelfState,
	float SelfStamina,
	float Distance);

/// <summary>
/// Deterministic duelist policy. It only emits a guard wish and one
/// <see cref="CombatIntent"/>; the game runs both through the same state
/// machine, stamina checks, and hit resolution as the player.
/// </summary>
public sealed class DuelistAi
{
	private readonly AiProfile _profile;
	private readonly Random _random;

	private bool _sawWindup;
	private bool _timedRead;
	private float _guardDelay = -1f;
	private float _guardHold;
	private float _turtleTimer;
	private float _attackCooldown;

	public DuelistAi(AiProfile profile, int seed)
	{
		_profile = profile;
		_random = new Random(seed);
	}

	public bool GuardHeld => _guardHold > 0f;

	public CombatIntent? Intent { get; private set; }

	/// <summary>Desired spacing: positive walks toward the player, negative backs off.</summary>
	public float Approach { get; private set; }

	public AiProfile Profile => _profile;

	/// <summary>The last choice the AI committed to, or null before the first one.</summary>
	public AiDecision? LastDecision { get; private set; }

	/// <summary>Increments once per committed choice; the game uses it to log each event once.</summary>
	public long DecisionId { get; private set; }

	/// <summary>Seconds left on the pending guard read; negative when no read is pending.</summary>
	public float ReactionRemaining => _guardDelay;

	public float GuardRemaining => _guardHold;

	public void Reset()
	{
		_sawWindup = false;
		_timedRead = false;
		_guardDelay = -1f;
		_guardHold = 0f;
		_turtleTimer = 0f;
		_attackCooldown = 0f;
		Intent = null;
		Approach = 0f;
		LastDecision = null;
		DecisionId = 0;
	}

	public void Update(float step, in AiContext ctx)
	{
		Intent = null;
		Approach = 0f;
		_attackCooldown = MathF.Max(0f, _attackCooldown - step);
		_turtleTimer = MathF.Max(0f, _turtleTimer - step);
		_guardHold = MathF.Max(0f, _guardHold - step);

		if (ctx.SelfState == CombatState.Dead || ctx.PlayerState == CombatState.Dead)
		{
			return;
		}

		// Spacing: close in when out of reach, give ground when too close, brace
		// while the player's attack is committed.
		if (ctx.SelfState is CombatState.Idle or CombatState.GuardRaise or CombatState.GuardHold)
		{
			if (ctx.PlayerState is CombatState.Windup or CombatState.Active)
			{
				Approach = 0f;
			}
			else if (ctx.Distance > 2.3f)
			{
				Approach = 1f;
			}
			else if (ctx.Distance < 1.35f)
			{
				Approach = -1f;
			}
		}

		// React to a new wind-up after a profile reaction delay.
		if (ctx.PlayerState == CombatState.Windup && !_sawWindup)
		{
			_sawWindup = true;
			var reaction = _profile.ReactionSeconds *
				(0.9f + 0.2f * (float)_random.NextDouble());
			_guardDelay = MathF.Max(0f, reaction - CombatTuning.GuardRaiseDuration);

			// A timed-block read: start the raise slightly later so the guard is
			// fresh when the hit lands, without finishing after the active window.
			_timedRead = Roll(_profile.TimedBlockChance);
			if (_timedRead)
			{
				_guardDelay = MathF.Max(_guardDelay, 0.04f);
			}
		}
		else if (ctx.PlayerState is not (CombatState.Windup or CombatState.Active))
		{
			_sawWindup = false;
		}

		if (_guardDelay >= 0f)
		{
			_guardDelay -= step;
			if (_guardDelay < 0f &&
				ctx.PlayerState is CombatState.Windup or CombatState.Active)
			{
				if (Roll(_profile.BlockChance))
				{
					_guardHold = 1.2f;
					Decide("guard", _timedRead ? "timed block" : "wind-up read");
				}
				else
				{
					Decide("hold", "block roll failed");
				}
			}
		}

		// Pre-guard sometimes, which produces normal blocks instead of timed ones.
		if (ctx.SelfState == CombatState.Idle &&
			ctx.PlayerState == CombatState.Idle &&
			ctx.Distance <= 2.5f &&
			_turtleTimer <= 0f)
		{
			_turtleTimer = 0.5f;
			if (Roll(_profile.TurtleChance))
			{
				_guardHold = 1.0f;
				Decide("guard", "pre-guard");
			}
		}

		if (ctx.SelfState is not (CombatState.Idle or CombatState.GuardRaise or
				CombatState.GuardHold))
		{
			return;
		}

		// Punish recovery, including out of a held guard.
		if (ctx.PlayerState == CombatState.Recovery &&
			_attackCooldown <= 0f &&
			Roll(_profile.Aggression))
		{
			_guardHold = 0f;
			var attack = ChooseAttack(ctx);
			Intent = attack;
			Decide(attack.ToString(), "punish recovery");
			_attackCooldown = 1.3f;
			return;
		}

		if (ctx.SelfState != CombatState.Idle)
		{
			return;
		}

		// Greedy mistake: swing into a committed wind-up instead of guarding.
		if (ctx.PlayerState == CombatState.Windup &&
			_attackCooldown <= 0f &&
			Roll(_profile.MistakeChance))
		{
			_guardHold = 0f;
			var attack = ChooseAttack(ctx);
			Intent = attack;
			Decide(attack.ToString(), "greedy trade");
			_attackCooldown = 1.4f;
			return;
		}

		// Punish a held guard with a power attack (guard-break pressure).
		if (ctx.PlayerState is CombatState.GuardRaise or CombatState.GuardHold &&
			ctx.Distance <= 2.2f &&
			_attackCooldown <= 0f &&
			Roll(_profile.Aggression * 0.6f))
		{
			_guardHold = 0f;
			var attack = ctx.SelfStamina >= 30f &&
						 Roll(_profile.PowerChance)
				? CombatIntent.PowerSlash
				: _random.Next(2) == 0
					? CombatIntent.SlashLeft
					: CombatIntent.SlashRight;
			Intent = attack;
			Decide(attack.ToString(), "guard pressure");
			_attackCooldown = 1.3f;
			return;
		}

		// Pressure an idle player at close range.
		if (ctx.PlayerState == CombatState.Idle &&
			ctx.Distance <= 2.2f &&
			_attackCooldown <= 0f &&
			Roll(_profile.Aggression * 0.5f))
		{
			_guardHold = 0f;
			var attack = ChooseAttack(ctx);
			Intent = attack;
			Decide(attack.ToString(), "idle pressure");
			_attackCooldown = 1.6f;
		}
	}

	private CombatIntent ChooseAttack(in AiContext ctx)
	{
		// Power attacks are punish tools: the long wind-up loses a raw trade, so
		// only use one on a recovering or low-stamina target.
		var power = ctx.SelfStamina >= 30f &&
			(ctx.PlayerState == CombatState.Recovery ||
			 ctx.PlayerStamina < 20f) &&
			Roll(_profile.PowerChance);

		return power
			? CombatIntent.PowerSlash
			: _random.Next(2) == 0
				? CombatIntent.SlashLeft
				: CombatIntent.SlashRight;
	}

	private bool Roll(float chance) => _random.NextDouble() < chance;

	private void Decide(string action, string reason)
	{
		LastDecision = new AiDecision(action, reason);
		DecisionId++;
	}
}
