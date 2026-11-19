using Microsoft.Xna.Framework;
using SkyrimMelee.Combat;

namespace SkyrimMelee.Tests;

public class AttackDefinitionTests
{
	private static IReadOnlyList<SwordClip> Clips => SwordClips.Create();
	private static IReadOnlyList<AttackDefinition> Attacks => AttackCatalog.Create(Clips);
	private static AttackDefinition Attack(string name) => Attacks.Single(a => a.Name == name);
	private static AttackDefinition SlashLeftAttack => Attack("SlashLeft");
	private static AttackDefinition SlashRightAttack => Attack("SlashRight");

	[Fact]
	public void CatalogContainsTheAttacks()
	{
		var names = Attacks.Select(a => a.Name).ToArray();

		Assert.Equal(4, names.Length);
		Assert.Contains("SlashLeft", names);
		Assert.Contains("SlashRight", names);
		Assert.Contains("PowerSlash", names);
		Assert.Contains("Bash", names);
	}

	[Fact]
	public void PowerSlashRequiresStamina()
	{
		var power = Attack("PowerSlash");

		Assert.True(power.RequiresStamina);
		Assert.True(power.AlwaysStagger);
		Assert.Equal(30f, power.StaminaCost, 4);
		Assert.Equal(1.8f, power.DamageMultiplier, 4);
		Assert.Equal(45f, power.StaggerPower, 4);
	}

	[Fact]
	public void BashRequiresStaminaButDoesNotAlwaysStagger()
	{
		var bash = Attack("Bash");

		Assert.True(bash.RequiresStamina);
		Assert.False(bash.AlwaysStagger);
		Assert.Equal(15f, bash.StaminaCost, 4);
		Assert.Equal(0.4f, bash.DamageMultiplier, 4);
		Assert.Equal(40f, bash.StaggerPower, 4);
	}

	[Fact]
	public void BashStartsAndEndsAtTheGuardPose()
	{
		var guard = Clips.Single(clip => clip.Name == "GuardHold").Keys[0].Pose;
		var bash = Attack("Bash").Clip;

		Assert.True(SamePose(guard, bash.Keys[0].Pose));
		Assert.True(SamePose(guard, bash.Keys[^1].Pose));
	}

	[Fact]
	public void PowerSlashHiltTravelsABodyScaleArc()
	{
		var travel = MaxHiltTravel(Attack("PowerSlash").Clip);

		Assert.True(travel > 0.5f, $"PowerSlash grip travels only {travel:0.000} m.");
	}

	[Fact]
	public void ClipDurationsMatchTuningTotals()
	{
		foreach (var attack in Attacks)
		{
			Assert.Equal(attack.Total, attack.Clip.Duration, 4);
		}
	}

	[Fact]
	public void PhaseBoundariesFollowTheTuning()
	{
		var slash = Attacks.First(a => a.Name == "SlashLeft");

		Assert.Equal(0.25f, slash.WindupEnd, 5);
		Assert.Equal(0.47f, slash.ActiveEnd, 5);
		Assert.Equal(AttackPhase.Windup, slash.GetPhase(0.1f));
		Assert.Equal(AttackPhase.Active, slash.GetPhase(slash.WindupEnd));
		Assert.Equal(AttackPhase.Active, slash.GetPhase(slash.ActiveEnd - 0.001f));
		Assert.Equal(AttackPhase.Recovery, slash.GetPhase(slash.ActiveEnd));
		Assert.False(slash.IsActive(0.2f));
		Assert.True(slash.IsActive(slash.WindupEnd));
		Assert.False(slash.IsActive(slash.ActiveEnd));
	}

