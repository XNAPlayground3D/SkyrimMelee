using DigitalRiseModel;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra;
using SkyrimMelee.Combat;
using SkyrimMelee.Core;
using SkyrimMelee.Input;
using SkyrimMelee.Fighters;
using SkyrimMelee.Graphics;
using SkyrimMelee.Ui;
using SkyrimMelee.Attacks;
using SkyrimMelee.Physics;
using SkyrimMelee.Ai;
using SkyrimMelee.Animation;
using SkyrimMelee.Tuning;

namespace SkyrimMelee;

public sealed class SkyrimMeleeGame : Game
{
	private static readonly Color BackgroundColor = new(14, 15, 20);
	private static readonly Color AxisXColor = new(0xE6, 0x5A, 0x5A);
	private static readonly Color AxisYColor = new(0x5A, 0xD0, 0x6A);
	private static readonly Color AxisZColor = new(0x5A, 0x90, 0xE6);
	private static readonly Color HitFlashColor = new(0xFF, 0x6B, 0x4A);

	private readonly GameOptions _options;
	private readonly GameClock _clock = new();
	private readonly Camera _camera = new();
	private readonly GraphicsDeviceManager _graphics;

	private MyraHost? _ui;
	private HudPanel? _hud;
	private DebugOverlay? _overlay;
	private PrimitiveLibrary? _primitives;
	private ForwardRenderer? _renderer;
	private Arena? _arena;
	private FighterRig? _player;
	private FighterRig? _opponent;
	private GreatswordRig? _playerSword;
	private GreatswordRig? _opponentSword;
	private DrMaterial? _gizmoGrip;
	private DrMaterial? _gizmoMid;
	private DrMaterial? _gizmoTip;
	private DrMaterial? _bladeDebug;
	private DrMaterial? _axisX;
	private DrMaterial? _axisY;
	private DrMaterial? _axisZ;
	private DrMaterial? _hurtboxDebug;
	private DrMaterial? _guardDebug;
	private DrMaterial? _hitDebug;
	private DrMaterial? _trailDebug;
	private CombatWorld? _combatWorld;
	private BladeSweep? _bladeSweep;
	private readonly SwingHitSet _swingHits = new();
	private readonly SwingHitSet _opponentSwingHits = new();
	private readonly InputRouter _input = new();
	private readonly AttackCharge _attackCharge = new();
	private int _hitCount;
	private string _lastHit = "-";
	private string _lastRejection = "-";
	private Vector2 _moveInput;
	private bool _wasActive = true;
	private bool _skipLook;
	private readonly FighterStats _playerStats = new();
	private readonly FighterStats _opponentStats = new();
	private BladeGeometry? _lastBlade;
	private BladeGeometry? _opponentLastBlade;
	private DuelistAi? _ai;
	private readonly float[] _hitFlashes = new float[2 * 5];
	private float _staminaCue;
	private bool _guardHurtbox;
	private bool _opponentGuardHurtbox;
	private ScreenGradient? _backdrop;
	private SpriteBatch? _spriteBatch;
	private SwordMotion? _swordMotion;
	private BodyMotion? _bodyMotion;
	private IReadOnlyList<SwordClip> _swordClips = Array.Empty<SwordClip>();
	private IReadOnlyList<BodyClip> _bodyClips = Array.Empty<BodyClip>();
	private SwordMotion? _opponentSwordMotion;
	private BodyMotion? _opponentBodyMotion;
	private AttackDefinition? _slashAttack;
	private AttackDefinition? _slashRightAttack;
	private AttackDefinition? _powerSlashAttack;
	private AttackDefinition? _bashAttack;
	private readonly CombatStateMachine _combat = new();
	private readonly CombatStateMachine _opponentCombat = new();
	private readonly DuelDirector _duel = new();
	private bool _paused;
	private double _baseTimeScale = 1.0;
	private float _swordClipSpeed = 1f;
	private float _fps;
	private double _fpsWindowTime;
	private int _fpsWindowFrames;
	private double _labelTimer;
	private bool _f1WasDown;
	private bool _f2WasDown;
	private bool _f3WasDown;
	private bool _viewWasDown;
	private int _scrollWheelValue;
	private bool _pauseWasDown;
	private bool _rWasDown;
	private bool _minusWasDown;
	private bool _plusWasDown;
	private bool _d1WasDown;
	private bool _d2WasDown;
	private bool _d4WasDown;
	private bool _d5WasDown;
	private bool _d6WasDown;
	private bool _d7WasDown;
	private bool _d8WasDown;
	private bool _bashKeyWasDown;
	private bool _tuneSlowWasDown;
	private bool _tuneFastWasDown;
	private bool _tuneSelectWasDown;
	private readonly TuningSession _tuning = new();
	private readonly BladeGeometry[] _bladeTrail = new BladeGeometry[20];
	private int _trailCount;
	private int _trailIndex;
	private Vector3 _contactPoint;
	private Vector3 _contactNormal = Vector3.UnitY;
	private float _contactTimer;
	private string _lastOutcome = "-";
	private int _blocksTaken;
	private int _timedBlocksTaken;
	private int _guardBreaksTaken;
	private int _hitsTaken;
	private int _foeBlocks;
	private int _foeGuardBreaks;
	private bool _reactionAdvance;
	private bool _rightMouseWasDown;

	public SkyrimMeleeGame(GameOptions options)
	{
		_options = options;
		_graphics = new GraphicsDeviceManager(this)
		{
			PreferredBackBufferWidth = 1600,
			PreferredBackBufferHeight = 900,
			SynchronizeWithVerticalRetrace = true
		};

		_baseTimeScale = options.SlowMotion ? 0.25 : 1.0;
		_clock.TimeScale = _baseTimeScale;

		// Draw at the display rate; the simulation advances through GameClock.
		IsFixedTimeStep = false;
		IsMouseVisible = true;

		Window.Title = "SkyrimMelee";
		Window.AllowUserResizing = true;
	}

	/// <summary>Number of fixed 60 Hz steps executed since launch.</summary>
	public long SimulationSteps => _clock.FrameIndex;

	protected override void Initialize()
	{
		_graphics.ApplyChanges();
		base.Initialize();
	}

	protected override void LoadContent()
	{
		if (!_options.NoUi)
		{
			_ui = new MyraHost(this);

			_hud = new HudPanel();
			_overlay = new DebugOverlay(_options);
			_overlay.Visible = _options.Debug;
			_hud.Widgets.Add(_overlay);

			_ui.Root = _hud;
		}

		_primitives = new PrimitiveLibrary(GraphicsDevice);
		_renderer = new ForwardRenderer(GraphicsDevice) { Wireframe = _options.Debug };
		_arena = new Arena(_primitives);
		_player = new FighterRig(_primitives, new Vector3(-1f, 0f, 0f), 0f);
		_opponent = new FighterRig(_primitives, new Vector3(1f, 0f, 0f), MathHelper.Pi);
		_playerSword = new GreatswordRig(_primitives, FighterVisuals.PlayerHand);
		_opponentSword = new GreatswordRig(_primitives, FighterVisuals.OpponentHand);
		_gizmoGrip = _primitives.CreateMaterial(new Color(0xFF, 0x6B, 0x4A));
		_gizmoMid = _primitives.CreateMaterial(new Color(0x9F, 0xD8, 0xFF));
		_gizmoTip = _primitives.CreateMaterial(new Color(0xFF, 0xD2, 0x7A));
		_bladeDebug = _primitives.CreateMaterial(new Color(0x5A, 0xD7, 0xFF));
		_axisX = _primitives.CreateMaterial(AxisXColor);
		_axisY = _primitives.CreateMaterial(AxisYColor);
		_axisZ = _primitives.CreateMaterial(AxisZColor);
		_hurtboxDebug = _primitives.CreateMaterial(new Color(0x49, 0xE8, 0x8A));
		_guardDebug = _primitives.CreateMaterial(new Color(0xFF, 0xC2, 0x4B));
		_hitDebug = _primitives.CreateMaterial(HitFlashColor);
		_trailDebug = _primitives.CreateMaterial(new Color(0x3A, 0x8E, 0xA8));

		_swordClips = SwordClips.Create();
		_bodyClips = BodyClips.Create();
		var attacks = AttackCatalog.Create(_swordClips);
		_slashAttack = attacks.Single(attack => attack.Name == "SlashLeft");
		_slashRightAttack = attacks.Single(attack => attack.Name == "SlashRight");
		_powerSlashAttack = attacks.Single(attack => attack.Name == "PowerSlash");
		_bashAttack = attacks.Single(attack => attack.Name == "Bash");
		_swordMotion = new SwordMotion();
		_swordMotion.ResetTo(_swordClips[0], _options.ClipTime ?? 0f);
		_bodyMotion = new BodyMotion();
		// The opponent does not act until the AI exists, but it reacts to hits.
		_opponentSwordMotion = new SwordMotion();
		_opponentSwordMotion.ResetTo(_swordClips[0]);
		_opponentBodyMotion = new BodyMotion();
		_ai = new DuelistAi(AiProfile.Adept, seed: 20261120);

		_combatWorld = new CombatWorld();
		_combatWorld.AddFighter(FighterId.Player, _player.DrawTransform);
		_combatWorld.AddFighter(FighterId.Opponent, _opponent.DrawTransform);
		_bladeSweep = new BladeSweep(_combatWorld);

		if (_options.ClipTime is not null)
		{
			SetPaused(true);
		}

		ApplySwordPose();

		_backdrop = new ScreenGradient(GraphicsDevice);
		_spriteBatch = new SpriteBatch(GraphicsDevice);
		_camera.Update(_player.Position, 0f);
		IsMouseVisible = false;
		CenterMouse();
		_skipLook = true;

		base.LoadContent();
	}

