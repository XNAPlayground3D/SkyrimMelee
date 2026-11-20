namespace SkyrimMelee.Ai;

/// <summary>One opponent choice, with the reason it was made, for the F1 overlay.</summary>
public readonly record struct AiDecision(string Action, string Reason);
