namespace SkyrimMelee.Tuning;

public enum TuningKey
{
	HitFlash,
	StaggerMicro,
	StaggerSmall,
	StaggerLarge
}

/// <summary>
/// Session-only overrides for timings that do not rebuild clips. The HUD prints
/// the nudged value as a `CombatTuning` line so it can be pasted back unchanged.
/// </summary>
public sealed class TuningSession
{
	public const float Step = 0.01f;
	public const float MinValue = 0.01f;
	public const float MaxValue = 3f;

	private readonly Dictionary<TuningKey, float> _overrides = new();

	public TuningKey Selected { get; private set; } = TuningKey.HitFlash;

	public string LastChange { get; private set; } = "-";

	public static int Count => 4;

	public static float Default(TuningKey key) => key switch
	{
		TuningKey.HitFlash => CombatTuning.HitFlashDuration,
		TuningKey.StaggerMicro => CombatTuning.StaggerMicroDuration,
		TuningKey.StaggerSmall => CombatTuning.StaggerSmallDuration,
		TuningKey.StaggerLarge => CombatTuning.StaggerLargeDuration,
		_ => 0f
	};

	public static string Name(TuningKey key) => key switch
	{
		TuningKey.HitFlash => nameof(CombatTuning.HitFlashDuration),
		TuningKey.StaggerMicro => nameof(CombatTuning.StaggerMicroDuration),
		TuningKey.StaggerSmall => nameof(CombatTuning.StaggerSmallDuration),
		TuningKey.StaggerLarge => nameof(CombatTuning.StaggerLargeDuration),
		_ => key.ToString()
	};

	public float Value(TuningKey key) =>
		_overrides.TryGetValue(key, out var value) ? value : Default(key);

	public float SelectedValue => Value(Selected);

	public void Next() =>
		Selected = (TuningKey)(((int)Selected + 1) % Count);

	public void Nudge(int direction)
	{
		var value = Math.Clamp(
			SelectedValue + Math.Sign(direction) * Step, MinValue, MaxValue);
		_overrides[Selected] = value;
		LastChange = FormattableString.Invariant($"{Name(Selected)} = {value:0.00}f");
		Console.WriteLine($"[tune] {LastChange}");
	}
}
