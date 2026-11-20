namespace SkyrimMelee.Ai;

/// <summary>
/// Per-round opponent performance counters for the debug overlay. Pure data so
/// the rates are testable without a running fight.
/// </summary>
public sealed class AiFightStats
{
	public int AttacksThrown { get; private set; }

	public int HitsLanded { get; private set; }

	public int Blocks { get; private set; }

	public int GuardBreaks { get; private set; }

	/// <summary>Clean hits that got through the opponent's guard.</summary>
	public int IncomingHits { get; private set; }

	/// <summary>Every attack that reached the opponent: blocked, broken, or clean.</summary>
	public int Incoming => Blocks + GuardBreaks + IncomingHits;

	public float HitRate => AttacksThrown == 0
		? 0f
		: HitsLanded / (float)AttacksThrown;

	public float BlockRate => Incoming == 0
		? 0f
		: Blocks / (float)Incoming;

	public void RecordAttack() => AttacksThrown++;

	public void RecordHit() => HitsLanded++;

	public void RecordBlock() => Blocks++;

	public void RecordGuardBreak() => GuardBreaks++;

	public void RecordIncomingHit() => IncomingHits++;

	public void Reset()
	{
		AttacksThrown = 0;
		HitsLanded = 0;
		Blocks = 0;
		GuardBreaks = 0;
		IncomingHits = 0;
	}
}
