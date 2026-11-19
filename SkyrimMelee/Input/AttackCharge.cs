using SkyrimMelee.Tuning;

namespace SkyrimMelee.Input;

public enum ChargeResult
{
	None,
	Tap,
	Power
}

/// <summary>
/// Turns a held attack button into a tap attack or a charged power attack.
/// A quick release taps; holding past <see cref="CombatTuning.PowerSlashHoldTime"/>
/// fires one power attack and suppresses the release tap.
/// </summary>
public sealed class AttackCharge
{
	private bool _wasDown;
	private float _held;
	private bool _charged;

	public float HeldSeconds => _held;

	public ChargeResult Update(bool down, float deltaSeconds)
	{
		var result = ChargeResult.None;

		if (down && !_wasDown)
		{
			_held = 0f;
			_charged = false;
		}

		if (down)
		{
			_held += MathF.Max(0f, deltaSeconds);
			if (!_charged && _held >= CombatTuning.PowerSlashHoldTime)
			{
				_charged = true;
				result = ChargeResult.Power;
			}
		}
		else if (_wasDown && !_charged)
		{
			result = ChargeResult.Tap;
		}

		_wasDown = down;
		return result;
	}

	public void Reset()
	{
		_wasDown = false;
		_held = 0f;
		_charged = false;
	}
}
