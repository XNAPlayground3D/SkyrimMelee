using System.Diagnostics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using Microsoft.Xna.Framework;
using NumVector3 = System.Numerics.Vector3;

namespace SkyrimMelee.Combat;

/// <summary>One confirmed collidable contact along a blade sweep.</summary>
public readonly record struct BladeHit(
	Hurtbox Hurtbox,
	Vector3 Point,
	Vector3 Normal,
	float TimeOfImpact);

/// <summary>
/// Sweeps the blade through the static hurtboxes. The blade is approximated by
/// three spheres; each simulation step is split so no sphere travels more than
/// <see cref="MaxSubstepTravel"/> per sweep. Hits are deduplicated per hurtbox
/// and returned in time-of-impact order.
/// </summary>
public sealed class BladeSweep
{
	public const float MaxSubstepTravel = 0.35f;
	public const int MaxSubsteps = 8;

	// Blade fractions from base to tip, with the design's sphere radii.
	private static readonly (float Fraction, float Radius)[] SphereChain =
	{
		(0f, 0.055f),
		(0.5f, 0.040f),
		(1f, 0.025f)
	};

	private readonly Simulation _simulation;
	private readonly BufferPool _pool;
	private readonly HurtboxSet _hurtboxes;
	private readonly Sphere[] _shapes;
	private readonly List<RawHit> _rawHits = new(8);
	private readonly List<BladeHit> _hits = new(8);
	private readonly Dictionary<StaticHandle, int> _hitIndex = new();

	public BladeSweep(CombatWorld world)
	{
		_simulation = world.Simulation;
		_pool = world.Pool;
		_hurtboxes = world.Hurtboxes;
		_shapes = SphereChain.Select(sphere => new Sphere(sphere.Radius)).ToArray();
	}

	/// <summary>
	/// Sweeps from the previous blade segment to the current one. The returned
	/// list is reused by the next call; consume it before sweeping again.
	/// </summary>
	public IReadOnlyList<BladeHit> Sweep(
		BladeGeometry previous, BladeGeometry current, float deltaSeconds)
	{
		_hits.Clear();
		_hitIndex.Clear();

		if (deltaSeconds <= 0f)
		{
			return _hits;
		}

		var tipTravel = Vector3.Distance(previous.Tip, current.Tip);
		var substeps = Math.Clamp(
			(int)MathF.Ceiling(tipTravel / MaxSubstepTravel), 1, MaxSubsteps);

		for (var substep = 0; substep < substeps; substep++)
		{
			var t0 = substep / (float)substeps;
			var t1 = (substep + 1) / (float)substeps;

			var base0 = Vector3.Lerp(previous.Base, current.Base, t0);
			var tip0 = Vector3.Lerp(previous.Tip, current.Tip, t0);
			var base1 = Vector3.Lerp(previous.Base, current.Base, t1);
			var tip1 = Vector3.Lerp(previous.Tip, current.Tip, t1);

			var substepTravel = Vector3.Distance(tip0, tip1);
			Debug.Assert(substepTravel <= MaxSubstepTravel + 1e-3f,
				$"Blade substep travels {substepTravel:0.0000} m; cap is {MaxSubstepTravel} m.");

			for (var i = 0; i < SphereChain.Length; i++)
			{
				var fraction = SphereChain[i].Fraction;
				var center0 = Vector3.Lerp(base0, tip0, fraction);
				var center1 = Vector3.Lerp(base1, tip1, fraction);
				var motion = center1 - center0;
				if (motion.LengthSquared() < 1e-10f)
				{
					continue;
				}

				_rawHits.Clear();
				var collector = new HitCollector
				{
					Hurtboxes = _hurtboxes,
					Hits = _rawHits
				};

				var pose = new RigidPose(ToBepu(center0));
				var velocity = new BodyVelocity(ToBepu(motion));
				_simulation.Sweep(_shapes[i], pose, velocity, 1f, _pool, ref collector);

				foreach (var raw in _rawHits)
				{
					var time = (t0 + (t1 - t0) * raw.Time) * deltaSeconds;
					AddHit(raw, time);
				}
			}
		}

		_hits.Sort(static (a, b) => a.TimeOfImpact.CompareTo(b.TimeOfImpact));
		return _hits;
	}

	private void AddHit(in RawHit raw, float time)
	{
		if (!_hurtboxes.TryGet(raw.Handle, out var hurtbox))
		{
			return;
		}

		if (_hitIndex.TryGetValue(raw.Handle, out var index))
		{
			if (time < _hits[index].TimeOfImpact)
			{
				_hits[index] = new BladeHit(hurtbox, ToXna(raw.Point), ToXna(raw.Normal), time);
			}

			return;
		}

		_hitIndex.Add(raw.Handle, _hits.Count);
		_hits.Add(new BladeHit(hurtbox, ToXna(raw.Point), ToXna(raw.Normal), time));
	}

	private static NumVector3 ToBepu(Vector3 value) => new(value.X, value.Y, value.Z);

	private static Vector3 ToXna(NumVector3 value) => new(value.X, value.Y, value.Z);

	private readonly record struct RawHit(
		StaticHandle Handle, NumVector3 Point, NumVector3 Normal, float Time);

	private struct HitCollector : ISweepHitHandler
	{
		public HurtboxSet Hurtboxes;
		public List<RawHit> Hits;

		public bool AllowTest(CollidableReference collidable) =>
			collidable.Mobility == CollidableMobility.Static;

		public bool AllowTest(CollidableReference collidable, int childIndex) => true;

		public void OnHit(ref float maximumT, float t, in NumVector3 point,
			in NumVector3 normal, CollidableReference collidable)
		{
			// Leave maximumT untouched so the sweep reports every hurtbox on the path.
			if (collidable.Mobility != CollidableMobility.Static ||
				!Hurtboxes.TryGet(collidable.StaticHandle, out _))
			{
				return;
			}

			Hits.Add(new RawHit(collidable.StaticHandle, point, normal, t));
		}

		public void OnHitAtZeroT(ref float maximumT, CollidableReference collidable)
		{
			if (collidable.Mobility != CollidableMobility.Static ||
				!Hurtboxes.TryGet(collidable.StaticHandle, out _))
			{
				return;
			}

			Hits.Add(new RawHit(collidable.StaticHandle, default, default, 0f));
		}
	}
}
