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
	private CombatWorld? _combatWorld;
	private BladeSweep? _bladeSweep;
	private readonly SwingHitSet _swingHits = new();
	private readonly InputRouter _input = new();
	private string _lastRejection = "-";
	private readonly FighterStats _playerStats = new();
	private readonly FighterStats _opponentStats = new();
	private BladeGeometry? _lastBlade;
	private readonly float[] _hitFlashes = new float[2 * 5];
	private float _staminaCue;
	private bool _guardHurtbox;
	private ScreenGradient? _backdrop;
	private SpriteBatch? _spriteBatch;
	private SwordMotion? _swordMotion;
	private BodyMotion? _bodyMotion;
	private IReadOnlyList<SwordClip> _swordClips = Array.Empty<SwordClip>();
	private IReadOnlyList<BodyClip> _bodyClips = Array.Empty<BodyClip>();
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
	private bool _fWasDown;
	private bool _reactionAdvance;
	private bool _leftMouseWasDown;
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

		_combatWorld = new CombatWorld();
		_combatWorld.AddFighter(FighterId.Player, _player.DrawTransform);
		_combatWorld.AddFighter(FighterId.Opponent, _opponent.Transform);
		_bladeSweep = new BladeSweep(_combatWorld);

		if (_options.ClipTime is not null)
		{
			SetPaused(true);
		}

		ApplySwordPose();

		_backdrop = new ScreenGradient(GraphicsDevice);
		_spriteBatch = new SpriteBatch(GraphicsDevice);
		_camera.Update(_clock.SimTime, 0f);

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
		if (_input.Tick((float)elapsed))
		{
			_lastRejection = "buffer expired";
		}

		UpdateFps(elapsed);
		UpdateSwordClipInput(keyboard, uiCaptured);

		var mouse = Mouse.GetState();
		var leftMouseDown = mouse.LeftButton == ButtonState.Pressed;
		if (leftMouseDown && !_leftMouseWasDown && !_paused && !uiCaptured)
		{
			var shift = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);
			if (shift)
			{
				_input.Push(CombatIntent.PowerSlash);
			}
			else if (_combat.State is CombatState.GuardRaise or CombatState.GuardHold)
			{
				_input.Push(CombatIntent.Bash);
			}
			else
			{
				_input.Push(CombatIntent.SlashLeft);
			}
		}

		_leftMouseWasDown = leftMouseDown;

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
		_camera.Update(_clock.SimTime, (float)elapsed);

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
		var wasActive = _combat.State == CombatState.Active;

		if (_combat.State != CombatState.Idle || _reactionAdvance)
		{
			_swordMotion?.Update(step);
			_bodyMotion?.Update(step);
		}

		_combat.Update(step);
		_opponentCombat.Update(step);

		var blade = CurrentPlayerBlade();
		if ((wasActive || _combat.State == CombatState.Active) &&
			_lastBlade is { } previous && blade is { } current)
		{
			ResolveHits(previous, current, step);
		}

		_lastBlade = blade;
		UpdateHitFlashes(step);

		var blocking = _combat.State is CombatState.GuardRaise or
			CombatState.GuardHold or CombatState.GuardLower;
		_playerStats.Update(step, blocking);
		_opponentStats.Update(step, false);

		if (_duel.Update(step))
		{
			ResetRound();
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

	private void ResolveHits(BladeGeometry previous, BladeGeometry current, float step)
	{
		if (_bladeSweep is null)
		{
			return;
		}

		foreach (var hit in _bladeSweep.Sweep(previous, current, step))
		{
			if (!_swingHits.TryRegister(hit, FighterId.Player, _combat.AttackTime,
					out var hitEvent))
			{
				continue;
			}

			if (hitEvent.Target != FighterId.Opponent ||
				hitEvent.Hurtbox == HurtboxKind.GuardBlade ||
				_opponentStats.IsDead)
			{
				continue;
			}

			_hitFlashes[HitIndex(hitEvent.Target, hitEvent.Hurtbox)] =
				CombatTuning.HitFlashDuration;
			_opponent?.SetBodyPartColor(hitEvent.Hurtbox, HitFlashColor);

			var stagger = StaggerRules.Resolve(hitEvent.Attack, _opponentStats);
			if (stagger.Staggered)
			{
				_opponentCombat.ApplyStagger(stagger.Duration);
			}

			var defense = DefenseResolver.Resolve(
				hitEvent.Attack, _playerStats, _opponentStats, HitOutcome.Clean);
			_opponentStats.ApplyDamage(defense.Damage.Final);

			if (_opponentStats.IsDead)
			{
				_opponentCombat.Kill();
				_duel.ReportDeath(FighterId.Opponent);
			}
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

		var bash = keyboard.IsKeyDown(Keys.F);
		if (bash && !_fWasDown && !_paused && !uiCaptured)
		{
			_input.Push(CombatIntent.Bash);
		}

		_fWasDown = bash;

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

		_playerStats.Spend(attack);
		_swingHits.Begin(attack);
		PlayClip(attack.Name);
		_lastRejection = "-";
		return true;
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
		if (_paused || _swordMotion is null || !_combat.TryStartGuard())
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

		var duration = large ? CombatTuning.StaggerLargeDuration : CombatTuning.StaggerSmallDuration;
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

		_combat.ApplyStagger(CombatTuning.StaggerMicroDuration);
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
			if (_rightMouseWasDown && _combat.TryResumeGuard())
			{
				PlayClip("GuardHold");
			}
			else
			{
				PlayClip("GuardLower");
			}
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
		_guardHurtbox = false;
		_lastBlade = null;
		_swingHits.Reset();
		ClearHitFlashes();
		_playerStats.Reset();
		_opponentStats.Reset();
		_staminaCue = 0f;
		_lastRejection = "-";
		_input.Clear();
		_swordMotion.ResetTo(_swordClips[0]);
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

		if (_playerSword is not null)
		{
			_playerSword.Pivot = pivot;
		}

		if (_opponentSword is not null)
		{
			_opponentSword.Pivot = pivot;
		}

		if (_combatWorld is not null && _player is not null && _opponent is not null)
		{
			_combatWorld.UpdateFighter(FighterId.Player, _player.DrawTransform);
			_combatWorld.UpdateFighter(FighterId.Opponent, _opponent.Transform);
		}
	}

	private void UpdateGuardHurtbox()
	{
		if (_combatWorld is null)
		{
			return;
		}

		var guarding = _combat.State == CombatState.GuardHold;
		if (guarding && !_guardHurtbox)
		{
			if (_swordMotion is not null && _playerSword is not null && _player is not null)
			{
				var swordWorld = _playerSword.GetWorld(_player.GetRightHandTransform());
				_combatWorld.RegisterGuardBlade(
					FighterId.Player, BladeGeometry.FromWorld(swordWorld));
				_guardHurtbox = true;
			}
		}
		else if (!guarding && _guardHurtbox)
		{
			_combatWorld.UnregisterGuardBlade(FighterId.Player);
			_guardHurtbox = false;
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
		_overlay?.Update(_clock, _combat, _swordMotion?.Clip?.Name ?? "-",
			_swordMotion?.NormalizedTime ?? 0f, $"intent {buffered}{rejection}");
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
				_renderer.Draw(_player.Instance, _player.DrawTransform);

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
				_renderer.Draw(_opponent.Instance, _opponent.Transform);

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

			_renderer.End();
			DrawCameraAxesGizmo();
		}

		_ui?.Draw();

		base.Draw(gameTime);
	}
}
