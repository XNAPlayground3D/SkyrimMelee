using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SkyrimMelee.Validation;

public enum ModelKind
{
	Fighter,
	Sword
}

/// <summary>A bone or skin joint with its world rest position.</summary>
public readonly record struct BoneFact(string Name, string? ParentName, Vector3 RestPosition);

/// <summary>One skin from the loaded model.</summary>
public readonly record struct SkinFact(int Index, string? Name, IReadOnlyList<BoneFact> Joints);

/// <summary>One sampled keyframe, flattened out of the animation channel.</summary>
public readonly record struct KeyframeFact(float Time, Vector3 Translation, Quaternion Rotation, Vector3 Scale);

/// <summary>One animation channel targeting a named bone.</summary>
public readonly record struct ChannelFact(string BoneName, IReadOnlyList<KeyframeFact> Keyframes);

/// <summary>One animation clip and its duration.</summary>
public readonly record struct ClipFact(string Name, float Duration, IReadOnlyList<ChannelFact> Channels);

/// <summary>A material and the size of its base-colour texture.</summary>
public readonly record struct MaterialFact(int Index, string? Name, int TextureWidth, int TextureHeight);

/// <summary>A mesh part as exposed by the loaded <c>DrModel</c>.</summary>
public readonly record struct PartFact(
	int MaterialIndex,
	PrimitiveType PrimitiveType,
	int TriangleCount,
	bool Skinned,
	int SkinIndex,
	int SkinJointCount,
	int InfluenceCount,
	float MinWeightSum,
	float MaxWeightSum,
	int MaxJointIndex,
	bool HasNormals,
	string? SkinningIssue = null);

/// <summary>
/// Everything the validator needs from a loaded <c>DrModel</c>, flattened so
/// the rules can be tested without a graphics device.
/// </summary>
public sealed record ModelFacts(
	string File,
	ModelKind Kind,
	IReadOnlyList<BoneFact> Bones,
	IReadOnlyList<SkinFact> Skins,
	IReadOnlyList<PartFact> Parts,
	IReadOnlyList<ClipFact> Clips,
	BoundingBox? Bounds,
	IReadOnlyList<MaterialFact> Materials);
