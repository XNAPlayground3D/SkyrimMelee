using SkyrimMelee.Tuning;

namespace SkyrimMelee.Tests;

public class TuningSessionTests
{
	[Fact]
	public void DefaultsMatchCombatTuning()
	{
		var session = new TuningSession();

		Assert.Equal(CombatTuning.HitFlashDuration, session.SelectedValue, 5);
		Assert.Equal(CombatTuning.StaggerMicroDuration,
			session.Value(TuningKey.StaggerMicro), 5);
		Assert.Equal(CombatTuning.StaggerSmallDuration,
			session.Value(TuningKey.StaggerSmall), 5);
		Assert.Equal(CombatTuning.StaggerLargeDuration,
			session.Value(TuningKey.StaggerLarge), 5);
	}

	[Fact]
	public void NudgeStepsByTenMillisecondsAndClamps()
	{
		var session = new TuningSession();
		var start = session.SelectedValue;

		session.Nudge(1);
		Assert.Equal(start + TuningSession.Step, session.SelectedValue, 5);

		for (var i = 0; i < 1000; i++)
		{
			session.Nudge(-1);
		}

		Assert.Equal(TuningSession.MinValue, session.SelectedValue, 5);
	}

	[Fact]
	public void NextCyclesEveryKey()
	{
		var session = new TuningSession();
		var seen = new List<TuningKey> { session.Selected };

		for (var i = 0; i < TuningSession.Count - 1; i++)
		{
			session.Next();
			seen.Add(session.Selected);
		}

		Assert.Equal(TuningSession.Count, seen.Distinct().Count());

		session.Next();
		Assert.Equal(TuningKey.HitFlash, session.Selected);
	}

	[Fact]
	public void LastChangeIsPasteReady()
	{
		var session = new TuningSession();
		session.Nudge(1);

		Assert.StartsWith("HitFlashDuration = ", session.LastChange);
		Assert.EndsWith("f", session.LastChange);
	}
}
