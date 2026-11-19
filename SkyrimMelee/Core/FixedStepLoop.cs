namespace SkyrimMelee.Core;

/// <summary>
/// Turns variable-rate frame time into a fixed number of 60 Hz simulation steps.
/// Rendering stays at the display rate; gameplay only ever advances in full steps.
/// </summary>
public sealed class FixedStepLoop
{
	public const double StepDuration = 1.0 / 60.0;
	public const int MaxStepsPerFrame = 5;

	// A single frame longer than this is treated as a stall, not as catch-up work.
	private const double MaxFrameTime = 0.25;

	private double _accumulator;

	/// <summary>Steps discarded by the per-frame cap during the last <see cref="Advance"/> call.</summary>
	public int DroppedSteps { get; private set; }

	/// <summary>Unconsumed time carried into the next call, in seconds.</summary>
	public double Accumulator => _accumulator;

	/// <summary>Returns how many simulation steps should run for this frame.</summary>
	public int Advance(double elapsedSeconds)
	{
		DroppedSteps = 0;

		if (double.IsNaN(elapsedSeconds) || elapsedSeconds <= 0)
		{
			return 0;
		}

		if (elapsedSeconds > MaxFrameTime)
		{
			elapsedSeconds = MaxFrameTime;
		}

		_accumulator += elapsedSeconds;

		var requested = (int)(_accumulator / StepDuration);
		if (requested <= MaxStepsPerFrame)
		{
			_accumulator -= requested * StepDuration;
			return requested;
		}

		// Too far behind: drop the backlog rather than spiral into ever-longer frames.
		DroppedSteps = requested - MaxStepsPerFrame;
		_accumulator = 0;
		return MaxStepsPerFrame;
	}

	public void Reset()
	{
		_accumulator = 0;
		DroppedSteps = 0;
	}
}
