namespace SkyrimMelee.Input;

public enum CombatIntent
{
	SlashLeft,
	SlashRight,
	PowerSlash,
	Bash
}

/// <summary>
/// One-slot, newest-wins input buffer. An intent lives for 120 ms of real time
/// and is consumed on the first legal frame (or discarded when it expires).
/// </summary>
public sealed class InputRouter
{
	public const float BufferDuration = 0.12f;

	private CombatIntent? _pending;
	private float _age;

	public CombatIntent? Pending => _pending;

	public bool IsPending => _pending is not null;

	public float Age => _age;

	public void Push(CombatIntent intent)
	{
		_pending = intent;
		_age = 0f;
	}

	public bool TryPeek(out CombatIntent intent)
	{
		if (_pending is { } pending)
		{
			intent = pending;
			return true;
		}

		intent = default;
		return false;
	}

	public void Clear()
	{
		_pending = null;
		_age = 0f;
	}

	/// <summary>Advances the buffer; returns true when a pending intent expired.</summary>
	public bool Tick(float deltaSeconds)
	{
		if (_pending is null)
		{
			return false;
		}

		_age += deltaSeconds;
		if (_age <= BufferDuration)
		{
			return false;
		}

		_pending = null;
		_age = 0f;
		return true;
	}
}
