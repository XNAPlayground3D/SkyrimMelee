using DigitalRiseModel;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SkyrimMelee.Core;
using SkyrimMelee.Fighters;
using SkyrimMelee.Graphics;
using SkyrimMelee.Ui;

namespace SkyrimMelee;

public sealed class SkyrimMeleeGame : Game
{
	private static readonly Color BackgroundColor = new(14, 15, 20);

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
	private ScreenGradient? _backdrop;
	private float _swordYaw;
	private float _swordPitch = 0.18f;
	private float _swordRoll = -0.85f;
	private float _fps;
	private double _fpsWindowTime;
	private int _fpsWindowFrames;
	private double _labelTimer;
	private bool _f1WasDown;
	private bool _f2WasDown;

	public SkyrimMeleeGame(GameOptions options)
	{
		_options = options;
		_graphics = new GraphicsDeviceManager(this)
		{
			PreferredBackBufferWidth = 1600,
			PreferredBackBufferHeight = 900,
			SynchronizeWithVerticalRetrace = true
		};

		if (options.SlowMotion)
		{
			_clock.TimeScale = 0.25;
		}

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
		_player = new FighterRig(_primitives, FighterVisuals.Player, new Vector3(-1f, 0f, 0f), 0f);
		_opponent = new FighterRig(_primitives, FighterVisuals.Opponent, new Vector3(1f, 0f, 0f), MathHelper.Pi);
		_playerSword = new GreatswordRig(_primitives);
		_opponentSword = new GreatswordRig(_primitives);
		_gizmoGrip = _primitives.CreateMaterial(new Color(0xFF, 0x6B, 0x4A));
		_gizmoMid = _primitives.CreateMaterial(new Color(0x9F, 0xD8, 0xFF));
		_gizmoTip = _primitives.CreateMaterial(new Color(0xFF, 0xD2, 0x7A));
		UpdateSwordPivot();

		_backdrop = new ScreenGradient(GraphicsDevice);
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
		UpdateSwordPivotInput(keyboard, (float)elapsed);

		var steps = _clock.Advance(elapsed);
		for (var i = 0; i < steps; i++)
		{
			StepSimulation();
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
		// T0.4: still a skeleton. Round state, combat, physics and AI hook in from T1 onward.
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

	private void UpdateSwordPivotInput(KeyboardState keyboard, float elapsed)
	{
		const float rate = 1.6f;

		if (keyboard.IsKeyDown(Keys.Left) || keyboard.IsKeyDown(Keys.J))
		{
			_swordYaw -= rate * elapsed;
		}

		if (keyboard.IsKeyDown(Keys.Right) || keyboard.IsKeyDown(Keys.L))
		{
			_swordYaw += rate * elapsed;
		}

		if (keyboard.IsKeyDown(Keys.Up) || keyboard.IsKeyDown(Keys.I))
		{
			_swordPitch -= rate * elapsed;
		}

		if (keyboard.IsKeyDown(Keys.Down) || keyboard.IsKeyDown(Keys.K))
		{
			_swordPitch += rate * elapsed;
		}

		if (keyboard.IsKeyDown(Keys.Q))
		{
			_swordRoll += rate * elapsed;
		}

		if (keyboard.IsKeyDown(Keys.E))
		{
			_swordRoll -= rate * elapsed;
		}

		UpdateSwordPivot();
	}

	private void UpdateSwordPivot()
	{
		var pivot = Matrix.CreateFromYawPitchRoll(_swordYaw, _swordPitch, _swordRoll);

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

	private void UpdateDebugText()
	{
		if (_hud is null)
		{
			return;
		}

		_hud.UpdateStats(_fps, _renderer?.DrawCalls ?? 0, _renderer?.PrimitivesDrawn ?? 0);
		_overlay?.Update(_clock);
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
				}
			}

			_renderer.End();
		}

		_ui?.Draw();

		base.Draw(gameTime);
	}
}
