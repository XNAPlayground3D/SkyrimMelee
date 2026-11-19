using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Myra;
using Myra.Graphics2D.UI;
using SkyrimMelee.Core;
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

	private readonly FixedStepLoop _loop = new();
	private readonly GraphicsDeviceManager _graphics;

	private MyraHost? _ui;
	private Label? _debugLabel;
	private float _fps;
	private double _fpsWindowTime;
	private int _fpsWindowFrames;
	private double _wallTime;
	private double _labelTimer;
	private long _droppedSteps;

	public SkyrimMeleeGame()
	{
		_graphics = new GraphicsDeviceManager(this)
		{
			PreferredBackBufferWidth = 1600,
			PreferredBackBufferHeight = 900,
			SynchronizeWithVerticalRetrace = true
		};

		// Draw at the display rate; the simulation advances through FixedStepLoop.
		IsFixedTimeStep = false;
		IsMouseVisible = true;

		Window.Title = "SkyrimMelee";
		Window.AllowUserResizing = true;
	}

	/// <summary>Number of fixed 60 Hz simulation steps since launch.</summary>
	public long SimulationSteps { get; private set; }

	protected override void Initialize()
	{
		_graphics.ApplyChanges();
		base.Initialize();
	}

	protected override void LoadContent()
	{
		_ui = new MyraHost(this);

		var font = DefaultAssets.DebugFontSystem.GetFont(20);
		_debugLabel = new Label
		{
			Text = "SkyrimMelee",
			Font = font,
			TextColor = TextColor,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Left = 24,
			Top = 20
		};

		var root = new Panel();
		root.Widgets.Add(_debugLabel);
		_ui.Root = root;

		base.LoadContent();
	}

	protected override void UnloadContent()
	{
		_ui?.Dispose();
		_ui = null;
		base.UnloadContent();
	}

	protected override void Update(GameTime gameTime)
	{
		if (Keyboard.GetState().IsKeyDown(Keys.Escape) ||
			GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
		{
			Exit();
			return;
		}

		var elapsed = gameTime.ElapsedGameTime.TotalSeconds;
		_wallTime += elapsed;
		UpdateFps(elapsed);

		var steps = _loop.Advance(elapsed);
		_droppedSteps += _loop.DroppedSteps;
		for (var i = 0; i < steps; i++)
		{
			StepSimulation();
		}

		_labelTimer += elapsed;
		if (_labelTimer >= 0.25)
		{
			_labelTimer = 0;
			UpdateDebugLabel();
		}

		base.Update(gameTime);
	}

	private void StepSimulation()
	{
		SimulationSteps++;

		// T0.3: still a skeleton. Round state, combat, physics and AI hook in from T1 onward.
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

	private void UpdateDebugLabel()
	{
		if (_debugLabel is null || _ui is null)
		{
			return;
		}

		var driftMs = (SimulationSteps * FixedStepLoop.StepDuration - _wallTime) * 1000.0;
		var width = GraphicsDevice.PresentationParameters.BackBufferWidth;
		var height = GraphicsDevice.PresentationParameters.BackBufferHeight;

		_debugLabel.Text = FormattableString.Invariant(
			$"SkyrimMelee  |  {BuildConfiguration}  |  {_fps:0.0} FPS\n{width}x{height}  |  sim {SimulationSteps}  |  drift {driftMs:+0.0;-0.0;0.0} ms  |  dropped {_droppedSteps}");
	}

	protected override void Draw(GameTime gameTime)
	{
		GraphicsDevice.Clear(BackgroundColor);

		_ui?.Draw();

		base.Draw(gameTime);
	}
}
