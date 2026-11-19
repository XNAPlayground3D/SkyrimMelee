using SkyrimMelee.Core;

namespace SkyrimMelee.Tests;

public class FixedStepLoopTests
{
	private const double Step = FixedStepLoop.StepDuration;

	[Fact]
	public void OneFrameAtSixtyHertzRunsExactlyOneStep()
	{
		var loop = new FixedStepLoop();

		Assert.Equal(1, loop.Advance(Step));
		Assert.Equal(0d, loop.Accumulator, 9);
	}

	[Fact]
	public void SubStepFramesAccumulateAndPreserveRemainder()
	{
		var loop = new FixedStepLoop();

		Assert.Equal(0, loop.Advance(0.005));
		Assert.Equal(0, loop.Advance(0.005));
		Assert.Equal(1, loop.Advance(0.010));
		Assert.Equal(0.020 - Step, loop.Accumulator, 9);
	}

	[Fact]
	public void SixtyFramesOfOneStepProduceSixtySteps()
	{
		var loop = new FixedStepLoop();

		var total = 0;
		for (var i = 0; i < 60; i++)
		{
			total += loop.Advance(Step);
		}

		Assert.Equal(60, total);
		Assert.Equal(0d, loop.Accumulator, 9);
	}

	[Fact]
	public void FractionalFrameDriftStillProducesSixtySteps()
	{
		var loop = new FixedStepLoop();

		var total = 0;
		for (var i = 0; i < 59; i++)
		{
			total += loop.Advance(Step);
		}

		total += loop.Advance(Step + 0.001);

		Assert.Equal(60, total);
	}

	[Theory]
	[InlineData(0.25, 5, 10)]
	[InlineData(1.0, 5, 10)]
	[InlineData(10.0, 5, 10)]
	public void StallsAreCappedAndReportDroppedSteps(double elapsed, int expectedSteps, int expectedDropped)
	{
		var loop = new FixedStepLoop();

		var steps = loop.Advance(elapsed);

		Assert.Equal(expectedSteps, steps);
		Assert.Equal(expectedDropped, loop.DroppedSteps);
		Assert.Equal(0d, loop.Accumulator, 9);
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(double.NaN)]
	public void NonPositiveOrNaNFrameTimeAdvancesNothing(double elapsed)
	{
		var loop = new FixedStepLoop();

		var steps = loop.Advance(elapsed);

		Assert.Equal(0, steps);
		Assert.Equal(0d, loop.Accumulator, 9);
		Assert.Equal(0, loop.DroppedSteps);
	}

	[Fact]
	public void ResetClearsAccumulatorAndDrops()
	{
		var loop = new FixedStepLoop();
		loop.Advance(0.2);
		loop.Advance(1.0);

		loop.Reset();

		Assert.Equal(0d, loop.Accumulator, 9);
		Assert.Equal(0, loop.DroppedSteps);
		Assert.Equal(1, loop.Advance(Step));
	}
}
