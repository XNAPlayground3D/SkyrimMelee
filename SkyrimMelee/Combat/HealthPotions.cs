using Microsoft.Xna.Framework;
using SkyrimMelee.Tuning;

namespace SkyrimMelee.Combat;

/// <summary>
/// Seeded arena pickups. Spawns are driven by the fixed simulation step, so the
/// same seed and movement produce the same potion sequence every run.
/// </summary>
public sealed class HealthPotions
{
	private readonly Random _random;
	private readonly List<Vector3> _positions = new();
	private float _spawnTimer;

	public HealthPotions(int seed)
	{
		_random = new Random(seed);
		Reset();
	}

	public IReadOnlyList<Vector3> Positions => _positions;

	public void Reset()
	{
		_positions.Clear();
		_spawnTimer = CombatTuning.PotionInitialDelay;
	}

	public void Update(float deltaSeconds, Vector3 avoidA, Vector3 avoidB)
	{
		if (deltaSeconds <= 0f)
		{
			return;
		}

		if (_positions.Count >= CombatTuning.PotionMaxActive)
		{
			_spawnTimer = CombatTuning.PotionSpawnInterval;
			return;
		}

		_spawnTimer -= deltaSeconds;
		if (_spawnTimer > 0f)
		{
			return;
		}

		SpawnAwayFrom(avoidA, avoidB);
		_spawnTimer = CombatTuning.PotionSpawnInterval;
	}

	public void RemoveAt(int index) => _positions.RemoveAt(index);

	/// <summary>
	/// Finds the first potion within pickup range of either fighter and returns
	/// the nearest taker; ties go to the player.
	/// </summary>
	public static bool TryTake(
		IReadOnlyList<Vector3> positions, Vector3 player, Vector3 opponent,
		out int index, out bool playerTakes)
	{
		var radius = CombatTuning.PotionPickupRadius;
		var radiusSquared = radius * radius;

		for (var i = 0; i < positions.Count; i++)
		{
			var position = positions[i];
			var playerDistance = Vector3.DistanceSquared(player, position);
			var opponentDistance = Vector3.DistanceSquared(opponent, position);
			if (playerDistance > radiusSquared && opponentDistance > radiusSquared)
			{
				continue;
			}

			index = i;
			playerTakes = playerDistance <= opponentDistance;
			return true;
		}

		index = -1;
		playerTakes = false;
		return false;
	}

	private void SpawnAwayFrom(Vector3 avoidA, Vector3 avoidB)
	{
		const int attempts = 6;
		var min = CombatTuning.PotionSpawnMinRadius;
		var max = CombatTuning.PotionSpawnMaxRadius;

		for (var attempt = 0; attempt < attempts; attempt++)
		{
			var angle = MathHelper.TwoPi * (float)_random.NextDouble();
			var radius = MathF.Sqrt(
				min * min + (max * max - min * min) * (float)_random.NextDouble());
			var position = new Vector3(
				MathF.Cos(angle) * radius, 0f, MathF.Sin(angle) * radius);

			if (Vector3.DistanceSquared(position, avoidA) > 1f &&
				Vector3.DistanceSquared(position, avoidB) > 1f)
			{
				_positions.Add(position);
				return;
			}
		}

		// Failed the avoidance rolls: keep the last position rather than skip a spawn.
		var fallbackAngle = MathHelper.TwoPi * (float)_random.NextDouble();
		_positions.Add(new Vector3(
			MathF.Cos(fallbackAngle) * max, 0f,
			MathF.Sin(fallbackAngle) * max));
	}
}
