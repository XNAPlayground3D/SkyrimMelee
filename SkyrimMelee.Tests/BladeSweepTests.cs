using Microsoft.Xna.Framework;
using SkyrimMelee.Combat;

namespace SkyrimMelee.Tests;

public class BladeSweepTests
{
	private const float FixedStep = 1f / 60f;

	[Theory]
	[InlineData(1)] // 60 FPS: one fixed step per render frame
	[InlineData(6)] // 10 FPS: six fixed steps per render frame
	public void PowerSwingHitsTheTorsoOnceWithoutTunneling(int stepsPerFrame)
	{
		var clip = SwordClips.Create().Single(c => c.Name == "PowerSlash");
		var hand = Matrix.CreateTranslation(SwordClips.HandAnchor) *
				   Matrix.CreateTranslation(-1f, 0f, 0f);

		BladeGeometry BladeAt(float time) =>
			BladeGeometry.FromPose(clip.Sample(time), hand);

		// Put a target torso directly on the blade's mid-swing path.
		var targetCenter = BladeAt(0.68f).Tip;
		var root = Matrix.CreateTranslation(targetCenter - new Vector3(0f, 1.26f, 0f));

		using var world = new CombatWorld();
		world.AddFighter(FighterId.Opponent, root);
		var sweep = new BladeSweep(world);

		var time = 0.50f;
		var previous = BladeAt(time);
		var first = (BladeHit?)null;
		var firstOverall = (BladeHit?)null;
		var firstTime = 0f;
		var torsoSeen = false;
		var maxStepTravel = 0f;

		while (time < 0.95f)
		{
			for (var frameStep = 0; frameStep < stepsPerFrame && time < 0.95f; frameStep++)
			{
				var next = time + FixedStep;
				var current = BladeAt(next);
				maxStepTravel = MathF.Max(maxStepTravel,
					Vector3.Distance(previous.Tip, current.Tip));

				var hits = sweep.Sweep(previous, current, FixedStep);
				if (firstOverall is null && hits.Count > 0)
				{
					firstOverall = hits[0];
				}

				foreach (var hit in hits)
				{
					if (hit.Hurtbox.Kind != HurtboxKind.Torso || torsoSeen)
					{
						continue;
					}

					torsoSeen = true;
					first = hit;
					firstTime = time + hit.TimeOfImpact;
				}

				previous = current;
				time = next;
			}
		}

		Assert.True(first is not null, "The power swing never hit the target torso.");
		Assert.True(firstOverall is not null, "The power swing never hit anything.");
		Assert.Equal(HurtboxKind.Torso, firstOverall!.Value.Hurtbox.Kind);

		// The step is fast enough to need substeps, so the sweep must not tunnel.
		Assert.True(maxStepTravel > BladeSweep.MaxSubstepTravel,
			$"tip travel per step was only {maxStepTravel:0.000} m.");

		var tuning = CombatTuning.PowerSlash;
		var firstHit = first!.Value;
		Assert.True(firstTime >= tuning.Windup && firstTime <= tuning.Windup + tuning.Active,
			$"first contact at {firstTime:0.000} s is outside the active window.");
		Assert.True(Vector3.Distance(firstHit.Point, targetCenter) < 0.6f,
			$"contact point {firstHit.Point} is far from the target torso {targetCenter}.");
		Assert.True(firstHit.Normal.LengthSquared() > 0.5f,
			$"normal {firstHit.Normal} is not a unit vector.");
	}
}
