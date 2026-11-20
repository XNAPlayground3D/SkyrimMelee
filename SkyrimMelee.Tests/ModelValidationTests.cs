using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SkyrimMelee.Validation;

namespace SkyrimMelee.Tests;

public class ModelValidationTests
{
	[Fact]
	public void ValidFighterPasses()
	{
		var report = ModelValidator.Validate(ValidFighter());

		Assert.True(report.IsValid, string.Join("\n", report.Issues));
	}

	[Fact]
	public void ValidSwordPasses()
	{
		var report = ModelValidator.Validate(ValidSword());

		Assert.True(report.IsValid, string.Join("\n", report.Issues));
	}

	[Fact]
	public void SyntheticRootParentIsAccepted()
	{
		var fighter = ValidFighter();
		var skin = fighter.Skins[0] with
		{
			Joints = fighter.Skins[0].Joints
				.Select(joint => joint.Name == "root" ? joint with { ParentName = "_Root" } : joint)
				.ToArray()
		};

		var report = ModelValidator.Validate(fighter with { Skins = [skin] });

		Assert.True(report.IsValid, string.Join("\n", report.Issues));
	}

	[Fact]
	public void ArmatureParentIsAccepted()
	{
		var fighter = ValidFighter();
		var skin = fighter.Skins[0] with
		{
			Joints = fighter.Skins[0].Joints
				.Select(joint => joint.Name == "root" ? joint with { ParentName = "FighterRig" } : joint)
				.ToArray()
		};

		var report = ModelValidator.Validate(fighter with { Skins = [skin] });

		Assert.True(report.IsValid, string.Join("\n", report.Issues));
	}

	[Fact]
	public void MissingJointFails()
	{
		var fighter = ValidFighter();
		var skin = fighter.Skins[0] with
		{
			Joints = fighter.Skins[0].Joints.Where(joint => joint.Name != "head").ToArray()
		};

		AssertFails(fighter with { Skins = [skin] }, "missing joint 'head'");
	}

	[Fact]
	public void WrongParentFails()
	{
		var fighter = ValidFighter();
		var skin = fighter.Skins[0] with
		{
			Joints = fighter.Skins[0].Joints
				.Select(joint => joint.Name == "head" ? joint with { ParentName = "root" } : joint)
				.ToArray()
		};

		AssertFails(fighter with { Skins = [skin] }, "expected 'neck'");
	}

	[Fact]
	public void WrongRestPositionFails()
	{
		var fighter = ValidFighter();
		var skin = fighter.Skins[0] with
		{
			Joints = fighter.Skins[0].Joints
				.Select(joint => joint.Name == "head" ? joint with { RestPosition = new Vector3(0f, 1.7f, 0f) } : joint)
				.ToArray()
		};

		AssertFails(fighter with { Skins = [skin] }, "rest position");
	}

	[Fact]
	public void TooManyInfluencesFail()
	{
		var fighter = ValidFighter() with
		{
			Parts = [ValidFighter().Parts[0] with { InfluenceCount = 5 }]
		};

		AssertFails(fighter, "5 skin influences");
	}

	[Fact]
	public void UnnormalisedWeightsFail()
	{
		var fighter = ValidFighter() with
		{
			Parts = [ValidFighter().Parts[0] with { MinWeightSum = 0.8f, MaxWeightSum = 0.9f }]
		};

		AssertFails(fighter, "weights sum");
	}

	[Fact]
	public void TriangleBudgetFails()
	{
		var fighter = ValidFighter() with
		{
			Parts = [ValidFighter().Parts[0] with { TriangleCount = ModelSpec.MaxFighterTriangles + 1 }]
		};

		AssertFails(fighter, "triangles");
	}

	[Fact]
	public void WrongClipDurationFails()
	{
		var fighter = ValidFighter();
		var clips = fighter.Clips
			.Select(clip => clip.Name == "SlashLeft" ? clip with { Duration = 0.9f } : clip)
			.ToArray();

		AssertFails(fighter with { Clips = clips }, "duration");
	}

	[Fact]
	public void MissingClipFails()
	{
		var fighter = ValidFighter();
		var clips = fighter.Clips.Where(clip => clip.Name != "Idle").ToArray();

		AssertFails(fighter with { Clips = clips }, "missing clip 'Idle'");
	}

	[Fact]
	public void MissingMovementClipFails()
	{
		var fighter = ValidFighter();
		var clips = fighter.Clips.Where(clip => clip.Name != "WalkForward").ToArray();

		AssertFails(fighter with { Clips = clips }, "missing clip 'WalkForward'");
	}

	[Fact]
	public void RootMotionFails()
	{
		var fighter = ValidFighter();
		var clip = fighter.Clips.First(item => item.Name == "SlashLeft");
		var root = new ChannelFact(
			"root",
			[
				new KeyframeFact(0f, new Vector3(0f, 0.1f, 0f), Quaternion.Identity, Vector3.One),
				new KeyframeFact(clip.Duration, new Vector3(0f, 0.1f, 0f), Quaternion.Identity, Vector3.One)
			]);

		var clips = fighter.Clips
			.Select(item => item.Name == "SlashLeft"
				? item with { Channels = [.. item.Channels, root] }
				: item)
			.ToArray();

		AssertFails(fighter with { Clips = clips }, "root motion");
	}

