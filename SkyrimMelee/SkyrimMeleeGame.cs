using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra;
using Myra.Graphics2D.UI;
using SkyrimMelee.Core;
using SkyrimMelee.Debug;
using SkyrimMelee.Graphics;
using SkyrimMelee.Ui;

namespace SkyrimMelee;

public sealed class SkyrimMeleeGame : Game
{
	private static readonly Color BackgroundColor = new(14, 15, 20);
	private static readonly Color TextColor = new(230, 220, 200);

#if DEBUG
	private const string BuildConfiguration = "Debug";
#else
	private const string BuildConfiguration = "Release";
#endif

	private readonly GameOptions _options;
	private readonly GameClock _clock = new();
	private readonly Camera _camera = new();
	private readonly GraphicsDeviceManager _graphics;

	private MyraHost? _ui;
	private Label? _debugLabel;
	private DebugOverlay? _overlay;
	private PrimitiveLibrary? _primitives;
	private ForwardRenderer? _renderer;
	private Arena? _arena;
	private ScreenGradient? _backdrop;
	private float _fps;
	private double _fpsWindowTime;
	private int _fpsWindowFrames;
	private double _labelTimer;
	private bool _f1WasDown;

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

			_debugLabel = new Label
			{
				Text = "SkyrimMelee",
				Font = DefaultAssets.DebugFontSystem.GetFont(20),
				TextColor = TextColor,
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Left = 24,
				Top = 20
			};

			_overlay = new DebugOverlay(_options);
			_overlay.Visible = _options.Debug;

			var root = new Panel();
			root.Widgets.Add(_debugLabel);
			root.Widgets.Add(_overlay.Widget);
			_ui.Root = root;
		}

		_primitives = new PrimitiveLibrary(GraphicsDevice);
		_renderer = new ForwardRenderer(GraphicsDevice);
		_arena = new Arena(_primitives);
		_backdrop = new ScreenGradient(GraphicsDevice);
		_camera.Update(_clock.SimTime, 0f);

		base.LoadContent();
	}

	protected override void UnloadContent()
	{
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

		var elapsed = gameTime.ElapsedGameTime.TotalSeconds;
		UpdateFps(elapsed);

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

	private void UpdateDebugText()
	{
		if (_debugLabel is null || _ui is null)
		{
			return;
		}

		var width = GraphicsDevice.PresentationParameters.BackBufferWidth;
		var height = GraphicsDevice.PresentationParameters.BackBufferHeight;
		var driftMs = _clock.Drift * 1000.0;
		var god = _options.GodMode ? "  |  god" : string.Empty;

		_debugLabel.Text = FormattableString.Invariant(
			$"SkyrimMelee  |  {BuildConfiguration}  |  {_fps:0.0} FPS\n{width}x{height}  |  sim {_clock.FrameIndex}  |  {_clock.TimeScale:0.00}x  |  drift {driftMs:+0.0;-0.0;0.0} ms  |  dropped {_clock.TotalDroppedSteps}{god}");

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
			_renderer.End();
		}

		_ui?.Draw();

		base.Draw(gameTime);
	}
}