	[Fact]
	public void EveryAttackHoldsItsTelegraphForAtLeastOneHundredMilliseconds()
	{
		foreach (var attack in Attacks)
		{
			var keys = attack.Clip.Keys;
			var held = false;

			for (var i = 0; i < keys.Count - 1; i++)
			{
				if (keys[i + 1].Time * attack.Clip.Duration > attack.WindupEnd + 0.001f)
				{
					break;
				}

				if (!SamePose(keys[i].Pose, keys[i + 1].Pose))
				{
					continue;
				}

				var hold = (keys[i + 1].Time - keys[i].Time) * attack.Clip.Duration;
				if (hold >= 0.1f - 0.001f)
				{
					held = true;
					break;
				}
			}

			Assert.True(held, $"{attack.Name} lacks a 100 ms telegraph hold.");
		}
	}

	[Fact]
	public void EveryAttackMovesTheHilt()
	{
		foreach (var attack in Attacks)
		{
			var maxOffset = attack.Clip.Keys.Max(k => k.Pose.GripOffset.Length());
			Assert.True(maxOffset > 0.05f, $"{attack.Name} hilt never translates.");
		}
	}

	[Fact]
	public void IdleSwordStandsNearlyVertical()
	{
		var idle = SlashLeftAttack.Clip.Keys[0].Pose;
		var blade = Vector3.TransformNormal(Vector3.UnitY, Matrix.CreateFromQuaternion(idle.Rotation));

		Assert.True(blade.Y > 0.98f, $"idle blade={blade}");
	}

	[Fact]
	public void SlashLeftWindsUpBehindThenSweepsLeft()
	{
		var keys = SlashLeftAttack.Clip.Keys;
		var windup = keys[2].Pose;
		var tip = BladeGeometry.FromPose(windup, Matrix.Identity).Tip;
		var follow = keys[^2].Pose.GripOffset;

		Assert.True(tip.X < -0.5f, $"windup tip x={tip.X:0.000}");
		Assert.True(follow.Z < -0.6f, $"follow z={follow.Z:0.000}");
		Assert.True(windup.GripOffset.Z - follow.Z > 0.8f,
			$"lateral sweep={windup.GripOffset.Z - follow.Z:0.000} m");
	}

	[Fact]
	public void SlashLeftDrivesTowardTheOpponent()
	{
		var keys = SlashLeftAttack.Clip.Keys;
		var windup = keys[2].Pose.GripOffset;
		var contact = keys[5].Pose.GripOffset;

		// The hilt must move forward through the fight, not only sideways.
		Assert.True(contact.X - windup.X > 0.4f,
			$"forward drive={contact.X - windup.X:0.000} m");
	}

	[Fact]
	public void SlashLeftHiltTravelsABodyScaleArc()
	{
		var keys = SlashLeftAttack.Clip.Keys;
		var maxTravel = 0f;

		for (var i = 0; i < keys.Count; i++)
		{
			for (var j = i + 1; j < keys.Count; j++)
			{
				maxTravel = MathF.Max(maxTravel,
					Vector3.Distance(keys[i].Pose.GripOffset, keys[j].Pose.GripOffset));
			}
		}

		// A wrist-only swing travels a few centimetres; a body-driven one does not.
		Assert.True(maxTravel > 0.5f, $"SlashLeft grip travels only {maxTravel:0.000} m.");
	}

	[Fact]
	public void BladeDirectionRoundTripsThroughThePoseBasis()
	{
		var hold = SlashLeftAttack.Clip.Keys[3].Pose;
		var blade = Vector3.TransformNormal(Vector3.UnitY, Matrix.CreateFromQuaternion(hold.Rotation));

		var expected = Vector3.Normalize(new Vector3(-0.25f, 0.95f, 0.18f));
		Assert.True(Vector3.Distance(blade, expected) < 0.01f, $"blade={blade}");
	}