	[Fact]
	public void AnimatedWeaponBoneFails()
	{
		var fighter = ValidFighter();
		var clip = fighter.Clips.First(item => item.Name == "SlashLeft");
		var weapon = new ChannelFact(
			"weapon_r",
			[
				new KeyframeFact(0f, Vector3.Zero, Quaternion.Identity, Vector3.One),
				new KeyframeFact(clip.Duration, Vector3.Zero, Quaternion.Identity, Vector3.One)
			]);

		var clips = fighter.Clips
			.Select(item => item.Name == "SlashLeft"
				? item with { Channels = [.. item.Channels, weapon] }
				: item)
			.ToArray();

		AssertFails(fighter with { Clips = clips }, "weapon_r");
	}

	[Fact]
	public void LoopSeamFails()
	{
		var fighter = ValidFighter();
		var idle = fighter.Clips.First(item => item.Name == "Idle");
		var channel = idle.Channels[0] with
		{
			Keyframes =
			[
				idle.Channels[0].Keyframes[0],
				idle.Channels[0].Keyframes[^1] with { Translation = new Vector3(0f, 0.05f, 0f) }
			]
		};

		var clips = fighter.Clips
			.Select(item => item.Name == "Idle"
				? item with { Channels = [channel] }
				: item)
			.ToArray();

		AssertFails(fighter with { Clips = clips }, "looped clip");
	}

	[Fact]
	public void WrongTextureSizeFails()
	{
		var fighter = ValidFighter() with
		{
			Materials = [new MaterialFact(0, "body", 512, 512)]
		};

		AssertFails(fighter, "512x512");
	}

	[Fact]
	public void SwordCannotBeSkinned()
	{
		var sword = ValidSword() with
		{
			Parts = [ValidSword().Parts[0] with { Skinned = true, SkinIndex = 0 }]
		};

		AssertFails(sword, "must not be skinned");
	}

	[Fact]
	public void SwordTipFails()
	{
		var sword = ValidSword() with
		{
			Bounds = new BoundingBox(new Vector3(-0.15f, -0.23f, -0.03f), new Vector3(0.15f, 1.4f, 0.03f))
		};

		AssertFails(sword, "blade tip");
	}

	private static void AssertFails(ModelFacts facts, string fragment)
	{
		var report = ModelValidator.Validate(facts);

		Assert.False(report.IsValid);
		Assert.Contains(
			report.Issues,
			issue => issue.Severity == ValidationSeverity.Error &&
				issue.Message.Contains(fragment, StringComparison.OrdinalIgnoreCase));
	}

	private static ModelFacts ValidFighter()
	{
		var joints = ModelSpec.Joints
			.Select(joint => new BoneFact(
				joint.Name,
				joint.Parent,
				joint.RestPosition ?? Vector3.Zero))
			.ToArray();

		var skin = new SkinFact(0, "fighter_skin", joints);
		var material = new MaterialFact(0, "fighter", 1024, 1024);
		var part = new PartFact(
			MaterialIndex: 0,
			PrimitiveType: PrimitiveType.TriangleList,
			TriangleCount: 100,
			Skinned: true,
			SkinIndex: 0,
			SkinJointCount: joints.Length,
			InfluenceCount: 4,
			MinWeightSum: 1f,
			MaxWeightSum: 1f,
			MaxJointIndex: joints.Length - 1,
			HasNormals: true);

		var clips = ModelSpec.AnimationClips
			.Select(spec => new ClipFact(
				spec.Name,
				spec.Duration,
				[
					new ChannelFact(
						"pelvis",
						[
							new KeyframeFact(0f, Vector3.Zero, Quaternion.Identity, Vector3.One),
							new KeyframeFact(spec.Duration, Vector3.Zero, Quaternion.Identity, Vector3.One)
						])
				]))
			.ToArray();

		return new ModelFacts(
			"fighter.glb",
			ModelKind.Fighter,
			joints,
			[skin],
			[part],
			clips,
			new BoundingBox(new Vector3(-0.78f, 0f, -0.3f), new Vector3(0.78f, 1.8f, 0.3f)),
			[material]);
	}

	private static ModelFacts ValidSword()
	{
		var part = new PartFact(
			MaterialIndex: 0,
			PrimitiveType: PrimitiveType.TriangleList,
			TriangleCount: 100,
			Skinned: false,
			SkinIndex: -1,
			SkinJointCount: 0,
			InfluenceCount: 0,
			MinWeightSum: float.NaN,
			MaxWeightSum: float.NaN,
			MaxJointIndex: -1,
			HasNormals: true);

		return new ModelFacts(
			"sword.glb",
			ModelKind.Sword,
			[],
			[],
			[part],
			[],
			new BoundingBox(new Vector3(-0.15f, -0.23f, -0.03f), new Vector3(0.15f, 1.47f, 0.03f)),
			[new MaterialFact(0, "sword", 1024, 1024)]);
	}
}
