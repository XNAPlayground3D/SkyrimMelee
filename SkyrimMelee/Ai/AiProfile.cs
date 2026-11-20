namespace SkyrimMelee.Ai;

/// <summary>Tunable personality for a duelist. Values are per-fight, never global.</summary>
public sealed record AiProfile(
	string Name,
	float ReactionSeconds,
	float BlockChance,
	float TimedBlockChance,
	float TurtleChance,
	float Aggression,
	float PowerChance,
	float MistakeChance)
{
	public static readonly AiProfile Novice = new(
		"Novice",
		ReactionSeconds: 0.22f,
		BlockChance: 0.40f,
		TimedBlockChance: 0.10f,
		TurtleChance: 0.10f,
		Aggression: 0.40f,
		PowerChance: 0.10f,
		MistakeChance: 0.35f);

	public static readonly AiProfile Adept = new(
		"Adept",
		ReactionSeconds: 0.10f,
		BlockChance: 0.75f,
		TimedBlockChance: 0.25f,
		TurtleChance: 0.45f,
		Aggression: 0.55f,
		PowerChance: 0.25f,
		MistakeChance: 0.10f);

	public static readonly AiProfile Master = new(
		"Master",
		ReactionSeconds: 0.06f,
		BlockChance: 0.92f,
		TimedBlockChance: 0.50f,
		TurtleChance: 0.70f,
		Aggression: 0.30f,
		PowerChance: 0.45f,
		MistakeChance: 0.02f);

	public static IReadOnlyList<AiProfile> All { get; } =
		new[] { Novice, Adept, Master };

	/// <summary>The profile used when neither the menu nor --profile overrides it.</summary>
	public static AiProfile Default => Novice;

	public static AiProfile? ByName(string? name) =>
		name is null
			? null
			: All.FirstOrDefault(profile =>
				string.Equals(profile.Name, name, StringComparison.OrdinalIgnoreCase));
}