	[Fact]
	public void SlashLeftSweepsTheBladeThroughAWideArc()
	{
		var directions = SlashLeftAttack.Clip.Keys
			.Select(k => Vector3.TransformNormal(Vector3.UnitY, Matrix.CreateFromQuaternion(k.Pose.Rotation)))
			.ToArray();

		var maxAngle = 0f;
		for (var i = 0; i < directions.Length; i++)
		{
			for (var j = i + 1; j < directions.Length; j++)
			{
				var dot = Math.Clamp(Vector3.Dot(directions[i], directions[j]), -1f, 1f);
				maxAngle = MathF.Max(maxAngle, MathF.Acos(dot));
			}
		}

		Assert.True(maxAngle > 2f, $"SlashLeft blade sweeps only {MathHelper.ToDegrees(maxAngle):0} degrees.");
	}

	[Fact]
	public void PowerSlashRaisesThenChops()
	{
		var clip = Attack("PowerSlash").Clip;
		var hand = Matrix.CreateTranslation(SwordClips.HandAnchor);
		var windup = BladeGeometry.FromPose(clip.Keys[2].Pose, hand).Tip;
		var contact = BladeGeometry.FromPose(clip.Keys[5].Pose, hand).Tip;

		Assert.True(windup.Y > 2.5f, $"windup tip y={windup.Y:0.00}");
		Assert.True(contact.Y < windup.Y - 1f, $"contact tip y={contact.Y:0.00}");
		Assert.True(contact.X > windup.X + 1.5f, $"contact tip x={contact.X:0.00}");
	}

	[Fact]
	public void SlashRightIsTheMirrorOfSlashLeft()
	{
		var left = SlashLeftAttack.Clip;
		var right = SlashRightAttack.Clip;

		Assert.Equal(left.Duration, right.Duration, 5);
		Assert.Equal(left.Keys.Count, right.Keys.Count);

		var mirror = Matrix.CreateScale(1f, 1f, -1f);

		for (var i = 0; i < left.Keys.Count; i++)
		{
			var leftPose = left.Keys[i].Pose;
			var rightPose = right.Keys[i].Pose;

			if (i == 0 || i == left.Keys.Count - 1)
			{
				Assert.True(SamePose(leftPose, rightPose),
					"SlashRight must start and end in the same right-side idle");
				continue;
			}

			var leftGrip = leftPose.GripOffset + SwordClips.HandAnchor;
			var rightGrip = rightPose.GripOffset + SwordClips.HandAnchor;

			Assert.Equal(leftGrip.X, rightGrip.X, 4);
			Assert.Equal(leftGrip.Y, rightGrip.Y, 4);
			Assert.Equal(-leftGrip.Z, rightGrip.Z, 4);

			var expected = mirror * Matrix.CreateFromQuaternion(leftPose.Rotation) * mirror;
			var actual = Matrix.CreateFromQuaternion(rightPose.Rotation);

			AssertVec(Vector3.TransformNormal(Vector3.UnitX, expected),
				Vector3.TransformNormal(Vector3.UnitX, actual));
			AssertVec(Vector3.TransformNormal(Vector3.UnitY, expected),
				Vector3.TransformNormal(Vector3.UnitY, actual));
			AssertVec(Vector3.TransformNormal(Vector3.UnitZ, expected),
				Vector3.TransformNormal(Vector3.UnitZ, actual));
		}
	}

	private static float MaxHiltTravel(SwordClip clip)
	{
		var keys = clip.Keys;
		var max = 0f;

		for (var i = 0; i < keys.Count; i++)
		{
			for (var j = i + 1; j < keys.Count; j++)
			{
				max = MathF.Max(max,
					Vector3.Distance(keys[i].Pose.GripOffset, keys[j].Pose.GripOffset));
			}
		}

		return max;
	}

	private static void AssertVec(Vector3 expected, Vector3 actual)
	{
		Assert.Equal(expected.X, actual.X, 4);
		Assert.Equal(expected.Y, actual.Y, 4);
		Assert.Equal(expected.Z, actual.Z, 4);
	}

	private static bool SamePose(SwordPose a, SwordPose b) =>
		Vector3.Distance(a.GripOffset, b.GripOffset) < 1e-5f &&
		MathF.Abs(Quaternion.Dot(a.Rotation, b.Rotation)) > 0.99999f;
}
