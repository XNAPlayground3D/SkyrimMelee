using DigitalRiseModel;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra;
using SkyrimMelee.Combat;
using SkyrimMelee.Core;
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
	private ScreenGradient? _backdrop;
	private SpriteBatch? _spriteBatch;
	private SwordMotion? _swordMotion;
	private IReadOnlyList<SwordClip> _swordClips = Array.Empty<SwordClip>();
	private AttackDefinition? _slashAttack;
	private AttackDefinition? _slashRightAttack;
	private readonly CombatStateMachine _combat = new();
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

		_swordClips = SwordClips.Create();
		var attacks = AttackCatalog.Create(_swordClips);
		_slashAttack = attacks.Single(attack => attack.Name == "SlashLeft");
		_slashRightAttack = attacks.Single(attack => attack.Name == "SlashRight");
		_swordMotion = new SwordMotion();
		_swordMotion.ResetTo(_swordClips[0], _options.ClipTime ?? 0f);

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
		UpdateFps(elapsed);
		UpdateSwordClipInput(keyboard);

		var mouse = Mouse.GetState();
		var leftMouseDown = mouse.LeftButton == ButtonState.Pressed;
		if (leftMouseDown && !_leftMouseWasDown)
		{
			PlaySlash();
		}

		_leftMouseWasDown = leftMouseDown;

		var rightMouseDown = mouse.RightButton == ButtonState.Pressed;
		if (rightMouseDown && !_rightMouseWasDown)
		{
			PlayGuardRaise();
		}
		else if (!rightMouseDown && _rightMouseWasDown)
		{
			PlayGuardLower();
		}

		_rightMouseWasDown = rightMouseDown;

		var steps = _clock.Advance(elapsed);
		for (var i = 0; i < steps; i++)
		{
			StepSimulation();
		}

		if (_swordMotion is not null)
		{
			if (!_paused)
			{
				AdvanceGuard();
			}

			ApplySwordPose();

			_hud?.UpdatePlayback(_swordMotion.Clip?.Name ?? "-", _swordClipSpeed,
				_swordMotion.Time, _swordMotion.Clip?.Duration ?? 0f);
		}

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

		if (_combat.State != CombatState.Idle)
		{
			_swordMotion?.Update(step);
		}

		_combat.Update(step);
	}

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

	private void UpdateSwordClipInput(KeyboardState keyboard)
	{
		var leftSlash = keyboard.IsKeyDown(Keys.D1) || keyboard.IsKeyDown(Keys.NumPad1);
		if (leftSlash && !_d1WasDown)
		{
			PlaySlash();
		}

		_d1WasDown = leftSlash;

		var rightSlash = keyboard.IsKeyDown(Keys.D2) || keyboard.IsKeyDown(Keys.NumPad2);
		if (rightSlash && !_d2WasDown)
		{
			PlaySlashRight();
		}

		_d2WasDown = rightSlash;

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
		ApplySwordPose();
	}

	private void PlaySlash()
	{
		if (_paused || _swordMotion is null || _slashAttack is null ||
			!_combat.TryStartAttack(_slashAttack))
		{
			return;
		}

		PlayClip("SlashLeft");
	}

	private void PlaySlashRight()
	{
		if (_paused || _swordMotion is null || _slashRightAttack is null ||
			!_combat.TryStartAttack(_slashRightAttack))
		{
			return;
		}

		PlayClip("SlashRight");
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
	}

	private void RestartSwordClip()
	{
		if (_swordMotion is null || _swordClips.Count == 0)
		{
			return;
		}

		_combat.Reset();
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

		if (_playerSword is not null)
		{
			_playerSword.Pivot = pivot;
		}

		if (_opponentSword is not null)
		{
			_opponentSword.Pivot = pivot;
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
		_overlay?.Update(_clock, _combat, _swordMotion?.Clip?.Name ?? "-",
			_swordMotion?.NormalizedTime ?? 0f);
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
				_renderer.Draw(_player.Instance, _player.Transform);

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

			_renderer.End();
			DrawCameraAxesGizmo();
		}

		_ui?.Draw();

		base.Draw(gameTime);
	}
}
