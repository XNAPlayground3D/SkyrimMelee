namespace SkyrimMelee.Core;

/// <summary>
/// Deterministic simulation clock. Real frame time is scaled, accumulated, and converted
/// into whole 60 Hz steps; gameplay only ever observes fixed step time.
/// </summary>
public sealed class GameClock
{
	private readonly FixedStepLoop _loop = new();

	/// <summary>Steps executed since launch.</summary>
	public long FrameIndex { get; private set; }

	/// <summary>Simulated time, always a whole number of steps.</summary>
	public double SimTime { get; private set; }

	/// <summary>Fixed step duration passed to simulation.</summary>
	public double DeltaTime => FixedStepLoop.StepDuration;

	/// <summary>Wall-clock time since launch, unaffected by <see cref="TimeScale"/>.</summary>
	public double RealTime { get; private set; }

	/// <summary>Accumulated scaled time fed into the step loop (including fractions).</summary>
	public double ScaledTime { get; private set; }

	/// <summary>Accumulated scaled time minus executed sim time; negative means dropped steps.</summary>
	public double Drift => SimTime - ScaledTime;

	/// <summary>Multiplier applied to frame time before stepping (0.25 = slow motion, 0 = paused).</summary>
	public double TimeScale { get; set; } = 1.0;

	/// <summary>Steps discarded by the per-frame catch-up cap during the last <see cref="Advance"/> call.</summary>
	public int LastDroppedSteps => _loop.DroppedSteps;

	public long TotalDroppedSteps { get; private set; }

	/// <summary>Unconsumed scaled time carried into the next frame.</summary>
	public double Accumulator => _loop.Accumulator;

	/// <summary>Advances the clock and returns how many fixed steps should be simulated.</summary>
	public int Advance(double realElapsedSeconds)
	{
		if (double.IsNaN(realElapsedSeconds) || realElapsedSeconds <= 0)
		{
			return 0;
		}

		RealTime += realElapsedSeconds;

		var scaled = realElapsedSeconds * TimeScale;
		ScaledTime += scaled;

		var steps = _loop.Advance(scaled);
		if (steps > 0)
		{
			FrameIndex += steps;
			SimTime += steps * FixedStepLoop.StepDuration;
		}

		TotalDroppedSteps += _loop.DroppedSteps;
		return steps;
	}
}
