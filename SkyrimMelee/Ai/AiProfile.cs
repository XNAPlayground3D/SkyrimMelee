namespace SkyrimMelee.Ai;

/// <summary>Tunable personality for a duelist. Values are per-fight, never global.</summary>
public sealed record AiProfile(
	string Name,
	float ReactionSeconds,
	float BlockChance,
	float TurtleChance,
	float Aggression,
	float PowerChance,
	float MistakeChance)
{
	/// <summary>Default sparring partner until T6.2 adds selectable presets.</summary>
	public static readonly AiProfile Adept = new(
		"Adept",
		ReactionSeconds: 0.10f,
		BlockChance: 0.75f,
		TurtleChance: 0.45f,
		Aggression: 0.55f,
		PowerChance: 0.25f,
		MistakeChance: 0.10f);
}
