using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuUtilities;
using BepuUtilities.Memory;
using Microsoft.Xna.Framework;
using SkyrimMelee.Core;
using NumVector3 = System.Numerics.Vector3;
using NumQuaternion = System.Numerics.Quaternion;
using XnaMatrix = Microsoft.Xna.Framework.Matrix;

namespace SkyrimMelee.Combat;

/// <summary>
/// Query-only Bepu world: a floor plus static hurtboxes for both fighters.
/// The simulation is never stepped; shapes are moved by rewriting static poses.
/// </summary>
public sealed class CombatWorld : IDisposable
{
	private readonly BufferPool _pool = new();
	private readonly Simulation _simulation;
	private readonly Dictionary<FighterId, FighterEntry> _fighters = new();
	private readonly TypedIndex _guardShape;
	private StaticHandle? _guardHandle;
	private bool _disposed;

	public CombatWorld()
	{
		_simulation = Simulation.Create(
			_pool,
			new NarrowPhaseCallbacks(),
			new PoseIntegratorCallbacks(),
			new SolveDescription(1, 1, 8));

		var floorShape = _simulation.Shapes.Add(new Box(6f, 0.1f, 6f));
		var floor = new StaticDescription(
			new NumVector3(0f, -0.1f, 0f),
			NumQuaternion.Identity,
			floorShape);
		_simulation.Statics.Add(in floor);

		// The guard blade's length is fixed; only its pose changes.
		var blade = HurtboxDefinition.For(HurtboxKind.GuardBlade);
		_guardShape = _simulation.Shapes.Add(new Capsule(blade.Radius, blade.HalfLength));
	}

	public Simulation Simulation => _simulation;

	public BufferPool Pool => _pool;

	public HurtboxSet Hurtboxes { get; } = new();

	public bool IsGuardBladeRegistered => _guardHandle is not null;

	/// <summary>The live guard collidable, or null when the guard is down.</summary>
	public StaticHandle? GuardBladeHandle => _guardHandle;

	public void AddFighter(FighterId id, XnaMatrix transform)
	{
		if (_fighters.ContainsKey(id))
		{
			throw new InvalidOperationException($"Fighter {id} is already registered.");
		}

		var entry = new FighterEntry { LastTransform = transform };
		foreach (var kind in new[]
		{
			HurtboxKind.Head, HurtboxKind.Torso, HurtboxKind.Hips, HurtboxKind.Legs
		})
		{
			var definition = HurtboxDefinition.For(kind);
			var shape = definition.Shape == HurtboxShapeKind.Sphere
				? _simulation.Shapes.Add(new Sphere(definition.Radius))
				: _simulation.Shapes.Add(new Capsule(definition.Radius, definition.HalfLength));

			var description = Description(definition, shape, transform);
			var handle = _simulation.Statics.Add(in description);

			entry.Handles[kind] = (shape, handle);
			Hurtboxes.Add(handle, new Hurtbox(id, kind));
		}

		_fighters[id] = entry;
	}

	/// <summary>Re-applies every hurtbox pose when the fighter's rigid body transform changes.</summary>
	public void UpdateFighter(FighterId id, XnaMatrix transform)
	{
		if (!_fighters.TryGetValue(id, out var entry) || entry.LastTransform.Equals(transform))
		{
			return;
		}

		entry.LastTransform = transform;
		foreach (var (kind, (shape, handle)) in entry.Handles)
		{
			var description = Description(HurtboxDefinition.For(kind), shape, transform);
			_simulation.Statics.ApplyDescription(handle, in description);
		}
	}

	public void RegisterGuardBlade(BladeGeometry blade)
	{
		if (_guardHandle is not null)
		{
			return;
		}

		var axis = blade.Axis;
		if (axis.LengthSquared() < 1e-8f)
		{
			return;
		}

		var description = new StaticDescription(
			ToBepu((blade.Base + blade.Tip) * 0.5f),
			ToBepu(GeometryMath.RotationFromY(Vector3.Normalize(axis))),
			_guardShape);

		_guardHandle = _simulation.Statics.Add(in description);
		Hurtboxes.Add(_guardHandle.Value, new Hurtbox(FighterId.Player, HurtboxKind.GuardBlade));
	}

	public void UnregisterGuardBlade()
	{
		if (_guardHandle is null)
		{
			return;
		}

		Hurtboxes.Remove(_guardHandle.Value);
		_simulation.Statics.Remove(_guardHandle.Value);
		_guardHandle = null;
	}

	public bool TryGetPose(StaticHandle handle, out Vector3 center, out Quaternion orientation)
	{
		var reference = _simulation.Statics[handle];
		if (!reference.Exists)
		{
			center = Vector3.Zero;
			orientation = Quaternion.Identity;
			return false;
		}

		var pose = reference.Pose;
		center = new Vector3(pose.Position.X, pose.Position.Y, pose.Position.Z);
		orientation = new Quaternion(
			pose.Orientation.X, pose.Orientation.Y, pose.Orientation.Z, pose.Orientation.W);
		return true;
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		_simulation.Dispose();
		_pool.Clear();
	}

	private static StaticDescription Description(
		HurtboxDefinition definition, TypedIndex shape, XnaMatrix transform) =>
		new(
			ToBepu(Vector3.Transform(definition.Center, transform)),
			ToBepu(Quaternion.CreateFromRotationMatrix(transform)),
			shape);

	private static NumVector3 ToBepu(Vector3 value) => new(value.X, value.Y, value.Z);

	private static NumQuaternion ToBepu(Quaternion value) =>
		new(value.X, value.Y, value.Z, value.W);

	private sealed class FighterEntry
	{
		public XnaMatrix LastTransform;
		public Dictionary<HurtboxKind, (TypedIndex Shape, StaticHandle Handle)> Handles { get; } = new();
	}

	private struct NarrowPhaseCallbacks : INarrowPhaseCallbacks
	{
		public void Initialize(Simulation simulation)
		{
		}

		public bool AllowContactGeneration(
			int workerIndex, CollidableReference a, CollidableReference b,
			ref float speculativeMargin) => true;

		public bool AllowContactGeneration(
			int workerIndex, CollidablePair pair, int childIndexA, int childIndexB) => true;

		public bool ConfigureContactManifold<TManifold>(
			int workerIndex, CollidablePair pair, ref TManifold manifold,
			out PairMaterialProperties pairMaterial)
			where TManifold : unmanaged, IContactManifold<TManifold>
		{
			pairMaterial = default;
			return true;
		}

		public bool ConfigureContactManifold(
			int workerIndex, CollidablePair pair, int childIndexA, int childIndexB,
			ref ConvexContactManifold manifold) => true;

		public void Dispose()
		{
		}
	}

	private struct PoseIntegratorCallbacks : IPoseIntegratorCallbacks
	{
		public AngularIntegrationMode AngularIntegrationMode => AngularIntegrationMode.Nonconserving;

		public bool AllowSubstepsForUnconstrainedBodies => false;

		public bool IntegrateVelocityForKinematics => false;

		public void Initialize(Simulation simulation)
		{
		}

		public void PrepareForIntegration(float dt)
		{
		}

		public void IntegrateVelocity(
			System.Numerics.Vector<int> bodyIndices,
			Vector3Wide position,
			QuaternionWide orientation,
			BodyInertiaWide localInertia,
			System.Numerics.Vector<int> integrationMask,
			int workerIndex,
			System.Numerics.Vector<float> dt,
			ref BodyVelocityWide velocity)
		{
		}

		public void Dispose()
		{
		}
	}
}
