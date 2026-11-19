
using SkyrimMelee.Tuning;
using SkyrimMelee.Attacks;
using SkyrimMelee.Physics;

namespace SkyrimMelee.Combat;

public enum DuelPhase
{
	Fighting,
	PostDeath,
	MatchOver
}

/// <summary>
/// Best-of-three bookkeeping: round number, score, post-death hold, and the
/// match winner. The director does not own the fighters; the caller resets
/// them when <see cref="Update"/> reports a new round.
/// </summary>
public sealed class DuelDirector
{
	public const int RoundsToWin = 2;
	public const float PostDeathHold = 3f;

	public int Round { get; private set; } = 1;
	public int PlayerScore { get; private set; }
	public int OpponentScore { get; private set; }
	public DuelPhase Phase { get; private set; } = DuelPhase.Fighting;
	public FighterId? RoundWinner { get; private set; }
	public FighterId? MatchWinner { get; private set; }
	public float PhaseTime { get; private set; }

	/// <summary>Records a death; returns false outside the fighting phase.</summary>
	public bool ReportDeath(FighterId loser)
	{
		if (Phase != DuelPhase.Fighting)
		{
			return false;
		}

		RoundWinner = loser == FighterId.Player ? FighterId.Opponent : FighterId.Player;
		if (RoundWinner == FighterId.Player)
		{
			PlayerScore++;
		}
		else
		{
			OpponentScore++;
		}

		Phase = DuelPhase.PostDeath;
		PhaseTime = 0f;
		return true;
	}

	/// <summary>Records a double KO; no score, the same round is replayed.</summary>
	public bool ReportDraw()
	{
		if (Phase != DuelPhase.Fighting)
		{
			return false;
		}

		RoundWinner = null;
		Phase = DuelPhase.PostDeath;
		PhaseTime = 0f;
		return true;
	}

	/// <summary>
	/// Advances the post-death hold. Returns true when a new round should start
	/// (the caller resets the fighters).
	/// </summary>
	public bool Update(float deltaSeconds)
	{
		if (Phase != DuelPhase.PostDeath || deltaSeconds <= 0f)
		{
			return false;
		}

		PhaseTime += deltaSeconds;
		if (PhaseTime < PostDeathHold)
		{
			return false;
		}

		if (RoundWinner is null)
		{
			// Double KO: replay the same round without changing the score.
			Phase = DuelPhase.Fighting;
			PhaseTime = 0f;
			return true;
		}

		if (PlayerScore >= RoundsToWin || OpponentScore >= RoundsToWin)
		{
			Phase = DuelPhase.MatchOver;
			MatchWinner = PlayerScore >= RoundsToWin
				? FighterId.Player
				: FighterId.Opponent;
			return false;
		}

		Round++;
		RoundWinner = null;
		Phase = DuelPhase.Fighting;
		PhaseTime = 0f;
		return true;
	}

	public void Reset()
	{
		Round = 1;
		PlayerScore = 0;
		OpponentScore = 0;
		Phase = DuelPhase.Fighting;
		RoundWinner = null;
		MatchWinner = null;
		PhaseTime = 0f;
	}
}
