namespace SkyrimMelee.Combat;

public static class AttackCatalog
{
	public static IReadOnlyList<AttackDefinition> Create(IReadOnlyList<SwordClip> clips)
	{
		SwordClip Find(string name) =>
			clips.FirstOrDefault(c => c.Name == name)
			?? throw new InvalidOperationException($"Missing sword clip '{name}'.");

		return new[]
		{
			new AttackDefinition("SlashLeft", Find("SlashLeft"), CombatTuning.Slash),
			new AttackDefinition("SlashRight", Find("SlashRight"), CombatTuning.Slash),
			new AttackDefinition("Overhead", Find("Overhead"), CombatTuning.Overhead),
			new AttackDefinition("PowerSlash", Find("PowerSlash"), CombatTuning.PowerSlash)
		};
	}
}
