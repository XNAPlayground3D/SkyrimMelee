using SkyrimMelee.Core;

namespace SkyrimMelee.Tests;

public class GameClockTests
{
	private const double Step = FixedStepLoop.StepDuration;

	[Fact]
	public void OneSecondOfFramesRunsSixtySteps()
	{
		var clock = new GameClock();

		var total = 0;
		for (var i = 0; i < 60; i++)
		{
			total += clock.Advance(Step);
		}

		Assert.Equal(60, total);
		Assert.Equal(60L, clock.FrameIndex);
		Assert.Equal(1.0, clock.SimTime, 9);
		Assert.Equal(1.0, clock.RealTime, 9);
		Assert.Equal(1.0, clock.ScaledTime, 9);
		Assert.Equal(0.0, clock.Drift, 9);
		Assert.Equal(Step, clock.DeltaTime, 9);
	}

	[Fact]
	public void SlowMotionScalesStepsButNotRealTime()
	{
		var clock = new GameClock { TimeScale = 0.25 };

		var total = 0;
		for (var i = 0; i < 60; i++)
		{
			total += clock.Advance(Step);
		}

		Assert.Equal(15, total);
		Assert.Equal(15L, clock.FrameIndex);
		Assert.Equal(0.25, clock.SimTime, 9);
		Assert.Equal(0.25, clock.ScaledTime, 9);
		Assert.Equal(1.0, clock.RealTime, 9);
	}

	[Fact]
	public void TimeScaleCanChangeMidRun()
	{
		var clock = new GameClock();

		for (var i = 0; i < 30; i++)
		{
			clock.Advance(Step);
		}

		clock.TimeScale = 0.25;

		for (var i = 0; i < 60; i++)
		{
			clock.Advance(Step);
		}

		Assert.Equal(45L, clock.FrameIndex);
		Assert.Equal(0.75, clock.SimTime, 9);
		Assert.Equal(0.75, clock.ScaledTime, 9);
		Assert.Equal(1.5, clock.RealTime, 9);
	}

	[Fact]
	public void StalledFrameIsCappedAndShowsNegativeDrift()
	{
		var clock = new GameClock();

		var steps = clock.Advance(1.0);

		Assert.Equal(FixedStepLoop.MaxStepsPerFrame, steps);
		Assert.Equal(5L, clock.FrameIndex);
		Assert.Equal(10L, clock.TotalDroppedSteps);
		Assert.Equal(10, clock.LastDroppedSteps);
		Assert.Equal(5 * Step, clock.SimTime, 9);
		Assert.Equal(1.0, clock.ScaledTime, 9);
		Assert.Equal(-(1.0 - 5 * Step), clock.Drift, 9);
	}

	[Fact]
	public void SubStepFramesCarryTheAccumulator()
	{
		var clock = new GameClock();

		Assert.Equal(0, clock.Advance(0.005));
		Assert.Equal(0, clock.Advance(0.005));
		Assert.Equal(0.01, clock.Accumulator, 9);

		Assert.Equal(1, clock.Advance(0.010));
		Assert.Equal(0.020 - Step, clock.Accumulator, 9);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(double.NaN)]
	public void NonPositiveOrNaNFrameTimeDoesNotAdvance(double elapsed)
	{
		var clock = new GameClock();

		Assert.Equal(0, clock.Advance(elapsed));
		Assert.Equal(0L, clock.FrameIndex);
		Assert.Equal(0.0, clock.SimTime, 9);
		Assert.Equal(0.0, clock.RealTime, 9);
		Assert.Equal(0.0, clock.ScaledTime, 9);
	}

	[Fact]
	public void ZeroTimeScalePausesSimulationButAdvancesRealTime()
	{
		var clock = new GameClock { TimeScale = 0 };

		for (var i = 0; i < 10; i++)
		{
			Assert.Equal(0, clock.Advance(Step));
		}

		Assert.Equal(0L, clock.FrameIndex);
		Assert.Equal(10 * Step, clock.RealTime, 9);
		Assert.Equal(0.0, clock.ScaledTime, 9);
	}
}
