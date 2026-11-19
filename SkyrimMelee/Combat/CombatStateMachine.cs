
using SkyrimMelee.Tuning;
using SkyrimMelee.Attacks;
using SkyrimMelee.Physics;

namespace SkyrimMelee.Combat;

/// <summary>
/// Motion-only combat sequencing: idle / attack phases / guard / stagger / death.
/// The state times come from <see cref="CombatTuning"/>; rendering and damage are separate.
/// </summary>
public sealed class CombatStateMachine
{
	private static readonly Dictionary<CombatState, CombatState[]> Exits = new()
	{
		[CombatState.Idle] = new[]
		{
			CombatState.Windup, CombatState.GuardRaise, CombatState.GuardHold,
			CombatState.Stagger, CombatState.Dead
		},
		[CombatState.Windup] = new[]
		{
			CombatState.Active, CombatState.Stagger, CombatState.Dead
		},
		[CombatState.Active] = new[]
		{
			CombatState.Recovery, CombatState.Stagger, CombatState.Dead
		},
		[CombatState.Recovery] = new[]
		{
			CombatState.Idle, CombatState.Stagger, CombatState.Dead
		},
		[CombatState.GuardRaise] = new[]
		{
			CombatState.GuardHold, CombatState.GuardLower, CombatState.Windup,
			CombatState.Stagger, CombatState.Dead
		},
		[CombatState.GuardHold] = new[]
		{
			CombatState.GuardLower, CombatState.Windup, CombatState.Stagger, CombatState.Dead
		},
		[CombatState.GuardLower] = new[]
		{
			CombatState.Idle, CombatState.Stagger, CombatState.Dead
		},
		[CombatState.Stagger] = new[]
		{
			CombatState.Idle, CombatState.Dead
		},
		[CombatState.Dead] = new[]
		{
			CombatState.Idle
		}
	};

	private AttackDefinition? _attack;
	private float _stateTime;
	private float _staggerDuration;

	public CombatState State { get; private set; } = CombatState.Idle;

	public float StateTime => _stateTime;

	public AttackDefinition? Attack => _attack;

	public bool IsAttacking => State is CombatState.Windup or CombatState.Active or CombatState.Recovery;

	public bool IsFacingLocked => IsAttacking;

	public bool IsActive => State == CombatState.Active;

	public bool Blocks => State == CombatState.GuardHold;

	public bool IsDead => State == CombatState.Dead;

	/// <summary>Timed duration of the current state, or 0 for Idle / GuardHold / Dead.</summary>
	public float StateDuration => GetDuration() ?? 0f;

	/// <summary>Time since the current attack began, used to sample its clip.</summary>
	public float AttackTime
	{
		get
		{
			if (_attack is null)
			{
				return 0f;
			}

			return State switch
			{
				CombatState.Windup => _stateTime,
				CombatState.Active => _attack.Tuning.Windup + _stateTime,
				CombatState.Recovery => _attack.Tuning.Windup + _attack.Tuning.Active + _stateTime,
				_ => 0f
			};
		}
	}

	public static IReadOnlyDictionary<CombatState, CombatState[]> Transitions => Exits;

	public static bool CanTransition(CombatState from, CombatState to) =>
		Exits.TryGetValue(from, out var exits) && Array.IndexOf(exits, to) >= 0;

	public bool TryStartAttack(AttackDefinition attack)
	{
		if (attack is null)
		{
			throw new ArgumentNullException(nameof(attack));
		}

		if (!CanTransition(State, CombatState.Windup))
		{
			return false;
		}

		_attack = attack;
		Enter(CombatState.Windup);
		return true;
	}

	public bool TryStartGuard()
	{
		if (!CanTransition(State, CombatState.GuardRaise))
		{
			return false;
		}

		_attack = null;
		Enter(CombatState.GuardRaise);
		return true;
	}

	/// <summary>Re-enters GuardHold directly, e.g. after a bash while RMB is held.</summary>
	public bool TryResumeGuard()
	{
		if (!CanTransition(State, CombatState.GuardHold))
		{
			return false;
		}

		_attack = null;
		Enter(CombatState.GuardHold);
		return true;
	}

	public bool TryReleaseGuard()
	{
		if (!CanTransition(State, CombatState.GuardLower))
		{
			return false;
		}

		_attack = null;
		Enter(CombatState.GuardLower);
		return true;
	}

	public void ApplyStagger(float durationSeconds)
	{
		if (!CanTransition(State, CombatState.Stagger))
		{
			return;
		}

		_staggerDuration = MathF.Max(0.01f, durationSeconds);
		_attack = null;
		Enter(CombatState.Stagger);
	}

	public void Kill()
	{
		if (!CanTransition(State, CombatState.Dead))
		{
			return;
		}

		_attack = null;
		Enter(CombatState.Dead);
	}

	/// <summary>Forced reset; bypasses the transition table.</summary>
	public void Reset()
	{
		_attack = null;
		_staggerDuration = 0f;
		State = CombatState.Idle;
		_stateTime = 0f;
	}

	public void Update(float deltaSeconds)
	{
		if (deltaSeconds <= 0f || State == CombatState.Dead)
		{
			return;
		}

		_stateTime += deltaSeconds;
		var guard = 0;

		while (guard++ < 16)
		{
			var duration = GetDuration();
			if (duration is null || _stateTime < duration.Value)
			{
				break;
			}

			var overflow = _stateTime - duration.Value;
			var before = State;
			Advance();

			if (State == before)
			{
				break;
			}

			_stateTime = overflow;
		}
	}

	private void Advance()
	{
		switch (State)
		{
			case CombatState.Windup:
				Enter(CombatState.Active);
				break;
			case CombatState.Active:
				Enter(CombatState.Recovery);
				break;
			case CombatState.Recovery:
				_attack = null;
				Enter(CombatState.Idle);
				break;
			case CombatState.GuardRaise:
				Enter(CombatState.GuardHold);
				break;
			case CombatState.GuardLower:
				Enter(CombatState.Idle);
				break;
			case CombatState.Stagger:
				Enter(CombatState.Idle);
				break;
		}
	}

	private void Enter(CombatState state)
	{
		if (!CanTransition(State, state))
		{
			throw new InvalidOperationException($"Illegal combat transition {State} -> {state}.");
		}

		State = state;
		_stateTime = 0f;
	}

	private float? GetDuration() => State switch
	{
		CombatState.Windup => _attack?.Tuning.Windup,
		CombatState.Active => _attack?.Tuning.Active,
		CombatState.Recovery => _attack?.Tuning.Recovery,
		CombatState.GuardRaise => CombatTuning.GuardRaiseDuration,
		CombatState.GuardLower => CombatTuning.GuardLowerDuration,
		CombatState.Stagger => _staggerDuration,
		_ => null
	};
}