	protected override void UnloadContent()
	{
		_combatWorld?.Dispose();
		_combatWorld = null;

		_playerSword?.Dispose();
		_playerSword = null;

		_opponentSword?.Dispose();
		_opponentSword = null;

		_player?.Dispose();
		_player = null;

		_opponent?.Dispose();
		_opponent = null;

		_arena?.Dispose();
		_arena = null;

		_backdrop?.Dispose();
		_backdrop = null;

		_spriteBatch?.Dispose();
		_spriteBatch = null;

		_renderer?.Dispose();
		_renderer = null;

		_primitives?.Dispose();
		_primitives = null;

		_ui?.Dispose();
		_ui = null;
		base.UnloadContent();
	}

	protected override void Update(GameTime gameTime)
	{
		var keyboard = Keyboard.GetState();
		if (keyboard.IsKeyDown(Keys.Escape) ||
			GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
		{
			Exit();
			return;
		}

		var f1Down = keyboard.IsKeyDown(Keys.F1);
		if (f1Down && !_f1WasDown)
		{
			_overlay?.Toggle();
		}

		_f1WasDown = f1Down;

		var f2Down = keyboard.IsKeyDown(Keys.F2);
		if (f2Down && !_f2WasDown && _renderer is not null)
		{
			_renderer.Wireframe = !_renderer.Wireframe;
		}

		_f2WasDown = f2Down;

		var elapsed = gameTime.ElapsedGameTime.TotalSeconds;
		var uiCaptured = _ui?.CapturesInput == true;

		var f3Down = keyboard.IsKeyDown(Keys.F3);
		if (f3Down && !_f3WasDown && !uiCaptured)
		{
			_camera.CyclePreset();
		}

		_f3WasDown = f3Down;

		var viewToggle = keyboard.IsKeyDown(Keys.F);
		if (viewToggle && !_viewWasDown && !uiCaptured && !_paused)
		{
			_camera.ToggleMode();
		}

		_viewWasDown = viewToggle;

		var mouseState = Mouse.GetState();
		var scrollDelta = mouseState.ScrollWheelValue - _scrollWheelValue;
		_scrollWheelValue = mouseState.ScrollWheelValue;
		if (scrollDelta != 0 && !uiCaptured && !_paused)
		{
			_camera.Zoom(scrollDelta / 120f);
		}

		UpdateTuningKeys(keyboard, uiCaptured);

		var moveX = (keyboard.IsKeyDown(Keys.D) ? 1f : 0f) -
					(keyboard.IsKeyDown(Keys.A) ? 1f : 0f);
		var moveY = (keyboard.IsKeyDown(Keys.W) ? 1f : 0f) -
					(keyboard.IsKeyDown(Keys.S) ? 1f : 0f);
		_moveInput = new Vector2(moveX, moveY);
		UpdateLook(uiCaptured);
		if (_input.Tick((float)elapsed))
		{
			_lastRejection = "buffer expired";
		}

		UpdateFps(elapsed);
		UpdateSwordClipInput(keyboard, uiCaptured);

		var mouse = Mouse.GetState();
		if (!_paused && !uiCaptured)
		{
			var charge = _attackCharge.Update(
				mouse.LeftButton == ButtonState.Pressed, (float)elapsed);

			if (charge == ChargeResult.Power)
			{
				_input.Push(CombatIntent.PowerSlash);
			}
			else if (charge == ChargeResult.Tap)
			{
				_input.Push(_combat.State is CombatState.GuardRaise or CombatState.GuardHold
					? CombatIntent.Bash
					: SlashCombo.Next(_combat.State, _combat.Attack));
			}
		}
		else
		{
			_attackCharge.Reset();
		}

		var steps = _clock.Advance(elapsed);
		for (var i = 0; i < steps; i++)
		{
			StepSimulation();
		}

		ProcessIntent(uiCaptured);
		UpdateGuardFromMouse(uiCaptured);

		if (_swordMotion is not null)
		{
			if (!_paused)
			{
				AdvanceGuard();
			}

			ApplySwordPose();
			UpdateGuardHurtbox();

			_hud?.UpdatePlayback(_swordMotion.Clip?.Name ?? "-", _swordClipSpeed,
				_swordMotion.Time, _swordMotion.Clip?.Duration ?? 0f);
		}

		_staminaCue = MathF.Max(0f, _staminaCue - (float)elapsed);
		_camera.IdleDrift = !_paused && _combat.State == CombatState.Idle &&
			_moveInput.LengthSquared() < 0.01f;
		_camera.Update(_player?.Position ?? Vector3.Zero, (float)elapsed);

		_labelTimer += elapsed;
		if (_labelTimer >= 0.25)
		{
			_labelTimer = 0;
			UpdateDebugText();
		}

		base.Update(gameTime);
	}

	private void StepSimulation()
	{
		if (_paused)
		{
			return;
		}

		var step = (float)_clock.DeltaTime * _swordClipSpeed;
		FaceCameraYaw();
		ApplyMovement(step);
		var wasActive = _combat.State == CombatState.Active;

		if (_combat.State != CombatState.Idle || _reactionAdvance)
		{
			_swordMotion?.Update(step);
			_bodyMotion?.Update(step);
		}

		_combat.Update(step);
		_opponentCombat.Update(step);
		UpdateOpponentFacing();
		UpdateOpponentAi(step);
		if (_opponentCombat.State != CombatState.Idle)
		{
			_opponentSwordMotion?.Update(step);
			_opponentBodyMotion?.Update(step);
		}

		AdvanceOpponentGuard();

		var blade = CurrentPlayerBlade();
		ResolveBashHit();
		if ((wasActive || _combat.State == CombatState.Active) &&
			_lastBlade is { } previous && blade is { } current)
		{
			ResolveHits(previous, current, step);
		}

		_lastBlade = blade;

		var opponentBlade = CurrentOpponentBlade();
		if (_opponentCombat.State == CombatState.Active &&
			_opponentLastBlade is { } opponentPrevious && opponentBlade is { } opponentCurrent)
		{
			ResolveOpponentHits(opponentPrevious, opponentCurrent, step);
		}

		_opponentLastBlade = opponentBlade;
		if (blade.HasValue && _overlay?.Visible == true)
		{
			_bladeTrail[_trailIndex] = blade.Value;
			_trailIndex = (_trailIndex + 1) % _bladeTrail.Length;
			_trailCount = Math.Min(_bladeTrail.Length, _trailCount + 1);
		}

		_contactTimer = MathF.Max(0f, _contactTimer - step);
		UpdateHitFlashes(step);

		var blocking = _combat.State is CombatState.GuardRaise or
			CombatState.GuardHold or CombatState.GuardLower;
		_playerStats.Update(step, blocking);
		_opponentStats.Update(step, _opponentCombat.Blocks);

		if (_duel.Update(step))
		{
			ResetRound();
		}
	}

	private void UpdateLook(bool uiCaptured)
	{
		if (uiCaptured)
		{
			IsMouseVisible = true;
			return;
		}

		IsMouseVisible = false;

		if (!IsActive)
		{
			_wasActive = false;
			CenterMouse();
			return;
		}

		if (!_wasActive)
		{
			_wasActive = true;
			_skipLook = true;
		}

		var center = MouseCenter();
		var mouse = Mouse.GetState();
		if (!_skipLook)
		{
			_camera.AddLook(mouse.X - center.X, mouse.Y - center.Y);
		}

		_skipLook = false;
		Mouse.SetPosition(center.X, center.Y);
	}

	private Point MouseCenter()
	{
		var width = GraphicsDevice.PresentationParameters.BackBufferWidth;
		var height = GraphicsDevice.PresentationParameters.BackBufferHeight;
		return new Point(width / 2, height / 2);
	}

	private void CenterMouse() => Mouse.SetPosition(MouseCenter().X, MouseCenter().Y);

	/// <summary>Turns the body with the camera whenever the fighter is not committed to a swing.</summary>
	private void FaceCameraYaw()
	{
		if (_player is null || _combat.State is CombatState.Windup or CombatState.Active)
		{
			return;
		}

		var forward = new Vector3(MathF.Cos(_camera.Yaw), 0f, MathF.Sin(_camera.Yaw));
		_player.FacingYaw = Locomotion.FacingFor(forward);
	}

	private void ApplyMovement(float step)
	{
		if (_player is null || _opponent is null || _moveInput.LengthSquared() < 0.01f)
		{
			return;
		}

		var multiplier = Locomotion.MovementMultiplier(
			_combat.State, _combat.Attack?.LocksMovement == true);
		if (multiplier <= 0f)
		{
			return;
		}

		var input = Vector2.Normalize(_moveInput);
		var forward = new Vector3(MathF.Cos(_camera.Yaw), 0f, MathF.Sin(_camera.Yaw));
		var right = new Vector3(-MathF.Sin(_camera.Yaw), 0f, MathF.Cos(_camera.Yaw));
		var move = forward * input.Y + right * input.X;
		if (move.LengthSquared() < 1e-4f)
		{
			return;
		}

		move.Normalize();
		var speed = CombatTuning.PlayerWalkSpeed * multiplier * step;
		var position = _player.Position + move * speed;
		_player.Position = Locomotion.Clamp(
			position,
			_opponent.Position,
			CombatTuning.ArenaRadius,
			CombatTuning.OpponentSeparation);

		// Skyrim-style: moving keeps the body facing the camera, but an attack
		// locks its facing for the committed swing.
		if (_combat.State is not (CombatState.Windup or CombatState.Active))
		{
			_player.FacingYaw = Locomotion.FacingFor(forward);
		}
	}

	private BladeGeometry? CurrentPlayerBlade()
	{
		if (_swordMotion is null || _player is null)
		{
			return null;
		}

		var swordWorld = _swordMotion.CurrentPose.ToMatrix() * _player.GetRightHandTransform();
		return BladeGeometry.FromWorld(swordWorld);
	}

	/// <summary>
	/// Bash hit volume: Skyrim's shove is a short forward stagger, not a blade
	/// sweep. A connected bash always staggers the target (guard shove or
	/// interrupt), which is what makes it useful against a committed attack.
	/// </summary>
	private void ResolveBashHit()
	{
		if (_player is null || _opponent is null || _opponentStats.IsDead ||
			_combat.Attack?.Name != "Bash" || _combat.State != CombatState.Active)
		{
			return;
		}

		var forward = new Vector3(
			MathF.Cos(_player.FacingYaw), 0f, -MathF.Sin(_player.FacingYaw));
		if (!BashRules.InRange(_player.Position, forward, _opponent.Position))
		{
			return;
		}

		var point = _opponent.Position + new Vector3(0f, 1.15f, 0f);
		var hit = new BladeHit(
			new Hurtbox(FighterId.Opponent, HurtboxKind.Torso), point, forward, 0f);
		if (!_swingHits.TryRegister(
				hit, FighterId.Player, _combat.AttackTime, out var hitEvent))
		{
			return;
		}

		var guarding = _opponentCombat.Blocks;
		var outcome = guarding ? HitOutcome.Block : HitOutcome.Clean;
		var defense = DefenseResolver.Resolve(
			hitEvent.Attack, _playerStats, _opponentStats, outcome);
		_opponentStats.ApplyDamage(defense.Damage.Final);

		_hitCount++;
		_lastHit = $"Bash@{_combat.AttackTime:0.00}";
		_lastOutcome = $"bash {(guarding ? "block" : "clean")} torso";
		_contactPoint = point;
		_contactNormal = forward;
		_contactTimer = 1f;

		if (!guarding)
		{
			for (var i = 0; i < 4; i++)
			{
				_hitFlashes[HitIndex(FighterId.Opponent, (HurtboxKind)i)] =
					_tuning.Value(TuningKey.HitFlash);
			}

			_opponent.SetBodyColor(HitFlashColor);
		}

		var large = defense.GuardBroken;
		_opponentCombat.ApplyStagger(large
			? _tuning.Value(TuningKey.StaggerLarge)
			: _tuning.Value(TuningKey.StaggerSmall));
		PlayOpponentClip(large ? "StaggerLarge" : "StaggerSmall");

		if (_options.ScreenShake)
		{
			_camera.AddShake(guarding ? 0.012f : 0.030f, 0.09f);
		}

		if (_opponentStats.IsDead)
		{
			_opponentCombat.Kill();
			PlayOpponentClip("Death");
			_duel.ReportDeath(FighterId.Opponent);
		}
	}

	private void ResolveHits(BladeGeometry previous, BladeGeometry current, float step)
	{
		if (_bladeSweep is null || _opponent is null || _player is null)
		{
			return;
		}

		var hits = _bladeSweep.Sweep(previous, current, step);
		if (hits.Count == 0)
		{
			return;
		}

		var guarding = _opponentCombat.Blocks;
		var guardAge = guarding ? _opponentCombat.StateTime : 0f;
		var defenderForward = new Vector3(
			MathF.Cos(_opponent.FacingYaw), 0f, -MathF.Sin(_opponent.FacingYaw));
		var outcome = GuardResolver.Classify(
			hits, FighterId.Opponent, guarding, guardAge, defenderForward,
			_opponent.Position, _player.Position, out var contact);

		if (!_swingHits.TryRegister(contact, FighterId.Player, _combat.AttackTime,
				out var hitEvent))
		{
			return;
		}

		if (hitEvent.Target != FighterId.Opponent || _opponentStats.IsDead)
		{
			return;
		}

		_hitCount++;
		_lastHit = $"{hitEvent.Hurtbox}@{_combat.AttackTime:0.00}";
		_lastOutcome = $"{outcome} {hitEvent.Hurtbox}".ToLowerInvariant();
		_contactPoint = hitEvent.Point;
		_contactNormal = hitEvent.Normal;
		_contactTimer = 1f;

		if (outcome != HitOutcome.Clean)
		{
			var block = DefenseResolver.Resolve(
				hitEvent.Attack, _playerStats, _opponentStats, outcome);

			if (block.GuardBroken)
			{
				_foeGuardBreaks++;
				_opponentCombat.ApplyStagger(block.DefenderStagger);
				PlayOpponentClip("StaggerLarge");
			}
			else
			{
				_foeBlocks++;
			}

			if (block.AttackerStagger > 0f)
			{
				_combat.ApplyStagger(block.AttackerStagger);
				PlayMicroStagger();
			}

			if (_options.ScreenShake)
			{
				_camera.AddShake(0.010f, 0.09f);
			}

			return;
		}

		// Feedback: the whole defender flashes coral, so a low or occluded
		// hit part (hips/legs) cannot hide the hit. Damage is once per swing.
		for (var i = 0; i < 4; i++)
		{
			_hitFlashes[HitIndex(hitEvent.Target, (HurtboxKind)i)] =
				_tuning.Value(TuningKey.HitFlash);
		}

		_opponent.SetBodyColor(HitFlashColor);

		if (!_swingHits.TryMarkDamagingHit())
		{
			return;
		}

		var stagger = StaggerRules.Resolve(hitEvent.Attack, _opponentStats);
		var staggerLarge = stagger.Staggered &&
			stagger.Duration >= CombatTuning.StaggerLargeDuration - 0.001f;
		if (stagger.Staggered)
		{
			var duration = _tuning.Value(
				staggerLarge ? TuningKey.StaggerLarge : TuningKey.StaggerSmall);
			_opponentCombat.ApplyStagger(duration);
		}

		var defense = DefenseResolver.Resolve(
			hitEvent.Attack, _playerStats, _opponentStats, HitOutcome.Clean);
		_opponentStats.ApplyDamage(defense.Damage.Final);

		if (_options.ScreenShake)
		{
			var severity = Math.Clamp(
				defense.Damage.Final / _opponentStats.MaxHealth, 0f, 1f);
			_camera.AddShake(0.012f + severity * 0.10f, 0.09f);
		}

		if (_opponentStats.IsDead)
		{
			_opponentCombat.Kill();
			PlayOpponentClip("Death");
			_duel.ReportDeath(FighterId.Opponent);
		}
		else if (stagger.Staggered)
		{
			PlayOpponentClip(staggerLarge ? "StaggerLarge" : "StaggerSmall");
		}
	}

	private BladeGeometry? CurrentOpponentBlade()
	{
		if (_opponentSwordMotion is null || _opponent is null)
		{
			return null;
		}

		var swordWorld = _opponentSwordMotion.CurrentPose.ToMatrix() *
							_opponent.GetRightHandTransform();
		return BladeGeometry.FromWorld(swordWorld);
	}

	private void UpdateOpponentFacing()
	{
		if (_player is null || _opponent is null ||
			_opponentCombat.State is CombatState.Windup or CombatState.Active)
		{
			return;
		}

		var direction = _player.Position - _opponent.Position;
		if (direction.LengthSquared() > 1e-6f)
		{
			_opponent.FacingYaw = Locomotion.FacingFor(direction);
		}
	}

	private void UpdateOpponentAi(float step)
	{
		if (_ai is null || _player is null || _opponent is null ||
			_opponentStats.IsDead || _playerStats.IsDead)
		{
			return;
		}

		var context = new AiContext(
			_combat.State,
			_playerStats.Stamina,
			_opponentCombat.State,
			_opponentStats.Stamina,
			Vector3.Distance(_player.Position, _opponent.Position));

		_ai.Update(step, context);
		MoveOpponent(step, _ai.Approach);
		ApplyOpponentGuard(_ai.GuardHeld);

		if (_ai.Intent is { } intent)
		{
			TryOpponentAttack(intent);
		}
	}

	private void MoveOpponent(float step, float approach)
	{
		if (_player is null || _opponent is null || MathF.Abs(approach) < 0.01f)
		{
			return;
		}

		var multiplier = Locomotion.MovementMultiplier(_opponentCombat.State);
		if (multiplier <= 0f)
		{
			return;
		}

		var direction = _player.Position - _opponent.Position;
		direction.Y = 0f;
		if (direction.LengthSquared() < 1e-6f)
		{
			return;
		}

		direction.Normalize();
		var position = _opponent.Position +
			direction * (approach * CombatTuning.PlayerWalkSpeed * multiplier * step);
		_opponent.Position = Locomotion.Clamp(
			position,
			_player.Position,
			CombatTuning.ArenaRadius,
			CombatTuning.OpponentSeparation);
	}

	private void ApplyOpponentGuard(bool held)
	{
		if (held)
		{
			if (_opponentCombat.State == CombatState.Idle &&
				_opponentCombat.TryStartGuard())
			{
				PlayOpponentClip("GuardRaise");
			}

			return;
		}

		if (_opponentCombat.State is CombatState.GuardRaise or CombatState.GuardHold &&
			_opponentCombat.TryReleaseGuard())
		{
			PlayOpponentClip("GuardLower");
		}
	}

	private void AdvanceOpponentGuard()
	{
		if (_opponentSwordMotion?.Clip is not { } clip || !_opponentSwordMotion.IsFinished)
		{
			return;
		}

		if (clip.Name == "GuardRaise")
		{
			PlayOpponentClip("GuardHold");
		}
		else if (clip.Name == "GuardLower")
		{
			ResetOpponentToIdle();
		}
	}

	private void ResetOpponentToIdle()
	{
		if (_opponentSwordMotion is null)
		{
			return;
		}

		_opponentSwordMotion.ResetTo(_swordClips[0]);
		_opponentBodyMotion?.Reset();
		ApplySwordPose();
	}

	private void TryOpponentAttack(CombatIntent intent)
	{
		if (_opponentCombat.State != CombatState.Idle)
		{
			return;
		}

		var attack = intent switch
		{
			CombatIntent.SlashLeft => _slashAttack,
			CombatIntent.SlashRight => _slashRightAttack,
			CombatIntent.PowerSlash => _powerSlashAttack,
			CombatIntent.Bash => _bashAttack,
			_ => null
		};

		if (attack is null || !_opponentStats.CanAfford(attack) ||
			!_opponentCombat.TryStartAttack(attack))
		{
			return;
		}

		if (_player is not null && _opponent is not null)
		{
			var direction = _player.Position - _opponent.Position;
			if (direction.LengthSquared() > 1e-6f)
			{
				_opponent.FacingYaw = Locomotion.FacingFor(direction);
			}
		}

		_opponentStats.Spend(attack);
		_opponentSwingHits.Begin(attack);
		PlayOpponentClip(attack.Name);
	}

	private void ResolveOpponentHits(BladeGeometry previous, BladeGeometry current, float step)
	{
		if (_bladeSweep is null || _player is null || _opponent is null)
		{
			return;
		}

		var hits = _bladeSweep.Sweep(previous, current, step);
		if (hits.Count == 0)
		{
			return;
		}

		var guarding = _combat.Blocks;
		var guardAge = guarding ? _combat.StateTime : 0f;
		var defenderForward = new Vector3(
			MathF.Cos(_player.FacingYaw), 0f, -MathF.Sin(_player.FacingYaw));
		var outcome = GuardResolver.Classify(
			hits, FighterId.Player, guarding, guardAge, defenderForward,
			_player.Position, _opponent.Position, out var contact);

		if (!_opponentSwingHits.TryRegister(contact, FighterId.Opponent,
				_opponentCombat.AttackTime, out var hitEvent))
		{
			return;
		}

		if (hitEvent.Target != FighterId.Player)
		{
			return;
		}

		_lastOutcome = $"taken {outcome} {contact.Hurtbox.Kind}".ToLowerInvariant();
		_contactPoint = hitEvent.Point;
		_contactNormal = hitEvent.Normal;
		_contactTimer = 1f;

		var defense = DefenseResolver.Resolve(
			hitEvent.Attack, _opponentStats, _playerStats, outcome);
		_playerStats.ApplyDamage(defense.Damage.Final);

		if (outcome == HitOutcome.Clean)
		{
			_hitsTaken++;
		}
		else if (defense.GuardBroken)
		{
			_guardBreaksTaken++;
		}
		else if (outcome == HitOutcome.TimedBlock)
		{
			_timedBlocksTaken++;
		}
		else
		{
			_blocksTaken++;
		}

		if (outcome == HitOutcome.Clean)
		{
			for (var i = 0; i < 4; i++)
			{
				_hitFlashes[HitIndex(FighterId.Player, (HurtboxKind)i)] =
					_tuning.Value(TuningKey.HitFlash);
			}

			_player.SetBodyColor(HitFlashColor);

			var stagger = StaggerRules.Resolve(hitEvent.Attack, _playerStats);
			if (stagger.Staggered)
			{
				var large = stagger.Duration >=
					CombatTuning.StaggerLargeDuration - 0.001f;
				_combat.ApplyStagger(_tuning.Value(
					large ? TuningKey.StaggerLarge : TuningKey.StaggerSmall));
				PlayClip(large ? "StaggerLarge" : "StaggerSmall");
			}
		}
		else if (defense.GuardBroken)
		{
			_combat.TryReleaseGuard();
			_combat.ApplyStagger(defense.DefenderStagger);
			PlayClip("StaggerLarge");
		}
		else if (defense.AttackerStagger > 0f)
		{
			_opponentCombat.ApplyStagger(defense.AttackerStagger);
			PlayOpponentClip("StaggerMicro");
		}

		if (_options.ScreenShake)
		{
			var severity = Math.Clamp(
				defense.Damage.Final / _playerStats.MaxHealth, 0f, 1f);
			_camera.AddShake(
				outcome == HitOutcome.Clean ? 0.012f + severity * 0.10f : 0.010f,
				0.09f);
		}

		if (_playerStats.IsDead)
		{
			_combat.Kill();
			PlayDeath();
			_duel.ReportDeath(FighterId.Player);
		}
	}

	private void UpdateHitFlashes(float step)
	{
		for (var i = 0; i < _hitFlashes.Length; i++)
		{
			if (_hitFlashes[i] <= 0f)
			{
				continue;
			}

			_hitFlashes[i] -= step;
			if (_hitFlashes[i] > 0f)
			{
				continue;
			}

			_hitFlashes[i] = 0f;
			var fighter = (FighterId)(i / 5);
			var kind = (HurtboxKind)(i % 5);
			(fighter == FighterId.Player ? _player : _opponent)
				?.SetBodyPartColor(kind, Color.White);
		}
	}

	private void ClearHitFlashes()
	{
		Array.Clear(_hitFlashes);
		foreach (var kind in new[]
		{
			HurtboxKind.Head, HurtboxKind.Torso, HurtboxKind.Hips, HurtboxKind.Legs
		})
		{
			_player?.SetBodyPartColor(kind, Color.White);
			_opponent?.SetBodyPartColor(kind, Color.White);
		}
	}

	private static int HitIndex(FighterId fighter, HurtboxKind kind) =>
		(int)fighter * 5 + (int)kind;

	private void UpdateFps(double elapsed)
	{
		_fpsWindowTime += elapsed;
		_fpsWindowFrames++;

		if (_fpsWindowTime < 0.25)
		{
			return;
		}

		_fps = (float)(_fpsWindowFrames / _fpsWindowTime);
		_fpsWindowTime = 0;
		_fpsWindowFrames = 0;
	}

	private void UpdateSwordClipInput(KeyboardState keyboard, bool uiCaptured)
	{
		var leftSlash = keyboard.IsKeyDown(Keys.D1) || keyboard.IsKeyDown(Keys.NumPad1);
		if (leftSlash && !_d1WasDown && !_paused && !uiCaptured)
		{
			_input.Push(CombatIntent.SlashLeft);
		}

		_d1WasDown = leftSlash;

		var rightSlash = keyboard.IsKeyDown(Keys.D2) || keyboard.IsKeyDown(Keys.NumPad2);
		if (rightSlash && !_d2WasDown && !_paused && !uiCaptured)
		{
			_input.Push(CombatIntent.SlashRight);
		}

		_d2WasDown = rightSlash;

		var staggerSmall = keyboard.IsKeyDown(Keys.D4) || keyboard.IsKeyDown(Keys.NumPad4);
		if (staggerSmall && !_d4WasDown)
		{
			PlayStagger(false);
		}

		_d4WasDown = staggerSmall;

		var staggerLarge = keyboard.IsKeyDown(Keys.D5) || keyboard.IsKeyDown(Keys.NumPad5);
		if (staggerLarge && !_d5WasDown)
		{
			PlayStagger(true);
		}

		_d5WasDown = staggerLarge;

		var bash = keyboard.IsKeyDown(Keys.B);
		if (bash && !_bashKeyWasDown && !_paused && !uiCaptured)
		{
			_input.Push(CombatIntent.Bash);
		}

		_bashKeyWasDown = bash;

		var death = keyboard.IsKeyDown(Keys.D6) || keyboard.IsKeyDown(Keys.NumPad6);
		if (death && !_d6WasDown)
		{
			PlayDeath();
		}

		_d6WasDown = death;

		var victory = keyboard.IsKeyDown(Keys.D7) || keyboard.IsKeyDown(Keys.NumPad7);
		if (victory && !_d7WasDown)
		{
			PlayVictory();
		}

		_d7WasDown = victory;

		var microStagger = keyboard.IsKeyDown(Keys.D8) || keyboard.IsKeyDown(Keys.NumPad8);
		if (microStagger && !_d8WasDown)
		{
			PlayMicroStagger();
		}

		_d8WasDown = microStagger;

		var pause = keyboard.IsKeyDown(Keys.P);
		if (pause && !_pauseWasDown)
		{
			SetPaused(!_paused);
		}

		_pauseWasDown = pause;

		var restart = keyboard.IsKeyDown(Keys.R);
		if (restart && !_rWasDown)
		{
			RestartSwordClip();
		}

		_rWasDown = restart;

		var slower = keyboard.IsKeyDown(Keys.Z) || keyboard.IsKeyDown(Keys.OemMinus);
		if (slower && !_minusWasDown)
		{
			_swordClipSpeed = MathF.Max(0.05f, _swordClipSpeed - 0.1f);
		}

		_minusWasDown = slower;

		var faster = keyboard.IsKeyDown(Keys.X) || keyboard.IsKeyDown(Keys.OemPlus);
		if (faster && !_plusWasDown)
		{
			_swordClipSpeed = MathF.Min(2f, _swordClipSpeed + 0.1f);
		}

		_plusWasDown = faster;
	}

	private SwordClip? FindClip(string name) =>
		_swordClips.FirstOrDefault(clip => clip.Name == name);

	private void PlayClip(string name)
	{
		_reactionAdvance = false;

		if (_swordMotion is null)
		{
			return;
		}

		var clip = FindClip(name);
		if (clip is null)
		{
			return;
		}

		_swordMotion.Play(clip, restart: true);

		var body = _bodyClips.FirstOrDefault(candidate => candidate.Name == name);
		if (body is null)
		{
			_bodyMotion?.Reset();
		}
		else
		{
			_bodyMotion?.Play(body, restart: true);
		}

		ApplySwordPose();
	}

	private void PlayOpponentClip(string name)
	{
		if (_opponentSwordMotion is null)
		{
			return;
		}

		var clip = FindClip(name);
		if (clip is null)
		{
			return;
		}

		_opponentSwordMotion.Play(clip, restart: true);

		var body = _bodyClips.FirstOrDefault(candidate => candidate.Name == name);
		if (body is null)
		{
			_opponentBodyMotion?.Reset();
		}
		else
		{
			_opponentBodyMotion?.Play(body, restart: true);
		}

		ApplySwordPose();
	}

	private void ProcessIntent(bool uiCaptured)
	{
		if (uiCaptured)
		{
			_input.Clear();
			return;
		}

		if (_paused || !_input.TryPeek(out var intent))
		{
			return;
		}

		if (TryExecuteIntent(intent))
		{
			_input.Clear();
		}
	}

	private bool TryExecuteIntent(CombatIntent intent)
	{
		if (_swordMotion is null)
		{
			return true;
		}

		var attack = intent switch
		{
			CombatIntent.SlashLeft => _slashAttack,
			CombatIntent.SlashRight => _slashRightAttack,
			CombatIntent.PowerSlash => _powerSlashAttack,
			CombatIntent.Bash => _bashAttack,
			_ => null
		};

		if (attack is null)
		{
			return true;
		}

		if (intent == CombatIntent.Bash &&
			_combat.State is not (CombatState.GuardRaise or CombatState.GuardHold))
		{
			_lastRejection = "bash needs guard";
			return true;
		}

		// Skyrim: a power attack with no stamina degrades to a normal swing
		// (the stamina bar flashes) instead of being rejected.
		var powerFallback = false;
		if (intent == CombatIntent.PowerSlash && !StaminaRules.Allows(_playerStats, attack))
		{
			if (_slashAttack is null)
			{
				return true;
			}

			attack = _slashAttack;
			_staminaCue = 0.8f;
			powerFallback = true;
		}

		if (!_playerStats.CanAfford(attack))
		{
			_staminaCue = 0.8f;
			_lastRejection = "stamina";
			return true;
		}

		if (!_combat.TryStartAttack(attack))
		{
			return false;
		}

		if (_player is not null)
		{
			var forward = new Vector3(MathF.Cos(_camera.Yaw), 0f, MathF.Sin(_camera.Yaw));
			_player.FacingYaw = Locomotion.FacingFor(forward);
		}

		_playerStats.Spend(attack);
		_swingHits.Begin(attack);
		_trailCount = 0;
		PlayClip(attack.Name);

		if (attack.Name == "PowerSlash")
		{
			_camera.PunchFov(2.5f, 0.18f);
		}

		_lastRejection = powerFallback ? "no stamina: power->slash" : "-";
		return true;
	}

	private void UpdateTuningKeys(KeyboardState keyboard, bool uiCaptured)
	{
		var slowKey = keyboard.IsKeyDown(Keys.OemOpenBrackets);
		var fastKey = keyboard.IsKeyDown(Keys.OemCloseBrackets);
		var selectKey = keyboard.IsKeyDown(Keys.OemSemicolon);
		var active = _overlay?.Visible == true && !uiCaptured && !_paused;

		if (active && slowKey && !_tuneSlowWasDown)
		{
			_tuning.Nudge(-1);
		}

		if (active && fastKey && !_tuneFastWasDown)
		{
			_tuning.Nudge(1);
		}

		if (active && selectKey && !_tuneSelectWasDown)
		{
			_tuning.Next();
		}

		_tuneSlowWasDown = slowKey;
		_tuneFastWasDown = fastKey;
		_tuneSelectWasDown = selectKey;
	}

	private void UpdateGuardFromMouse(bool uiCaptured)
	{
		var right = Mouse.GetState().RightButton == ButtonState.Pressed;

		if (!_paused && !uiCaptured)
		{
			if (right)
			{
				PlayGuardRaise();
			}
			else if (_combat.State is CombatState.GuardRaise or CombatState.GuardHold)
			{
				PlayGuardLower();
			}
		}

		_rightMouseWasDown = right;
	}

	private void PlayGuardRaise()
	{
		if (_paused || _swordMotion is null)
		{
			return;
		}

		// A bash that just ended resumes at the guard pose. Replaying
		// GuardRaise would start from idle and read as a dip after every bash.
		if (_swordMotion.Clip?.Name == "Bash" && _combat.TryResumeGuard())
		{
			PlayClip("GuardHold");
			return;
		}

		if (!_combat.TryStartGuard())
		{
			return;
		}

		PlayClip("GuardRaise");
	}

	private void PlayGuardLower()
	{
		if (_paused || _swordMotion is null || !_combat.TryReleaseGuard())
		{
			return;
		}

		PlayClip("GuardLower");
	}

	private void PlayStagger(bool large)
	{
		if (_paused || _swordMotion is null)
		{
			return;
		}

		var duration = large
			? _tuning.Value(TuningKey.StaggerLarge)
			: _tuning.Value(TuningKey.StaggerSmall);
		_combat.ApplyStagger(duration);
		if (_combat.State != CombatState.Stagger)
		{
			return;
		}

		PlayClip(large ? "StaggerLarge" : "StaggerSmall");
	}

	private void PlayMicroStagger()
	{
		if (_paused || _swordMotion is null)
		{
			return;
		}

		_combat.ApplyStagger(_tuning.Value(TuningKey.StaggerMicro));
		if (_combat.State != CombatState.Stagger)
		{
			return;
		}

		PlayClip("StaggerMicro");
	}

	private void PlayDeath()
	{
		if (_paused || _swordMotion is null)
		{
			return;
		}

		_combat.Kill();
		if (!_combat.IsDead)
		{
			return;
		}

		PlayClip("Death");
	}

	private void PlayVictory()
	{
		if (_paused || _swordMotion is null || _combat.State != CombatState.Idle)
		{
			return;
		}

		PlayClip("Victory");
		_reactionAdvance = true;
	}

	private void SetPaused(bool paused)
	{
		_paused = paused;
		_clock.TimeScale = paused ? 0.0 : _baseTimeScale;
		_hud?.UpdatePause(_paused);
	}

	private void AdvanceGuard()
	{
		if (_swordMotion?.Clip is not { } clip || !_swordMotion.IsFinished)
		{
			return;
		}

		if (clip.Name == "GuardRaise")
		{
			PlayClip("GuardHold");
		}
		else if (clip.Name == "GuardLower")
		{
			PlayClip("SlashLeft");
		}
		else if (clip.Name == "Bash")
		{
			if (!_rightMouseWasDown)
			{
				// The FSM is already Idle by the time the bash clip ends, so the
				// idle gate would freeze the lower; let this one reaction play out.
				PlayClip("GuardLower");
				_reactionAdvance = true;
				return;
			}

			if (_combat.TryResumeGuard())
			{
				PlayClip("GuardHold");
			}

			// Otherwise the bash recovery is still committed; the next frame
			// resumes the guard as soon as the FSM reaches Idle.
		}
	}

	private void RestartSwordClip()
	{
		_duel.Reset();
		ResetRound();
	}

	private void ResetRound()
	{
		if (_swordMotion is null || _swordClips.Count == 0)
		{
			return;
		}

		_combat.Reset();
		_opponentCombat.Reset();
		_reactionAdvance = false;
		_bodyMotion?.Reset();
		_combatWorld?.UnregisterGuardBlade(FighterId.Player);
		_combatWorld?.UnregisterGuardBlade(FighterId.Opponent);
		_guardHurtbox = false;
		_opponentGuardHurtbox = false;
		_lastBlade = null;
		_opponentLastBlade = null;
		_swingHits.Reset();
		_opponentSwingHits.Reset();
		_ai?.Reset();
		ClearHitFlashes();
		_playerStats.Reset();
		_opponentStats.Reset();
		_staminaCue = 0f;
		_lastRejection = "-";
		_hitCount = 0;
		_lastHit = "-";
		_lastOutcome = "-";
		_blocksTaken = 0;
		_timedBlocksTaken = 0;
		_guardBreaksTaken = 0;
		_hitsTaken = 0;
		_foeBlocks = 0;
		_foeGuardBreaks = 0;
		_trailCount = 0;
		_trailIndex = 0;
		_contactTimer = 0f;
		_input.Clear();
		_swordMotion.ResetTo(_swordClips[0]);
		_opponentSwordMotion?.ResetTo(_swordClips[0]);
		_opponentBodyMotion?.Reset();
		ApplySwordPose();
	}

	private void ApplySwordPose()
	{
		if (_swordMotion is null)
		{
			return;
		}

		var pivot = _swordMotion.CurrentPose.ToMatrix();

		if (_player is not null && _bodyMotion is not null)
		{
			_player.BodyTransform = _bodyMotion.CurrentPose.ToMatrix();
		}

		// Only the player's sword follows input clips; the opponent holds idle
		// until the AI drives it, but still plays its hit/death reactions.
		if (_playerSword is not null)
		{
			_playerSword.Pivot = pivot;
		}

		if (_opponentSword is not null && _opponentSwordMotion is not null)
		{
			_opponentSword.Pivot = _opponentSwordMotion.CurrentPose.ToMatrix();
		}

		if (_opponent is not null && _opponentBodyMotion is not null)
		{
			_opponent.BodyTransform = _opponentBodyMotion.CurrentPose.ToMatrix();
		}

		if (_combatWorld is not null && _player is not null && _opponent is not null)
		{
			_combatWorld.UpdateFighter(FighterId.Player, _player.DrawTransform);
			_combatWorld.UpdateFighter(FighterId.Opponent, _opponent.DrawTransform);
		}
	}

	private void UpdateGuardHurtbox()
	{
		if (_combatWorld is null)
		{
			return;
		}

		var guarding = _combat.State == CombatState.GuardHold;
		if (guarding && _playerSword is not null && _player is not null)
		{
			var swordWorld = _playerSword.GetWorld(_player.GetRightHandTransform());
			var blade = BladeGeometry.FromWorld(swordWorld);

			if (!_guardHurtbox)
			{
				_combatWorld.RegisterGuardBlade(FighterId.Player, blade);
				_guardHurtbox = true;
			}
			else
			{
				_combatWorld.SetGuardBladePose(FighterId.Player, blade);
			}
		}
		else if (!guarding && _guardHurtbox)
		{
			_combatWorld.UnregisterGuardBlade(FighterId.Player);
			_guardHurtbox = false;
		}

		var opponentGuarding = _opponentCombat.State == CombatState.GuardHold;
		if (opponentGuarding && _opponentSword is not null && _opponent is not null)
		{
			var swordWorld = _opponentSword.GetWorld(_opponent.GetRightHandTransform());
			var blade = BladeGeometry.FromWorld(swordWorld);

			if (!_opponentGuardHurtbox)
			{
				_combatWorld.RegisterGuardBlade(FighterId.Opponent, blade);
				_opponentGuardHurtbox = true;
			}
			else
			{
				_combatWorld.SetGuardBladePose(FighterId.Opponent, blade);
			}
		}
		else if (!opponentGuarding && _opponentGuardHurtbox)
		{
			_combatWorld.UnregisterGuardBlade(FighterId.Opponent);
			_opponentGuardHurtbox = false;
		}
	}

	private void DrawSwordGizmo(GreatswordRig sword, Matrix swordWorld)
	{
		if (_overlay?.Visible != true)
		{
			return;
		}

		DrawSwordMarker(swordWorld, Vector3.Zero, _gizmoGrip);
		DrawSwordMarker(swordWorld, new Vector3(0f, 0.8f, 0f), _gizmoMid);
		DrawSwordMarker(swordWorld, new Vector3(0f, GreatswordRig.BladeTipY, 0f), _gizmoTip);
	}

	private void DrawSwordMarker(Matrix swordWorld, Vector3 localPosition, DrMaterial? material)
	{
		if (_renderer is null || _primitives is null || material is null)
		{
			return;
		}

		var world = Matrix.CreateScale(0.06f) *
					Matrix.CreateTranslation(Vector3.Transform(localPosition, swordWorld));
		_renderer.Draw(_primitives.Sphere, material, world);
	}

	private void DrawBladeCapsule(Matrix swordWorld)
	{
		if (_overlay?.Visible != true || _renderer is null || _primitives is null || _bladeDebug is null)
		{
			return;
		}

		var blade = BladeGeometry.FromWorld(swordWorld);
		if (blade.Length <= 0.0001f)
		{
			return;
		}

		var rotation = blade.AxisRotation;
		var axisWorld = Matrix.CreateScale(blade.Length) *
						Matrix.CreateFromQuaternion(rotation) *
						Matrix.CreateTranslation(blade.Base);
		_renderer.DrawLines(_primitives.LineSegment, _bladeDebug, axisWorld);

		DrawBladeRing(blade.Base, rotation, blade.BaseRadius);
		DrawBladeRing(blade.Tip, rotation, blade.TipRadius);
	}

	private void DrawBladeRing(Vector3 center, Quaternion rotation, float radius)
	{
		if (_renderer is null || _primitives is null || _bladeDebug is null)
		{
			return;
		}

		var world = Matrix.CreateScale(radius) *
					Matrix.CreateFromQuaternion(rotation) *
					Matrix.CreateTranslation(center);
		_renderer.DrawLines(_primitives.LineCircle, _bladeDebug, world);
	}

	private void DrawBladeTrail()
	{
		if (_renderer is null || _primitives is null || _trailDebug is null)
		{
			return;
		}

		for (var i = 0; i < _trailCount; i++)
		{
			var index = (_trailIndex - 1 - i + _bladeTrail.Length * 2) % _bladeTrail.Length;
			var blade = _bladeTrail[index];
			if (blade.Length <= 1e-4f)
			{
				continue;
			}

			var world = Matrix.CreateScale(blade.Length) *
						Matrix.CreateFromQuaternion(blade.AxisRotation) *
						Matrix.CreateTranslation(blade.Base);
			_renderer.DrawLines(_primitives.LineSegment, _trailDebug, world);
		}
	}

	private void DrawContactMarker()
	{
		if (_contactTimer <= 0f || _renderer is null || _primitives is null || _hitDebug is null)
		{
			return;
		}

		var dot = Matrix.CreateScale(0.05f) * Matrix.CreateTranslation(_contactPoint);
		_renderer.Draw(_primitives.Sphere, _hitDebug, dot);

		if (_contactNormal.LengthSquared() > 1e-6f)
		{
			var normal = Vector3.Normalize(_contactNormal);
			var line = Matrix.CreateScale(0.35f) *
					   Matrix.CreateFromQuaternion(GeometryMath.RotationFromY(normal)) *
					   Matrix.CreateTranslation(_contactPoint);
			_renderer.DrawLines(_primitives.LineSegment, _hitDebug, line);
		}
	}

	private void DrawHurtboxes()
	{
		if (_combatWorld is null || _renderer is null || _primitives is null)
		{
			return;
		}

		foreach (var entry in _combatWorld.Hurtboxes.Entries)
		{
			if (!_combatWorld.TryGetPose(entry.Key, out var center, out var orientation))
			{
				continue;
			}

			var definition = HurtboxDefinition.For(entry.Value.Kind);
			var flashing = _hitFlashes[HitIndex(entry.Value.Fighter, entry.Value.Kind)] > 0f;
			var material = flashing
				? _hitDebug
				: entry.Value.Kind == HurtboxKind.GuardBlade ? _guardDebug : _hurtboxDebug;
			if (material is null)
			{
				continue;
			}

			if (definition.Shape == HurtboxShapeKind.Sphere)
			{
				DrawWireSphere(center, definition.Radius, material);
			}
			else
			{
				DrawWireCapsule(center, orientation, definition.Radius,
					definition.HalfLength, material);
			}
		}
	}

	private void DrawWireSphere(Vector3 center, float radius, DrMaterial material)
	{
		if (_renderer is null || _primitives is null)
		{
			return;
		}

		var xz = Matrix.CreateScale(radius) * Matrix.CreateTranslation(center);
		var xy = Matrix.CreateScale(radius) *
				 Matrix.CreateRotationX(MathHelper.PiOver2) *
				 Matrix.CreateTranslation(center);
		var yz = Matrix.CreateScale(radius) *
				 Matrix.CreateRotationZ(MathHelper.PiOver2) *
				 Matrix.CreateTranslation(center);

		_renderer.DrawLines(_primitives.LineCircle, material, xz);
		_renderer.DrawLines(_primitives.LineCircle, material, xy);
		_renderer.DrawLines(_primitives.LineCircle, material, yz);
	}

	private void DrawWireCapsule(Vector3 center, Quaternion orientation,
		float radius, float halfLength, DrMaterial material)
	{
		if (_renderer is null || _primitives is null)
		{
			return;
		}

		var rotation = Matrix.CreateFromQuaternion(orientation);

		void Ring(float y)
		{
			var world = Matrix.CreateScale(radius) *
						Matrix.CreateTranslation(0f, y, 0f) *
						rotation *
						Matrix.CreateTranslation(center);
			_renderer.DrawLines(_primitives.LineCircle, material, world);
		}

		void Rail(float x, float z)
		{
			var world = Matrix.CreateScale(1f, MathF.Max(2f * halfLength, 0.0001f), 1f) *
						Matrix.CreateTranslation(x, -halfLength, z) *
						rotation *
						Matrix.CreateTranslation(center);
			_renderer.DrawLines(_primitives.LineSegment, material, world);
		}

		Ring(halfLength);
		Ring(-halfLength);
		Rail(radius, 0f);
		Rail(-radius, 0f);
		Rail(0f, radius);
		Rail(0f, -radius);
	}

	/// <summary>
	/// Corner gizmo showing the camera basis in world space: X = right (red),
	/// Y = up (green), Z = forward (blue). A fixed isometric view makes the
	/// camera sway readable.
	/// </summary>
	private void DrawCameraAxesGizmo()
	{
		if (_overlay?.Visible != true || _renderer is null || _primitives is null ||
			_axisX is null || _axisY is null || _axisZ is null)
		{
			return;
		}

		const int size = 150;
		const int margin = 16;
		var originalViewport = GraphicsDevice.Viewport;
		var drawCalls = _renderer.DrawCalls;
		var primitives = _renderer.PrimitivesDrawn;

		var view = Matrix.CreateLookAt(new Vector3(2.2f, 2.2f, 2.2f), Vector3.Zero, Vector3.Up);
		var projection = Matrix.CreateOrthographic(2.2f, 2.2f, 0.1f, 10f);

		var gizmoViewport = new Viewport(
			margin,
			GraphicsDevice.PresentationParameters.BackBufferHeight - size - margin,
			size,
			size);

		GraphicsDevice.Viewport = gizmoViewport;
		_renderer.Begin(view, projection);

		var cameraView = _camera.View;
		cameraView.Translation = Vector3.Zero;
		var right = new Vector3(cameraView.M11, cameraView.M12, cameraView.M13);
		var up = new Vector3(cameraView.M21, cameraView.M22, cameraView.M23);
		var forward = -new Vector3(cameraView.M31, cameraView.M32, cameraView.M33);

		DrawAxis(right, _axisX);
		DrawAxis(up, _axisY);
		DrawAxis(forward, _axisZ);

		_renderer.End();
		_renderer.RestoreCounters(drawCalls, primitives);
		GraphicsDevice.Viewport = originalViewport;

		DrawAxisLabels(gizmoViewport, right, up, forward, view, projection);
	}

	private void DrawAxisLabels(Viewport viewport, Vector3 right, Vector3 up, Vector3 forward,
		Matrix view, Matrix projection)
	{
		if (_spriteBatch is null)
		{
			return;
		}

		var origin = viewport.Project(Vector3.Zero, projection, view, Matrix.Identity);
		_spriteBatch.Begin();
		DrawAxisLabel("X", right, AxisXColor, origin, view, projection, viewport);
		DrawAxisLabel("Y", up, AxisYColor, origin, view, projection, viewport);
		DrawAxisLabel("Z", forward, AxisZColor, origin, view, projection, viewport);
		_spriteBatch.End();
	}

	private void DrawAxisLabel(string text, Vector3 direction, Color color, Vector3 origin,
		Matrix view, Matrix projection, Viewport viewport)
	{
		if (_spriteBatch is null)
		{
			return;
		}

		var point = viewport.Project(direction, projection, view, Matrix.Identity);
		var screen = new Vector2(point.X, point.Y);
		var away = screen - new Vector2(origin.X, origin.Y);

		if (away.LengthSquared() > 0.0001f)
		{
			screen += Vector2.Normalize(away) * 9f;
		}

		DefaultAssets.DebugFont.DrawText(_spriteBatch, text, screen, color);
	}

	private void DrawAxis(Vector3 direction, DrMaterial material)
	{
		if (_renderer is null || _primitives is null)
		{
			return;
		}

		var world = Matrix.CreateFromQuaternion(GeometryMath.RotationFromY(direction));
		_renderer.DrawLines(_primitives.LineSegment, material, world);
	}

	private void UpdateDebugText()
	{
		if (_hud is null)
		{
			return;
		}

		_hud.UpdateStats(_fps, _renderer?.DrawCalls ?? 0, _renderer?.PrimitivesDrawn ?? 0);
		_hud.UpdatePause(_paused);
		_hud.UpdateVitals(_playerStats.Health, _playerStats.MaxHealth,
			_playerStats.Stamina, _playerStats.MaxStamina, _staminaCue > 0f);
		_hud.UpdateDuel(_duel.Round, _duel.PlayerScore, _duel.OpponentScore);
		var buffered = _input.IsPending ? $"buffered {_input.Pending}" : "idle";
		var rejection = _lastRejection == "-" ? string.Empty : $"  reject {_lastRejection}";
		var motion = FormattableString.Invariant(
			$"move {_moveInput.X:0},{_moveInput.Y:0}  pos {_player?.Position.X ?? 0f:0.00},{_player?.Position.Z ?? 0f:0.00}  yaw {_camera.Yaw:0.00}  face {_player?.FacingYaw ?? 0f:0.00}  hits {_hitCount} {_lastHit}  foe {_opponentStats.Health:0}/{_opponentStats.MaxHealth:0} @ {_opponent?.Position.X ?? 0f:0.00},{_opponent?.Position.Z ?? 0f:0.00}");
		var vitals = FormattableString.Invariant(
			$"vitals hp {_playerStats.Health:0}/{_playerStats.MaxHealth:0} st {_playerStats.Stamina:0} poise {_playerStats.Poise:0}/{_playerStats.MaxPoise:0}");
		var guard = $"guard blade={(_guardHurtbox ? "yes" : "no")} " +
					$"block={(_combat.Blocks ? "yes" : "no")} last={_lastOutcome} " +
					$"blk {_blocksTaken}/{_timedBlocksTaken} brk {_guardBreaksTaken} hit {_hitsTaken} " +
					$"| foe {_opponentCombat.State}/{_opponentSwordMotion?.Clip?.Name ?? "-"} " +
					$"blk {_foeBlocks} brk {_foeGuardBreaks}";
		var tune = FormattableString.Invariant(
			$"tune {TuningSession.Name(_tuning.Selected)} {_tuning.SelectedValue * 1000:0} ms  [ ] nudge  ; select  -> {_tuning.LastChange}");
		_overlay?.Update(_clock, _combat, _swordMotion?.Clip?.Name ?? "-",
			_swordMotion?.NormalizedTime ?? 0f, vitals, guard, tune,
			$"intent {buffered}{rejection}", motion);
	}

	protected override void Draw(GameTime gameTime)
	{
		GraphicsDevice.Clear(BackgroundColor);
		_backdrop?.Draw();

		if (_renderer is not null && _arena is not null)
		{
			var width = GraphicsDevice.PresentationParameters.BackBufferWidth;
			var height = GraphicsDevice.PresentationParameters.BackBufferHeight;
			var aspect = width / (float)Math.Max(1, height);

			_renderer.Begin(_camera.View, _camera.GetProjection(aspect));
			_renderer.Draw(_arena.Instance, Matrix.Identity);

			if (_player is not null)
			{
				// First person sits inside the player's head; hide the capsule
				// but keep the sword, exactly like Skyrim.
				if (!_camera.FirstPerson)
				{
					_renderer.Draw(_player.Instance, _player.DrawTransform);
				}

				if (_playerSword is not null)
				{
					var swordWorld = _playerSword.GetWorld(_player.GetRightHandTransform());
					_renderer.Draw(_playerSword.Instance, swordWorld);
					DrawSwordGizmo(_playerSword, swordWorld);
					DrawBladeCapsule(swordWorld);
				}
			}

			if (_opponent is not null)
			{
				_renderer.Draw(_opponent.Instance, _opponent.DrawTransform);

				if (_opponentSword is not null)
				{
					var swordWorld = _opponentSword.GetWorld(_opponent.GetRightHandTransform());
					_renderer.Draw(_opponentSword.Instance, swordWorld);
					DrawSwordGizmo(_opponentSword, swordWorld);
					DrawBladeCapsule(swordWorld);
				}
			}

			if (_renderer.Wireframe)
			{
				DrawHurtboxes();
			}

			if (_overlay?.Visible == true)
			{
				DrawBladeTrail();
				DrawContactMarker();
			}

			_renderer.End();
			DrawCameraAxesGizmo();
		}

		_ui?.Draw();

		base.Draw(gameTime);
	}
}
