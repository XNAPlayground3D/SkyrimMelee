using BepuPhysics;
using Microsoft.Xna.Framework;
using SkyrimMelee.Tuning;
using SkyrimMelee.Animation;

namespace SkyrimMelee.Physics;

public enum FighterId
{
	Player,
	Opponent
}

public enum HurtboxKind
{
	Head,
	Torso,
	Hips,
	Legs,
	GuardBlade
}

public enum HurtboxShapeKind
{
	Sphere,
	Capsule
}

/// <summary>Identifies one collidable as belonging to a fighter and a body region.</summary>
public readonly record struct Hurtbox(FighterId Fighter, HurtboxKind Kind);

/// <summary>Shape dimensions and fighter-local centre for a hurtbox kind.</summary>
public readonly record struct HurtboxDefinition(
	HurtboxShapeKind Shape,
	float Radius,
	float HalfLength,
	Vector3 Center)
{
	public static HurtboxDefinition For(HurtboxKind kind) => kind switch
	{
		// The visual body uses these exact definitions, so every rendered part is also a hurtbox.
		HurtboxKind.Head => new(HurtboxShapeKind.Sphere, 0.115f, 0f, new Vector3(0f, 1.67f, 0f)),
		HurtboxKind.Torso => new(HurtboxShapeKind.Capsule, 0.16f, 0.21f, new Vector3(0f, 1.26f, 0f)),
		HurtboxKind.Hips => new(HurtboxShapeKind.Capsule, 0.14f, 0.09f, new Vector3(0f, 0.86f, 0f)),
		HurtboxKind.Legs => new(HurtboxShapeKind.Capsule, 0.13f, 0.25f, new Vector3(0f, 0.38f, 0f)),
		HurtboxKind.GuardBlade => new(HurtboxShapeKind.Capsule, 0.03f, 0.645f, Vector3.Zero),
		_ => throw new ArgumentOutOfRangeException(nameof(kind))
	};
}

/// <summary>Maps Bepu static handles back to the fighter and body region that own them.</summary>
public sealed class HurtboxSet
{
	private readonly Dictionary<StaticHandle, Hurtbox> _entries = new();

	public int Count => _entries.Count;

	public IEnumerable<KeyValuePair<StaticHandle, Hurtbox>> Entries => _entries;

	public void Add(StaticHandle handle, Hurtbox hurtbox) => _entries.Add(handle, hurtbox);

	public void Remove(StaticHandle handle) => _entries.Remove(handle);

	public bool TryGet(StaticHandle handle, out Hurtbox hurtbox) =>
		_entries.TryGetValue(handle, out hurtbox);

	public void Clear() => _entries.Clear();
}
