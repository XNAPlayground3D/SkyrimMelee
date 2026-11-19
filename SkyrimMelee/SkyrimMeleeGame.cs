using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SkyrimMelee.Core;

namespace SkyrimMelee;

public sealed class SkyrimMeleeGame : Game
{
	private static readonly Color BackgroundColor = new(14, 15, 20);

	private readonly FixedStepLoop _loop = new();
	private readonly GraphicsDeviceManager _graphics;

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

	protected override void Update(GameTime gameTime)
	{
		if (Keyboard.GetState().IsKeyDown(Keys.Escape) ||
			GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
		{
			Exit();
			return;
		}

		var steps = _loop.Advance(gameTime.ElapsedGameTime.TotalSeconds);
		for (var i = 0; i < steps; i++)
		{
			StepSimulation();
		}

		base.Update(gameTime);
	}

	private void StepSimulation()
	{
		SimulationSteps++;

		// T0.2: skeleton only. Round state, combat, physics and AI hook in from T0.3 onward.
	}

	protected override void Draw(GameTime gameTime)
	{
		GraphicsDevice.Clear(BackgroundColor);

		base.Draw(gameTime);
	}
}
